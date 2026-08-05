// Port of c/enc/block_splitter.{h,c} + block_splitter_inc.h (brotli v1.1.0) —
// block split point selection. The block_splitter_inc.h template
// (InitialEntropyCodes / RandomSample / RefineEntropyCodes / FindBlocks /
// RemapBlockIds / BuildBlockHistograms / ClusterBlocks / SplitByteVector) is
// expanded manually for Literal (uint8_t data) / Command / Distance (uint16_t
// data), the Histogram.cs house triple pattern. FindBlocks is the hot FP zone:
// every double expression keeps the exact C shape (PORTING.md).
//
// Rename note: the C file-local `static double BitCost(size_t)` helper is
// ported as `BitCostFn` because `BitCost` already names the class holding the
// bit_cost.{h,c} port (a C# member cannot shadow its enclosing type's name).

using System.Runtime.CompilerServices;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.BitCost;
using static DnBrotli.Enc.Cluster;
using static DnBrotli.Enc.Command;
using static DnBrotli.Enc.FastLog;
using static DnBrotli.Enc.Histogram;
using static DnBrotli.Enc.MemoryManager;
using static DnBrotli.Enc.Quality;

namespace DnBrotli.Enc;

/// <summary><c>struct BlockSplit</c> (block_splitter.h).</summary>
internal unsafe struct BlockSplit
{
    public nuint num_types;   /* Amount of distinct types */
    public nuint num_blocks;  /* Amount of values in types and length */
    public byte* types;
    public uint* lengths;

    public nuint types_alloc_size;
    public nuint lengths_alloc_size;
}

internal static unsafe class BlockSplitter
{
    private const nuint kMaxLiteralHistograms = 100;
    private const nuint kMaxCommandHistograms = 50;
    private const double kLiteralBlockSwitchCost = 28.1;
    private const double kCommandBlockSwitchCost = 13.5;
    private const double kDistanceBlockSwitchCost = 14.6;
    private const nuint kLiteralStrideLength = 70;
    private const nuint kCommandStrideLength = 40;
    private const nuint kDistanceStrideLength = 40;
    private const nuint kSymbolsPerLiteralHistogram = 544;
    private const nuint kSymbolsPerCommandHistogram = 530;
    private const nuint kSymbolsPerDistanceHistogram = 544;
    private const nuint kMinLengthForBlockSplitting = 128;
    private const nuint kIterMulForRefining = 2;
    private const nuint kMinItersForRefining = 100;

    private static nuint CountLiterals(Command* cmds, nuint num_commands)
    {
        /* Count how many we have. */
        nuint total_length = 0;
        nuint i;
        for (i = 0; i < num_commands; ++i)
        {
            total_length += cmds[i].insert_len_;
        }
        return total_length;
    }

    private static void CopyLiteralsToByteArray(Command* cmds,
                                                nuint num_commands,
                                                byte* data,
                                                nuint offset,
                                                nuint mask,
                                                byte* literals)
    {
        nuint pos = 0;
        nuint from_pos = offset & mask;
        nuint i;
        for (i = 0; i < num_commands; ++i)
        {
            nuint insert_len = cmds[i].insert_len_;
            if (from_pos + insert_len > mask)
            {
                nuint head_size = mask + 1 - from_pos;
                Buffer.MemoryCopy(data + from_pos, literals + pos, head_size, head_size);  /* memcpy */
                from_pos = 0;
                pos += head_size;
                insert_len -= head_size;
            }
            if (insert_len > 0)
            {
                Buffer.MemoryCopy(data + from_pos, literals + pos, insert_len, insert_len);  /* memcpy */
                pos += insert_len;
            }
            from_pos = (from_pos + insert_len + CommandCopyLen(&cmds[i])) & mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MyRand(uint* seed)
    {
        /* Initial seed should be 7. In this case, loop length is (1 << 29). */
        *seed *= 16807U;
        return *seed;
    }

    /// <summary>C name: <c>BitCost</c> (block_splitter.c file-local helper); renamed —
    /// see the header comment.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double BitCostFn(nuint count)
    {
        return count == 0 ? -2.0 : FastLog2(count);
    }

    private const int HISTOGRAMS_PER_BATCH = 64;
    private const int CLUSTERS_PER_BATCH = 16;

    /* ==================== FN = Literal (DataType uint8_t) ==================== */

    private static void InitialEntropyCodesLiteral(byte* data, nuint length,
                                                   nuint stride,
                                                   nuint num_histograms,
                                                   HistogramLiteral* histograms)
    {
        uint seed = 7;
        nuint block_length = length / num_histograms;
        nuint i;
        ClearHistogramsLiteral(histograms, num_histograms);
        for (i = 0; i < num_histograms; ++i)
        {
            nuint pos = length * i / num_histograms;
            if (i != 0)
            {
                pos += MyRand(&seed) % block_length;
            }
            if (pos + stride >= length)
            {
                pos = length - stride - 1;
            }
            HistogramAddVectorLiteral(&histograms[i], data + pos, stride);
        }
    }

    private static void RandomSampleLiteral(uint* seed,
                                            byte* data,
                                            nuint length,
                                            nuint stride,
                                            HistogramLiteral* sample)
    {
        nuint pos = 0;
        if (stride >= length)
        {
            stride = length;
        }
        else
        {
            pos = MyRand(seed) % (length - stride + 1);
        }
        HistogramAddVectorLiteral(sample, data + pos, stride);
    }

    private static void RefineEntropyCodesLiteral(byte* data, nuint length,
                                                  nuint stride,
                                                  nuint num_histograms,
                                                  HistogramLiteral* histograms,
                                                  HistogramLiteral* tmp)
    {
        nuint iters =
            kIterMulForRefining * length / stride + kMinItersForRefining;
        uint seed = 7;
        nuint iter;
        iters = ((iters + num_histograms - 1) / num_histograms) * num_histograms;
        for (iter = 0; iter < iters; ++iter)
        {
            HistogramClearLiteral(tmp);
            RandomSampleLiteral(&seed, data, length, stride, tmp);
            HistogramAddHistogramLiteral(&histograms[iter % num_histograms], tmp);
        }
    }

    /* Assigns a block id from the range [0, num_histograms) to each data element
       in data[0..length) and fills in block_id[0..length) with the assigned values.
       Returns the number of blocks, i.e. one plus the number of block switches. */
    private static nuint FindBlocksLiteral(byte* data, nuint length,
                                           double block_switch_bitcost,
                                           nuint num_histograms,
                                           HistogramLiteral* histograms,
                                           double* insert_cost,
                                           double* cost,
                                           byte* switch_signal,
                                           byte* block_id)
    {
        nuint alphabet_size = HistogramDataSizeLiteral();
        nuint bitmap_len = (num_histograms + 7) >> 3;
        nuint num_blocks = 1;
        nuint byte_ix;
        nuint i;
        nuint j;

        /* Trivial case: single historgram -> single block type. */
        if (num_histograms <= 1)
        {
            for (i = 0; i < length; ++i)
            {
                block_id[i] = 0;
            }
            return 1;
        }

        /* Fill bitcost for each symbol of all histograms.
         * Non-existing symbol cost: 2 + log2(total_count).
         * Regular symbol cost: -log2(symbol_count / total_count). */
        new Span<double>(insert_cost, (int)(alphabet_size * num_histograms)).Clear();  /* memset */
        for (i = 0; i < num_histograms; ++i)
        {
            insert_cost[i] = FastLog2((uint)histograms[i].total_count_);
        }
        for (i = alphabet_size; i != 0;)
        {
            /* Reverse order to use the 0-th row as a temporary storage. */
            --i;
            for (j = 0; j < num_histograms; ++j)
            {
                insert_cost[i * num_histograms + j] =
                    insert_cost[j] - BitCostFn(histograms[j].data_[i]);
            }
        }

        /* After each iteration of this loop, cost[k] will contain the difference
           between the minimum cost of arriving at the current byte position using
           entropy code k, and the minimum cost of arriving at the current byte
           position. This difference is capped at the block switch cost, and if it
           reaches block switch cost, it means that when we trace back from the last
           position, we need to switch here. */
        new Span<double>(cost, (int)num_histograms).Clear();  /* memset */
        new Span<byte>(switch_signal, (int)(length * bitmap_len)).Clear();  /* memset */
        for (byte_ix = 0; byte_ix < length; ++byte_ix)
        {
            nuint ix = byte_ix * bitmap_len;
            nuint symbol = data[byte_ix];
            nuint insert_cost_ix = symbol * num_histograms;
            double min_cost = 1e99;
            double block_switch_cost = block_switch_bitcost;
            nuint k;
            for (k = 0; k < num_histograms; ++k)
            {
                /* We are coding the symbol with entropy code k. */
                cost[k] += insert_cost[insert_cost_ix + k];
                if (cost[k] < min_cost)
                {
                    min_cost = cost[k];
                    block_id[byte_ix] = (byte)k;
                }
            }
            /* More blocks for the beginning. */
            if (byte_ix < 2000)
            {
                block_switch_cost *= 0.77 + 0.07 * (double)byte_ix / 2000;
            }
            for (k = 0; k < num_histograms; ++k)
            {
                cost[k] -= min_cost;
                if (cost[k] >= block_switch_cost)
                {
                    byte mask = (byte)(1u << (int)(k & 7));
                    cost[k] = block_switch_cost;
                    switch_signal[ix + (k >> 3)] |= mask;
                }
            }
        }

        byte_ix = length - 1;
        {  /* Trace back from the last position and switch at the marked places. */
            nuint ix = byte_ix * bitmap_len;
            byte cur_id = block_id[byte_ix];
            while (byte_ix > 0)
            {
                byte mask = (byte)(1u << (cur_id & 7));
                --byte_ix;
                ix -= bitmap_len;
                if ((switch_signal[ix + (nuint)(cur_id >> 3)] & mask) != 0)
                {
                    if (cur_id != block_id[byte_ix])
                    {
                        cur_id = block_id[byte_ix];
                        ++num_blocks;
                    }
                }
                block_id[byte_ix] = cur_id;
            }
        }
        return num_blocks;
    }

    private static nuint RemapBlockIdsLiteral(byte* block_ids, nuint length,
                                              ushort* new_id, nuint num_histograms)
    {
        const ushort kInvalidId = 256;
        ushort next_id = 0;
        nuint i;
        for (i = 0; i < num_histograms; ++i)
        {
            new_id[i] = kInvalidId;
        }
        for (i = 0; i < length; ++i)
        {
            if (new_id[block_ids[i]] == kInvalidId)
            {
                new_id[block_ids[i]] = next_id++;
            }
        }
        for (i = 0; i < length; ++i)
        {
            block_ids[i] = (byte)new_id[block_ids[i]];
        }
        return next_id;
    }

    private static void BuildBlockHistogramsLiteral(byte* data, nuint length,
                                                    byte* block_ids,
                                                    nuint num_histograms,
                                                    HistogramLiteral* histograms)
    {
        nuint i;
        ClearHistogramsLiteral(histograms, num_histograms);
        for (i = 0; i < length; ++i)
        {
            HistogramAddLiteral(&histograms[block_ids[i]], data[i]);
        }
    }

    /* Given the initial partitioning build partitioning with limited number
     * of histograms (and block types). */
    private static void ClusterBlocksLiteral(MemoryManager* m,
                                             byte* data, nuint length,
                                             nuint num_blocks,
                                             byte* block_ids,
                                             BlockSplit* split)
    {
        uint* histogram_symbols = BROTLI_ALLOC<uint>(m, num_blocks);
        uint* u32 = BROTLI_ALLOC<uint>(m, num_blocks + 4 * HISTOGRAMS_PER_BATCH);
        nuint expected_num_clusters = CLUSTERS_PER_BATCH *
            (num_blocks + HISTOGRAMS_PER_BATCH - 1) / HISTOGRAMS_PER_BATCH;
        nuint all_histograms_size = 0;
        nuint all_histograms_capacity = expected_num_clusters;
        HistogramLiteral* all_histograms =
            BROTLI_ALLOC<HistogramLiteral>(m, all_histograms_capacity);
        nuint cluster_size_size = 0;
        nuint cluster_size_capacity = expected_num_clusters;
        uint* cluster_size = BROTLI_ALLOC<uint>(m, cluster_size_capacity);
        nuint num_clusters = 0;
        HistogramLiteral* histograms = BROTLI_ALLOC<HistogramLiteral>(m,
            BROTLI_MIN(num_blocks, (nuint)HISTOGRAMS_PER_BATCH));  /* BROTLI_MIN(size_t, ...) */
        nuint max_num_pairs =
            HISTOGRAMS_PER_BATCH * HISTOGRAMS_PER_BATCH / 2;
        nuint pairs_capacity = max_num_pairs + 1;
        HistogramPair* pairs = BROTLI_ALLOC<HistogramPair>(m, pairs_capacity);
        nuint pos = 0;
        uint* clusters;
        nuint num_final_clusters;
        const uint kInvalidIndex = uint.MaxValue;  /* BROTLI_UINT32_MAX */
        uint* new_index;
        nuint i;
        uint* sizes = u32 + 0 * HISTOGRAMS_PER_BATCH;
        uint* new_clusters = u32 + 1 * HISTOGRAMS_PER_BATCH;
        uint* symbols = u32 + 2 * HISTOGRAMS_PER_BATCH;
        uint* remap = u32 + 3 * HISTOGRAMS_PER_BATCH;
        uint* block_lengths = u32 + 4 * HISTOGRAMS_PER_BATCH;
        /* TODO(eustas): move to arena? */
        HistogramLiteral* tmp = BROTLI_ALLOC<HistogramLiteral>(m, 2);

        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(histogram_symbols) ||
            BROTLI_IS_NULL(u32) || BROTLI_IS_NULL(all_histograms) ||
            BROTLI_IS_NULL(cluster_size) || BROTLI_IS_NULL(histograms) ||
            BROTLI_IS_NULL(pairs) || BROTLI_IS_NULL(tmp))
        {
            return;
        }

        new Span<uint>(u32, (int)(num_blocks + 4 * HISTOGRAMS_PER_BATCH)).Clear();  /* memset */

        /* Calculate block lengths (convert repeating values -> series length). */
        {
            nuint block_idx = 0;
            for (i = 0; i < length; ++i)
            {
                ++block_lengths[block_idx];
                if (i + 1 == length || block_ids[i] != block_ids[i + 1])
                {
                    ++block_idx;
                }
            }
        }

        /* Pre-cluster blocks (cluster batches). */
        for (i = 0; i < num_blocks; i += HISTOGRAMS_PER_BATCH)
        {
            nuint num_to_combine =
                BROTLI_MIN(num_blocks - i, (nuint)HISTOGRAMS_PER_BATCH);  /* BROTLI_MIN(size_t, ...) */
            nuint num_new_clusters;
            nuint j;
            for (j = 0; j < num_to_combine; ++j)
            {
                nuint k;
                nuint block_length = block_lengths[i + j];
                HistogramClearLiteral(&histograms[j]);
                for (k = 0; k < block_length; ++k)
                {
                    HistogramAddLiteral(&histograms[j], data[pos++]);
                }
                histograms[j].bit_cost_ = BrotliPopulationCostLiteral(&histograms[j]);
                new_clusters[j] = (uint)j;
                symbols[j] = (uint)j;
                sizes[j] = 1;
            }
            num_new_clusters = BrotliHistogramCombineLiteral(
                histograms, tmp, sizes, symbols, new_clusters, pairs, num_to_combine,
                num_to_combine, HISTOGRAMS_PER_BATCH, max_num_pairs);
            BROTLI_ENSURE_CAPACITY(m, ref all_histograms,
                ref all_histograms_capacity, all_histograms_size + num_new_clusters);
            BROTLI_ENSURE_CAPACITY(m, ref cluster_size,
                ref cluster_size_capacity, cluster_size_size + num_new_clusters);
            if (BROTLI_IS_OOM(m)) return;
            for (j = 0; j < num_new_clusters; ++j)
            {
                all_histograms[all_histograms_size++] = histograms[new_clusters[j]];
                cluster_size[cluster_size_size++] = sizes[new_clusters[j]];
                remap[new_clusters[j]] = (uint)j;
            }
            for (j = 0; j < num_to_combine; ++j)
            {
                histogram_symbols[i + j] = (uint)num_clusters + remap[symbols[j]];
            }
            num_clusters += num_new_clusters;
        }
        BROTLI_FREE(m, ref histograms);

        /* Final clustering. */
        max_num_pairs = BROTLI_MIN(
            64 * num_clusters, (num_clusters / 2) * num_clusters);  /* BROTLI_MIN(size_t, ...) */
        if (pairs_capacity < max_num_pairs + 1)
        {
            BROTLI_FREE(m, ref pairs);
            pairs = BROTLI_ALLOC<HistogramPair>(m, max_num_pairs + 1);
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(pairs)) return;
        }
        clusters = BROTLI_ALLOC<uint>(m, num_clusters);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(clusters)) return;
        for (i = 0; i < num_clusters; ++i)
        {
            clusters[i] = (uint)i;
        }
        num_final_clusters = BrotliHistogramCombineLiteral(
            all_histograms, tmp, cluster_size, histogram_symbols, clusters, pairs,
            num_clusters, num_blocks, BROTLI_MAX_NUMBER_OF_BLOCK_TYPES,
            max_num_pairs);
        BROTLI_FREE(m, ref pairs);
        BROTLI_FREE(m, ref cluster_size);

        /* Assign blocks to final histograms. */
        new_index = BROTLI_ALLOC<uint>(m, num_clusters);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(new_index)) return;
        for (i = 0; i < num_clusters; ++i) new_index[i] = kInvalidIndex;
        pos = 0;
        {
            uint next_index = 0;
            for (i = 0; i < num_blocks; ++i)
            {
                nuint j;
                uint best_out;
                double best_bits;
                HistogramClearLiteral(tmp);
                for (j = 0; j < block_lengths[i]; ++j)
                {
                    HistogramAddLiteral(tmp, data[pos++]);
                }
                /* Among equally good histograms prefer last used. */
                /* TODO(eustas): should we give a block-switch discount here? */
                best_out = (i == 0) ? histogram_symbols[0] : histogram_symbols[i - 1];
                best_bits = BrotliHistogramBitCostDistanceLiteral(
                    tmp, &all_histograms[best_out], tmp + 1);
                for (j = 0; j < num_final_clusters; ++j)
                {
                    double cur_bits = BrotliHistogramBitCostDistanceLiteral(
                        tmp, &all_histograms[clusters[j]], tmp + 1);
                    if (cur_bits < best_bits)
                    {
                        best_bits = cur_bits;
                        best_out = clusters[j];
                    }
                }
                histogram_symbols[i] = best_out;
                if (new_index[best_out] == kInvalidIndex)
                {
                    new_index[best_out] = next_index++;
                }
            }
        }
        BROTLI_FREE(m, ref tmp);
        BROTLI_FREE(m, ref clusters);
        BROTLI_FREE(m, ref all_histograms);
        BROTLI_ENSURE_CAPACITY(
            m, ref split->types, ref split->types_alloc_size, num_blocks);
        BROTLI_ENSURE_CAPACITY(
            m, ref split->lengths, ref split->lengths_alloc_size, num_blocks);
        if (BROTLI_IS_OOM(m)) return;

        /* Rewrite final assignment to block-split. There might be less blocks
         * than |num_blocks| due to clustering. */
        {
            uint cur_length = 0;
            nuint block_idx = 0;
            byte max_type = 0;
            for (i = 0; i < num_blocks; ++i)
            {
                cur_length += block_lengths[i];
                if (i + 1 == num_blocks ||
                    histogram_symbols[i] != histogram_symbols[i + 1])
                {
                    byte id = (byte)new_index[histogram_symbols[i]];
                    split->types[block_idx] = id;
                    split->lengths[block_idx] = cur_length;
                    max_type = BROTLI_MAX(max_type, id);  /* BROTLI_MAX(uint8_t, ...) */
                    cur_length = 0;
                    ++block_idx;
                }
            }
            split->num_blocks = block_idx;
            split->num_types = (nuint)max_type + 1;
        }
        BROTLI_FREE(m, ref new_index);
        BROTLI_FREE(m, ref u32);
        BROTLI_FREE(m, ref histogram_symbols);
    }

    /* Create BlockSplit (partitioning) given the limits, estimates and "effort"
     * parameters.
     *
     * NB: max_histograms is often less than number of histograms allowed by format;
     *     this is done intentionally, to save some "space" for context-aware
     *     clustering (here entropy is estimated for context-free symbols). */
    private static void SplitByteVectorLiteral(MemoryManager* m,
                                               byte* data, nuint length,
                                               nuint symbols_per_histogram,
                                               nuint max_histograms,
                                               nuint sampling_stride_length,
                                               double block_switch_cost,
                                               BrotliEncoderParams* @params,
                                               BlockSplit* split)
    {
        nuint data_size = HistogramDataSizeLiteral();
        HistogramLiteral* histograms;
        HistogramLiteral* tmp;
        /* Calculate number of histograms; initial estimate is one histogram per
         * specified amount of symbols; however, this value is capped. */
        nuint num_histograms = length / symbols_per_histogram + 1;
        if (num_histograms > max_histograms)
        {
            num_histograms = max_histograms;
        }

        /* Corner case: no input. */
        if (length == 0)
        {
            split->num_types = 1;
            return;
        }

        if (length < kMinLengthForBlockSplitting)
        {
            BROTLI_ENSURE_CAPACITY(m,
                ref split->types, ref split->types_alloc_size, split->num_blocks + 1);
            BROTLI_ENSURE_CAPACITY(m,
                ref split->lengths, ref split->lengths_alloc_size, split->num_blocks + 1);
            if (BROTLI_IS_OOM(m)) return;
            split->num_types = 1;
            split->types[split->num_blocks] = 0;
            split->lengths[split->num_blocks] = (uint)length;
            split->num_blocks++;
            return;
        }
        histograms = BROTLI_ALLOC<HistogramLiteral>(m, num_histograms + 1);
        tmp = histograms + num_histograms;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(histograms)) return;
        /* Find good entropy codes. */
        InitialEntropyCodesLiteral(data, length,
                                   sampling_stride_length,
                                   num_histograms, histograms);
        RefineEntropyCodesLiteral(data, length,
                                  sampling_stride_length,
                                  num_histograms, histograms, tmp);
        {
            /* Find a good path through literals with the good entropy codes. */
            byte* block_ids = BROTLI_ALLOC<byte>(m, length);
            nuint num_blocks = 0;
            nuint bitmaplen = (num_histograms + 7) >> 3;
            double* insert_cost = BROTLI_ALLOC<double>(m, data_size * num_histograms);
            double* cost = BROTLI_ALLOC<double>(m, num_histograms);
            byte* switch_signal = BROTLI_ALLOC<byte>(m, length * bitmaplen);
            ushort* new_id = BROTLI_ALLOC<ushort>(m, num_histograms);
            nuint iters = @params->quality < HQ_ZOPFLIFICATION_QUALITY ? 3 : (nuint)10;
            nuint i;
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(block_ids) ||
                BROTLI_IS_NULL(insert_cost) || BROTLI_IS_NULL(cost) ||
                BROTLI_IS_NULL(switch_signal) || BROTLI_IS_NULL(new_id))
            {
                return;
            }
            for (i = 0; i < iters; ++i)
            {
                num_blocks = FindBlocksLiteral(data, length,
                                               block_switch_cost,
                                               num_histograms, histograms,
                                               insert_cost, cost, switch_signal,
                                               block_ids);
                num_histograms = RemapBlockIdsLiteral(block_ids, length,
                                                      new_id, num_histograms);
                BuildBlockHistogramsLiteral(data, length, block_ids,
                                            num_histograms, histograms);
            }
            BROTLI_FREE(m, ref insert_cost);
            BROTLI_FREE(m, ref cost);
            BROTLI_FREE(m, ref switch_signal);
            BROTLI_FREE(m, ref new_id);
            BROTLI_FREE(m, ref histograms);
            ClusterBlocksLiteral(m, data, length, num_blocks, block_ids, split);
            if (BROTLI_IS_OOM(m)) return;
            BROTLI_FREE(m, ref block_ids);
        }
    }

    /* ==================== FN = Command (DataType uint16_t) ==================== */

    private static void InitialEntropyCodesCommand(ushort* data, nuint length,
                                                   nuint stride,
                                                   nuint num_histograms,
                                                   HistogramCommand* histograms)
    {
        uint seed = 7;
        nuint block_length = length / num_histograms;
        nuint i;
        ClearHistogramsCommand(histograms, num_histograms);
        for (i = 0; i < num_histograms; ++i)
        {
            nuint pos = length * i / num_histograms;
            if (i != 0)
            {
                pos += MyRand(&seed) % block_length;
            }
            if (pos + stride >= length)
            {
                pos = length - stride - 1;
            }
            HistogramAddVectorCommand(&histograms[i], data + pos, stride);
        }
    }

    private static void RandomSampleCommand(uint* seed,
                                            ushort* data,
                                            nuint length,
                                            nuint stride,
                                            HistogramCommand* sample)
    {
        nuint pos = 0;
        if (stride >= length)
        {
            stride = length;
        }
        else
        {
            pos = MyRand(seed) % (length - stride + 1);
        }
        HistogramAddVectorCommand(sample, data + pos, stride);
    }

    private static void RefineEntropyCodesCommand(ushort* data, nuint length,
                                                  nuint stride,
                                                  nuint num_histograms,
                                                  HistogramCommand* histograms,
                                                  HistogramCommand* tmp)
    {
        nuint iters =
            kIterMulForRefining * length / stride + kMinItersForRefining;
        uint seed = 7;
        nuint iter;
        iters = ((iters + num_histograms - 1) / num_histograms) * num_histograms;
        for (iter = 0; iter < iters; ++iter)
        {
            HistogramClearCommand(tmp);
            RandomSampleCommand(&seed, data, length, stride, tmp);
            HistogramAddHistogramCommand(&histograms[iter % num_histograms], tmp);
        }
    }

    /* Assigns a block id from the range [0, num_histograms) to each data element
       in data[0..length) and fills in block_id[0..length) with the assigned values.
       Returns the number of blocks, i.e. one plus the number of block switches. */
    private static nuint FindBlocksCommand(ushort* data, nuint length,
                                           double block_switch_bitcost,
                                           nuint num_histograms,
                                           HistogramCommand* histograms,
                                           double* insert_cost,
                                           double* cost,
                                           byte* switch_signal,
                                           byte* block_id)
    {
        nuint alphabet_size = HistogramDataSizeCommand();
        nuint bitmap_len = (num_histograms + 7) >> 3;
        nuint num_blocks = 1;
        nuint byte_ix;
        nuint i;
        nuint j;

        /* Trivial case: single historgram -> single block type. */
        if (num_histograms <= 1)
        {
            for (i = 0; i < length; ++i)
            {
                block_id[i] = 0;
            }
            return 1;
        }

        /* Fill bitcost for each symbol of all histograms.
         * Non-existing symbol cost: 2 + log2(total_count).
         * Regular symbol cost: -log2(symbol_count / total_count). */
        new Span<double>(insert_cost, (int)(alphabet_size * num_histograms)).Clear();  /* memset */
        for (i = 0; i < num_histograms; ++i)
        {
            insert_cost[i] = FastLog2((uint)histograms[i].total_count_);
        }
        for (i = alphabet_size; i != 0;)
        {
            /* Reverse order to use the 0-th row as a temporary storage. */
            --i;
            for (j = 0; j < num_histograms; ++j)
            {
                insert_cost[i * num_histograms + j] =
                    insert_cost[j] - BitCostFn(histograms[j].data_[i]);
            }
        }

        /* After each iteration of this loop, cost[k] will contain the difference
           between the minimum cost of arriving at the current byte position using
           entropy code k, and the minimum cost of arriving at the current byte
           position. This difference is capped at the block switch cost, and if it
           reaches block switch cost, it means that when we trace back from the last
           position, we need to switch here. */
        new Span<double>(cost, (int)num_histograms).Clear();  /* memset */
        new Span<byte>(switch_signal, (int)(length * bitmap_len)).Clear();  /* memset */
        for (byte_ix = 0; byte_ix < length; ++byte_ix)
        {
            nuint ix = byte_ix * bitmap_len;
            nuint symbol = data[byte_ix];
            nuint insert_cost_ix = symbol * num_histograms;
            double min_cost = 1e99;
            double block_switch_cost = block_switch_bitcost;
            nuint k;
            for (k = 0; k < num_histograms; ++k)
            {
                /* We are coding the symbol with entropy code k. */
                cost[k] += insert_cost[insert_cost_ix + k];
                if (cost[k] < min_cost)
                {
                    min_cost = cost[k];
                    block_id[byte_ix] = (byte)k;
                }
            }
            /* More blocks for the beginning. */
            if (byte_ix < 2000)
            {
                block_switch_cost *= 0.77 + 0.07 * (double)byte_ix / 2000;
            }
            for (k = 0; k < num_histograms; ++k)
            {
                cost[k] -= min_cost;
                if (cost[k] >= block_switch_cost)
                {
                    byte mask = (byte)(1u << (int)(k & 7));
                    cost[k] = block_switch_cost;
                    switch_signal[ix + (k >> 3)] |= mask;
                }
            }
        }

        byte_ix = length - 1;
        {  /* Trace back from the last position and switch at the marked places. */
            nuint ix = byte_ix * bitmap_len;
            byte cur_id = block_id[byte_ix];
            while (byte_ix > 0)
            {
                byte mask = (byte)(1u << (cur_id & 7));
                --byte_ix;
                ix -= bitmap_len;
                if ((switch_signal[ix + (nuint)(cur_id >> 3)] & mask) != 0)
                {
                    if (cur_id != block_id[byte_ix])
                    {
                        cur_id = block_id[byte_ix];
                        ++num_blocks;
                    }
                }
                block_id[byte_ix] = cur_id;
            }
        }
        return num_blocks;
    }

    private static nuint RemapBlockIdsCommand(byte* block_ids, nuint length,
                                              ushort* new_id, nuint num_histograms)
    {
        const ushort kInvalidId = 256;
        ushort next_id = 0;
        nuint i;
        for (i = 0; i < num_histograms; ++i)
        {
            new_id[i] = kInvalidId;
        }
        for (i = 0; i < length; ++i)
        {
            if (new_id[block_ids[i]] == kInvalidId)
            {
                new_id[block_ids[i]] = next_id++;
            }
        }
        for (i = 0; i < length; ++i)
        {
            block_ids[i] = (byte)new_id[block_ids[i]];
        }
        return next_id;
    }

    private static void BuildBlockHistogramsCommand(ushort* data, nuint length,
                                                    byte* block_ids,
                                                    nuint num_histograms,
                                                    HistogramCommand* histograms)
    {
        nuint i;
        ClearHistogramsCommand(histograms, num_histograms);
        for (i = 0; i < length; ++i)
        {
            HistogramAddCommand(&histograms[block_ids[i]], data[i]);
        }
    }

    /* Given the initial partitioning build partitioning with limited number
     * of histograms (and block types). */
    private static void ClusterBlocksCommand(MemoryManager* m,
                                             ushort* data, nuint length,
                                             nuint num_blocks,
                                             byte* block_ids,
                                             BlockSplit* split)
    {
        uint* histogram_symbols = BROTLI_ALLOC<uint>(m, num_blocks);
        uint* u32 = BROTLI_ALLOC<uint>(m, num_blocks + 4 * HISTOGRAMS_PER_BATCH);
        nuint expected_num_clusters = CLUSTERS_PER_BATCH *
            (num_blocks + HISTOGRAMS_PER_BATCH - 1) / HISTOGRAMS_PER_BATCH;
        nuint all_histograms_size = 0;
        nuint all_histograms_capacity = expected_num_clusters;
        HistogramCommand* all_histograms =
            BROTLI_ALLOC<HistogramCommand>(m, all_histograms_capacity);
        nuint cluster_size_size = 0;
        nuint cluster_size_capacity = expected_num_clusters;
        uint* cluster_size = BROTLI_ALLOC<uint>(m, cluster_size_capacity);
        nuint num_clusters = 0;
        HistogramCommand* histograms = BROTLI_ALLOC<HistogramCommand>(m,
            BROTLI_MIN(num_blocks, (nuint)HISTOGRAMS_PER_BATCH));  /* BROTLI_MIN(size_t, ...) */
        nuint max_num_pairs =
            HISTOGRAMS_PER_BATCH * HISTOGRAMS_PER_BATCH / 2;
        nuint pairs_capacity = max_num_pairs + 1;
        HistogramPair* pairs = BROTLI_ALLOC<HistogramPair>(m, pairs_capacity);
        nuint pos = 0;
        uint* clusters;
        nuint num_final_clusters;
        const uint kInvalidIndex = uint.MaxValue;  /* BROTLI_UINT32_MAX */
        uint* new_index;
        nuint i;
        uint* sizes = u32 + 0 * HISTOGRAMS_PER_BATCH;
        uint* new_clusters = u32 + 1 * HISTOGRAMS_PER_BATCH;
        uint* symbols = u32 + 2 * HISTOGRAMS_PER_BATCH;
        uint* remap = u32 + 3 * HISTOGRAMS_PER_BATCH;
        uint* block_lengths = u32 + 4 * HISTOGRAMS_PER_BATCH;
        /* TODO(eustas): move to arena? */
        HistogramCommand* tmp = BROTLI_ALLOC<HistogramCommand>(m, 2);

        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(histogram_symbols) ||
            BROTLI_IS_NULL(u32) || BROTLI_IS_NULL(all_histograms) ||
            BROTLI_IS_NULL(cluster_size) || BROTLI_IS_NULL(histograms) ||
            BROTLI_IS_NULL(pairs) || BROTLI_IS_NULL(tmp))
        {
            return;
        }

        new Span<uint>(u32, (int)(num_blocks + 4 * HISTOGRAMS_PER_BATCH)).Clear();  /* memset */

        /* Calculate block lengths (convert repeating values -> series length). */
        {
            nuint block_idx = 0;
            for (i = 0; i < length; ++i)
            {
                ++block_lengths[block_idx];
                if (i + 1 == length || block_ids[i] != block_ids[i + 1])
                {
                    ++block_idx;
                }
            }
        }

        /* Pre-cluster blocks (cluster batches). */
        for (i = 0; i < num_blocks; i += HISTOGRAMS_PER_BATCH)
        {
            nuint num_to_combine =
                BROTLI_MIN(num_blocks - i, (nuint)HISTOGRAMS_PER_BATCH);  /* BROTLI_MIN(size_t, ...) */
            nuint num_new_clusters;
            nuint j;
            for (j = 0; j < num_to_combine; ++j)
            {
                nuint k;
                nuint block_length = block_lengths[i + j];
                HistogramClearCommand(&histograms[j]);
                for (k = 0; k < block_length; ++k)
                {
                    HistogramAddCommand(&histograms[j], data[pos++]);
                }
                histograms[j].bit_cost_ = BrotliPopulationCostCommand(&histograms[j]);
                new_clusters[j] = (uint)j;
                symbols[j] = (uint)j;
                sizes[j] = 1;
            }
            num_new_clusters = BrotliHistogramCombineCommand(
                histograms, tmp, sizes, symbols, new_clusters, pairs, num_to_combine,
                num_to_combine, HISTOGRAMS_PER_BATCH, max_num_pairs);
            BROTLI_ENSURE_CAPACITY(m, ref all_histograms,
                ref all_histograms_capacity, all_histograms_size + num_new_clusters);
            BROTLI_ENSURE_CAPACITY(m, ref cluster_size,
                ref cluster_size_capacity, cluster_size_size + num_new_clusters);
            if (BROTLI_IS_OOM(m)) return;
            for (j = 0; j < num_new_clusters; ++j)
            {
                all_histograms[all_histograms_size++] = histograms[new_clusters[j]];
                cluster_size[cluster_size_size++] = sizes[new_clusters[j]];
                remap[new_clusters[j]] = (uint)j;
            }
            for (j = 0; j < num_to_combine; ++j)
            {
                histogram_symbols[i + j] = (uint)num_clusters + remap[symbols[j]];
            }
            num_clusters += num_new_clusters;
        }
        BROTLI_FREE(m, ref histograms);

        /* Final clustering. */
        max_num_pairs = BROTLI_MIN(
            64 * num_clusters, (num_clusters / 2) * num_clusters);  /* BROTLI_MIN(size_t, ...) */
        if (pairs_capacity < max_num_pairs + 1)
        {
            BROTLI_FREE(m, ref pairs);
            pairs = BROTLI_ALLOC<HistogramPair>(m, max_num_pairs + 1);
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(pairs)) return;
        }
        clusters = BROTLI_ALLOC<uint>(m, num_clusters);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(clusters)) return;
        for (i = 0; i < num_clusters; ++i)
        {
            clusters[i] = (uint)i;
        }
        num_final_clusters = BrotliHistogramCombineCommand(
            all_histograms, tmp, cluster_size, histogram_symbols, clusters, pairs,
            num_clusters, num_blocks, BROTLI_MAX_NUMBER_OF_BLOCK_TYPES,
            max_num_pairs);
        BROTLI_FREE(m, ref pairs);
        BROTLI_FREE(m, ref cluster_size);

        /* Assign blocks to final histograms. */
        new_index = BROTLI_ALLOC<uint>(m, num_clusters);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(new_index)) return;
        for (i = 0; i < num_clusters; ++i) new_index[i] = kInvalidIndex;
        pos = 0;
        {
            uint next_index = 0;
            for (i = 0; i < num_blocks; ++i)
            {
                nuint j;
                uint best_out;
                double best_bits;
                HistogramClearCommand(tmp);
                for (j = 0; j < block_lengths[i]; ++j)
                {
                    HistogramAddCommand(tmp, data[pos++]);
                }
                /* Among equally good histograms prefer last used. */
                /* TODO(eustas): should we give a block-switch discount here? */
                best_out = (i == 0) ? histogram_symbols[0] : histogram_symbols[i - 1];
                best_bits = BrotliHistogramBitCostDistanceCommand(
                    tmp, &all_histograms[best_out], tmp + 1);
                for (j = 0; j < num_final_clusters; ++j)
                {
                    double cur_bits = BrotliHistogramBitCostDistanceCommand(
                        tmp, &all_histograms[clusters[j]], tmp + 1);
                    if (cur_bits < best_bits)
                    {
                        best_bits = cur_bits;
                        best_out = clusters[j];
                    }
                }
                histogram_symbols[i] = best_out;
                if (new_index[best_out] == kInvalidIndex)
                {
                    new_index[best_out] = next_index++;
                }
            }
        }
        BROTLI_FREE(m, ref tmp);
        BROTLI_FREE(m, ref clusters);
        BROTLI_FREE(m, ref all_histograms);
        BROTLI_ENSURE_CAPACITY(
            m, ref split->types, ref split->types_alloc_size, num_blocks);
        BROTLI_ENSURE_CAPACITY(
            m, ref split->lengths, ref split->lengths_alloc_size, num_blocks);
        if (BROTLI_IS_OOM(m)) return;

        /* Rewrite final assignment to block-split. There might be less blocks
         * than |num_blocks| due to clustering. */
        {
            uint cur_length = 0;
            nuint block_idx = 0;
            byte max_type = 0;
            for (i = 0; i < num_blocks; ++i)
            {
                cur_length += block_lengths[i];
                if (i + 1 == num_blocks ||
                    histogram_symbols[i] != histogram_symbols[i + 1])
                {
                    byte id = (byte)new_index[histogram_symbols[i]];
                    split->types[block_idx] = id;
                    split->lengths[block_idx] = cur_length;
                    max_type = BROTLI_MAX(max_type, id);  /* BROTLI_MAX(uint8_t, ...) */
                    cur_length = 0;
                    ++block_idx;
                }
            }
            split->num_blocks = block_idx;
            split->num_types = (nuint)max_type + 1;
        }
        BROTLI_FREE(m, ref new_index);
        BROTLI_FREE(m, ref u32);
        BROTLI_FREE(m, ref histogram_symbols);
    }

    /* Create BlockSplit (partitioning) given the limits, estimates and "effort"
     * parameters; see the Literal instantiation for the NB note. */
    private static void SplitByteVectorCommand(MemoryManager* m,
                                               ushort* data, nuint length,
                                               nuint symbols_per_histogram,
                                               nuint max_histograms,
                                               nuint sampling_stride_length,
                                               double block_switch_cost,
                                               BrotliEncoderParams* @params,
                                               BlockSplit* split)
    {
        nuint data_size = HistogramDataSizeCommand();
        HistogramCommand* histograms;
        HistogramCommand* tmp;
        /* Calculate number of histograms; initial estimate is one histogram per
         * specified amount of symbols; however, this value is capped. */
        nuint num_histograms = length / symbols_per_histogram + 1;
        if (num_histograms > max_histograms)
        {
            num_histograms = max_histograms;
        }

        /* Corner case: no input. */
        if (length == 0)
        {
            split->num_types = 1;
            return;
        }

        if (length < kMinLengthForBlockSplitting)
        {
            BROTLI_ENSURE_CAPACITY(m,
                ref split->types, ref split->types_alloc_size, split->num_blocks + 1);
            BROTLI_ENSURE_CAPACITY(m,
                ref split->lengths, ref split->lengths_alloc_size, split->num_blocks + 1);
            if (BROTLI_IS_OOM(m)) return;
            split->num_types = 1;
            split->types[split->num_blocks] = 0;
            split->lengths[split->num_blocks] = (uint)length;
            split->num_blocks++;
            return;
        }
        histograms = BROTLI_ALLOC<HistogramCommand>(m, num_histograms + 1);
        tmp = histograms + num_histograms;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(histograms)) return;
        /* Find good entropy codes. */
        InitialEntropyCodesCommand(data, length,
                                   sampling_stride_length,
                                   num_histograms, histograms);
        RefineEntropyCodesCommand(data, length,
                                  sampling_stride_length,
                                  num_histograms, histograms, tmp);
        {
            /* Find a good path through literals with the good entropy codes. */
            byte* block_ids = BROTLI_ALLOC<byte>(m, length);
            nuint num_blocks = 0;
            nuint bitmaplen = (num_histograms + 7) >> 3;
            double* insert_cost = BROTLI_ALLOC<double>(m, data_size * num_histograms);
            double* cost = BROTLI_ALLOC<double>(m, num_histograms);
            byte* switch_signal = BROTLI_ALLOC<byte>(m, length * bitmaplen);
            ushort* new_id = BROTLI_ALLOC<ushort>(m, num_histograms);
            nuint iters = @params->quality < HQ_ZOPFLIFICATION_QUALITY ? 3 : (nuint)10;
            nuint i;
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(block_ids) ||
                BROTLI_IS_NULL(insert_cost) || BROTLI_IS_NULL(cost) ||
                BROTLI_IS_NULL(switch_signal) || BROTLI_IS_NULL(new_id))
            {
                return;
            }
            for (i = 0; i < iters; ++i)
            {
                num_blocks = FindBlocksCommand(data, length,
                                               block_switch_cost,
                                               num_histograms, histograms,
                                               insert_cost, cost, switch_signal,
                                               block_ids);
                num_histograms = RemapBlockIdsCommand(block_ids, length,
                                                      new_id, num_histograms);
                BuildBlockHistogramsCommand(data, length, block_ids,
                                            num_histograms, histograms);
            }
            BROTLI_FREE(m, ref insert_cost);
            BROTLI_FREE(m, ref cost);
            BROTLI_FREE(m, ref switch_signal);
            BROTLI_FREE(m, ref new_id);
            BROTLI_FREE(m, ref histograms);
            ClusterBlocksCommand(m, data, length, num_blocks, block_ids, split);
            if (BROTLI_IS_OOM(m)) return;
            BROTLI_FREE(m, ref block_ids);
        }
    }

    /* ==================== FN = Distance (DataType uint16_t) ==================== */

    private static void InitialEntropyCodesDistance(ushort* data, nuint length,
                                                    nuint stride,
                                                    nuint num_histograms,
                                                    HistogramDistance* histograms)
    {
        uint seed = 7;
        nuint block_length = length / num_histograms;
        nuint i;
        ClearHistogramsDistance(histograms, num_histograms);
        for (i = 0; i < num_histograms; ++i)
        {
            nuint pos = length * i / num_histograms;
            if (i != 0)
            {
                pos += MyRand(&seed) % block_length;
            }
            if (pos + stride >= length)
            {
                pos = length - stride - 1;
            }
            HistogramAddVectorDistance(&histograms[i], data + pos, stride);
        }
    }

    private static void RandomSampleDistance(uint* seed,
                                             ushort* data,
                                             nuint length,
                                             nuint stride,
                                             HistogramDistance* sample)
    {
        nuint pos = 0;
        if (stride >= length)
        {
            stride = length;
        }
        else
        {
            pos = MyRand(seed) % (length - stride + 1);
        }
        HistogramAddVectorDistance(sample, data + pos, stride);
    }

    private static void RefineEntropyCodesDistance(ushort* data, nuint length,
                                                   nuint stride,
                                                   nuint num_histograms,
                                                   HistogramDistance* histograms,
                                                   HistogramDistance* tmp)
    {
        nuint iters =
            kIterMulForRefining * length / stride + kMinItersForRefining;
        uint seed = 7;
        nuint iter;
        iters = ((iters + num_histograms - 1) / num_histograms) * num_histograms;
        for (iter = 0; iter < iters; ++iter)
        {
            HistogramClearDistance(tmp);
            RandomSampleDistance(&seed, data, length, stride, tmp);
            HistogramAddHistogramDistance(&histograms[iter % num_histograms], tmp);
        }
    }

    /* Assigns a block id from the range [0, num_histograms) to each data element
       in data[0..length) and fills in block_id[0..length) with the assigned values.
       Returns the number of blocks, i.e. one plus the number of block switches. */
    private static nuint FindBlocksDistance(ushort* data, nuint length,
                                            double block_switch_bitcost,
                                            nuint num_histograms,
                                            HistogramDistance* histograms,
                                            double* insert_cost,
                                            double* cost,
                                            byte* switch_signal,
                                            byte* block_id)
    {
        nuint alphabet_size = HistogramDataSizeDistance();
        nuint bitmap_len = (num_histograms + 7) >> 3;
        nuint num_blocks = 1;
        nuint byte_ix;
        nuint i;
        nuint j;

        /* Trivial case: single historgram -> single block type. */
        if (num_histograms <= 1)
        {
            for (i = 0; i < length; ++i)
            {
                block_id[i] = 0;
            }
            return 1;
        }

        /* Fill bitcost for each symbol of all histograms.
         * Non-existing symbol cost: 2 + log2(total_count).
         * Regular symbol cost: -log2(symbol_count / total_count). */
        new Span<double>(insert_cost, (int)(alphabet_size * num_histograms)).Clear();  /* memset */
        for (i = 0; i < num_histograms; ++i)
        {
            insert_cost[i] = FastLog2((uint)histograms[i].total_count_);
        }
        for (i = alphabet_size; i != 0;)
        {
            /* Reverse order to use the 0-th row as a temporary storage. */
            --i;
            for (j = 0; j < num_histograms; ++j)
            {
                insert_cost[i * num_histograms + j] =
                    insert_cost[j] - BitCostFn(histograms[j].data_[i]);
            }
        }

        /* After each iteration of this loop, cost[k] will contain the difference
           between the minimum cost of arriving at the current byte position using
           entropy code k, and the minimum cost of arriving at the current byte
           position. This difference is capped at the block switch cost, and if it
           reaches block switch cost, it means that when we trace back from the last
           position, we need to switch here. */
        new Span<double>(cost, (int)num_histograms).Clear();  /* memset */
        new Span<byte>(switch_signal, (int)(length * bitmap_len)).Clear();  /* memset */
        for (byte_ix = 0; byte_ix < length; ++byte_ix)
        {
            nuint ix = byte_ix * bitmap_len;
            nuint symbol = data[byte_ix];
            nuint insert_cost_ix = symbol * num_histograms;
            double min_cost = 1e99;
            double block_switch_cost = block_switch_bitcost;
            nuint k;
            for (k = 0; k < num_histograms; ++k)
            {
                /* We are coding the symbol with entropy code k. */
                cost[k] += insert_cost[insert_cost_ix + k];
                if (cost[k] < min_cost)
                {
                    min_cost = cost[k];
                    block_id[byte_ix] = (byte)k;
                }
            }
            /* More blocks for the beginning. */
            if (byte_ix < 2000)
            {
                block_switch_cost *= 0.77 + 0.07 * (double)byte_ix / 2000;
            }
            for (k = 0; k < num_histograms; ++k)
            {
                cost[k] -= min_cost;
                if (cost[k] >= block_switch_cost)
                {
                    byte mask = (byte)(1u << (int)(k & 7));
                    cost[k] = block_switch_cost;
                    switch_signal[ix + (k >> 3)] |= mask;
                }
            }
        }

        byte_ix = length - 1;
        {  /* Trace back from the last position and switch at the marked places. */
            nuint ix = byte_ix * bitmap_len;
            byte cur_id = block_id[byte_ix];
            while (byte_ix > 0)
            {
                byte mask = (byte)(1u << (cur_id & 7));
                --byte_ix;
                ix -= bitmap_len;
                if ((switch_signal[ix + (nuint)(cur_id >> 3)] & mask) != 0)
                {
                    if (cur_id != block_id[byte_ix])
                    {
                        cur_id = block_id[byte_ix];
                        ++num_blocks;
                    }
                }
                block_id[byte_ix] = cur_id;
            }
        }
        return num_blocks;
    }

    private static nuint RemapBlockIdsDistance(byte* block_ids, nuint length,
                                               ushort* new_id, nuint num_histograms)
    {
        const ushort kInvalidId = 256;
        ushort next_id = 0;
        nuint i;
        for (i = 0; i < num_histograms; ++i)
        {
            new_id[i] = kInvalidId;
        }
        for (i = 0; i < length; ++i)
        {
            if (new_id[block_ids[i]] == kInvalidId)
            {
                new_id[block_ids[i]] = next_id++;
            }
        }
        for (i = 0; i < length; ++i)
        {
            block_ids[i] = (byte)new_id[block_ids[i]];
        }
        return next_id;
    }

    private static void BuildBlockHistogramsDistance(ushort* data, nuint length,
                                                     byte* block_ids,
                                                     nuint num_histograms,
                                                     HistogramDistance* histograms)
    {
        nuint i;
        ClearHistogramsDistance(histograms, num_histograms);
        for (i = 0; i < length; ++i)
        {
            HistogramAddDistance(&histograms[block_ids[i]], data[i]);
        }
    }

    /* Given the initial partitioning build partitioning with limited number
     * of histograms (and block types). */
    private static void ClusterBlocksDistance(MemoryManager* m,
                                              ushort* data, nuint length,
                                              nuint num_blocks,
                                              byte* block_ids,
                                              BlockSplit* split)
    {
        uint* histogram_symbols = BROTLI_ALLOC<uint>(m, num_blocks);
        uint* u32 = BROTLI_ALLOC<uint>(m, num_blocks + 4 * HISTOGRAMS_PER_BATCH);
        nuint expected_num_clusters = CLUSTERS_PER_BATCH *
            (num_blocks + HISTOGRAMS_PER_BATCH - 1) / HISTOGRAMS_PER_BATCH;
        nuint all_histograms_size = 0;
        nuint all_histograms_capacity = expected_num_clusters;
        HistogramDistance* all_histograms =
            BROTLI_ALLOC<HistogramDistance>(m, all_histograms_capacity);
        nuint cluster_size_size = 0;
        nuint cluster_size_capacity = expected_num_clusters;
        uint* cluster_size = BROTLI_ALLOC<uint>(m, cluster_size_capacity);
        nuint num_clusters = 0;
        HistogramDistance* histograms = BROTLI_ALLOC<HistogramDistance>(m,
            BROTLI_MIN(num_blocks, (nuint)HISTOGRAMS_PER_BATCH));  /* BROTLI_MIN(size_t, ...) */
        nuint max_num_pairs =
            HISTOGRAMS_PER_BATCH * HISTOGRAMS_PER_BATCH / 2;
        nuint pairs_capacity = max_num_pairs + 1;
        HistogramPair* pairs = BROTLI_ALLOC<HistogramPair>(m, pairs_capacity);
        nuint pos = 0;
        uint* clusters;
        nuint num_final_clusters;
        const uint kInvalidIndex = uint.MaxValue;  /* BROTLI_UINT32_MAX */
        uint* new_index;
        nuint i;
        uint* sizes = u32 + 0 * HISTOGRAMS_PER_BATCH;
        uint* new_clusters = u32 + 1 * HISTOGRAMS_PER_BATCH;
        uint* symbols = u32 + 2 * HISTOGRAMS_PER_BATCH;
        uint* remap = u32 + 3 * HISTOGRAMS_PER_BATCH;
        uint* block_lengths = u32 + 4 * HISTOGRAMS_PER_BATCH;
        /* TODO(eustas): move to arena? */
        HistogramDistance* tmp = BROTLI_ALLOC<HistogramDistance>(m, 2);

        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(histogram_symbols) ||
            BROTLI_IS_NULL(u32) || BROTLI_IS_NULL(all_histograms) ||
            BROTLI_IS_NULL(cluster_size) || BROTLI_IS_NULL(histograms) ||
            BROTLI_IS_NULL(pairs) || BROTLI_IS_NULL(tmp))
        {
            return;
        }

        new Span<uint>(u32, (int)(num_blocks + 4 * HISTOGRAMS_PER_BATCH)).Clear();  /* memset */

        /* Calculate block lengths (convert repeating values -> series length). */
        {
            nuint block_idx = 0;
            for (i = 0; i < length; ++i)
            {
                ++block_lengths[block_idx];
                if (i + 1 == length || block_ids[i] != block_ids[i + 1])
                {
                    ++block_idx;
                }
            }
        }

        /* Pre-cluster blocks (cluster batches). */
        for (i = 0; i < num_blocks; i += HISTOGRAMS_PER_BATCH)
        {
            nuint num_to_combine =
                BROTLI_MIN(num_blocks - i, (nuint)HISTOGRAMS_PER_BATCH);  /* BROTLI_MIN(size_t, ...) */
            nuint num_new_clusters;
            nuint j;
            for (j = 0; j < num_to_combine; ++j)
            {
                nuint k;
                nuint block_length = block_lengths[i + j];
                HistogramClearDistance(&histograms[j]);
                for (k = 0; k < block_length; ++k)
                {
                    HistogramAddDistance(&histograms[j], data[pos++]);
                }
                histograms[j].bit_cost_ = BrotliPopulationCostDistance(&histograms[j]);
                new_clusters[j] = (uint)j;
                symbols[j] = (uint)j;
                sizes[j] = 1;
            }
            num_new_clusters = BrotliHistogramCombineDistance(
                histograms, tmp, sizes, symbols, new_clusters, pairs, num_to_combine,
                num_to_combine, HISTOGRAMS_PER_BATCH, max_num_pairs);
            BROTLI_ENSURE_CAPACITY(m, ref all_histograms,
                ref all_histograms_capacity, all_histograms_size + num_new_clusters);
            BROTLI_ENSURE_CAPACITY(m, ref cluster_size,
                ref cluster_size_capacity, cluster_size_size + num_new_clusters);
            if (BROTLI_IS_OOM(m)) return;
            for (j = 0; j < num_new_clusters; ++j)
            {
                all_histograms[all_histograms_size++] = histograms[new_clusters[j]];
                cluster_size[cluster_size_size++] = sizes[new_clusters[j]];
                remap[new_clusters[j]] = (uint)j;
            }
            for (j = 0; j < num_to_combine; ++j)
            {
                histogram_symbols[i + j] = (uint)num_clusters + remap[symbols[j]];
            }
            num_clusters += num_new_clusters;
        }
        BROTLI_FREE(m, ref histograms);

        /* Final clustering. */
        max_num_pairs = BROTLI_MIN(
            64 * num_clusters, (num_clusters / 2) * num_clusters);  /* BROTLI_MIN(size_t, ...) */
        if (pairs_capacity < max_num_pairs + 1)
        {
            BROTLI_FREE(m, ref pairs);
            pairs = BROTLI_ALLOC<HistogramPair>(m, max_num_pairs + 1);
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(pairs)) return;
        }
        clusters = BROTLI_ALLOC<uint>(m, num_clusters);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(clusters)) return;
        for (i = 0; i < num_clusters; ++i)
        {
            clusters[i] = (uint)i;
        }
        num_final_clusters = BrotliHistogramCombineDistance(
            all_histograms, tmp, cluster_size, histogram_symbols, clusters, pairs,
            num_clusters, num_blocks, BROTLI_MAX_NUMBER_OF_BLOCK_TYPES,
            max_num_pairs);
        BROTLI_FREE(m, ref pairs);
        BROTLI_FREE(m, ref cluster_size);

        /* Assign blocks to final histograms. */
        new_index = BROTLI_ALLOC<uint>(m, num_clusters);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(new_index)) return;
        for (i = 0; i < num_clusters; ++i) new_index[i] = kInvalidIndex;
        pos = 0;
        {
            uint next_index = 0;
            for (i = 0; i < num_blocks; ++i)
            {
                nuint j;
                uint best_out;
                double best_bits;
                HistogramClearDistance(tmp);
                for (j = 0; j < block_lengths[i]; ++j)
                {
                    HistogramAddDistance(tmp, data[pos++]);
                }
                /* Among equally good histograms prefer last used. */
                /* TODO(eustas): should we give a block-switch discount here? */
                best_out = (i == 0) ? histogram_symbols[0] : histogram_symbols[i - 1];
                best_bits = BrotliHistogramBitCostDistanceDistance(
                    tmp, &all_histograms[best_out], tmp + 1);
                for (j = 0; j < num_final_clusters; ++j)
                {
                    double cur_bits = BrotliHistogramBitCostDistanceDistance(
                        tmp, &all_histograms[clusters[j]], tmp + 1);
                    if (cur_bits < best_bits)
                    {
                        best_bits = cur_bits;
                        best_out = clusters[j];
                    }
                }
                histogram_symbols[i] = best_out;
                if (new_index[best_out] == kInvalidIndex)
                {
                    new_index[best_out] = next_index++;
                }
            }
        }
        BROTLI_FREE(m, ref tmp);
        BROTLI_FREE(m, ref clusters);
        BROTLI_FREE(m, ref all_histograms);
        BROTLI_ENSURE_CAPACITY(
            m, ref split->types, ref split->types_alloc_size, num_blocks);
        BROTLI_ENSURE_CAPACITY(
            m, ref split->lengths, ref split->lengths_alloc_size, num_blocks);
        if (BROTLI_IS_OOM(m)) return;

        /* Rewrite final assignment to block-split. There might be less blocks
         * than |num_blocks| due to clustering. */
        {
            uint cur_length = 0;
            nuint block_idx = 0;
            byte max_type = 0;
            for (i = 0; i < num_blocks; ++i)
            {
                cur_length += block_lengths[i];
                if (i + 1 == num_blocks ||
                    histogram_symbols[i] != histogram_symbols[i + 1])
                {
                    byte id = (byte)new_index[histogram_symbols[i]];
                    split->types[block_idx] = id;
                    split->lengths[block_idx] = cur_length;
                    max_type = BROTLI_MAX(max_type, id);  /* BROTLI_MAX(uint8_t, ...) */
                    cur_length = 0;
                    ++block_idx;
                }
            }
            split->num_blocks = block_idx;
            split->num_types = (nuint)max_type + 1;
        }
        BROTLI_FREE(m, ref new_index);
        BROTLI_FREE(m, ref u32);
        BROTLI_FREE(m, ref histogram_symbols);
    }

    /* Create BlockSplit (partitioning) given the limits, estimates and "effort"
     * parameters; see the Literal instantiation for the NB note. */
    private static void SplitByteVectorDistance(MemoryManager* m,
                                                ushort* data, nuint length,
                                                nuint symbols_per_histogram,
                                                nuint max_histograms,
                                                nuint sampling_stride_length,
                                                double block_switch_cost,
                                                BrotliEncoderParams* @params,
                                                BlockSplit* split)
    {
        nuint data_size = HistogramDataSizeDistance();
        HistogramDistance* histograms;
        HistogramDistance* tmp;
        /* Calculate number of histograms; initial estimate is one histogram per
         * specified amount of symbols; however, this value is capped. */
        nuint num_histograms = length / symbols_per_histogram + 1;
        if (num_histograms > max_histograms)
        {
            num_histograms = max_histograms;
        }

        /* Corner case: no input. */
        if (length == 0)
        {
            split->num_types = 1;
            return;
        }

        if (length < kMinLengthForBlockSplitting)
        {
            BROTLI_ENSURE_CAPACITY(m,
                ref split->types, ref split->types_alloc_size, split->num_blocks + 1);
            BROTLI_ENSURE_CAPACITY(m,
                ref split->lengths, ref split->lengths_alloc_size, split->num_blocks + 1);
            if (BROTLI_IS_OOM(m)) return;
            split->num_types = 1;
            split->types[split->num_blocks] = 0;
            split->lengths[split->num_blocks] = (uint)length;
            split->num_blocks++;
            return;
        }
        histograms = BROTLI_ALLOC<HistogramDistance>(m, num_histograms + 1);
        tmp = histograms + num_histograms;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(histograms)) return;
        /* Find good entropy codes. */
        InitialEntropyCodesDistance(data, length,
                                    sampling_stride_length,
                                    num_histograms, histograms);
        RefineEntropyCodesDistance(data, length,
                                   sampling_stride_length,
                                   num_histograms, histograms, tmp);
        {
            /* Find a good path through literals with the good entropy codes. */
            byte* block_ids = BROTLI_ALLOC<byte>(m, length);
            nuint num_blocks = 0;
            nuint bitmaplen = (num_histograms + 7) >> 3;
            double* insert_cost = BROTLI_ALLOC<double>(m, data_size * num_histograms);
            double* cost = BROTLI_ALLOC<double>(m, num_histograms);
            byte* switch_signal = BROTLI_ALLOC<byte>(m, length * bitmaplen);
            ushort* new_id = BROTLI_ALLOC<ushort>(m, num_histograms);
            nuint iters = @params->quality < HQ_ZOPFLIFICATION_QUALITY ? 3 : (nuint)10;
            nuint i;
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(block_ids) ||
                BROTLI_IS_NULL(insert_cost) || BROTLI_IS_NULL(cost) ||
                BROTLI_IS_NULL(switch_signal) || BROTLI_IS_NULL(new_id))
            {
                return;
            }
            for (i = 0; i < iters; ++i)
            {
                num_blocks = FindBlocksDistance(data, length,
                                                block_switch_cost,
                                                num_histograms, histograms,
                                                insert_cost, cost, switch_signal,
                                                block_ids);
                num_histograms = RemapBlockIdsDistance(block_ids, length,
                                                       new_id, num_histograms);
                BuildBlockHistogramsDistance(data, length, block_ids,
                                             num_histograms, histograms);
            }
            BROTLI_FREE(m, ref insert_cost);
            BROTLI_FREE(m, ref cost);
            BROTLI_FREE(m, ref switch_signal);
            BROTLI_FREE(m, ref new_id);
            BROTLI_FREE(m, ref histograms);
            ClusterBlocksDistance(m, data, length, num_blocks, block_ids, split);
            if (BROTLI_IS_OOM(m)) return;
            BROTLI_FREE(m, ref block_ids);
        }
    }

    /* ==================== block_splitter.c public surface ==================== */

    internal static void BrotliInitBlockSplit(BlockSplit* self)
    {
        self->num_types = 0;
        self->num_blocks = 0;
        self->types = null;
        self->lengths = null;
        self->types_alloc_size = 0;
        self->lengths_alloc_size = 0;
    }

    internal static void BrotliDestroyBlockSplit(MemoryManager* m, BlockSplit* self)
    {
        BROTLI_FREE(m, ref self->types);
        BROTLI_FREE(m, ref self->lengths);
    }

    /* Extracts literals, command distance and prefix codes, then applies
     * SplitByteVector to create partitioning. */
    internal static void BrotliSplitBlock(MemoryManager* m,
                                          Command* cmds,
                                          nuint num_commands,
                                          byte* data,
                                          nuint pos,
                                          nuint mask,
                                          BrotliEncoderParams* @params,
                                          BlockSplit* literal_split,
                                          BlockSplit* insert_and_copy_split,
                                          BlockSplit* dist_split)
    {
        {
            nuint literals_count = CountLiterals(cmds, num_commands);
            byte* literals = BROTLI_ALLOC<byte>(m, literals_count);
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(literals)) return;
            /* Create a continuous array of literals. */
            CopyLiteralsToByteArray(cmds, num_commands, data, pos, mask, literals);
            /* Create the block split on the array of literals.
             * Literal histograms can have alphabet size up to 256.
             * Though, to accomodate context modeling, less than half of maximum size
             * is allowed. */
            SplitByteVectorLiteral(
                m, literals, literals_count,
                kSymbolsPerLiteralHistogram, kMaxLiteralHistograms,
                kLiteralStrideLength, kLiteralBlockSwitchCost, @params,
                literal_split);
            if (BROTLI_IS_OOM(m)) return;
            BROTLI_FREE(m, ref literals);
            /* NB: this might be a good place for injecting extra splitting without
             *     increasing encoder complexity; however, output parition would be less
             *     optimal than one produced with forced splitting inside
             *     SplitByteVector (FindBlocks / ClusterBlocks). */
        }

        {
            /* Compute prefix codes for commands. */
            ushort* insert_and_copy_codes = BROTLI_ALLOC<ushort>(m, num_commands);
            nuint i;
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(insert_and_copy_codes)) return;
            for (i = 0; i < num_commands; ++i)
            {
                insert_and_copy_codes[i] = cmds[i].cmd_prefix_;
            }
            /* Create the block split on the array of command prefixes. */
            SplitByteVectorCommand(
                m, insert_and_copy_codes, num_commands,
                kSymbolsPerCommandHistogram, kMaxCommandHistograms,
                kCommandStrideLength, kCommandBlockSwitchCost, @params,
                insert_and_copy_split);
            if (BROTLI_IS_OOM(m)) return;
            /* TODO(eustas): reuse for distances? */
            BROTLI_FREE(m, ref insert_and_copy_codes);
        }

        {
            /* Create a continuous array of distance prefixes. */
            ushort* distance_prefixes = BROTLI_ALLOC<ushort>(m, num_commands);
            nuint j = 0;
            nuint i;
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(distance_prefixes)) return;
            for (i = 0; i < num_commands; ++i)
            {
                Command* cmd = &cmds[i];
                if (CommandCopyLen(cmd) != 0 && cmd->cmd_prefix_ >= 128)
                {
                    distance_prefixes[j++] = (ushort)(cmd->dist_prefix_ & 0x3FF);
                }
            }
            /* Create the block split on the array of distance prefixes. */
            SplitByteVectorDistance(
                m, distance_prefixes, j,
                kSymbolsPerDistanceHistogram, kMaxCommandHistograms,
                kDistanceStrideLength, kDistanceBlockSwitchCost, @params,
                dist_split);
            if (BROTLI_IS_OOM(m)) return;
            BROTLI_FREE(m, ref distance_prefixes);
        }
    }
}
