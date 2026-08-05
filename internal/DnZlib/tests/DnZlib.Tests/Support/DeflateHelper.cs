using DnZlib;

namespace DnZlib.Tests.Support;

public static class DeflateHelper
{
    /// <summary>Compress fully, draining output in <paramref name="outChunk"/>-sized pieces.</summary>
    public static byte[] DeflateAll(byte[] data, int level, int windowBits, int outChunk = 1 << 16,
        CompressionStrategy strategy = CompressionStrategy.Default)
    {
        using var zs = new ZStream();
        if (zs.DeflateInit2(level, CompressionMethod.Deflated, windowBits, 8, strategy) != ZlibResult.Ok)
            throw new InvalidOperationException("DeflateInit2 failed");

        zs.Input = data;
        return DriveToEnd(zs, outChunk);
    }

    /// <summary>Compress with a preset dictionary.</summary>
    public static byte[] DeflateWithDictionary(byte[] data, byte[] dictionary, int level, int windowBits, int outChunk = 1 << 16)
    {
        using var zs = new ZStream();
        if (zs.DeflateInit2(level, CompressionMethod.Deflated, windowBits, 8, CompressionStrategy.Default) != ZlibResult.Ok)
            throw new InvalidOperationException("DeflateInit2 failed");
        if (zs.DeflateSetDictionary(dictionary) != ZlibResult.Ok)
            throw new InvalidOperationException("DeflateSetDictionary failed");

        zs.Input = data;
        return DriveToEnd(zs, outChunk);
    }

    private static byte[] DriveToEnd(ZStream zs, int outChunk)
    {
        var outBuf = new byte[outChunk];
        using var ms = new MemoryStream();
        while (true)
        {
            zs.Output = outBuf;
            long before = zs.TotalOut;
            ZlibResult rc = zs.Deflate(FlushMode.Finish);
            ms.Write(outBuf, 0, (int)(zs.TotalOut - before));
            if (rc == ZlibResult.StreamEnd)
                break;
            if (rc is ZlibResult.Ok or ZlibResult.BufError)
                continue;
            throw new InvalidOperationException($"deflate error {rc}: {zs.Message}");
        }
        zs.DeflateEnd();
        return ms.ToArray();
    }

    /// <summary>Decompress a zlib stream that used a preset dictionary (drives the NeedDict flow).</summary>
    public static byte[] InflateWithDictionary(byte[] compressed, byte[] dictionary, int windowBits, int outChunk = 1 << 16)
    {
        using var zs = new ZStream();
        if (zs.InflateInit2(windowBits) != ZlibResult.Ok)
            throw new InvalidOperationException("InflateInit2 failed");

        zs.Input = compressed;
        var outBuf = new byte[outChunk];
        using var ms = new MemoryStream();
        while (true)
        {
            zs.Output = outBuf;
            long before = zs.TotalOut;
            ZlibResult rc = zs.Inflate(FlushMode.NoFlush);
            ms.Write(outBuf, 0, (int)(zs.TotalOut - before));
            if (rc == ZlibResult.StreamEnd)
                break;
            if (rc == ZlibResult.NeedDict)
            {
                if (zs.InflateSetDictionary(dictionary) != ZlibResult.Ok)
                    throw new InvalidOperationException("InflateSetDictionary failed");
                continue;
            }
            if (rc == ZlibResult.Ok)
                continue;
            throw new InvalidOperationException($"inflate error {rc}: {zs.Message}");
        }
        zs.InflateEnd();
        return ms.ToArray();
    }
}
