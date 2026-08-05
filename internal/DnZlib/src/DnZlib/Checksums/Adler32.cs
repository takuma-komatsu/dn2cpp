using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace DnZlib;

/// <summary>
/// Adler-32 checksum (the checksum used by the zlib container). Mirrors zlib's <c>adler32_z</c>.
/// </summary>
public static class Adler32
{
    private const uint Base = 65521; // largest prime below 2^16
    private const uint Nmax = 5552;  // largest n so that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1

    /// <summary>Compute the Adler-32 of <paramref name="data"/> (initial value 1).</summary>
    public static uint Compute(ReadOnlySpan<byte> data) => Update(1, data);

    /// <summary>Continue a running Adler-32 with more <paramref name="data"/>.</summary>
    public static uint Update(uint adler, ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return adler;
        unsafe
        {
            fixed (byte* p = data)
                return Update(adler, p, (nuint)data.Length);
        }
    }

    /// <summary>
    /// Combine two Adler-32 values as if their data streams were concatenated, where
    /// <paramref name="len2"/> is the byte length of the second stream.
    /// </summary>
    public static uint Combine(uint adler1, uint adler2, long len2)
    {
        if (len2 < 0)
            return 0xFFFFFFFFu;

        uint rem = (uint)(len2 % Base);
        uint sum1 = adler1 & 0xFFFF;
        uint sum2 = (uint)((ulong)rem * sum1 % Base);

        sum1 += (adler2 & 0xFFFF) + Base - 1;
        sum2 += ((adler1 >> 16) & 0xFFFF) + ((adler2 >> 16) & 0xFFFF) + Base - rem;

        if (sum1 >= Base) sum1 -= Base;
        if (sum1 >= Base) sum1 -= Base;
        if (sum2 >= (Base << 1)) sum2 -= (Base << 1);
        if (sum2 >= Base) sum2 -= Base;

        return sum1 | (sum2 << 16);
    }

    /// <summary>Core update over a raw buffer.</summary>
    internal static unsafe uint Update(uint adler, byte* buf, nuint len) =>
        Vector128.IsHardwareAccelerated ? UpdateVector(adler, buf, len) : UpdateScalar(adler, buf, len);

    /// <summary>Portable scalar implementation (mirrors zlib <c>adler32_z</c>).</summary>
    internal static unsafe uint UpdateScalar(uint adler, byte* buf, nuint len)
    {
        uint sum2 = (adler >> 16) & 0xFFFF;
        adler &= 0xFFFF;

        if (len == 1)
        {
            adler += buf[0];
            if (adler >= Base) adler -= Base;
            sum2 += adler;
            if (sum2 >= Base) sum2 -= Base;
            return adler | (sum2 << 16);
        }

        if (len < 16)
        {
            while (len-- != 0)
            {
                adler += *buf++;
                sum2 += adler;
            }
            if (adler >= Base) adler -= Base;
            sum2 %= Base;
            return adler | (sum2 << 16);
        }

        while (len >= Nmax)
        {
            len -= Nmax;
            uint n = Nmax / 16;
            do
            {
                Do16(ref adler, ref sum2, buf);
                buf += 16;
            }
            while (--n != 0);
            adler %= Base;
            sum2 %= Base;
        }

        if (len != 0)
        {
            while (len >= 16)
            {
                len -= 16;
                Do16(ref adler, ref sum2, buf);
                buf += 16;
            }
            while (len-- != 0)
            {
                adler += *buf++;
                sum2 += adler;
            }
            adler %= Base;
            sum2 %= Base;
        }

        return adler | (sum2 << 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void Do16(ref uint adler, ref uint sum2, byte* p)
    {
        adler += p[0]; sum2 += adler;
        adler += p[1]; sum2 += adler;
        adler += p[2]; sum2 += adler;
        adler += p[3]; sum2 += adler;
        adler += p[4]; sum2 += adler;
        adler += p[5]; sum2 += adler;
        adler += p[6]; sum2 += adler;
        adler += p[7]; sum2 += adler;
        adler += p[8]; sum2 += adler;
        adler += p[9]; sum2 += adler;
        adler += p[10]; sum2 += adler;
        adler += p[11]; sum2 += adler;
        adler += p[12]; sum2 += adler;
        adler += p[13]; sum2 += adler;
        adler += p[14]; sum2 += adler;
        adler += p[15]; sum2 += adler;
    }

    /// <summary>
    /// Generic-Vector128 implementation: the JIT lowers WidenLower/WidenUpper/Sum to NEON on
    /// arm64 and SSE2 on x86 from this one source — same pattern as compare256/slide_hash. Uses a
    /// non-fused widen+multiply+sum decomposition rather than NEON's fused pairwise-widening ops
    /// (which have no generic Vector128 equivalent), but is still byte-exact with the scalar Do16
    /// and, since the per-block dependency chain drops from 32 scalar steps to ~2-4, faster.
    /// </summary>
    internal static unsafe uint UpdateVector(uint adler, byte* buf, nuint len)
    {
        uint sum2 = (adler >> 16) & 0xFFFF;
        adler &= 0xFFFF;

        if (len == 1)
        {
            adler += buf[0];
            if (adler >= Base) adler -= Base;
            sum2 += adler;
            if (sum2 >= Base) sum2 -= Base;
            return adler | (sum2 << 16);
        }

        if (len < 16)
        {
            while (len-- != 0)
            {
                adler += *buf++;
                sum2 += adler;
            }
            if (adler >= Base) adler -= Base;
            sum2 %= Base;
            return adler | (sum2 << 16);
        }

        while (len >= Nmax)
        {
            len -= Nmax;
            uint n = Nmax / 16;
            do
            {
                Do16Vector(ref adler, ref sum2, buf);
                buf += 16;
            }
            while (--n != 0);
            adler %= Base;
            sum2 %= Base;
        }

        if (len != 0)
        {
            while (len >= 16)
            {
                len -= 16;
                Do16Vector(ref adler, ref sum2, buf);
                buf += 16;
            }
            while (len-- != 0)
            {
                adler += *buf++;
                sum2 += adler;
            }
            adler %= Base;
            sum2 %= Base;
        }

        return adler | (sum2 << 16);
    }

    // Position weights within a 16-byte block: byte p[0] contributes 16x to sum2, p[15] 1x —
    // matches the scalar Do16 unrolled (sum2 += adler after each of the 16 running-sum steps).
    // WidenLower(v) holds p0..p7, WidenUpper(v) holds p8..p15, so WHi/WLo weight those halves.
    private static Vector128<ushort> WHi => Vector128.Create((ushort)16, 15, 14, 13, 12, 11, 10, 9);
    private static Vector128<ushort> WLo => Vector128.Create((ushort)8, 7, 6, 5, 4, 3, 2, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void Do16Vector(ref uint adler, ref uint sum2, byte* p)
    {
        Vector128<byte> v = Vector128.Load(p);
        Vector128<ushort> lo = Vector128.WidenLower(v); // p0..p7
        Vector128<ushort> hi = Vector128.WidenUpper(v); // p8..p15

        // Both sums fit in ushort: max blockSum = 16*255 = 4080, max blockWeighted =
        // 255*(16+15+...+1) = 255*136 = 34680 (NMAX=5552=2^4*347, so 16 evenly divides every
        // batch — no tail handling needed at this granularity).
        uint blockSum = Vector128.Sum(lo + hi);
        uint blockWeighted = Vector128.Sum((lo * WHi) + (hi * WLo));

        // Uses the pre-update adler, matching the scalar Do16's "sum2 += adler" happening before
        // adler absorbs this block's contribution in the algebraic expansion.
        sum2 += 16u * adler + blockWeighted;
        adler += blockSum;
    }
}
