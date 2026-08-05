// Port of c/enc/find_match_length.h (brotli v1.1.0): maximal matching prefix
// of two strings. The BROTLI_TZCNT64 && BROTLI_64_BITS && BROTLI_LITTLE_ENDIAN
// branch is the scalar port (every dn2cpp target). Per the PORTING.md SIMD
// policy an architecture-generic Vector128 fast path is layered on top, gated
// on Vector128.IsHardwareAccelerated with the byte-exact scalar body as the
// fallback — both paths return identical lengths (asserted by
// EncoderGreedyPathTests.FindMatchLength_VectorAndScalar_Agree).

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

internal static unsafe class FindMatchLength
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint FindMatchLengthWithLimit(byte* s1, byte* s2, nuint limit)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return FindMatchLengthWithLimitVector128(s1, s2, limit);
        }
        return FindMatchLengthWithLimitScalar(s1, s2, limit);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint FindMatchLengthWithLimitScalar(byte* s1, byte* s2, nuint limit)
    {
        byte* s1_orig = s1;
        for (; limit >= 8; limit -= 8)
        {
            ulong x = BROTLI_UNALIGNED_LOAD64LE(s2) ^
                      BROTLI_UNALIGNED_LOAD64LE(s1);
            s2 += 8;
            if (x != 0)
            {
                nuint matching_bits = BROTLI_TZCNT64(x);
                return (nuint)(s1 - s1_orig) + (matching_bits >> 3);
            }
            s1 += 8;
        }
        while (limit != 0 && *s1 == *s2)
        {
            limit--;
            ++s2;
            ++s1;
        }
        return (nuint)(s1 - s1_orig);
    }

    /// <summary>Vector128 fast path: identical result to the scalar body. Reads
    /// only within [s, s + limit) rounded down to whole 16-byte blocks, then
    /// hands the tail to the scalar loop (which itself reads at most 8-byte
    /// blocks inside the limit) — no wider access than the C original.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint FindMatchLengthWithLimitVector128(byte* s1, byte* s2, nuint limit)
    {
        byte* s1_orig = s1;
        for (; limit >= 16; limit -= 16)
        {
            uint mismatch = ~Vector128.Equals(
                Vector128.Load(s1), Vector128.Load(s2)).ExtractMostSignificantBits()
                & 0xFFFFu;
            if (mismatch != 0)
            {
                return (nuint)(s1 - s1_orig) +
                    (uint)System.Numerics.BitOperations.TrailingZeroCount(mismatch);
            }
            s1 += 16;
            s2 += 16;
        }
        return (nuint)(s1 - s1_orig) + FindMatchLengthWithLimitScalar(s1, s2, limit);
    }
}
