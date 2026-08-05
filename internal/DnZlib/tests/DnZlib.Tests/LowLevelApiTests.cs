using DnZlib;
using DnZlib.Tests.Support;

namespace DnZlib.Tests;

public class LowLevelApiTests
{
    [Fact]
    public void DeflateInit2_RejectsBadParameters()
    {
        using var zs = new ZStream();
        Assert.Equal(ZlibResult.StreamError, zs.DeflateInit2(10, CompressionMethod.Deflated, 15, 8, CompressionStrategy.Default));
        Assert.Equal(ZlibResult.StreamError, zs.DeflateInit2(6, CompressionMethod.Deflated, 7, 8, CompressionStrategy.Default));
        Assert.Equal(ZlibResult.StreamError, zs.DeflateInit2(6, CompressionMethod.Deflated, 16, 8, CompressionStrategy.Default));
        Assert.Equal(ZlibResult.StreamError, zs.DeflateInit2(6, CompressionMethod.Deflated, 15, 0, CompressionStrategy.Default));
        Assert.Equal(ZlibResult.StreamError, zs.DeflateInit2(6, CompressionMethod.Deflated, 15, 10, CompressionStrategy.Default));
    }

    [Fact]
    public void InflateInit2_RejectsBadWindowBits()
    {
        using var zs = new ZStream();
        Assert.Equal(ZlibResult.StreamError, zs.InflateInit2(-16));
        Assert.Equal(ZlibResult.StreamError, zs.InflateInit2(7));
    }

    [Fact]
    public void Operations_OnUninitializedStream_ReturnStreamError()
    {
        using var zs = new ZStream();
        Assert.Equal(ZlibResult.StreamError, zs.Deflate(FlushMode.NoFlush));
        Assert.Equal(ZlibResult.StreamError, zs.Inflate(FlushMode.NoFlush));
        Assert.Equal(ZlibResult.StreamError, zs.DeflateEnd());
        Assert.Equal(ZlibResult.StreamError, zs.InflateEnd());
    }

    [Fact]
    public void DoubleEnd_ReturnsStreamErrorSecondTime()
    {
        var zs = new ZStream();
        Assert.Equal(ZlibResult.Ok, zs.DeflateInit(6));
        Assert.Equal(ZlibResult.Ok, zs.DeflateEnd());
        Assert.Equal(ZlibResult.StreamError, zs.DeflateEnd());
    }

    [Fact]
    public void LowLevel_ManualDeflateInflate_RoundTrips()
    {
        byte[] data = Corpus.All.First(c => c.Name == "textish100k").Data;

        // Compress with the low-level API driven manually.
        byte[] comp;
        using (var zc = new ZStream())
        {
            Assert.Equal(ZlibResult.Ok, zc.DeflateInit2(6, CompressionMethod.Deflated, 15, 8, CompressionStrategy.Default));
            zc.Input = data;
            var outBuf = new byte[Zlib.CompressBound(data.Length)];
            zc.Output = outBuf;
            Assert.Equal(ZlibResult.StreamEnd, zc.Deflate(FlushMode.Finish));
            comp = outBuf.AsSpan(0, (int)zc.TotalOut).ToArray();
            Assert.Equal(ZlibResult.Ok, zc.DeflateEnd());
        }

        // Decompress with the low-level API.
        using var zd = new ZStream();
        Assert.Equal(ZlibResult.Ok, zd.InflateInit2(15));
        zd.Input = comp;
        var back = new byte[data.Length];
        zd.Output = back;
        Assert.Equal(ZlibResult.StreamEnd, zd.Inflate(FlushMode.Finish));
        Assert.Equal((long)data.Length, zd.TotalOut);
        Assert.Equal(ZlibResult.Ok, zd.InflateEnd());
        Assert.Equal(data, back);
    }

    [Fact]
    public void Adler_Tracks_UncompressedChecksum()
    {
        byte[] data = Corpus.All.First(c => c.Name == "dna64k").Data;
        using var zc = new ZStream();
        zc.DeflateInit2(6, CompressionMethod.Deflated, 15, 8, CompressionStrategy.Default);
        zc.Input = data;
        var outBuf = new byte[Zlib.CompressBound(data.Length)];
        zc.Output = outBuf;
        zc.Deflate(FlushMode.Finish);
        // After finish, the running adler equals adler32 of the input.
        Assert.Equal(Adler32.Compute(data), zc.Adler);
        zc.DeflateEnd();
    }
}
