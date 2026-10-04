using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Dn2Cpp.Runtime;

// The Stream base supplies argument validation and sync-over-async. ConsoleHandle
// keeps a concurrent Dispose from recycling a handle during a native read or write.
internal sealed class Dn2CppConsoleStream : Stream
{
    private sealed class ConsoleHandle : SafeHandle
    {
        internal ConsoleHandle(IntPtr value) : base(new IntPtr(-1), ConsoleRuntime.StandardHandleOwned()) => SetHandle(value);

        public override bool IsInvalid => handle == new IntPtr(-1);

        protected override bool ReleaseHandle()
        {
            ConsoleRuntime.CloseStandardHandle(handle);
            return true;
        }
    }

    private readonly ConsoleHandle _handle;
    private readonly bool _readable;

    private Dn2CppConsoleStream(IntPtr handle, bool readable)
    {
        _handle = new ConsoleHandle(handle);
        _readable = readable;
    }

    internal static Stream Open(int stream, int bufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bufferSize);
        IntPtr handle = ConsoleRuntime.OpenStandardHandle(stream);
        if (handle == new IntPtr(-1))
            return Stream.Null;
        return new Dn2CppConsoleStream(handle, stream == 0);
    }

    public override bool CanRead => _readable && !_handle.IsClosed;
    public override bool CanWrite => !_readable && !_handle.IsClosed;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException("Stream does not support seeking.");
    public override long Position
    {
        get => throw new NotSupportedException("Stream does not support seeking.");
        set => throw new NotSupportedException("Stream does not support seeking.");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        if (!CanRead)
            throw new NotSupportedException("Stream does not support reading.");
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        if (!ConsoleRuntime.StandardHandleOwned())
            return ConsoleRuntime.ReadStandardHandle(_handle.IsClosed ? IntPtr.Zero : _handle.DangerousGetHandle(), buffer);
        bool added = false;
        try
        {
            _handle.DangerousAddRef(ref added);
            return ConsoleRuntime.ReadStandardHandle(_handle.DangerousGetHandle(), buffer);
        }
        finally
        {
            if (added)
                _handle.DangerousRelease();
        }
    }

    public override int ReadByte()
    {
        Span<byte> buffer = stackalloc byte[1];
        return Read(buffer) == 0 ? -1 : buffer[0];
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        if (!CanWrite)
            throw new NotSupportedException("Stream does not support writing.");
        Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (buffer.IsEmpty)
            return;
        if (!ConsoleRuntime.StandardHandleOwned())
        {
            ConsoleRuntime.WriteStandardHandle(_handle.IsClosed ? IntPtr.Zero : _handle.DangerousGetHandle(), buffer);
            return;
        }
        bool added = false;
        try
        {
            _handle.DangerousAddRef(ref added);
            ConsoleRuntime.WriteStandardHandle(_handle.DangerousGetHandle(), buffer);
        }
        finally
        {
            if (added)
                _handle.DangerousRelease();
        }
    }

    public override void WriteByte(byte value)
    {
        Span<byte> buffer = stackalloc byte[1];
        buffer[0] = value;
        Write(buffer);
    }

    public override void Flush()
    {
        if (_handle.IsClosed)
            throw new ObjectDisposedException(null, "Cannot access a closed file.");
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException("Stream does not support seeking.");

    public override void SetLength(long value) =>
        throw new NotSupportedException("Stream does not support seeking.");

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _handle.Dispose();
        base.Dispose(disposing);
    }
}
