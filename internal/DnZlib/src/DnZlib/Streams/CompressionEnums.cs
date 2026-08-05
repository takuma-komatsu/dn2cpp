namespace DnZlib.Streams;

/// <summary>Whether a compression stream compresses or decompresses (mirrors the BCL enum values).</summary>
public enum CompressionMode
{
    Decompress = 0,
    Compress = 1,
}

/// <summary>Compression level preset (mirrors <c>System.IO.Compression.CompressionLevel</c>).</summary>
public enum CompressionLevel
{
    Optimal = 0,
    Fastest = 1,
    NoCompression = 2,
    SmallestSize = 3,
}

/// <summary>
/// Advanced compression knobs the BCL hides — raw window sizing, memory level, and preset
/// dictionary — for callers that need them.
/// </summary>
public sealed class ZlibCompressionOptions
{
    /// <summary>zlib compression level (-1 = default, 0..9).</summary>
    public int CompressionLevel { get; set; } = -1;

    /// <summary>Compression strategy.</summary>
    public CompressionStrategy CompressionStrategy { get; set; } = CompressionStrategy.Default;

    /// <summary>Memory level (1..9); 8 by default. Larger uses more memory for better/faster compression.</summary>
    public int MemLevel { get; set; } = 8;
}
