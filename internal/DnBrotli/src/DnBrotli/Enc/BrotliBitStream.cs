// Port of c/enc/brotli_bit_stream.{h,c} (brotli v1.1.0) — complete.
//
// Everything the q0..q9 paths and the stream framing reach —
// BrotliStoreHuffmanTree (plus its serialization helpers),
// BrotliBuildAndStoreHuffmanTreeFast, BuildAndStoreHuffmanTree,
// StoreCompressedMetaBlockHeader, StoreCommandExtra, BuildHistograms,
// StoreDataWithHuffmanCodes, BrotliStoreMetaBlockTrivial (q3),
// BrotliStoreMetaBlockFast (q<=2), BrotliStoreUncompressedMetaBlock, and the
// general q>=4 store: block-switch coding (BlockTypeCodeCalculator /
// BlockSplitCode / StoreBlockSwitch / BuildAndStoreBlockSplitCode), context
// map encoding (MoveToFrontTransform + RunLengthCodeZeros + EncodeContextMap,
// StoreTrivialContextMap), the BlockEncoder machinery with the
// block_encoder_inc.h triple (BuildAndStoreEntropyCodes{Literal,Command,
// Distance}) and BrotliStoreMetaBlock. MetaBlockSplit lives in Metablock.cs
// (c/enc/metablock.h).
//
// All Store functions here use a storage_ix, which is always the bit
// position for the current storage; there are no out-of-range checks —
// callers guarantee capacity, as in C.

using System.Runtime.CompilerServices;

using DnBrotli.Common;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Context;
using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.Command;
using static DnBrotli.Enc.EntropyEncode;
using static DnBrotli.Enc.EntropyEncodeStatic;
using static DnBrotli.Enc.Histogram;
using static DnBrotli.Enc.MemoryManager;
using static DnBrotli.Enc.WriteBits;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

internal static unsafe class BrotliBitStream
{
    /* MAX_HUFFMAN_TREE_SIZE == 2 * BROTLI_NUM_COMMAND_SYMBOLS + 1 */
    internal const int MAX_HUFFMAN_TREE_SIZE = 2 * BROTLI_NUM_COMMAND_SYMBOLS + 1;
    /* The maximum size of Huffman dictionary for distances assuming that
       NPOSTFIX = 0 and NDIRECT = 0.
       MAX_SIMPLE_DISTANCE_ALPHABET_SIZE ==
       BROTLI_DISTANCE_ALPHABET_SIZE(0, 0, BROTLI_LARGE_MAX_DISTANCE_BITS) */
    internal const int MAX_SIMPLE_DISTANCE_ALPHABET_SIZE = 140;

    private static void BrotliStoreHuffmanTreeOfHuffmanTreeToBitMask(
        int num_codes, byte* code_length_bitdepth,
        nuint* storage_ix, byte* storage)
    {
        ReadOnlySpan<byte> kStorageOrder =  /* [BROTLI_CODE_LENGTH_CODES] */
        [
            1, 2, 3, 4, 0, 5, 17, 6, 16, 7, 8, 9, 10, 11, 12, 13, 14, 15,
        ];
        /* The bit lengths of the Huffman code over the code length alphabet
           are compressed with the following static Huffman code:
             Symbol   Code
             ------   ----
             0          00
             1        1110
             2         110
             3          01
             4          10
             5        1111 */
        ReadOnlySpan<byte> kHuffmanBitLengthHuffmanCodeSymbols =
        [
            0, 7, 3, 2, 1, 15,
        ];
        ReadOnlySpan<byte> kHuffmanBitLengthHuffmanCodeBitLengths =
        [
            2, 4, 3, 2, 2, 4,
        ];

        nuint skip_some = 0;  /* skips none. */

        /* Throw away trailing zeros: */
        nuint codes_to_store = BROTLI_CODE_LENGTH_CODES;
        if (num_codes > 1)
        {
            for (; codes_to_store > 0; --codes_to_store)
            {
                if (code_length_bitdepth[kStorageOrder[(int)codes_to_store - 1]] != 0)
                {
                    break;
                }
            }
        }
        if (code_length_bitdepth[kStorageOrder[0]] == 0 &&
            code_length_bitdepth[kStorageOrder[1]] == 0)
        {
            skip_some = 2;  /* skips two. */
            if (code_length_bitdepth[kStorageOrder[2]] == 0)
            {
                skip_some = 3;  /* skips three. */
            }
        }
        BrotliWriteBits(2, skip_some, storage_ix, storage);
        {
            nuint i;
            for (i = skip_some; i < codes_to_store; ++i)
            {
                nuint l = code_length_bitdepth[kStorageOrder[(int)i]];
                BrotliWriteBits(kHuffmanBitLengthHuffmanCodeBitLengths[(int)l],
                    kHuffmanBitLengthHuffmanCodeSymbols[(int)l], storage_ix, storage);
            }
        }
    }

    private static void BrotliStoreHuffmanTreeToBitMask(
        nuint huffman_tree_size, byte* huffman_tree,
        byte* huffman_tree_extra_bits, byte* code_length_bitdepth,
        ushort* code_length_bitdepth_symbols,
        nuint* storage_ix, byte* storage)
    {
        nuint i;
        for (i = 0; i < huffman_tree_size; ++i)
        {
            nuint ix = huffman_tree[i];
            BrotliWriteBits(code_length_bitdepth[ix], code_length_bitdepth_symbols[ix],
                            storage_ix, storage);
            /* Extra bits */
            switch (ix)
            {
                case BROTLI_REPEAT_PREVIOUS_CODE_LENGTH:
                    BrotliWriteBits(2, huffman_tree_extra_bits[i], storage_ix, storage);
                    break;
                case BROTLI_REPEAT_ZERO_CODE_LENGTH:
                    BrotliWriteBits(3, huffman_tree_extra_bits[i], storage_ix, storage);
                    break;
            }
        }
    }

    private static void StoreSimpleHuffmanTree(byte* depths,
                                               nuint* symbols,  /* size_t symbols[4] */
                                               nuint num_symbols,
                                               nuint max_bits,
                                               nuint* storage_ix, byte* storage)
    {
        /* value of 1 indicates a simple Huffman code */
        BrotliWriteBits(2, 1, storage_ix, storage);
        BrotliWriteBits(2, num_symbols - 1, storage_ix, storage);  /* NSYM - 1 */

        {
            /* Sort */
            nuint i;
            for (i = 0; i < num_symbols; i++)
            {
                nuint j;
                for (j = i + 1; j < num_symbols; j++)
                {
                    if (depths[symbols[j]] < depths[symbols[i]])
                    {
                        nuint tmp = symbols[j];  /* BROTLI_SWAP */
                        symbols[j] = symbols[i];
                        symbols[i] = tmp;
                    }
                }
            }
        }

        if (num_symbols == 2)
        {
            BrotliWriteBits(max_bits, symbols[0], storage_ix, storage);
            BrotliWriteBits(max_bits, symbols[1], storage_ix, storage);
        }
        else if (num_symbols == 3)
        {
            BrotliWriteBits(max_bits, symbols[0], storage_ix, storage);
            BrotliWriteBits(max_bits, symbols[1], storage_ix, storage);
            BrotliWriteBits(max_bits, symbols[2], storage_ix, storage);
        }
        else
        {
            BrotliWriteBits(max_bits, symbols[0], storage_ix, storage);
            BrotliWriteBits(max_bits, symbols[1], storage_ix, storage);
            BrotliWriteBits(max_bits, symbols[2], storage_ix, storage);
            BrotliWriteBits(max_bits, symbols[3], storage_ix, storage);
            /* tree-select */
            BrotliWriteBits(1, depths[symbols[0]] == 1 ? 1u : 0u, storage_ix, storage);
        }
    }

    /* num = alphabet size
       depths = symbol depths */
    internal static void BrotliStoreHuffmanTree(byte* depths, nuint num,
                                                HuffmanTree* tree,
                                                nuint* storage_ix, byte* storage)
    {
        /* Write the Huffman tree into the brotli-representation.
           The command alphabet is the largest, so this allocation will fit all
           alphabets. */
        byte* huffman_tree = stackalloc byte[BROTLI_NUM_COMMAND_SYMBOLS];
        byte* huffman_tree_extra_bits = stackalloc byte[BROTLI_NUM_COMMAND_SYMBOLS];
        nuint huffman_tree_size = 0;
        byte* code_length_bitdepth = stackalloc byte[BROTLI_CODE_LENGTH_CODES];
        ushort* code_length_bitdepth_symbols = stackalloc ushort[BROTLI_CODE_LENGTH_CODES];
        uint* huffman_tree_histogram = stackalloc uint[BROTLI_CODE_LENGTH_CODES];
        new Span<byte>(code_length_bitdepth, BROTLI_CODE_LENGTH_CODES).Clear();
        new Span<uint>(huffman_tree_histogram, BROTLI_CODE_LENGTH_CODES).Clear();
        nuint i;
        int num_codes = 0;
        nuint code = 0;

        BrotliWriteHuffmanTree(depths, num, &huffman_tree_size, huffman_tree,
                               huffman_tree_extra_bits);

        /* Calculate the statistics of the Huffman tree in brotli-representation. */
        for (i = 0; i < huffman_tree_size; ++i)
        {
            ++huffman_tree_histogram[huffman_tree[i]];
        }

        for (i = 0; i < BROTLI_CODE_LENGTH_CODES; ++i)
        {
            if (huffman_tree_histogram[i] != 0)
            {
                if (num_codes == 0)
                {
                    code = i;
                    num_codes = 1;
                }
                else if (num_codes == 1)
                {
                    num_codes = 2;
                    break;
                }
            }
        }

        /* Calculate another Huffman tree to use for compressing both the
           earlier Huffman tree with. */
        BrotliCreateHuffmanTree(huffman_tree_histogram, BROTLI_CODE_LENGTH_CODES,
                                5, tree, code_length_bitdepth);
        BrotliConvertBitDepthsToSymbols(code_length_bitdepth,
                                        BROTLI_CODE_LENGTH_CODES,
                                        code_length_bitdepth_symbols);

        /* Now, we have all the data, let's start storing it */
        BrotliStoreHuffmanTreeOfHuffmanTreeToBitMask(num_codes, code_length_bitdepth,
                                                     storage_ix, storage);

        if (num_codes == 1)
        {
            code_length_bitdepth[code] = 0;
        }

        /* Store the real Huffman tree now. */
        BrotliStoreHuffmanTreeToBitMask(huffman_tree_size,
                                        huffman_tree,
                                        huffman_tree_extra_bits,
                                        code_length_bitdepth,
                                        code_length_bitdepth_symbols,
                                        storage_ix, storage);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SortHuffmanTree(HuffmanTree* v0, HuffmanTree* v1)
    {
        return v0->total_count_ < v1->total_count_ ? 1 : 0;
    }

    internal static void BrotliBuildAndStoreHuffmanTreeFast(HuffmanTree* tree,
                                                            uint* histogram,
                                                            nuint histogram_total,
                                                            nuint max_bits,
                                                            byte* depth, ushort* bits,
                                                            nuint* storage_ix,
                                                            byte* storage)
    {
        nuint count = 0;
        nuint* symbols = stackalloc nuint[4] { 0, 0, 0, 0 };
        nuint length = 0;
        nuint total = histogram_total;
        while (total != 0)
        {
            if (histogram[length] != 0)
            {
                if (count < 4)
                {
                    symbols[count] = length;
                }
                ++count;
                total -= histogram[length];
            }
            ++length;
        }

        if (count <= 1)
        {
            BrotliWriteBits(4, 1, storage_ix, storage);
            BrotliWriteBits(max_bits, symbols[0], storage_ix, storage);
            depth[symbols[0]] = 0;
            bits[symbols[0]] = 0;
            return;
        }

        new Span<byte>(depth, (int)length).Clear();
        {
            uint count_limit;
            for (count_limit = 1; ; count_limit *= 2)
            {
                HuffmanTree* node = tree;
                nuint l;
                for (l = length; l != 0;)
                {
                    --l;
                    if (histogram[l] != 0)
                    {
                        if (histogram[l] >= count_limit)
                        {
                            InitHuffmanTree(node, histogram[l], -1, (short)l);
                        }
                        else
                        {
                            InitHuffmanTree(node, count_limit, -1, (short)l);
                        }
                        ++node;
                    }
                }
                {
                    int n = (int)(node - tree);
                    HuffmanTree sentinel;
                    int i = 0;      /* Points to the next leaf node. */
                    int j = n + 1;  /* Points to the next non-leaf node. */
                    int k;

                    SortHuffmanTreeItems(tree, (nuint)n, &SortHuffmanTree);
                    /* The nodes are:
                       [0, n): the sorted leaf nodes that we start with.
                       [n]: we add a sentinel here.
                       [n + 1, 2n): new parent nodes are added here, starting from
                                    (n+1). These are naturally in ascending order.
                       [2n]: we add a sentinel at the end as well.
                       There will be (2n+1) elements at the end. */
                    InitHuffmanTree(&sentinel, uint.MaxValue, -1, -1);
                    *node++ = sentinel;
                    *node++ = sentinel;

                    for (k = n - 1; k > 0; --k)
                    {
                        int left, right;
                        if (tree[i].total_count_ <= tree[j].total_count_)
                        {
                            left = i;
                            ++i;
                        }
                        else
                        {
                            left = j;
                            ++j;
                        }
                        if (tree[i].total_count_ <= tree[j].total_count_)
                        {
                            right = i;
                            ++i;
                        }
                        else
                        {
                            right = j;
                            ++j;
                        }
                        /* The sentinel node becomes the parent node. */
                        node[-1].total_count_ =
                            tree[left].total_count_ + tree[right].total_count_;
                        node[-1].index_left_ = (short)left;
                        node[-1].index_right_or_value_ = (short)right;
                        /* Add back the last sentinel node. */
                        *node++ = sentinel;
                    }
                    if (BrotliSetDepth(2 * n - 1, tree, depth, 14) != 0)
                    {
                        /* We need to pack the Huffman tree in 14 bits. If this was not
                           successful, add fake entities to the lowest values and retry. */
                        break;
                    }
                }
            }
        }
        BrotliConvertBitDepthsToSymbols(depth, length, bits);
        if (count <= 4)
        {
            nuint i;
            /* value of 1 indicates a simple Huffman code */
            BrotliWriteBits(2, 1, storage_ix, storage);
            BrotliWriteBits(2, count - 1, storage_ix, storage);  /* NSYM - 1 */

            /* Sort */
            for (i = 0; i < count; i++)
            {
                nuint j;
                for (j = i + 1; j < count; j++)
                {
                    if (depth[symbols[j]] < depth[symbols[i]])
                    {
                        nuint tmp = symbols[j];  /* BROTLI_SWAP */
                        symbols[j] = symbols[i];
                        symbols[i] = tmp;
                    }
                }
            }

            if (count == 2)
            {
                BrotliWriteBits(max_bits, symbols[0], storage_ix, storage);
                BrotliWriteBits(max_bits, symbols[1], storage_ix, storage);
            }
            else if (count == 3)
            {
                BrotliWriteBits(max_bits, symbols[0], storage_ix, storage);
                BrotliWriteBits(max_bits, symbols[1], storage_ix, storage);
                BrotliWriteBits(max_bits, symbols[2], storage_ix, storage);
            }
            else
            {
                BrotliWriteBits(max_bits, symbols[0], storage_ix, storage);
                BrotliWriteBits(max_bits, symbols[1], storage_ix, storage);
                BrotliWriteBits(max_bits, symbols[2], storage_ix, storage);
                BrotliWriteBits(max_bits, symbols[3], storage_ix, storage);
                /* tree-select */
                BrotliWriteBits(1, depth[symbols[0]] == 1 ? 1u : 0u, storage_ix, storage);
            }
        }
        else
        {
            byte previous_value = 8;
            nuint i;
            /* Complex Huffman Tree */
            StoreStaticCodeLengthCode(storage_ix, storage);

            /* Actual RLE coding. */
            for (i = 0; i < length;)
            {
                byte value = depth[i];
                nuint reps = 1;
                nuint k;
                for (k = i + 1; k < length && depth[k] == value; ++k)
                {
                    ++reps;
                }
                i += reps;
                if (value == 0)
                {
                    BrotliWriteBits(kZeroRepsDepth[reps], kZeroRepsBits[reps],
                                    storage_ix, storage);
                }
                else
                {
                    if (previous_value != value)
                    {
                        BrotliWriteBits(kCodeLengthDepth[value], kCodeLengthBits[value],
                                        storage_ix, storage);
                        --reps;
                    }
                    if (reps < 3)
                    {
                        while (reps != 0)
                        {
                            reps--;
                            BrotliWriteBits(kCodeLengthDepth[value], kCodeLengthBits[value],
                                            storage_ix, storage);
                        }
                    }
                    else
                    {
                        reps -= 3;
                        BrotliWriteBits(kNonZeroRepsDepth[reps], kNonZeroRepsBits[reps],
                                        storage_ix, storage);
                    }
                    previous_value = value;
                }
            }
        }
    }

    internal static void JumpToByteBoundary(nuint* storage_ix, byte* storage)
    {
        *storage_ix = (*storage_ix + 7u) & ~(nuint)7u;
        storage[*storage_ix >> 3] = 0;
    }

    /* Stores the compressed meta-block header.
       REQUIRES: length > 0
       REQUIRES: length <= (1 << 24) */
    private static void StoreCompressedMetaBlockHeader(int is_final_block,
                                                       nuint length,
                                                       nuint* storage_ix,
                                                       byte* storage)
    {
        ulong lenbits;
        nuint nlenbits;
        ulong nibblesbits;

        /* Write ISLAST bit. */
        BrotliWriteBits(1, (ulong)is_final_block, storage_ix, storage);
        /* Write ISEMPTY bit. */
        if (is_final_block != 0)
        {
            BrotliWriteBits(1, 0, storage_ix, storage);
        }

        BrotliEncodeMlen(length, &lenbits, &nlenbits, &nibblesbits);
        BrotliWriteBits(2, nibblesbits, storage_ix, storage);
        BrotliWriteBits(nlenbits, lenbits, storage_ix, storage);

        if (is_final_block == 0)
        {
            /* Write ISUNCOMPRESSED bit. */
            BrotliWriteBits(1, 0, storage_ix, storage);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StoreCommandExtra(
        Command* cmd, nuint* storage_ix, byte* storage)
    {
        uint copylen_code = CommandCopyLenCode(cmd);
        ushort inscode = GetInsertLengthCode(cmd->insert_len_);
        ushort copycode = GetCopyLengthCode(copylen_code);
        uint insnumextra = GetInsertExtra(inscode);
        ulong insextraval = cmd->insert_len_ - GetInsertBase(inscode);
        ulong copyextraval = copylen_code - GetCopyBase(copycode);
        ulong bits = (copyextraval << (int)insnumextra) | insextraval;
        BrotliWriteBits(
            insnumextra + GetCopyExtra(copycode), bits, storage_ix, storage);
    }

    /* Builds a Huffman tree from histogram[0:length] into depth[0:length] and
       bits[0:length] and stores the encoded tree to the bit stream. */
    internal static void BuildAndStoreHuffmanTree(uint* histogram,
                                                  nuint histogram_length,
                                                  nuint alphabet_size,
                                                  HuffmanTree* tree,
                                                  byte* depth,
                                                  ushort* bits,
                                                  nuint* storage_ix,
                                                  byte* storage)
    {
        nuint count = 0;
        nuint* s4 = stackalloc nuint[4] { 0, 0, 0, 0 };
        nuint i;
        nuint max_bits = 0;
        for (i = 0; i < histogram_length; i++)
        {
            if (histogram[i] != 0)
            {
                if (count < 4)
                {
                    s4[count] = i;
                }
                else if (count > 4)
                {
                    break;
                }
                count++;
            }
        }

        {
            nuint max_bits_counter = alphabet_size - 1;
            while (max_bits_counter != 0)
            {
                max_bits_counter >>= 1;
                ++max_bits;
            }
        }

        if (count <= 1)
        {
            BrotliWriteBits(4, 1, storage_ix, storage);
            BrotliWriteBits(max_bits, s4[0], storage_ix, storage);
            depth[s4[0]] = 0;
            bits[s4[0]] = 0;
            return;
        }

        new Span<byte>(depth, (int)histogram_length).Clear();
        BrotliCreateHuffmanTree(histogram, histogram_length, 15, tree, depth);
        BrotliConvertBitDepthsToSymbols(depth, histogram_length, bits);

        if (count <= 4)
        {
            StoreSimpleHuffmanTree(depth, s4, count, max_bits, storage_ix, storage);
        }
        else
        {
            BrotliStoreHuffmanTree(depth, histogram_length, tree, storage_ix, storage);
        }
    }

    private static void BuildHistograms(byte* input,
                                        nuint start_pos,
                                        nuint mask,
                                        Command* commands,
                                        nuint n_commands,
                                        HistogramLiteral* lit_histo,
                                        HistogramCommand* cmd_histo,
                                        HistogramDistance* dist_histo)
    {
        nuint pos = start_pos;
        nuint i;
        for (i = 0; i < n_commands; ++i)
        {
            Command cmd = commands[i];
            nuint j;
            HistogramAddCommand(cmd_histo, cmd.cmd_prefix_);
            for (j = cmd.insert_len_; j != 0; --j)
            {
                HistogramAddLiteral(lit_histo, input[pos & mask]);
                ++pos;
            }
            pos += CommandCopyLen(&cmd);
            if (CommandCopyLen(&cmd) != 0 && cmd.cmd_prefix_ >= 128)
            {
                HistogramAddDistance(dist_histo, (nuint)(cmd.dist_prefix_ & 0x3FF));
            }
        }
    }

    private static void StoreDataWithHuffmanCodes(byte* input,
                                                  nuint start_pos,
                                                  nuint mask,
                                                  Command* commands,
                                                  nuint n_commands,
                                                  byte* lit_depth,
                                                  ushort* lit_bits,
                                                  byte* cmd_depth,
                                                  ushort* cmd_bits,
                                                  byte* dist_depth,
                                                  ushort* dist_bits,
                                                  nuint* storage_ix,
                                                  byte* storage)
    {
        nuint pos = start_pos;
        nuint i;
        for (i = 0; i < n_commands; ++i)
        {
            Command cmd = commands[i];
            nuint cmd_code = cmd.cmd_prefix_;
            nuint j;
            BrotliWriteBits(
                cmd_depth[cmd_code], cmd_bits[cmd_code], storage_ix, storage);
            StoreCommandExtra(&cmd, storage_ix, storage);
            for (j = cmd.insert_len_; j != 0; --j)
            {
                byte literal = input[pos & mask];
                BrotliWriteBits(
                    lit_depth[literal], lit_bits[literal], storage_ix, storage);
                ++pos;
            }
            pos += CommandCopyLen(&cmd);
            if (CommandCopyLen(&cmd) != 0 && cmd.cmd_prefix_ >= 128)
            {
                nuint dist_code = (nuint)(cmd.dist_prefix_ & 0x3FF);
                uint distnumextra = (uint)cmd.dist_prefix_ >> 10;
                uint distextra = cmd.dist_extra_;
                BrotliWriteBits(dist_depth[dist_code], dist_bits[dist_code],
                                storage_ix, storage);
                BrotliWriteBits(distnumextra, distextra, storage_ix, storage);
            }
        }
    }

    /// <summary><c>struct MetablockArena</c>. The HuffmanTree scratch array is a
    /// flattened <c>fixed ulong</c> sibling buffer (PORTING.md: embedded arrays of
    /// small structs), reinterpreted as <c>HuffmanTree*</c>. Capacity is 12 bytes
    /// per entry, not 8: sizeof(HuffmanTree) is 8 under .NET (uint + short + short,
    /// packed) but 12 under dn2cpp (small struct fields widen to int32).</summary>
    internal struct MetablockArena
    {
        public HistogramLiteral lit_histo;
        public HistogramCommand cmd_histo;
        public HistogramDistance dist_histo;
        /* TODO(eustas): merge bits and depth? */
        public fixed byte lit_depth[BROTLI_NUM_LITERAL_SYMBOLS];
        public fixed ushort lit_bits[BROTLI_NUM_LITERAL_SYMBOLS];
        public fixed byte cmd_depth[BROTLI_NUM_COMMAND_SYMBOLS];
        public fixed ushort cmd_bits[BROTLI_NUM_COMMAND_SYMBOLS];
        public fixed byte dist_depth[MAX_SIMPLE_DISTANCE_ALPHABET_SIZE];
        public fixed ushort dist_bits[MAX_SIMPLE_DISTANCE_ALPHABET_SIZE];
        public fixed ulong tree_[(3 * MAX_HUFFMAN_TREE_SIZE + 1) / 2];  /* HuffmanTree tree[MAX_HUFFMAN_TREE_SIZE] */
    }

    /* --- The general (q>=4) metablock store machinery; kept together here,
       in c/enc/brotli_bit_stream.c the pieces live before the *MetaBlock*
       functions. --- */

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint BlockLengthPrefixCode(uint len)
    {
        uint code = (len >= 177) ? (len >= 753 ? 20u : 14u) : (len >= 41 ? 7u : 0u);
        while (code < (BROTLI_NUM_BLOCK_LEN_SYMBOLS - 1) &&
            len >= _kBrotliPrefixCodeRanges[code + 1].offset) ++code;
        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetBlockLengthPrefixCode(uint len, nuint* code,
        uint* n_extra, uint* extra)
    {
        *code = BlockLengthPrefixCode(len);
        *n_extra = _kBrotliPrefixCodeRanges[*code].nbits;
        *extra = len - _kBrotliPrefixCodeRanges[*code].offset;
    }

    internal struct BlockTypeCodeCalculator
    {
        public nuint last_type;
        public nuint second_last_type;
    }

    private static void InitBlockTypeCodeCalculator(BlockTypeCodeCalculator* self)
    {
        self->last_type = 1;
        self->second_last_type = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint NextBlockTypeCode(
        BlockTypeCodeCalculator* calculator, byte type)
    {
        nuint type_code = (type == calculator->last_type + 1) ? 1u :
            (type == calculator->second_last_type) ? 0u : type + 2u;
        calculator->second_last_type = calculator->last_type;
        calculator->last_type = type;
        return type_code;
    }

    /* Data structure that stores almost everything that is needed to encode each
       block switch command. */
    internal struct BlockSplitCode
    {
        public BlockTypeCodeCalculator type_code_calculator;
        public fixed byte type_depths[BROTLI_MAX_BLOCK_TYPE_SYMBOLS];
        public fixed ushort type_bits[BROTLI_MAX_BLOCK_TYPE_SYMBOLS];
        public fixed byte length_depths[BROTLI_NUM_BLOCK_LEN_SYMBOLS];
        public fixed ushort length_bits[BROTLI_NUM_BLOCK_LEN_SYMBOLS];
    }

    /* Stores a number between 0 and 255. */
    private static void StoreVarLenUint8(nuint n, nuint* storage_ix, byte* storage)
    {
        if (n == 0)
        {
            BrotliWriteBits(1, 0, storage_ix, storage);
        }
        else
        {
            nuint nbits = Log2FloorNonZero(n);
            BrotliWriteBits(1, 1, storage_ix, storage);
            BrotliWriteBits(3, nbits, storage_ix, storage);
            BrotliWriteBits(nbits, n - ((nuint)1 << (int)nbits), storage_ix, storage);
        }
    }

    private static nuint IndexOf(byte* v, nuint v_size, byte value)
    {
        nuint i = 0;
        for (; i < v_size; ++i)
        {
            if (v[i] == value) return i;
        }
        return i;
    }

    private static void MoveToFront(byte* v, nuint index)
    {
        byte value = v[index];
        nuint i;
        for (i = index; i != 0; --i)
        {
            v[i] = v[i - 1];
        }
        v[0] = value;
    }

    private static void MoveToFrontTransform(uint* v_in,
                                             nuint v_size,
                                             uint* v_out)
    {
        nuint i;
        byte* mtf = stackalloc byte[256];
        uint max_value;
        if (v_size == 0)
        {
            return;
        }
        max_value = v_in[0];
        for (i = 1; i < v_size; ++i)
        {
            if (v_in[i] > max_value) max_value = v_in[i];
        }
        /* BROTLI_DCHECK(max_value < 256u); */
        for (i = 0; i <= max_value; ++i)
        {
            mtf[i] = (byte)i;
        }
        {
            nuint mtf_size = max_value + 1;
            for (i = 0; i < v_size; ++i)
            {
                nuint index = IndexOf(mtf, mtf_size, (byte)v_in[i]);
                /* BROTLI_DCHECK(index < mtf_size); */
                v_out[i] = (uint)index;
                MoveToFront(mtf, index);
            }
        }
    }

    /* Finds runs of zeros in v[0..in_size) and replaces them with a prefix code of
       the run length plus extra bits (lower 9 bits is the prefix code and the rest
       are the extra bits). Non-zero values in v[] are shifted by
       *max_length_prefix. Will not create prefix codes bigger than the initial
       value of *max_run_length_prefix. The prefix code of run length L is simply
       Log2Floor(L) and the number of extra bits is the same as the prefix code. */
    private static void RunLengthCodeZeros(nuint in_size,
        uint* v, nuint* out_size,
        uint* max_run_length_prefix)
    {
        uint max_reps = 0;
        nuint i;
        uint max_prefix;
        for (i = 0; i < in_size;)
        {
            uint reps = 0;
            for (; i < in_size && v[i] != 0; ++i) ;
            for (; i < in_size && v[i] == 0; ++i)
            {
                ++reps;
            }
            max_reps = BROTLI_MAX(reps, max_reps);
        }
        max_prefix = max_reps > 0 ? Log2FloorNonZero(max_reps) : 0;
        max_prefix = BROTLI_MIN(max_prefix, *max_run_length_prefix);
        *max_run_length_prefix = max_prefix;
        *out_size = 0;
        for (i = 0; i < in_size;)
        {
            /* BROTLI_DCHECK(*out_size <= i); */
            if (v[i] != 0)
            {
                v[*out_size] = v[i] + *max_run_length_prefix;
                ++i;
                ++(*out_size);
            }
            else
            {
                uint reps = 1;
                nuint k;
                for (k = i + 1; k < in_size && v[k] == 0; ++k)
                {
                    ++reps;
                }
                i += reps;
                while (reps != 0)
                {
                    if (reps < (2u << (int)max_prefix))
                    {
                        uint run_length_prefix = Log2FloorNonZero(reps);
                        uint extra_bits = reps - (1u << (int)run_length_prefix);
                        v[*out_size] = run_length_prefix + (extra_bits << 9);
                        ++(*out_size);
                        break;
                    }
                    else
                    {
                        uint extra_bits = (1u << (int)max_prefix) - 1u;
                        v[*out_size] = max_prefix + (extra_bits << 9);
                        reps -= (2u << (int)max_prefix) - 1u;
                        ++(*out_size);
                    }
                }
            }
        }
    }

    private const int SYMBOL_BITS = 9;

    internal struct EncodeContextMapArena
    {
        public fixed uint histogram[BROTLI_MAX_CONTEXT_MAP_SYMBOLS];
        public fixed byte depths[BROTLI_MAX_CONTEXT_MAP_SYMBOLS];
        public fixed ushort bits[BROTLI_MAX_CONTEXT_MAP_SYMBOLS];
    }

    private static void EncodeContextMap(MemoryManager* m,
                                         EncodeContextMapArena* arena,
                                         uint* context_map,
                                         nuint context_map_size,
                                         nuint num_clusters,
                                         HuffmanTree* tree,
                                         nuint* storage_ix, byte* storage)
    {
        nuint i;
        uint* rle_symbols;
        uint max_run_length_prefix = 6;
        nuint num_rle_symbols = 0;
        uint* histogram = arena->histogram;
        const uint kSymbolMask = (1u << SYMBOL_BITS) - 1u;
        byte* depths = arena->depths;
        ushort* bits = arena->bits;

        StoreVarLenUint8(num_clusters - 1, storage_ix, storage);

        if (num_clusters == 1)
        {
            return;
        }

        rle_symbols = BROTLI_ALLOC<uint>(m, context_map_size);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(rle_symbols)) return;
        MoveToFrontTransform(context_map, context_map_size, rle_symbols);
        RunLengthCodeZeros(context_map_size, rle_symbols,
                           &num_rle_symbols, &max_run_length_prefix);
        new Span<uint>(histogram, BROTLI_MAX_CONTEXT_MAP_SYMBOLS).Clear();
        for (i = 0; i < num_rle_symbols; ++i)
        {
            ++histogram[rle_symbols[i] & kSymbolMask];
        }
        {
            bool use_rle = max_run_length_prefix > 0;
            BrotliWriteBits(1, use_rle ? 1u : 0u, storage_ix, storage);
            if (use_rle)
            {
                BrotliWriteBits(4, max_run_length_prefix - 1, storage_ix, storage);
            }
        }
        BuildAndStoreHuffmanTree(histogram, num_clusters + max_run_length_prefix,
                                 num_clusters + max_run_length_prefix,
                                 tree, depths, bits, storage_ix, storage);
        for (i = 0; i < num_rle_symbols; ++i)
        {
            uint rle_symbol = rle_symbols[i] & kSymbolMask;
            uint extra_bits_val = rle_symbols[i] >> SYMBOL_BITS;
            BrotliWriteBits(depths[rle_symbol], bits[rle_symbol], storage_ix, storage);
            if (rle_symbol > 0 && rle_symbol <= max_run_length_prefix)
            {
                BrotliWriteBits(rle_symbol, extra_bits_val, storage_ix, storage);
            }
        }
        BrotliWriteBits(1, 1, storage_ix, storage);  /* use move-to-front */
        BROTLI_FREE(m, ref rle_symbols);
    }

    /* Stores the block switch command with index block_ix to the bit stream. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StoreBlockSwitch(BlockSplitCode* code,
                                         uint block_len,
                                         byte block_type,
                                         int is_first_block,
                                         nuint* storage_ix,
                                         byte* storage)
    {
        nuint typecode = NextBlockTypeCode(&code->type_code_calculator, block_type);
        nuint lencode;
        uint len_nextra;
        uint len_extra;
        if (is_first_block == 0)
        {
            BrotliWriteBits(code->type_depths[typecode], code->type_bits[typecode],
                            storage_ix, storage);
        }
        GetBlockLengthPrefixCode(block_len, &lencode, &len_nextra, &len_extra);

        BrotliWriteBits(code->length_depths[lencode], code->length_bits[lencode],
                        storage_ix, storage);
        BrotliWriteBits(len_nextra, len_extra, storage_ix, storage);
    }

    /* Builds a BlockSplitCode data structure from the block split given by the
       vector of block types and block lengths and stores it to the bit stream. */
    private static void BuildAndStoreBlockSplitCode(byte* types,
                                                    uint* lengths,
                                                    nuint num_blocks,
                                                    nuint num_types,
                                                    HuffmanTree* tree,
                                                    BlockSplitCode* code,
                                                    nuint* storage_ix,
                                                    byte* storage)
    {
        uint* type_histo = stackalloc uint[BROTLI_MAX_BLOCK_TYPE_SYMBOLS];
        uint* length_histo = stackalloc uint[BROTLI_NUM_BLOCK_LEN_SYMBOLS];
        nuint i;
        BlockTypeCodeCalculator type_code_calculator;
        new Span<uint>(type_histo, (int)(num_types + 2)).Clear();
        new Span<uint>(length_histo, BROTLI_NUM_BLOCK_LEN_SYMBOLS).Clear();
        InitBlockTypeCodeCalculator(&type_code_calculator);
        for (i = 0; i < num_blocks; ++i)
        {
            nuint type_code = NextBlockTypeCode(&type_code_calculator, types[i]);
            if (i != 0) ++type_histo[type_code];
            ++length_histo[BlockLengthPrefixCode(lengths[i])];
        }
        StoreVarLenUint8(num_types - 1, storage_ix, storage);
        if (num_types > 1)
        {  /* TODO(eustas): else? could StoreBlockSwitch occur? */
            BuildAndStoreHuffmanTree(&type_histo[0], num_types + 2, num_types + 2, tree,
                                     &code->type_depths[0], &code->type_bits[0],
                                     storage_ix, storage);
            BuildAndStoreHuffmanTree(&length_histo[0], BROTLI_NUM_BLOCK_LEN_SYMBOLS,
                                     BROTLI_NUM_BLOCK_LEN_SYMBOLS,
                                     tree, &code->length_depths[0],
                                     &code->length_bits[0], storage_ix, storage);
            StoreBlockSwitch(code, lengths[0], types[0], 1, storage_ix, storage);
        }
    }

    /* Stores a context map where the histogram type is always the block type. */
    private static void StoreTrivialContextMap(EncodeContextMapArena* arena,
                                               nuint num_types,
                                               nuint context_bits,
                                               HuffmanTree* tree,
                                               nuint* storage_ix,
                                               byte* storage)
    {
        StoreVarLenUint8(num_types - 1, storage_ix, storage);
        if (num_types > 1)
        {
            nuint repeat_code = context_bits - 1u;
            nuint repeat_bits = ((nuint)1u << (int)repeat_code) - 1u;
            nuint alphabet_size = num_types + repeat_code;
            uint* histogram = arena->histogram;
            byte* depths = arena->depths;
            ushort* bits = arena->bits;
            nuint i;
            new Span<uint>(histogram, (int)alphabet_size).Clear();
            /* Write RLEMAX. */
            BrotliWriteBits(1, 1, storage_ix, storage);
            BrotliWriteBits(4, repeat_code - 1, storage_ix, storage);
            histogram[repeat_code] = (uint)num_types;
            histogram[0] = 1;
            for (i = context_bits; i < alphabet_size; ++i)
            {
                histogram[i] = 1;
            }
            BuildAndStoreHuffmanTree(histogram, alphabet_size, alphabet_size,
                                     tree, depths, bits, storage_ix, storage);
            for (i = 0; i < num_types; ++i)
            {
                nuint code = (i == 0 ? 0 : i + context_bits - 1);
                BrotliWriteBits(depths[code], bits[code], storage_ix, storage);
                BrotliWriteBits(
                    depths[repeat_code], bits[repeat_code], storage_ix, storage);
                BrotliWriteBits(repeat_code, repeat_bits, storage_ix, storage);
            }
            /* Write IMTF (inverse-move-to-front) bit. */
            BrotliWriteBits(1, 1, storage_ix, storage);
        }
    }

    /* Manages the encoding of one block category (literal, command or distance). */
    internal struct BlockEncoder
    {
        public nuint histogram_length_;
        public nuint num_block_types_;
        public byte* block_types_;  /* Not owned. */
        public uint* block_lengths_;  /* Not owned. */
        public nuint num_blocks_;
        public BlockSplitCode block_split_code_;
        public nuint block_ix_;
        public nuint block_len_;
        public nuint entropy_ix_;
        public byte* depths_;
        public ushort* bits_;
    }

    private static void InitBlockEncoder(BlockEncoder* self, nuint histogram_length,
        nuint num_block_types, byte* block_types,
        uint* block_lengths, nuint num_blocks)
    {
        self->histogram_length_ = histogram_length;
        self->num_block_types_ = num_block_types;
        self->block_types_ = block_types;
        self->block_lengths_ = block_lengths;
        self->num_blocks_ = num_blocks;
        InitBlockTypeCodeCalculator(&self->block_split_code_.type_code_calculator);
        self->block_ix_ = 0;
        self->block_len_ = num_blocks == 0 ? 0 : block_lengths[0];
        self->entropy_ix_ = 0;
        self->depths_ = null;
        self->bits_ = null;
    }

    private static void CleanupBlockEncoder(MemoryManager* m, BlockEncoder* self)
    {
        BROTLI_FREE(m, ref self->depths_);
        BROTLI_FREE(m, ref self->bits_);
    }

    /* Creates entropy codes of block lengths and block types and stores them
       to the bit stream. */
    private static void BuildAndStoreBlockSwitchEntropyCodes(BlockEncoder* self,
        HuffmanTree* tree, nuint* storage_ix, byte* storage)
    {
        BuildAndStoreBlockSplitCode(self->block_types_, self->block_lengths_,
            self->num_blocks_, self->num_block_types_, tree, &self->block_split_code_,
            storage_ix, storage);
    }

    /* Stores the next symbol with the entropy code of the current block type.
       Updates the block type and block length at block boundaries. */
    private static void StoreSymbol(BlockEncoder* self, nuint symbol, nuint* storage_ix,
        byte* storage)
    {
        if (self->block_len_ == 0)
        {
            nuint block_ix = ++self->block_ix_;
            uint block_len = self->block_lengths_[block_ix];
            byte block_type = self->block_types_[block_ix];
            self->block_len_ = block_len;
            self->entropy_ix_ = block_type * self->histogram_length_;
            StoreBlockSwitch(&self->block_split_code_, block_len, block_type, 0,
                storage_ix, storage);
        }
        --self->block_len_;
        {
            nuint ix = self->entropy_ix_ + symbol;
            BrotliWriteBits(self->depths_[ix], self->bits_[ix], storage_ix, storage);
        }
    }

    /* Stores the next symbol with the entropy code of the current block type and
       context value.
       Updates the block type and block length at block boundaries. */
    private static void StoreSymbolWithContext(BlockEncoder* self, nuint symbol,
        nuint context, uint* context_map, nuint* storage_ix,
        byte* storage, nuint context_bits)
    {
        if (self->block_len_ == 0)
        {
            nuint block_ix = ++self->block_ix_;
            uint block_len = self->block_lengths_[block_ix];
            byte block_type = self->block_types_[block_ix];
            self->block_len_ = block_len;
            self->entropy_ix_ = (nuint)block_type << (int)context_bits;
            StoreBlockSwitch(&self->block_split_code_, block_len, block_type, 0,
                storage_ix, storage);
        }
        --self->block_len_;
        {
            nuint histo_ix = context_map[self->entropy_ix_ + context];
            nuint ix = histo_ix * self->histogram_length_ + symbol;
            BrotliWriteBits(self->depths_[ix], self->bits_[ix], storage_ix, storage);
        }
    }

    /* --- block_encoder_inc.h expanded for FN(X) = XLiteral --- */

    /* Creates entropy codes for all block types and stores them to the bit
       stream. */
    private static void BuildAndStoreEntropyCodesLiteral(MemoryManager* m, BlockEncoder* self,
        HistogramLiteral* histograms, nuint histograms_size,
        nuint alphabet_size, HuffmanTree* tree,
        nuint* storage_ix, byte* storage)
    {
        nuint table_size = histograms_size * self->histogram_length_;
        self->depths_ = BROTLI_ALLOC<byte>(m, table_size);
        self->bits_ = BROTLI_ALLOC<ushort>(m, table_size);
        if (BROTLI_IS_OOM(m)) return;

        {
            nuint i;
            for (i = 0; i < histograms_size; ++i)
            {
                nuint ix = i * self->histogram_length_;
                BuildAndStoreHuffmanTree(&histograms[i].data_[0], self->histogram_length_,
                    alphabet_size, tree, &self->depths_[ix], &self->bits_[ix],
                    storage_ix, storage);
            }
        }
    }

    /* --- block_encoder_inc.h expanded for FN(X) = XCommand --- */

    private static void BuildAndStoreEntropyCodesCommand(MemoryManager* m, BlockEncoder* self,
        HistogramCommand* histograms, nuint histograms_size,
        nuint alphabet_size, HuffmanTree* tree,
        nuint* storage_ix, byte* storage)
    {
        nuint table_size = histograms_size * self->histogram_length_;
        self->depths_ = BROTLI_ALLOC<byte>(m, table_size);
        self->bits_ = BROTLI_ALLOC<ushort>(m, table_size);
        if (BROTLI_IS_OOM(m)) return;

        {
            nuint i;
            for (i = 0; i < histograms_size; ++i)
            {
                nuint ix = i * self->histogram_length_;
                BuildAndStoreHuffmanTree(&histograms[i].data_[0], self->histogram_length_,
                    alphabet_size, tree, &self->depths_[ix], &self->bits_[ix],
                    storage_ix, storage);
            }
        }
    }

    /* --- block_encoder_inc.h expanded for FN(X) = XDistance --- */

    private static void BuildAndStoreEntropyCodesDistance(MemoryManager* m, BlockEncoder* self,
        HistogramDistance* histograms, nuint histograms_size,
        nuint alphabet_size, HuffmanTree* tree,
        nuint* storage_ix, byte* storage)
    {
        nuint table_size = histograms_size * self->histogram_length_;
        self->depths_ = BROTLI_ALLOC<byte>(m, table_size);
        self->bits_ = BROTLI_ALLOC<ushort>(m, table_size);
        if (BROTLI_IS_OOM(m)) return;

        {
            nuint i;
            for (i = 0; i < histograms_size; ++i)
            {
                nuint ix = i * self->histogram_length_;
                BuildAndStoreHuffmanTree(&histograms[i].data_[0], self->histogram_length_,
                    alphabet_size, tree, &self->depths_[ix], &self->bits_[ix],
                    storage_ix, storage);
            }
        }
    }

    internal struct StoreMetablockArena
    {
        public BlockEncoder literal_enc;
        public BlockEncoder command_enc;
        public BlockEncoder distance_enc;
        public EncodeContextMapArena context_map_arena;
    }

    internal static void BrotliStoreMetaBlock(MemoryManager* m,
        byte* input, nuint start_pos, nuint length, nuint mask,
        byte prev_byte, byte prev_byte2, int is_last,
        BrotliEncoderParams* @params, ContextType literal_context_mode,
        Command* commands, nuint n_commands, MetaBlockSplit* mb,
        nuint* storage_ix, byte* storage)
    {
        nuint pos = start_pos;
        nuint i;
        uint num_distance_symbols = @params->dist.alphabet_size_max;
        uint num_effective_distance_symbols = @params->dist.alphabet_size_limit;
        HuffmanTree* tree;
        byte* literal_context_lut = BROTLI_CONTEXT_LUT((nuint)literal_context_mode);
        StoreMetablockArena* arena = null;
        BlockEncoder* literal_enc = null;
        BlockEncoder* command_enc = null;
        BlockEncoder* distance_enc = null;
        BrotliDistanceParams* dist = &@params->dist;
        /* BROTLI_DCHECK(
            num_effective_distance_symbols <= BROTLI_NUM_HISTOGRAM_DISTANCE_SYMBOLS); */

        StoreCompressedMetaBlockHeader(is_last, length, storage_ix, storage);

        tree = BROTLI_ALLOC<HuffmanTree>(m, MAX_HUFFMAN_TREE_SIZE);
        arena = BROTLI_ALLOC<StoreMetablockArena>(m, 1);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(tree) || BROTLI_IS_NULL(arena)) return;
        literal_enc = &arena->literal_enc;
        command_enc = &arena->command_enc;
        distance_enc = &arena->distance_enc;
        InitBlockEncoder(literal_enc, BROTLI_NUM_LITERAL_SYMBOLS,
            mb->literal_split.num_types, mb->literal_split.types,
            mb->literal_split.lengths, mb->literal_split.num_blocks);
        InitBlockEncoder(command_enc, BROTLI_NUM_COMMAND_SYMBOLS,
            mb->command_split.num_types, mb->command_split.types,
            mb->command_split.lengths, mb->command_split.num_blocks);
        InitBlockEncoder(distance_enc, num_effective_distance_symbols,
            mb->distance_split.num_types, mb->distance_split.types,
            mb->distance_split.lengths, mb->distance_split.num_blocks);

        BuildAndStoreBlockSwitchEntropyCodes(literal_enc, tree, storage_ix, storage);
        BuildAndStoreBlockSwitchEntropyCodes(command_enc, tree, storage_ix, storage);
        BuildAndStoreBlockSwitchEntropyCodes(distance_enc, tree, storage_ix, storage);

        BrotliWriteBits(2, dist->distance_postfix_bits, storage_ix, storage);
        BrotliWriteBits(
            4, dist->num_direct_distance_codes >> (int)dist->distance_postfix_bits,
            storage_ix, storage);
        for (i = 0; i < mb->literal_split.num_types; ++i)
        {
            BrotliWriteBits(2, (ulong)literal_context_mode, storage_ix, storage);
        }

        if (mb->literal_context_map_size == 0)
        {
            StoreTrivialContextMap(
                &arena->context_map_arena, mb->literal_histograms_size,
                BROTLI_LITERAL_CONTEXT_BITS, tree, storage_ix, storage);
        }
        else
        {
            EncodeContextMap(m, &arena->context_map_arena,
                mb->literal_context_map, mb->literal_context_map_size,
                mb->literal_histograms_size, tree, storage_ix, storage);
            if (BROTLI_IS_OOM(m)) return;
        }

        if (mb->distance_context_map_size == 0)
        {
            StoreTrivialContextMap(
                &arena->context_map_arena, mb->distance_histograms_size,
                BROTLI_DISTANCE_CONTEXT_BITS, tree, storage_ix, storage);
        }
        else
        {
            EncodeContextMap(m, &arena->context_map_arena,
                mb->distance_context_map, mb->distance_context_map_size,
                mb->distance_histograms_size, tree, storage_ix, storage);
            if (BROTLI_IS_OOM(m)) return;
        }

        BuildAndStoreEntropyCodesLiteral(m, literal_enc, mb->literal_histograms,
            mb->literal_histograms_size, BROTLI_NUM_LITERAL_SYMBOLS, tree,
            storage_ix, storage);
        if (BROTLI_IS_OOM(m)) return;
        BuildAndStoreEntropyCodesCommand(m, command_enc, mb->command_histograms,
            mb->command_histograms_size, BROTLI_NUM_COMMAND_SYMBOLS, tree,
            storage_ix, storage);
        if (BROTLI_IS_OOM(m)) return;
        BuildAndStoreEntropyCodesDistance(m, distance_enc, mb->distance_histograms,
            mb->distance_histograms_size, num_distance_symbols, tree,
            storage_ix, storage);
        if (BROTLI_IS_OOM(m)) return;
        BROTLI_FREE(m, ref tree);

        for (i = 0; i < n_commands; ++i)
        {
            Command cmd = commands[i];
            nuint cmd_code = cmd.cmd_prefix_;
            StoreSymbol(command_enc, cmd_code, storage_ix, storage);
            StoreCommandExtra(&cmd, storage_ix, storage);
            if (mb->literal_context_map_size == 0)
            {
                nuint j;
                for (j = cmd.insert_len_; j != 0; --j)
                {
                    StoreSymbol(literal_enc, input[pos & mask], storage_ix, storage);
                    ++pos;
                }
            }
            else
            {
                nuint j;
                for (j = cmd.insert_len_; j != 0; --j)
                {
                    nuint context =
                        BROTLI_CONTEXT(prev_byte, prev_byte2, literal_context_lut);
                    byte literal = input[pos & mask];
                    StoreSymbolWithContext(literal_enc, literal, context,
                        mb->literal_context_map, storage_ix, storage,
                        BROTLI_LITERAL_CONTEXT_BITS);
                    prev_byte2 = prev_byte;
                    prev_byte = literal;
                    ++pos;
                }
            }
            pos += CommandCopyLen(&cmd);
            if (CommandCopyLen(&cmd) != 0)
            {
                prev_byte2 = input[(pos - 2) & mask];
                prev_byte = input[(pos - 1) & mask];
                if (cmd.cmd_prefix_ >= 128)
                {
                    nuint dist_code = (nuint)(cmd.dist_prefix_ & 0x3FF);
                    uint distnumextra = (uint)cmd.dist_prefix_ >> 10;
                    ulong distextra = cmd.dist_extra_;
                    if (mb->distance_context_map_size == 0)
                    {
                        StoreSymbol(distance_enc, dist_code, storage_ix, storage);
                    }
                    else
                    {
                        nuint context = CommandDistanceContext(&cmd);
                        StoreSymbolWithContext(distance_enc, dist_code, context,
                            mb->distance_context_map, storage_ix, storage,
                            BROTLI_DISTANCE_CONTEXT_BITS);
                    }
                    BrotliWriteBits(distnumextra, distextra, storage_ix, storage);
                }
            }
        }
        CleanupBlockEncoder(m, distance_enc);
        CleanupBlockEncoder(m, command_enc);
        CleanupBlockEncoder(m, literal_enc);
        BROTLI_FREE(m, ref arena);
        if (is_last != 0)
        {
            JumpToByteBoundary(storage_ix, storage);
        }
    }

    internal static void BrotliStoreMetaBlockTrivial(MemoryManager* m,
        byte* input, nuint start_pos, nuint length, nuint mask,
        int is_last, BrotliEncoderParams* @params,
        Command* commands, nuint n_commands,
        nuint* storage_ix, byte* storage)
    {
        MetablockArena* arena = MemoryManager.BROTLI_ALLOC<MetablockArena>(m, 1);
        uint num_distance_symbols = @params->dist.alphabet_size_max;
        if (MemoryManager.BROTLI_IS_OOM(m) || MemoryManager.BROTLI_IS_NULL(arena)) return;

        StoreCompressedMetaBlockHeader(is_last, length, storage_ix, storage);

        HistogramClearLiteral(&arena->lit_histo);
        HistogramClearCommand(&arena->cmd_histo);
        HistogramClearDistance(&arena->dist_histo);

        BuildHistograms(input, start_pos, mask, commands, n_commands,
                        &arena->lit_histo, &arena->cmd_histo, &arena->dist_histo);

        BrotliWriteBits(13, 0, storage_ix, storage);

        BuildAndStoreHuffmanTree(arena->lit_histo.data_, BROTLI_NUM_LITERAL_SYMBOLS,
                                 BROTLI_NUM_LITERAL_SYMBOLS, (HuffmanTree*)arena->tree_,
                                 arena->lit_depth, arena->lit_bits,
                                 storage_ix, storage);
        BuildAndStoreHuffmanTree(arena->cmd_histo.data_, BROTLI_NUM_COMMAND_SYMBOLS,
                                 BROTLI_NUM_COMMAND_SYMBOLS, (HuffmanTree*)arena->tree_,
                                 arena->cmd_depth, arena->cmd_bits,
                                 storage_ix, storage);
        BuildAndStoreHuffmanTree(arena->dist_histo.data_,
                                 MAX_SIMPLE_DISTANCE_ALPHABET_SIZE,
                                 num_distance_symbols, (HuffmanTree*)arena->tree_,
                                 arena->dist_depth, arena->dist_bits,
                                 storage_ix, storage);
        StoreDataWithHuffmanCodes(input, start_pos, mask, commands,
                                  n_commands, arena->lit_depth, arena->lit_bits,
                                  arena->cmd_depth, arena->cmd_bits,
                                  arena->dist_depth, arena->dist_bits,
                                  storage_ix, storage);
        MemoryManager.BROTLI_FREE(m, ref arena);
        if (is_last != 0)
        {
            JumpToByteBoundary(storage_ix, storage);
        }
    }

    internal static void BrotliStoreMetaBlockFast(MemoryManager* m,
        byte* input, nuint start_pos, nuint length, nuint mask,
        int is_last, BrotliEncoderParams* @params,
        Command* commands, nuint n_commands,
        nuint* storage_ix, byte* storage)
    {
        MetablockArena* arena = MemoryManager.BROTLI_ALLOC<MetablockArena>(m, 1);
        uint num_distance_symbols = @params->dist.alphabet_size_max;
        uint distance_alphabet_bits =
            Log2FloorNonZero(num_distance_symbols - 1) + 1;
        if (MemoryManager.BROTLI_IS_OOM(m) || MemoryManager.BROTLI_IS_NULL(arena)) return;

        StoreCompressedMetaBlockHeader(is_last, length, storage_ix, storage);

        BrotliWriteBits(13, 0, storage_ix, storage);

        if (n_commands <= 128)
        {
            uint* histogram = stackalloc uint[BROTLI_NUM_LITERAL_SYMBOLS];
            new Span<uint>(histogram, BROTLI_NUM_LITERAL_SYMBOLS).Clear();
            nuint pos = start_pos;
            nuint num_literals = 0;
            nuint i;
            for (i = 0; i < n_commands; ++i)
            {
                Command cmd = commands[i];
                nuint j;
                for (j = cmd.insert_len_; j != 0; --j)
                {
                    ++histogram[input[pos & mask]];
                    ++pos;
                }
                num_literals += cmd.insert_len_;
                pos += CommandCopyLen(&cmd);
            }
            BrotliBuildAndStoreHuffmanTreeFast((HuffmanTree*)arena->tree_, histogram, num_literals,
                                               /* max_bits = */ 8,
                                               arena->lit_depth, arena->lit_bits,
                                               storage_ix, storage);
            StoreStaticCommandHuffmanTree(storage_ix, storage);
            StoreStaticDistanceHuffmanTree(storage_ix, storage);
            fixed (byte* static_cmd_depth = kStaticCommandCodeDepth)
            fixed (ushort* static_cmd_bits = kStaticCommandCodeBits)
            fixed (byte* static_dist_depth = kStaticDistanceCodeDepth)
            fixed (ushort* static_dist_bits = kStaticDistanceCodeBits)
            {
                StoreDataWithHuffmanCodes(input, start_pos, mask, commands,
                                          n_commands, arena->lit_depth, arena->lit_bits,
                                          static_cmd_depth,
                                          static_cmd_bits,
                                          static_dist_depth,
                                          static_dist_bits,
                                          storage_ix, storage);
            }
        }
        else
        {
            HistogramClearLiteral(&arena->lit_histo);
            HistogramClearCommand(&arena->cmd_histo);
            HistogramClearDistance(&arena->dist_histo);
            BuildHistograms(input, start_pos, mask, commands, n_commands,
                            &arena->lit_histo, &arena->cmd_histo, &arena->dist_histo);
            BrotliBuildAndStoreHuffmanTreeFast((HuffmanTree*)arena->tree_, arena->lit_histo.data_,
                                               arena->lit_histo.total_count_,
                                               /* max_bits = */ 8,
                                               arena->lit_depth, arena->lit_bits,
                                               storage_ix, storage);
            BrotliBuildAndStoreHuffmanTreeFast((HuffmanTree*)arena->tree_, arena->cmd_histo.data_,
                                               arena->cmd_histo.total_count_,
                                               /* max_bits = */ 10,
                                               arena->cmd_depth, arena->cmd_bits,
                                               storage_ix, storage);
            BrotliBuildAndStoreHuffmanTreeFast((HuffmanTree*)arena->tree_, arena->dist_histo.data_,
                                               arena->dist_histo.total_count_,
                                               /* max_bits = */
                                               distance_alphabet_bits,
                                               arena->dist_depth, arena->dist_bits,
                                               storage_ix, storage);
            StoreDataWithHuffmanCodes(input, start_pos, mask, commands,
                                      n_commands, arena->lit_depth, arena->lit_bits,
                                      arena->cmd_depth, arena->cmd_bits,
                                      arena->dist_depth, arena->dist_bits,
                                      storage_ix, storage);
        }

        MemoryManager.BROTLI_FREE(m, ref arena);

        if (is_last != 0)
        {
            JumpToByteBoundary(storage_ix, storage);
        }
    }

    /* |nibblesbits| represents the 2 bits to encode MNIBBLES (0-3)
       REQUIRES: length > 0
       REQUIRES: length <= (1 << 24) */
    private static void BrotliEncodeMlen(nuint length, ulong* bits,
                                         nuint* numbits, ulong* nibblesbits)
    {
        nuint lg = (length == 1) ? 1 : Log2FloorNonZero((nuint)(uint)(length - 1)) + 1;
        nuint mnibbles = (lg < 16 ? 16 : (lg + 3)) / 4;
        *nibblesbits = mnibbles - 4;
        *numbits = mnibbles * 4;
        *bits = length - 1;
    }

    /* Stores the uncompressed meta-block header.
       REQUIRES: length > 0
       REQUIRES: length <= (1 << 24) */
    private static void BrotliStoreUncompressedMetaBlockHeader(nuint length,
                                                               nuint* storage_ix,
                                                               byte* storage)
    {
        ulong lenbits;
        nuint nlenbits;
        ulong nibblesbits;

        /* Write ISLAST bit.
           Uncompressed block cannot be the last one, so set to 0. */
        BrotliWriteBits(1, 0, storage_ix, storage);
        BrotliEncodeMlen(length, &lenbits, &nlenbits, &nibblesbits);
        BrotliWriteBits(2, nibblesbits, storage_ix, storage);
        BrotliWriteBits(nlenbits, lenbits, storage_ix, storage);
        /* Write ISUNCOMPRESSED bit. */
        BrotliWriteBits(1, 1, storage_ix, storage);
    }

    /* This is for storing uncompressed blocks (simple raw storage of
       bytes-as-bytes). */
    internal static void BrotliStoreUncompressedMetaBlock(int is_final_block,
                                                          byte* input,
                                                          nuint position, nuint mask,
                                                          nuint len,
                                                          nuint* storage_ix,
                                                          byte* storage)
    {
        nuint masked_pos = position & mask;
        BrotliStoreUncompressedMetaBlockHeader(len, storage_ix, storage);
        JumpToByteBoundary(storage_ix, storage);

        if (masked_pos + len > mask + 1)
        {
            nuint len1 = mask + 1 - masked_pos;
            Buffer.MemoryCopy(&input[masked_pos], &storage[*storage_ix >> 3], len1, len1);
            *storage_ix += len1 << 3;
            len -= len1;
            masked_pos = 0;
        }
        Buffer.MemoryCopy(&input[masked_pos], &storage[*storage_ix >> 3], len, len);
        *storage_ix += len << 3;

        /* We need to clear the next 4 bytes to continue to be
           compatible with BrotliWriteBits. */
        BrotliWriteBitsPrepareStorage(*storage_ix, storage);

        /* Since the uncompressed block itself may not be the final block, add an
           empty one after this. */
        if (is_final_block != 0)
        {
            BrotliWriteBits(1, 1, storage_ix, storage);  /* islast */
            BrotliWriteBits(1, 1, storage_ix, storage);  /* isempty */
            JumpToByteBoundary(storage_ix, storage);
        }
    }
}
