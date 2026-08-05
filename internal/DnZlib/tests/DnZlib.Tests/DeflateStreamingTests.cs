using System.Text;
using DnZlib;
using DnZlib.Tests.Oracles;
using DnZlib.Tests.Support;

namespace DnZlib.Tests;

public class DeflateStreamingTests
{
    public static IEnumerable<object[]> Corpora() => Corpus.Data();

    [Theory]
    [MemberData(nameof(Corpora))]
    public void ChunkedOutput_RoundTrips(string name, byte[] data)
    {
        _ = name;
        foreach (int level in new[] { 1, 4, 5, 6, 9 })
        foreach (int outChunk in new[] { 7, 64, 4096 })
        {
            byte[] comp = DeflateHelper.DeflateAll(data, level, 15, outChunk);
            Assert.Equal(data, Zlib.Uncompress(comp, ZlibFormat.Zlib));
            Assert.Equal(data, SystemZlib.InflateZlib(comp));
        }
    }

    [Fact]
    public void Dictionary_RoundTrip_Ours()
    {
        byte[] dict = Encoding.ASCII.GetBytes("the quick brown fox jumps over the lazy dog ");
        byte[] data = Encoding.ASCII.GetBytes(
            "the lazy dog jumps over the quick brown fox the quick brown fox is quick and brown");
        byte[] comp = DeflateHelper.DeflateWithDictionary(data, dict, 6, 15);
        Assert.Equal(data, DeflateHelper.InflateWithDictionary(comp, dict, 15));
    }

    [Fact]
    public void Dictionary_ImprovesCompression()
    {
        byte[] dict = Corpus.All.First(c => c.Name == "textish100k").Data.AsSpan(0, 4096).ToArray();
        byte[] data = Corpus.All.First(c => c.Name == "textish100k").Data.AsSpan(4096, 2000).ToArray();
        byte[] withDict = DeflateHelper.DeflateWithDictionary(data, dict, 9, 15);
        byte[] without = Zlib.Compress(data, 9, ZlibFormat.Zlib);
        // A relevant dictionary should not hurt and generally helps small inputs.
        Assert.Equal(data, DeflateHelper.InflateWithDictionary(withDict, dict, 15));
        Assert.True(withDict.Length <= without.Length + 8);
    }

    [Fact]
    public void SyncFlush_MidStream_RoundTrips()
    {
        byte[] data = Corpus.All.First(c => c.Name == "textish100k").Data;
        using var zc = new ZStream();
        Assert.Equal(ZlibResult.Ok, zc.DeflateInit2(6, CompressionMethod.Deflated, 15, 8, CompressionStrategy.Default));
        using var ms = new MemoryStream();
        var outBuf = new byte[65536];

        zc.Input = data.AsMemory(0, data.Length / 2);
        Drain(zc, ms, outBuf, FlushMode.SyncFlush);
        zc.Input = data.AsMemory(data.Length / 2);
        Drain(zc, ms, outBuf, FlushMode.Finish);
        zc.DeflateEnd();

        byte[] comp = ms.ToArray();
        Assert.Equal(data, Zlib.Uncompress(comp, ZlibFormat.Zlib));
        Assert.Equal(data, SystemZlib.InflateZlib(comp));
    }

    private static void Drain(ZStream zc, MemoryStream ms, byte[] outBuf, FlushMode flush)
    {
        while (true)
        {
            zc.Output = outBuf;
            long before = zc.TotalOut;
            ZlibResult rc = zc.Deflate(flush);
            ms.Write(outBuf, 0, (int)(zc.TotalOut - before));
            if (flush == FlushMode.Finish)
            {
                if (rc == ZlibResult.StreamEnd)
                    break;
            }
            else if (zc.Output.Length > 0)
            {
                break; // flush completed with output space remaining
            }
        }
    }

    [Fact]
    public void DeflateReset_ReuseEqualsFresh()
    {
        byte[] a = Corpus.All.First(c => c.Name == "dna64k").Data;
        byte[] b = Corpus.All.First(c => c.Name == "textish100k").Data;

        using var zs = new ZStream();
        zs.DeflateInit2(6, CompressionMethod.Deflated, 15, 8, CompressionStrategy.Default);
        zs.Input = a;
        byte[] first = ReadAll(zs);

        Assert.Equal(ZlibResult.Ok, zs.DeflateReset());
        zs.Input = b;
        byte[] second = ReadAll(zs);
        zs.DeflateEnd();

        Assert.Equal(a, Zlib.Uncompress(first, ZlibFormat.Zlib));
        Assert.Equal(b, Zlib.Uncompress(second, ZlibFormat.Zlib));
        Assert.Equal(Zlib.Compress(b, 6, ZlibFormat.Zlib), second);
    }

    private static byte[] ReadAll(ZStream zs)
    {
        var outBuf = new byte[1 << 16];
        using var ms = new MemoryStream();
        while (true)
        {
            zs.Output = outBuf;
            long before = zs.TotalOut;
            ZlibResult rc = zs.Deflate(FlushMode.Finish);
            ms.Write(outBuf, 0, (int)(zs.TotalOut - before));
            if (rc == ZlibResult.StreamEnd)
                break;
        }
        return ms.ToArray();
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void DeflateBound_IsUpperBound(string name, byte[] data)
    {
        _ = name;
        foreach (int level in new[] { 0, 1, 6, 9 })
        {
            byte[] comp = Zlib.Compress(data, level, ZlibFormat.Zlib);
            Assert.True(comp.Length <= Zlib.CompressBound(data.Length, ZlibFormat.Zlib),
                $"level {level}: {comp.Length} > bound {Zlib.CompressBound(data.Length, ZlibFormat.Zlib)}");
        }
    }
}
