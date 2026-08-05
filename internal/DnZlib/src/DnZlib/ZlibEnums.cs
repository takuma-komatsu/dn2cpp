namespace DnZlib;

/// <summary>Return code from the low-level zlib-compatible API (mirrors zlib's <c>Z_*</c> codes).</summary>
public enum ZlibResult
{
    Ok = 0,
    StreamEnd = 1,
    NeedDict = 2,
    ErrNo = -1,
    StreamError = -2,
    DataError = -3,
    MemError = -4,
    BufError = -5,
    VersionError = -6,
}

/// <summary>Flush request passed to <c>Deflate</c>/<c>Inflate</c> (mirrors zlib's <c>Z_*_FLUSH</c>).</summary>
public enum FlushMode
{
    NoFlush = 0,
    PartialFlush = 1,
    SyncFlush = 2,
    FullFlush = 3,
    Finish = 4,
    Block = 5,
    Trees = 6,
}

/// <summary>Compression strategy tuning (mirrors zlib's <c>Z_*</c> strategies).</summary>
public enum CompressionStrategy
{
    Default = 0,
    Filtered = 1,
    HuffmanOnly = 2,
    Rle = 3,
    Fixed = 4,
}

/// <summary>Compression method. Only DEFLATE is defined by the format.</summary>
public enum CompressionMethod
{
    Deflated = 8,
}

/// <summary>Best-guess classification of the uncompressed data.</summary>
public enum ZlibDataType
{
    Binary = 0,
    Text = 1,
    Unknown = 2,
}

/// <summary>High-level container format selector (sugar over signed <c>windowBits</c>).</summary>
public enum ZlibFormat
{
    /// <summary>zlib format (RFC 1950), windowBits 15.</summary>
    Zlib,

    /// <summary>Raw DEFLATE (RFC 1951), windowBits -15.</summary>
    Raw,

    /// <summary>gzip format (RFC 1952), windowBits 31.</summary>
    Gzip,

    /// <summary>Auto-detect zlib vs gzip on decompression, windowBits 47.</summary>
    AutoDetect,
}
