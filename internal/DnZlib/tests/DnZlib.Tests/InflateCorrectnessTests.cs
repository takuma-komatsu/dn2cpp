using System.IO.Compression;
using DnZlib.Tests.Oracles;
using DnZlib.Tests.Support;

namespace DnZlib.Tests;

public class InflateCorrectnessTests
{
    public static IEnumerable<object[]> Corpora() => Corpus.Data();

    // .NET's DeflateStream/ZLibStream/GZipStream emit *nothing* for empty input (a BCL quirk), so
    // an empty compressed buffer legitimately maps back to empty data. Real zlib streams (native
    // oracle) always carry a header/trailer and are exercised below.
    private static void Check(byte[] data, byte[] comp, int windowBits, int outChunk = 1 << 16)
    {
        if (comp.Length == 0)
        {
            Assert.Empty(data);
            return;
        }
        Assert.Equal(data, InflateHelper.InflateAll(comp, windowBits, outChunk));
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Inflate_Zlib_FromSystem(string name, byte[] data)
    {
        _ = name;
        Check(data, SystemZlib.DeflateZlib(data, CompressionLevel.Optimal), 15);
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Inflate_Gzip_FromSystem(string name, byte[] data)
    {
        _ = name;
        Check(data, SystemZlib.DeflateGzip(data, CompressionLevel.Optimal), 31);
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Inflate_Raw_FromSystem(string name, byte[] data)
    {
        _ = name;
        Check(data, SystemZlib.DeflateRaw(data, CompressionLevel.Optimal), -15);
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Inflate_AutoDetect_Zlib(string name, byte[] data)
    {
        _ = name;
        Check(data, SystemZlib.DeflateZlib(data, CompressionLevel.SmallestSize), 47);
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Inflate_AutoDetect_Gzip(string name, byte[] data)
    {
        _ = name;
        Check(data, SystemZlib.DeflateGzip(data, CompressionLevel.SmallestSize), 47);
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Inflate_Zlib_FromNative(string name, byte[] data)
    {
        _ = name;
        if (!LibZOracle.Available)
            return;
        // Native zlib always emits a real 8-byte stream even for empty input.
        Assert.Equal(data, InflateHelper.InflateAll(LibZOracle.Compress(data, level: 9), 15));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(13)]
    [InlineData(101)]
    [InlineData(4096)]
    // 258+ drives every call through InflateFast's chunked match-copy path (avail_out>=258) rather
    // than the sub-258 slow path, exercising the vectorized copy at the smallest possible margins.
    [InlineData(258)]
    [InlineData(259)]
    [InlineData(260)]
    [InlineData(300)]
    public void Inflate_TinyOutputChunks_ExercisesWindow(int chunk)
    {
        byte[] data = Corpus.All.First(c => c.Name == "pattern200k").Data;
        byte[] comp = SystemZlib.DeflateZlib(data, CompressionLevel.Optimal);
        Assert.Equal(data, InflateHelper.InflateAll(comp, 15, outChunk: chunk));
    }

    // Forces every symbol through the generic (non-Fast) Match case with the smallest possible
    // per-call output space, and asserts bytes past the caller's destination slice are never
    // touched — the failure mode a plain decompressed-data equality check would not catch.
    [Theory]
    [InlineData("pattern200k", 1)]
    [InlineData("pattern200k", 2)]
    [InlineData("pattern200k", 3)]
    [InlineData("repeatA5k", 1)]
    [InlineData("repeatA5k", 2)]
    [InlineData("repeatA5k", 3)]
    public void Inflate_TinyOutputChunks_NeverWritesPastDestination(string corpusName, int chunk)
    {
        byte[] data = Corpus.All.First(c => c.Name == corpusName).Data;
        byte[] comp = SystemZlib.DeflateZlib(data, CompressionLevel.Optimal);
        Assert.Equal(data, InflateHelper.InflateAllWithSentinelGuard(comp, 15, outChunk: chunk));
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Inflate_AllLevels_Zlib(string name, byte[] data)
    {
        _ = name;
        foreach (CompressionLevel level in new[]
                 {
                     CompressionLevel.NoCompression, CompressionLevel.Fastest,
                     CompressionLevel.Optimal, CompressionLevel.SmallestSize,
                 })
        {
            Check(data, SystemZlib.DeflateZlib(data, level), 15, outChunk: 4096);
        }
    }
}
