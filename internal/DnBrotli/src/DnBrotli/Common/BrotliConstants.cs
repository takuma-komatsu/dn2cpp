namespace DnBrotli.Common;

/// <summary>Shared brotli constants. Values mirror <c>c/include/brotli/encode.h</c>,
/// <c>c/include/brotli/decode.h</c> and <c>c/common/constants.h</c> exactly.</summary>
internal static class BrotliConstants
{
    /// <summary><c>BROTLI_VERSION</c> of the reference implementation this port tracks
    /// (1.1.0, encoded as <c>(major &lt;&lt; 24) | (minor &lt;&lt; 12) | patch</c>).</summary>
    internal const uint Version = (1u << 24) | (1u << 12) | 0u;

    internal const int MinWindowBits = 10;   // BROTLI_MIN_WINDOW_BITS
    internal const int MaxWindowBits = 24;   // BROTLI_MAX_WINDOW_BITS
    internal const int LargeMaxWindowBits = 30; // BROTLI_LARGE_MAX_WINDOW_BITS
    internal const int MinInputBlockBits = 16;  // BROTLI_MIN_INPUT_BLOCK_BITS
    internal const int MaxInputBlockBits = 24;  // BROTLI_MAX_INPUT_BLOCK_BITS

    internal const int MinQuality = 0;       // BROTLI_MIN_QUALITY
    internal const int MaxQuality = 11;      // BROTLI_MAX_QUALITY
    internal const int DefaultQuality = 11;  // BROTLI_DEFAULT_QUALITY
    internal const int DefaultWindow = 22;   // BROTLI_DEFAULT_WINDOW

    /// <summary><c>BROTLI_WINDOW_GAP</c>: the invalid backward-distance slack between the
    /// window size and the maximum usable distance.</summary>
    internal const int WindowGap = 16;

    /// <summary><c>BROTLI_MAX_BACKWARD_LIMIT(W)</c>: largest usable backward distance for
    /// window bits <paramref name="windowBits"/>.</summary>
    internal static nuint MaxBackwardLimit(int windowBits)
    {
        return ((nuint)1 << windowBits) - WindowGap;
    }

    // The remainder of c/common/constants.h, kept under the exact C names
    // (PORTING.md): the decoder/encoder engine ports reference these verbatim.

    /* Specification: 7.3. Encoding of the context map */
    internal const int BROTLI_CONTEXT_MAP_MAX_RLE = 16;

    /* Specification: 2. Compressed representation overview */
    internal const int BROTLI_MAX_NUMBER_OF_BLOCK_TYPES = 256;

    /* Specification: 3.3. Alphabet sizes: insert-and-copy length */
    internal const int BROTLI_NUM_LITERAL_SYMBOLS = 256;
    internal const int BROTLI_NUM_COMMAND_SYMBOLS = 704;
    internal const int BROTLI_NUM_BLOCK_LEN_SYMBOLS = 26;
    internal const int BROTLI_MAX_CONTEXT_MAP_SYMBOLS =
        BROTLI_MAX_NUMBER_OF_BLOCK_TYPES + BROTLI_CONTEXT_MAP_MAX_RLE;
    internal const int BROTLI_MAX_BLOCK_TYPE_SYMBOLS = BROTLI_MAX_NUMBER_OF_BLOCK_TYPES + 2;

    /* Specification: 3.5. Complex prefix codes */
    internal const int BROTLI_REPEAT_PREVIOUS_CODE_LENGTH = 16;
    internal const int BROTLI_REPEAT_ZERO_CODE_LENGTH = 17;
    internal const int BROTLI_CODE_LENGTH_CODES = BROTLI_REPEAT_ZERO_CODE_LENGTH + 1;
    /* "code length of 8 is repeated" */
    internal const int BROTLI_INITIAL_REPEATED_CODE_LENGTH = 8;

    /* "Large Window Brotli" */
    internal const uint BROTLI_LARGE_MAX_DISTANCE_BITS = 62;
    internal const int BROTLI_LARGE_MIN_WBITS = 10;
    internal const int BROTLI_LARGE_MAX_WBITS = 30;

    /* Specification: 4. Encoding of distances */
    internal const int BROTLI_NUM_DISTANCE_SHORT_CODES = 16;
    internal const int BROTLI_MAX_NPOSTFIX = 3;
    internal const int BROTLI_MAX_NDIRECT = 120;
    internal const uint BROTLI_MAX_DISTANCE_BITS = 24;

    /// <summary><c>BROTLI_DISTANCE_ALPHABET_SIZE(NPOSTFIX, NDIRECT, MAXNBITS)</c>.</summary>
    internal static uint BROTLI_DISTANCE_ALPHABET_SIZE(uint npostfix, uint ndirect, uint maxnbits)
    {
        return BROTLI_NUM_DISTANCE_SHORT_CODES + ndirect + (maxnbits << (int)(npostfix + 1));
    }

    /* BROTLI_NUM_DISTANCE_SYMBOLS ==
       BROTLI_DISTANCE_ALPHABET_SIZE(BROTLI_MAX_NDIRECT, BROTLI_MAX_NPOSTFIX,
                                     BROTLI_LARGE_MAX_DISTANCE_BITS) == 1128
       NB: the C macro invocation swaps the NPOSTFIX/NDIRECT argument names, exactly
       as reproduced here. */
    internal const int BROTLI_NUM_DISTANCE_SYMBOLS = 1128;

    /* ((1 << 26) - 4) is the maximal distance that can be expressed in RFC 7932
       brotli stream using NPOSTFIX = 0 and NDIRECT = 0. With other NPOSTFIX and
       NDIRECT values distances up to ((1 << 29) + 88) could be expressed. */
    internal const int BROTLI_MAX_DISTANCE = 0x3FFFFFC;

    /* ((1 << 31) - 4) is the safe distance limit. Using this number as a limit
       allows safe distance calculation without overflows, given the distance
       alphabet size is limited to corresponding size. */
    internal const int BROTLI_MAX_ALLOWED_DISTANCE = 0x7FFFFFFC;

    /* Specification: 4. Encoding of Literal Insertion Lengths and Copy Lengths */
    internal const int BROTLI_NUM_INS_COPY_CODES = 24;

    /* 7.1. Context modes and context ID lookup for literals */
    /* "context IDs for literals are in the range of 0..63" */
    internal const int BROTLI_LITERAL_CONTEXT_BITS = 6;

    /* 7.2. Context ID for distances */
    internal const int BROTLI_DISTANCE_CONTEXT_BITS = 2;

    /// <summary><c>BrotliCalculateDistanceCodeLimit</c> from <c>c/common/constants.h</c>:
    /// calculates the maximal size of the distance alphabet such that distances greater
    /// than <paramref name="max_distance"/> can not be represented (used by the decoder's
    /// "Large Window Brotli" mode).</summary>
    internal static BrotliDistanceCodeLimit BrotliCalculateDistanceCodeLimit(
        uint max_distance, uint npostfix, uint ndirect)
    {
        BrotliDistanceCodeLimit result;
        if (max_distance <= ndirect)
        {
            /* This case never happens / exists only for the sake of completeness. */
            result.max_alphabet_size = max_distance + BROTLI_NUM_DISTANCE_SHORT_CODES;
            result.max_distance = max_distance;
            return result;
        }
        else
        {
            /* The first prohibited value. */
            uint forbidden_distance = max_distance + 1;
            /* Subtract "directly" encoded region. */
            uint offset = forbidden_distance - ndirect - 1;
            uint ndistbits = 0;
            uint tmp;
            uint half;
            uint group;
            /* Postfix for the last dcode in the group. */
            uint postfix = (1u << (int)npostfix) - 1;
            uint extra;
            uint start;
            /* Remove postfix and "head-start". */
            offset = (offset >> (int)npostfix) + 4;
            /* Calculate the number of distance bits. */
            tmp = offset / 2;
            /* Poor-man's log2floor, to avoid extra dependencies. */
            while (tmp != 0) { ndistbits++; tmp = tmp >> 1; }
            /* One bit is covered with subrange addressing ("half"). */
            ndistbits--;
            /* Find subrange. */
            half = (offset >> (int)ndistbits) & 1;
            /* Calculate the "group" part of dcode. */
            group = ((ndistbits - 1) << 1) | half;
            /* Calculated "group" covers the prohibited distance value. */
            if (group == 0)
            {
                /* This case is added for correctness; does not occur for limit > 128. */
                result.max_alphabet_size = ndirect + BROTLI_NUM_DISTANCE_SHORT_CODES;
                result.max_distance = ndirect;
                return result;
            }
            /* Decrement "group", so it is the last permitted "group". */
            group--;
            /* After group was decremented, ndistbits and half must be recalculated. */
            ndistbits = (group >> 1) + 1;
            /* The last available distance in the subrange has all extra bits set. */
            extra = (1u << (int)ndistbits) - 1;
            /* Calculate region start. NB: ndistbits >= 1. */
            start = (1u << (int)(ndistbits + 1)) - 4;
            /* Move to subregion. */
            start += (group & 1) << (int)ndistbits;
            /* Calculate the alphabet size. */
            result.max_alphabet_size = ((group << (int)npostfix) | postfix) + ndirect +
                BROTLI_NUM_DISTANCE_SHORT_CODES + 1;
            /* Calculate the maximal distance representable by alphabet. */
            result.max_distance = ((start + extra) << (int)npostfix) + postfix + ndirect + 1;
            return result;
        }
    }

    /// <summary><c>_kBrotliPrefixCodeRanges[BROTLI_NUM_BLOCK_LEN_SYMBOLS]</c> from
    /// <c>c/common/constants.c</c> — the block-length prefix code (in pre-1.1 brotli this
    /// table was <c>kBlockLengthPrefixCode</c> in <c>c/dec/prefix.h</c>).</summary>
    internal static readonly BrotliPrefixCodeRange[] _kBrotliPrefixCodeRanges =
    {
        new(1, 2),     new(5, 2),     new(9, 2),   new(13, 2),    new(17, 3),    new(25, 3),
        new(33, 3),    new(41, 3),    new(49, 4),  new(65, 4),    new(81, 4),    new(97, 4),
        new(113, 5),   new(145, 5),   new(177, 5), new(209, 5),   new(241, 6),   new(305, 6),
        new(369, 7),   new(497, 8),   new(753, 9), new(1265, 10), new(2289, 11), new(4337, 12),
        new(8433, 13), new(16625, 24),
    };
}

/// <summary><c>BrotliDistanceCodeLimit</c> from <c>c/common/constants.h</c>.</summary>
internal struct BrotliDistanceCodeLimit
{
    public uint max_alphabet_size;
    public uint max_distance;
}

/// <summary><c>BrotliPrefixCodeRange</c>: the range of values belonging to a prefix code —
/// <c>[offset, offset + 2^nbits)</c>.</summary>
internal readonly struct BrotliPrefixCodeRange
{
    public readonly ushort offset;
    public readonly byte nbits;

    public BrotliPrefixCodeRange(ushort offset, byte nbits)
    {
        this.offset = offset;
        this.nbits = nbits;
    }
}
