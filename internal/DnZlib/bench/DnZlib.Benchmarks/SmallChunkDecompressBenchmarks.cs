using BenchmarkDotNet.Attributes;
using DnZlib;

namespace DnZlib.Benchmarks;

/// <summary>
/// Decompression throughput when the caller drains output in small (sub-258-byte) chunks — the
/// path that used to fall back to a byte-at-a-time copy instead of Fast()'s vectorized chunk copy.
/// </summary>
[MemoryDiagnoser]
public class SmallChunkDecompressBenchmarks
{
    [Params("text", "binary")]
    public string Input = "";

    private byte[] _compressed = [];

    [GlobalSetup]
    public void Setup()
    {
        byte[] data = BenchData.Get(Input);
        _compressed = Zlib.Compress(data, 6, ZlibFormat.Zlib);
    }

    [Benchmark]
    public int DnZlib_64ByteChunks()
    {
        using var zs = new ZStream();
        zs.InflateInit2(15);
        var outBuf = new byte[64];
        int total = 0, consumedTotal = 0;
        ReadOnlySpan<byte> input = _compressed;

        while (true)
        {
            ZlibResult rc = zs.Inflate(input[consumedTotal..], outBuf, FlushMode.NoFlush,
                                        out int consumed, out int written);
            consumedTotal += consumed;
            total += written;
            if (rc == ZlibResult.StreamEnd)
                break;
        }

        zs.InflateEnd();
        return total;
    }
}
