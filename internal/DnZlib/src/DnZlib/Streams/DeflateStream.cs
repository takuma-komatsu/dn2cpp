using System.Buffers;
using DnZlib.Streams;

namespace DnZlib.Streams;

/// <summary>
/// A <see cref="Stream"/> that compresses to / decompresses from raw DEFLATE (RFC 1951) using the
/// DnZlib engine. <see cref="ZLibStream"/> and <see cref="GZipStream"/> derive from it with a
/// different container format.
/// </summary>
public class DeflateStream : Stream
{
    private const int DefaultBufferSize = 8192;

    private Stream _base;
    private readonly bool _leaveOpen;
    private readonly CompressionMode _mode;
    private ZStream? _zs;
    private byte[]? _buffer;

    // Decompression input buffering.
    private int _inStart;
    private int _inEnd;
    private bool _eof;

    private bool _finished;
    private bool _disposed;

    /// <summary>Create a raw DEFLATE stream in the given mode.</summary>
    public DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
        : this(stream, mode, leaveOpen, -15)
    {
    }

    /// <summary>Create a raw DEFLATE compressor at the given level.</summary>
    public DeflateStream(Stream stream, CompressionLevel level, bool leaveOpen = false)
        : this(stream, level, leaveOpen, -15)
    {
    }

    private protected DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen, int windowBits)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _base = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;
        _buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        _zs = new ZStream();

        ZlibResult rc = mode == CompressionMode.Decompress
            ? _zs.InflateInit2(windowBits)
            : _zs.DeflateInit2(-1, CompressionMethod.Deflated, windowBits, 8, CompressionStrategy.Default);
        if (rc != ZlibResult.Ok)
            throw new ZlibException(rc, "failed to initialize the compression engine");

        if (mode == CompressionMode.Decompress && !stream.CanRead)
            throw new ArgumentException("stream must be readable for decompression", nameof(stream));
        if (mode == CompressionMode.Compress && !stream.CanWrite)
            throw new ArgumentException("stream must be writable for compression", nameof(stream));
    }

    private protected DeflateStream(Stream stream, CompressionLevel level, bool leaveOpen, int windowBits)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _base = stream;
        _mode = CompressionMode.Compress;
        _leaveOpen = leaveOpen;
        _buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        _zs = new ZStream();

        ZlibResult rc = _zs.DeflateInit2(MapLevel(level), CompressionMethod.Deflated, windowBits, 8,
            CompressionStrategy.Default);
        if (rc != ZlibResult.Ok)
            throw new ZlibException(rc, "failed to initialize the compression engine");
        if (!stream.CanWrite)
            throw new ArgumentException("stream must be writable for compression", nameof(stream));
    }

    private static int MapLevel(CompressionLevel level) => level switch
    {
        CompressionLevel.Optimal => -1,
        CompressionLevel.Fastest => 1,
        CompressionLevel.NoCompression => 0,
        CompressionLevel.SmallestSize => 9,
        _ => -1,
    };

    /// <summary>The underlying stream.</summary>
    public Stream BaseStream => _base;

    public override bool CanRead => _mode == CompressionMode.Decompress && !_disposed && _base.CanRead;
    public override bool CanWrite => _mode == CompressionMode.Compress && !_disposed && _base.CanWrite;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArgs(buffer, offset, count);
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        EnsureNotDisposed();
        if (_mode != CompressionMode.Decompress)
            throw new InvalidOperationException("stream is not in decompression mode");

        int startLen = buffer.Length;
        while (!buffer.IsEmpty && !_eof)
        {
            if (_inStart == _inEnd)
            {
                _inEnd = _base.Read(_buffer!, 0, _buffer!.Length);
                _inStart = 0;
            }

            ReadOnlySpan<byte> input = _buffer.AsSpan(_inStart, _inEnd - _inStart);
            ZlibResult rc = _zs!.Inflate(input, buffer, FlushMode.NoFlush, out int consumed, out int written);
            _inStart += consumed;
            buffer = buffer[written..];

            if (rc == ZlibResult.StreamEnd)
            {
                _eof = true;
                break;
            }
            if (rc is ZlibResult.DataError or ZlibResult.NeedDict or ZlibResult.StreamError)
                throw new ZlibException(rc, _zs.Message ?? "decompression failed");
            if (written == 0 && consumed == 0 && _inEnd == 0)
                break; // reached end of base stream without a complete stream
        }
        return startLen - buffer.Length;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArgs(buffer, offset, count);
        Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        EnsureNotDisposed();
        if (_mode != CompressionMode.Compress)
            throw new InvalidOperationException("stream is not in compression mode");

        while (true)
        {
            ZlibResult rc = _zs!.Deflate(buffer, _buffer, FlushMode.NoFlush, out int consumed, out int written);
            _ = rc;
            if (written > 0)
                _base.Write(_buffer!, 0, written);
            buffer = buffer[consumed..];
            if (buffer.IsEmpty && written < _buffer!.Length)
                break;
        }
    }

    public override void Flush()
    {
        EnsureNotDisposed();
        if (_mode == CompressionMode.Compress && !_finished)
            DriveOutput(FlushMode.SyncFlush);
        _base.Flush();
    }

    private void DriveOutput(FlushMode flush)
    {
        while (true)
        {
            ZlibResult rc = _zs!.Deflate(ReadOnlySpan<byte>.Empty, _buffer, flush, out _, out int written);
            if (written > 0)
                _base.Write(_buffer!, 0, written);
            if (flush == FlushMode.Finish)
            {
                if (rc == ZlibResult.StreamEnd)
                    break;
            }
            else if (written < _buffer!.Length)
            {
                break;
            }
        }
    }

    // --- async ---

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        if (_mode != CompressionMode.Decompress)
            throw new InvalidOperationException("stream is not in decompression mode");

        int startLen = buffer.Length;
        while (!buffer.IsEmpty && !_eof)
        {
            if (_inStart == _inEnd)
            {
                _inEnd = await _base.ReadAsync(_buffer.AsMemory(0, _buffer!.Length), cancellationToken).ConfigureAwait(false);
                _inStart = 0;
            }

            ZlibResult rc = _zs!.Inflate(_buffer.AsSpan(_inStart, _inEnd - _inStart), buffer.Span,
                FlushMode.NoFlush, out int consumed, out int written);
            _inStart += consumed;
            buffer = buffer[written..];

            if (rc == ZlibResult.StreamEnd)
            {
                _eof = true;
                break;
            }
            if (rc is ZlibResult.DataError or ZlibResult.NeedDict or ZlibResult.StreamError)
                throw new ZlibException(rc, _zs.Message ?? "decompression failed");
            if (written == 0 && consumed == 0 && _inEnd == 0)
                break;
        }
        return startLen - buffer.Length;
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        if (_mode != CompressionMode.Compress)
            throw new InvalidOperationException("stream is not in compression mode");

        while (true)
        {
            ZlibResult rc = _zs!.Deflate(buffer.Span, _buffer, FlushMode.NoFlush, out int consumed, out int written);
            _ = rc;
            if (written > 0)
                await _base.WriteAsync(_buffer.AsMemory(0, written), cancellationToken).ConfigureAwait(false);
            buffer = buffer[consumed..];
            if (buffer.IsEmpty && written < _buffer!.Length)
                break;
        }
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        EnsureNotDisposed();
        if (_mode == CompressionMode.Compress && !_finished)
            await DriveOutputAsync(FlushMode.SyncFlush, cancellationToken).ConfigureAwait(false);
        await _base.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask DriveOutputAsync(FlushMode flush, CancellationToken ct)
    {
        while (true)
        {
            ZlibResult rc = _zs!.Deflate(ReadOnlyMemory<byte>.Empty.Span, _buffer, flush, out _, out int written);
            if (written > 0)
                await _base.WriteAsync(_buffer.AsMemory(0, written), ct).ConfigureAwait(false);
            if (flush == FlushMode.Finish)
            {
                if (rc == ZlibResult.StreamEnd)
                    break;
            }
            else if (written < _buffer!.Length)
            {
                break;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            try
            {
                if (disposing && _mode == CompressionMode.Compress && !_finished && _zs != null)
                {
                    DriveOutput(FlushMode.Finish);
                    _finished = true;
                }
            }
            finally
            {
                _disposed = true;
                _zs?.Dispose();
                _zs = null;
                if (_buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = null;
                }
                if (disposing && !_leaveOpen)
                    _base.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                if (_mode == CompressionMode.Compress && !_finished && _zs != null)
                {
                    await DriveOutputAsync(FlushMode.Finish, default).ConfigureAwait(false);
                    _finished = true;
                }
            }
            finally
            {
                _disposed = true;
                _zs?.Dispose();
                _zs = null;
                if (_buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = null;
                }
                if (!_leaveOpen)
                    await _base.DisposeAsync().ConfigureAwait(false);
            }
        }
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static void ValidateBufferArgs(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (buffer.Length - offset < count)
            throw new ArgumentException("invalid offset/count for buffer");
    }
}
