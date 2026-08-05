using System.Runtime.InteropServices;
using DnZlib.Inflate;

namespace DnZlib.Tests.Inflate;

public class ChunkCopyHelperTests
{
    private const int Pad = 32;
    private const int Slack = 32;
    private const byte Sentinel = 0xEE;

    public static IEnumerable<object[]> DistLenCombinations()
    {
        int[] dists = { 1, 2, 3, 15, 16, 17, 31, 32 };
        int[] lens = { 1, 3, 4, 15, 16, 17, 31, 32, 257, 258 };
        foreach (int dist in dists)
            foreach (int len in lens)
                yield return new object[] { dist, len };
    }

    /// <summary>Plain byte-by-byte LZ77 forward propagation (the scalar loop being replaced), used
    /// as the oracle: dist &lt; len repeats the pattern (dist=1 is RLE), not memmove semantics.</summary>
    private static byte[] ForwardPropagate(int dist, int len)
    {
        var scratch = new byte[dist + len];
        for (int i = 0; i < dist; i++)
            scratch[i] = (byte)((i % 250) + 1); // avoid 0 and the 0xEE sentinel
        for (int i = 0; i < len; i++)
            scratch[dist + i] = scratch[i];
        return scratch[dist..];
    }

    [Theory]
    [MemberData(nameof(DistLenCombinations))]
    public unsafe void CopyOverlapping_MatchesForwardLoop(int dist, int len)
    {
        byte[] expected = ForwardPropagate(dist, len);
        int total = Pad + dist + len + Slack;
        byte* buf = (byte*)NativeMemory.Alloc((nuint)total);
        try
        {
            for (int i = 0; i < total; i++) buf[i] = Sentinel;
            for (int i = 0; i < dist; i++) buf[Pad + i] = (byte)((i % 250) + 1);

            byte* dst = buf + Pad + dist;
            byte* safeEnd = dst + len; // zero headroom past the requested range

            byte* result = ChunkCopyHelper.CopyOverlapping(dst, (uint)dist, (uint)len, safeEnd);

            Assert.Equal((nint)safeEnd, (nint)result);
            for (int i = 0; i < len; i++)
                Assert.Equal(expected[i], dst[i]);
            for (int i = 0; i < Slack; i++)
                Assert.Equal(Sentinel, safeEnd[i]);
        }
        finally
        {
            NativeMemory.Free(buf);
        }
    }

    [Theory]
    [MemberData(nameof(DistLenCombinations))]
    public unsafe void CopyDisjoint_MatchesSourceBuffer(int dist, int len)
    {
        // dist is unused by CopyDisjoint itself; reusing the same data set just varies len.
        _ = dist;
        byte[] source = new byte[len];
        for (int i = 0; i < len; i++) source[i] = (byte)((i % 250) + 1);

        int total = len + Slack;
        byte* dstBuf = (byte*)NativeMemory.Alloc((nuint)total);
        fixed (byte* srcBuf = source)
        {
            try
            {
                for (int i = 0; i < total; i++) dstBuf[i] = Sentinel;

                byte* safeEnd = dstBuf + len;
                byte* result = ChunkCopyHelper.CopyDisjoint(dstBuf, srcBuf, (uint)len, safeEnd);

                Assert.Equal((nint)safeEnd, (nint)result);
                for (int i = 0; i < len; i++)
                    Assert.Equal(source[i], dstBuf[i]);
                for (int i = 0; i < Slack; i++)
                    Assert.Equal(Sentinel, safeEnd[i]);
            }
            finally
            {
                NativeMemory.Free(dstBuf);
            }
        }
    }

    /// <summary>
    /// The non-obvious case found during design: a match split so that a large disjoint window
    /// segment is followed by a tiny (&lt; ChunkSize) overlapping tail right at the true buffer end
    /// (e.g. dist=257, len=258 mirrors InfFast's op=257/len=258 wnext==0 case). A naive
    /// "avail_out&gt;=258 means 16 bytes of overshoot is always safe" implementation overruns here;
    /// CopyOverlapping must fall back to the scalar tail instead.
    /// </summary>
    [Theory]
    [InlineData(257, 258)]
    [InlineData(255, 258)]
    [InlineData(1, 258)]
    [InlineData(2, 258)]
    public unsafe void CopyOverlapping_TinyHeadroomAtBufferEnd_NoOverrun(int dist, int len)
    {
        byte[] expected = ForwardPropagate(dist, len);
        int total = Pad + dist + len; // no slack at all past safeEnd
        byte* buf = (byte*)NativeMemory.Alloc((nuint)total);
        try
        {
            for (int i = 0; i < total; i++) buf[i] = Sentinel;
            for (int i = 0; i < dist; i++) buf[Pad + i] = (byte)((i % 250) + 1);

            byte* dst = buf + Pad + dist;
            byte* safeEnd = dst + len; // == buf + total: one byte past this is unallocated

            byte* result = ChunkCopyHelper.CopyOverlapping(dst, (uint)dist, (uint)len, safeEnd);

            Assert.Equal((nint)safeEnd, (nint)result);
            for (int i = 0; i < len; i++)
                Assert.Equal(expected[i], dst[i]);
        }
        finally
        {
            NativeMemory.Free(buf);
        }
    }

    [Fact]
    public unsafe void CopyOverlapping_ZeroLength_NoOp()
    {
        byte* buf = (byte*)NativeMemory.Alloc(16);
        try
        {
            for (int i = 0; i < 16; i++) buf[i] = Sentinel;
            byte* dst = buf + 8;
            byte* result = ChunkCopyHelper.CopyOverlapping(dst, 3, 0, dst);
            Assert.Equal((nint)dst, (nint)result);
            for (int i = 0; i < 16; i++)
                Assert.Equal(Sentinel, buf[i]);
        }
        finally
        {
            NativeMemory.Free(buf);
        }
    }
}
