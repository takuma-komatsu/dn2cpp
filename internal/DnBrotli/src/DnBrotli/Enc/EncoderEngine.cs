// Port of c/enc/encode.c (brotli v1.1.0) — the compressor state machine and
// the public encode.h surface, function-for-function in the C source order.
//
// Scope: the complete streaming machinery (CompressStream with
// Process/Flush/Finish/EmitMetadata, the "flint", byte-padding injection,
// metadata blocks, one-shot BrotliEncoderCompress with its uncompressed
// fallback), the q0/q1 routing to the fragment compressors, the q2/q3
// greedy path (hash.h quickly hashers -> backward_references.c ->
// BrotliStoreMetaBlockFast/Trivial), and the q4..q9 generic path (hashers
// {5, 6, 40, 41, 42}, ChooseContextMap / ShouldUseComplexStaticContextMap /
// DecideOverLiteralContextModeling, the q>=4 metablock-build branch of
// WriteMetaBlockInternal with metablock.c + BrotliStoreMetaBlock), and the
// q10/q11 Zopfli routing (backward_references_hq.c -> BackwardReferencesHq.cs,
// hasher type 10). The remaining C call boundaries throw
// NotImplementedException, all tagged [deferred]:
//   - the ExtendLastCommand compound-dictionary walk
//   - hasher types {35, 55, 65} (lgwin > 24)
// The dictionary APIs (BrotliEncoderPrepareDictionary & friends) are likewise
// deferred; they are additive and independent of the state machine.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DnBrotli.Common;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Context;
using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.BackwardReferences;
using static DnBrotli.Enc.BackwardReferencesHq;
using static DnBrotli.Enc.BrotliBitStream;
using static DnBrotli.Enc.BrotliEncoderFlintState;
using static DnBrotli.Enc.BrotliEncoderStreamState;
using static DnBrotli.Enc.Command;
using static DnBrotli.Enc.CompressFragmentTwoPass;
using static DnBrotli.Enc.Hash;
using static DnBrotli.Enc.MemoryManager;
using static DnBrotli.Enc.Quality;
using static DnBrotli.Enc.RingBuffer;
using static DnBrotli.Enc.WriteBits;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

internal static unsafe class EncoderEngine
{
    private const int BROTLI_TRUE = 1;
    private const int BROTLI_FALSE = 0;

    /* C-name aliases for the public enum members (values match the C exactly). */
    private const BrotliEncoderOperation BROTLI_OPERATION_PROCESS = BrotliEncoderOperation.Process;
    private const BrotliEncoderOperation BROTLI_OPERATION_FLUSH = BrotliEncoderOperation.Flush;
    private const BrotliEncoderOperation BROTLI_OPERATION_FINISH = BrotliEncoderOperation.Finish;
    private const BrotliEncoderOperation BROTLI_OPERATION_EMIT_METADATA = BrotliEncoderOperation.EmitMetadata;

    private const BrotliEncoderMode BROTLI_MODE_FONT = BrotliEncoderMode.Font;
    private const BrotliEncoderMode BROTLI_DEFAULT_MODE = BrotliEncoderMode.Generic;

    private const uint BROTLI_UINT32_MAX = uint.MaxValue;

    /* kMinUTF8Ratio (c/enc/utf8_util.h) lives in Utf8Util.cs. */
    private const double kMinUTF8Ratio = Utf8Util.kMinUTF8Ratio;

    private static nuint InputBlockSize(BrotliEncoderState* s)
    {
        return (nuint)1 << s->@params.lgblock;
    }

    private static ulong UnprocessedInputSize(BrotliEncoderState* s)
    {
        return s->input_pos_ - s->last_processed_pos_;
    }

    private static nuint RemainingInputBlockSize(BrotliEncoderState* s)
    {
        ulong delta = UnprocessedInputSize(s);
        nuint block_size = InputBlockSize(s);
        if (delta >= block_size) return 0;
        return block_size - (nuint)delta;
    }

    internal static int BrotliEncoderSetParameter(
        BrotliEncoderState* state, BrotliEncoderParameter p, uint value)
    {
        /* Changing parameters on the fly is not implemented yet. */
        if (state->is_initialized_ != 0) return BROTLI_FALSE;
        /* TODO(eustas): Validate/clamp parameters here. */
        switch (p)
        {
            case BrotliEncoderParameter.Mode:  /* BROTLI_PARAM_MODE */
                state->@params.mode = (BrotliEncoderMode)value;
                return BROTLI_TRUE;

            case BrotliEncoderParameter.Quality:  /* BROTLI_PARAM_QUALITY */
                state->@params.quality = (int)value;
                return BROTLI_TRUE;

            case BrotliEncoderParameter.LgWin:  /* BROTLI_PARAM_LGWIN */
                state->@params.lgwin = (int)value;
                return BROTLI_TRUE;

            case BrotliEncoderParameter.LgBlock:  /* BROTLI_PARAM_LGBLOCK */
                state->@params.lgblock = (int)value;
                return BROTLI_TRUE;

            case BrotliEncoderParameter.DisableLiteralContextModeling:
                if ((value != 0) && (value != 1)) return BROTLI_FALSE;
                state->@params.disable_literal_context_modeling = value != 0 ? 1 : 0;
                return BROTLI_TRUE;

            case BrotliEncoderParameter.SizeHint:  /* BROTLI_PARAM_SIZE_HINT */
                state->@params.size_hint = value;
                return BROTLI_TRUE;

            case BrotliEncoderParameter.LargeWindow:  /* BROTLI_PARAM_LARGE_WINDOW */
                state->@params.large_window = value != 0 ? 1 : 0;
                return BROTLI_TRUE;

            case BrotliEncoderParameter.NPostfix:  /* BROTLI_PARAM_NPOSTFIX */
                state->@params.dist.distance_postfix_bits = value;
                return BROTLI_TRUE;

            case BrotliEncoderParameter.NDirect:  /* BROTLI_PARAM_NDIRECT */
                state->@params.dist.num_direct_distance_codes = value;
                return BROTLI_TRUE;

            case BrotliEncoderParameter.StreamOffset:  /* BROTLI_PARAM_STREAM_OFFSET */
                if (value > (1u << 30)) return BROTLI_FALSE;
                state->@params.stream_offset = value;
                return BROTLI_TRUE;

            default: return BROTLI_FALSE;
        }
    }

    /* Wraps 64-bit input position to 32-bit ring-buffer position preserving
       "not-a-first-lap" feature. */
    private static uint WrapPosition(ulong position)
    {
        uint result = (uint)position;
        ulong gb = position >> 30;
        if (gb > 2)
        {
            /* Wrap every 2GiB; The first 3GB are continuous. */
            result = (result & ((1u << 30) - 1)) | (((uint)((gb - 1) & 1) + 1) << 30);
        }
        return result;
    }

    private static byte* GetBrotliStorage(BrotliEncoderState* s, nuint size)
    {
        MemoryManager* m = &s->memory_manager_;
        if (s->storage_size_ < size)
        {
            BROTLI_FREE(m, ref s->storage_);
            s->storage_ = BROTLI_ALLOC<byte>(m, size);
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(s->storage_)) return null;
            s->storage_size_ = size;
        }
        return s->storage_;
    }

    private static nuint HashTableSize(nuint max_table_size, nuint input_size)
    {
        nuint htsize = 256;
        while (htsize < max_table_size && htsize < input_size)
        {
            htsize <<= 1;
        }
        return htsize;
    }

    private static int* GetHashTable(BrotliEncoderState* s, int quality,
                                     nuint input_size, nuint* table_size)
    {
        /* Use smaller hash table when input.size() is smaller, since we
           fill the table, incurring O(hash table size) overhead for
           compression, and if the input is short, we won't need that
           many hash table entries anyway. */
        MemoryManager* m = &s->memory_manager_;
        nuint max_table_size = MaxHashTableSize(quality);
        nuint htsize = HashTableSize(max_table_size, input_size);
        int* table;
        if (quality == FAST_ONE_PASS_COMPRESSION_QUALITY)
        {
            /* Only odd shifts are supported by fast-one-pass. */
            if ((htsize & 0xAAAAA) == 0)
            {
                htsize <<= 1;
            }
        }

        if (htsize <= (1 << 10))  /* sizeof(s->small_table_) / sizeof(s->small_table_[0]) */
        {
            table = s->small_table_;
        }
        else
        {
            if (htsize > s->large_table_size_)
            {
                s->large_table_size_ = htsize;
                BROTLI_FREE(m, ref s->large_table_);
                s->large_table_ = BROTLI_ALLOC<int>(m, htsize);
                if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(s->large_table_)) return null;
            }
            table = s->large_table_;
        }

        *table_size = htsize;
        new Span<int>(table, (int)htsize).Clear();
        return table;
    }

    private static void EncodeWindowBits(int lgwin, int large_window,
        ushort* last_bytes, byte* last_bytes_bits)
    {
        if (large_window != 0)
        {
            *last_bytes = (ushort)(((lgwin & 0x3F) << 8) | 0x11);
            *last_bytes_bits = 14;
        }
        else
        {
            if (lgwin == 16)
            {
                *last_bytes = 0;
                *last_bytes_bits = 1;
            }
            else if (lgwin == 17)
            {
                *last_bytes = 1;
                *last_bytes_bits = 7;
            }
            else if (lgwin > 17)
            {
                *last_bytes = (ushort)(((lgwin - 17) << 1) | 0x01);
                *last_bytes_bits = 4;
            }
            else
            {
                *last_bytes = (ushort)(((lgwin - 8) << 4) | 0x01);
                *last_bytes_bits = 7;
            }
        }
    }

    private static readonly byte[] kDefaultCommandDepths =  /* [128] */
    {
        0, 4, 4, 5, 6, 6, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8,
        0, 0, 0, 4, 4, 4, 4, 4, 5, 5, 6, 6, 6, 6, 7, 7,
        7, 7, 10, 10, 10, 10, 10, 10, 0, 4, 4, 5, 5, 5, 6, 6,
        7, 8, 8, 9, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10,
        5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        6, 6, 6, 6, 6, 6, 5, 5, 5, 5, 5, 5, 4, 4, 4, 4,
        4, 4, 4, 5, 5, 5, 5, 5, 5, 6, 6, 7, 7, 7, 8, 10,
        12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
    };
    private static readonly ushort[] kDefaultCommandBits =  /* [128] */
    {
        0,   0,   8,   9,   3,  35,   7,   71,
        39, 103,  23,  47, 175, 111, 239,   31,
        0,   0,   0,   4,  12,   2,  10,    6,
        13,  29,  11,  43,  27,  59,  87,   55,
        15,  79, 319, 831, 191, 703, 447,  959,
        0,  14,   1,  25,   5,  21,  19,   51,
        119, 159,  95, 223, 479, 991,  63,  575,
        127, 639, 383, 895, 255, 767, 511, 1023,
        14, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        27, 59, 7, 39, 23, 55, 30, 1, 17, 9, 25, 5, 0, 8, 4, 12,
        2, 10, 6, 21, 13, 29, 3, 19, 11, 15, 47, 31, 95, 63, 127, 255,
        767, 2815, 1791, 3839, 511, 2559, 1535, 3583, 1023, 3071, 2047, 4095,
    };
    private static readonly byte[] kDefaultCommandCode =  /* [57] */
    {
        0xff, 0x77, 0xd5, 0xbf, 0xe7, 0xde, 0xea, 0x9e, 0x51, 0x5d, 0xde, 0xc6,
        0x70, 0x57, 0xbc, 0x58, 0x58, 0x58, 0xd8, 0xd8, 0x58, 0xd5, 0xcb, 0x8c,
        0xea, 0xe0, 0xc3, 0x87, 0x1f, 0x83, 0xc1, 0x60, 0x1c, 0x67, 0xb2, 0xaa,
        0x06, 0x83, 0xc1, 0x60, 0x30, 0x18, 0xcc, 0xa1, 0xce, 0x88, 0x54, 0x94,
        0x46, 0xe1, 0xb0, 0xd0, 0x4e, 0xb2, 0xf7, 0x04, 0x00,
    };
    private const nuint kDefaultCommandCodeNumBits = 448;

    /* Initializes the command and distance prefix codes for the first block. */
    private static void InitCommandPrefixCodes(BrotliOnePassArena* s)
    {
        /* COPY_ARRAY(s->cmd_depth, kDefaultCommandDepths); etc. */
        fixed (byte* p = kDefaultCommandDepths)
        {
            Buffer.MemoryCopy(p, s->cmd_depth, 128, 128);
        }
        fixed (ushort* p = kDefaultCommandBits)
        {
            Buffer.MemoryCopy(p, s->cmd_bits, 128 * sizeof(ushort), 128 * sizeof(ushort));
        }

        /* Initialize the pre-compressed form of the command and distance prefix
           codes. */
        fixed (byte* p = kDefaultCommandCode)
        {
            Buffer.MemoryCopy(p, s->cmd_code, kDefaultCommandCode.Length, kDefaultCommandCode.Length);
        }
        s->cmd_code_numbits = kDefaultCommandCodeNumBits;
    }

    private static bool ShouldCompress(
        byte* data, nuint mask, ulong last_flush_pos,
        nuint bytes, nuint num_literals, nuint num_commands)
    {
        /* TODO(eustas): find more precise minimal block overhead. */
        if (bytes <= 2) return false;
        if (num_commands < (bytes >> 8) + 2)
        {
            if ((double)num_literals > 0.99 * (double)bytes)
            {
                uint* literal_histo = stackalloc uint[256];
                new Span<uint>(literal_histo, 256).Clear();
                const uint kSampleRate = 13;
                const double kMinEntropy = 7.92;
                double bit_cost_threshold =
                    (double)bytes * kMinEntropy / kSampleRate;
                nuint t = (bytes + kSampleRate - 1) / kSampleRate;
                uint pos = (uint)last_flush_pos;
                nuint i;
                for (i = 0; i < t; i++)
                {
                    ++literal_histo[data[pos & mask]];
                    pos += kSampleRate;
                }
                if (BitCost.BitsEntropy(literal_histo, 256) > bit_cost_threshold)
                {
                    return false;
                }
            }
        }
        return true;
    }

    /* The C declares these tables as function-local static const uint32_t[64];
       consumers keep raw pointers to them past the callee's return, so they are
       copied once into pointer-stable native memory at type initialization. */
    private static uint* AllocStaticContextMap(ReadOnlySpan<uint> values)
    {
        uint* map = (uint*)NativeMemory.Alloc(64 * sizeof(uint));
        values.CopyTo(new Span<uint>(map, 64));
        return map;
    }

    private static readonly uint* kStaticContextMapContinuation = AllocStaticContextMap(
    [
        1, 1, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    ]);
    private static readonly uint* kStaticContextMapSimpleUTF8 = AllocStaticContextMap(
    [
        0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    ]);

    /* Decide about the context map based on the ability of the prediction
       ability of the previous byte UTF8-prefix on the next byte. The
       prediction ability is calculated as Shannon entropy. Here we need
       Shannon entropy instead of 'BitsEntropy' since the prefix will be
       encoded with the remaining 6 bits of the following byte, and
       BitsEntropy will assume that symbol to be stored alone using Huffman
       coding. */
    private static void ChooseContextMap(int quality,
                                         uint* bigram_histo,
                                         nuint* num_literal_contexts,
                                         uint** literal_context_map)
    {
        uint* monogram_histo = stackalloc uint[3] { 0, 0, 0 };
        uint* two_prefix_histo = stackalloc uint[6] { 0, 0, 0, 0, 0, 0 };
        nuint total;
        nuint i;
        nuint dummy;
        double* entropy = stackalloc double[4];
        for (i = 0; i < 9; ++i)
        {
            monogram_histo[i % 3] += bigram_histo[i];
            two_prefix_histo[i % 6] += bigram_histo[i];
        }
        entropy[1] = BitCost.ShannonEntropy(monogram_histo, 3, &dummy);
        entropy[2] = (BitCost.ShannonEntropy(two_prefix_histo, 3, &dummy) +
                      BitCost.ShannonEntropy(two_prefix_histo + 3, 3, &dummy));
        entropy[3] = 0;
        for (i = 0; i < 3; ++i)
        {
            entropy[3] += BitCost.ShannonEntropy(bigram_histo + 3 * i, 3, &dummy);
        }

        total = monogram_histo[0] + monogram_histo[1] + monogram_histo[2];
        /* BROTLI_DCHECK(total != 0); */
        entropy[0] = 1.0 / (double)total;
        entropy[1] *= entropy[0];
        entropy[2] *= entropy[0];
        entropy[3] *= entropy[0];

        if (quality < MIN_QUALITY_FOR_HQ_CONTEXT_MODELING)
        {
            /* 3 context models is a bit slower, don't use it at lower qualities. */
            entropy[3] = entropy[1] * 10;
        }
        /* If expected savings by symbol are less than 0.2 bits, skip the
           context modeling -- in exchange for faster decoding speed. */
        if (entropy[1] - entropy[2] < 0.2 &&
            entropy[1] - entropy[3] < 0.2)
        {
            *num_literal_contexts = 1;
        }
        else if (entropy[2] - entropy[3] < 0.02)
        {
            *num_literal_contexts = 2;
            *literal_context_map = kStaticContextMapSimpleUTF8;
        }
        else
        {
            *num_literal_contexts = 3;
            *literal_context_map = kStaticContextMapContinuation;
        }
    }

    private static readonly uint* kStaticContextMapComplexUTF8 = AllocStaticContextMap(
    [
        11, 11, 12, 12, /* 0 special */
        0, 0, 0, 0, /* 4 lf */
        1, 1, 9, 9, /* 8 space */
        2, 2, 2, 2, /* !, first after space/lf and after something else. */
        1, 1, 1, 1, /* " */
        8, 3, 3, 3, /* % */
        1, 1, 1, 1, /* ({[ */
        2, 2, 2, 2, /* }]) */
        8, 4, 4, 4, /* :; */
        8, 7, 4, 4, /* . */
        8, 0, 0, 0, /* > */
        3, 3, 3, 3, /* [0..9] */
        5, 5, 10, 5, /* [A-Z] */
        5, 5, 10, 5,
        6, 6, 6, 6, /* [a-z] */
        6, 6, 6, 6,
    ]);

    /* Decide if we want to use a more complex static context map containing 13
       context values, based on the entropy reduction of histograms over the
       first 5 bits of literals. */
    private static bool ShouldUseComplexStaticContextMap(byte* input,
        nuint start_pos, nuint length, nuint mask, int quality, nuint size_hint,
        nuint* num_literal_contexts, uint** literal_context_map,
        uint* arena)
    {
        /* BROTLI_UNUSED(quality); */
        /* Try the more complex static context map only for long data. */
        if (size_hint < (1 << 20))
        {
            return false;
        }
        else
        {
            nuint end_pos = start_pos + length;
            /* To make entropy calculations faster, we collect histograms
               over the 5 most significant bits of literals. One histogram
               without context and 13 additional histograms for each context value. */
            uint* combined_histo = arena;
            uint* context_histo = arena + 32;
            uint total = 0;
            double* entropy = stackalloc double[3];
            nuint dummy;
            nuint i;
            byte* utf8_lut = BROTLI_CONTEXT_LUT((nuint)ContextType.CONTEXT_UTF8);
            new Span<uint>(arena, 32 * 14).Clear();
            for (; start_pos + 64 <= end_pos; start_pos += 4096)
            {
                nuint stride_end_pos = start_pos + 64;
                byte prev2 = input[start_pos & mask];
                byte prev1 = input[(start_pos + 1) & mask];
                nuint pos;
                /* To make the analysis of the data faster we only examine 64 byte long
                   strides at every 4kB intervals. */
                for (pos = start_pos + 2; pos < stride_end_pos; ++pos)
                {
                    byte literal = input[pos & mask];
                    byte context = (byte)kStaticContextMapComplexUTF8[
                        BROTLI_CONTEXT(prev1, prev2, utf8_lut)];
                    ++total;
                    ++combined_histo[literal >> 3];
                    ++context_histo[(context << 5) + (literal >> 3)];
                    prev2 = prev1;
                    prev1 = literal;
                }
            }
            entropy[1] = BitCost.ShannonEntropy(combined_histo, 32, &dummy);
            entropy[2] = 0;
            for (i = 0; i < 13; ++i)
            {
                entropy[2] += BitCost.ShannonEntropy(context_histo + (i << 5), 32, &dummy);
            }
            entropy[0] = 1.0 / (double)total;
            entropy[1] *= entropy[0];
            entropy[2] *= entropy[0];
            /* The triggering heuristics below were tuned by compressing the individual
               files of the silesia corpus. If we skip this kind of context modeling
               for not very well compressible input (i.e. entropy using context modeling
               is 60% of maximal entropy) or if expected savings by symbol are less
               than 0.2 bits, then in every case when it triggers, the final compression
               ratio is improved. Note however that this heuristics might be too strict
               for some cases and could be tuned further. */
            if (entropy[2] > 3.0 || entropy[1] - entropy[2] < 0.2)
            {
                return false;
            }
            else
            {
                *num_literal_contexts = 13;
                *literal_context_map = kStaticContextMapComplexUTF8;
                return true;
            }
        }
    }

    private static void DecideOverLiteralContextModeling(byte* input,
        nuint start_pos, nuint length, nuint mask, int quality, nuint size_hint,
        nuint* num_literal_contexts, uint** literal_context_map,
        uint* arena)
    {
        if (quality < MIN_QUALITY_FOR_CONTEXT_MODELING || length < 64)
        {
            return;
        }
        else if (ShouldUseComplexStaticContextMap(
            input, start_pos, length, mask, quality, size_hint,
            num_literal_contexts, literal_context_map, arena))
        {
            /* Context map was already set, nothing else to do. */
        }
        else
        {
            /* Gather bi-gram data of the UTF8 byte prefixes. To make the analysis of
               UTF8 data faster we only examine 64 byte long strides at every 4kB
               intervals. */
            nuint end_pos = start_pos + length;
            uint* bigram_prefix_histo = arena;
            new Span<uint>(bigram_prefix_histo, 9).Clear();
            for (; start_pos + 64 <= end_pos; start_pos += 4096)
            {
                ReadOnlySpan<int> lut = [0, 0, 1, 2];  /* static const int lut[4] */
                nuint stride_end_pos = start_pos + 64;
                int prev = lut[input[start_pos & mask] >> 6] * 3;
                nuint pos;
                for (pos = start_pos + 1; pos < stride_end_pos; ++pos)
                {
                    byte literal = input[pos & mask];
                    ++bigram_prefix_histo[prev + lut[literal >> 6]];
                    prev = lut[literal >> 6] * 3;
                }
            }
            ChooseContextMap(quality, &bigram_prefix_histo[0], num_literal_contexts,
                             literal_context_map);
        }
    }

    /* Chooses the literal context mode for a metablock */
    private static ContextType ChooseContextMode(BrotliEncoderParams* @params,
        byte* data, nuint pos, nuint mask, nuint length)
    {
        /* We only do the computation for the option of something else than
           CONTEXT_UTF8 for the highest qualities */
        if (@params->quality >= MIN_QUALITY_FOR_HQ_BLOCK_SPLITTING &&
            Utf8Util.BrotliIsMostlyUTF8(data, pos, mask, length, kMinUTF8Ratio) == 0)
        {
            return ContextType.CONTEXT_SIGNED;
        }
        return ContextType.CONTEXT_UTF8;
    }

    private static void WriteMetaBlockInternal(MemoryManager* m,
                                               byte* data,
                                               nuint mask,
                                               ulong last_flush_pos,
                                               nuint bytes,
                                               int is_last,
                                               ContextType literal_context_mode,
                                               BrotliEncoderParams* @params,
                                               byte prev_byte,
                                               byte prev_byte2,
                                               nuint num_literals,
                                               nuint num_commands,
                                               Command* commands,
                                               int* saved_dist_cache,
                                               int* dist_cache,
                                               nuint* storage_ix,
                                               byte* storage)
    {
        uint wrapped_last_flush_pos = WrapPosition(last_flush_pos);
        ushort last_bytes;
        byte last_bytes_bits;
        byte* literal_context_lut = BROTLI_CONTEXT_LUT((nuint)literal_context_mode);
        BrotliEncoderParams block_params = *@params;

        if (bytes == 0)
        {
            /* Write the ISLAST and ISEMPTY bits. */
            BrotliWriteBits(2, 3, storage_ix, storage);
            *storage_ix = (*storage_ix + 7u) & ~(nuint)7u;
            return;
        }

        if (!ShouldCompress(data, mask, last_flush_pos, bytes,
                            num_literals, num_commands))
        {
            /* Restore the distance cache, as its last update by
               CreateBackwardReferences is now unused. */
            Buffer.MemoryCopy(saved_dist_cache, dist_cache, 4 * sizeof(int), 4 * sizeof(int));
            BrotliStoreUncompressedMetaBlock(is_last, data,
                                             wrapped_last_flush_pos, mask, bytes,
                                             storage_ix, storage);
            return;
        }

        last_bytes = (ushort)((storage[1] << 8) | storage[0]);
        last_bytes_bits = (byte)(*storage_ix);
        if (@params->quality <= MAX_QUALITY_FOR_STATIC_ENTROPY_CODES)
        {
            BrotliStoreMetaBlockFast(m, data, wrapped_last_flush_pos,
                                     bytes, mask, is_last, @params,
                                     commands, num_commands,
                                     storage_ix, storage);
            if (BROTLI_IS_OOM(m)) return;
        }
        else if (@params->quality < MIN_QUALITY_FOR_BLOCK_SPLIT)
        {
            BrotliStoreMetaBlockTrivial(m, data, wrapped_last_flush_pos,
                                        bytes, mask, is_last, @params,
                                        commands, num_commands,
                                        storage_ix, storage);
            if (BROTLI_IS_OOM(m)) return;
        }
        else
        {
            MetaBlockSplit mb = default;
            Metablock.InitMetaBlockSplit(&mb);
            if (@params->quality < MIN_QUALITY_FOR_HQ_BLOCK_SPLITTING)
            {
                nuint num_literal_contexts = 1;
                uint* literal_context_map = null;
                if (@params->disable_literal_context_modeling == 0)
                {
                    /* TODO(eustas): pull to higher level and reuse. */
                    uint* arena = BROTLI_ALLOC<uint>(m, 14 * 32);
                    if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(arena)) return;
                    DecideOverLiteralContextModeling(
                        data, wrapped_last_flush_pos, bytes, mask, @params->quality,
                        @params->size_hint, &num_literal_contexts,
                        &literal_context_map, arena);
                    BROTLI_FREE(m, ref arena);
                }
                Metablock.BrotliBuildMetaBlockGreedy(m, data, wrapped_last_flush_pos, mask,
                    prev_byte, prev_byte2, literal_context_lut, num_literal_contexts,
                    literal_context_map, commands, num_commands, &mb);
                if (BROTLI_IS_OOM(m)) return;
            }
            else
            {
                Metablock.BrotliBuildMetaBlock(m, data, wrapped_last_flush_pos, mask, &block_params,
                                               prev_byte, prev_byte2,
                                               commands, num_commands,
                                               literal_context_mode,
                                               &mb);
                if (BROTLI_IS_OOM(m)) return;
            }
            if (@params->quality >= MIN_QUALITY_FOR_OPTIMIZE_HISTOGRAMS)
            {
                /* The number of distance symbols effectively used for distance
                   histograms. It might be less than distance alphabet size
                   for "Large Window Brotli" (32-bit). */
                Metablock.BrotliOptimizeHistograms(block_params.dist.alphabet_size_limit, &mb);
            }
            BrotliStoreMetaBlock(m, data, wrapped_last_flush_pos, bytes, mask,
                                 prev_byte, prev_byte2,
                                 is_last,
                                 &block_params,
                                 literal_context_mode,
                                 commands, num_commands,
                                 &mb,
                                 storage_ix, storage);
            if (BROTLI_IS_OOM(m)) return;
            Metablock.DestroyMetaBlockSplit(m, &mb);
        }
        if (bytes + 4 < (*storage_ix >> 3))
        {
            /* Restore the distance cache and last byte. */
            Buffer.MemoryCopy(saved_dist_cache, dist_cache, 4 * sizeof(int), 4 * sizeof(int));
            storage[0] = (byte)last_bytes;
            storage[1] = (byte)(last_bytes >> 8);
            *storage_ix = last_bytes_bits;
            BrotliStoreUncompressedMetaBlock(is_last, data,
                                             wrapped_last_flush_pos, mask,
                                             bytes, storage_ix, storage);
        }
    }

    private static void ChooseDistanceParams(BrotliEncoderParams* @params)
    {
        uint distance_postfix_bits = 0;
        uint num_direct_distance_codes = 0;

        if (@params->quality >= MIN_QUALITY_FOR_NONZERO_DISTANCE_PARAMS)
        {
            uint ndirect_msb;
            if (@params->mode == BROTLI_MODE_FONT)
            {
                distance_postfix_bits = 1;
                num_direct_distance_codes = 12;
            }
            else
            {
                distance_postfix_bits = @params->dist.distance_postfix_bits;
                num_direct_distance_codes = @params->dist.num_direct_distance_codes;
            }
            ndirect_msb = (num_direct_distance_codes >> (int)distance_postfix_bits) & 0x0F;
            if (distance_postfix_bits > BROTLI_MAX_NPOSTFIX ||
                num_direct_distance_codes > BROTLI_MAX_NDIRECT ||
                (ndirect_msb << (int)distance_postfix_bits) != num_direct_distance_codes)
            {
                distance_postfix_bits = 0;
                num_direct_distance_codes = 0;
            }
        }

        Metablock.BrotliInitDistanceParams(&@params->dist, distance_postfix_bits,
                                           num_direct_distance_codes, @params->large_window);
    }

    private static int EnsureInitialized(BrotliEncoderState* s)
    {
        MemoryManager* m = &s->memory_manager_;
        if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
        if (s->is_initialized_ != 0) return BROTLI_TRUE;

        s->last_bytes_bits_ = 0;
        s->last_bytes_ = 0;
        s->flint_ = (sbyte)BROTLI_FLINT_DONE;
        s->remaining_metadata_bytes_ = BROTLI_UINT32_MAX;

        SanitizeParams(&s->@params);
        s->@params.lgblock = ComputeLgBlock(&s->@params);
        ChooseDistanceParams(&s->@params);

        if (s->@params.stream_offset != 0)
        {
            s->flint_ = (sbyte)BROTLI_FLINT_NEEDS_2_BYTES;
            /* Poison the distance cache. -16 +- 3 is still less than zero (invalid). */
            s->dist_cache_[0] = -16;
            s->dist_cache_[1] = -16;
            s->dist_cache_[2] = -16;
            s->dist_cache_[3] = -16;
            Buffer.MemoryCopy(s->dist_cache_, s->saved_dist_cache_,
                4 * sizeof(int), 4 * sizeof(int));
        }

        RingBufferSetup(&s->@params, &s->ringbuffer_);

        /* Initialize last byte with stream header. */
        {
            int lgwin = s->@params.lgwin;
            if (s->@params.quality == FAST_ONE_PASS_COMPRESSION_QUALITY ||
                s->@params.quality == FAST_TWO_PASS_COMPRESSION_QUALITY)
            {
                lgwin = BROTLI_MAX(lgwin, 18);
            }
            if (s->@params.stream_offset == 0)
            {
                /* dn2cpp: locals instead of `&s->last_bytes_(_bits_)` — the backend
                   widens sub-int32 struct fields; see Command.InitCommand. */
                ushort last_bytes = 0;
                byte last_bytes_bits = 0;
                EncodeWindowBits(lgwin, s->@params.large_window,
                                 &last_bytes, &last_bytes_bits);
                s->last_bytes_ = last_bytes;
                s->last_bytes_bits_ = last_bytes_bits;
            }
            else
            {
                /* Bigger values have the same effect, but could cause overflows. */
                s->@params.stream_offset = BROTLI_MIN(
                    s->@params.stream_offset, MaxBackwardLimit(lgwin));
            }
        }

        if (s->@params.quality == FAST_ONE_PASS_COMPRESSION_QUALITY)
        {
            s->one_pass_arena_ = BROTLI_ALLOC<BrotliOnePassArena>(m, 1);
            if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
            InitCommandPrefixCodes(s->one_pass_arena_);
        }
        else if (s->@params.quality == FAST_TWO_PASS_COMPRESSION_QUALITY)
        {
            s->two_pass_arena_ = BROTLI_ALLOC<BrotliTwoPassArena>(m, 1);
            if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
        }

        s->is_initialized_ = BROTLI_TRUE;
        return BROTLI_TRUE;
    }

    private static void BrotliEncoderInitParams(BrotliEncoderParams* @params)
    {
        @params->mode = BROTLI_DEFAULT_MODE;
        @params->large_window = BROTLI_FALSE;
        @params->quality = DefaultQuality;  /* BROTLI_DEFAULT_QUALITY */
        @params->lgwin = DefaultWindow;     /* BROTLI_DEFAULT_WINDOW */
        @params->lgblock = 0;
        @params->stream_offset = 0;
        @params->size_hint = 0;
        @params->disable_literal_context_modeling = BROTLI_FALSE;
        EncoderDict.BrotliInitSharedEncoderDictionary(&@params->dictionary);
        @params->dist.distance_postfix_bits = 0;
        @params->dist.num_direct_distance_codes = 0;
        @params->dist.alphabet_size_max =
            BROTLI_DISTANCE_ALPHABET_SIZE(0, 0, BROTLI_MAX_DISTANCE_BITS);
        @params->dist.alphabet_size_limit = @params->dist.alphabet_size_max;
        @params->dist.max_distance = BROTLI_MAX_DISTANCE;
    }

    private static void BrotliEncoderCleanupParams(MemoryManager* m,
        BrotliEncoderParams* @params)
    {
        EncoderDict.BrotliCleanupSharedEncoderDictionary(m, &@params->dictionary);
    }

    private static void BrotliEncoderInitState(BrotliEncoderState* s)
    {
        BrotliEncoderInitParams(&s->@params);
        s->input_pos_ = 0;
        s->num_commands_ = 0;
        s->num_literals_ = 0;
        s->last_insert_len_ = 0;
        s->last_flush_pos_ = 0;
        s->last_processed_pos_ = 0;
        s->prev_byte_ = 0;
        s->prev_byte2_ = 0;
        s->storage_size_ = 0;
        s->storage_ = null;
        HasherInit(&s->hasher_);
        s->large_table_ = null;
        s->large_table_size_ = 0;
        s->one_pass_arena_ = null;
        s->two_pass_arena_ = null;
        s->command_buf_ = null;
        s->literal_buf_ = null;
        s->total_in_ = 0;
        s->next_out_ = null;
        s->available_out_ = 0;
        s->total_out_ = 0;
        s->stream_state_ = BROTLI_STREAM_PROCESSING;
        s->is_last_block_emitted_ = BROTLI_FALSE;
        s->is_initialized_ = BROTLI_FALSE;

        RingBufferInit(&s->ringbuffer_);

        s->commands_ = null;
        s->cmd_alloc_size_ = 0;

        /* Initialize distance cache. */
        s->dist_cache_[0] = 4;
        s->dist_cache_[1] = 11;
        s->dist_cache_[2] = 15;
        s->dist_cache_[3] = 16;
        /* Save the state of the distance cache in case we need to restore it for
           emitting an uncompressed block. */
        Buffer.MemoryCopy(s->dist_cache_, s->saved_dist_cache_,
            4 * sizeof(int), 4 * sizeof(int));
    }

    internal static BrotliEncoderState* BrotliEncoderCreateInstance(
        nint alloc_func, nint free_func, void* opaque)
    {
        BrotliEncoderState* state = (BrotliEncoderState*)BrotliBootstrapAlloc(
            (nuint)sizeof(BrotliEncoderState), alloc_func, free_func, opaque);
        if (state == null)
        {
            /* BROTLI_DUMP(); */
            return null;
        }
        BrotliInitMemoryManager(
            &state->memory_manager_, alloc_func, free_func, opaque);
        BrotliEncoderInitState(state);
        return state;
    }

    private static void BrotliEncoderCleanupState(BrotliEncoderState* s)
    {
        MemoryManager* m = &s->memory_manager_;

        if (BROTLI_IS_OOM(m))
        {
            BrotliWipeOutMemoryManager(m);
            return;
        }

        BROTLI_FREE(m, ref s->storage_);
        BROTLI_FREE(m, ref s->commands_);
        RingBufferFree(m, &s->ringbuffer_);
        DestroyHasher(m, &s->hasher_);
        BROTLI_FREE(m, ref s->large_table_);
        BROTLI_FREE(m, ref s->one_pass_arena_);
        BROTLI_FREE(m, ref s->two_pass_arena_);
        BROTLI_FREE(m, ref s->command_buf_);
        BROTLI_FREE(m, ref s->literal_buf_);
        BrotliEncoderCleanupParams(m, &s->@params);
    }

    /* Deinitializes and frees BrotliEncoderState instance. */
    internal static void BrotliEncoderDestroyInstance(BrotliEncoderState* state)
    {
        if (state == null)
        {
            return;
        }
        else
        {
            BrotliEncoderCleanupState(state);
            BrotliBootstrapFree(state, &state->memory_manager_);
        }
    }

    /*
       Copies the given input data to the internal ring buffer of the compressor.
       No processing of the data occurs at this time and this function can be
       called multiple times before calling WriteBrotliData() to process the
       accumulated input. At most input_block_size() bytes of input data can be
       copied to the ring buffer, otherwise the next WriteBrotliData() will fail.
     */
    private static void CopyInputToRingBuffer(BrotliEncoderState* s,
                                              nuint input_size,
                                              byte* input_buffer)
    {
        RingBuffer* ringbuffer_ = &s->ringbuffer_;
        MemoryManager* m = &s->memory_manager_;
        RingBufferWrite(m, input_buffer, input_size, ringbuffer_);
        if (BROTLI_IS_OOM(m)) return;
        s->input_pos_ += input_size;

        /* TL;DR: If needed, initialize 7 more bytes in the ring buffer to make the
           hashing not depend on uninitialized data. This makes compression
           deterministic and it prevents uninitialized memory warnings in Valgrind.
           Even without erasing, the output would be valid (but nondeterministic).

           Only clear during the first round of ring-buffer writes. On
           subsequent rounds data in the ring-buffer would be affected. */
        if (ringbuffer_->pos_ <= ringbuffer_->mask_)
        {
            /* This is the first time when the ring buffer is being written.
               We clear 7 bytes just after the bytes that have been copied from
               the input buffer.

               The ring-buffer has a "tail" that holds a copy of the beginning,
               but only once the ring buffer has been fully written once, i.e.,
               pos <= mask. For the first time, we need to write values
               in this tail (where index may be larger than mask), so that
               we have exactly defined behavior and don't read uninitialized
               memory. Due to performance reasons, hashing reads data using a
               LOAD64, which can go 7 bytes beyond the bytes written in the
               ring-buffer. */
            new Span<byte>(ringbuffer_->buffer_ + ringbuffer_->pos_, 7).Clear();
        }
    }

    /* Marks all input as processed.
       Returns true if position wrapping occurs. */
    private static bool UpdateLastProcessedPos(BrotliEncoderState* s)
    {
        uint wrapped_last_processed_pos = WrapPosition(s->last_processed_pos_);
        uint wrapped_input_pos = WrapPosition(s->input_pos_);
        s->last_processed_pos_ = s->input_pos_;
        return wrapped_input_pos < wrapped_last_processed_pos;
    }

    private static void ExtendLastCommand(BrotliEncoderState* s, uint* bytes,
                                          uint* wrapped_last_processed_pos)
    {
        Command* last_command = &s->commands_[s->num_commands_ - 1];
        byte* data = s->ringbuffer_.buffer_;
        uint mask = s->ringbuffer_.mask_;
        ulong max_backward_distance =
            (((ulong)1) << s->@params.lgwin) - (ulong)WindowGap;
        ulong last_copy_len = last_command->copy_len_ & 0x1FFFFFF;
        ulong last_processed_pos = s->last_processed_pos_ - last_copy_len;
        ulong max_distance = last_processed_pos < max_backward_distance ?
            last_processed_pos : max_backward_distance;
        ulong cmd_dist = (ulong)s->dist_cache_[0];
        uint distance_code = CommandRestoreDistanceCode(last_command,
                                                        &s->@params.dist);
        CompoundDictionary* dict = &s->@params.dictionary.compound;
        nuint compound_dictionary_size = dict->total_size;
        if (distance_code < BROTLI_NUM_DISTANCE_SHORT_CODES ||
            distance_code - (BROTLI_NUM_DISTANCE_SHORT_CODES - 1) == cmd_dist)
        {
            if (cmd_dist <= max_distance)
            {
                while (*bytes != 0 && data[*wrapped_last_processed_pos & mask] ==
                       data[(*wrapped_last_processed_pos - cmd_dist) & mask])
                {
                    last_command->copy_len_++;
                    (*bytes)--;
                    (*wrapped_last_processed_pos)++;
                }
            }
            else
            {
                if ((cmd_dist - max_distance - 1) < compound_dictionary_size &&
                    last_copy_len < cmd_dist - max_distance)
                {
                    /* Deferred boundary: extending a copy into the compound
                       dictionary (compound_dictionary.h chunk walk). Unreachable
                       while the compound part stays inert (total_size == 0). */
                    throw new NotImplementedException(
                        "DnBrotli DB-deferred: compound dictionary (encode.c ExtendLastCommand)");
                }
            }
            /* The copy length is at most the metablock size, and thus expressible. */
            /* dn2cpp: local instead of `&last_command->cmd_prefix_` — the backend
               widens sub-int32 struct fields; see Command.InitCommand. */
            ushort cmd_prefix = 0;
            GetLengthCode(last_command->insert_len_,
                          (nuint)((int)(last_command->copy_len_ & 0x1FFFFFF) +
                                  (int)(last_command->copy_len_ >> 25)),
                          (last_command->dist_prefix_ & 0x3FF) == 0 ? 1 : 0,
                          &cmd_prefix);
            last_command->cmd_prefix_ = cmd_prefix;
        }
    }

    /* BrotliCreateBackwardReferences (q2..q9) is the real port in
       BackwardReferences.cs; BrotliCreate[Hq]ZopfliBackwardReferences
       (q10/q11) live in BackwardReferencesHq.cs. Both imported via
       `using static`. */

    /*
       Processes the accumulated input data and sets |*out_size| to the length of
       the new output meta-block, or to zero if no new output meta-block has been
       created (in this case the processed input data is buffered internally).
       If |*out_size| is positive, |*output| points to the start of the output
       data. If |is_last| or |force_flush| is BROTLI_TRUE, an output meta-block is
       always created. However, until |is_last| is BROTLI_TRUE encoder may retain up
       to 7 bits of the last byte of output. Byte-alignment could be enforced by
       emitting an empty meta-data block.
       Returns BROTLI_FALSE if the size of the input data is larger than
       input_block_size().
     */
    private static int EncodeData(
        BrotliEncoderState* s, int is_last,
        int force_flush, nuint* out_size, byte** output)
    {
        ulong delta = UnprocessedInputSize(s);
        uint bytes = (uint)delta;
        uint wrapped_last_processed_pos = WrapPosition(s->last_processed_pos_);
        byte* data;
        uint mask;
        MemoryManager* m = &s->memory_manager_;
        ContextType literal_context_mode;
        byte* literal_context_lut;
        bool fast_compress =
            s->@params.quality == FAST_ONE_PASS_COMPRESSION_QUALITY ||
            s->@params.quality == FAST_TWO_PASS_COMPRESSION_QUALITY;

        data = s->ringbuffer_.buffer_;
        mask = s->ringbuffer_.mask_;

        if (delta == 0)
        {  /* No new input; still might want to flush or finish. */
            if (data == null)
            {  /* No input has been processed so far. */
                if (is_last != 0)
                {  /* Emit complete finalized stream. */
                    s->last_bytes_ |= (ushort)(3u << s->last_bytes_bits_);
                    s->last_bytes_bits_ = (byte)(s->last_bytes_bits_ + 2u);
                    s->tiny_buf_.u8[0] = (byte)s->last_bytes_;
                    s->tiny_buf_.u8[1] = (byte)(s->last_bytes_ >> 8);
                    *output = s->tiny_buf_.u8;
                    *out_size = ((nuint)s->last_bytes_bits_ + 7u) >> 3;
                    return BROTLI_TRUE;
                }
                else
                {  /* No data, not last -> no-op. */
                    *out_size = 0;
                    return BROTLI_TRUE;
                }
            }
            else
            {
                /* Fast compress performs flush every block -> flush is no-op. */
                if (is_last == 0 && (force_flush == 0 || fast_compress))
                {  /* Another no-op. */
                    *out_size = 0;
                    return BROTLI_TRUE;
                }
            }
        }

        if (s->@params.quality > s->@params.dictionary.max_quality) return BROTLI_FALSE;
        /* Adding more blocks after "last" block is forbidden. */
        if (s->is_last_block_emitted_ != 0) return BROTLI_FALSE;
        if (is_last != 0) s->is_last_block_emitted_ = BROTLI_TRUE;

        if (delta > InputBlockSize(s))
        {
            return BROTLI_FALSE;
        }
        if (s->@params.quality == FAST_TWO_PASS_COMPRESSION_QUALITY &&
            s->command_buf_ == null)
        {
            s->command_buf_ =
                BROTLI_ALLOC<uint>(m, kCompressFragmentTwoPassBlockSize);
            s->literal_buf_ =
                BROTLI_ALLOC<byte>(m, kCompressFragmentTwoPassBlockSize);
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(s->command_buf_) ||
                BROTLI_IS_NULL(s->literal_buf_))
            {
                return BROTLI_FALSE;
            }
        }

        if (fast_compress)
        {
            byte* storage;
            nuint storage_ix = s->last_bytes_bits_;
            nuint table_size;
            int* table;

            storage = GetBrotliStorage(s, 2 * bytes + 503);
            if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
            storage[0] = (byte)s->last_bytes_;
            storage[1] = (byte)(s->last_bytes_ >> 8);
            table = GetHashTable(s, s->@params.quality, bytes, &table_size);
            if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
            if (s->@params.quality == FAST_ONE_PASS_COMPRESSION_QUALITY)
            {
                CompressFragment.BrotliCompressFragmentFast(
                    s->one_pass_arena_, &data[wrapped_last_processed_pos & mask],
                    bytes, is_last,
                    table, table_size,
                    &storage_ix, storage);
                if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
            }
            else
            {
                BrotliCompressFragmentTwoPass(
                    s->two_pass_arena_, &data[wrapped_last_processed_pos & mask],
                    bytes, is_last,
                    s->command_buf_, s->literal_buf_,
                    table, table_size,
                    &storage_ix, storage);
                if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
            }
            s->last_bytes_ = storage[storage_ix >> 3];
            s->last_bytes_bits_ = (byte)(storage_ix & 7u);
            UpdateLastProcessedPos(s);
            *output = &storage[0];
            *out_size = storage_ix >> 3;
            return BROTLI_TRUE;
        }

        {
            /* Theoretical max number of commands is 1 per 2 bytes. */
            nuint newsize = s->num_commands_ + bytes / 2 + 1;
            if (newsize > s->cmd_alloc_size_)
            {
                Command* new_commands;
                /* Reserve a bit more memory to allow merging with a next block
                   without reallocation: that would impact speed. */
                newsize += (bytes / 4) + 16;
                s->cmd_alloc_size_ = newsize;
                new_commands = BROTLI_ALLOC<Command>(m, newsize);
                if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(new_commands)) return BROTLI_FALSE;
                if (s->commands_ != null)
                {
                    Buffer.MemoryCopy(s->commands_, new_commands,
                        (nuint)sizeof(Command) * newsize,
                        (nuint)sizeof(Command) * s->num_commands_);
                    BROTLI_FREE(m, ref s->commands_);
                }
                s->commands_ = new_commands;
            }
        }

        InitOrStitchToPreviousBlock(m, &s->hasher_, data, mask, &s->@params,
            wrapped_last_processed_pos, bytes, is_last);

        literal_context_mode = ChooseContextMode(
            &s->@params, data, WrapPosition(s->last_flush_pos_),
            mask, (nuint)(s->input_pos_ - s->last_flush_pos_));
        literal_context_lut = BROTLI_CONTEXT_LUT((nuint)literal_context_mode);

        if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;

        if (s->num_commands_ != 0 && s->last_insert_len_ == 0)
        {
            ExtendLastCommand(s, &bytes, &wrapped_last_processed_pos);
        }

        if (s->@params.quality == ZOPFLIFICATION_QUALITY)
        {
            BrotliCreateZopfliBackwardReferences(m, bytes, wrapped_last_processed_pos,
                data, mask, literal_context_lut, &s->@params,
                &s->hasher_, s->dist_cache_,
                &s->last_insert_len_, &s->commands_[s->num_commands_],
                &s->num_commands_, &s->num_literals_);
            if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
        }
        else if (s->@params.quality == HQ_ZOPFLIFICATION_QUALITY)
        {
            BrotliCreateHqZopfliBackwardReferences(m, bytes, wrapped_last_processed_pos,
                data, mask, literal_context_lut, &s->@params,
                &s->hasher_, s->dist_cache_,
                &s->last_insert_len_, &s->commands_[s->num_commands_],
                &s->num_commands_, &s->num_literals_);
            if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
        }
        else
        {
            BrotliCreateBackwardReferences(bytes, wrapped_last_processed_pos,
                data, mask, literal_context_lut, &s->@params,
                &s->hasher_, s->dist_cache_,
                &s->last_insert_len_, &s->commands_[s->num_commands_],
                &s->num_commands_, &s->num_literals_);
        }

        {
            nuint max_length = MaxMetablockSize(&s->@params);
            nuint max_literals = max_length / 8;
            nuint max_commands = max_length / 8;
            nuint processed_bytes = (nuint)(s->input_pos_ - s->last_flush_pos_);
            /* If maximal possible additional block doesn't fit metablock, flush now. */
            /* TODO(eustas): Postpone decision until next block arrives? */
            bool next_input_fits_metablock =
                processed_bytes + InputBlockSize(s) <= max_length;
            /* If block splitting is not used, then flush as soon as there is some
               amount of commands / literals produced. */
            bool should_flush =
                s->@params.quality < MIN_QUALITY_FOR_BLOCK_SPLIT &&
                s->num_literals_ + s->num_commands_ >= MAX_NUM_DELAYED_SYMBOLS;
            if (is_last == 0 && force_flush == 0 && !should_flush &&
                next_input_fits_metablock &&
                s->num_literals_ < max_literals &&
                s->num_commands_ < max_commands)
            {
                /* Merge with next input block. Everything will happen later. */
                if (UpdateLastProcessedPos(s))
                {
                    HasherReset(&s->hasher_);
                }
                *out_size = 0;
                return BROTLI_TRUE;
            }
        }

        /* Create the last insert-only command. */
        if (s->last_insert_len_ > 0)
        {
            InitInsertCommand(&s->commands_[s->num_commands_++], s->last_insert_len_);
            s->num_literals_ += s->last_insert_len_;
            s->last_insert_len_ = 0;
        }

        if (is_last == 0 && s->input_pos_ == s->last_flush_pos_)
        {
            /* We have no new input data and we don't have to finish the stream, so
               nothing to do. */
            *out_size = 0;
            return BROTLI_TRUE;
        }
        {
            uint metablock_size =
                (uint)(s->input_pos_ - s->last_flush_pos_);
            byte* storage = GetBrotliStorage(s, 2 * metablock_size + 503);
            nuint storage_ix = s->last_bytes_bits_;
            if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
            storage[0] = (byte)s->last_bytes_;
            storage[1] = (byte)(s->last_bytes_ >> 8);
            WriteMetaBlockInternal(
                m, data, mask, s->last_flush_pos_, metablock_size, is_last,
                literal_context_mode, &s->@params, s->prev_byte_, s->prev_byte2_,
                s->num_literals_, s->num_commands_, s->commands_, s->saved_dist_cache_,
                s->dist_cache_, &storage_ix, storage);
            if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
            s->last_bytes_ = storage[storage_ix >> 3];
            s->last_bytes_bits_ = (byte)(storage_ix & 7u);
            s->last_flush_pos_ = s->input_pos_;
            if (UpdateLastProcessedPos(s))
            {
                HasherReset(&s->hasher_);
            }
            if (s->last_flush_pos_ > 0)
            {
                s->prev_byte_ = data[((uint)s->last_flush_pos_ - 1) & mask];
            }
            if (s->last_flush_pos_ > 1)
            {
                s->prev_byte2_ = data[(uint)(s->last_flush_pos_ - 2) & mask];
            }
            s->num_commands_ = 0;
            s->num_literals_ = 0;
            /* Save the state of the distance cache in case we need to restore it for
               emitting an uncompressed block. */
            Buffer.MemoryCopy(s->dist_cache_, s->saved_dist_cache_,
                4 * sizeof(int), 4 * sizeof(int));
            *output = &storage[0];
            *out_size = storage_ix >> 3;
            return BROTLI_TRUE;
        }
    }

    /* Dumps remaining output bits and metadata header to |header|.
       Returns number of produced bytes.
       REQUIRED: |header| should be 8-byte aligned and at least 16 bytes long.
       REQUIRED: |block_size| <= (1 << 24). */
    private static nuint WriteMetadataHeader(
        BrotliEncoderState* s, nuint block_size, byte* header)
    {
        nuint storage_ix;
        storage_ix = s->last_bytes_bits_;
        header[0] = (byte)s->last_bytes_;
        header[1] = (byte)(s->last_bytes_ >> 8);
        s->last_bytes_ = 0;
        s->last_bytes_bits_ = 0;

        BrotliWriteBits(1, 0, &storage_ix, header);
        BrotliWriteBits(2, 3, &storage_ix, header);
        BrotliWriteBits(1, 0, &storage_ix, header);
        if (block_size == 0)
        {
            BrotliWriteBits(2, 0, &storage_ix, header);
        }
        else
        {
            uint nbits = (block_size == 1) ? 1 :
                (Log2FloorNonZero((nuint)((uint)block_size - 1)) + 1);
            uint nbytes = (nbits + 7) / 8;
            BrotliWriteBits(2, nbytes, &storage_ix, header);
            BrotliWriteBits(8 * nbytes, block_size - 1, &storage_ix, header);
        }
        return (storage_ix + 7u) >> 3;
    }

    internal static nuint BrotliEncoderMaxCompressedSize(nuint input_size)
    {
        /* [window bits / empty metadata] + N * [uncompressed] + [last empty] */
        nuint num_large_blocks = input_size >> 14;
        nuint overhead = 2 + (4 * num_large_blocks) + 3 + 1;
        nuint result = input_size + overhead;
        if (input_size == 0) return 2;
        return (result < input_size) ? 0 : result;
    }

    /* Wraps data to uncompressed brotli stream with minimal window size.
       |output| should point at region with at least BrotliEncoderMaxCompressedSize
       addressable bytes.
       Returns the length of stream. */
    private static nuint MakeUncompressedStream(
        byte* input, nuint input_size, byte* output)
    {
        nuint size = input_size;
        nuint result = 0;
        nuint offset = 0;
        if (input_size == 0)
        {
            output[0] = 6;
            return 1;
        }
        output[result++] = 0x21;  /* window bits = 10, is_last = false */
        output[result++] = 0x03;  /* empty metadata, padding */
        while (size > 0)
        {
            uint nibbles = 0;
            uint chunk_size;
            uint bits;
            chunk_size = (size > (1u << 24)) ? (1u << 24) : (uint)size;
            if (chunk_size > (1u << 16)) nibbles = (chunk_size > (1u << 20)) ? 2u : 1u;
            bits =
                (nibbles << 1) | ((chunk_size - 1) << 3) | (1u << (int)(19 + 4 * nibbles));
            output[result++] = (byte)bits;
            output[result++] = (byte)(bits >> 8);
            output[result++] = (byte)(bits >> 16);
            if (nibbles == 2) output[result++] = (byte)(bits >> 24);
            Buffer.MemoryCopy(&input[offset], &output[result], chunk_size, chunk_size);
            result += chunk_size;
            offset += chunk_size;
            size -= chunk_size;
        }
        output[result++] = 3;
        return result;
    }

    internal static int BrotliEncoderCompress(
        int quality, int lgwin, BrotliEncoderMode mode, nuint input_size,
        byte* input_buffer,
        nuint* encoded_size,
        byte* encoded_buffer)
    {
        BrotliEncoderState* s;
        nuint out_size = *encoded_size;
        byte* input_start = input_buffer;
        byte* output_start = encoded_buffer;
        nuint max_out_size = BrotliEncoderMaxCompressedSize(input_size);
        if (out_size == 0)
        {
            /* Output buffer needs at least one byte. */
            return BROTLI_FALSE;
        }
        if (input_size == 0)
        {
            /* Handle the special case of empty input. */
            *encoded_size = 1;
            *encoded_buffer = 6;
            return BROTLI_TRUE;
        }

        s = BrotliEncoderCreateInstance(0, 0, null);
        if (s == null)
        {
            return BROTLI_FALSE;
        }
        else
        {
            nuint available_in = input_size;
            byte* next_in = input_buffer;
            nuint available_out = *encoded_size;
            byte* next_out = encoded_buffer;
            nuint total_out = 0;
            int result = BROTLI_FALSE;
            /* TODO(eustas): check that parameters are sane. */
            BrotliEncoderSetParameter(s, BrotliEncoderParameter.Quality, (uint)quality);
            BrotliEncoderSetParameter(s, BrotliEncoderParameter.LgWin, (uint)lgwin);
            BrotliEncoderSetParameter(s, BrotliEncoderParameter.Mode, (uint)mode);
            BrotliEncoderSetParameter(s, BrotliEncoderParameter.SizeHint, (uint)input_size);
            if (lgwin > MaxWindowBits)
            {
                BrotliEncoderSetParameter(s, BrotliEncoderParameter.LargeWindow, BROTLI_TRUE);
            }
            try
            {
                result = BrotliEncoderCompressStream(s, BROTLI_OPERATION_FINISH,
                    &available_in, &next_in, &available_out, &next_out, &total_out);
            }
            finally
            {
                /* Not in the C (which cannot throw): release the instance even when a
                   deferred fence aborts the compression mid-stream. */
                if (BrotliEncoderIsFinished(s) == 0) result = 0;
                *encoded_size = total_out;
                BrotliEncoderDestroyInstance(s);
            }
            if (result == 0 || (max_out_size != 0 && *encoded_size > max_out_size))
            {
                goto fallback;
            }
            return BROTLI_TRUE;
        }
    fallback:
        *encoded_size = 0;
        if (max_out_size == 0) return BROTLI_FALSE;
        if (out_size >= max_out_size)
        {
            *encoded_size =
                MakeUncompressedStream(input_start, input_size, output_start);
            return BROTLI_TRUE;
        }
        return BROTLI_FALSE;
    }

    private static void InjectBytePaddingBlock(BrotliEncoderState* s)
    {
        uint seal = s->last_bytes_;
        nuint seal_bits = s->last_bytes_bits_;
        byte* destination;
        s->last_bytes_ = 0;
        s->last_bytes_bits_ = 0;
        /* is_last = 0, data_nibbles = 11, reserved = 0, meta_nibbles = 00 */
        seal |= 0x6u << (int)seal_bits;
        seal_bits += 6;
        /* If we have already created storage, then append to it.
           Storage is valid until next block is being compressed. */
        if (s->next_out_ != null)
        {
            destination = s->next_out_ + s->available_out_;
        }
        else
        {
            destination = s->tiny_buf_.u8;
            s->next_out_ = destination;
        }
        destination[0] = (byte)seal;
        if (seal_bits > 8) destination[1] = (byte)(seal >> 8);
        if (seal_bits > 16) destination[2] = (byte)(seal >> 16);
        s->available_out_ += (seal_bits + 7) >> 3;
    }

    /* Fills the |total_out|, if it is not NULL. */
    private static void SetTotalOut(BrotliEncoderState* s, nuint* total_out)
    {
        if (total_out != null)
        {
            /* Saturating conversion uint64_t -> size_t */
            nuint result = nuint.MaxValue;
            if (s->total_out_ < result)
            {
                result = (nuint)s->total_out_;
            }
            *total_out = result;
        }
    }

    /* Injects padding bits or pushes compressed data to output.
       Returns false if nothing is done. */
    private static int InjectFlushOrPushOutput(BrotliEncoderState* s,
        nuint* available_out, byte** next_out, nuint* total_out)
    {
        if (s->stream_state_ == BROTLI_STREAM_FLUSH_REQUESTED &&
            s->last_bytes_bits_ != 0)
        {
            InjectBytePaddingBlock(s);
            return BROTLI_TRUE;
        }

        if (s->available_out_ != 0 && *available_out != 0)
        {
            nuint copy_output_size =
                BROTLI_MIN(s->available_out_, *available_out);
            Buffer.MemoryCopy(s->next_out_, *next_out, copy_output_size, copy_output_size);
            *next_out += copy_output_size;
            *available_out -= copy_output_size;
            s->next_out_ += copy_output_size;
            s->available_out_ -= copy_output_size;
            s->total_out_ += copy_output_size;
            SetTotalOut(s, total_out);
            return BROTLI_TRUE;
        }

        return BROTLI_FALSE;
    }

    private static void CheckFlushComplete(BrotliEncoderState* s)
    {
        if (s->stream_state_ == BROTLI_STREAM_FLUSH_REQUESTED &&
            s->available_out_ == 0)
        {
            s->stream_state_ = BROTLI_STREAM_PROCESSING;
            s->next_out_ = null;
        }
    }

    private static int BrotliEncoderCompressStreamFast(
        BrotliEncoderState* s, BrotliEncoderOperation op, nuint* available_in,
        byte** next_in, nuint* available_out, byte** next_out,
        nuint* total_out)
    {
        nuint block_size_limit = (nuint)1 << s->@params.lgwin;
        nuint buf_size = BROTLI_MIN(kCompressFragmentTwoPassBlockSize,
            BROTLI_MIN(*available_in, block_size_limit));
        uint* tmp_command_buf = null;
        uint* command_buf = null;
        byte* tmp_literal_buf = null;
        byte* literal_buf = null;
        MemoryManager* m = &s->memory_manager_;
        if (s->@params.quality != FAST_ONE_PASS_COMPRESSION_QUALITY &&
            s->@params.quality != FAST_TWO_PASS_COMPRESSION_QUALITY)
        {
            return BROTLI_FALSE;
        }
        if (s->@params.quality == FAST_TWO_PASS_COMPRESSION_QUALITY)
        {
            if (s->command_buf_ == null && buf_size == kCompressFragmentTwoPassBlockSize)
            {
                s->command_buf_ =
                    BROTLI_ALLOC<uint>(m, kCompressFragmentTwoPassBlockSize);
                s->literal_buf_ =
                    BROTLI_ALLOC<byte>(m, kCompressFragmentTwoPassBlockSize);
                if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(s->command_buf_) ||
                    BROTLI_IS_NULL(s->literal_buf_))
                {
                    return BROTLI_FALSE;
                }
            }
            if (s->command_buf_ != null)
            {
                command_buf = s->command_buf_;
                literal_buf = s->literal_buf_;
            }
            else
            {
                tmp_command_buf = BROTLI_ALLOC<uint>(m, buf_size);
                tmp_literal_buf = BROTLI_ALLOC<byte>(m, buf_size);
                if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(tmp_command_buf) ||
                    BROTLI_IS_NULL(tmp_literal_buf))
                {
                    return BROTLI_FALSE;
                }
                command_buf = tmp_command_buf;
                literal_buf = tmp_literal_buf;
            }
        }

        while (true)
        {
            if (InjectFlushOrPushOutput(s, available_out, next_out, total_out) != 0)
            {
                continue;
            }

            /* Compress block only when internal output buffer is empty, stream is not
               finished, there is no pending flush request, and there is either
               additional input or pending operation. */
            if (s->available_out_ == 0 &&
                s->stream_state_ == BROTLI_STREAM_PROCESSING &&
                (*available_in != 0 || op != BROTLI_OPERATION_PROCESS))
            {
                nuint block_size = BROTLI_MIN(block_size_limit, *available_in);
                bool is_last =
                    (*available_in == block_size) && (op == BROTLI_OPERATION_FINISH);
                bool force_flush =
                    (*available_in == block_size) && (op == BROTLI_OPERATION_FLUSH);
                nuint max_out_size = 2 * block_size + 503;
                bool inplace = true;
                byte* storage = null;
                nuint storage_ix = s->last_bytes_bits_;
                nuint table_size;
                int* table;

                if (force_flush && block_size == 0)
                {
                    s->stream_state_ = BROTLI_STREAM_FLUSH_REQUESTED;
                    continue;
                }
                if (max_out_size <= *available_out)
                {
                    storage = *next_out;
                }
                else
                {
                    inplace = false;
                    storage = GetBrotliStorage(s, max_out_size);
                    if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
                }
                storage[0] = (byte)s->last_bytes_;
                storage[1] = (byte)(s->last_bytes_ >> 8);
                table = GetHashTable(s, s->@params.quality, block_size, &table_size);
                if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;

                if (s->@params.quality == FAST_ONE_PASS_COMPRESSION_QUALITY)
                {
                    CompressFragment.BrotliCompressFragmentFast(s->one_pass_arena_, *next_in,
                        block_size, is_last ? 1 : 0, table, table_size, &storage_ix, storage);
                    if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
                }
                else
                {
                    BrotliCompressFragmentTwoPass(s->two_pass_arena_, *next_in,
                        block_size, is_last ? 1 : 0, command_buf, literal_buf,
                        table, table_size, &storage_ix, storage);
                    if (BROTLI_IS_OOM(m)) return BROTLI_FALSE;
                }
                if (block_size != 0)
                {
                    *next_in += block_size;
                    *available_in -= block_size;
                    s->total_in_ += block_size;
                }
                if (inplace)
                {
                    nuint out_bytes = storage_ix >> 3;
                    *next_out += out_bytes;
                    *available_out -= out_bytes;
                    s->total_out_ += out_bytes;
                    SetTotalOut(s, total_out);
                }
                else
                {
                    nuint out_bytes = storage_ix >> 3;
                    s->next_out_ = storage;
                    s->available_out_ = out_bytes;
                }
                s->last_bytes_ = storage[storage_ix >> 3];
                s->last_bytes_bits_ = (byte)(storage_ix & 7u);

                if (force_flush) s->stream_state_ = BROTLI_STREAM_FLUSH_REQUESTED;
                if (is_last) s->stream_state_ = BROTLI_STREAM_FINISHED;
                continue;
            }
            break;
        }
        BROTLI_FREE(m, ref tmp_command_buf);
        BROTLI_FREE(m, ref tmp_literal_buf);
        CheckFlushComplete(s);
        return BROTLI_TRUE;
    }

    private static int ProcessMetadata(
        BrotliEncoderState* s, nuint* available_in, byte** next_in,
        nuint* available_out, byte** next_out, nuint* total_out)
    {
        if (*available_in > (1u << 24)) return BROTLI_FALSE;
        /* Switch to metadata block workflow, if required. */
        if (s->stream_state_ == BROTLI_STREAM_PROCESSING)
        {
            s->remaining_metadata_bytes_ = (uint)*available_in;
            s->stream_state_ = BROTLI_STREAM_METADATA_HEAD;
        }
        if (s->stream_state_ != BROTLI_STREAM_METADATA_HEAD &&
            s->stream_state_ != BROTLI_STREAM_METADATA_BODY)
        {
            return BROTLI_FALSE;
        }

        while (true)
        {
            if (InjectFlushOrPushOutput(s, available_out, next_out, total_out) != 0)
            {
                continue;
            }
            if (s->available_out_ != 0) break;

            if (s->input_pos_ != s->last_flush_pos_)
            {
                {
                    int result = EncodeData(s, BROTLI_FALSE, BROTLI_TRUE,
                        &s->available_out_, &s->next_out_);
                    if (result == 0) return BROTLI_FALSE;
                }
                continue;
            }

            if (s->stream_state_ == BROTLI_STREAM_METADATA_HEAD)
            {
                s->next_out_ = s->tiny_buf_.u8;
                s->available_out_ =
                    WriteMetadataHeader(s, s->remaining_metadata_bytes_, s->next_out_);
                s->stream_state_ = BROTLI_STREAM_METADATA_BODY;
                continue;
            }
            else
            {
                /* Exit workflow only when there is no more input and no more output.
                   Otherwise client may continue producing empty metadata blocks. */
                if (s->remaining_metadata_bytes_ == 0)
                {
                    s->remaining_metadata_bytes_ = BROTLI_UINT32_MAX;
                    s->stream_state_ = BROTLI_STREAM_PROCESSING;
                    break;
                }
                if (*available_out != 0)
                {
                    /* Directly copy input to output. */
                    uint copy = (uint)BROTLI_MIN(
                        s->remaining_metadata_bytes_, *available_out);
                    Buffer.MemoryCopy(*next_in, *next_out, copy, copy);
                    *next_in += copy;
                    *available_in -= copy;
                    s->total_in_ += copy;  /* not actually data input, though */
                    s->remaining_metadata_bytes_ -= copy;
                    *next_out += copy;
                    *available_out -= copy;
                }
                else
                {
                    /* This guarantees progress in "TakeOutput" workflow. */
                    uint copy = BROTLI_MIN(s->remaining_metadata_bytes_, 16u);
                    s->next_out_ = s->tiny_buf_.u8;
                    Buffer.MemoryCopy(*next_in, s->next_out_, copy, copy);
                    *next_in += copy;
                    *available_in -= copy;
                    s->total_in_ += copy;  /* not actually data input, though */
                    s->remaining_metadata_bytes_ -= copy;
                    s->available_out_ = copy;
                }
                continue;
            }
        }

        return BROTLI_TRUE;
    }

    private static void UpdateSizeHint(BrotliEncoderState* s, nuint available_in)
    {
        if (s->@params.size_hint == 0)
        {
            ulong delta = UnprocessedInputSize(s);
            ulong tail = available_in;
            uint limit = 1u << 30;
            uint total;
            if ((delta >= limit) || (tail >= limit) || ((delta + tail) >= limit))
            {
                total = limit;
            }
            else
            {
                total = (uint)(delta + tail);
            }
            s->@params.size_hint = total;
        }
    }

    internal static int BrotliEncoderCompressStream(
        BrotliEncoderState* s, BrotliEncoderOperation op, nuint* available_in,
        byte** next_in, nuint* available_out, byte** next_out,
        nuint* total_out)
    {
        if (EnsureInitialized(s) == 0) return BROTLI_FALSE;

        /* Unfinished metadata block; check requirements. */
        if (s->remaining_metadata_bytes_ != BROTLI_UINT32_MAX)
        {
            if (*available_in != s->remaining_metadata_bytes_) return BROTLI_FALSE;
            if (op != BROTLI_OPERATION_EMIT_METADATA) return BROTLI_FALSE;
        }

        if (op == BROTLI_OPERATION_EMIT_METADATA)
        {
            UpdateSizeHint(s, 0);  /* First data metablock might be emitted here. */
            return ProcessMetadata(
                s, available_in, next_in, available_out, next_out, total_out);
        }

        if (s->stream_state_ == BROTLI_STREAM_METADATA_HEAD ||
            s->stream_state_ == BROTLI_STREAM_METADATA_BODY)
        {
            return BROTLI_FALSE;
        }

        if (s->stream_state_ != BROTLI_STREAM_PROCESSING && *available_in != 0)
        {
            return BROTLI_FALSE;
        }
        if (s->@params.quality == FAST_ONE_PASS_COMPRESSION_QUALITY ||
            s->@params.quality == FAST_TWO_PASS_COMPRESSION_QUALITY)
        {
            return BrotliEncoderCompressStreamFast(s, op, available_in, next_in,
                available_out, next_out, total_out);
        }
        while (true)
        {
            nuint remaining_block_size = RemainingInputBlockSize(s);
            /* Shorten input to flint size. */
            if (s->flint_ >= 0 && remaining_block_size > (nuint)s->flint_)
            {
                remaining_block_size = (nuint)s->flint_;
            }

            if (remaining_block_size != 0 && *available_in != 0)
            {
                nuint copy_input_size =
                    BROTLI_MIN(remaining_block_size, *available_in);
                CopyInputToRingBuffer(s, copy_input_size, *next_in);
                *next_in += copy_input_size;
                *available_in -= copy_input_size;
                s->total_in_ += copy_input_size;
                if (s->flint_ > 0) s->flint_ = (sbyte)(s->flint_ - (int)copy_input_size);
                continue;
            }

            if (InjectFlushOrPushOutput(s, available_out, next_out, total_out) != 0)
            {
                /* Exit the "emit flint" workflow. */
                if (s->flint_ == (sbyte)BROTLI_FLINT_WAITING_FOR_FLUSHING)
                {
                    CheckFlushComplete(s);
                    if (s->stream_state_ == BROTLI_STREAM_PROCESSING)
                    {
                        s->flint_ = (sbyte)BROTLI_FLINT_DONE;
                    }
                }
                continue;
            }

            /* Compress data only when internal output buffer is empty, stream is not
               finished and there is no pending flush request. */
            if (s->available_out_ == 0 &&
                s->stream_state_ == BROTLI_STREAM_PROCESSING)
            {
                if (remaining_block_size == 0 || op != BROTLI_OPERATION_PROCESS)
                {
                    int is_last =
                        ((*available_in == 0) && op == BROTLI_OPERATION_FINISH) ? 1 : 0;
                    int force_flush =
                        ((*available_in == 0) && op == BROTLI_OPERATION_FLUSH) ? 1 : 0;
                    int result;
                    /* Force emitting (uncompressed) piece containing flint. */
                    if (is_last == 0 && s->flint_ == 0)
                    {
                        s->flint_ = (sbyte)BROTLI_FLINT_WAITING_FOR_FLUSHING;
                        force_flush = 1;
                    }
                    UpdateSizeHint(s, *available_in);
                    result = EncodeData(s, is_last, force_flush,
                        &s->available_out_, &s->next_out_);
                    if (result == 0) return BROTLI_FALSE;
                    if (force_flush != 0) s->stream_state_ = BROTLI_STREAM_FLUSH_REQUESTED;
                    if (is_last != 0) s->stream_state_ = BROTLI_STREAM_FINISHED;
                    continue;
                }
            }
            break;
        }
        CheckFlushComplete(s);
        return BROTLI_TRUE;
    }

    internal static int BrotliEncoderIsFinished(BrotliEncoderState* s)
    {
        return (s->stream_state_ == BROTLI_STREAM_FINISHED &&
            BrotliEncoderHasMoreOutput(s) == 0) ? BROTLI_TRUE : BROTLI_FALSE;
    }

    internal static int BrotliEncoderHasMoreOutput(BrotliEncoderState* s)
    {
        return s->available_out_ != 0 ? BROTLI_TRUE : BROTLI_FALSE;
    }

    internal static byte* BrotliEncoderTakeOutput(BrotliEncoderState* s, nuint* size)
    {
        nuint consumed_size = s->available_out_;
        byte* result = s->next_out_;
        if (*size != 0)
        {
            consumed_size = BROTLI_MIN(*size, s->available_out_);
        }
        if (consumed_size != 0)
        {
            s->next_out_ += consumed_size;
            s->available_out_ -= consumed_size;
            s->total_out_ += consumed_size;
            CheckFlushComplete(s);
            *size = consumed_size;
        }
        else
        {
            *size = 0;
            result = null;
        }
        return result;
    }

    internal static uint BrotliEncoderVersion()
    {
        return BrotliConstants.Version;  /* BROTLI_VERSION */
    }

    internal static nuint BrotliEncoderEstimatePeakMemoryUsage(int quality, int lgwin,
                                                               nuint input_size)
    {
        BrotliEncoderParams @params;
        /* BROTLI_ENCODER_MEMORY_MANAGER_SLOTS == 0 in the EXIT_ON_OOM build, so
           memory_manager_size == 0 (it only feeds the q>=2 branch anyway). */
        BrotliEncoderInitParams(&@params);
        @params.quality = quality;
        @params.lgwin = lgwin;
        @params.size_hint = input_size;
        @params.large_window = lgwin > MaxWindowBits ? 1 : 0;
        SanitizeParams(&@params);
        @params.lgblock = ComputeLgBlock(&@params);
        ChooseHasher(&@params, &@params.hasher);
        if (@params.quality == FAST_ONE_PASS_COMPRESSION_QUALITY ||
            @params.quality == FAST_TWO_PASS_COMPRESSION_QUALITY)
        {
            nuint state_size = (nuint)sizeof(BrotliEncoderState);
            nuint block_size = BROTLI_MIN(input_size, (nuint)1 << @params.lgwin);
            nuint hash_table_size =
                HashTableSize(MaxHashTableSize(@params.quality), block_size);
            nuint hash_size =
                (hash_table_size < (1u << 10)) ? 0 : sizeof(int) * hash_table_size;
            nuint cmdbuf_size = @params.quality == FAST_TWO_PASS_COMPRESSION_QUALITY ?
                5 * BROTLI_MIN(block_size, (nuint)1 << 17) : 0;
            if (@params.quality == FAST_ONE_PASS_COMPRESSION_QUALITY)
            {
                state_size += (nuint)sizeof(BrotliOnePassArena);
            }
            else
            {
                state_size += (nuint)sizeof(BrotliTwoPassArena);
            }
            return hash_size + cmdbuf_size + state_size;
        }
        else
        {
            nuint short_ringbuffer_size = (nuint)1 << @params.lgblock;
            int ringbuffer_bits = ComputeRbBits(&@params);
            nuint ringbuffer_size = input_size < short_ringbuffer_size ?
                input_size : ((nuint)1u << ringbuffer_bits) + short_ringbuffer_size;
            nuint* hash_size = stackalloc nuint[4] { 0, 0, 0, 0 };
            nuint metablock_size =
                BROTLI_MIN(input_size, MaxMetablockSize(&@params));
            nuint inputblock_size =
                BROTLI_MIN(input_size, (nuint)1 << @params.lgblock);
            nuint cmdbuf_size = metablock_size * 2 + inputblock_size * 6;
            nuint outbuf_size = metablock_size * 2 + 503;
            nuint histogram_size = 0;
            HasherSize(&@params, BROTLI_TRUE, input_size, hash_size);
            if (@params.quality < MIN_QUALITY_FOR_BLOCK_SPLIT)
            {
                cmdbuf_size = BROTLI_MIN(cmdbuf_size,
                    (nuint)MAX_NUM_DELAYED_SYMBOLS * (nuint)sizeof(Command) + inputblock_size * 12);
            }
            if (@params.quality >= MIN_QUALITY_FOR_HQ_BLOCK_SPLITTING)
            {
                /* Only a very rough estimation, based on enwik8. */
                histogram_size = (nuint)200 << 20;
            }
            else if (@params.quality >= MIN_QUALITY_FOR_BLOCK_SPLIT)
            {
                nuint literal_histograms =
                    BROTLI_MIN(metablock_size / 6144, 256);
                nuint command_histograms =
                    BROTLI_MIN(metablock_size / 6144, 256);
                nuint distance_histograms =
                    BROTLI_MIN(metablock_size / 6144, 256);
                histogram_size = literal_histograms * (nuint)sizeof(HistogramLiteral) +
                                 command_histograms * (nuint)sizeof(HistogramCommand) +
                                 distance_histograms * (nuint)sizeof(HistogramDistance);
            }
            /* memory_manager_size == 0 in the EXIT_ON_OOM build. */
            return (ringbuffer_size +
                    hash_size[0] + hash_size[1] + hash_size[2] + hash_size[3] +
                    cmdbuf_size +
                    outbuf_size +
                    histogram_size);
        }
    }
}
