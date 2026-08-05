// Port of c/enc/metablock.{h,c} + metablock_inc.h (brotli v1.1.0).
//
// Complete: MetaBlockSplit (metablock.h), BrotliInitDistanceParams, the
// distance-parameter search of BrotliBuildMetaBlock (RecomputeDistancePrefixes
// + ComputeDistanceCost), BrotliBuildMetaBlock (HQ block splitting + context
// clustering), the greedy splitter family — the metablock_inc.h template
// expanded three times (BlockSplitterLiteral / BlockSplitterCommand /
// BlockSplitterDistance, the Histogram.cs triple pattern) plus
// ContextBlockSplitter and MapStaticContexts — BrotliBuildMetaBlockGreedy and
// BrotliOptimizeHistograms.

using System.Runtime.InteropServices;

using DnBrotli.Common;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Context;
using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.BitCost;
using static DnBrotli.Enc.BlockSplitter;
using static DnBrotli.Enc.Command;
using static DnBrotli.Enc.EntropyEncode;
using static DnBrotli.Enc.Histogram;
using static DnBrotli.Enc.MemoryManager;

namespace DnBrotli.Enc;

/// <summary><c>struct MetaBlockSplit</c> (metablock.h).</summary>
internal unsafe struct MetaBlockSplit
{
    public BlockSplit literal_split;
    public BlockSplit command_split;
    public BlockSplit distance_split;
    public uint* literal_context_map;
    public nuint literal_context_map_size;
    public uint* distance_context_map;
    public nuint distance_context_map_size;
    public HistogramLiteral* literal_histograms;
    public nuint literal_histograms_size;
    public HistogramCommand* command_histograms;
    public nuint command_histograms_size;
    public HistogramDistance* distance_histograms;
    public nuint distance_histograms_size;
}

/// <summary><c>BlockSplitterLiteral</c> (metablock_inc.h, FN(X) = XLiteral):
/// greedy block splitter for one block category. The embedded
/// <c>HistogramType combined_histo[2]</c> becomes two adjacent sequential
/// fields addressed via <c>&amp;self-&gt;combined_histo0</c> (PORTING.md:
/// embedded arrays of small structs become nested structs).</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct BlockSplitterLiteral
{
    /* Alphabet size of particular block category. */
    public nuint alphabet_size_;
    /* We collect at least this many symbols for each block. */
    public nuint min_block_size_;
    /* We merge histograms A and B if
         entropy(A+B) < entropy(A) + entropy(B) + split_threshold_,
       where A is the current histogram and B is the histogram of the last or the
       second last block type. */
    public double split_threshold_;

    public nuint num_blocks_;
    public BlockSplit* split_;  /* not owned */
    public HistogramLiteral* histograms_;  /* not owned */
    public nuint* histograms_size_;  /* not owned */

    /* Temporary storage for BlockSplitterFinishBlock. */
    public HistogramLiteral combined_histo0;  /* HistogramType combined_histo[2] */
    public HistogramLiteral combined_histo1;

    /* The number of symbols that we want to collect before deciding on whether
       or not to merge the block with a previous one or emit a new block. */
    public nuint target_block_size_;
    /* The number of symbols in the current histogram. */
    public nuint block_size_;
    /* Offset of the current histogram. */
    public nuint curr_histogram_ix_;
    /* Offset of the histograms of the previous two block types. */
    public fixed ulong last_histogram_ix_[2];  /* size_t[2] */
    /* Entropy of the previous two block types. */
    public fixed double last_entropy_[2];
    /* The number of times we merged the current block with the last one. */
    public nuint merge_last_count_;
}

/// <summary><c>BlockSplitterCommand</c> (metablock_inc.h, FN(X) = XCommand).</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct BlockSplitterCommand
{
    public nuint alphabet_size_;
    public nuint min_block_size_;
    public double split_threshold_;

    public nuint num_blocks_;
    public BlockSplit* split_;  /* not owned */
    public HistogramCommand* histograms_;  /* not owned */
    public nuint* histograms_size_;  /* not owned */

    public HistogramCommand combined_histo0;  /* HistogramType combined_histo[2] */
    public HistogramCommand combined_histo1;

    public nuint target_block_size_;
    public nuint block_size_;
    public nuint curr_histogram_ix_;
    public fixed ulong last_histogram_ix_[2];  /* size_t[2] */
    public fixed double last_entropy_[2];
    public nuint merge_last_count_;
}

/// <summary><c>BlockSplitterDistance</c> (metablock_inc.h, FN(X) = XDistance).</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct BlockSplitterDistance
{
    public nuint alphabet_size_;
    public nuint min_block_size_;
    public double split_threshold_;

    public nuint num_blocks_;
    public BlockSplit* split_;  /* not owned */
    public HistogramDistance* histograms_;  /* not owned */
    public nuint* histograms_size_;  /* not owned */

    public HistogramDistance combined_histo0;  /* HistogramType combined_histo[2] */
    public HistogramDistance combined_histo1;

    public nuint target_block_size_;
    public nuint block_size_;
    public nuint curr_histogram_ix_;
    public fixed ulong last_histogram_ix_[2];  /* size_t[2] */
    public fixed double last_entropy_[2];
    public nuint merge_last_count_;
}

/// <summary><c>ContextBlockSplitter</c> (metablock.c): greedy block splitter for
/// one block category (literal, command or distance), gathering histograms for
/// all context buckets.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ContextBlockSplitter
{
    /* Alphabet size of particular block category. */
    public nuint alphabet_size_;
    public nuint num_contexts_;
    public nuint max_block_types_;
    /* We collect at least this many symbols for each block. */
    public nuint min_block_size_;
    /* We merge histograms A and B if
         entropy(A+B) < entropy(A) + entropy(B) + split_threshold_,
       where A is the current histogram and B is the histogram of the last or the
       second last block type. */
    public double split_threshold_;

    public nuint num_blocks_;
    public BlockSplit* split_;  /* not owned */
    public HistogramLiteral* histograms_;  /* not owned */
    public nuint* histograms_size_;  /* not owned */

    /* The number of symbols that we want to collect before deciding on whether
       or not to merge the block with a previous one or emit a new block. */
    public nuint target_block_size_;
    /* The number of symbols in the current histogram. */
    public nuint block_size_;
    /* Offset of the current histogram. */
    public nuint curr_histogram_ix_;
    /* Offset of the histograms of the previous two block types. */
    public fixed ulong last_histogram_ix_[2];  /* size_t[2] */
    /* Entropy of the previous two block types. */
    public fixed double last_entropy_[2 * Metablock.BROTLI_MAX_STATIC_CONTEXTS];
    /* The number of times we merged the current block with the last one. */
    public nuint merge_last_count_;
}

/// <summary><c>GreedyMetablockArena</c> (metablock.c). The C
/// <c>union { BlockSplitterLiteral plain; ContextBlockSplitter ctx; }</c>
/// becomes an explicit-layout overlay at offset 0.</summary>
[StructLayout(LayoutKind.Explicit)]
internal unsafe struct GreedyMetablockArenaLitBlocks
{
    [FieldOffset(0)] public BlockSplitterLiteral plain;
    [FieldOffset(0)] public ContextBlockSplitter ctx;
}

/// <summary><c>struct GreedyMetablockArena</c>.</summary>
internal unsafe struct GreedyMetablockArena
{
    public GreedyMetablockArenaLitBlocks lit_blocks;
    public BlockSplitterCommand cmd_blocks;
    public BlockSplitterDistance dist_blocks;
}

internal static unsafe class Metablock
{
    internal const int BROTLI_MAX_STATIC_CONTEXTS = 13;

    /// <summary><c>InitMetaBlockSplit</c> (metablock.h).</summary>
    internal static void InitMetaBlockSplit(MetaBlockSplit* mb)
    {
        BrotliInitBlockSplit(&mb->literal_split);
        BrotliInitBlockSplit(&mb->command_split);
        BrotliInitBlockSplit(&mb->distance_split);
        mb->literal_context_map = null;
        mb->literal_context_map_size = 0;
        mb->distance_context_map = null;
        mb->distance_context_map_size = 0;
        mb->literal_histograms = null;
        mb->literal_histograms_size = 0;
        mb->command_histograms = null;
        mb->command_histograms_size = 0;
        mb->distance_histograms = null;
        mb->distance_histograms_size = 0;
    }

    /// <summary><c>DestroyMetaBlockSplit</c> (metablock.h).</summary>
    internal static void DestroyMetaBlockSplit(MemoryManager* m, MetaBlockSplit* mb)
    {
        BrotliDestroyBlockSplit(m, &mb->literal_split);
        BrotliDestroyBlockSplit(m, &mb->command_split);
        BrotliDestroyBlockSplit(m, &mb->distance_split);
        BROTLI_FREE(m, ref mb->literal_context_map);
        BROTLI_FREE(m, ref mb->distance_context_map);
        BROTLI_FREE(m, ref mb->literal_histograms);
        BROTLI_FREE(m, ref mb->command_histograms);
        BROTLI_FREE(m, ref mb->distance_histograms);
    }

    internal static void BrotliInitDistanceParams(BrotliDistanceParams* dist_params,
        uint npostfix, uint ndirect, int large_window)
    {
        uint alphabet_size_max;
        uint alphabet_size_limit;
        uint max_distance;

        dist_params->distance_postfix_bits = npostfix;
        dist_params->num_direct_distance_codes = ndirect;

        alphabet_size_max = BROTLI_DISTANCE_ALPHABET_SIZE(
            npostfix, ndirect, BROTLI_MAX_DISTANCE_BITS);
        alphabet_size_limit = alphabet_size_max;
        max_distance = ndirect + (1U << (int)(BROTLI_MAX_DISTANCE_BITS + npostfix + 2)) -
            (1U << (int)(npostfix + 2));

        if (large_window != 0)
        {
            BrotliDistanceCodeLimit limit = BrotliCalculateDistanceCodeLimit(
                BROTLI_MAX_ALLOWED_DISTANCE, npostfix, ndirect);
            alphabet_size_max = BROTLI_DISTANCE_ALPHABET_SIZE(
                npostfix, ndirect, BROTLI_LARGE_MAX_DISTANCE_BITS);
            alphabet_size_limit = limit.max_alphabet_size;
            max_distance = limit.max_distance;
        }

        dist_params->alphabet_size_max = alphabet_size_max;
        dist_params->alphabet_size_limit = alphabet_size_limit;
        dist_params->max_distance = max_distance;
    }

    private static void RecomputeDistancePrefixes(Command* cmds,
                                                  nuint num_commands,
                                                  BrotliDistanceParams* orig_params,
                                                  BrotliDistanceParams* new_params)
    {
        nuint i;

        if (orig_params->distance_postfix_bits == new_params->distance_postfix_bits &&
            orig_params->num_direct_distance_codes ==
            new_params->num_direct_distance_codes)
        {
            return;
        }

        for (i = 0; i < num_commands; ++i)
        {
            Command* cmd = &cmds[i];
            if (CommandCopyLen(cmd) != 0 && cmd->cmd_prefix_ >= 128)
            {
                /* dn2cpp: local instead of `&cmd->dist_prefix_` — the backend
                   widens sub-int32 struct fields; see Command.InitCommand. */
                ushort dist_prefix = 0;
                Prefix.PrefixEncodeCopyDistance(CommandRestoreDistanceCode(cmd, orig_params),
                                                new_params->num_direct_distance_codes,
                                                new_params->distance_postfix_bits,
                                                &dist_prefix,
                                                &cmd->dist_extra_);
                cmd->dist_prefix_ = dist_prefix;
            }
        }
    }

    private static bool ComputeDistanceCost(Command* cmds,
                                            nuint num_commands,
                                            BrotliDistanceParams* orig_params,
                                            BrotliDistanceParams* new_params,
                                            double* cost,
                                            HistogramDistance* tmp)
    {
        nuint i;
        bool equal_params = false;
        ushort dist_prefix = 0;
        uint dist_extra;
        double extra_bits = 0.0;
        HistogramClearDistance(tmp);

        if (orig_params->distance_postfix_bits == new_params->distance_postfix_bits &&
            orig_params->num_direct_distance_codes ==
            new_params->num_direct_distance_codes)
        {
            equal_params = true;
        }

        for (i = 0; i < num_commands; i++)
        {
            Command* cmd = &cmds[i];
            if (CommandCopyLen(cmd) != 0 && cmd->cmd_prefix_ >= 128)
            {
                if (equal_params)
                {
                    dist_prefix = cmd->dist_prefix_;
                }
                else
                {
                    uint distance = CommandRestoreDistanceCode(cmd, orig_params);
                    if (distance > new_params->max_distance)
                    {
                        return false;
                    }
                    Prefix.PrefixEncodeCopyDistance(distance,
                                                    new_params->num_direct_distance_codes,
                                                    new_params->distance_postfix_bits,
                                                    &dist_prefix,
                                                    &dist_extra);
                }
                HistogramAddDistance(tmp, (nuint)(dist_prefix & 0x3FF));
                extra_bits += dist_prefix >> 10;
            }
        }

        *cost = BrotliPopulationCostDistance(tmp) + extra_bits;
        return true;
    }

    internal static void BrotliBuildMetaBlock(MemoryManager* m,
                                              byte* ringbuffer,
                                              nuint pos,
                                              nuint mask,
                                              BrotliEncoderParams* @params,
                                              byte prev_byte,
                                              byte prev_byte2,
                                              Command* cmds,
                                              nuint num_commands,
                                              ContextType literal_context_mode,
                                              MetaBlockSplit* mb)
    {
        /* Histogram ids need to fit in one byte. */
        const nuint kMaxNumberOfHistograms = 256;
        HistogramDistance* distance_histograms;
        HistogramLiteral* literal_histograms;
        ContextType* literal_context_modes = null;
        nuint literal_histograms_size;
        nuint distance_histograms_size;
        nuint i;
        nuint literal_context_multiplier = 1;
        uint npostfix;
        uint ndirect_msb = 0;
        bool check_orig = true;
        double best_dist_cost = 1e99;
        BrotliEncoderParams* p = @params;
        BrotliDistanceParams orig_params = p->dist;
        BrotliDistanceParams new_params = p->dist;
        HistogramDistance* tmp = BROTLI_ALLOC<HistogramDistance>(m, 1);

        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(tmp)) return;

        for (npostfix = 0; npostfix <= BROTLI_MAX_NPOSTFIX; npostfix++)
        {
            for (; ndirect_msb < 16; ndirect_msb++)
            {
                uint ndirect = ndirect_msb << (int)npostfix;
                bool skip;
                double dist_cost;
                BrotliInitDistanceParams(&new_params, npostfix, ndirect,
                                         p->large_window);
                if (npostfix == orig_params.distance_postfix_bits &&
                    ndirect == orig_params.num_direct_distance_codes)
                {
                    check_orig = false;
                }
                skip = !ComputeDistanceCost(
                    cmds, num_commands, &orig_params, &new_params, &dist_cost, tmp);
                if (skip || (dist_cost > best_dist_cost))
                {
                    break;
                }
                best_dist_cost = dist_cost;
                p->dist = new_params;
            }
            if (ndirect_msb > 0) ndirect_msb--;
            ndirect_msb /= 2;
        }
        if (check_orig)
        {
            double dist_cost;
            ComputeDistanceCost(cmds, num_commands, &orig_params, &orig_params,
                                &dist_cost, tmp);
            if (dist_cost < best_dist_cost)
            {
                /* NB: currently unused; uncomment when more param tuning is added. */
                /* best_dist_cost = dist_cost; */
                p->dist = orig_params;
            }
        }
        BROTLI_FREE(m, ref tmp);
        RecomputeDistancePrefixes(cmds, num_commands, &orig_params, &p->dist);

        BrotliSplitBlock(m, cmds, num_commands,
                         ringbuffer, pos, mask, p,
                         &mb->literal_split,
                         &mb->command_split,
                         &mb->distance_split);
        if (BROTLI_IS_OOM(m)) return;

        if (p->disable_literal_context_modeling == 0)
        {
            literal_context_multiplier = 1 << BROTLI_LITERAL_CONTEXT_BITS;
            literal_context_modes =
                BROTLI_ALLOC<ContextType>(m, mb->literal_split.num_types);
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(literal_context_modes)) return;
            for (i = 0; i < mb->literal_split.num_types; ++i)
            {
                literal_context_modes[i] = literal_context_mode;
            }
        }

        literal_histograms_size =
            mb->literal_split.num_types * literal_context_multiplier;
        literal_histograms =
            BROTLI_ALLOC<HistogramLiteral>(m, literal_histograms_size);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(literal_histograms)) return;
        ClearHistogramsLiteral(literal_histograms, literal_histograms_size);

        distance_histograms_size =
            mb->distance_split.num_types << BROTLI_DISTANCE_CONTEXT_BITS;
        distance_histograms =
            BROTLI_ALLOC<HistogramDistance>(m, distance_histograms_size);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(distance_histograms)) return;
        ClearHistogramsDistance(distance_histograms, distance_histograms_size);

        /* BROTLI_DCHECK(mb->command_histograms == 0); */
        mb->command_histograms_size = mb->command_split.num_types;
        mb->command_histograms =
            BROTLI_ALLOC<HistogramCommand>(m, mb->command_histograms_size);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(mb->command_histograms)) return;
        ClearHistogramsCommand(mb->command_histograms, mb->command_histograms_size);

        BrotliBuildHistogramsWithContext(cmds, num_commands,
            &mb->literal_split, &mb->command_split, &mb->distance_split,
            ringbuffer, pos, mask, prev_byte, prev_byte2, literal_context_modes,
            literal_histograms, mb->command_histograms, distance_histograms);
        BROTLI_FREE(m, ref literal_context_modes);

        /* BROTLI_DCHECK(mb->literal_context_map == 0); */
        mb->literal_context_map_size =
            mb->literal_split.num_types << BROTLI_LITERAL_CONTEXT_BITS;
        mb->literal_context_map =
            BROTLI_ALLOC<uint>(m, mb->literal_context_map_size);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(mb->literal_context_map)) return;

        /* BROTLI_DCHECK(mb->literal_histograms == 0); */
        mb->literal_histograms_size = mb->literal_context_map_size;
        mb->literal_histograms =
            BROTLI_ALLOC<HistogramLiteral>(m, mb->literal_histograms_size);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(mb->literal_histograms)) return;

        Cluster.BrotliClusterHistogramsLiteral(m, literal_histograms, literal_histograms_size,
            kMaxNumberOfHistograms, mb->literal_histograms,
            &mb->literal_histograms_size, mb->literal_context_map);
        if (BROTLI_IS_OOM(m)) return;
        BROTLI_FREE(m, ref literal_histograms);

        if (p->disable_literal_context_modeling != 0)
        {
            /* Distribute assignment to all contexts. */
            for (i = mb->literal_split.num_types; i != 0;)
            {
                nuint j = 0;
                i--;
                for (; j < (1 << BROTLI_LITERAL_CONTEXT_BITS); j++)
                {
                    mb->literal_context_map[(i << BROTLI_LITERAL_CONTEXT_BITS) + j] =
                        mb->literal_context_map[i];
                }
            }
        }

        /* BROTLI_DCHECK(mb->distance_context_map == 0); */
        mb->distance_context_map_size =
            mb->distance_split.num_types << BROTLI_DISTANCE_CONTEXT_BITS;
        mb->distance_context_map =
            BROTLI_ALLOC<uint>(m, mb->distance_context_map_size);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(mb->distance_context_map)) return;

        /* BROTLI_DCHECK(mb->distance_histograms == 0); */
        mb->distance_histograms_size = mb->distance_context_map_size;
        mb->distance_histograms =
            BROTLI_ALLOC<HistogramDistance>(m, mb->distance_histograms_size);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(mb->distance_histograms)) return;

        Cluster.BrotliClusterHistogramsDistance(m, distance_histograms,
                                                mb->distance_context_map_size,
                                                kMaxNumberOfHistograms,
                                                mb->distance_histograms,
                                                &mb->distance_histograms_size,
                                                mb->distance_context_map);
        if (BROTLI_IS_OOM(m)) return;
        BROTLI_FREE(m, ref distance_histograms);
    }

    /* --- metablock_inc.h expanded for FN(X) = XLiteral --- */

    private static void InitBlockSplitterLiteral(
        MemoryManager* m, BlockSplitterLiteral* self, nuint alphabet_size,
        nuint min_block_size, double split_threshold, nuint num_symbols,
        BlockSplit* split, HistogramLiteral** histograms, nuint* histograms_size)
    {
        nuint max_num_blocks = num_symbols / min_block_size + 1;
        /* We have to allocate one more histogram than the maximum number of block
           types for the current histogram when the meta-block is too big. */
        nuint max_num_types =
            BROTLI_MIN(max_num_blocks, BROTLI_MAX_NUMBER_OF_BLOCK_TYPES + 1);
        self->alphabet_size_ = alphabet_size;
        self->min_block_size_ = min_block_size;
        self->split_threshold_ = split_threshold;
        self->num_blocks_ = 0;
        self->split_ = split;
        self->histograms_size_ = histograms_size;
        self->target_block_size_ = min_block_size;
        self->block_size_ = 0;
        self->curr_histogram_ix_ = 0;
        self->merge_last_count_ = 0;
        BROTLI_ENSURE_CAPACITY(m,
            ref split->types, ref split->types_alloc_size, max_num_blocks);
        BROTLI_ENSURE_CAPACITY(m,
            ref split->lengths, ref split->lengths_alloc_size, max_num_blocks);
        if (BROTLI_IS_OOM(m)) return;
        self->split_->num_blocks = max_num_blocks;
        /* BROTLI_DCHECK(*histograms == 0); */
        *histograms_size = max_num_types;
        *histograms = BROTLI_ALLOC<HistogramLiteral>(m, *histograms_size);
        self->histograms_ = *histograms;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(*histograms)) return;
        /* Clear only current histogram. */
        HistogramClearLiteral(&self->histograms_[0]);
        self->last_histogram_ix_[0] = self->last_histogram_ix_[1] = 0;
    }

    /* Does either of three things:
         (1) emits the current block with a new block type;
         (2) emits the current block with the type of the second last block;
         (3) merges the current block with the last block. */
    private static void BlockSplitterFinishBlockLiteral(
        BlockSplitterLiteral* self, int is_final)
    {
        BlockSplit* split = self->split_;
        double* last_entropy = self->last_entropy_;
        HistogramLiteral* histograms = self->histograms_;
        HistogramLiteral* combined_histo = &self->combined_histo0;  /* C: self->combined_histo[2] */
        self->block_size_ =
            BROTLI_MAX(self->block_size_, self->min_block_size_);
        if (self->num_blocks_ == 0)
        {
            /* Create first block. */
            split->lengths[0] = (uint)self->block_size_;
            split->types[0] = 0;
            last_entropy[0] =
                BitsEntropy(histograms[0].data_, self->alphabet_size_);
            last_entropy[1] = last_entropy[0];
            ++self->num_blocks_;
            ++split->num_types;
            ++self->curr_histogram_ix_;
            if (self->curr_histogram_ix_ < *self->histograms_size_)
                HistogramClearLiteral(&histograms[self->curr_histogram_ix_]);
            self->block_size_ = 0;
        }
        else if (self->block_size_ > 0)
        {
            double entropy = BitsEntropy(histograms[self->curr_histogram_ix_].data_,
                                         self->alphabet_size_);
            double* combined_entropy = stackalloc double[2];
            double* diff = stackalloc double[2];
            nuint j;
            for (j = 0; j < 2; ++j)
            {
                nuint last_histogram_ix = (nuint)self->last_histogram_ix_[j];
                combined_histo[j] = histograms[self->curr_histogram_ix_];
                HistogramAddHistogramLiteral(&combined_histo[j],
                    &histograms[last_histogram_ix]);
                combined_entropy[j] = BitsEntropy(
                    &combined_histo[j].data_[0], self->alphabet_size_);
                diff[j] = combined_entropy[j] - entropy - last_entropy[j];
            }

            if (split->num_types < BROTLI_MAX_NUMBER_OF_BLOCK_TYPES &&
                diff[0] > self->split_threshold_ &&
                diff[1] > self->split_threshold_)
            {
                /* Create new block. */
                split->lengths[self->num_blocks_] = (uint)self->block_size_;
                split->types[self->num_blocks_] = (byte)split->num_types;
                self->last_histogram_ix_[1] = self->last_histogram_ix_[0];
                self->last_histogram_ix_[0] = (byte)split->num_types;
                last_entropy[1] = last_entropy[0];
                last_entropy[0] = entropy;
                ++self->num_blocks_;
                ++split->num_types;
                ++self->curr_histogram_ix_;
                if (self->curr_histogram_ix_ < *self->histograms_size_)
                    HistogramClearLiteral(&histograms[self->curr_histogram_ix_]);
                self->block_size_ = 0;
                self->merge_last_count_ = 0;
                self->target_block_size_ = self->min_block_size_;
            }
            else if (diff[1] < diff[0] - 20.0)
            {
                /* Combine this block with second last block. */
                split->lengths[self->num_blocks_] = (uint)self->block_size_;
                split->types[self->num_blocks_] = split->types[self->num_blocks_ - 2];
                {
                    ulong __tmp = self->last_histogram_ix_[0];  /* BROTLI_SWAP */
                    self->last_histogram_ix_[0] = self->last_histogram_ix_[1];
                    self->last_histogram_ix_[1] = __tmp;
                }
                histograms[(nuint)self->last_histogram_ix_[0]] = combined_histo[1];
                last_entropy[1] = last_entropy[0];
                last_entropy[0] = combined_entropy[1];
                ++self->num_blocks_;
                self->block_size_ = 0;
                HistogramClearLiteral(&histograms[self->curr_histogram_ix_]);
                self->merge_last_count_ = 0;
                self->target_block_size_ = self->min_block_size_;
            }
            else
            {
                /* Combine this block with last block. */
                split->lengths[self->num_blocks_ - 1] += (uint)self->block_size_;
                histograms[(nuint)self->last_histogram_ix_[0]] = combined_histo[0];
                last_entropy[0] = combined_entropy[0];
                if (split->num_types == 1)
                {
                    last_entropy[1] = last_entropy[0];
                }
                self->block_size_ = 0;
                HistogramClearLiteral(&histograms[self->curr_histogram_ix_]);
                if (++self->merge_last_count_ > 1)
                {
                    self->target_block_size_ += self->min_block_size_;
                }
            }
        }
        if (is_final != 0)
        {
            *self->histograms_size_ = split->num_types;
            split->num_blocks = self->num_blocks_;
        }
    }

    /* Adds the next symbol to the current histogram. When the current histogram
       reaches the target size, decides on merging the block. */
    private static void BlockSplitterAddSymbolLiteral(BlockSplitterLiteral* self, nuint symbol)
    {
        HistogramAddLiteral(&self->histograms_[self->curr_histogram_ix_], symbol);
        ++self->block_size_;
        if (self->block_size_ == self->target_block_size_)
        {
            BlockSplitterFinishBlockLiteral(self, /* is_final = */ 0);
        }
    }

    /* --- metablock_inc.h expanded for FN(X) = XCommand --- */

    private static void InitBlockSplitterCommand(
        MemoryManager* m, BlockSplitterCommand* self, nuint alphabet_size,
        nuint min_block_size, double split_threshold, nuint num_symbols,
        BlockSplit* split, HistogramCommand** histograms, nuint* histograms_size)
    {
        nuint max_num_blocks = num_symbols / min_block_size + 1;
        /* We have to allocate one more histogram than the maximum number of block
           types for the current histogram when the meta-block is too big. */
        nuint max_num_types =
            BROTLI_MIN(max_num_blocks, BROTLI_MAX_NUMBER_OF_BLOCK_TYPES + 1);
        self->alphabet_size_ = alphabet_size;
        self->min_block_size_ = min_block_size;
        self->split_threshold_ = split_threshold;
        self->num_blocks_ = 0;
        self->split_ = split;
        self->histograms_size_ = histograms_size;
        self->target_block_size_ = min_block_size;
        self->block_size_ = 0;
        self->curr_histogram_ix_ = 0;
        self->merge_last_count_ = 0;
        BROTLI_ENSURE_CAPACITY(m,
            ref split->types, ref split->types_alloc_size, max_num_blocks);
        BROTLI_ENSURE_CAPACITY(m,
            ref split->lengths, ref split->lengths_alloc_size, max_num_blocks);
        if (BROTLI_IS_OOM(m)) return;
        self->split_->num_blocks = max_num_blocks;
        /* BROTLI_DCHECK(*histograms == 0); */
        *histograms_size = max_num_types;
        *histograms = BROTLI_ALLOC<HistogramCommand>(m, *histograms_size);
        self->histograms_ = *histograms;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(*histograms)) return;
        /* Clear only current histogram. */
        HistogramClearCommand(&self->histograms_[0]);
        self->last_histogram_ix_[0] = self->last_histogram_ix_[1] = 0;
    }

    private static void BlockSplitterFinishBlockCommand(
        BlockSplitterCommand* self, int is_final)
    {
        BlockSplit* split = self->split_;
        double* last_entropy = self->last_entropy_;
        HistogramCommand* histograms = self->histograms_;
        HistogramCommand* combined_histo = &self->combined_histo0;  /* C: self->combined_histo[2] */
        self->block_size_ =
            BROTLI_MAX(self->block_size_, self->min_block_size_);
        if (self->num_blocks_ == 0)
        {
            /* Create first block. */
            split->lengths[0] = (uint)self->block_size_;
            split->types[0] = 0;
            last_entropy[0] =
                BitsEntropy(histograms[0].data_, self->alphabet_size_);
            last_entropy[1] = last_entropy[0];
            ++self->num_blocks_;
            ++split->num_types;
            ++self->curr_histogram_ix_;
            if (self->curr_histogram_ix_ < *self->histograms_size_)
                HistogramClearCommand(&histograms[self->curr_histogram_ix_]);
            self->block_size_ = 0;
        }
        else if (self->block_size_ > 0)
        {
            double entropy = BitsEntropy(histograms[self->curr_histogram_ix_].data_,
                                         self->alphabet_size_);
            double* combined_entropy = stackalloc double[2];
            double* diff = stackalloc double[2];
            nuint j;
            for (j = 0; j < 2; ++j)
            {
                nuint last_histogram_ix = (nuint)self->last_histogram_ix_[j];
                combined_histo[j] = histograms[self->curr_histogram_ix_];
                HistogramAddHistogramCommand(&combined_histo[j],
                    &histograms[last_histogram_ix]);
                combined_entropy[j] = BitsEntropy(
                    &combined_histo[j].data_[0], self->alphabet_size_);
                diff[j] = combined_entropy[j] - entropy - last_entropy[j];
            }

            if (split->num_types < BROTLI_MAX_NUMBER_OF_BLOCK_TYPES &&
                diff[0] > self->split_threshold_ &&
                diff[1] > self->split_threshold_)
            {
                /* Create new block. */
                split->lengths[self->num_blocks_] = (uint)self->block_size_;
                split->types[self->num_blocks_] = (byte)split->num_types;
                self->last_histogram_ix_[1] = self->last_histogram_ix_[0];
                self->last_histogram_ix_[0] = (byte)split->num_types;
                last_entropy[1] = last_entropy[0];
                last_entropy[0] = entropy;
                ++self->num_blocks_;
                ++split->num_types;
                ++self->curr_histogram_ix_;
                if (self->curr_histogram_ix_ < *self->histograms_size_)
                    HistogramClearCommand(&histograms[self->curr_histogram_ix_]);
                self->block_size_ = 0;
                self->merge_last_count_ = 0;
                self->target_block_size_ = self->min_block_size_;
            }
            else if (diff[1] < diff[0] - 20.0)
            {
                /* Combine this block with second last block. */
                split->lengths[self->num_blocks_] = (uint)self->block_size_;
                split->types[self->num_blocks_] = split->types[self->num_blocks_ - 2];
                {
                    ulong __tmp = self->last_histogram_ix_[0];  /* BROTLI_SWAP */
                    self->last_histogram_ix_[0] = self->last_histogram_ix_[1];
                    self->last_histogram_ix_[1] = __tmp;
                }
                histograms[(nuint)self->last_histogram_ix_[0]] = combined_histo[1];
                last_entropy[1] = last_entropy[0];
                last_entropy[0] = combined_entropy[1];
                ++self->num_blocks_;
                self->block_size_ = 0;
                HistogramClearCommand(&histograms[self->curr_histogram_ix_]);
                self->merge_last_count_ = 0;
                self->target_block_size_ = self->min_block_size_;
            }
            else
            {
                /* Combine this block with last block. */
                split->lengths[self->num_blocks_ - 1] += (uint)self->block_size_;
                histograms[(nuint)self->last_histogram_ix_[0]] = combined_histo[0];
                last_entropy[0] = combined_entropy[0];
                if (split->num_types == 1)
                {
                    last_entropy[1] = last_entropy[0];
                }
                self->block_size_ = 0;
                HistogramClearCommand(&histograms[self->curr_histogram_ix_]);
                if (++self->merge_last_count_ > 1)
                {
                    self->target_block_size_ += self->min_block_size_;
                }
            }
        }
        if (is_final != 0)
        {
            *self->histograms_size_ = split->num_types;
            split->num_blocks = self->num_blocks_;
        }
    }

    private static void BlockSplitterAddSymbolCommand(BlockSplitterCommand* self, nuint symbol)
    {
        HistogramAddCommand(&self->histograms_[self->curr_histogram_ix_], symbol);
        ++self->block_size_;
        if (self->block_size_ == self->target_block_size_)
        {
            BlockSplitterFinishBlockCommand(self, /* is_final = */ 0);
        }
    }

    /* --- metablock_inc.h expanded for FN(X) = XDistance --- */

    private static void InitBlockSplitterDistance(
        MemoryManager* m, BlockSplitterDistance* self, nuint alphabet_size,
        nuint min_block_size, double split_threshold, nuint num_symbols,
        BlockSplit* split, HistogramDistance** histograms, nuint* histograms_size)
    {
        nuint max_num_blocks = num_symbols / min_block_size + 1;
        /* We have to allocate one more histogram than the maximum number of block
           types for the current histogram when the meta-block is too big. */
        nuint max_num_types =
            BROTLI_MIN(max_num_blocks, BROTLI_MAX_NUMBER_OF_BLOCK_TYPES + 1);
        self->alphabet_size_ = alphabet_size;
        self->min_block_size_ = min_block_size;
        self->split_threshold_ = split_threshold;
        self->num_blocks_ = 0;
        self->split_ = split;
        self->histograms_size_ = histograms_size;
        self->target_block_size_ = min_block_size;
        self->block_size_ = 0;
        self->curr_histogram_ix_ = 0;
        self->merge_last_count_ = 0;
        BROTLI_ENSURE_CAPACITY(m,
            ref split->types, ref split->types_alloc_size, max_num_blocks);
        BROTLI_ENSURE_CAPACITY(m,
            ref split->lengths, ref split->lengths_alloc_size, max_num_blocks);
        if (BROTLI_IS_OOM(m)) return;
        self->split_->num_blocks = max_num_blocks;
        /* BROTLI_DCHECK(*histograms == 0); */
        *histograms_size = max_num_types;
        *histograms = BROTLI_ALLOC<HistogramDistance>(m, *histograms_size);
        self->histograms_ = *histograms;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(*histograms)) return;
        /* Clear only current histogram. */
        HistogramClearDistance(&self->histograms_[0]);
        self->last_histogram_ix_[0] = self->last_histogram_ix_[1] = 0;
    }

    private static void BlockSplitterFinishBlockDistance(
        BlockSplitterDistance* self, int is_final)
    {
        BlockSplit* split = self->split_;
        double* last_entropy = self->last_entropy_;
        HistogramDistance* histograms = self->histograms_;
        HistogramDistance* combined_histo = &self->combined_histo0;  /* C: self->combined_histo[2] */
        self->block_size_ =
            BROTLI_MAX(self->block_size_, self->min_block_size_);
        if (self->num_blocks_ == 0)
        {
            /* Create first block. */
            split->lengths[0] = (uint)self->block_size_;
            split->types[0] = 0;
            last_entropy[0] =
                BitsEntropy(histograms[0].data_, self->alphabet_size_);
            last_entropy[1] = last_entropy[0];
            ++self->num_blocks_;
            ++split->num_types;
            ++self->curr_histogram_ix_;
            if (self->curr_histogram_ix_ < *self->histograms_size_)
                HistogramClearDistance(&histograms[self->curr_histogram_ix_]);
            self->block_size_ = 0;
        }
        else if (self->block_size_ > 0)
        {
            double entropy = BitsEntropy(histograms[self->curr_histogram_ix_].data_,
                                         self->alphabet_size_);
            double* combined_entropy = stackalloc double[2];
            double* diff = stackalloc double[2];
            nuint j;
            for (j = 0; j < 2; ++j)
            {
                nuint last_histogram_ix = (nuint)self->last_histogram_ix_[j];
                combined_histo[j] = histograms[self->curr_histogram_ix_];
                HistogramAddHistogramDistance(&combined_histo[j],
                    &histograms[last_histogram_ix]);
                combined_entropy[j] = BitsEntropy(
                    &combined_histo[j].data_[0], self->alphabet_size_);
                diff[j] = combined_entropy[j] - entropy - last_entropy[j];
            }

            if (split->num_types < BROTLI_MAX_NUMBER_OF_BLOCK_TYPES &&
                diff[0] > self->split_threshold_ &&
                diff[1] > self->split_threshold_)
            {
                /* Create new block. */
                split->lengths[self->num_blocks_] = (uint)self->block_size_;
                split->types[self->num_blocks_] = (byte)split->num_types;
                self->last_histogram_ix_[1] = self->last_histogram_ix_[0];
                self->last_histogram_ix_[0] = (byte)split->num_types;
                last_entropy[1] = last_entropy[0];
                last_entropy[0] = entropy;
                ++self->num_blocks_;
                ++split->num_types;
                ++self->curr_histogram_ix_;
                if (self->curr_histogram_ix_ < *self->histograms_size_)
                    HistogramClearDistance(&histograms[self->curr_histogram_ix_]);
                self->block_size_ = 0;
                self->merge_last_count_ = 0;
                self->target_block_size_ = self->min_block_size_;
            }
            else if (diff[1] < diff[0] - 20.0)
            {
                /* Combine this block with second last block. */
                split->lengths[self->num_blocks_] = (uint)self->block_size_;
                split->types[self->num_blocks_] = split->types[self->num_blocks_ - 2];
                {
                    ulong __tmp = self->last_histogram_ix_[0];  /* BROTLI_SWAP */
                    self->last_histogram_ix_[0] = self->last_histogram_ix_[1];
                    self->last_histogram_ix_[1] = __tmp;
                }
                histograms[(nuint)self->last_histogram_ix_[0]] = combined_histo[1];
                last_entropy[1] = last_entropy[0];
                last_entropy[0] = combined_entropy[1];
                ++self->num_blocks_;
                self->block_size_ = 0;
                HistogramClearDistance(&histograms[self->curr_histogram_ix_]);
                self->merge_last_count_ = 0;
                self->target_block_size_ = self->min_block_size_;
            }
            else
            {
                /* Combine this block with last block. */
                split->lengths[self->num_blocks_ - 1] += (uint)self->block_size_;
                histograms[(nuint)self->last_histogram_ix_[0]] = combined_histo[0];
                last_entropy[0] = combined_entropy[0];
                if (split->num_types == 1)
                {
                    last_entropy[1] = last_entropy[0];
                }
                self->block_size_ = 0;
                HistogramClearDistance(&histograms[self->curr_histogram_ix_]);
                if (++self->merge_last_count_ > 1)
                {
                    self->target_block_size_ += self->min_block_size_;
                }
            }
        }
        if (is_final != 0)
        {
            *self->histograms_size_ = split->num_types;
            split->num_blocks = self->num_blocks_;
        }
    }

    private static void BlockSplitterAddSymbolDistance(BlockSplitterDistance* self, nuint symbol)
    {
        HistogramAddDistance(&self->histograms_[self->curr_histogram_ix_], symbol);
        ++self->block_size_;
        if (self->block_size_ == self->target_block_size_)
        {
            BlockSplitterFinishBlockDistance(self, /* is_final = */ 0);
        }
    }

    /* --- ContextBlockSplitter (metablock.c) --- */

    private static void InitContextBlockSplitter(
        MemoryManager* m, ContextBlockSplitter* self, nuint alphabet_size,
        nuint num_contexts, nuint min_block_size, double split_threshold,
        nuint num_symbols, BlockSplit* split, HistogramLiteral** histograms,
        nuint* histograms_size)
    {
        nuint max_num_blocks = num_symbols / min_block_size + 1;
        nuint max_num_types;
        /* BROTLI_DCHECK(num_contexts <= BROTLI_MAX_STATIC_CONTEXTS); */

        self->alphabet_size_ = alphabet_size;
        self->num_contexts_ = num_contexts;
        self->max_block_types_ = BROTLI_MAX_NUMBER_OF_BLOCK_TYPES / num_contexts;
        self->min_block_size_ = min_block_size;
        self->split_threshold_ = split_threshold;
        self->num_blocks_ = 0;
        self->split_ = split;
        self->histograms_size_ = histograms_size;
        self->target_block_size_ = min_block_size;
        self->block_size_ = 0;
        self->curr_histogram_ix_ = 0;
        self->merge_last_count_ = 0;

        /* We have to allocate one more histogram than the maximum number of block
           types for the current histogram when the meta-block is too big. */
        max_num_types =
            BROTLI_MIN(max_num_blocks, self->max_block_types_ + 1);
        BROTLI_ENSURE_CAPACITY(m,
            ref split->types, ref split->types_alloc_size, max_num_blocks);
        BROTLI_ENSURE_CAPACITY(m,
            ref split->lengths, ref split->lengths_alloc_size, max_num_blocks);
        if (BROTLI_IS_OOM(m)) return;
        split->num_blocks = max_num_blocks;
        if (BROTLI_IS_OOM(m)) return;
        /* BROTLI_DCHECK(*histograms == 0); */
        *histograms_size = max_num_types * num_contexts;
        *histograms = BROTLI_ALLOC<HistogramLiteral>(m, *histograms_size);
        self->histograms_ = *histograms;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(*histograms)) return;
        /* Clear only current histogram. */
        ClearHistogramsLiteral(&self->histograms_[0], num_contexts);
        self->last_histogram_ix_[0] = self->last_histogram_ix_[1] = 0;
    }

    /* Does either of three things:
         (1) emits the current block with a new block type;
         (2) emits the current block with the type of the second last block;
         (3) merges the current block with the last block. */
    private static void ContextBlockSplitterFinishBlock(
        ContextBlockSplitter* self, MemoryManager* m, int is_final)
    {
        BlockSplit* split = self->split_;
        nuint num_contexts = self->num_contexts_;
        double* last_entropy = self->last_entropy_;
        HistogramLiteral* histograms = self->histograms_;

        if (self->block_size_ < self->min_block_size_)
        {
            self->block_size_ = self->min_block_size_;
        }
        if (self->num_blocks_ == 0)
        {
            nuint i;
            /* Create first block. */
            split->lengths[0] = (uint)self->block_size_;
            split->types[0] = 0;

            for (i = 0; i < num_contexts; ++i)
            {
                last_entropy[i] =
                    BitsEntropy(histograms[i].data_, self->alphabet_size_);
                last_entropy[num_contexts + i] = last_entropy[i];
            }
            ++self->num_blocks_;
            ++split->num_types;
            self->curr_histogram_ix_ += num_contexts;
            if (self->curr_histogram_ix_ < *self->histograms_size_)
            {
                ClearHistogramsLiteral(
                    &self->histograms_[self->curr_histogram_ix_], self->num_contexts_);
            }
            self->block_size_ = 0;
        }
        else if (self->block_size_ > 0)
        {
            /* Try merging the set of histograms for the current block type with the
               respective set of histograms for the last and second last block types.
               Decide over the split based on the total reduction of entropy across
               all contexts. */
            double* entropy = stackalloc double[BROTLI_MAX_STATIC_CONTEXTS];
            HistogramLiteral* combined_histo =
                BROTLI_ALLOC<HistogramLiteral>(m, 2 * num_contexts);
            double* combined_entropy = stackalloc double[2 * BROTLI_MAX_STATIC_CONTEXTS];
            double* diff = stackalloc double[2] { 0.0, 0.0 };
            nuint i;
            if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(combined_histo)) return;
            for (i = 0; i < num_contexts; ++i)
            {
                nuint curr_histo_ix = self->curr_histogram_ix_ + i;
                nuint j;
                entropy[i] = BitsEntropy(histograms[curr_histo_ix].data_,
                                         self->alphabet_size_);
                for (j = 0; j < 2; ++j)
                {
                    nuint jx = j * num_contexts + i;
                    nuint last_histogram_ix = (nuint)self->last_histogram_ix_[j] + i;
                    combined_histo[jx] = histograms[curr_histo_ix];
                    HistogramAddHistogramLiteral(&combined_histo[jx],
                        &histograms[last_histogram_ix]);
                    combined_entropy[jx] = BitsEntropy(
                        &combined_histo[jx].data_[0], self->alphabet_size_);
                    diff[j] += combined_entropy[jx] - entropy[i] - last_entropy[jx];
                }
            }

            if (split->num_types < self->max_block_types_ &&
                diff[0] > self->split_threshold_ &&
                diff[1] > self->split_threshold_)
            {
                /* Create new block. */
                split->lengths[self->num_blocks_] = (uint)self->block_size_;
                split->types[self->num_blocks_] = (byte)split->num_types;
                self->last_histogram_ix_[1] = self->last_histogram_ix_[0];
                self->last_histogram_ix_[0] = split->num_types * num_contexts;
                for (i = 0; i < num_contexts; ++i)
                {
                    last_entropy[num_contexts + i] = last_entropy[i];
                    last_entropy[i] = entropy[i];
                }
                ++self->num_blocks_;
                ++split->num_types;
                self->curr_histogram_ix_ += num_contexts;
                if (self->curr_histogram_ix_ < *self->histograms_size_)
                {
                    ClearHistogramsLiteral(
                        &self->histograms_[self->curr_histogram_ix_], self->num_contexts_);
                }
                self->block_size_ = 0;
                self->merge_last_count_ = 0;
                self->target_block_size_ = self->min_block_size_;
            }
            else if (diff[1] < diff[0] - 20.0)
            {
                /* Combine this block with second last block. */
                split->lengths[self->num_blocks_] = (uint)self->block_size_;
                split->types[self->num_blocks_] = split->types[self->num_blocks_ - 2];
                {
                    ulong __tmp = self->last_histogram_ix_[0];  /* BROTLI_SWAP */
                    self->last_histogram_ix_[0] = self->last_histogram_ix_[1];
                    self->last_histogram_ix_[1] = __tmp;
                }
                for (i = 0; i < num_contexts; ++i)
                {
                    histograms[(nuint)self->last_histogram_ix_[0] + i] =
                        combined_histo[num_contexts + i];
                    last_entropy[num_contexts + i] = last_entropy[i];
                    last_entropy[i] = combined_entropy[num_contexts + i];
                    HistogramClearLiteral(&histograms[self->curr_histogram_ix_ + i]);
                }
                ++self->num_blocks_;
                self->block_size_ = 0;
                self->merge_last_count_ = 0;
                self->target_block_size_ = self->min_block_size_;
            }
            else
            {
                /* Combine this block with last block. */
                split->lengths[self->num_blocks_ - 1] += (uint)self->block_size_;
                for (i = 0; i < num_contexts; ++i)
                {
                    histograms[(nuint)self->last_histogram_ix_[0] + i] = combined_histo[i];
                    last_entropy[i] = combined_entropy[i];
                    if (split->num_types == 1)
                    {
                        last_entropy[num_contexts + i] = last_entropy[i];
                    }
                    HistogramClearLiteral(&histograms[self->curr_histogram_ix_ + i]);
                }
                self->block_size_ = 0;
                if (++self->merge_last_count_ > 1)
                {
                    self->target_block_size_ += self->min_block_size_;
                }
            }
            BROTLI_FREE(m, ref combined_histo);
        }
        if (is_final != 0)
        {
            *self->histograms_size_ = split->num_types * num_contexts;
            split->num_blocks = self->num_blocks_;
        }
    }

    /* Adds the next symbol to the current block type and context. When the
       current block reaches the target size, decides on merging the block. */
    private static void ContextBlockSplitterAddSymbol(
        ContextBlockSplitter* self, MemoryManager* m,
        nuint symbol, nuint context)
    {
        HistogramAddLiteral(&self->histograms_[self->curr_histogram_ix_ + context],
            symbol);
        ++self->block_size_;
        if (self->block_size_ == self->target_block_size_)
        {
            ContextBlockSplitterFinishBlock(self, m, /* is_final = */ 0);
            if (BROTLI_IS_OOM(m)) return;
        }
    }

    private static void MapStaticContexts(MemoryManager* m,
                                          nuint num_contexts,
                                          uint* static_context_map,
                                          MetaBlockSplit* mb)
    {
        nuint i;
        /* BROTLI_DCHECK(mb->literal_context_map == 0); */
        mb->literal_context_map_size =
            mb->literal_split.num_types << BROTLI_LITERAL_CONTEXT_BITS;
        mb->literal_context_map =
            BROTLI_ALLOC<uint>(m, mb->literal_context_map_size);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(mb->literal_context_map)) return;

        for (i = 0; i < mb->literal_split.num_types; ++i)
        {
            uint offset = (uint)(i * num_contexts);
            nuint j;
            for (j = 0; j < (1u << BROTLI_LITERAL_CONTEXT_BITS); ++j)
            {
                mb->literal_context_map[(i << BROTLI_LITERAL_CONTEXT_BITS) + j] =
                    offset + static_context_map[j];
            }
        }
    }

    private static void BrotliBuildMetaBlockGreedyInternal(
        MemoryManager* m, GreedyMetablockArena* arena, byte* ringbuffer,
        nuint pos, nuint mask, byte prev_byte, byte prev_byte2,
        byte* literal_context_lut, nuint num_contexts,
        uint* static_context_map, Command* commands,
        nuint n_commands, MetaBlockSplit* mb)
    {
        nuint num_literals = 0;
        nuint i;
        for (i = 0; i < n_commands; ++i)
        {
            num_literals += commands[i].insert_len_;
        }

        if (num_contexts == 1)
        {
            InitBlockSplitterLiteral(m, &arena->lit_blocks.plain, 256, 512, 400.0,
                num_literals, &mb->literal_split, &mb->literal_histograms,
                &mb->literal_histograms_size);
        }
        else
        {
            InitContextBlockSplitter(m, &arena->lit_blocks.ctx, 256, num_contexts, 512,
                400.0, num_literals, &mb->literal_split, &mb->literal_histograms,
                &mb->literal_histograms_size);
        }
        if (BROTLI_IS_OOM(m)) return;
        InitBlockSplitterCommand(m, &arena->cmd_blocks, BROTLI_NUM_COMMAND_SYMBOLS,
            1024, 500.0, n_commands, &mb->command_split, &mb->command_histograms,
            &mb->command_histograms_size);
        if (BROTLI_IS_OOM(m)) return;
        InitBlockSplitterDistance(m, &arena->dist_blocks, 64, 512, 100.0, n_commands,
            &mb->distance_split, &mb->distance_histograms,
            &mb->distance_histograms_size);
        if (BROTLI_IS_OOM(m)) return;

        for (i = 0; i < n_commands; ++i)
        {
            Command cmd = commands[i];
            nuint j;
            BlockSplitterAddSymbolCommand(&arena->cmd_blocks, cmd.cmd_prefix_);
            for (j = cmd.insert_len_; j != 0; --j)
            {
                byte literal = ringbuffer[pos & mask];
                if (num_contexts == 1)
                {
                    BlockSplitterAddSymbolLiteral(&arena->lit_blocks.plain, literal);
                }
                else
                {
                    nuint context =
                        BROTLI_CONTEXT(prev_byte, prev_byte2, literal_context_lut);
                    ContextBlockSplitterAddSymbol(&arena->lit_blocks.ctx, m, literal,
                                                  static_context_map[context]);
                    if (BROTLI_IS_OOM(m)) return;
                }
                prev_byte2 = prev_byte;
                prev_byte = literal;
                ++pos;
            }
            pos += CommandCopyLen(&cmd);
            if (CommandCopyLen(&cmd) != 0)
            {
                prev_byte2 = ringbuffer[(pos - 2) & mask];
                prev_byte = ringbuffer[(pos - 1) & mask];
                if (cmd.cmd_prefix_ >= 128)
                {
                    BlockSplitterAddSymbolDistance(
                        &arena->dist_blocks, (nuint)(cmd.dist_prefix_ & 0x3FF));
                }
            }
        }

        if (num_contexts == 1)
        {
            BlockSplitterFinishBlockLiteral(
                &arena->lit_blocks.plain, /* is_final = */ 1);
        }
        else
        {
            ContextBlockSplitterFinishBlock(
                &arena->lit_blocks.ctx, m, /* is_final = */ 1);
            if (BROTLI_IS_OOM(m)) return;
        }
        BlockSplitterFinishBlockCommand(
            &arena->cmd_blocks, /* is_final = */ 1);
        BlockSplitterFinishBlockDistance(
            &arena->dist_blocks, /* is_final = */ 1);

        if (num_contexts > 1)
        {
            MapStaticContexts(m, num_contexts, static_context_map, mb);
        }
    }

    internal static void BrotliBuildMetaBlockGreedy(MemoryManager* m,
                                                    byte* ringbuffer,
                                                    nuint pos,
                                                    nuint mask,
                                                    byte prev_byte,
                                                    byte prev_byte2,
                                                    byte* literal_context_lut,
                                                    nuint num_contexts,
                                                    uint* static_context_map,
                                                    Command* commands,
                                                    nuint n_commands,
                                                    MetaBlockSplit* mb)
    {
        GreedyMetablockArena* arena = BROTLI_ALLOC<GreedyMetablockArena>(m, 1);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(arena)) return;
        if (num_contexts == 1)
        {
            BrotliBuildMetaBlockGreedyInternal(m, arena, ringbuffer, pos, mask,
                prev_byte, prev_byte2, literal_context_lut, 1, null, commands,
                n_commands, mb);
        }
        else
        {
            BrotliBuildMetaBlockGreedyInternal(m, arena, ringbuffer, pos, mask,
                prev_byte, prev_byte2, literal_context_lut, num_contexts,
                static_context_map, commands, n_commands, mb);
        }
        BROTLI_FREE(m, ref arena);
    }

    internal static void BrotliOptimizeHistograms(uint num_distance_codes,
                                                  MetaBlockSplit* mb)
    {
        byte* good_for_rle = stackalloc byte[BROTLI_NUM_COMMAND_SYMBOLS];
        nuint i;
        for (i = 0; i < mb->literal_histograms_size; ++i)
        {
            BrotliOptimizeHuffmanCountsForRle(256, mb->literal_histograms[i].data_,
                                              good_for_rle);
        }
        for (i = 0; i < mb->command_histograms_size; ++i)
        {
            BrotliOptimizeHuffmanCountsForRle(BROTLI_NUM_COMMAND_SYMBOLS,
                                              mb->command_histograms[i].data_,
                                              good_for_rle);
        }
        for (i = 0; i < mb->distance_histograms_size; ++i)
        {
            BrotliOptimizeHuffmanCountsForRle(num_distance_codes,
                                              mb->distance_histograms[i].data_,
                                              good_for_rle);
        }
    }
}
