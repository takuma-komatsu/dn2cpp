using System.IO.Compression;
using DnZlib.Tests.Oracles;
using DnZlib.Tests.Support;

namespace DnZlib.Tests;

/// <summary>Inflate must never crash or hang on arbitrary, truncated, or corrupted input.</summary>
public class RobustnessFuzzTests
{
    public static IEnumerable<object[]> Corpora() => Corpus.Data();

    [Fact]
    public void RandomGarbage_NeverCrashesOrHangs()
    {
        var rnd = new Random(0xF0F0);
        for (int i = 0; i < 3000; i++)
        {
            var buf = new byte[rnd.Next(0, 300)];
            rnd.NextBytes(buf);
            foreach (int wb in new[] { 15, 31, -15, 47 })
                _ = InflateHelper.TryInflate(buf, wb);
        }
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void TruncatedStreams_NeverCrashOrHang(string name, byte[] data)
    {
        _ = name;
        byte[] comp = SystemZlib.DeflateZlib(data, CompressionLevel.Optimal);
        if (comp.Length == 0)
            return;
        int step = Math.Max(1, comp.Length / 64);
        for (int len = 0; len < comp.Length; len += step)
            _ = InflateHelper.TryInflate(comp[..len], 15);
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void CorruptedStreams_NeverCrashOrHang(string name, byte[] data)
    {
        _ = name;
        byte[] comp = SystemZlib.DeflateGzip(data, CompressionLevel.Optimal);
        if (comp.Length == 0)
            return;
        var rnd = new Random(data.Length * 131 + 7);
        for (int t = 0; t < 60; t++)
        {
            var c = (byte[])comp.Clone();
            c[rnd.Next(c.Length)] ^= (byte)(1 << rnd.Next(8));
            _ = InflateHelper.TryInflate(c, 31);
        }
    }

    [Fact]
    public void InvalidHeaders_ReturnError()
    {
        // Not a zlib header (fails the %31 check).
        Assert.Null(InflateHelper.TryInflate([0x00, 0x00], 15));
        // Bad compression method.
        Assert.Null(InflateHelper.TryInflate([0x79, 0x00], 15));
        // gzip magic but truncated.
        Assert.Null(InflateHelper.TryInflate([0x1f, 0x8b], 31));
    }
}
