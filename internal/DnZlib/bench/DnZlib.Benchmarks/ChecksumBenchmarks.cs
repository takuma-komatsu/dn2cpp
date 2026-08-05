using BenchmarkDotNet.Attributes;
using DnZlib;

namespace DnZlib.Benchmarks;

/// <summary>Throughput of our checksums across input sizes (run: <c>--filter *Checksum*</c>).</summary>
[MemoryDiagnoser]
public class ChecksumBenchmarks
{
    [Params(4096, 65536, 1048576)]
    public int Size;

    private byte[] _data = [];

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[Size];
        new Random(1).NextBytes(_data);
    }

    [Benchmark]
    public uint Crc32_Ours() => Crc32.Compute(_data);

    [Benchmark]
    public uint Adler32_Ours() => Adler32.Compute(_data);
}
