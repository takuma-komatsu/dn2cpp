using DnZlib;

namespace DnZlib.Tests.Support;

public static class InflateHelper
{
    /// <summary>
    /// Fully decompress <paramref name="compressed"/> using our engine, feeding input whole and
    /// draining output in <paramref name="outChunk"/>-sized pieces (small chunks exercise the
    /// sliding window and mid-stream state save/restore).
    /// </summary>
    public static byte[] InflateAll(byte[] compressed, int windowBits, int outChunk = 1 << 16)
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
            int produced = (int)(zs.TotalOut - before);
            ms.Write(outBuf, 0, produced);

            if (rc == ZlibResult.StreamEnd)
                break;
            if (rc == ZlibResult.Ok)
                continue;
            if (rc == ZlibResult.BufError)
            {
                // No forward progress possible: truncated/insufficient input.
                if (produced == 0)
                    throw new InvalidOperationException("unexpected Z_BUF_ERROR (truncated stream?)");
                continue;
            }
            throw new InvalidOperationException($"inflate error {rc}: {zs.Message}");
        }

        zs.InflateEnd();
        return ms.ToArray();
    }

    /// <summary>
    /// Like <see cref="InflateAll"/>, but decompresses into a slice of a larger sentinel-filled
    /// buffer on every call and asserts the bytes immediately past the slice are never touched.
    /// Targets writes that overrun the caller's destination span — something a correctness-only
    /// assertion on the decompressed bytes would not necessarily catch.
    /// </summary>
    public static byte[] InflateAllWithSentinelGuard(byte[] compressed, int windowBits, int outChunk)
    {
        using var zs = new ZStream();
        if (zs.InflateInit2(windowBits) != ZlibResult.Ok)
            throw new InvalidOperationException("InflateInit2 failed");

        zs.Input = compressed;
        using var ms = new MemoryStream();
        const byte Sentinel = 0xCC;
        const int GuardBytes = 64;

        while (true)
        {
            var backing = new byte[outChunk + GuardBytes];
            Array.Fill(backing, Sentinel);

            zs.Output = backing.AsMemory(0, outChunk);
            long before = zs.TotalOut;
            ZlibResult rc = zs.Inflate(FlushMode.NoFlush);
            int produced = (int)(zs.TotalOut - before);

            for (int i = outChunk; i < backing.Length; i++)
            {
                if (backing[i] != Sentinel)
                {
                    throw new InvalidOperationException(
                        $"sentinel byte at offset {i - outChunk} past the destination slice was overwritten");
                }
            }

            ms.Write(backing, 0, produced);

            if (rc == ZlibResult.StreamEnd)
                break;
            if (rc == ZlibResult.Ok)
                continue;
            if (rc == ZlibResult.BufError)
            {
                if (produced == 0)
                    throw new InvalidOperationException("unexpected Z_BUF_ERROR (truncated stream?)");
                continue;
            }
            throw new InvalidOperationException($"inflate error {rc}: {zs.Message}");
        }

        zs.InflateEnd();
        return ms.ToArray();
    }

    /// <summary>Attempt to decompress; return null on any error (for robustness/fuzz testing).</summary>
    public static byte[]? TryInflate(byte[] compressed, int windowBits, int maxOutput = 1 << 24)
    {
        using var zs = new ZStream();
        if (zs.InflateInit2(windowBits) != ZlibResult.Ok)
            return null;

        zs.Input = compressed;
        var outBuf = new byte[1 << 15];
        using var ms = new MemoryStream();
        int guard = 0;

        while (true)
        {
            if (++guard > 1_000_000)
                throw new InvalidOperationException("inflate did not terminate");
            zs.Output = outBuf;
            long before = zs.TotalOut;
            ZlibResult rc = zs.Inflate(FlushMode.NoFlush);
            int produced = (int)(zs.TotalOut - before);
            ms.Write(outBuf, 0, produced);
            if (ms.Length > maxOutput)
                return null;

            if (rc == ZlibResult.StreamEnd)
                break;
            if (rc == ZlibResult.Ok)
                continue;
            if (rc == ZlibResult.BufError)
            {
                if (produced == 0)
                    return null; // no progress
                continue;
            }
            return null; // DataError etc.
        }

        zs.InflateEnd();
        return ms.ToArray();
    }
}
