using System.IO.Compression;
using BenchmarkDotNet.Attributes;
using DnZlib;

namespace DnZlib.Benchmarks;

/// <summary>Compression throughput: DnZlib vs System.IO.Compression (native zlib-ng).</summary>
[MemoryDiagnoser]
public class CompressBenchmarks
{
    [Params(1, 6, 7, 8, 9)]
    public int Level;

    [Params("text", "binary", "source")]
    public string Input = "";

    private byte[] _data = [];

    [GlobalSetup]
    public void Setup() => _data = BenchData.Get(Input);

    [Benchmark]
    public byte[] DnZlib() => Zlib.Compress(_data, Level, ZlibFormat.Zlib);

    [Benchmark(Baseline = true)]
    public byte[] SystemIOCompression()
    {
        // Drive native zlib-ng at the SAME numeric level as DnZlib, via net10's ZLibCompressionOptions.
        // The classic CompressionLevel enum only has Fastest/Optimal/SmallestSize, and Optimal maps to
        // zlib level 6 — so mapping levels 7 and 8 to Optimal would compare DnZlib's real level 7/8
        // against native level 6, inflating those rows (the historical "L7/text 2.29x" artifact).
        using var ms = new MemoryStream();
        using (var z = new ZLibStream(ms, new ZLibCompressionOptions { CompressionLevel = Level }, leaveOpen: true))
            z.Write(_data, 0, _data.Length);
        return ms.ToArray();
    }
}
