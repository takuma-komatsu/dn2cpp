using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace DnZlib.Inflate;

/// <summary>
/// Vectorized LZ77 match-copy helpers for the inflate fast path — the decompression-side analogue
/// of zlib-ng's chunkcopy_safe/chunkmemset_safe. Every copy is byte-exact with the forward
/// byte-by-byte loop it replaces (LZ77 "copy from earlier in the output" semantics: dist &lt; len
/// repeats the pattern, e.g. dist=1 is RLE — this is NOT memmove semantics). The vector paths may
/// overshoot past the logical end of a copy by up to ChunkSize-1 bytes, so every call is bounded
/// by <paramref name="safeEnd"/> — the true end of the destination buffer for this inflate() call —
/// rather than trusting the caller's 258-byte slack, which is not enough once a single match is
/// split across multiple segments (see the wnext-wrap cases in InfFast.cs).
/// </summary>
internal static unsafe class ChunkCopyHelper
{
    private const int ChunkSize = 16; // sizeof(Vector128<byte>) — fixed width on every ISA.

    /// <summary>
    /// dst &lt;- src, len bytes, where src/dst never alias within a ChunkSize-sized load/store —
    /// true when src is the sliding window (a distinct allocation) or, for a same-buffer call,
    /// when the caller has already established dist &gt;= len or dist &gt;= ChunkSize. Never reads or
    /// writes at or past <paramref name="safeEnd"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte* CopyDisjoint(byte* dst, byte* src, uint len, byte* safeEnd)
    {
        if (len == 0)
            return dst;

        if (Vector128.IsHardwareAccelerated && safeEnd - dst >= ChunkSize)
            return CopyDisjointVector(dst, src, len);

        // Safe here (unlike CopyOverlapping's scalar fallback below): src/dst are guaranteed
        // non-aliasing by this method's contract, so a plain memcpy is equivalent to the
        // byte-by-byte loop it replaces.
        Unsafe.CopyBlockUnaligned(dst, src, len);
        return dst + len;
    }

    private static byte* CopyDisjointVector(byte* dst, byte* src, uint len)
    {
        if (len <= ChunkSize)
        {
            // Single (possibly overshooting) store; total footprint bounded to ChunkSize
            // regardless of len — safe since the caller already checked safeEnd - dst >= ChunkSize.
            Vector128.Store(Vector128.Load(src), dst);
            return dst + len;
        }

        uint done = 0;
        while (done + ChunkSize < len)
        {
            Vector128.Store(Vector128.Load(src + done), dst + done);
            done += ChunkSize;
        }
        // Final chunk pulled back to end exactly at len: zero overshoot once len >= ChunkSize.
        Vector128.Store(Vector128.Load(src + (len - ChunkSize)), dst + (len - ChunkSize));
        return dst + len;
    }

    /// <summary>
    /// dst &lt;- (dst - dist), len bytes — LZ77's "copy from earlier in the same output" with
    /// forward-propagation semantics. Clamped against <paramref name="safeEnd"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte* CopyOverlapping(byte* dst, uint dist, uint len, byte* safeEnd)
    {
        if (len == 0)
            return dst;

        // dist >= len: source range ends at/before dst, no overlap at all — CopyDisjoint is
        // safe for any code path.
        // dist >= ChunkSize: a forward *chunked* copy never reads a not-yet-written position
        // (the classic "safe once the gap >= chunk size" rule) — but that safety is specific to
        // CopyDisjointVector's fixed ChunkSize-wide forward stride. CopyDisjoint's scalar
        // fallback is a plain memcpy (Unsafe.CopyBlockUnaligned) with no overlap guarantee, so
        // routing a still-overlapping region (dist < len) through it when the vector path is
        // unavailable corrupts the forward-propagation. Gate the shortcut on hardware
        // acceleration: without it, every dist < len falls through to the RLE-correct scalar
        // loop below (which handles any dist, not just dist < ChunkSize). When hardware
        // acceleration is present and dist >= ChunkSize with dist < len, len > ChunkSize and the
        // copy fits, so CopyDisjoint always takes its vector path — no overlap ever reaches the
        // memcpy fallback.
        if (dist >= len || (Vector128.IsHardwareAccelerated && dist >= ChunkSize))
            return CopyDisjoint(dst, dst - dist, len, safeEnd);

        // 0 < dist < min(len, ChunkSize): genuine run-length-style overlap (e.g. dist=1 is RLE).
        // CopyDisjoint must NOT be used as a fallback here — it does one dist-wide shift-copy, it
        // does not repeat the pattern, so the result would be wrong, not just unvectorized. Without
        // enough safeEnd headroom for the doubling trick below, fall back to a genuinely correct
        // scalar forward loop instead.
        if (!Vector128.IsHardwareAccelerated || safeEnd - dst < 2 * ChunkSize)
        {
            // NOTE: written as src[i], not dst[i - dist] — i and dist are both uint, so i - dist
            // underflows to a huge value whenever i < dist (always true on the first iteration).
            byte* src = dst - dist;
            for (uint i = 0; i < len; i++)
                dst[i] = src[i];
            return dst + len;
        }

        // Double the covered distance each step (zlib-ng's CHUNKUNROLL trick) until it reaches
        // ChunkSize or covers the rest of len, then finish with a disjoint copy at the now-widened
        // distance. Each step's store starts before the final write cursor and is ChunkSize wide,
        // so the total footprint from the loop's starting dst never exceeds 2*ChunkSize — covered
        // by the headroom check above.
        uint d = dist;
        while (d < len && d < ChunkSize)
        {
            Vector128.Store(Vector128.Load(dst - d), dst);
            dst += d;
            len -= d;
            d += d;
        }
        return CopyDisjoint(dst, dst - d, len, safeEnd);
    }
}
