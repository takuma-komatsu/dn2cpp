// Port of c/enc/prefix.h (brotli v1.1.0) — the encoder-side prefix coding
// helpers (the decoder side lives in Dec/Prefix.cs).

using System.Runtime.CompilerServices;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

internal static unsafe class Prefix
{
    /* Here distance_code is an intermediate code, i.e. one of the special codes or
       the actual distance increased by BROTLI_NUM_DISTANCE_SHORT_CODES - 1. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void PrefixEncodeCopyDistance(nuint distance_code,
                                                  nuint num_direct_codes,
                                                  nuint postfix_bits,
                                                  ushort* code,
                                                  uint* extra_bits)
    {
        if (distance_code < BROTLI_NUM_DISTANCE_SHORT_CODES + num_direct_codes)
        {
            *code = (ushort)distance_code;
            *extra_bits = 0;
            return;
        }
        else
        {
            nuint dist = ((nuint)1 << (int)(postfix_bits + 2u)) +
                (distance_code - BROTLI_NUM_DISTANCE_SHORT_CODES - num_direct_codes);
            nuint bucket = Log2FloorNonZero(dist) - 1;
            nuint postfix_mask = (1u << (int)postfix_bits) - 1;
            nuint postfix = dist & postfix_mask;
            nuint prefix = (dist >> (int)bucket) & 1;
            nuint offset = (2 + prefix) << (int)bucket;
            nuint nbits = bucket - postfix_bits;
            *code = (ushort)((nbits << 10) |
                ((nuint)BROTLI_NUM_DISTANCE_SHORT_CODES + num_direct_codes +
                 ((2 * (nbits - 1) + prefix) << (int)postfix_bits) + postfix));
            *extra_bits = (uint)((dist - offset) >> (int)postfix_bits);
        }
    }
}
