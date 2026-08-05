// Port of c/enc/write_bits.h (brotli v1.1.0).
//
// Only the BROTLI_LITTLE_ENDIAN branch is ported (PORTING.md: every dn2cpp
// target is little-endian, asserted in Internal.Bits): a single unaligned
// 64-bit store per call, exactly like the C.

using System.Runtime.CompilerServices;

using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

internal static unsafe class WriteBits
{
    /* This function writes bits into bytes in increasing addresses, and within
       a byte least-significant-bit first.

       The function can write up to 56 bits in one go. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliWriteBits(nuint n_bits, ulong bits, nuint* pos, byte* array)
    {
        /* This branch of the code can write up to 56 bits at a time,
           7 bits are lost by being perhaps already in *p and at least
           1 bit is needed to initialize the bit-stream ahead (i.e. if 7
           bits are in *p and we write 57 bits, then the next write will
           access a byte that was never initialized). */
        {
            byte* p = &array[*pos >> 3];
            ulong v = (ulong)(*p);  /* Zero-extend 8 to 64 bits. */
            v |= bits << (int)(*pos & 7);
            BROTLI_UNALIGNED_STORE64LE(p, v);  /* Set some bits. */
            *pos += n_bits;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliWriteBitsPrepareStorage(nuint pos, byte* array)
    {
        array[pos >> 3] = 0;
    }
}
