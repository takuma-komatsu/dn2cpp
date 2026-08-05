using BenchmarkDotNet.Attributes;
using BclBrotliEncoder = System.IO.Compression.BrotliEncoder;

namespace DnBrotli.Benchmarks;

/// <summary>Compression throughput: DnBrotli vs System.IO.Compression (native brotli).</summary>
[MemoryDiagnoser]
public class CompressBenchmarks
{
    [Params(1, 4, 9, 11)]
    public int Quality;

    [Params("text", "binary", "source")]
    public string Input = "";

    private byte[] _data = [];

    [GlobalSetup]
    public void Setup() => _data = BenchData.Get(Input);

    [Benchmark]
    public byte[] DnBrotli() => Brotli.Compress(_data, Quality, 22);

    [Benchmark(Baseline = true)]
    public byte[] SystemIOCompression()
    {
        // Drive the BCL's native brotli at the SAME quality and window as DnBrotli via the
        // static BrotliEncoder.TryCompress(quality, window) overload. BrotliCompressionOptions
        // only exposes Quality (window fixed at the default 22 — which DnBrotli matches), and
        // the CompressionLevel enum maps to qualities {1, 4 (Optimal), 11} only, so the static
        // TryCompress is the one BCL surface that pins both knobs explicitly.
        var dest = new byte[BclBrotliEncoder.GetMaxCompressedLength(_data.Length)];
        if (!BclBrotliEncoder.TryCompress(_data, dest, out int written, Quality, 22))
            throw new InvalidOperationException("BCL BrotliEncoder.TryCompress failed");
        return dest.AsSpan(0, written).ToArray();
    }
}
