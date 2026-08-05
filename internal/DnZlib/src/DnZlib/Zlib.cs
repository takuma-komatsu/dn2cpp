using System.Buffers;

namespace DnZlib;

/// <summary>High-level one-shot compression helpers over the low-level <see cref="ZStream"/> engine.</summary>
public static partial class Zlib
{
    /// <summary>Map a container format to the signed windowBits value the engine expects.</summary>
    internal static int WindowBitsFor(ZlibFormat format) => format switch
    {
        ZlibFormat.Zlib => 15,
        ZlibFormat.Raw => -15,
        ZlibFormat.Gzip => 31,
        ZlibFormat.AutoDetect => 47,
        _ => 15,
    };

    /// <summary>Container format to signed windowBits for compression (AutoDetect maps to zlib).</summary>
    internal static int WindowBitsForCompress(ZlibFormat format) => format switch
    {
        ZlibFormat.Raw => -15,
        ZlibFormat.Gzip => 31,
        _ => 15,
    };

    private static int WrapOverhead(ZlibFormat format) => format switch
    {
        ZlibFormat.Gzip => 18,
        ZlibFormat.Raw => 0,
        _ => 6,
    };

    /// <summary>Compress <paramref name="source"/>, returning a right-sized buffer.</summary>
    public static byte[] Compress(ReadOnlySpan<byte> source, int level = -1,
        ZlibFormat format = ZlibFormat.Zlib, CompressionStrategy strategy = CompressionStrategy.Default)
    {
        using var zs = new ZStream();
        ZlibResult rc = zs.DeflateInit2(level, CompressionMethod.Deflated,
            WindowBitsForCompress(format), 8, strategy);
        if (rc != ZlibResult.Ok)
            throw new ZlibException(rc, "DeflateInit2 failed");

        // deflateBound() guarantees enough output space for a single deflate(Z_FINISH) call from a
        // fresh stream, so this rents once and never needs to grow/retry (mirrors TryCompress).
        int cap = Math.Max(64, CompressBound(source.Length, format));
        byte[] pooled = ArrayPool<byte>.Shared.Rent(cap);
        try
        {
            rc = zs.Deflate(source, pooled.AsSpan(0, cap), FlushMode.Finish, out _, out int written);
            zs.DeflateEnd();
            if (rc != ZlibResult.StreamEnd)
                throw new ZlibException(rc, zs.Message ?? "deflate failed");
            return pooled.AsSpan(0, written).ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pooled);
        }
    }

    /// <summary>Try to compress into a caller buffer in a single pass; false if it doesn't fit.</summary>
    public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten,
        int level = -1, ZlibFormat format = ZlibFormat.Zlib, CompressionStrategy strategy = CompressionStrategy.Default)
    {
        bytesWritten = 0;
        using var zs = new ZStream();
        if (zs.DeflateInit2(level, CompressionMethod.Deflated, WindowBitsForCompress(format), 8, strategy) != ZlibResult.Ok)
            return false;
        ZlibResult rc = zs.Deflate(source, destination, FlushMode.Finish, out _, out int written);
        zs.DeflateEnd();
        if (rc == ZlibResult.StreamEnd)
        {
            bytesWritten = written;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Worst-case compressed size for <paramref name="sourceLen"/> input bytes. Conservative
    /// (zlib-ng-style, ~14% max expansion) rather than the tight classic-zlib bound, because
    /// <c>deflate_quick</c>/<c>deflate_medium</c> emit static-Huffman-only blocks with no stored-block
    /// fallback for incompressible input.
    /// </summary>
    public static int CompressBound(int sourceLen, ZlibFormat format = ZlibFormat.Zlib) =>
        sourceLen + ((sourceLen + 7) >> 3) + ((sourceLen + 63) >> 6) + 5 + WrapOverhead(format);

    /// <summary>Decompress a full stream, returning a right-sized buffer.</summary>
    public static byte[] Uncompress(ReadOnlySpan<byte> source, ZlibFormat format = ZlibFormat.Zlib, int sizeHint = -1)
    {
        using var zs = new ZStream();
        ZlibResult rc = zs.InflateInit2(WindowBitsFor(format));
        if (rc != ZlibResult.Ok)
            throw new ZlibException(rc, "InflateInit2 failed");

        int cap = sizeHint > 0 ? sizeHint : Math.Max(64, source.Length * 4);
        byte[] pooled = ArrayPool<byte>.Shared.Rent(cap);
        int total = 0, consumedTotal = 0;

        try
        {
            while (true)
            {
                if (total == pooled.Length)
                {
                    byte[] grown = ArrayPool<byte>.Shared.Rent(pooled.Length * 2);
                    pooled.AsSpan(0, total).CopyTo(grown);
                    ArrayPool<byte>.Shared.Return(pooled);
                    pooled = grown;
                }

                rc = zs.Inflate(source[consumedTotal..], pooled.AsSpan(total), FlushMode.NoFlush,
                                 out int consumed, out int written);
                consumedTotal += consumed;
                total += written;

                if (rc == ZlibResult.StreamEnd)
                    break;
                if (rc == ZlibResult.Ok)
                    continue;
                if (rc == ZlibResult.BufError && total == pooled.Length)
                    continue; // output was full; grow and retry
                zs.InflateEnd();
                throw new ZlibException(rc, zs.Message ?? "inflate failed");
            }

            zs.InflateEnd();
            return pooled.AsSpan(0, total).ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pooled);
        }
    }

    /// <summary>
    /// Try to decompress into a caller-provided buffer in a single pass. Returns false (without
    /// throwing) if <paramref name="destination"/> is too small or the stream is invalid.
    /// </summary>
    public static bool TryUncompress(ReadOnlySpan<byte> source, Span<byte> destination,
                                     out int bytesWritten, ZlibFormat format = ZlibFormat.Zlib)
    {
        bytesWritten = 0;
        using var zs = new ZStream();
        if (zs.InflateInit2(WindowBitsFor(format)) != ZlibResult.Ok)
            return false;

        ZlibResult rc = zs.Inflate(source, destination, FlushMode.Finish, out _, out int written);
        zs.InflateEnd();
        if (rc == ZlibResult.StreamEnd)
        {
            bytesWritten = written;
            return true;
        }
        return false;
    }
}
