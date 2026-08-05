// Port of c/enc/histogram.{h,c} (brotli v1.1.0) — the complete histogram_inc.h
// template instantiations (Literal / Command / Distance, expanded manually per
// the house triple pattern) plus BrotliBuildHistogramsWithContext and its
// BlockSplitIterator helpers from histogram.c. BlockSplit itself lives in
// BlockSplitter.cs (c/enc/block_splitter.h).

using System.Runtime.CompilerServices;

using DnBrotli.Common;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Context;
using static DnBrotli.Enc.Command;

namespace DnBrotli.Enc;

/// <summary><c>HistogramLiteral</c> (histogram_inc.h, DATA_SIZE 256).</summary>
internal unsafe struct HistogramLiteral
{
    public fixed uint data_[BROTLI_NUM_LITERAL_SYMBOLS];
    public nuint total_count_;
    public double bit_cost_;
}

/// <summary><c>HistogramCommand</c> (histogram_inc.h, DATA_SIZE 704).</summary>
internal unsafe struct HistogramCommand
{
    public fixed uint data_[BROTLI_NUM_COMMAND_SYMBOLS];
    public nuint total_count_;
    public double bit_cost_;
}

/// <summary><c>HistogramDistance</c> (histogram_inc.h, DATA_SIZE 544).</summary>
internal unsafe struct HistogramDistance
{
    public fixed uint data_[Histogram.BROTLI_NUM_HISTOGRAM_DISTANCE_SYMBOLS];
    public nuint total_count_;
    public double bit_cost_;
}

internal static unsafe class Histogram
{
    /* The distance symbols effectively used by "Large Window Brotli" (32-bit). */
    internal const int BROTLI_NUM_HISTOGRAM_DISTANCE_SYMBOLS = 544;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramClearLiteral(HistogramLiteral* self)
    {
        new Span<uint>(self->data_, BROTLI_NUM_LITERAL_SYMBOLS).Clear();
        self->total_count_ = 0;
        self->bit_cost_ = double.PositiveInfinity;  /* HUGE_VAL */
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramAddLiteral(HistogramLiteral* self, nuint val)
    {
        ++self->data_[val];
        ++self->total_count_;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramAddVectorLiteral(HistogramLiteral* self, byte* p, nuint n)
    {
        self->total_count_ += n;
        n += 1;
        while (--n != 0) ++self->data_[*p++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramAddHistogramLiteral(HistogramLiteral* self, HistogramLiteral* v)
    {
        nuint i;
        self->total_count_ += v->total_count_;
        for (i = 0; i < BROTLI_NUM_LITERAL_SYMBOLS; ++i)
        {
            self->data_[i] += v->data_[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint HistogramDataSizeLiteral()
    {
        return BROTLI_NUM_LITERAL_SYMBOLS;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ClearHistogramsLiteral(HistogramLiteral* array, nuint length)
    {
        nuint i;
        for (i = 0; i < length; ++i) HistogramClearLiteral(array + i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramClearCommand(HistogramCommand* self)
    {
        new Span<uint>(self->data_, BROTLI_NUM_COMMAND_SYMBOLS).Clear();
        self->total_count_ = 0;
        self->bit_cost_ = double.PositiveInfinity;  /* HUGE_VAL */
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramAddCommand(HistogramCommand* self, nuint val)
    {
        ++self->data_[val];
        ++self->total_count_;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramAddVectorCommand(HistogramCommand* self, ushort* p, nuint n)
    {
        self->total_count_ += n;
        n += 1;
        while (--n != 0) ++self->data_[*p++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramAddHistogramCommand(HistogramCommand* self, HistogramCommand* v)
    {
        nuint i;
        self->total_count_ += v->total_count_;
        for (i = 0; i < BROTLI_NUM_COMMAND_SYMBOLS; ++i)
        {
            self->data_[i] += v->data_[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint HistogramDataSizeCommand()
    {
        return BROTLI_NUM_COMMAND_SYMBOLS;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ClearHistogramsCommand(HistogramCommand* array, nuint length)
    {
        nuint i;
        for (i = 0; i < length; ++i) HistogramClearCommand(array + i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramClearDistance(HistogramDistance* self)
    {
        new Span<uint>(self->data_, BROTLI_NUM_HISTOGRAM_DISTANCE_SYMBOLS).Clear();
        self->total_count_ = 0;
        self->bit_cost_ = double.PositiveInfinity;  /* HUGE_VAL */
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramAddDistance(HistogramDistance* self, nuint val)
    {
        ++self->data_[val];
        ++self->total_count_;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramAddVectorDistance(HistogramDistance* self, ushort* p, nuint n)
    {
        self->total_count_ += n;
        n += 1;
        while (--n != 0) ++self->data_[*p++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HistogramAddHistogramDistance(HistogramDistance* self, HistogramDistance* v)
    {
        nuint i;
        self->total_count_ += v->total_count_;
        for (i = 0; i < BROTLI_NUM_HISTOGRAM_DISTANCE_SYMBOLS; ++i)
        {
            self->data_[i] += v->data_[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint HistogramDataSizeDistance()
    {
        return BROTLI_NUM_HISTOGRAM_DISTANCE_SYMBOLS;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ClearHistogramsDistance(HistogramDistance* array, nuint length)
    {
        nuint i;
        for (i = 0; i < length; ++i) HistogramClearDistance(array + i);
    }

    /* ==================== histogram.c ==================== */

    private static void InitBlockSplitIterator(BlockSplitIterator* self,
        BlockSplit* split)
    {
        self->split_ = split;
        self->idx_ = 0;
        self->type_ = 0;
        self->length_ = split->lengths != null ? split->lengths[0] : 0;
    }

    private static void BlockSplitIteratorNext(BlockSplitIterator* self)
    {
        if (self->length_ == 0)
        {
            ++self->idx_;
            self->type_ = self->split_->types[self->idx_];
            self->length_ = self->split_->lengths[self->idx_];
        }
        --self->length_;
    }

    internal static void BrotliBuildHistogramsWithContext(
        Command* cmds, nuint num_commands,
        BlockSplit* literal_split, BlockSplit* insert_and_copy_split,
        BlockSplit* dist_split, byte* ringbuffer, nuint start_pos,
        nuint mask, byte prev_byte, byte prev_byte2,
        ContextType* context_modes, HistogramLiteral* literal_histograms,
        HistogramCommand* insert_and_copy_histograms,
        HistogramDistance* copy_dist_histograms)
    {
        nuint pos = start_pos;
        BlockSplitIterator literal_it;
        BlockSplitIterator insert_and_copy_it;
        BlockSplitIterator dist_it;
        nuint i;

        InitBlockSplitIterator(&literal_it, literal_split);
        InitBlockSplitIterator(&insert_and_copy_it, insert_and_copy_split);
        InitBlockSplitIterator(&dist_it, dist_split);
        for (i = 0; i < num_commands; ++i)
        {
            Command* cmd = &cmds[i];
            nuint j;
            BlockSplitIteratorNext(&insert_and_copy_it);
            HistogramAddCommand(&insert_and_copy_histograms[insert_and_copy_it.type_],
                cmd->cmd_prefix_);
            /* TODO(eustas): unwrap iterator blocks. */
            for (j = cmd->insert_len_; j != 0; --j)
            {
                nuint context;
                BlockSplitIteratorNext(&literal_it);
                context = literal_it.type_;
                if (context_modes != null)
                {
                    byte* lut = BROTLI_CONTEXT_LUT((nuint)context_modes[context]);
                    context = (context << BROTLI_LITERAL_CONTEXT_BITS) +
                        BROTLI_CONTEXT(prev_byte, prev_byte2, lut);
                }
                HistogramAddLiteral(&literal_histograms[context],
                    ringbuffer[pos & mask]);
                prev_byte2 = prev_byte;
                prev_byte = ringbuffer[pos & mask];
                ++pos;
            }
            pos += CommandCopyLen(cmd);
            if (CommandCopyLen(cmd) != 0)
            {
                prev_byte2 = ringbuffer[(pos - 2) & mask];
                prev_byte = ringbuffer[(pos - 1) & mask];
                if (cmd->cmd_prefix_ >= 128)
                {
                    nuint context;
                    BlockSplitIteratorNext(&dist_it);
                    context = (dist_it.type_ << BROTLI_DISTANCE_CONTEXT_BITS) +
                        CommandDistanceContext(cmd);
                    HistogramAddDistance(&copy_dist_histograms[context],
                        (nuint)(cmd->dist_prefix_ & 0x3FF));
                }
            }
        }
    }
}

/// <summary><c>struct BlockSplitIterator</c> (histogram.c).</summary>
internal unsafe struct BlockSplitIterator
{
    public BlockSplit* split_;  /* Not owned. */
    public nuint idx_;
    public nuint type_;
    public nuint length_;
}
