// Port of c/enc/command.{h,c} (brotli v1.1.0): a sequence of literals plus a
// backward reference copy. The static helpers live on the Command struct
// itself (imported via `using static DnBrotli.Enc.Command`).

using System.Runtime.CompilerServices;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

/// <summary><c>struct Command</c>.</summary>
internal unsafe struct Command
{
    public uint insert_len_;
    /* Stores copy_len in low 25 bits and copy_code - copy_len in high 7 bit. */
    public uint copy_len_;
    /* Stores distance extra bits. */
    public uint dist_extra_;
    public ushort cmd_prefix_;
    /* Stores distance code in low 10 bits
       and number of extra bits in high 6 bits. */
    public ushort dist_prefix_;

    /* ==================== command.c data ==================== */

    internal static readonly uint[] kBrotliInsBase =  /* [BROTLI_NUM_INS_COPY_CODES] */
    {
        0,  1,  2,  3,  4,   5,   6,   8,   10,   14,   18,   26,
        34, 50, 66, 98, 130, 194, 322, 578, 1090, 2114, 6210, 22594,
    };
    internal static readonly uint[] kBrotliInsExtra =  /* [BROTLI_NUM_INS_COPY_CODES] */
    {
        0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 7, 8, 9, 10, 12, 14, 24,
    };
    internal static readonly uint[] kBrotliCopyBase =  /* [BROTLI_NUM_INS_COPY_CODES] */
    {
        2,  3,  4,  5,  6,  7,   8,   9,   10,  12,  14,   18,
        22, 30, 38, 54, 70, 102, 134, 198, 326, 582, 1094, 2118,
    };
    internal static readonly uint[] kBrotliCopyExtra =  /* [BROTLI_NUM_INS_COPY_CODES] */
    {
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 7, 8, 9, 10, 24,
    };

    /* ==================== command.h functions ==================== */

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort GetInsertLengthCode(nuint insertlen)
    {
        if (insertlen < 6)
        {
            return (ushort)insertlen;
        }
        else if (insertlen < 130)
        {
            uint nbits = Log2FloorNonZero(insertlen - 2) - 1u;
            return (ushort)((nbits << 1) + ((insertlen - 2) >> (int)nbits) + 2);
        }
        else if (insertlen < 2114)
        {
            return (ushort)(Log2FloorNonZero(insertlen - 66) + 10);
        }
        else if (insertlen < 6210)
        {
            return 21;
        }
        else if (insertlen < 22594)
        {
            return 22;
        }
        else
        {
            return 23;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort GetCopyLengthCode(nuint copylen)
    {
        if (copylen < 10)
        {
            return (ushort)(copylen - 2);
        }
        else if (copylen < 134)
        {
            uint nbits = Log2FloorNonZero(copylen - 6) - 1u;
            return (ushort)((nbits << 1) + ((copylen - 6) >> (int)nbits) + 4);
        }
        else if (copylen < 2118)
        {
            return (ushort)(Log2FloorNonZero(copylen - 70) + 12);
        }
        else
        {
            return 23;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort CombineLengthCodes(
        ushort inscode, ushort copycode, int use_last_distance)
    {
        ushort bits64 =
            (ushort)((copycode & 0x7u) | ((inscode & 0x7u) << 3));
        if (use_last_distance != 0 && inscode < 8u && copycode < 16u)
        {
            return (copycode < 8u) ? bits64 : (ushort)(bits64 | 64u);
        }
        else
        {
            /* Specification: 5 Encoding of ... (last table) */
            /* offset = 2 * index, where index is in range [0..8] */
            uint offset = 2u * (((uint)copycode >> 3) + 3u * ((uint)inscode >> 3));
            /* All values in specification are K * 64,
               where   K = [2, 3, 6, 4, 5, 8, 7, 9, 10],
                   i + 1 = [1, 2, 3, 4, 5, 6, 7, 8,  9],
               K - i - 1 = [1, 1, 3, 0, 0, 2, 0, 1,  2] = D.
               All values in D require only 2 bits to encode.
               Magic constant is shifted 6 bits left, to avoid final multiplication. */
            offset = (offset << 5) + 0x40u + ((0x520D40u >> (int)offset) & 0xC0u);
            return (ushort)(offset | bits64);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void GetLengthCode(nuint insertlen, nuint copylen,
                                       int use_last_distance,
                                       ushort* code)
    {
        ushort inscode = GetInsertLengthCode(insertlen);
        ushort copycode = GetCopyLengthCode(copylen);
        *code = CombineLengthCodes(inscode, copycode, use_last_distance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint GetInsertBase(ushort inscode)
    {
        return kBrotliInsBase[inscode];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint GetInsertExtra(ushort inscode)
    {
        return kBrotliInsExtra[inscode];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint GetCopyBase(ushort copycode)
    {
        return kBrotliCopyBase[copycode];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint GetCopyExtra(ushort copycode)
    {
        return kBrotliCopyExtra[copycode];
    }

    /* distance_code is e.g. 0 for same-as-last short code, or 16 for offset 1. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void InitCommand(Command* self,
        BrotliDistanceParams* dist, nuint insertlen,
        nuint copylen, int copylen_code_delta, nuint distance_code)
    {
        /* Don't rely on signed int representation, use honest casts. */
        uint delta = (byte)((sbyte)copylen_code_delta);
        self->insert_len_ = (uint)insertlen;
        self->copy_len_ = (uint)(copylen | ((nuint)delta << 25));
        /* The distance prefix and extra bits are stored in this Command as if
           npostfix and ndirect were 0, they are only recomputed later after the
           clustering if needed. */
        /* dn2cpp: written through zero-initialized locals, never `&self->field` —
           the backend widens sub-int32 struct fields to int32, so a 16-bit store
           through the field's address would leave the slot's upper bytes as
           whatever the (non-zeroing) allocator handed out. */
        ushort dist_prefix = 0;
        Prefix.PrefixEncodeCopyDistance(
            distance_code, dist->num_direct_distance_codes,
            dist->distance_postfix_bits, &dist_prefix, &self->dist_extra_);
        self->dist_prefix_ = dist_prefix;
        ushort cmd_prefix = 0;
        GetLengthCode(
            insertlen, (nuint)((int)copylen + copylen_code_delta),
            (self->dist_prefix_ & 0x3FF) == 0 ? 1 : 0, &cmd_prefix);
        self->cmd_prefix_ = cmd_prefix;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void InitInsertCommand(Command* self, nuint insertlen)
    {
        self->insert_len_ = (uint)insertlen;
        self->copy_len_ = 4 << 25;
        self->dist_extra_ = 0;
        self->dist_prefix_ = BROTLI_NUM_DISTANCE_SHORT_CODES;
        /* dn2cpp: local instead of `&self->cmd_prefix_` — see InitCommand. */
        ushort cmd_prefix = 0;
        GetLengthCode(insertlen, 4, 0, &cmd_prefix);
        self->cmd_prefix_ = cmd_prefix;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint CommandRestoreDistanceCode(
        Command* self, BrotliDistanceParams* dist)
    {
        if ((self->dist_prefix_ & 0x3FFu) <
            BROTLI_NUM_DISTANCE_SHORT_CODES + dist->num_direct_distance_codes)
        {
            return self->dist_prefix_ & 0x3FFu;
        }
        else
        {
            uint dcode = self->dist_prefix_ & 0x3FFu;
            uint nbits = (uint)self->dist_prefix_ >> 10;
            uint extra = self->dist_extra_;
            uint postfix_mask = (1U << (int)dist->distance_postfix_bits) - 1U;
            uint hcode = (dcode - dist->num_direct_distance_codes -
                BROTLI_NUM_DISTANCE_SHORT_CODES) >>
                (int)dist->distance_postfix_bits;
            uint lcode = (dcode - dist->num_direct_distance_codes -
                BROTLI_NUM_DISTANCE_SHORT_CODES) & postfix_mask;
            uint offset = ((2U + (hcode & 1U)) << (int)nbits) - 4U;
            return ((offset + extra) << (int)dist->distance_postfix_bits) + lcode +
                dist->num_direct_distance_codes + BROTLI_NUM_DISTANCE_SHORT_CODES;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint CommandDistanceContext(Command* self)
    {
        uint r = (uint)self->cmd_prefix_ >> 6;
        uint c = (uint)self->cmd_prefix_ & 7;
        if ((r == 0 || r == 2 || r == 4 || r == 7) && (c <= 2))
        {
            return c;
        }
        return 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint CommandCopyLen(Command* self)
    {
        return self->copy_len_ & 0x1FFFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint CommandCopyLenCode(Command* self)
    {
        uint modifier = self->copy_len_ >> 25;
        int delta = (sbyte)((byte)(modifier | ((modifier & 0x40) << 1)));
        return (uint)((int)(self->copy_len_ & 0x1FFFFFF) + delta);
    }
}
