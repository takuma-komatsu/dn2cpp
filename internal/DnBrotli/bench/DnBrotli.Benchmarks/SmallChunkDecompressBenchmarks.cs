using System.Buffers;
using BenchmarkDotNet.Attributes;
using BclBrotliEncoder = System.IO.Compression.BrotliEncoder;

namespace DnBrotli.Benchmarks;

/// <summary>
/// Decompression throughput when the caller drains output in small (64-byte) chunks through the
/// resumable <see cref="BrotliDecoder"/> struct — the many-small-writes path a streaming consumer
/// (e.g. <c>BrotliStream.Read</c> into a small buffer) exercises, where per-call overhead
/// dominates instead of bulk copy throughput.
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
        var dest = new byte[BclBrotliEncoder.GetMaxCompressedLength(data.Length)];
        if (!BclBrotliEncoder.TryCompress(data, dest, out int written, 4, 22))
            throw new InvalidOperationException("BCL BrotliEncoder.TryCompress failed");
        _compressed = dest.AsSpan(0, written).ToArray();
    }

    [Benchmark]
    public int DnBrotli_64ByteChunks()
    {
        using var decoder = new BrotliDecoder();
        var outBuf = new byte[64];
        int total = 0, consumedTotal = 0;
        ReadOnlySpan<byte> input = _compressed;

        while (true)
        {
            OperationStatus status = decoder.Decompress(input[consumedTotal..], outBuf,
                                                        out int consumed, out int written);
            consumedTotal += consumed;
            total += written;
            if (status == OperationStatus.Done)
                break;
            if (status == OperationStatus.InvalidData)
                throw new InvalidOperationException("brotli stream reported InvalidData");
        }

        return total;
    }
}
