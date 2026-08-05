using System.Runtime.Intrinsics;

namespace DnZlib.Deflate;

/// <summary>
/// Rebases a hash table when the window slides: every entry has <c>wsize</c> subtracted, saturating
/// at 0. Uses the cross-platform <see cref="Vector128"/> saturating-subtract, which the JIT lowers to
/// NEON <c>uqsub</c> on arm64 and SSE2 <c>psubusw</c> on x86 — one implementation, both ISAs. Produces
/// exactly the same table as the scalar <c>m &gt;= wsize ? m - wsize : 0</c>.
/// </summary>
internal static unsafe class SlideHashHelper
{
    public static void Slide(ushort* table, uint n, ushort wsize)
    {
        if (Vector128.IsHardwareAccelerated)
            SlideVector(table, n, wsize);
        else
            SlideScalar(table, n, wsize);
    }

    private static void SlideScalar(ushort* table, uint n, ushort wsize)
    {
        ushort* p = table + n;
        do
        {
            ushort m = *--p;
            *p = (ushort)(m >= wsize ? m - wsize : 0);
        }
        while (--n != 0);
    }

    // Hash/prev tables are power-of-two sized (>= 256), so n is always a multiple of 8; the scalar
    // tail only guards against a hypothetical odd remainder.
    private static void SlideVector(ushort* table, uint n, ushort wsize)
    {
        Vector128<ushort> v = Vector128.Create(wsize);
        uint vecEnd = n & ~7u;   // 8 ushorts (128 bits) per iteration
        uint i = 0;
        for (; i < vecEnd; i += 8)
            Vector128.Store(Vector128.SubtractSaturate(Vector128.Load(table + i), v), table + i);
        for (; i < n; i++)
        {
            ushort m = table[i];
            table[i] = (ushort)(m >= wsize ? m - wsize : 0);
        }
    }
}
