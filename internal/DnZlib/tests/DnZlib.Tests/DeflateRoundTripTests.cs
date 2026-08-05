using DnZlib;
using DnZlib.Tests.Oracles;
using DnZlib.Tests.Support;

namespace DnZlib.Tests;

public class DeflateRoundTripTests
{
    public static IEnumerable<object[]> Corpora() => Corpus.Data();

    private static readonly int[] Levels = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    [Theory]
    [MemberData(nameof(Corpora))]
    public void OurCompress_OurInflate_Zlib(string name, byte[] data)
    {
        _ = name;
        foreach (int level in Levels)
        {
            byte[] comp = Zlib.Compress(data, level, ZlibFormat.Zlib);
            Assert.Equal(data, Zlib.Uncompress(comp, ZlibFormat.Zlib));
        }
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void OurCompress_OurInflate_Gzip(string name, byte[] data)
    {
        _ = name;
        foreach (int level in Levels)
        {
            byte[] comp = Zlib.Compress(data, level, ZlibFormat.Gzip);
            Assert.Equal(data, Zlib.Uncompress(comp, ZlibFormat.AutoDetect));
        }
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void OurCompress_OurInflate_Raw(string name, byte[] data)
    {
        _ = name;
        foreach (int level in Levels)
        {
            byte[] comp = Zlib.Compress(data, level, ZlibFormat.Raw);
            Assert.Equal(data, Zlib.Uncompress(comp, ZlibFormat.Raw));
        }
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void OurCompress_SystemInflate_Zlib(string name, byte[] data)
    {
        _ = name;
        foreach (int level in Levels)
        {
            byte[] comp = Zlib.Compress(data, level, ZlibFormat.Zlib);
            Assert.Equal(data, SystemZlib.InflateZlib(comp));
        }
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void OurCompress_SystemInflate_Gzip(string name, byte[] data)
    {
        _ = name;
        foreach (int level in Levels)
        {
            byte[] comp = Zlib.Compress(data, level, ZlibFormat.Gzip);
            Assert.Equal(data, SystemZlib.InflateGzip(comp));
        }
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void OurCompress_NativeInflate_Zlib(string name, byte[] data)
    {
        _ = name;
        if (!LibZOracle.Available)
            return;
        foreach (int level in Levels)
        {
            byte[] comp = Zlib.Compress(data, level, ZlibFormat.Zlib);
            Assert.Equal(data, LibZOracle.Uncompress(comp, data.Length));
        }
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void OurCompress_Strategies_RoundTrip(string name, byte[] data)
    {
        _ = name;
        foreach (int level in Levels)
        foreach (CompressionStrategy strat in new[]
                 {
                     CompressionStrategy.Filtered, CompressionStrategy.HuffmanOnly,
                     CompressionStrategy.Rle, CompressionStrategy.Fixed,
                 })
        {
            byte[] comp = Zlib.Compress(data, level, ZlibFormat.Zlib, strat);
            Assert.Equal(data, Zlib.Uncompress(comp, ZlibFormat.Zlib));
            Assert.Equal(data, SystemZlib.InflateZlib(comp));
        }
    }

    [Fact]
    public void Compress_ActuallyCompresses()
    {
        byte[] data = Corpus.All.First(c => c.Name == "textish100k").Data;
        byte[] comp = Zlib.Compress(data, 6, ZlibFormat.Zlib);
        Assert.True(comp.Length < data.Length / 2, $"expected compression, got {comp.Length} vs {data.Length}");
    }

    [Fact]
    public void TryCompress_TryUncompress_RoundTrip()
    {
        byte[] data = Corpus.All.First(c => c.Name == "dna64k").Data;
        var comp = new byte[Zlib.CompressBound(data.Length)];
        Assert.True(Zlib.TryCompress(data, comp, out int cw, 6));
        var back = new byte[data.Length];
        Assert.True(Zlib.TryUncompress(comp.AsSpan(0, cw), back, out int dw));
        Assert.Equal(data.Length, dw);
        Assert.Equal(data, back);
    }
}
