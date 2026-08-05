namespace DnZlib.Streams;

/// <summary>A <see cref="Stream"/> for the gzip container format (RFC 1952).</summary>
public sealed class GZipStream : DeflateStream
{
    /// <summary>Create a gzip stream in the given mode.</summary>
    public GZipStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
        : base(stream, mode, leaveOpen, 31)
    {
    }

    /// <summary>Create a gzip compressor at the given level.</summary>
    public GZipStream(Stream stream, CompressionLevel level, bool leaveOpen = false)
        : base(stream, level, leaveOpen, 31)
    {
    }
}
