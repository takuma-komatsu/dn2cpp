using System.IO.Compression;
using BenchmarkDotNet.Attributes;
using DnZlib;

namespace DnZlib.Benchmarks;

/// <summary>Decompression throughput: DnZlib vs System.IO.Compression (native zlib-ng).</summary>
[MemoryDiagnoser]
public class DecompressBenchmarks
{
    [Params("text", "binary")]
    public string Input = "";

    private byte[] _compressed = [];
    private int _originalLength;

    [GlobalSetup]
    public void Setup()
    {
        byte[] data = BenchData.Get(Input);
        _originalLength = data.Length;
        _compressed = Zlib.Compress(data, 6, ZlibFormat.Zlib);
    }

    [Benchmark]
    public byte[] DnZlib() => Zlib.Uncompress(_compressed, ZlibFormat.Zlib, _originalLength);

    [Benchmark(Baseline = true)]
    public byte[] SystemIOCompression()
    {
        using var src = new MemoryStream(_compressed);
        using var z = new ZLibStream(src, CompressionMode.Decompress);
        var outBuf = new byte[_originalLength];
        int total = 0;
        int n;
        while (total < outBuf.Length && (n = z.Read(outBuf, total, outBuf.Length - total)) > 0)
            total += n;
        return outBuf;
    }
}
