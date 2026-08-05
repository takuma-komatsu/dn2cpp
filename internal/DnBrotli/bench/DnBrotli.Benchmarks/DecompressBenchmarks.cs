using BenchmarkDotNet.Attributes;
using BclBrotliDecoder = System.IO.Compression.BrotliDecoder;
using BclBrotliEncoder = System.IO.Compression.BrotliEncoder;

namespace DnBrotli.Benchmarks;

/// <summary>
/// Decompression throughput: DnBrotli vs System.IO.Compression (native brotli), over blobs
/// pre-compressed by the BCL (= native brotli) at quality 4 — real-world-encoder output, not
/// DnBrotli's own (DnBrotli's raw stream happens to be byte-identical to native brotli v1.1.0
/// today, but that is implementation-defined and nothing here should rely on it).
/// </summary>
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
        var dest = new byte[BclBrotliEncoder.GetMaxCompressedLength(data.Length)];
        if (!BclBrotliEncoder.TryCompress(data, dest, out int written, 4, 22))
            throw new InvalidOperationException("BCL BrotliEncoder.TryCompress failed");
        _compressed = dest.AsSpan(0, written).ToArray();
    }

    [Benchmark]
    public byte[] DnBrotli() => Brotli.Decompress(_compressed, _originalLength);

    [Benchmark(Baseline = true)]
    public byte[] SystemIOCompression()
    {
        // Like-for-like one-shot: the BCL's static BrotliDecoder.TryDecompress into an
        // exact-size buffer, matching Brotli.Decompress's sizeHint-seeded single rental.
        var outBuf = new byte[_originalLength];
        if (!BclBrotliDecoder.TryDecompress(_compressed, outBuf, out int written)
            || written != _originalLength)
        {
            throw new InvalidOperationException("BCL BrotliDecoder.TryDecompress failed");
        }
        return outBuf;
    }
}
