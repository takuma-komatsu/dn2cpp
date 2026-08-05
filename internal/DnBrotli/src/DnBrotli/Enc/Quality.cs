// Port of c/enc/quality.h (brotli v1.1.0): constants and formulas that affect
// speed-ratio trade-offs and thus define quality levels. Ported whole — the
// q>=2 encoder paths depend on the q>=2 formulas.

using System.Runtime.CompilerServices;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Platform;

namespace DnBrotli.Enc;

internal static unsafe class Quality
{
    internal const int FAST_ONE_PASS_COMPRESSION_QUALITY = 0;
    internal const int FAST_TWO_PASS_COMPRESSION_QUALITY = 1;
    internal const int ZOPFLIFICATION_QUALITY = 10;
    internal const int HQ_ZOPFLIFICATION_QUALITY = 11;

    internal const int MAX_QUALITY_FOR_STATIC_ENTROPY_CODES = 2;
    internal const int MIN_QUALITY_FOR_BLOCK_SPLIT = 4;
    internal const int MIN_QUALITY_FOR_NONZERO_DISTANCE_PARAMS = 4;
    internal const int MIN_QUALITY_FOR_OPTIMIZE_HISTOGRAMS = 4;
    internal const int MIN_QUALITY_FOR_EXTENSIVE_REFERENCE_SEARCH = 5;
    internal const int MIN_QUALITY_FOR_CONTEXT_MODELING = 5;
    internal const int MIN_QUALITY_FOR_HQ_CONTEXT_MODELING = 7;
    internal const int MIN_QUALITY_FOR_HQ_BLOCK_SPLITTING = 10;

    /* For quality below MIN_QUALITY_FOR_BLOCK_SPLIT there is no block splitting,
       so we buffer at most this much literals and commands. */
    internal const int MAX_NUM_DELAYED_SYMBOLS = 0x2FFF;

    /* Returns hash-table size for quality levels 0 and 1. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint MaxHashTableSize(int quality)
    {
        return quality == FAST_ONE_PASS_COMPRESSION_QUALITY ? (nuint)(1 << 15) : (nuint)(1 << 17);
    }

    /* The maximum length for which the zopflification uses distinct distances. */
    internal const int MAX_ZOPFLI_LEN_QUALITY_10 = 150;
    internal const int MAX_ZOPFLI_LEN_QUALITY_11 = 325;

    /* Do not thoroughly search when a long copy is found. */
    internal const int BROTLI_LONG_COPY_QUICK_STEP = 16384;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint MaxZopfliLen(BrotliEncoderParams* @params)
    {
        return @params->quality <= 10 ?
            MAX_ZOPFLI_LEN_QUALITY_10 :
            (nuint)MAX_ZOPFLI_LEN_QUALITY_11;
    }

    /* Number of best candidates to evaluate to expand Zopfli chain. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint MaxZopfliCandidates(BrotliEncoderParams* @params)
    {
        return @params->quality <= 10 ? 1 : (nuint)5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SanitizeParams(BrotliEncoderParams* @params)
    {
        @params->quality = BROTLI_MIN(MaxQuality, BROTLI_MAX(MinQuality, @params->quality));
        if (@params->quality <= MAX_QUALITY_FOR_STATIC_ENTROPY_CODES)
        {
            @params->large_window = 0;
        }
        if (@params->lgwin < MinWindowBits)
        {
            @params->lgwin = MinWindowBits;
        }
        else
        {
            int max_lgwin = @params->large_window != 0 ? LargeMaxWindowBits : MaxWindowBits;
            if (@params->lgwin > max_lgwin) @params->lgwin = max_lgwin;
        }
    }

    /* Returns optimized lg_block value. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ComputeLgBlock(BrotliEncoderParams* @params)
    {
        int lgblock = @params->lgblock;
        if (@params->quality == FAST_ONE_PASS_COMPRESSION_QUALITY ||
            @params->quality == FAST_TWO_PASS_COMPRESSION_QUALITY)
        {
            lgblock = @params->lgwin;
        }
        else if (@params->quality < MIN_QUALITY_FOR_BLOCK_SPLIT)
        {
            lgblock = 14;
        }
        else if (lgblock == 0)
        {
            lgblock = 16;
            if (@params->quality >= 9 && @params->lgwin > lgblock)
            {
                lgblock = BROTLI_MIN(18, @params->lgwin);
            }
        }
        else
        {
            lgblock = BROTLI_MIN(MaxInputBlockBits, BROTLI_MAX(MinInputBlockBits, lgblock));
        }
        return lgblock;
    }

    /* Returns log2 of the size of main ring buffer area.
       Allocate at least lgwin + 1 bits for the ring buffer so that the newly
       added block fits there completely and we still get lgwin bits and at least
       read_block_size_bits + 1 bits because the copy tail length needs to be
       smaller than ring-buffer size. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ComputeRbBits(BrotliEncoderParams* @params)
    {
        return 1 + BROTLI_MAX(@params->lgwin, @params->lgblock);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint MaxMetablockSize(BrotliEncoderParams* @params)
    {
        int bits = BROTLI_MIN(ComputeRbBits(@params), MaxInputBlockBits);
        return (nuint)1 << bits;
    }

    /* When searching for backward references and have not seen matches for a long
       time, we can skip some match lookups. Unsuccessful match lookups are very
       expensive and this kind of a heuristic speeds up compression quite a lot.
       At first 8 byte strides are taken and every second byte is put to hasher.
       After 4x more literals stride by 16 bytes, every put 4-th byte to hasher.
       Applied only to qualities 2 to 9. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint LiteralSpreeLengthForSparseSearch(BrotliEncoderParams* @params)
    {
        return @params->quality < 9 ? 64 : (nuint)512;
    }

    /* Quality-to-hasher mapping; see the table in c/enc/quality.h. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ChooseHasher(BrotliEncoderParams* @params, BrotliHasherParams* hparams)
    {
        if (@params->quality > 9)
        {
            hparams->type = 10;
        }
        else if (@params->quality == 4 && @params->size_hint >= (1 << 20))
        {
            hparams->type = 54;
        }
        else if (@params->quality < 5)
        {
            hparams->type = @params->quality;
        }
        else if (@params->lgwin <= 16)
        {
            hparams->type = @params->quality < 7 ? 40 : @params->quality < 9 ? 41 : 42;
        }
        else if (@params->size_hint >= (1 << 20) && @params->lgwin >= 19)
        {
            hparams->type = 6;
            hparams->block_bits = @params->quality - 1;
            hparams->bucket_bits = 15;
            hparams->num_last_distances_to_check =
                @params->quality < 7 ? 4 : @params->quality < 9 ? 10 : 16;
        }
        else
        {
            hparams->type = 5;
            hparams->block_bits = @params->quality - 1;
            hparams->bucket_bits = @params->quality < 7 ? 14 : 15;
            hparams->num_last_distances_to_check =
                @params->quality < 7 ? 4 : @params->quality < 9 ? 10 : 16;
        }

        if (@params->lgwin > 24)
        {
            /* Different hashers for large window brotli: not for qualities <= 2,
               these are too fast for large window. Not for qualities >= 10: their
               hasher already works well with large window. So the changes are:
               H3 --> H35: for quality 3.
               H54 --> H55: for quality 4 with size hint > 1MB
               H6 --> H65: for qualities 5, 6, 7, 8, 9. */
            if (hparams->type == 3)
            {
                hparams->type = 35;
            }
            if (hparams->type == 54)
            {
                hparams->type = 55;
            }
            if (hparams->type == 6)
            {
                hparams->type = 65;
            }
        }
    }
}
