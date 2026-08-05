namespace DnZlib.Streams;

/// <summary>A <see cref="Stream"/> for the zlib container format (RFC 1950).</summary>
public sealed class ZLibStream : DeflateStream
{
    /// <summary>Create a zlib stream in the given mode.</summary>
    public ZLibStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
        : base(stream, mode, leaveOpen, 15)
    {
    }

    /// <summary>Create a zlib compressor at the given level.</summary>
    public ZLibStream(Stream stream, CompressionLevel level, bool leaveOpen = false)
        : base(stream, level, leaveOpen, 15)
    {
    }
}
