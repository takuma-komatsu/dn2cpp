namespace DnZlib;

/// <summary>
/// gzip header fields, used with <c>InflateGetHeader</c> to capture a decoded header and with
/// <c>DeflateSetHeader</c> to supply one when compressing (mirrors zlib's <c>gz_header</c>).
/// </summary>
public sealed class GzipHeader
{
    /// <summary>True if the compressed data is believed to be text.</summary>
    public bool Text { get; set; }

    /// <summary>Modification time (Unix seconds); 0 if unavailable.</summary>
    public uint ModifiedTime { get; set; }

    /// <summary>Extra flags from the compression method (inflate-filled).</summary>
    public int ExtraFlags { get; set; }

    /// <summary>Operating system identifier; 255 = unknown.</summary>
    public int OperatingSystem { get; set; } = 255;

    /// <summary>Optional extra field bytes.</summary>
    public byte[]? Extra { get; set; }

    /// <summary>Original file name (Latin-1), null-terminated in the stream.</summary>
    public byte[]? Name { get; set; }

    /// <summary>File comment (Latin-1), null-terminated in the stream.</summary>
    public byte[]? Comment { get; set; }

    /// <summary>True if a header CRC is present.</summary>
    public bool HeaderCrc { get; set; }

    /// <summary>Set true once inflate has finished reading the header.</summary>
    public bool Done { get; internal set; }
}
