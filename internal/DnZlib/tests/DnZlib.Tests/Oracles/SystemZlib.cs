using System.IO.Compression;

namespace DnZlib.Tests.Oracles;

/// <summary>Oracle backed by the BCL's <c>System.IO.Compression</c> (native zlib-ng).</summary>
internal static class SystemZlib
{
    public static byte[] DeflateZlib(byte[] data, CompressionLevel level) =>
        Compress(data, s => new ZLibStream(s, level, leaveOpen: true));

    public static byte[] DeflateGzip(byte[] data, CompressionLevel level) =>
        Compress(data, s => new GZipStream(s, level, leaveOpen: true));

    public static byte[] DeflateRaw(byte[] data, CompressionLevel level) =>
        Compress(data, s => new DeflateStream(s, level, leaveOpen: true));

    public static byte[] InflateZlib(byte[] data) =>
        Decompress(data, s => new ZLibStream(s, CompressionMode.Decompress));

    public static byte[] InflateGzip(byte[] data) =>
        Decompress(data, s => new GZipStream(s, CompressionMode.Decompress));

    public static byte[] InflateRaw(byte[] data) =>
        Decompress(data, s => new DeflateStream(s, CompressionMode.Decompress));

    private static byte[] Compress(byte[] data, Func<Stream, Stream> factory)
    {
        using var ms = new MemoryStream();
        using (Stream comp = factory(ms))
            comp.Write(data, 0, data.Length);
        return ms.ToArray();
    }

    private static byte[] Decompress(byte[] data, Func<Stream, Stream> factory)
    {
        using var src = new MemoryStream(data);
        using Stream decomp = factory(src);
        using var outMs = new MemoryStream();
        decomp.CopyTo(outMs);
        return outMs.ToArray();
    }
}
