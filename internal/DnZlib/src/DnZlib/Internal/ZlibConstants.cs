namespace DnZlib.Internal;

/// <summary>Header wrapping mode for a stream.</summary>
internal enum WrapMode
{
    /// <summary>Raw DEFLATE (RFC 1951), no header/trailer.</summary>
    Raw = 0,

    /// <summary>zlib format (RFC 1950): 2-byte header + adler32 trailer.</summary>
    Zlib = 1,

    /// <summary>gzip format (RFC 1952): 10+ byte header + crc32/isize trailer.</summary>
    Gzip = 2,
}

/// <summary>Shared numeric constants mirroring zlib's <c>zconf.h</c>/<c>deflate.h</c>/<c>inftrees.h</c>.</summary>
internal static class ZlibConstants
{
    /// <summary>Compatibility version string reported by the library.</summary>
    public const string Version = "1.3.2";

    public const int MaxWBits = 15;
    public const int DefWBits = 15;
    public const int MinWBits = 8;

    /// <summary>The only supported compression method (DEFLATE).</summary>
    public const int ZDeflated = 8;

    /// <summary>Sentinel level meaning "library default".</summary>
    public const int DefaultCompression = -1;

    public const int MinMatch = 3;
    public const int MaxMatch = 258;

    // inftrees ENOUGH sizing: max table entries for length and distance codes.
    public const int EnoughLens = 852;
    public const int EnoughDists = 592;
    public const int Enough = EnoughLens + EnoughDists;

    // Adler-32 parameters.
    public const uint AdlerBase = 65521;
    public const int AdlerNMax = 5552;
}
