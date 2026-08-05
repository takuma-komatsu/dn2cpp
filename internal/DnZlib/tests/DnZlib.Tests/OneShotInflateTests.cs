using System.IO.Compression;
using DnZlib;
using DnZlib.Tests.Oracles;
using DnZlib.Tests.Support;

namespace DnZlib.Tests;

public class OneShotInflateTests
{
    public static IEnumerable<object[]> Corpora() => Corpus.Data();

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Uncompress_Zlib(string name, byte[] data)
    {
        _ = name;
        byte[] comp = SystemZlib.DeflateZlib(data, CompressionLevel.Optimal);
        if (comp.Length == 0)
            return;
        Assert.Equal(data, Zlib.Uncompress(comp, ZlibFormat.Zlib));
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Uncompress_Gzip_AutoDetect(string name, byte[] data)
    {
        _ = name;
        byte[] comp = SystemZlib.DeflateGzip(data, CompressionLevel.Optimal);
        if (comp.Length == 0)
            return;
        Assert.Equal(data, Zlib.Uncompress(comp, ZlibFormat.AutoDetect));
    }

    [Fact]
    public void TryUncompress_RightSized_Succeeds()
    {
        byte[] data = Corpus.All.First(c => c.Name == "textish100k").Data;
        byte[] comp = SystemZlib.DeflateZlib(data, CompressionLevel.Optimal);
        var dst = new byte[data.Length];
        Assert.True(Zlib.TryUncompress(comp, dst, out int written));
        Assert.Equal(data.Length, written);
        Assert.Equal(data, dst);
    }

    [Fact]
    public void TryUncompress_TooSmall_ReturnsFalse()
    {
        byte[] data = Corpus.All.First(c => c.Name == "dna64k").Data;
        byte[] comp = SystemZlib.DeflateZlib(data, CompressionLevel.Optimal);
        var dst = new byte[data.Length / 2];
        Assert.False(Zlib.TryUncompress(comp, dst, out int written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void Uncompress_UndersizedHint_GrowsAndDecompressesCorrectly()
    {
        byte[] data = Corpus.All.First(c => c.Name == "zeros70k").Data;
        byte[] comp = SystemZlib.DeflateZlib(data, CompressionLevel.Optimal);
        byte[] result = Zlib.Uncompress(comp, ZlibFormat.Zlib, sizeHint: 1);
        Assert.Equal(data, result);
    }

    [Fact]
    public void Uncompress_AccurateHint_DoesNotCopyCompressedInput()
    {
        byte[] data = Corpus.All.First(c => c.Name == "textish100k").Data;
        byte[] comp = SystemZlib.DeflateZlib(data, CompressionLevel.Optimal);

        // Warm up (JIT) before measuring so the assertion isn't polluted by one-time costs.
        _ = Zlib.Uncompress(comp, ZlibFormat.Zlib, sizeHint: data.Length);

        long before = GC.GetAllocatedBytesForCurrentThread();
        byte[] result = Zlib.Uncompress(comp, ZlibFormat.Zlib, sizeHint: data.Length);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(data, result);
        // A regression back to `source.ToArray()` would add ~comp.Length bytes on top of this.
        Assert.True(allocated < data.Length + comp.Length / 2,
            $"expected allocation close to {data.Length} bytes (no defensive input copy), got {allocated}");
    }
}
