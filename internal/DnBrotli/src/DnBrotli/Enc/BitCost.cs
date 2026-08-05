// Port of c/enc/bit_cost.{h,c} + bit_cost_inc.h (brotli v1.1.0): the
// header-inline ShannonEntropy / BitsEntropy plus the FN(BrotliPopulationCost)
// template, expanded manually for Literal / Command / Distance (the
// Histogram.cs house triple pattern).
//
// FP-shape note: the C ShannonEntropy jumps into the middle of its two-way
// unrolled loop for odd sizes; the unrolling never changes the order in which
// elements 0..size-1 are accumulated, so the plain loop below performs the
// bit-identical sequence of double operations. Every double expression in the
// population-cost bodies keeps the exact C shape (PORTING.md).

using System.Runtime.CompilerServices;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.FastLog;
using static DnBrotli.Enc.Histogram;

namespace DnBrotli.Enc;

internal static unsafe class BitCost
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ShannonEntropy(uint* population, nuint size, nuint* total)
    {
        nuint sum = 0;
        double retval = 0;
        nuint i;
        nuint p;
        for (i = 0; i < size; ++i)
        {
            p = population[i];
            sum += p;
            retval -= (double)p * FastLog2(p);
        }
        if (sum != 0) retval += (double)sum * FastLog2(sum);
        *total = sum;
        return retval;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double BitsEntropy(uint* population, nuint size)
    {
        nuint sum;
        double retval = ShannonEntropy(population, size, &sum);
        if (retval < (double)sum)
        {
            /* At least one bit per literal is needed. */
            retval = (double)sum;
        }
        return retval;
    }

    /* ==================== bit_cost.c + bit_cost_inc.h ==================== */

    private const double kOneSymbolHistogramCost = 12;
    private const double kTwoSymbolHistogramCost = 20;
    private const double kThreeSymbolHistogramCost = 28;
    private const double kFourSymbolHistogramCost = 37;

    /// <summary><c>BrotliPopulationCostLiteral</c> (bit_cost_inc.h, FN = Literal).</summary>
    internal static double BrotliPopulationCostLiteral(HistogramLiteral* histogram)
    {
        nuint data_size = HistogramDataSizeLiteral();
        int count = 0;
        nuint* s = stackalloc nuint[5];
        double bits = 0.0;
        nuint i;
        if (histogram->total_count_ == 0)
        {
            return kOneSymbolHistogramCost;
        }
        for (i = 0; i < data_size; ++i)
        {
            if (histogram->data_[i] > 0)
            {
                s[count] = i;
                ++count;
                if (count > 4) break;
            }
        }
        if (count == 1)
        {
            return kOneSymbolHistogramCost;
        }
        if (count == 2)
        {
            return (kTwoSymbolHistogramCost + (double)histogram->total_count_);
        }
        if (count == 3)
        {
            uint histo0 = histogram->data_[s[0]];
            uint histo1 = histogram->data_[s[1]];
            uint histo2 = histogram->data_[s[2]];
            uint histomax = BROTLI_MAX(histo0, BROTLI_MAX(histo1, histo2));
            return (kThreeSymbolHistogramCost +
                    2u * (histo0 + histo1 + histo2) - histomax);
        }
        if (count == 4)
        {
            uint* histo = stackalloc uint[4];
            uint h23;
            uint histomax;
            for (i = 0; i < 4; ++i)
            {
                histo[i] = histogram->data_[s[i]];
            }
            /* Sort */
            for (i = 0; i < 4; ++i)
            {
                nuint j;
                for (j = i + 1; j < 4; ++j)
                {
                    if (histo[j] > histo[i])
                    {
                        (histo[j], histo[i]) = (histo[i], histo[j]);  /* BROTLI_SWAP */
                    }
                }
            }
            h23 = histo[2] + histo[3];
            histomax = BROTLI_MAX(h23, histo[0]);
            return (kFourSymbolHistogramCost +
                    3u * h23 + 2u * (histo[0] + histo[1]) - histomax);
        }

        {
            /* In this loop we compute the entropy of the histogram and simultaneously
               build a simplified histogram of the code length codes where we use the
               zero repeat code 17, but we don't use the non-zero repeat code 16. */
            nuint max_depth = 1;
            uint* depth_histo = stackalloc uint[BROTLI_CODE_LENGTH_CODES];  /* = { 0 } */
            new Span<uint>(depth_histo, BROTLI_CODE_LENGTH_CODES).Clear();
            double log2total = FastLog2(histogram->total_count_);
            for (i = 0; i < data_size;)
            {
                if (histogram->data_[i] > 0)
                {
                    /* Compute -log2(P(symbol)) = -log2(count(symbol)/total_count) =
                                                = log2(total_count) - log2(count(symbol)) */
                    double log2p = log2total - FastLog2(histogram->data_[i]);
                    /* Approximate the bit depth by round(-log2(P(symbol))) */
                    nuint depth = (nuint)(log2p + 0.5);
                    bits += histogram->data_[i] * log2p;
                    if (depth > 15)
                    {
                        depth = 15;
                    }
                    if (depth > max_depth)
                    {
                        max_depth = depth;
                    }
                    ++depth_histo[depth];
                    ++i;
                }
                else
                {
                    /* Compute the run length of zeros and add the appropriate number of 0
                       and 17 code length codes to the code length code histogram. */
                    uint reps = 1;
                    nuint k;
                    for (k = i + 1; k < data_size && histogram->data_[k] == 0; ++k)
                    {
                        ++reps;
                    }
                    i += reps;
                    if (i == data_size)
                    {
                        /* Don't add any cost for the last zero run, since these are encoded
                           only implicitly. */
                        break;
                    }
                    if (reps < 3)
                    {
                        depth_histo[0] += reps;
                    }
                    else
                    {
                        reps -= 2;
                        while (reps > 0)
                        {
                            ++depth_histo[BROTLI_REPEAT_ZERO_CODE_LENGTH];
                            /* Add the 3 extra bits for the 17 code length code. */
                            bits += 3;
                            reps >>= 3;
                        }
                    }
                }
            }
            /* Add the estimated encoding cost of the code length code histogram. */
            bits += (double)(18 + 2 * max_depth);
            /* Add the entropy of the code length code histogram. */
            bits += BitsEntropy(depth_histo, BROTLI_CODE_LENGTH_CODES);
        }
        return bits;
    }

    /// <summary><c>BrotliPopulationCostCommand</c> (bit_cost_inc.h, FN = Command).</summary>
    internal static double BrotliPopulationCostCommand(HistogramCommand* histogram)
    {
        nuint data_size = HistogramDataSizeCommand();
        int count = 0;
        nuint* s = stackalloc nuint[5];
        double bits = 0.0;
        nuint i;
        if (histogram->total_count_ == 0)
        {
            return kOneSymbolHistogramCost;
        }
        for (i = 0; i < data_size; ++i)
        {
            if (histogram->data_[i] > 0)
            {
                s[count] = i;
                ++count;
                if (count > 4) break;
            }
        }
        if (count == 1)
        {
            return kOneSymbolHistogramCost;
        }
        if (count == 2)
        {
            return (kTwoSymbolHistogramCost + (double)histogram->total_count_);
        }
        if (count == 3)
        {
            uint histo0 = histogram->data_[s[0]];
            uint histo1 = histogram->data_[s[1]];
            uint histo2 = histogram->data_[s[2]];
            uint histomax = BROTLI_MAX(histo0, BROTLI_MAX(histo1, histo2));
            return (kThreeSymbolHistogramCost +
                    2u * (histo0 + histo1 + histo2) - histomax);
        }
        if (count == 4)
        {
            uint* histo = stackalloc uint[4];
            uint h23;
            uint histomax;
            for (i = 0; i < 4; ++i)
            {
                histo[i] = histogram->data_[s[i]];
            }
            /* Sort */
            for (i = 0; i < 4; ++i)
            {
                nuint j;
                for (j = i + 1; j < 4; ++j)
                {
                    if (histo[j] > histo[i])
                    {
                        (histo[j], histo[i]) = (histo[i], histo[j]);  /* BROTLI_SWAP */
                    }
                }
            }
            h23 = histo[2] + histo[3];
            histomax = BROTLI_MAX(h23, histo[0]);
            return (kFourSymbolHistogramCost +
                    3u * h23 + 2u * (histo[0] + histo[1]) - histomax);
        }

        {
            /* In this loop we compute the entropy of the histogram and simultaneously
               build a simplified histogram of the code length codes where we use the
               zero repeat code 17, but we don't use the non-zero repeat code 16. */
            nuint max_depth = 1;
            uint* depth_histo = stackalloc uint[BROTLI_CODE_LENGTH_CODES];  /* = { 0 } */
            new Span<uint>(depth_histo, BROTLI_CODE_LENGTH_CODES).Clear();
            double log2total = FastLog2(histogram->total_count_);
            for (i = 0; i < data_size;)
            {
                if (histogram->data_[i] > 0)
                {
                    /* Compute -log2(P(symbol)) = -log2(count(symbol)/total_count) =
                                                = log2(total_count) - log2(count(symbol)) */
                    double log2p = log2total - FastLog2(histogram->data_[i]);
                    /* Approximate the bit depth by round(-log2(P(symbol))) */
                    nuint depth = (nuint)(log2p + 0.5);
                    bits += histogram->data_[i] * log2p;
                    if (depth > 15)
                    {
                        depth = 15;
                    }
                    if (depth > max_depth)
                    {
                        max_depth = depth;
                    }
                    ++depth_histo[depth];
                    ++i;
                }
                else
                {
                    /* Compute the run length of zeros and add the appropriate number of 0
                       and 17 code length codes to the code length code histogram. */
                    uint reps = 1;
                    nuint k;
                    for (k = i + 1; k < data_size && histogram->data_[k] == 0; ++k)
                    {
                        ++reps;
                    }
                    i += reps;
                    if (i == data_size)
                    {
                        /* Don't add any cost for the last zero run, since these are encoded
                           only implicitly. */
                        break;
                    }
                    if (reps < 3)
                    {
                        depth_histo[0] += reps;
                    }
                    else
                    {
                        reps -= 2;
                        while (reps > 0)
                        {
                            ++depth_histo[BROTLI_REPEAT_ZERO_CODE_LENGTH];
                            /* Add the 3 extra bits for the 17 code length code. */
                            bits += 3;
                            reps >>= 3;
                        }
                    }
                }
            }
            /* Add the estimated encoding cost of the code length code histogram. */
            bits += (double)(18 + 2 * max_depth);
            /* Add the entropy of the code length code histogram. */
            bits += BitsEntropy(depth_histo, BROTLI_CODE_LENGTH_CODES);
        }
        return bits;
    }

    /// <summary><c>BrotliPopulationCostDistance</c> (bit_cost_inc.h, FN = Distance).</summary>
    internal static double BrotliPopulationCostDistance(HistogramDistance* histogram)
    {
        nuint data_size = HistogramDataSizeDistance();
        int count = 0;
        nuint* s = stackalloc nuint[5];
        double bits = 0.0;
        nuint i;
        if (histogram->total_count_ == 0)
        {
            return kOneSymbolHistogramCost;
        }
        for (i = 0; i < data_size; ++i)
        {
            if (histogram->data_[i] > 0)
            {
                s[count] = i;
                ++count;
                if (count > 4) break;
            }
        }
        if (count == 1)
        {
            return kOneSymbolHistogramCost;
        }
        if (count == 2)
        {
            return (kTwoSymbolHistogramCost + (double)histogram->total_count_);
        }
        if (count == 3)
        {
            uint histo0 = histogram->data_[s[0]];
            uint histo1 = histogram->data_[s[1]];
            uint histo2 = histogram->data_[s[2]];
            uint histomax = BROTLI_MAX(histo0, BROTLI_MAX(histo1, histo2));
            return (kThreeSymbolHistogramCost +
                    2u * (histo0 + histo1 + histo2) - histomax);
        }
        if (count == 4)
        {
            uint* histo = stackalloc uint[4];
            uint h23;
            uint histomax;
            for (i = 0; i < 4; ++i)
            {
                histo[i] = histogram->data_[s[i]];
            }
            /* Sort */
            for (i = 0; i < 4; ++i)
            {
                nuint j;
                for (j = i + 1; j < 4; ++j)
                {
                    if (histo[j] > histo[i])
                    {
                        (histo[j], histo[i]) = (histo[i], histo[j]);  /* BROTLI_SWAP */
                    }
                }
            }
            h23 = histo[2] + histo[3];
            histomax = BROTLI_MAX(h23, histo[0]);
            return (kFourSymbolHistogramCost +
                    3u * h23 + 2u * (histo[0] + histo[1]) - histomax);
        }

        {
            /* In this loop we compute the entropy of the histogram and simultaneously
               build a simplified histogram of the code length codes where we use the
               zero repeat code 17, but we don't use the non-zero repeat code 16. */
            nuint max_depth = 1;
            uint* depth_histo = stackalloc uint[BROTLI_CODE_LENGTH_CODES];  /* = { 0 } */
            new Span<uint>(depth_histo, BROTLI_CODE_LENGTH_CODES).Clear();
            double log2total = FastLog2(histogram->total_count_);
            for (i = 0; i < data_size;)
            {
                if (histogram->data_[i] > 0)
                {
                    /* Compute -log2(P(symbol)) = -log2(count(symbol)/total_count) =
                                                = log2(total_count) - log2(count(symbol)) */
                    double log2p = log2total - FastLog2(histogram->data_[i]);
                    /* Approximate the bit depth by round(-log2(P(symbol))) */
                    nuint depth = (nuint)(log2p + 0.5);
                    bits += histogram->data_[i] * log2p;
                    if (depth > 15)
                    {
                        depth = 15;
                    }
                    if (depth > max_depth)
                    {
                        max_depth = depth;
                    }
                    ++depth_histo[depth];
                    ++i;
                }
                else
                {
                    /* Compute the run length of zeros and add the appropriate number of 0
                       and 17 code length codes to the code length code histogram. */
                    uint reps = 1;
                    nuint k;
                    for (k = i + 1; k < data_size && histogram->data_[k] == 0; ++k)
                    {
                        ++reps;
                    }
                    i += reps;
                    if (i == data_size)
                    {
                        /* Don't add any cost for the last zero run, since these are encoded
                           only implicitly. */
                        break;
                    }
                    if (reps < 3)
                    {
                        depth_histo[0] += reps;
                    }
                    else
                    {
                        reps -= 2;
                        while (reps > 0)
                        {
                            ++depth_histo[BROTLI_REPEAT_ZERO_CODE_LENGTH];
                            /* Add the 3 extra bits for the 17 code length code. */
                            bits += 3;
                            reps >>= 3;
                        }
                    }
                }
            }
            /* Add the estimated encoding cost of the code length code histogram. */
            bits += (double)(18 + 2 * max_depth);
            /* Add the entropy of the code length code histogram. */
            bits += BitsEntropy(depth_histo, BROTLI_CODE_LENGTH_CODES);
        }
        return bits;
    }
}
