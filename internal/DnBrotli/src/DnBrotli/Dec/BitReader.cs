// Port of c/dec/bit_reader.{h,c} (brotli v1.1.0).
//
// Configuration choices, fixed for every dn2cpp target (PORTING.md):
//   - brotli_reg_t is nuint and BROTLI_64_BITS is true: only the 64-bit
//     branches of the C are ported.
//   - BROTLI_UNALIGNED_READ_FAST is true (Unsafe.ReadUnaligned is a plain
//     load), so BrotliWarmupBitReader's aligned_read_mask is always 0 — the
//     alignment loop is retained but never iterates.
//   - BROTLI_IS_CONSTANT cannot be expressed in C#; BrotliFillBitWindow
//     always takes the generic branch of the C (bit_pos_ <= 32 -> one 32-bit
//     load), which is what C compilers emit for non-constant n_bits. The
//     n_bits<=8 / n_bits<=16 56/48-bit fast paths are constant-folding
//     variants of the same contract and are intentionally not ported.
//   - BitMask uses the UBFX-style expression form; the kBrotliBitMask table
//     is retained for fidelity (it is exported in the C).

using System.Runtime.CompilerServices;
using DnBrotli.Common;
using DnBrotli.Internal;

namespace DnBrotli.Dec;

/// <summary><c>BrotliBitReader</c>, field-for-field.</summary>
internal unsafe struct BrotliBitReader
{
    public nuint val_;       /* pre-fetched bits */
    public nuint bit_pos_;   /* current bit-reading position in val_ */
    public byte* next_in;    /* the byte we're reading from */
    public byte* guard_in;   /* position from which "fast-path" is prohibited */
    public byte* last_in;    /* == next_in + avail_in */
}

/// <summary><c>BrotliBitReaderState</c>: the save/restore memento.</summary>
internal unsafe struct BrotliBitReaderState
{
    public nuint val_;
    public nuint bit_pos_;
    public byte* next_in;
    public nuint avail_in;
}

internal static unsafe class BitReader
{
    internal const int BROTLI_SHORT_FILL_BIT_WINDOW_READ = sizeof(ulong) >> 1;  /* sizeof(brotli_reg_t) >> 1 */

    /* 162 bits + 7 bytes */
    internal const int BROTLI_FAST_INPUT_SLACK = 28;

    internal static readonly nuint[] kBrotliBitMask =
    {
        0x00000000,
        0x00000001, 0x00000003, 0x00000007, 0x0000000F,
        0x0000001F, 0x0000003F, 0x0000007F, 0x000000FF,
        0x000001FF, 0x000003FF, 0x000007FF, 0x00000FFF,
        0x00001FFF, 0x00003FFF, 0x00007FFF, 0x0000FFFF,
        0x0001FFFF, 0x0003FFFF, 0x0007FFFF, 0x000FFFFF,
        0x001FFFFF, 0x003FFFFF, 0x007FFFFF, 0x00FFFFFF,
        0x01FFFFFF, 0x03FFFFFF, 0x07FFFFFF, 0x0FFFFFFF,
        0x1FFFFFFF, 0x3FFFFFFF, 0x7FFFFFFF, 0xFFFFFFFF,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BitMask(nuint n)
    {
        /* Masking with this expression turns to a single
           "Unsigned Bit Field Extract" UBFX instruction on ARM. */
        return ~(~(nuint)0 << (int)n);
    }

    /// <summary><c>BrotliInitBitReader</c>.</summary>
    internal static void BrotliInitBitReader(BrotliBitReader* br)
    {
        br->val_ = 0;
        br->bit_pos_ = 0;
    }

    /// <summary><c>BrotliWarmupBitReader</c>: ensures that the accumulator is not empty.
    /// May consume up to <c>sizeof(brotli_reg_t) - 1</c> bytes of input. Returns 0 if
    /// data is required but there is no input available.</summary>
    internal static int BrotliWarmupBitReader(BrotliBitReader* br)
    {
        nuint aligned_read_mask = (nuint)((sizeof(nuint) >> 1) - 1);
        /* Fixing alignment after unaligned BrotliFillWindow would result accumulator
           overflow. If unalignment is caused by BrotliSafeReadBits, then there is
           enough space in accumulator to fix alignment. */
        /* BROTLI_UNALIGNED_READ_FAST */
        aligned_read_mask = 0;
        if (BrotliGetAvailableBits(br) == 0)
        {
            br->val_ = 0;
            if (BrotliPullByte(br) == 0)
            {
                return 0;
            }
        }

        while (((nuint)br->next_in & aligned_read_mask) != 0)
        {
            if (BrotliPullByte(br) == 0)
            {
                /* If we consumed all the input, we don't care about the alignment. */
                return 1;
            }
        }
        return 1;
    }

    /// <summary><c>BrotliSafeReadBits32Slow</c>: fallback for
    /// <see cref="BrotliSafeReadBits32"/>. Never called for RFC brotli streams; required
    /// only for "large-window" mode and other extensions.</summary>
    internal static int BrotliSafeReadBits32Slow(BrotliBitReader* br, nuint n_bits, nuint* val)
    {
        nuint low_val;
        nuint high_val;
        BrotliBitReaderState memento;
        BrotliBitReaderSaveState(br, &memento);
        if (BrotliSafeReadBits(br, 16, &low_val) == 0 ||
            BrotliSafeReadBits(br, n_bits - 16, &high_val) == 0)
        {
            BrotliBitReaderRestoreState(br, &memento);
            return 0;
        }
        *val = low_val | (high_val << 16);
        return 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BrotliBitReaderGetAvailIn(BrotliBitReader* br)
    {
        return (nuint)(br->last_in - br->next_in);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliBitReaderSaveState(BrotliBitReader* from, BrotliBitReaderState* to)
    {
        to->val_ = from->val_;
        to->bit_pos_ = from->bit_pos_;
        to->next_in = from->next_in;
        to->avail_in = BrotliBitReaderGetAvailIn(from);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliBitReaderSetInput(BrotliBitReader* br, byte* next_in, nuint avail_in)
    {
        br->next_in = next_in;
        /* C: `(avail_in == 0) ? next_in : (next_in + avail_in)` — spelled as if/else
           because dn2cpp types the ternary arms differently (byte* vs the
           pointer-add's void*) and rejects the stack merge. */
        if (avail_in == 0)
        {
            br->last_in = next_in;
        }
        else
        {
            br->last_in = next_in + avail_in;
        }
        if (avail_in + 1 > BROTLI_FAST_INPUT_SLACK)
        {
            br->guard_in = next_in + (avail_in + 1 - BROTLI_FAST_INPUT_SLACK);
        }
        else
        {
            br->guard_in = next_in;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliBitReaderRestoreState(BrotliBitReader* to, BrotliBitReaderState* from)
    {
        to->val_ = from->val_;
        to->bit_pos_ = from->bit_pos_;
        to->next_in = from->next_in;
        BrotliBitReaderSetInput(to, from->next_in, from->avail_in);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BrotliGetAvailableBits(BrotliBitReader* br)
    {
        return br->bit_pos_;
    }

    /// <summary><c>BrotliGetRemainingBytes</c>: amount of unread bytes the bit reader still
    /// has buffered from the input, including whole bytes in <c>val_</c>. Result is capped
    /// with the maximal ring-buffer size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BrotliGetRemainingBytes(BrotliBitReader* br)
    {
        const nuint kCap = (nuint)1 << BrotliConstants.BROTLI_LARGE_MAX_WBITS;
        nuint avail_in = BrotliBitReaderGetAvailIn(br);
        if (avail_in > kCap) return kCap;
        return avail_in + (BrotliGetAvailableBits(br) >> 3);
    }

    /// <summary><c>BrotliCheckInputAmount</c>: checks if there is enough input for the
    /// fast path (excluding the bits remaining in <c>val_</c>).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BrotliCheckInputAmount(BrotliBitReader* br)
    {
        return (br->next_in < br->guard_in) ? 1 : 0;
    }

    /* Load more bits into accumulator. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BrotliBitReaderLoadBits(nuint val, nuint new_bits, nuint count, nuint offset)
    {
        _ = count;
        return val | (new_bits << (int)offset);
    }

    /// <summary><c>BrotliFillBitWindow</c>: guarantees that there are at least
    /// <c>n_bits + 1</c> bits in the accumulator. Precondition: the accumulator contains
    /// at least 1 bit. <c>n_bits</c> should be in the range [1..24].</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliFillBitWindow(BrotliBitReader* br, nuint n_bits)
    {
        /* BROTLI_64_BITS generic branch; see the header comment of this file. */
        _ = n_bits;
        nuint bit_pos = br->bit_pos_;
        if (bit_pos <= 32)
        {
            br->val_ = BrotliBitReaderLoadBits(br->val_,
                Bits.BROTLI_UNALIGNED_LOAD32LE(br->next_in), 32, bit_pos);
            br->bit_pos_ = bit_pos + 32;
            br->next_in += BROTLI_SHORT_FILL_BIT_WINDOW_READ;
        }
    }

    /// <summary><c>BrotliFillBitWindow16</c>: mostly like <see cref="BrotliFillBitWindow"/>,
    /// but guarantees only 16 bits and reads no more than
    /// <c>BROTLI_SHORT_FILL_BIT_WINDOW_READ</c> bytes of input.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliFillBitWindow16(BrotliBitReader* br)
    {
        BrotliFillBitWindow(br, 17);
    }

    /// <summary><c>BrotliPullByte</c>: tries to pull one byte of input to the accumulator.
    /// Returns 0 if there is no input available.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BrotliPullByte(BrotliBitReader* br)
    {
        if (br->next_in == br->last_in)
        {
            return 0;
        }
        br->val_ = BrotliBitReaderLoadBits(br->val_, *br->next_in, 8, br->bit_pos_);
        br->bit_pos_ += 8;
        ++br->next_in;
        return 1;
    }

    /// <summary><c>BrotliGetBitsUnmasked</c>: returns currently available bits. The number
    /// of valid bits could be calculated by <see cref="BrotliGetAvailableBits"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BrotliGetBitsUnmasked(BrotliBitReader* br)
    {
        return br->val_;
    }

    /// <summary><c>BrotliGet16BitsUnmasked</c>: like <see cref="BrotliGetBits"/>, but does
    /// not mask the result. The result contains at least 16 valid bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BrotliGet16BitsUnmasked(BrotliBitReader* br)
    {
        BrotliFillBitWindow(br, 16);
        return BrotliGetBitsUnmasked(br);
    }

    /// <summary><c>BrotliGetBits</c>: returns the specified number of bits without
    /// advancing the bit position.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BrotliGetBits(BrotliBitReader* br, nuint n_bits)
    {
        BrotliFillBitWindow(br, n_bits);
        return BrotliGetBitsUnmasked(br) & BitMask(n_bits);
    }

    /// <summary><c>BrotliSafeGetBits</c>: tries to peek the specified amount of bits.
    /// Returns 0 if there is not enough input.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BrotliSafeGetBits(BrotliBitReader* br, nuint n_bits, nuint* val)
    {
        while (BrotliGetAvailableBits(br) < n_bits)
        {
            if (BrotliPullByte(br) == 0)
            {
                return 0;
            }
        }
        *val = BrotliGetBitsUnmasked(br) & BitMask(n_bits);
        return 1;
    }

    /// <summary><c>BrotliDropBits</c>: advances the bit position by <c>n_bits</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliDropBits(BrotliBitReader* br, nuint n_bits)
    {
        br->bit_pos_ -= n_bits;
        br->val_ >>= (int)n_bits;
    }

    /* Make sure that there are no spectre bits in accumulator.
       This is important for the cases when some bytes are skipped
       (i.e. never placed into accumulator). */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliBitReaderNormalize(BrotliBitReader* br)
    {
        /* Actually, it is enough to normalize when br->bit_pos_ == 0 */
        if (br->bit_pos_ < (nuint)(sizeof(nuint) << 3))
        {
            br->val_ &= ((nuint)1 << (int)br->bit_pos_) - 1;
        }
    }

    /// <summary><c>BrotliBitReaderUnload</c>: puts back unused whole bytes from the
    /// accumulator into the input stream.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliBitReaderUnload(BrotliBitReader* br)
    {
        nuint unused_bytes = BrotliGetAvailableBits(br) >> 3;
        nuint unused_bits = unused_bytes << 3;
        /* C: `(unused_bytes == 0) ? br->next_in : (br->next_in - unused_bytes)` —
           spelled as if/else because dn2cpp types the ternary arms differently
           (byte* vs the pointer-sub's void*) and rejects the stack merge. */
        if (unused_bytes != 0)
        {
            br->next_in = br->next_in - unused_bytes;
        }
        br->bit_pos_ -= unused_bits;
        BrotliBitReaderNormalize(br);
    }

    /// <summary><c>BrotliTakeBits</c>: reads the specified number of bits and advances the
    /// bit position. Precondition: the accumulator MUST contain at least
    /// <c>n_bits</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliTakeBits(BrotliBitReader* br, nuint n_bits, nuint* val)
    {
        *val = BrotliGetBitsUnmasked(br) & BitMask(n_bits);
        BrotliDropBits(br, n_bits);
    }

    /// <summary><c>BrotliReadBits24</c>: reads the specified number of bits and advances
    /// the bit position. Assumes that there is enough input to perform
    /// <see cref="BrotliFillBitWindow"/>. Up to 24 bits are allowed to be requested.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BrotliReadBits24(BrotliBitReader* br, nuint n_bits)
    {
        /* BROTLI_64_BITS */
        nuint val;
        BrotliFillBitWindow(br, n_bits);
        BrotliTakeBits(br, n_bits, &val);
        return val;
    }

    /// <summary><c>BrotliReadBits32</c>: same as <see cref="BrotliReadBits24"/>, but allows
    /// reading up to 32 bits (the large-window variant).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BrotliReadBits32(BrotliBitReader* br, nuint n_bits)
    {
        /* BROTLI_64_BITS */
        nuint val;
        BrotliFillBitWindow(br, n_bits);
        BrotliTakeBits(br, n_bits, &val);
        return val;
    }

    /// <summary><c>BrotliSafeReadBits</c>: tries to read the specified amount of bits.
    /// Returns 0 if there is not enough input. <c>n_bits</c> MUST be positive; up to 24
    /// bits are allowed to be requested.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BrotliSafeReadBits(BrotliBitReader* br, nuint n_bits, nuint* val)
    {
        while (BrotliGetAvailableBits(br) < n_bits)
        {
            if (BrotliPullByte(br) == 0)
            {
                return 0;
            }
        }
        BrotliTakeBits(br, n_bits, val);
        return 1;
    }

    /// <summary><c>BrotliSafeReadBits32</c>: same as <see cref="BrotliSafeReadBits"/>, but
    /// allows reading up to 32 bits (the large-window variant).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BrotliSafeReadBits32(BrotliBitReader* br, nuint n_bits, nuint* val)
    {
        /* BROTLI_64_BITS */
        while (BrotliGetAvailableBits(br) < n_bits)
        {
            if (BrotliPullByte(br) == 0)
            {
                return 0;
            }
        }
        BrotliTakeBits(br, n_bits, val);
        return 1;
    }

    /// <summary><c>BrotliJumpToByteBoundary</c>: advances the bit reader position to the
    /// next byte boundary and verifies that any skipped bits are set to zero.</summary>
    internal static int BrotliJumpToByteBoundary(BrotliBitReader* br)
    {
        nuint pad_bits_count = BrotliGetAvailableBits(br) & 0x7;
        nuint pad_bits = 0;
        if (pad_bits_count != 0)
        {
            BrotliTakeBits(br, pad_bits_count, &pad_bits);
        }
        BrotliBitReaderNormalize(br);
        return (pad_bits == 0) ? 1 : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BrotliDropBytes(BrotliBitReader* br, nuint num)
    {
        /* Detour is only legal when the accumulator is empty
           (bit_pos_ == 0 && val_ == 0). */
        br->next_in += num;
    }

    /// <summary><c>BrotliCopyBytes</c>: copies remaining input bytes stored in the bit
    /// reader to the output. Value <c>num</c> may not be larger than
    /// <see cref="BrotliGetRemainingBytes"/>. The bit reader must be warmed up again
    /// after this.</summary>
    internal static void BrotliCopyBytes(byte* dest, BrotliBitReader* br, nuint num)
    {
        while (BrotliGetAvailableBits(br) >= 8 && num > 0)
        {
            *dest = (byte)BrotliGetBitsUnmasked(br);
            BrotliDropBits(br, 8);
            ++dest;
            --num;
        }
        BrotliBitReaderNormalize(br);
        if (num > 0)
        {
            Buffer.MemoryCopy(br->next_in, dest, num, num);
            BrotliDropBytes(br, num);
        }
    }
}
