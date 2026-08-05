using System.Text;
using DnZlib;
using DnZlib.Tests.Oracles;

namespace DnZlib.Tests;

public class ChecksumTests
{
    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    /// <summary>Deterministic buffers spanning sizes, alignments, and content classes.</summary>
    public static IEnumerable<object[]> Buffers()
    {
        yield return [Array.Empty<byte>()];
        yield return [new byte[] { 0 }];
        yield return [new byte[] { 255 }];
        yield return [Ascii("a")];
        yield return [Ascii("123456789")];
        yield return [Ascii("The quick brown fox jumps over the lazy dog")];
        yield return [new byte[15]];
        yield return [new byte[16]];
        yield return [new byte[17]];
        yield return [new byte[1000]]; // all zeros
        yield return [Enumerable.Range(0, 256).Select(i => (byte)i).ToArray()];

        var rnd = new Random(0xC0FFEE);
        foreach (int n in new[] { 63, 64, 65, 5551, 5552, 5553, 65535, 65536, 100003 })
        {
            var b = new byte[n];
            rnd.NextBytes(b);
            yield return [b];
        }
    }

    [Fact]
    public void NativeOracle_IsAvailable()
    {
        // The verification strategy relies on a native zlib being present in the dev environment.
        Assert.True(LibZOracle.Available, "native zlib (libz) failed to load for oracle cross-checks");
    }

    [Fact]
    public void Crc32_KnownVectors()
    {
        Assert.Equal(0u, Crc32.Compute(Array.Empty<byte>()));
        Assert.Equal(0xCBF43926u, Crc32.Compute(Ascii("123456789")));
    }

    [Fact]
    public void Adler32_KnownVectors()
    {
        Assert.Equal(1u, Adler32.Compute(Array.Empty<byte>()));
        Assert.Equal(0x091E01DEu, Adler32.Compute(Ascii("123456789")));
    }

    [Theory]
    [MemberData(nameof(Buffers))]
    public void Crc32_MatchesNative(byte[] data)
    {
        if (!LibZOracle.Available)
            return;
        Assert.Equal(LibZOracle.Crc32(data), Crc32.Compute(data));
    }

    [Theory]
    [MemberData(nameof(Buffers))]
    public void Adler32_MatchesNative(byte[] data)
    {
        if (!LibZOracle.Available)
            return;
        Assert.Equal(LibZOracle.Adler32(data), Adler32.Compute(data));
    }

    [Theory]
    [MemberData(nameof(Buffers))]
    public void Crc32_IncrementalEqualsWhole(byte[] data)
    {
        for (int split = 0; split <= data.Length; split += Math.Max(1, data.Length / 4))
        {
            uint crc = Crc32.Update(0, data.AsSpan(0, split));
            crc = Crc32.Update(crc, data.AsSpan(split));
            Assert.Equal(Crc32.Compute(data), crc);
        }
    }

    [Theory]
    [MemberData(nameof(Buffers))]
    public void Adler32_IncrementalEqualsWhole(byte[] data)
    {
        for (int split = 0; split <= data.Length; split += Math.Max(1, data.Length / 4))
        {
            uint adler = Adler32.Update(1, data.AsSpan(0, split));
            adler = Adler32.Update(adler, data.AsSpan(split));
            Assert.Equal(Adler32.Compute(data), adler);
        }
    }

    [Theory]
    [MemberData(nameof(Buffers))]
    public void Crc32_CombineEqualsWhole(byte[] data)
    {
        int split = data.Length / 2;
        byte[] a = data[..split];
        byte[] b = data[split..];
        uint combined = Crc32.Combine(Crc32.Compute(a), Crc32.Compute(b), b.Length);
        Assert.Equal(Crc32.Compute(data), combined);
    }

    [Theory]
    [MemberData(nameof(Buffers))]
    public void Adler32_CombineEqualsWhole(byte[] data)
    {
        int split = data.Length / 2;
        byte[] a = data[..split];
        byte[] b = data[split..];
        uint combined = Adler32.Combine(Adler32.Compute(a), Adler32.Compute(b), b.Length);
        Assert.Equal(Adler32.Compute(data), combined);
    }

    [Theory]
    [MemberData(nameof(Buffers))]
    public unsafe void Adler32_VectorMatchesScalar(byte[] data)
    {
        fixed (byte* p = data)
        {
            uint scalar = Adler32.UpdateScalar(1, p, (nuint)data.Length);
            uint vector = Adler32.UpdateVector(1, p, (nuint)data.Length);
            Assert.Equal(scalar, vector);
        }
    }

    [Fact]
    public void Checksums_AllAlignments()
    {
        var rnd = new Random(7);
        var buf = new byte[512];
        rnd.NextBytes(buf);
        for (int off = 0; off < 16; off++)
        {
            for (int len = 0; len < 128; len++)
            {
                ReadOnlySpan<byte> slice = buf.AsSpan(off, len);
                if (LibZOracle.Available)
                {
                    Assert.Equal(LibZOracle.Crc32(slice), Crc32.Compute(slice));
                    Assert.Equal(LibZOracle.Adler32(slice), Adler32.Compute(slice));
                }
            }
        }
    }
}
