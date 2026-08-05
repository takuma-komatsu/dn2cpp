using DnZlib.Streams;
using DnZlib.Tests.Support;
using Sys = System.IO.Compression;

namespace DnZlib.Tests;

public class StreamClassTests
{
    public static IEnumerable<object[]> Corpora() => Corpus.Data();

    private static byte[] CompressWith(byte[] data, Func<Stream, Stream> make)
    {
        using var ms = new MemoryStream();
        using (Stream s = make(ms))
            s.Write(data, 0, data.Length);
        return ms.ToArray();
    }

    private static byte[] DecompressWith(byte[] comp, Func<Stream, Stream> make)
    {
        using var src = new MemoryStream(comp);
        using Stream s = make(src);
        using var outMs = new MemoryStream();
        s.CopyTo(outMs);
        return outMs.ToArray();
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Gzip_RoundTrip_AndWireCompat(string name, byte[] data)
    {
        _ = name;
        byte[] c = CompressWith(data, ms => new GZipStream(ms, CompressionMode.Compress, true));
        Assert.Equal(data, DecompressWith(c, ms => new GZipStream(ms, CompressionMode.Decompress)));
        Assert.Equal(data, DecompressWith(c, ms => new Sys.GZipStream(ms, Sys.CompressionMode.Decompress)));
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void ZLib_RoundTrip_AndWireCompat(string name, byte[] data)
    {
        _ = name;
        byte[] c = CompressWith(data, ms => new ZLibStream(ms, CompressionMode.Compress, true));
        Assert.Equal(data, DecompressWith(c, ms => new ZLibStream(ms, CompressionMode.Decompress)));
        Assert.Equal(data, DecompressWith(c, ms => new Sys.ZLibStream(ms, Sys.CompressionMode.Decompress)));
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Raw_RoundTrip_AndWireCompat(string name, byte[] data)
    {
        _ = name;
        byte[] c = CompressWith(data, ms => new DeflateStream(ms, CompressionMode.Compress, true));
        Assert.Equal(data, DecompressWith(c, ms => new DeflateStream(ms, CompressionMode.Decompress)));
        Assert.Equal(data, DecompressWith(c, ms => new Sys.DeflateStream(ms, Sys.CompressionMode.Decompress)));
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void SystemCompress_OurDecompress(string name, byte[] data)
    {
        _ = name;
        byte[] cg = CompressWith(data, ms => new Sys.GZipStream(ms, Sys.CompressionLevel.Optimal, true));
        Assert.Equal(data, DecompressWith(cg, ms => new GZipStream(ms, CompressionMode.Decompress)));

        byte[] cz = CompressWith(data, ms => new Sys.ZLibStream(ms, Sys.CompressionLevel.SmallestSize, true));
        Assert.Equal(data, DecompressWith(cz, ms => new ZLibStream(ms, CompressionMode.Decompress)));
    }

    [Fact]
    public async Task Async_RoundTrip()
    {
        byte[] data = Corpus.All.First(c => c.Name == "textish100k").Data;
        var ms = new MemoryStream();
        await using (var s = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
            await s.WriteAsync(data);

        ms.Position = 0;
        var outMs = new MemoryStream();
        await using (var s = new GZipStream(ms, CompressionMode.Decompress))
            await s.CopyToAsync(outMs);

        Assert.Equal(data, outMs.ToArray());
    }

    [Fact]
    public void LeaveOpen_Respected()
    {
        var open = new MemoryStream();
        using (var s = new ZLibStream(open, CompressionMode.Compress, leaveOpen: true))
            s.Write([1, 2, 3]);
        Assert.True(open.CanRead); // still open

        var closed = new MemoryStream();
        using (var s = new ZLibStream(closed, CompressionMode.Compress))
            s.Write([1, 2, 3]);
        Assert.False(closed.CanRead); // disposed with the wrapper
    }

    [Fact]
    public void CompressionLevels_ProduceOrderedSizes()
    {
        byte[] data = Corpus.All.First(c => c.Name == "textish100k").Data;
        int no = CompressWith(data, ms => new ZLibStream(ms, CompressionLevel.NoCompression, true)).Length;
        int fast = CompressWith(data, ms => new ZLibStream(ms, CompressionLevel.Fastest, true)).Length;
        int opt = CompressWith(data, ms => new ZLibStream(ms, CompressionLevel.Optimal, true)).Length;
        int small = CompressWith(data, ms => new ZLibStream(ms, CompressionLevel.SmallestSize, true)).Length;

        Assert.True(small <= opt, $"small {small} > opt {opt}");
        Assert.True(opt <= fast, $"opt {opt} > fast {fast}");
        Assert.True(fast < no, $"fast {fast} >= no {no}");
    }

    [Fact]
    public void EmptyInput_RoundTrips()
    {
        byte[] c = CompressWith([], ms => new GZipStream(ms, CompressionMode.Compress, true));
        Assert.NotEmpty(c); // our stream emits a real (non-empty) gzip stream even for empty input
        Assert.Equal([], DecompressWith(c, ms => new GZipStream(ms, CompressionMode.Decompress)));
        Assert.Equal([], DecompressWith(c, ms => new Sys.GZipStream(ms, Sys.CompressionMode.Decompress)));
    }
}
