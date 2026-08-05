using System.Runtime.InteropServices;
using System.Text;
using DnZlib.Raw;
using DnZlib.Tests.Oracles;
using DnZlib.Tests.Support;

namespace DnZlib.Tests;

/// <summary>
/// Exercises <see cref="RawZlib"/> directly — the zlib-C-ABI-shaped layer <see cref="ZStream"/>
/// and <see cref="Zlib"/> are built on top of. No P/Invoke happens anywhere here; these calls are
/// Pure C#, but the shapes (a <see cref="z_stream"/> struct plus free functions taking pointers to
/// it) are exactly what a real <c>DllImport</c> binding over native zlib would look like.
/// </summary>
public unsafe class RawApiTests
{
    [Theory]
    [MemberData(nameof(Corpus.Data), MemberType = typeof(Corpus))]
    public void DeflateInflate_RoundTrip_ThroughRawPointers(string name, byte[] data)
    {
        _ = name;
        var compressed = new byte[(int)RawZlib.compressBound((nuint)data.Length) + 64];
        int compressedLen;

        z_stream dstrm = default;
        fixed (byte* srcPtr = data)
        fixed (byte* destPtr = compressed)
        {
            Assert.Equal((int)ZlibResult.Ok,
                RawZlib.deflateInit2(&dstrm, 6, (int)CompressionMethod.Deflated, 15, 8, (int)CompressionStrategy.Default));

            dstrm.next_in = srcPtr;
            dstrm.avail_in = (uint)data.Length;
            dstrm.next_out = destPtr;
            dstrm.avail_out = (uint)compressed.Length;

            Assert.Equal((int)ZlibResult.StreamEnd, RawZlib.deflate(&dstrm, (int)FlushMode.Finish));
            compressedLen = (int)dstrm.total_out;
            Assert.Equal((int)ZlibResult.Ok, RawZlib.deflateEnd(&dstrm));
        }

        var decompressed = new byte[data.Length];
        z_stream istrm = default;
        fixed (byte* cPtr = compressed)
        fixed (byte* outPtr = decompressed)
        {
            Assert.Equal((int)ZlibResult.Ok, RawZlib.inflateInit2(&istrm, 15));

            istrm.next_in = cPtr;
            istrm.avail_in = (uint)compressedLen;
            istrm.next_out = outPtr;
            istrm.avail_out = (uint)decompressed.Length;

            Assert.Equal((int)ZlibResult.StreamEnd, RawZlib.inflate(&istrm, (int)FlushMode.Finish));
            Assert.Equal((long)data.Length, (long)istrm.total_out);
            Assert.Equal((int)ZlibResult.Ok, RawZlib.inflateEnd(&istrm));
        }

        Assert.Equal(data, decompressed);
    }

    [Theory]
    [MemberData(nameof(Corpus.Data), MemberType = typeof(Corpus))]
    public void Compress2_Uncompress_RoundTrip(string name, byte[] data)
    {
        _ = name;
        nuint destLen = RawZlib.compressBound((nuint)data.Length) + 64;
        var compressed = new byte[(int)destLen];

        fixed (byte* srcPtr = data)
        fixed (byte* destPtr = compressed)
        {
            Assert.Equal((int)ZlibResult.Ok, RawZlib.compress2(destPtr, &destLen, srcPtr, (nuint)data.Length, 6));
        }

        nuint uncompressedLen = (nuint)Math.Max(1, data.Length);
        var decompressed = new byte[(int)uncompressedLen];
        fixed (byte* cPtr = compressed)
        fixed (byte* outPtr = decompressed)
        {
            Assert.Equal((int)ZlibResult.Ok, RawZlib.uncompress(outPtr, &uncompressedLen, cPtr, destLen));
        }

        Assert.Equal((nuint)data.Length, uncompressedLen);
        Assert.Equal(data, decompressed.AsSpan(0, (int)uncompressedLen).ToArray());
    }

    [Theory]
    [MemberData(nameof(Corpus.Data), MemberType = typeof(Corpus))]
    public void Compress2_InteropsWith_NativeLibz(string name, byte[] data)
    {
        _ = name;
        if (!LibZOracle.Available)
            return;

        nuint destLen = RawZlib.compressBound((nuint)data.Length) + 64;
        var compressed = new byte[(int)destLen];
        fixed (byte* srcPtr = data)
        fixed (byte* destPtr = compressed)
        {
            Assert.Equal((int)ZlibResult.Ok, RawZlib.compress2(destPtr, &destLen, srcPtr, (nuint)data.Length, 6));
        }

        byte[] viaLibz = LibZOracle.Uncompress(compressed.AsSpan(0, (int)destLen), data.Length);
        Assert.Equal(data, viaLibz);
    }

    [Fact]
    public void DeflateInit2_RejectsBadParameters()
    {
        z_stream strm = default;
        Assert.Equal((int)ZlibResult.StreamError, RawZlib.deflateInit2(&strm, 10, (int)CompressionMethod.Deflated, 15, 8, (int)CompressionStrategy.Default));
        Assert.Equal((int)ZlibResult.StreamError, RawZlib.deflateInit2(&strm, 6, (int)CompressionMethod.Deflated, 7, 8, (int)CompressionStrategy.Default));
        Assert.Equal((int)ZlibResult.StreamError, RawZlib.deflateInit2(&strm, 6, (int)CompressionMethod.Deflated, 15, 0, (int)CompressionStrategy.Default));
    }

    [Fact]
    public void Init_RejectsVersionAndSizeMismatch()
    {
        z_stream strm = default;
        fixed (byte* badVersion = "9.9.9\0"u8)
        {
            Assert.Equal((int)ZlibResult.VersionError, RawZlib.deflateInit_(&strm, 6, badVersion, sizeof(z_stream)));
            Assert.Equal((int)ZlibResult.VersionError, RawZlib.inflateInit_(&strm, badVersion, sizeof(z_stream)));
        }
        fixed (byte* goodVersion = "1.3.2\0"u8)
        {
            Assert.Equal((int)ZlibResult.VersionError, RawZlib.deflateInit_(&strm, 6, goodVersion, sizeof(z_stream) + 1));
        }
    }

    [Fact]
    public void ZlibVersion_ReportsExpectedString()
    {
        string? version = Marshal.PtrToStringUTF8((IntPtr)RawZlib.zlibVersion());
        Assert.Equal("1.3.2", version);
    }

    [Fact]
    public void ZError_MapsKnownCodes()
    {
        Assert.Equal("stream end", Marshal.PtrToStringUTF8((IntPtr)RawZlib.zError((int)ZlibResult.StreamEnd)));
        Assert.Equal("data error", Marshal.PtrToStringUTF8((IntPtr)RawZlib.zError((int)ZlibResult.DataError)));
        Assert.Equal(string.Empty, Marshal.PtrToStringUTF8((IntPtr)RawZlib.zError((int)ZlibResult.Ok)));
    }

    [Theory]
    [MemberData(nameof(Corpus.Data), MemberType = typeof(Corpus))]
    public void Adler32Crc32_MatchManagedImplementations(string name, byte[] data)
    {
        _ = name;
        fixed (byte* p = data)
        {
            Assert.Equal(Adler32.Compute(data), (uint)RawZlib.adler32(1, p, (uint)data.Length));
            Assert.Equal(Crc32.Compute(data), (uint)RawZlib.crc32(0, p, (uint)data.Length));
        }
    }

    [Fact]
    public void Adler32Crc32_NullBuffer_ReturnsInitialValue()
    {
        Assert.Equal((nuint)1, RawZlib.adler32(1, null, 0));
        Assert.Equal((nuint)0, RawZlib.crc32(0, null, 0));
    }

    [Fact]
    public void Combine_MatchesSplitComputation()
    {
        byte[] a = Encoding.ASCII.GetBytes("the quick brown fox ");
        byte[] b = Encoding.ASCII.GetBytes("jumps over the lazy dog");

        uint adlerWhole = Adler32.Compute([.. a, .. b]);
        uint adlerA = Adler32.Compute(a);
        uint adlerB = Adler32.Compute(b);
        Assert.Equal(adlerWhole, (uint)RawZlib.adler32_combine(adlerA, adlerB, b.Length));

        uint crcWhole = Crc32.Compute([.. a, .. b]);
        uint crcA = Crc32.Compute(a);
        uint crcB = Crc32.Compute(b);
        Assert.Equal(crcWhole, (uint)RawZlib.crc32_combine(crcA, crcB, b.Length));
    }
}
