// Port of c/enc/compress_fragment.{h,c} (brotli v1.1.0): fast one-pass
// encoding of an input fragment (quality 0), independently from the input
// history. Adapted in C from snappy's CompressFragment().
//
// The C's FOR_TABLE_BITS_-baked BrotliCompressFragmentFastImpl{9,11,13,15}
// wrappers are kept so the dispatch shape matches the C exactly.
//
// Goto discipline (PORTING.md): the emit_commands / trawl / emit_remainder /
// next_block labels and their jumps are preserved 1:1.

using System.Runtime.CompilerServices;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.BrotliBitStream;
using static DnBrotli.Enc.EntropyEncode;
using static DnBrotli.Enc.FastLog;
using static DnBrotli.Enc.FindMatchLength;
using static DnBrotli.Enc.WriteBits;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

/// <summary><c>struct BrotliOnePassArena</c>. Lives in unmanaged memory only;
/// the embedded HuffmanTree array is a flattened fixed ulong buffer with a
/// typed accessor (PORTING.md).</summary>
internal unsafe struct BrotliOnePassArena
{
    public fixed byte lit_depth[256];
    public fixed ushort lit_bits[256];

    /* Command and distance prefix codes (each 64 symbols, stored back-to-back)
       used for the next block. The command prefix code is over a smaller alphabet
       with the following 64 symbols:
          0 - 15: insert length code 0, copy length code 0 - 15, same distance
         16 - 39: insert length code 0, copy length code 0 - 23
         40 - 63: insert length code 0 - 23, copy length code 0
       Note that symbols 16 and 40 represent the same code in the full alphabet,
       but we do not use either of them. */
    public fixed byte cmd_depth[128];
    public fixed ushort cmd_bits[128];
    public fixed uint cmd_histo[128];

    /* The compressed form of the command and distance prefix codes for the next
       block. */
    public fixed byte cmd_code[512];
    public nuint cmd_code_numbits;

    /* HuffmanTree tree[2 * BROTLI_NUM_LITERAL_SYMBOLS + 1] (513 entries), flattened.
       Capacity is 12 bytes per entry, not 8: under .NET sizeof(HuffmanTree) == 8
       (uint + short + short, packed), but the dn2cpp backend widens small struct
       fields to int32, making sizeof(HuffmanTree) == 12 — the buffer must hold
       either. (3 * N + 1) / 2 ulong slots >= 12 * N bytes. */
    public fixed ulong tree_[(3 * (2 * BROTLI_NUM_LITERAL_SYMBOLS + 1) + 1) / 2];
    public fixed uint histogram[256];
    public fixed byte tmp_depth[BROTLI_NUM_COMMAND_SYMBOLS];
    public fixed ushort tmp_bits[64];

    /// <summary><c>HuffmanTree tree[2 * BROTLI_NUM_LITERAL_SYMBOLS + 1]</c>. Arena
    /// instances live in unmanaged memory only, so the pointer never dangles.</summary>
    public HuffmanTree* tree
    {
        get
        {
            fixed (ulong* p = tree_)
            {
                return (HuffmanTree*)p;
            }
        }
    }
}

internal static unsafe class CompressFragment
{
    private const long MAX_DISTANCE = (1L << 18) - BROTLI_WINDOW_GAP;  /* BROTLI_MAX_BACKWARD_LIMIT(18) */
    private const int BROTLI_WINDOW_GAP = (int)WindowGap;

    /* kHashMul32 multiplier has these properties:
       * The multiplier must be odd. Otherwise we may lose the highest bit.
       * No long streaks of ones or zeros.
       * There is no effort to ensure that it is a prime, the oddity is enough
         for this use.
       * The number has been tuned heuristically against compression benchmarks. */
    private const uint kHashMul32 = 0x1E35A7BD;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Hash(byte* p, nuint shift)
    {
        ulong h = (BROTLI_UNALIGNED_LOAD64LE(p) << 24) * kHashMul32;
        return (uint)(h >> (int)shift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashBytesAtOffset(ulong v, int offset, nuint shift)
    {
        ulong h = ((v >> (8 * offset)) << 24) * kHashMul32;
        return (uint)(h >> (int)shift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMatch(byte* p1, byte* p2)
    {
        return BROTLI_UNALIGNED_LOAD32LE(p1) == BROTLI_UNALIGNED_LOAD32LE(p2) &&
            p1[4] == p2[4];
    }

    /* Builds a literal prefix code into "depths" and "bits" based on the statistics
       of the "input" string and stores it into the bit stream.
       Note that the prefix code here is built from the pre-LZ77 input, therefore
       we can only approximate the statistics of the actual literal stream.
       Moreover, for long inputs we build a histogram from a sample of the input
       and thus have to assign a non-zero depth for each literal.
       Returns estimated compression ratio millibytes/char for encoding given input
       with generated code. */
    private static nuint BuildAndStoreLiteralPrefixCode(BrotliOnePassArena* s,
                                                        byte* input,
                                                        nuint input_size,
                                                        byte* depths,
                                                        ushort* bits,
                                                        nuint* storage_ix,
                                                        byte* storage)
    {
        uint* histogram = s->histogram;
        nuint histogram_total;
        nuint i;
        new Span<uint>(histogram, 256).Clear();

        if (input_size < (1 << 15))
        {
            for (i = 0; i < input_size; ++i)
            {
                ++histogram[input[i]];
            }
            histogram_total = input_size;
            for (i = 0; i < 256; ++i)
            {
                /* We weigh the first 11 samples with weight 3 to account for the
                   balancing effect of the LZ77 phase on the histogram. */
                uint adjust = 2 * BROTLI_MIN(histogram[i], 11u);
                histogram[i] += adjust;
                histogram_total += adjust;
            }
        }
        else
        {
            const nuint kSampleRate = 29;
            for (i = 0; i < input_size; i += kSampleRate)
            {
                ++histogram[input[i]];
            }
            histogram_total = (input_size + kSampleRate - 1) / kSampleRate;
            for (i = 0; i < 256; ++i)
            {
                /* We add 1 to each population count to avoid 0 bit depths (since this is
                   only a sample and we don't know if the symbol appears or not), and we
                   weigh the first 11 samples with weight 3 to account for the balancing
                   effect of the LZ77 phase on the histogram (more frequent symbols are
                   more likely to be in backward references instead as literals). */
                uint adjust = 1 + 2 * BROTLI_MIN(histogram[i], 11u);
                histogram[i] += adjust;
                histogram_total += adjust;
            }
        }
        BrotliBuildAndStoreHuffmanTreeFast(s->tree, histogram, histogram_total,
                                           /* max_bits = */ 8,
                                           depths, bits, storage_ix, storage);
        {
            nuint literal_ratio = 0;
            for (i = 0; i < 256; ++i)
            {
                if (histogram[i] != 0) literal_ratio += histogram[i] * depths[i];
            }
            /* Estimated encoding ratio, millibytes per symbol. */
            return (literal_ratio * 125) / histogram_total;
        }
    }

    /* Builds a command and distance prefix code (each 64 symbols) into "depth" and
       "bits" based on "histogram" and stores it into the bit stream. */
    private static void BuildAndStoreCommandPrefixCode(BrotliOnePassArena* s,
        nuint* storage_ix, byte* storage)
    {
        uint* histogram = s->cmd_histo;
        byte* depth = s->cmd_depth;
        ushort* bits = s->cmd_bits;
        byte* tmp_depth = s->tmp_depth;
        ushort* tmp_bits = s->tmp_bits;
        new Span<byte>(tmp_depth, BROTLI_NUM_COMMAND_SYMBOLS).Clear();

        BrotliCreateHuffmanTree(histogram, 64, 15, s->tree, depth);
        BrotliCreateHuffmanTree(&histogram[64], 64, 14, s->tree, &depth[64]);
        /* We have to jump through a few hoops here in order to compute
           the command bits because the symbols are in a different order than in
           the full alphabet. This looks complicated, but having the symbols
           in this order in the command bits saves a few branches in the Emit*
           functions. */
        Buffer.MemoryCopy(depth, tmp_depth, 24, 24);
        Buffer.MemoryCopy(depth + 40, tmp_depth + 24, 8, 8);
        Buffer.MemoryCopy(depth + 24, tmp_depth + 32, 8, 8);
        Buffer.MemoryCopy(depth + 48, tmp_depth + 40, 8, 8);
        Buffer.MemoryCopy(depth + 32, tmp_depth + 48, 8, 8);
        Buffer.MemoryCopy(depth + 56, tmp_depth + 56, 8, 8);
        BrotliConvertBitDepthsToSymbols(tmp_depth, 64, tmp_bits);
        Buffer.MemoryCopy(tmp_bits, bits, 48, 48);
        Buffer.MemoryCopy(tmp_bits + 32, bits + 24, 16, 16);
        Buffer.MemoryCopy(tmp_bits + 48, bits + 32, 16, 16);
        Buffer.MemoryCopy(tmp_bits + 24, bits + 40, 16, 16);
        Buffer.MemoryCopy(tmp_bits + 40, bits + 48, 16, 16);
        Buffer.MemoryCopy(tmp_bits + 56, bits + 56, 16, 16);
        BrotliConvertBitDepthsToSymbols(&depth[64], 64, &bits[64]);
        {
            /* Create the bit length array for the full command alphabet. */
            nuint i;
            new Span<byte>(tmp_depth, 64).Clear();  /* only 64 first values were used */
            Buffer.MemoryCopy(depth, tmp_depth, 8, 8);
            Buffer.MemoryCopy(depth + 8, tmp_depth + 64, 8, 8);
            Buffer.MemoryCopy(depth + 16, tmp_depth + 128, 8, 8);
            Buffer.MemoryCopy(depth + 24, tmp_depth + 192, 8, 8);
            Buffer.MemoryCopy(depth + 32, tmp_depth + 384, 8, 8);
            for (i = 0; i < 8; ++i)
            {
                tmp_depth[128 + 8 * i] = depth[40 + i];
                tmp_depth[256 + 8 * i] = depth[48 + i];
                tmp_depth[448 + 8 * i] = depth[56 + i];
            }
            BrotliStoreHuffmanTree(
                tmp_depth, BROTLI_NUM_COMMAND_SYMBOLS, s->tree, storage_ix, storage);
        }
        BrotliStoreHuffmanTree(&depth[64], 64, s->tree, storage_ix, storage);
    }

    /* REQUIRES: insertlen < 6210 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitInsertLen(nuint insertlen,
                                      byte* depth,
                                      ushort* bits,
                                      uint* histo,
                                      nuint* storage_ix,
                                      byte* storage)
    {
        if (insertlen < 6)
        {
            nuint code = insertlen + 40;
            BrotliWriteBits(depth[code], bits[code], storage_ix, storage);
            ++histo[code];
        }
        else if (insertlen < 130)
        {
            nuint tail = insertlen - 2;
            uint nbits = Log2FloorNonZero(tail) - 1u;
            nuint prefix = tail >> (int)nbits;
            nuint inscode = ((nuint)nbits << 1) + prefix + 42;
            BrotliWriteBits(depth[inscode], bits[inscode], storage_ix, storage);
            BrotliWriteBits(nbits, tail - (prefix << (int)nbits), storage_ix, storage);
            ++histo[inscode];
        }
        else if (insertlen < 2114)
        {
            nuint tail = insertlen - 66;
            uint nbits = Log2FloorNonZero(tail);
            nuint code = nbits + 50;
            BrotliWriteBits(depth[code], bits[code], storage_ix, storage);
            BrotliWriteBits(nbits, tail - ((nuint)1 << (int)nbits), storage_ix, storage);
            ++histo[code];
        }
        else
        {
            BrotliWriteBits(depth[61], bits[61], storage_ix, storage);
            BrotliWriteBits(12, insertlen - 2114, storage_ix, storage);
            ++histo[61];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitLongInsertLen(nuint insertlen,
                                          byte* depth,
                                          ushort* bits,
                                          uint* histo,
                                          nuint* storage_ix,
                                          byte* storage)
    {
        if (insertlen < 22594)
        {
            BrotliWriteBits(depth[62], bits[62], storage_ix, storage);
            BrotliWriteBits(14, insertlen - 6210, storage_ix, storage);
            ++histo[62];
        }
        else
        {
            BrotliWriteBits(depth[63], bits[63], storage_ix, storage);
            BrotliWriteBits(24, insertlen - 22594, storage_ix, storage);
            ++histo[63];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitCopyLen(nuint copylen,
                                    byte* depth,
                                    ushort* bits,
                                    uint* histo,
                                    nuint* storage_ix,
                                    byte* storage)
    {
        if (copylen < 10)
        {
            BrotliWriteBits(
                depth[copylen + 14], bits[copylen + 14], storage_ix, storage);
            ++histo[copylen + 14];
        }
        else if (copylen < 134)
        {
            nuint tail = copylen - 6;
            uint nbits = Log2FloorNonZero(tail) - 1u;
            nuint prefix = tail >> (int)nbits;
            nuint code = ((nuint)nbits << 1) + prefix + 20;
            BrotliWriteBits(depth[code], bits[code], storage_ix, storage);
            BrotliWriteBits(nbits, tail - (prefix << (int)nbits), storage_ix, storage);
            ++histo[code];
        }
        else if (copylen < 2118)
        {
            nuint tail = copylen - 70;
            uint nbits = Log2FloorNonZero(tail);
            nuint code = nbits + 28;
            BrotliWriteBits(depth[code], bits[code], storage_ix, storage);
            BrotliWriteBits(nbits, tail - ((nuint)1 << (int)nbits), storage_ix, storage);
            ++histo[code];
        }
        else
        {
            BrotliWriteBits(depth[39], bits[39], storage_ix, storage);
            BrotliWriteBits(24, copylen - 2118, storage_ix, storage);
            ++histo[39];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitCopyLenLastDistance(nuint copylen,
                                                byte* depth,
                                                ushort* bits,
                                                uint* histo,
                                                nuint* storage_ix,
                                                byte* storage)
    {
        if (copylen < 12)
        {
            BrotliWriteBits(depth[copylen - 4], bits[copylen - 4], storage_ix, storage);
            ++histo[copylen - 4];
        }
        else if (copylen < 72)
        {
            nuint tail = copylen - 8;
            uint nbits = Log2FloorNonZero(tail) - 1;
            nuint prefix = tail >> (int)nbits;
            nuint code = ((nuint)nbits << 1) + prefix + 4;
            BrotliWriteBits(depth[code], bits[code], storage_ix, storage);
            BrotliWriteBits(nbits, tail - (prefix << (int)nbits), storage_ix, storage);
            ++histo[code];
        }
        else if (copylen < 136)
        {
            nuint tail = copylen - 8;
            nuint code = (tail >> 5) + 30;
            BrotliWriteBits(depth[code], bits[code], storage_ix, storage);
            BrotliWriteBits(5, tail & 31, storage_ix, storage);
            BrotliWriteBits(depth[64], bits[64], storage_ix, storage);
            ++histo[code];
            ++histo[64];
        }
        else if (copylen < 2120)
        {
            nuint tail = copylen - 72;
            uint nbits = Log2FloorNonZero(tail);
            nuint code = nbits + 28;
            BrotliWriteBits(depth[code], bits[code], storage_ix, storage);
            BrotliWriteBits(nbits, tail - ((nuint)1 << (int)nbits), storage_ix, storage);
            BrotliWriteBits(depth[64], bits[64], storage_ix, storage);
            ++histo[code];
            ++histo[64];
        }
        else
        {
            BrotliWriteBits(depth[39], bits[39], storage_ix, storage);
            BrotliWriteBits(24, copylen - 2120, storage_ix, storage);
            BrotliWriteBits(depth[64], bits[64], storage_ix, storage);
            ++histo[39];
            ++histo[64];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitDistance(nuint distance,
                                     byte* depth,
                                     ushort* bits,
                                     uint* histo,
                                     nuint* storage_ix, byte* storage)
    {
        nuint d = distance + 3;
        uint nbits = Log2FloorNonZero(d) - 1u;
        nuint prefix = (d >> (int)nbits) & 1;
        nuint offset = (2 + prefix) << (int)nbits;
        nuint distcode = 2 * ((nuint)nbits - 1) + prefix + 80;
        BrotliWriteBits(depth[distcode], bits[distcode], storage_ix, storage);
        BrotliWriteBits(nbits, d - offset, storage_ix, storage);
        ++histo[distcode];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitLiterals(byte* input, nuint len,
                                     byte* depth,
                                     ushort* bits,
                                     nuint* storage_ix, byte* storage)
    {
        nuint j;
        for (j = 0; j < len; j++)
        {
            byte lit = input[j];
            BrotliWriteBits(depth[lit], bits[lit], storage_ix, storage);
        }
    }

    /* REQUIRES: len <= 1 << 24. */
    private static void BrotliStoreMetaBlockHeader(
        nuint len, int is_uncompressed, nuint* storage_ix,
        byte* storage)
    {
        nuint nibbles = 6;
        /* ISLAST */
        BrotliWriteBits(1, 0, storage_ix, storage);
        if (len <= (1U << 16))
        {
            nibbles = 4;
        }
        else if (len <= (1U << 20))
        {
            nibbles = 5;
        }
        BrotliWriteBits(2, nibbles - 4, storage_ix, storage);
        BrotliWriteBits(nibbles * 4, len - 1, storage_ix, storage);
        /* ISUNCOMPRESSED */
        BrotliWriteBits(1, (ulong)is_uncompressed, storage_ix, storage);
    }

    private static void UpdateBits(nuint n_bits, uint bits, nuint pos,
        byte* array)
    {
        while (n_bits > 0)
        {
            nuint byte_pos = pos >> 3;
            nuint n_unchanged_bits = pos & 7;
            nuint n_changed_bits = BROTLI_MIN(n_bits, 8 - n_unchanged_bits);
            nuint total_bits = n_unchanged_bits + n_changed_bits;
            uint mask =
                (~((1u << (int)total_bits) - 1u)) | ((1u << (int)n_unchanged_bits) - 1u);
            uint unchanged_bits = array[byte_pos] & mask;
            uint changed_bits = bits & ((1u << (int)n_changed_bits) - 1u);
            array[byte_pos] =
                (byte)((changed_bits << (int)n_unchanged_bits) | unchanged_bits);
            n_bits -= n_changed_bits;
            bits >>= (int)n_changed_bits;
            pos += n_changed_bits;
        }
    }

    private static void RewindBitPosition(nuint new_storage_ix,
                                          nuint* storage_ix, byte* storage)
    {
        nuint bitpos = new_storage_ix & 7;
        nuint mask = (1u << (int)bitpos) - 1;
        storage[new_storage_ix >> 3] &= (byte)mask;
        *storage_ix = new_storage_ix;
    }

    private static bool ShouldMergeBlock(BrotliOnePassArena* s,
        byte* data, nuint len, byte* depths)
    {
        uint* histo = s->histogram;
        const nuint kSampleRate = 43;
        nuint i;
        new Span<uint>(histo, 256).Clear();
        for (i = 0; i < len; i += kSampleRate)
        {
            ++histo[data[i]];
        }
        {
            nuint total = (len + kSampleRate - 1) / kSampleRate;
            double r = (FastLog2(total) + 0.5) * (double)total + 200;
            for (i = 0; i < 256; ++i)
            {
                r -= (double)histo[i] * (depths[i] + FastLog2(histo[i]));
            }
            return r >= 0.0;
        }
    }

    /* Acceptable loss for uncompressible speedup is 2% */
    private const nuint MIN_RATIO = 980;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldUseUncompressedMode(
        byte* metablock_start, byte* next_emit,
        nuint insertlen, nuint literal_ratio)
    {
        nuint compressed = (nuint)(next_emit - metablock_start);
        if (compressed * 50 > insertlen)
        {
            return false;
        }
        else
        {
            return literal_ratio > MIN_RATIO;
        }
    }

    private static void EmitUncompressedMetaBlock(byte* begin, byte* end,
                                                  nuint storage_ix_start,
                                                  nuint* storage_ix, byte* storage)
    {
        nuint len = (nuint)(end - begin);
        RewindBitPosition(storage_ix_start, storage_ix, storage);
        BrotliStoreMetaBlockHeader(len, 1, storage_ix, storage);
        *storage_ix = (*storage_ix + 7u) & ~(nuint)7u;
        Buffer.MemoryCopy(begin, &storage[*storage_ix >> 3], len, len);
        *storage_ix += len << 3;
        storage[*storage_ix >> 3] = 0;
    }

    private static readonly uint[] kCmdHistoSeed =  /* [128] */
    {
        0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 0, 0, 0, 0,
    };

    private static void BrotliCompressFragmentFastImpl(
        BrotliOnePassArena* s, byte* input, nuint input_size,
        int is_last, int* table, nuint table_bits,
        nuint* storage_ix, byte* storage)
    {
        byte* cmd_depth = s->cmd_depth;
        ushort* cmd_bits = s->cmd_bits;
        uint* cmd_histo = s->cmd_histo;
        byte* lit_depth = s->lit_depth;
        ushort* lit_bits = s->lit_bits;
        byte* ip_end = null;

        /* "next_emit" is a pointer to the first byte that is not covered by a
           previous copy. Bytes between "next_emit" and the start of the next copy or
           the end of the input will be emitted as literal bytes. */
        byte* next_emit = input;
        /* Save the start of the first block for position and distance computations.
        */
        byte* base_ip = input;

        const nuint kFirstBlockSize = 3 << 15;
        const nuint kMergeBlockSize = 1 << 16;

        const nuint kInputMarginBytes = (nuint)BROTLI_WINDOW_GAP;
        const nuint kMinMatchLen = 5;

        byte* metablock_start = input;
        nuint block_size = BROTLI_MIN(input_size, kFirstBlockSize);
        nuint total_block_size = block_size;
        /* Save the bit position of the MLEN field of the meta-block header, so that
           we can update it later if we decide to extend this meta-block. */
        nuint mlen_storage_ix = *storage_ix + 3;

        nuint literal_ratio;

        byte* ip;
        int last_distance;

        nuint shift = 64u - table_bits;

        BrotliStoreMetaBlockHeader(block_size, 0, storage_ix, storage);
        /* No block splits, no contexts. */
        BrotliWriteBits(13, 0, storage_ix, storage);

        literal_ratio = BuildAndStoreLiteralPrefixCode(
            s, input, block_size, s->lit_depth, s->lit_bits, storage_ix, storage);

        {
            /* Store the pre-compressed command and distance prefix codes. */
            nuint i;
            for (i = 0; i + 7 < s->cmd_code_numbits; i += 8)
            {
                BrotliWriteBits(8, s->cmd_code[i >> 3], storage_ix, storage);
            }
        }
        BrotliWriteBits(s->cmd_code_numbits & 7,
                        s->cmd_code[s->cmd_code_numbits >> 3], storage_ix, storage);

    emit_commands:
        /* Initialize the command and distance histograms. We will gather
           statistics of command and distance codes during the processing
           of this block and use it to update the command and distance
           prefix codes for the next block. */
        fixed (uint* seed = kCmdHistoSeed)
        {
            Buffer.MemoryCopy(seed, s->cmd_histo, sizeof(uint) * 128, sizeof(uint) * 128);
        }

        /* "ip" is the input pointer. */
        ip = input;
        last_distance = -1;
        ip_end = input + block_size;

        if (block_size >= kInputMarginBytes)
        {
            /* For the last block, we need to keep a 16 bytes margin so that we can be
               sure that all distances are at most window size - 16.
               For all other blocks, we only need to keep a margin of 5 bytes so that
               we don't go over the block size with a copy. */
            nuint len_limit = BROTLI_MIN(block_size - kMinMatchLen,
                                       input_size - kInputMarginBytes);
            byte* ip_limit = input + len_limit;

            uint next_hash;
            for (next_hash = Hash(++ip, shift); ;)
            {
                /* Step 1: Scan forward in the input looking for a 5-byte-long match.
                   If we get close to exhausting the input then goto emit_remainder.

                   Heuristic match skipping: If 32 bytes are scanned with no matches
                   found, start looking only at every other byte. If 32 more bytes are
                   scanned, look at every third byte, etc.. When a match is found,
                   immediately go back to looking at every byte. This is a small loss
                   (~5% performance, ~0.1% density) for compressible data due to more
                   bookkeeping, but for non-compressible data (such as JPEG) it's a huge
                   win since the compressor quickly "realizes" the data is incompressible
                   and doesn't bother looking for matches everywhere.

                   The "skip" variable keeps track of how many bytes there are since the
                   last match; dividing it by 32 (i.e. right-shifting by five) gives the
                   number of bytes to move ahead for each iteration. */
                uint skip = 32;

                byte* next_ip = ip;
                byte* candidate;
            trawl:
                do
                {
                    uint hash = next_hash;
                    uint bytes_between_hash_lookups = skip++ >> 5;
                    ip = next_ip;
                    next_ip = ip + bytes_between_hash_lookups;
                    if (next_ip > ip_limit)
                    {
                        goto emit_remainder;
                    }
                    next_hash = Hash(next_ip, shift);
                    candidate = ip - last_distance;
                    if (IsMatch(ip, candidate))
                    {
                        if (candidate < ip)
                        {
                            table[hash] = (int)(ip - base_ip);
                            break;
                        }
                    }
                    candidate = base_ip + table[hash];

                    table[hash] = (int)(ip - base_ip);
                } while (!IsMatch(ip, candidate));

                /* Check copy distance. If candidate is not feasible, continue search.
                   Checking is done outside of hot loop to reduce overhead. */
                if (ip - candidate > MAX_DISTANCE) goto trawl;

                /* Step 2: Emit the found match together with the literal bytes from
                   "next_emit" to the bit stream, and then see if we can find a next match
                   immediately afterwards. Repeat until we find no match for the input
                   without emitting some literal bytes. */

                {
                    /* We have a 5-byte match at ip, and we need to emit bytes in
                       [next_emit, ip). */
                    byte* @base = ip;
                    nuint matched = 5 + FindMatchLengthWithLimit(
                        candidate + 5, ip + 5, (nuint)(ip_end - ip) - 5);
                    int distance = (int)(@base - candidate);  /* > 0 */
                    nuint insert = (nuint)(@base - next_emit);
                    ip += matched;
                    if (insert < 6210)
                    {
                        EmitInsertLen(insert, cmd_depth, cmd_bits, cmd_histo,
                                      storage_ix, storage);
                    }
                    else if (ShouldUseUncompressedMode(metablock_start, next_emit, insert,
                                                       literal_ratio))
                    {
                        EmitUncompressedMetaBlock(metablock_start, @base, mlen_storage_ix - 3,
                                                  storage_ix, storage);
                        input_size -= (nuint)(@base - input);
                        input = @base;
                        next_emit = input;
                        goto next_block;
                    }
                    else
                    {
                        EmitLongInsertLen(insert, cmd_depth, cmd_bits, cmd_histo,
                                          storage_ix, storage);
                    }
                    EmitLiterals(next_emit, insert, lit_depth, lit_bits,
                                 storage_ix, storage);
                    if (distance == last_distance)
                    {
                        BrotliWriteBits(cmd_depth[64], cmd_bits[64], storage_ix, storage);
                        ++cmd_histo[64];
                    }
                    else
                    {
                        EmitDistance((nuint)distance, cmd_depth, cmd_bits,
                                     cmd_histo, storage_ix, storage);
                        last_distance = distance;
                    }
                    EmitCopyLenLastDistance(matched, cmd_depth, cmd_bits, cmd_histo,
                                            storage_ix, storage);

                    next_emit = ip;
                    if (ip >= ip_limit)
                    {
                        goto emit_remainder;
                    }
                    /* We could immediately start working at ip now, but to improve
                       compression we first update "table" with the hashes of some positions
                       within the last copy. */
                    {
                        ulong input_bytes = BROTLI_UNALIGNED_LOAD64LE(ip - 3);
                        uint prev_hash = HashBytesAtOffset(input_bytes, 0, shift);
                        uint cur_hash = HashBytesAtOffset(input_bytes, 3, shift);
                        table[prev_hash] = (int)(ip - base_ip - 3);
                        prev_hash = HashBytesAtOffset(input_bytes, 1, shift);
                        table[prev_hash] = (int)(ip - base_ip - 2);
                        prev_hash = HashBytesAtOffset(input_bytes, 2, shift);
                        table[prev_hash] = (int)(ip - base_ip - 1);

                        candidate = base_ip + table[cur_hash];
                        table[cur_hash] = (int)(ip - base_ip);
                    }
                }

                while (IsMatch(ip, candidate))
                {
                    /* We have a 5-byte match at ip, and no need to emit any literal bytes
                       prior to ip. */
                    byte* @base = ip;
                    nuint matched = 5 + FindMatchLengthWithLimit(
                        candidate + 5, ip + 5, (nuint)(ip_end - ip) - 5);
                    if (ip - candidate > MAX_DISTANCE) break;
                    ip += matched;
                    last_distance = (int)(@base - candidate);  /* > 0 */
                    EmitCopyLen(matched, cmd_depth, cmd_bits, cmd_histo,
                                storage_ix, storage);
                    EmitDistance((nuint)last_distance, cmd_depth, cmd_bits,
                                 cmd_histo, storage_ix, storage);

                    next_emit = ip;
                    if (ip >= ip_limit)
                    {
                        goto emit_remainder;
                    }
                    /* We could immediately start working at ip now, but to improve
                       compression we first update "table" with the hashes of some positions
                       within the last copy. */
                    {
                        ulong input_bytes = BROTLI_UNALIGNED_LOAD64LE(ip - 3);
                        uint prev_hash = HashBytesAtOffset(input_bytes, 0, shift);
                        uint cur_hash = HashBytesAtOffset(input_bytes, 3, shift);
                        table[prev_hash] = (int)(ip - base_ip - 3);
                        prev_hash = HashBytesAtOffset(input_bytes, 1, shift);
                        table[prev_hash] = (int)(ip - base_ip - 2);
                        prev_hash = HashBytesAtOffset(input_bytes, 2, shift);
                        table[prev_hash] = (int)(ip - base_ip - 1);

                        candidate = base_ip + table[cur_hash];
                        table[cur_hash] = (int)(ip - base_ip);
                    }
                }

                next_hash = Hash(++ip, shift);
            }
        }

    emit_remainder:
        input += block_size;
        input_size -= block_size;
        block_size = BROTLI_MIN(input_size, kMergeBlockSize);

        /* Decide if we want to continue this meta-block instead of emitting the
           last insert-only command. */
        if (input_size > 0 &&
            total_block_size + block_size <= (1 << 20) &&
            ShouldMergeBlock(s, input, block_size, lit_depth))
        {
            /* Update the size of the current meta-block and continue emitting commands.
               We can do this because the current size and the new size both have 5
               nibbles. */
            total_block_size += block_size;
            UpdateBits(20, (uint)(total_block_size - 1), mlen_storage_ix, storage);
            goto emit_commands;
        }

        /* Emit the remaining bytes as literals. */
        if (next_emit < ip_end)
        {
            nuint insert = (nuint)(ip_end - next_emit);
            if (insert < 6210)
            {
                EmitInsertLen(insert, cmd_depth, cmd_bits, cmd_histo,
                              storage_ix, storage);
                EmitLiterals(next_emit, insert, lit_depth, lit_bits, storage_ix, storage);
            }
            else if (ShouldUseUncompressedMode(metablock_start, next_emit, insert,
                                               literal_ratio))
            {
                EmitUncompressedMetaBlock(metablock_start, ip_end, mlen_storage_ix - 3,
                                          storage_ix, storage);
            }
            else
            {
                EmitLongInsertLen(insert, cmd_depth, cmd_bits, cmd_histo,
                                  storage_ix, storage);
                EmitLiterals(next_emit, insert, lit_depth, lit_bits,
                             storage_ix, storage);
            }
        }
        next_emit = ip_end;

    next_block:
        /* If we have more data, write a new meta-block header and prefix codes and
           then continue emitting commands. */
        if (input_size > 0)
        {
            metablock_start = input;
            block_size = BROTLI_MIN(input_size, kFirstBlockSize);
            total_block_size = block_size;
            /* Save the bit position of the MLEN field of the meta-block header, so that
               we can update it later if we decide to extend this meta-block. */
            mlen_storage_ix = *storage_ix + 3;
            BrotliStoreMetaBlockHeader(block_size, 0, storage_ix, storage);
            /* No block splits, no contexts. */
            BrotliWriteBits(13, 0, storage_ix, storage);
            literal_ratio = BuildAndStoreLiteralPrefixCode(
                s, input, block_size, lit_depth, lit_bits, storage_ix, storage);
            BuildAndStoreCommandPrefixCode(s, storage_ix, storage);
            goto emit_commands;
        }

        if (is_last == 0)
        {
            /* If this is not the last block, update the command and distance prefix
               codes for the next block and store the compressed forms. */
            s->cmd_code[0] = 0;
            s->cmd_code_numbits = 0;
            BuildAndStoreCommandPrefixCode(s, &s->cmd_code_numbits, s->cmd_code);
        }
    }

    /* FOR_TABLE_BITS_(X): X(9) X(11) X(13) X(15) */
    private static void BrotliCompressFragmentFastImpl9(
        BrotliOnePassArena* s, byte* input, nuint input_size,
        int is_last, int* table, nuint* storage_ix, byte* storage)
    {
        BrotliCompressFragmentFastImpl(s, input, input_size, is_last, table, 9,
            storage_ix, storage);
    }

    private static void BrotliCompressFragmentFastImpl11(
        BrotliOnePassArena* s, byte* input, nuint input_size,
        int is_last, int* table, nuint* storage_ix, byte* storage)
    {
        BrotliCompressFragmentFastImpl(s, input, input_size, is_last, table, 11,
            storage_ix, storage);
    }

    private static void BrotliCompressFragmentFastImpl13(
        BrotliOnePassArena* s, byte* input, nuint input_size,
        int is_last, int* table, nuint* storage_ix, byte* storage)
    {
        BrotliCompressFragmentFastImpl(s, input, input_size, is_last, table, 13,
            storage_ix, storage);
    }

    private static void BrotliCompressFragmentFastImpl15(
        BrotliOnePassArena* s, byte* input, nuint input_size,
        int is_last, int* table, nuint* storage_ix, byte* storage)
    {
        BrotliCompressFragmentFastImpl(s, input, input_size, is_last, table, 15,
            storage_ix, storage);
    }

    /* Compresses "input" string to the "*storage" buffer as one or more complete
       meta-blocks, and updates the "*storage_ix" bit position.

       If "is_last" is 1, emits an additional empty last meta-block.

       REQUIRES: "input_size" is greater than zero, or "is_last" is 1.
       REQUIRES: "input_size" is less or equal to maximal metablock size (1 << 24).
       REQUIRES: All elements in "table[0..table_size-1]" are initialized to zero.
       REQUIRES: "table_size" is an odd (9, 11, 13, 15) power of two
       OUTPUT: maximal copy distance <= |input_size|
       OUTPUT: maximal copy distance <= BROTLI_MAX_BACKWARD_LIMIT(18) */
    internal static void BrotliCompressFragmentFast(
        BrotliOnePassArena* s, byte* input, nuint input_size,
        int is_last, int* table, nuint table_size,
        nuint* storage_ix, byte* storage)
    {
        nuint initial_storage_ix = *storage_ix;
        nuint table_bits = Log2FloorNonZero(table_size);

        if (input_size == 0)
        {
            BrotliWriteBits(1, 1, storage_ix, storage);  /* islast */
            BrotliWriteBits(1, 1, storage_ix, storage);  /* isempty */
            *storage_ix = (*storage_ix + 7u) & ~(nuint)7u;
            return;
        }

        switch (table_bits)
        {
            case 9:
                BrotliCompressFragmentFastImpl9(
                    s, input, input_size, is_last, table, storage_ix, storage);
                break;
            case 11:
                BrotliCompressFragmentFastImpl11(
                    s, input, input_size, is_last, table, storage_ix, storage);
                break;
            case 13:
                BrotliCompressFragmentFastImpl13(
                    s, input, input_size, is_last, table, storage_ix, storage);
                break;
            case 15:
                BrotliCompressFragmentFastImpl15(
                    s, input, input_size, is_last, table, storage_ix, storage);
                break;
            default: break;  /* BROTLI_DCHECK(0) */
        }

        /* If output is larger than single uncompressed block, rewrite it. */
        if (*storage_ix - initial_storage_ix > 31 + (input_size << 3))
        {
            EmitUncompressedMetaBlock(input, input + input_size, initial_storage_ix,
                                      storage_ix, storage);
        }

        if (is_last != 0)
        {
            BrotliWriteBits(1, 1, storage_ix, storage);  /* islast */
            BrotliWriteBits(1, 1, storage_ix, storage);  /* isempty */
            *storage_ix = (*storage_ix + 7u) & ~(nuint)7u;
        }
    }
}
