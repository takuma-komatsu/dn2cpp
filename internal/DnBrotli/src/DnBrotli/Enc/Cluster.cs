// Port of c/enc/cluster.{h,c} + cluster_inc.h (brotli v1.1.0) — functions for
// clustering similar histograms together. The cluster_inc.h template is
// expanded manually for Literal / Command / Distance (the Histogram.cs house
// triple pattern). Every double expression keeps the exact C shape
// (PORTING.md); BROTLI_MAX(double, ...) is ported as the C macro's ternary.

using System.Runtime.CompilerServices;

using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.BitCost;
using static DnBrotli.Enc.FastLog;
using static DnBrotli.Enc.Histogram;
using static DnBrotli.Enc.MemoryManager;

namespace DnBrotli.Enc;

/// <summary><c>struct HistogramPair</c>.</summary>
internal struct HistogramPair
{
    public uint idx1;
    public uint idx2;
    public double cost_combo;
    public double cost_diff;
}

internal static unsafe class Cluster
{
    /// <summary><c>HistogramPairIsLess</c> (BROTLI_BOOL -> int 0/1).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int HistogramPairIsLess(HistogramPair* p1, HistogramPair* p2)
    {
        if (p1->cost_diff != p2->cost_diff)
        {
            return (p1->cost_diff > p2->cost_diff) ? 1 : 0;
        }
        return ((p1->idx2 - p1->idx1) > (p2->idx2 - p2->idx1)) ? 1 : 0;
    }

    /* Returns entropy reduction of the context map when we combine two clusters. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ClusterCostDiff(nuint size_a, nuint size_b)
    {
        nuint size_c = size_a + size_b;
        return (double)size_a * FastLog2(size_a) +
            (double)size_b * FastLog2(size_b) -
            (double)size_c * FastLog2(size_c);
    }

    /* ==================== FN = Literal ==================== */

    /* Computes the bit cost reduction by combining out[idx1] and out[idx2] and if
       it is below a threshold, stores the pair (idx1, idx2) in the *pairs queue. */
    internal static void BrotliCompareAndPushToQueueLiteral(
        HistogramLiteral* @out, HistogramLiteral* tmp, uint* cluster_size,
        uint idx1, uint idx2, nuint max_num_pairs, HistogramPair* pairs,
        nuint* num_pairs)
    {
        int is_good_pair = 0;  /* BROTLI_FALSE */
        HistogramPair p;
        p.idx1 = p.idx2 = 0;
        p.cost_diff = p.cost_combo = 0;
        if (idx1 == idx2)
        {
            return;
        }
        if (idx2 < idx1)
        {
            uint t = idx2;
            idx2 = idx1;
            idx1 = t;
        }
        p.idx1 = idx1;
        p.idx2 = idx2;
        p.cost_diff = 0.5 * ClusterCostDiff(cluster_size[idx1], cluster_size[idx2]);
        p.cost_diff -= @out[idx1].bit_cost_;
        p.cost_diff -= @out[idx2].bit_cost_;

        if (@out[idx1].total_count_ == 0)
        {
            p.cost_combo = @out[idx2].bit_cost_;
            is_good_pair = 1;  /* BROTLI_TRUE */
        }
        else if (@out[idx2].total_count_ == 0)
        {
            p.cost_combo = @out[idx1].bit_cost_;
            is_good_pair = 1;  /* BROTLI_TRUE */
        }
        else
        {
            double threshold = *num_pairs == 0 ? 1e99 :
                (0.0 > pairs[0].cost_diff ? 0.0 : pairs[0].cost_diff);  /* BROTLI_MAX(double, 0.0, ...) */
            double cost_combo;
            *tmp = @out[idx1];
            HistogramAddHistogramLiteral(tmp, &@out[idx2]);
            cost_combo = BrotliPopulationCostLiteral(tmp);
            if (cost_combo < threshold - p.cost_diff)
            {
                p.cost_combo = cost_combo;
                is_good_pair = 1;  /* BROTLI_TRUE */
            }
        }
        if (is_good_pair != 0)
        {
            p.cost_diff += p.cost_combo;
            if (*num_pairs > 0 && HistogramPairIsLess(&pairs[0], &p) != 0)
            {
                /* Replace the top of the queue if needed. */
                if (*num_pairs < max_num_pairs)
                {
                    pairs[*num_pairs] = pairs[0];
                    ++(*num_pairs);
                }
                pairs[0] = p;
            }
            else if (*num_pairs < max_num_pairs)
            {
                pairs[*num_pairs] = p;
                ++(*num_pairs);
            }
        }
    }

    internal static nuint BrotliHistogramCombineLiteral(HistogramLiteral* @out,
                                                        HistogramLiteral* tmp,
                                                        uint* cluster_size,
                                                        uint* symbols,
                                                        uint* clusters,
                                                        HistogramPair* pairs,
                                                        nuint num_clusters,
                                                        nuint symbols_size,
                                                        nuint max_clusters,
                                                        nuint max_num_pairs)
    {
        double cost_diff_threshold = 0.0;
        nuint min_cluster_size = 1;
        nuint num_pairs = 0;

        {
            /* We maintain a vector of histogram pairs, with the property that the pair
               with the maximum bit cost reduction is the first. */
            nuint idx1;
            for (idx1 = 0; idx1 < num_clusters; ++idx1)
            {
                nuint idx2;
                for (idx2 = idx1 + 1; idx2 < num_clusters; ++idx2)
                {
                    BrotliCompareAndPushToQueueLiteral(@out, tmp, cluster_size, clusters[idx1],
                        clusters[idx2], max_num_pairs, &pairs[0], &num_pairs);
                }
            }
        }

        while (num_clusters > min_cluster_size)
        {
            uint best_idx1;
            uint best_idx2;
            nuint i;
            if (pairs[0].cost_diff >= cost_diff_threshold)
            {
                cost_diff_threshold = 1e99;
                min_cluster_size = max_clusters;
                continue;
            }
            /* Take the best pair from the top of heap. */
            best_idx1 = pairs[0].idx1;
            best_idx2 = pairs[0].idx2;
            HistogramAddHistogramLiteral(&@out[best_idx1], &@out[best_idx2]);
            @out[best_idx1].bit_cost_ = pairs[0].cost_combo;
            cluster_size[best_idx1] += cluster_size[best_idx2];
            for (i = 0; i < symbols_size; ++i)
            {
                if (symbols[i] == best_idx2)
                {
                    symbols[i] = best_idx1;
                }
            }
            for (i = 0; i < num_clusters; ++i)
            {
                if (clusters[i] == best_idx2)
                {
                    /* memmove(&clusters[i], &clusters[i + 1], ...) */
                    Buffer.MemoryCopy(&clusters[i + 1], &clusters[i],
                        (num_clusters - i - 1) * (nuint)sizeof(uint),
                        (num_clusters - i - 1) * (nuint)sizeof(uint));
                    break;
                }
            }
            --num_clusters;
            {
                /* Remove pairs intersecting the just combined best pair. */
                nuint copy_to_idx = 0;
                for (i = 0; i < num_pairs; ++i)
                {
                    HistogramPair* p = &pairs[i];
                    if (p->idx1 == best_idx1 || p->idx2 == best_idx1 ||
                        p->idx1 == best_idx2 || p->idx2 == best_idx2)
                    {
                        /* Remove invalid pair from the queue. */
                        continue;
                    }
                    if (HistogramPairIsLess(&pairs[0], p) != 0)
                    {
                        /* Replace the top of the queue if needed. */
                        HistogramPair front = pairs[0];
                        pairs[0] = *p;
                        pairs[copy_to_idx] = front;
                    }
                    else
                    {
                        pairs[copy_to_idx] = *p;
                    }
                    ++copy_to_idx;
                }
                num_pairs = copy_to_idx;
            }

            /* Push new pairs formed with the combined histogram to the heap. */
            for (i = 0; i < num_clusters; ++i)
            {
                BrotliCompareAndPushToQueueLiteral(@out, tmp, cluster_size, best_idx1,
                    clusters[i], max_num_pairs, &pairs[0], &num_pairs);
            }
        }
        return num_clusters;
    }

    /* What is the bit cost of moving histogram from cur_symbol to candidate. */
    internal static double BrotliHistogramBitCostDistanceLiteral(
        HistogramLiteral* histogram, HistogramLiteral* candidate,
        HistogramLiteral* tmp)
    {
        if (histogram->total_count_ == 0)
        {
            return 0.0;
        }
        else
        {
            *tmp = *histogram;
            HistogramAddHistogramLiteral(tmp, candidate);
            return BrotliPopulationCostLiteral(tmp) - candidate->bit_cost_;
        }
    }

    /* Find the best 'out' histogram for each of the 'in' histograms.
       When called, clusters[0..num_clusters) contains the unique values from
       symbols[0..in_size), but this property is not preserved in this function.
       Note: we assume that out[]->bit_cost_ is already up-to-date. */
    internal static void BrotliHistogramRemapLiteral(HistogramLiteral* @in,
        nuint in_size, uint* clusters, nuint num_clusters,
        HistogramLiteral* @out, HistogramLiteral* tmp, uint* symbols)
    {
        nuint i;
        for (i = 0; i < in_size; ++i)
        {
            uint best_out = i == 0 ? symbols[0] : symbols[i - 1];
            double best_bits =
                BrotliHistogramBitCostDistanceLiteral(&@in[i], &@out[best_out], tmp);
            nuint j;
            for (j = 0; j < num_clusters; ++j)
            {
                double cur_bits =
                    BrotliHistogramBitCostDistanceLiteral(&@in[i], &@out[clusters[j]], tmp);
                if (cur_bits < best_bits)
                {
                    best_bits = cur_bits;
                    best_out = clusters[j];
                }
            }
            symbols[i] = best_out;
        }

        /* Recompute each out based on raw and symbols. */
        for (i = 0; i < num_clusters; ++i)
        {
            HistogramClearLiteral(&@out[clusters[i]]);
        }
        for (i = 0; i < in_size; ++i)
        {
            HistogramAddHistogramLiteral(&@out[symbols[i]], &@in[i]);
        }
    }

    /* Reorders elements of the out[0..length) array and changes values in
       symbols[0..length) array in the following way:
         * when called, symbols[] contains indexes into out[], and has N unique
           values (possibly N < length)
         * on return, symbols'[i] = f(symbols[i]) and
                      out'[symbols'[i]] = out[symbols[i]], for each 0 <= i < length,
           where f is a bijection between the range of symbols[] and [0..N), and
           the first occurrences of values in symbols'[i] come in consecutive
           increasing order.
       Returns N, the number of unique values in symbols[]. */
    internal static nuint BrotliHistogramReindexLiteral(MemoryManager* m,
        HistogramLiteral* @out, uint* symbols, nuint length)
    {
        const uint kInvalidIndex = uint.MaxValue;  /* BROTLI_UINT32_MAX */
        uint* new_index = BROTLI_ALLOC<uint>(m, length);
        uint next_index;
        HistogramLiteral* tmp;
        nuint i;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(new_index)) return 0;
        for (i = 0; i < length; ++i)
        {
            new_index[i] = kInvalidIndex;
        }
        next_index = 0;
        for (i = 0; i < length; ++i)
        {
            if (new_index[symbols[i]] == kInvalidIndex)
            {
                new_index[symbols[i]] = next_index;
                ++next_index;
            }
        }
        /* TODO(eustas): by using idea of "cycle-sort" we can avoid allocation of
           tmp and reduce the number of copying by the factor of 2. */
        tmp = BROTLI_ALLOC<HistogramLiteral>(m, next_index);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(tmp)) return 0;
        next_index = 0;
        for (i = 0; i < length; ++i)
        {
            if (new_index[symbols[i]] == next_index)
            {
                tmp[next_index] = @out[symbols[i]];
                ++next_index;
            }
            symbols[i] = new_index[symbols[i]];
        }
        BROTLI_FREE(m, ref new_index);
        for (i = 0; i < next_index; ++i)
        {
            @out[i] = tmp[i];
        }
        BROTLI_FREE(m, ref tmp);
        return next_index;
    }

    internal static void BrotliClusterHistogramsLiteral(
        MemoryManager* m, HistogramLiteral* @in, nuint in_size,
        nuint max_histograms, HistogramLiteral* @out, nuint* out_size,
        uint* histogram_symbols)
    {
        uint* cluster_size = BROTLI_ALLOC<uint>(m, in_size);
        uint* clusters = BROTLI_ALLOC<uint>(m, in_size);
        nuint num_clusters = 0;
        const nuint max_input_histograms = 64;
        nuint pairs_capacity = max_input_histograms * max_input_histograms / 2;
        /* For the first pass of clustering, we allow all pairs. */
        HistogramPair* pairs = BROTLI_ALLOC<HistogramPair>(m, pairs_capacity + 1);
        /* TODO(eustas): move to "persistent" arena? */
        HistogramLiteral* tmp = BROTLI_ALLOC<HistogramLiteral>(m, 1);
        nuint i;

        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(cluster_size) ||
            BROTLI_IS_NULL(clusters) || BROTLI_IS_NULL(pairs) || BROTLI_IS_NULL(tmp))
        {
            return;
        }

        for (i = 0; i < in_size; ++i)
        {
            cluster_size[i] = 1;
        }

        for (i = 0; i < in_size; ++i)
        {
            @out[i] = @in[i];
            @out[i].bit_cost_ = BrotliPopulationCostLiteral(&@in[i]);
            histogram_symbols[i] = (uint)i;
        }

        for (i = 0; i < in_size; i += max_input_histograms)
        {
            nuint num_to_combine =
                BROTLI_MIN(in_size - i, max_input_histograms);  /* BROTLI_MIN(size_t, ...) */
            nuint num_new_clusters;
            nuint j;
            for (j = 0; j < num_to_combine; ++j)
            {
                clusters[num_clusters + j] = (uint)(i + j);
            }
            num_new_clusters =
                BrotliHistogramCombineLiteral(@out, tmp, cluster_size,
                                              &histogram_symbols[i],
                                              &clusters[num_clusters], pairs,
                                              num_to_combine, num_to_combine,
                                              max_histograms, pairs_capacity);
            num_clusters += num_new_clusters;
        }

        {
            /* For the second pass, we limit the total number of histogram pairs.
               After this limit is reached, we only keep searching for the best pair. */
            nuint max_num_pairs = BROTLI_MIN(
                64 * num_clusters, (num_clusters / 2) * num_clusters);  /* BROTLI_MIN(size_t, ...) */
            BROTLI_ENSURE_CAPACITY(m, ref pairs, ref pairs_capacity, max_num_pairs + 1);
            if (BROTLI_IS_OOM(m)) return;

            /* Collapse similar histograms. */
            num_clusters = BrotliHistogramCombineLiteral(@out, tmp, cluster_size,
                                                         histogram_symbols, clusters,
                                                         pairs, num_clusters, in_size,
                                                         max_histograms, max_num_pairs);
        }
        BROTLI_FREE(m, ref pairs);
        BROTLI_FREE(m, ref cluster_size);
        /* Find the optimal map from original histograms to the final ones. */
        BrotliHistogramRemapLiteral(@in, in_size, clusters, num_clusters,
                                    @out, tmp, histogram_symbols);
        BROTLI_FREE(m, ref tmp);
        BROTLI_FREE(m, ref clusters);
        /* Convert the context map to a canonical form. */
        *out_size = BrotliHistogramReindexLiteral(m, @out, histogram_symbols, in_size);
        if (BROTLI_IS_OOM(m)) return;
    }

    /* ==================== FN = Command ==================== */

    /* Computes the bit cost reduction by combining out[idx1] and out[idx2] and if
       it is below a threshold, stores the pair (idx1, idx2) in the *pairs queue. */
    internal static void BrotliCompareAndPushToQueueCommand(
        HistogramCommand* @out, HistogramCommand* tmp, uint* cluster_size,
        uint idx1, uint idx2, nuint max_num_pairs, HistogramPair* pairs,
        nuint* num_pairs)
    {
        int is_good_pair = 0;  /* BROTLI_FALSE */
        HistogramPair p;
        p.idx1 = p.idx2 = 0;
        p.cost_diff = p.cost_combo = 0;
        if (idx1 == idx2)
        {
            return;
        }
        if (idx2 < idx1)
        {
            uint t = idx2;
            idx2 = idx1;
            idx1 = t;
        }
        p.idx1 = idx1;
        p.idx2 = idx2;
        p.cost_diff = 0.5 * ClusterCostDiff(cluster_size[idx1], cluster_size[idx2]);
        p.cost_diff -= @out[idx1].bit_cost_;
        p.cost_diff -= @out[idx2].bit_cost_;

        if (@out[idx1].total_count_ == 0)
        {
            p.cost_combo = @out[idx2].bit_cost_;
            is_good_pair = 1;  /* BROTLI_TRUE */
        }
        else if (@out[idx2].total_count_ == 0)
        {
            p.cost_combo = @out[idx1].bit_cost_;
            is_good_pair = 1;  /* BROTLI_TRUE */
        }
        else
        {
            double threshold = *num_pairs == 0 ? 1e99 :
                (0.0 > pairs[0].cost_diff ? 0.0 : pairs[0].cost_diff);  /* BROTLI_MAX(double, 0.0, ...) */
            double cost_combo;
            *tmp = @out[idx1];
            HistogramAddHistogramCommand(tmp, &@out[idx2]);
            cost_combo = BrotliPopulationCostCommand(tmp);
            if (cost_combo < threshold - p.cost_diff)
            {
                p.cost_combo = cost_combo;
                is_good_pair = 1;  /* BROTLI_TRUE */
            }
        }
        if (is_good_pair != 0)
        {
            p.cost_diff += p.cost_combo;
            if (*num_pairs > 0 && HistogramPairIsLess(&pairs[0], &p) != 0)
            {
                /* Replace the top of the queue if needed. */
                if (*num_pairs < max_num_pairs)
                {
                    pairs[*num_pairs] = pairs[0];
                    ++(*num_pairs);
                }
                pairs[0] = p;
            }
            else if (*num_pairs < max_num_pairs)
            {
                pairs[*num_pairs] = p;
                ++(*num_pairs);
            }
        }
    }

    internal static nuint BrotliHistogramCombineCommand(HistogramCommand* @out,
                                                        HistogramCommand* tmp,
                                                        uint* cluster_size,
                                                        uint* symbols,
                                                        uint* clusters,
                                                        HistogramPair* pairs,
                                                        nuint num_clusters,
                                                        nuint symbols_size,
                                                        nuint max_clusters,
                                                        nuint max_num_pairs)
    {
        double cost_diff_threshold = 0.0;
        nuint min_cluster_size = 1;
        nuint num_pairs = 0;

        {
            /* We maintain a vector of histogram pairs, with the property that the pair
               with the maximum bit cost reduction is the first. */
            nuint idx1;
            for (idx1 = 0; idx1 < num_clusters; ++idx1)
            {
                nuint idx2;
                for (idx2 = idx1 + 1; idx2 < num_clusters; ++idx2)
                {
                    BrotliCompareAndPushToQueueCommand(@out, tmp, cluster_size, clusters[idx1],
                        clusters[idx2], max_num_pairs, &pairs[0], &num_pairs);
                }
            }
        }

        while (num_clusters > min_cluster_size)
        {
            uint best_idx1;
            uint best_idx2;
            nuint i;
            if (pairs[0].cost_diff >= cost_diff_threshold)
            {
                cost_diff_threshold = 1e99;
                min_cluster_size = max_clusters;
                continue;
            }
            /* Take the best pair from the top of heap. */
            best_idx1 = pairs[0].idx1;
            best_idx2 = pairs[0].idx2;
            HistogramAddHistogramCommand(&@out[best_idx1], &@out[best_idx2]);
            @out[best_idx1].bit_cost_ = pairs[0].cost_combo;
            cluster_size[best_idx1] += cluster_size[best_idx2];
            for (i = 0; i < symbols_size; ++i)
            {
                if (symbols[i] == best_idx2)
                {
                    symbols[i] = best_idx1;
                }
            }
            for (i = 0; i < num_clusters; ++i)
            {
                if (clusters[i] == best_idx2)
                {
                    /* memmove(&clusters[i], &clusters[i + 1], ...) */
                    Buffer.MemoryCopy(&clusters[i + 1], &clusters[i],
                        (num_clusters - i - 1) * (nuint)sizeof(uint),
                        (num_clusters - i - 1) * (nuint)sizeof(uint));
                    break;
                }
            }
            --num_clusters;
            {
                /* Remove pairs intersecting the just combined best pair. */
                nuint copy_to_idx = 0;
                for (i = 0; i < num_pairs; ++i)
                {
                    HistogramPair* p = &pairs[i];
                    if (p->idx1 == best_idx1 || p->idx2 == best_idx1 ||
                        p->idx1 == best_idx2 || p->idx2 == best_idx2)
                    {
                        /* Remove invalid pair from the queue. */
                        continue;
                    }
                    if (HistogramPairIsLess(&pairs[0], p) != 0)
                    {
                        /* Replace the top of the queue if needed. */
                        HistogramPair front = pairs[0];
                        pairs[0] = *p;
                        pairs[copy_to_idx] = front;
                    }
                    else
                    {
                        pairs[copy_to_idx] = *p;
                    }
                    ++copy_to_idx;
                }
                num_pairs = copy_to_idx;
            }

            /* Push new pairs formed with the combined histogram to the heap. */
            for (i = 0; i < num_clusters; ++i)
            {
                BrotliCompareAndPushToQueueCommand(@out, tmp, cluster_size, best_idx1,
                    clusters[i], max_num_pairs, &pairs[0], &num_pairs);
            }
        }
        return num_clusters;
    }

    /* What is the bit cost of moving histogram from cur_symbol to candidate. */
    internal static double BrotliHistogramBitCostDistanceCommand(
        HistogramCommand* histogram, HistogramCommand* candidate,
        HistogramCommand* tmp)
    {
        if (histogram->total_count_ == 0)
        {
            return 0.0;
        }
        else
        {
            *tmp = *histogram;
            HistogramAddHistogramCommand(tmp, candidate);
            return BrotliPopulationCostCommand(tmp) - candidate->bit_cost_;
        }
    }

    /* Find the best 'out' histogram for each of the 'in' histograms.
       When called, clusters[0..num_clusters) contains the unique values from
       symbols[0..in_size), but this property is not preserved in this function.
       Note: we assume that out[]->bit_cost_ is already up-to-date. */
    internal static void BrotliHistogramRemapCommand(HistogramCommand* @in,
        nuint in_size, uint* clusters, nuint num_clusters,
        HistogramCommand* @out, HistogramCommand* tmp, uint* symbols)
    {
        nuint i;
        for (i = 0; i < in_size; ++i)
        {
            uint best_out = i == 0 ? symbols[0] : symbols[i - 1];
            double best_bits =
                BrotliHistogramBitCostDistanceCommand(&@in[i], &@out[best_out], tmp);
            nuint j;
            for (j = 0; j < num_clusters; ++j)
            {
                double cur_bits =
                    BrotliHistogramBitCostDistanceCommand(&@in[i], &@out[clusters[j]], tmp);
                if (cur_bits < best_bits)
                {
                    best_bits = cur_bits;
                    best_out = clusters[j];
                }
            }
            symbols[i] = best_out;
        }

        /* Recompute each out based on raw and symbols. */
        for (i = 0; i < num_clusters; ++i)
        {
            HistogramClearCommand(&@out[clusters[i]]);
        }
        for (i = 0; i < in_size; ++i)
        {
            HistogramAddHistogramCommand(&@out[symbols[i]], &@in[i]);
        }
    }

    /* Reorders elements of the out[0..length) array and changes values in
       symbols[0..length) array; see the Literal instantiation for the contract.
       Returns N, the number of unique values in symbols[]. */
    internal static nuint BrotliHistogramReindexCommand(MemoryManager* m,
        HistogramCommand* @out, uint* symbols, nuint length)
    {
        const uint kInvalidIndex = uint.MaxValue;  /* BROTLI_UINT32_MAX */
        uint* new_index = BROTLI_ALLOC<uint>(m, length);
        uint next_index;
        HistogramCommand* tmp;
        nuint i;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(new_index)) return 0;
        for (i = 0; i < length; ++i)
        {
            new_index[i] = kInvalidIndex;
        }
        next_index = 0;
        for (i = 0; i < length; ++i)
        {
            if (new_index[symbols[i]] == kInvalidIndex)
            {
                new_index[symbols[i]] = next_index;
                ++next_index;
            }
        }
        /* TODO(eustas): by using idea of "cycle-sort" we can avoid allocation of
           tmp and reduce the number of copying by the factor of 2. */
        tmp = BROTLI_ALLOC<HistogramCommand>(m, next_index);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(tmp)) return 0;
        next_index = 0;
        for (i = 0; i < length; ++i)
        {
            if (new_index[symbols[i]] == next_index)
            {
                tmp[next_index] = @out[symbols[i]];
                ++next_index;
            }
            symbols[i] = new_index[symbols[i]];
        }
        BROTLI_FREE(m, ref new_index);
        for (i = 0; i < next_index; ++i)
        {
            @out[i] = tmp[i];
        }
        BROTLI_FREE(m, ref tmp);
        return next_index;
    }

    internal static void BrotliClusterHistogramsCommand(
        MemoryManager* m, HistogramCommand* @in, nuint in_size,
        nuint max_histograms, HistogramCommand* @out, nuint* out_size,
        uint* histogram_symbols)
    {
        uint* cluster_size = BROTLI_ALLOC<uint>(m, in_size);
        uint* clusters = BROTLI_ALLOC<uint>(m, in_size);
        nuint num_clusters = 0;
        const nuint max_input_histograms = 64;
        nuint pairs_capacity = max_input_histograms * max_input_histograms / 2;
        /* For the first pass of clustering, we allow all pairs. */
        HistogramPair* pairs = BROTLI_ALLOC<HistogramPair>(m, pairs_capacity + 1);
        /* TODO(eustas): move to "persistent" arena? */
        HistogramCommand* tmp = BROTLI_ALLOC<HistogramCommand>(m, 1);
        nuint i;

        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(cluster_size) ||
            BROTLI_IS_NULL(clusters) || BROTLI_IS_NULL(pairs) || BROTLI_IS_NULL(tmp))
        {
            return;
        }

        for (i = 0; i < in_size; ++i)
        {
            cluster_size[i] = 1;
        }

        for (i = 0; i < in_size; ++i)
        {
            @out[i] = @in[i];
            @out[i].bit_cost_ = BrotliPopulationCostCommand(&@in[i]);
            histogram_symbols[i] = (uint)i;
        }

        for (i = 0; i < in_size; i += max_input_histograms)
        {
            nuint num_to_combine =
                BROTLI_MIN(in_size - i, max_input_histograms);  /* BROTLI_MIN(size_t, ...) */
            nuint num_new_clusters;
            nuint j;
            for (j = 0; j < num_to_combine; ++j)
            {
                clusters[num_clusters + j] = (uint)(i + j);
            }
            num_new_clusters =
                BrotliHistogramCombineCommand(@out, tmp, cluster_size,
                                              &histogram_symbols[i],
                                              &clusters[num_clusters], pairs,
                                              num_to_combine, num_to_combine,
                                              max_histograms, pairs_capacity);
            num_clusters += num_new_clusters;
        }

        {
            /* For the second pass, we limit the total number of histogram pairs.
               After this limit is reached, we only keep searching for the best pair. */
            nuint max_num_pairs = BROTLI_MIN(
                64 * num_clusters, (num_clusters / 2) * num_clusters);  /* BROTLI_MIN(size_t, ...) */
            BROTLI_ENSURE_CAPACITY(m, ref pairs, ref pairs_capacity, max_num_pairs + 1);
            if (BROTLI_IS_OOM(m)) return;

            /* Collapse similar histograms. */
            num_clusters = BrotliHistogramCombineCommand(@out, tmp, cluster_size,
                                                         histogram_symbols, clusters,
                                                         pairs, num_clusters, in_size,
                                                         max_histograms, max_num_pairs);
        }
        BROTLI_FREE(m, ref pairs);
        BROTLI_FREE(m, ref cluster_size);
        /* Find the optimal map from original histograms to the final ones. */
        BrotliHistogramRemapCommand(@in, in_size, clusters, num_clusters,
                                    @out, tmp, histogram_symbols);
        BROTLI_FREE(m, ref tmp);
        BROTLI_FREE(m, ref clusters);
        /* Convert the context map to a canonical form. */
        *out_size = BrotliHistogramReindexCommand(m, @out, histogram_symbols, in_size);
        if (BROTLI_IS_OOM(m)) return;
    }

    /* ==================== FN = Distance ==================== */

    /* Computes the bit cost reduction by combining out[idx1] and out[idx2] and if
       it is below a threshold, stores the pair (idx1, idx2) in the *pairs queue. */
    internal static void BrotliCompareAndPushToQueueDistance(
        HistogramDistance* @out, HistogramDistance* tmp, uint* cluster_size,
        uint idx1, uint idx2, nuint max_num_pairs, HistogramPair* pairs,
        nuint* num_pairs)
    {
        int is_good_pair = 0;  /* BROTLI_FALSE */
        HistogramPair p;
        p.idx1 = p.idx2 = 0;
        p.cost_diff = p.cost_combo = 0;
        if (idx1 == idx2)
        {
            return;
        }
        if (idx2 < idx1)
        {
            uint t = idx2;
            idx2 = idx1;
            idx1 = t;
        }
        p.idx1 = idx1;
        p.idx2 = idx2;
        p.cost_diff = 0.5 * ClusterCostDiff(cluster_size[idx1], cluster_size[idx2]);
        p.cost_diff -= @out[idx1].bit_cost_;
        p.cost_diff -= @out[idx2].bit_cost_;

        if (@out[idx1].total_count_ == 0)
        {
            p.cost_combo = @out[idx2].bit_cost_;
            is_good_pair = 1;  /* BROTLI_TRUE */
        }
        else if (@out[idx2].total_count_ == 0)
        {
            p.cost_combo = @out[idx1].bit_cost_;
            is_good_pair = 1;  /* BROTLI_TRUE */
        }
        else
        {
            double threshold = *num_pairs == 0 ? 1e99 :
                (0.0 > pairs[0].cost_diff ? 0.0 : pairs[0].cost_diff);  /* BROTLI_MAX(double, 0.0, ...) */
            double cost_combo;
            *tmp = @out[idx1];
            HistogramAddHistogramDistance(tmp, &@out[idx2]);
            cost_combo = BrotliPopulationCostDistance(tmp);
            if (cost_combo < threshold - p.cost_diff)
            {
                p.cost_combo = cost_combo;
                is_good_pair = 1;  /* BROTLI_TRUE */
            }
        }
        if (is_good_pair != 0)
        {
            p.cost_diff += p.cost_combo;
            if (*num_pairs > 0 && HistogramPairIsLess(&pairs[0], &p) != 0)
            {
                /* Replace the top of the queue if needed. */
                if (*num_pairs < max_num_pairs)
                {
                    pairs[*num_pairs] = pairs[0];
                    ++(*num_pairs);
                }
                pairs[0] = p;
            }
            else if (*num_pairs < max_num_pairs)
            {
                pairs[*num_pairs] = p;
                ++(*num_pairs);
            }
        }
    }

    internal static nuint BrotliHistogramCombineDistance(HistogramDistance* @out,
                                                         HistogramDistance* tmp,
                                                         uint* cluster_size,
                                                         uint* symbols,
                                                         uint* clusters,
                                                         HistogramPair* pairs,
                                                         nuint num_clusters,
                                                         nuint symbols_size,
                                                         nuint max_clusters,
                                                         nuint max_num_pairs)
    {
        double cost_diff_threshold = 0.0;
        nuint min_cluster_size = 1;
        nuint num_pairs = 0;

        {
            /* We maintain a vector of histogram pairs, with the property that the pair
               with the maximum bit cost reduction is the first. */
            nuint idx1;
            for (idx1 = 0; idx1 < num_clusters; ++idx1)
            {
                nuint idx2;
                for (idx2 = idx1 + 1; idx2 < num_clusters; ++idx2)
                {
                    BrotliCompareAndPushToQueueDistance(@out, tmp, cluster_size, clusters[idx1],
                        clusters[idx2], max_num_pairs, &pairs[0], &num_pairs);
                }
            }
        }

        while (num_clusters > min_cluster_size)
        {
            uint best_idx1;
            uint best_idx2;
            nuint i;
            if (pairs[0].cost_diff >= cost_diff_threshold)
            {
                cost_diff_threshold = 1e99;
                min_cluster_size = max_clusters;
                continue;
            }
            /* Take the best pair from the top of heap. */
            best_idx1 = pairs[0].idx1;
            best_idx2 = pairs[0].idx2;
            HistogramAddHistogramDistance(&@out[best_idx1], &@out[best_idx2]);
            @out[best_idx1].bit_cost_ = pairs[0].cost_combo;
            cluster_size[best_idx1] += cluster_size[best_idx2];
            for (i = 0; i < symbols_size; ++i)
            {
                if (symbols[i] == best_idx2)
                {
                    symbols[i] = best_idx1;
                }
            }
            for (i = 0; i < num_clusters; ++i)
            {
                if (clusters[i] == best_idx2)
                {
                    /* memmove(&clusters[i], &clusters[i + 1], ...) */
                    Buffer.MemoryCopy(&clusters[i + 1], &clusters[i],
                        (num_clusters - i - 1) * (nuint)sizeof(uint),
                        (num_clusters - i - 1) * (nuint)sizeof(uint));
                    break;
                }
            }
            --num_clusters;
            {
                /* Remove pairs intersecting the just combined best pair. */
                nuint copy_to_idx = 0;
                for (i = 0; i < num_pairs; ++i)
                {
                    HistogramPair* p = &pairs[i];
                    if (p->idx1 == best_idx1 || p->idx2 == best_idx1 ||
                        p->idx1 == best_idx2 || p->idx2 == best_idx2)
                    {
                        /* Remove invalid pair from the queue. */
                        continue;
                    }
                    if (HistogramPairIsLess(&pairs[0], p) != 0)
                    {
                        /* Replace the top of the queue if needed. */
                        HistogramPair front = pairs[0];
                        pairs[0] = *p;
                        pairs[copy_to_idx] = front;
                    }
                    else
                    {
                        pairs[copy_to_idx] = *p;
                    }
                    ++copy_to_idx;
                }
                num_pairs = copy_to_idx;
            }

            /* Push new pairs formed with the combined histogram to the heap. */
            for (i = 0; i < num_clusters; ++i)
            {
                BrotliCompareAndPushToQueueDistance(@out, tmp, cluster_size, best_idx1,
                    clusters[i], max_num_pairs, &pairs[0], &num_pairs);
            }
        }
        return num_clusters;
    }

    /* What is the bit cost of moving histogram from cur_symbol to candidate. */
    internal static double BrotliHistogramBitCostDistanceDistance(
        HistogramDistance* histogram, HistogramDistance* candidate,
        HistogramDistance* tmp)
    {
        if (histogram->total_count_ == 0)
        {
            return 0.0;
        }
        else
        {
            *tmp = *histogram;
            HistogramAddHistogramDistance(tmp, candidate);
            return BrotliPopulationCostDistance(tmp) - candidate->bit_cost_;
        }
    }

    /* Find the best 'out' histogram for each of the 'in' histograms.
       When called, clusters[0..num_clusters) contains the unique values from
       symbols[0..in_size), but this property is not preserved in this function.
       Note: we assume that out[]->bit_cost_ is already up-to-date. */
    internal static void BrotliHistogramRemapDistance(HistogramDistance* @in,
        nuint in_size, uint* clusters, nuint num_clusters,
        HistogramDistance* @out, HistogramDistance* tmp, uint* symbols)
    {
        nuint i;
        for (i = 0; i < in_size; ++i)
        {
            uint best_out = i == 0 ? symbols[0] : symbols[i - 1];
            double best_bits =
                BrotliHistogramBitCostDistanceDistance(&@in[i], &@out[best_out], tmp);
            nuint j;
            for (j = 0; j < num_clusters; ++j)
            {
                double cur_bits =
                    BrotliHistogramBitCostDistanceDistance(&@in[i], &@out[clusters[j]], tmp);
                if (cur_bits < best_bits)
                {
                    best_bits = cur_bits;
                    best_out = clusters[j];
                }
            }
            symbols[i] = best_out;
        }

        /* Recompute each out based on raw and symbols. */
        for (i = 0; i < num_clusters; ++i)
        {
            HistogramClearDistance(&@out[clusters[i]]);
        }
        for (i = 0; i < in_size; ++i)
        {
            HistogramAddHistogramDistance(&@out[symbols[i]], &@in[i]);
        }
    }

    /* Reorders elements of the out[0..length) array and changes values in
       symbols[0..length) array; see the Literal instantiation for the contract.
       Returns N, the number of unique values in symbols[]. */
    internal static nuint BrotliHistogramReindexDistance(MemoryManager* m,
        HistogramDistance* @out, uint* symbols, nuint length)
    {
        const uint kInvalidIndex = uint.MaxValue;  /* BROTLI_UINT32_MAX */
        uint* new_index = BROTLI_ALLOC<uint>(m, length);
        uint next_index;
        HistogramDistance* tmp;
        nuint i;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(new_index)) return 0;
        for (i = 0; i < length; ++i)
        {
            new_index[i] = kInvalidIndex;
        }
        next_index = 0;
        for (i = 0; i < length; ++i)
        {
            if (new_index[symbols[i]] == kInvalidIndex)
            {
                new_index[symbols[i]] = next_index;
                ++next_index;
            }
        }
        /* TODO(eustas): by using idea of "cycle-sort" we can avoid allocation of
           tmp and reduce the number of copying by the factor of 2. */
        tmp = BROTLI_ALLOC<HistogramDistance>(m, next_index);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(tmp)) return 0;
        next_index = 0;
        for (i = 0; i < length; ++i)
        {
            if (new_index[symbols[i]] == next_index)
            {
                tmp[next_index] = @out[symbols[i]];
                ++next_index;
            }
            symbols[i] = new_index[symbols[i]];
        }
        BROTLI_FREE(m, ref new_index);
        for (i = 0; i < next_index; ++i)
        {
            @out[i] = tmp[i];
        }
        BROTLI_FREE(m, ref tmp);
        return next_index;
    }

    internal static void BrotliClusterHistogramsDistance(
        MemoryManager* m, HistogramDistance* @in, nuint in_size,
        nuint max_histograms, HistogramDistance* @out, nuint* out_size,
        uint* histogram_symbols)
    {
        uint* cluster_size = BROTLI_ALLOC<uint>(m, in_size);
        uint* clusters = BROTLI_ALLOC<uint>(m, in_size);
        nuint num_clusters = 0;
        const nuint max_input_histograms = 64;
        nuint pairs_capacity = max_input_histograms * max_input_histograms / 2;
        /* For the first pass of clustering, we allow all pairs. */
        HistogramPair* pairs = BROTLI_ALLOC<HistogramPair>(m, pairs_capacity + 1);
        /* TODO(eustas): move to "persistent" arena? */
        HistogramDistance* tmp = BROTLI_ALLOC<HistogramDistance>(m, 1);
        nuint i;

        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(cluster_size) ||
            BROTLI_IS_NULL(clusters) || BROTLI_IS_NULL(pairs) || BROTLI_IS_NULL(tmp))
        {
            return;
        }

        for (i = 0; i < in_size; ++i)
        {
            cluster_size[i] = 1;
        }

        for (i = 0; i < in_size; ++i)
        {
            @out[i] = @in[i];
            @out[i].bit_cost_ = BrotliPopulationCostDistance(&@in[i]);
            histogram_symbols[i] = (uint)i;
        }

        for (i = 0; i < in_size; i += max_input_histograms)
        {
            nuint num_to_combine =
                BROTLI_MIN(in_size - i, max_input_histograms);  /* BROTLI_MIN(size_t, ...) */
            nuint num_new_clusters;
            nuint j;
            for (j = 0; j < num_to_combine; ++j)
            {
                clusters[num_clusters + j] = (uint)(i + j);
            }
            num_new_clusters =
                BrotliHistogramCombineDistance(@out, tmp, cluster_size,
                                               &histogram_symbols[i],
                                               &clusters[num_clusters], pairs,
                                               num_to_combine, num_to_combine,
                                               max_histograms, pairs_capacity);
            num_clusters += num_new_clusters;
        }

        {
            /* For the second pass, we limit the total number of histogram pairs.
               After this limit is reached, we only keep searching for the best pair. */
            nuint max_num_pairs = BROTLI_MIN(
                64 * num_clusters, (num_clusters / 2) * num_clusters);  /* BROTLI_MIN(size_t, ...) */
            BROTLI_ENSURE_CAPACITY(m, ref pairs, ref pairs_capacity, max_num_pairs + 1);
            if (BROTLI_IS_OOM(m)) return;

            /* Collapse similar histograms. */
            num_clusters = BrotliHistogramCombineDistance(@out, tmp, cluster_size,
                                                          histogram_symbols, clusters,
                                                          pairs, num_clusters, in_size,
                                                          max_histograms, max_num_pairs);
        }
        BROTLI_FREE(m, ref pairs);
        BROTLI_FREE(m, ref cluster_size);
        /* Find the optimal map from original histograms to the final ones. */
        BrotliHistogramRemapDistance(@in, in_size, clusters, num_clusters,
                                     @out, tmp, histogram_symbols);
        BROTLI_FREE(m, ref tmp);
        BROTLI_FREE(m, ref clusters);
        /* Convert the context map to a canonical form. */
        *out_size = BrotliHistogramReindexDistance(m, @out, histogram_symbols, in_size);
        if (BROTLI_IS_OOM(m)) return;
    }
}
