using System.Buffers;

namespace DnBrotli.Streams;

/// <summary>
/// A <see cref="Stream"/> that compresses to / decompresses from the brotli format, mirroring
/// the BCL's <c>System.IO.Compression.BrotliStream</c> surface (same ctors, same
/// <c>CompressionLevel</c>-to-quality mapping, same mode/dispose semantics) with the DnZlib
/// <c>DeflateStream</c> implementation pattern: an ArrayPool-rented internal buffer, sync +
/// async paths that share one core loop, and a dispose that finalizes the brotli stream.
/// The compression side drives <see cref="BrotliEncoder"/>; the decompression side wraps
/// <see cref="BrotliDecoder"/>. The extra <see cref="BrotliCompressionOptions"/> ctor exposes
/// the window/mode knobs the BCL hides.
/// </summary>
public sealed class BrotliStream : Stream
{
    private const int DefaultInternalBufferSize = (1 << 16) - 16; // 65520, matching the BCL

    private readonly Stream _base;
    private readonly bool _leaveOpen;
    private readonly CompressionMode _mode;
    private BrotliEncoder _encoder;
    private BrotliDecoder _decoder;
    private byte[]? _buffer;

    // Decompression input buffering: unconsumed bytes are _buffer[_inStart.._inEnd).
    private int _inStart;
    private int _inEnd;
    private bool _eof;

    private bool _disposed;

    /// <summary>Create a brotli stream in the given mode. Compression uses the BCL defaults
    /// (quality 4, window 22).</summary>
    public BrotliStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        switch (mode)
        {
            case CompressionMode.Compress:
                if (!stream.CanWrite)
                {
                    throw new ArgumentException("stream must be writable for compression", nameof(stream));
                }
                _encoder.SetQuality(BrotliEncoder.QualityDefault);
                _encoder.SetWindow(BrotliEncoder.WindowBitsDefault);
                break;

            case CompressionMode.Decompress:
                if (!stream.CanRead)
                {
                    throw new ArgumentException("stream must be readable for decompression", nameof(stream));
                }
                break;

            default:
                throw new ArgumentException("invalid compression mode", nameof(mode));
        }

        _mode = mode;
        _base = stream;
        _leaveOpen = leaveOpen;
        _buffer = ArrayPool<byte>.Shared.Rent(DefaultInternalBufferSize);
    }

    /// <summary>Create a brotli compressor at the given level. The mapping matches the BCL:
    /// NoCompression → quality 0, Fastest → 1, Optimal → 4, SmallestSize → 11 (window 22).</summary>
    public BrotliStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen = false)
        : this(stream, CompressionMode.Compress, leaveOpen)
    {
        _encoder.SetQuality(GetQualityFromCompressionLevel(compressionLevel));
    }

    /// <summary>Create a brotli compressor with explicit quality/window/mode options.</summary>
    public BrotliStream(Stream stream, BrotliCompressionOptions compressionOptions, bool leaveOpen = false)
        : this(stream, CompressionMode.Compress, leaveOpen)
    {
        ArgumentNullException.ThrowIfNull(compressionOptions);
        _encoder.SetQuality(compressionOptions.Quality);
        _encoder.SetWindow(compressionOptions.Window);
        _encoder.SetMode(compressionOptions.Mode);
    }

    private static int GetQualityFromCompressionLevel(CompressionLevel compressionLevel) =>
        compressionLevel switch
        {
            CompressionLevel.NoCompression => BrotliEncoder.QualityMin,
            CompressionLevel.Fastest => 1,
            CompressionLevel.Optimal => BrotliEncoder.QualityDefault,
            CompressionLevel.SmallestSize => BrotliEncoder.QualityMax,
            _ => throw new ArgumentException("invalid compression level", nameof(compressionLevel)),
        };

    /// <summary>The underlying stream.</summary>
    public Stream BaseStream => _base;

    public override bool CanRead => _mode == CompressionMode.Decompress && !_disposed && _base.CanRead;
    public override bool CanWrite => _mode == CompressionMode.Compress && !_disposed && _base.CanWrite;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    // ==================== decompress ====================

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArgs(buffer, offset, count);
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        // Mode before disposed, like the BCL.
        if (_mode != CompressionMode.Decompress)
        {
            throw new InvalidOperationException("stream is not in decompression mode");
        }
        EnsureNotDisposed();

        int startLen = buffer.Length;
        while (!buffer.IsEmpty && !_eof)
        {
            // Decode first: the decoder may hold pending output that needs no further input
            // (its window can far exceed one internal buffer of compressed data).
            OperationStatus status = _decoder.Decompress(
                _buffer.AsSpan(_inStart, _inEnd - _inStart), buffer, out int consumed, out int written);
            _inStart += consumed;
            buffer = buffer[written..];

            if (status == OperationStatus.Done)
            {
                _eof = true;
                break;
            }
            if (status == OperationStatus.InvalidData)
            {
                // The BCL (historically) throws InvalidOperationException here, not
                // InvalidDataException; kept for drop-in parity.
                throw new InvalidOperationException("brotli stream is invalid");
            }
            if (status == OperationStatus.NeedMoreData)
            {
                // All buffered input was consumed; refill from the base stream.
                _inStart = 0;
                _inEnd = _base.Read(_buffer!, 0, _buffer!.Length);
                if (_inEnd == 0)
                {
                    break; // base exhausted mid-stream: return what we have (BCL non-strict behavior)
                }
            }
        }
        return startLen - buffer.Length;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArgs(buffer, offset, count);
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_mode != CompressionMode.Decompress)
        {
            throw new InvalidOperationException("stream is not in decompression mode");
        }
        EnsureNotDisposed();

        int startLen = buffer.Length;
        while (!buffer.IsEmpty && !_eof)
        {
            OperationStatus status = _decoder.Decompress(
                _buffer.AsSpan(_inStart, _inEnd - _inStart), buffer.Span, out int consumed, out int written);
            _inStart += consumed;
            buffer = buffer[written..];

            if (status == OperationStatus.Done)
            {
                _eof = true;
                break;
            }
            if (status == OperationStatus.InvalidData)
            {
                throw new InvalidOperationException("brotli stream is invalid");
            }
            if (status == OperationStatus.NeedMoreData)
            {
                _inStart = 0;
                _inEnd = await _base.ReadAsync(_buffer.AsMemory(0, _buffer!.Length), cancellationToken)
                    .ConfigureAwait(false);
                if (_inEnd == 0)
                {
                    break;
                }
            }
        }
        return startLen - buffer.Length;
    }

    // ==================== compress ====================

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArgs(buffer, offset, count);
        Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer) => WriteCore(buffer, isFinalBlock: false);

    private void WriteCore(ReadOnlySpan<byte> buffer, bool isFinalBlock)
    {
        if (_mode != CompressionMode.Compress)
        {
            throw new InvalidOperationException("stream is not in compression mode");
        }
        EnsureNotDisposed();

        OperationStatus lastResult = OperationStatus.DestinationTooSmall;
        while (lastResult == OperationStatus.DestinationTooSmall)
        {
            lastResult = _encoder.Compress(buffer, _buffer, out int consumed, out int written, isFinalBlock);
            if (lastResult == OperationStatus.InvalidData)
            {
                throw new InvalidOperationException("the encoder ran into invalid data");
            }
            if (written > 0)
            {
                _base.Write(_buffer!, 0, written);
            }
            buffer = buffer[consumed..];
        }
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArgs(buffer, offset, count);
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_mode != CompressionMode.Compress)
        {
            throw new InvalidOperationException("stream is not in compression mode");
        }
        EnsureNotDisposed();
        return cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled(cancellationToken)
            : WriteAsyncCore(buffer, cancellationToken, isFinalBlock: false);
    }

    private async ValueTask WriteAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken ct, bool isFinalBlock)
    {
        OperationStatus lastResult = OperationStatus.DestinationTooSmall;
        while (lastResult == OperationStatus.DestinationTooSmall)
        {
            lastResult = _encoder.Compress(buffer.Span, _buffer, out int consumed, out int written, isFinalBlock);
            if (lastResult == OperationStatus.InvalidData)
            {
                throw new InvalidOperationException("the encoder ran into invalid data");
            }
            if (written > 0)
            {
                await _base.WriteAsync(_buffer.AsMemory(0, written), ct).ConfigureAwait(false);
            }
            buffer = buffer[consumed..];
        }
    }

    /// <summary>In compression mode, emits an encoder flush (the bytes written so far become a
    /// decodable prefix) and flushes the underlying stream; in decompression mode it does
    /// nothing, like the BCL.</summary>
    public override void Flush()
    {
        EnsureNotDisposed();
        if (_mode == CompressionMode.Compress)
        {
            OperationStatus lastResult = OperationStatus.DestinationTooSmall;
            while (lastResult == OperationStatus.DestinationTooSmall)
            {
                lastResult = _encoder.Flush(_buffer, out int written);
                if (lastResult == OperationStatus.InvalidData)
                {
                    throw new InvalidDataException("the encoder ran into invalid data");
                }
                if (written > 0)
                {
                    _base.Write(_buffer!, 0, written);
                }
            }
            _base.Flush();
        }
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        EnsureNotDisposed();
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }
        return _mode != CompressionMode.Compress ? Task.CompletedTask : FlushAsyncCore(cancellationToken);
    }

    private async Task FlushAsyncCore(CancellationToken ct)
    {
        OperationStatus lastResult = OperationStatus.DestinationTooSmall;
        while (lastResult == OperationStatus.DestinationTooSmall)
        {
            lastResult = _encoder.Flush(_buffer, out int written);
            if (lastResult == OperationStatus.InvalidData)
            {
                throw new InvalidDataException("the encoder ran into invalid data");
            }
            if (written > 0)
            {
                await _base.WriteAsync(_buffer.AsMemory(0, written), ct).ConfigureAwait(false);
            }
        }
        await _base.FlushAsync(ct).ConfigureAwait(false);
    }

    // ==================== dispose ====================

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            try
            {
                if (disposing && _mode == CompressionMode.Compress)
                {
                    WriteCore(ReadOnlySpan<byte>.Empty, isFinalBlock: true);
                }
            }
            finally
            {
                _disposed = true;
                _encoder.Dispose();
                _decoder.Dispose();
                if (_buffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = null;
                }
                if (disposing && !_leaveOpen)
                {
                    _base.Dispose();
                }
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
                if (_mode == CompressionMode.Compress)
                {
                    await WriteAsyncCore(ReadOnlyMemory<byte>.Empty, CancellationToken.None, isFinalBlock: true)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                _disposed = true;
                _encoder.Dispose();
                _decoder.Dispose();
                if (_buffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = null;
                }
                if (!_leaveOpen)
                {
                    await _base.DisposeAsync().ConfigureAwait(false);
                }
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
        {
            throw new ArgumentException("invalid offset/count for buffer");
        }
    }
}
