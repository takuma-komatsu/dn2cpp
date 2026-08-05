// Port of c/enc/literal_cost.{h,c} (brotli v1.1.0): literal cost model to
// allow backward reference replacement to be efficient. Reached from the
// Zopfli cost model (backward_references_hq.c, q>=10); ported with the q4-q9
// stage because it is a leaf of utf8_util. FP shapes are exact (PORTING.md).

using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.FastLog;

namespace DnBrotli.Enc;

internal static unsafe class LiteralCost
{
    private static nuint UTF8Position(nuint last, nuint c, nuint clamp)
    {
        if (c < 128)
        {
            return 0;  /* Next one is the 'Byte 1' again. */
        }
        else if (c >= 192)
        {  /* Next one is the 'Byte 2' of utf-8 encoding. */
            return BROTLI_MIN(1, clamp);
        }
        else
        {
            /* Let's decide over the last byte if this ends the sequence. */
            if (last < 0xE0)
            {
                return 0;  /* Completed two or three byte coding. */
            }
            else
            {  /* Next one is the 'Byte 3' of utf-8 encoding. */
                return BROTLI_MIN(2, clamp);
            }
        }
    }

    private static nuint DecideMultiByteStatsLevel(nuint pos, nuint len, nuint mask,
                                                   byte* data)
    {
        nuint* counts = stackalloc nuint[3] { 0, 0, 0 };
        nuint max_utf8 = 1;  /* should be 2, but 1 compresses better. */
        nuint last_c = 0;
        nuint i;
        for (i = 0; i < len; ++i)
        {
            nuint c = data[(pos + i) & mask];
            ++counts[UTF8Position(last_c, c, 2)];
            last_c = c;
        }
        if (counts[2] < 500)
        {
            max_utf8 = 1;
        }
        if (counts[1] + counts[2] < 25)
        {
            max_utf8 = 0;
        }
        return max_utf8;
    }

    private static void EstimateBitCostsForLiteralsUTF8(nuint pos, nuint len, nuint mask,
                                                        byte* data,
                                                        nuint* histogram, float* cost)
    {
        /* max_utf8 is 0 (normal ASCII single byte modeling),
           1 (for 2-byte UTF-8 modeling), or 2 (for 3-byte UTF-8 modeling). */
        nuint max_utf8 = DecideMultiByteStatsLevel(pos, len, mask, data);
        nuint window_half = 495;
        nuint in_window = BROTLI_MIN(window_half, len);
        nuint* in_window_utf8 = stackalloc nuint[3] { 0, 0, 0 };
        nuint i;
        new Span<nuint>(histogram, 3 * 256).Clear();

        {  /* Bootstrap histograms. */
            nuint last_c = 0;
            nuint utf8_pos = 0;
            for (i = 0; i < in_window; ++i)
            {
                nuint c = data[(pos + i) & mask];
                ++histogram[256 * utf8_pos + c];
                ++in_window_utf8[utf8_pos];
                utf8_pos = UTF8Position(last_c, c, max_utf8);
                last_c = c;
            }
        }

        /* Compute bit costs with sliding window. */
        for (i = 0; i < len; ++i)
        {
            if (i >= window_half)
            {
                /* Remove a byte in the past. */
                nuint c =
                    i < window_half + 1 ? (nuint)0 : data[(pos + i - window_half - 1) & mask];
                nuint last_c =
                    i < window_half + 2 ? (nuint)0 : data[(pos + i - window_half - 2) & mask];
                nuint utf8_pos2 = UTF8Position(last_c, c, max_utf8);
                --histogram[256 * utf8_pos2 + data[(pos + i - window_half) & mask]];
                --in_window_utf8[utf8_pos2];
            }
            if (i + window_half < len)
            {
                /* Add a byte in the future. */
                nuint c = data[(pos + i + window_half - 1) & mask];
                nuint last_c = data[(pos + i + window_half - 2) & mask];
                nuint utf8_pos2 = UTF8Position(last_c, c, max_utf8);
                ++histogram[256 * utf8_pos2 + data[(pos + i + window_half) & mask]];
                ++in_window_utf8[utf8_pos2];
            }
            {
                nuint c = i < 1 ? (nuint)0 : data[(pos + i - 1) & mask];
                nuint last_c = i < 2 ? (nuint)0 : data[(pos + i - 2) & mask];
                nuint utf8_pos = UTF8Position(last_c, c, max_utf8);
                nuint masked_pos = (pos + i) & mask;
                nuint histo = histogram[256 * utf8_pos + data[masked_pos]];
                double lit_cost;
                if (histo == 0)
                {
                    histo = 1;
                }
                lit_cost = FastLog2(in_window_utf8[utf8_pos]) - FastLog2(histo);
                lit_cost += 0.02905;
                if (lit_cost < 1.0)
                {
                    lit_cost *= 0.5;
                    lit_cost += 0.5;
                }
                /* Make the first bytes more expensive -- seems to help, not sure why.
                   Perhaps because the entropy source is changing its properties
                   rapidly in the beginning of the file, perhaps because the beginning
                   of the data is a statistical "anomaly". */
                if (i < 2000)
                {
                    lit_cost += 0.7 - ((double)(2000 - i) / 2000.0 * 0.35);
                }
                cost[i] = (float)lit_cost;
            }
        }
    }

    internal static void BrotliEstimateBitCostsForLiterals(nuint pos, nuint len, nuint mask,
                                                           byte* data,
                                                           nuint* histogram, float* cost)
    {
        if (Utf8Util.BrotliIsMostlyUTF8(data, pos, mask, len, Utf8Util.kMinUTF8Ratio) != 0)
        {
            EstimateBitCostsForLiteralsUTF8(pos, len, mask, data, histogram, cost);
            return;
        }
        else
        {
            nuint window_half = 2000;
            nuint in_window = BROTLI_MIN(window_half, len);
            nuint i;
            new Span<nuint>(histogram, 256).Clear();

            /* Bootstrap histogram. */
            for (i = 0; i < in_window; ++i)
            {
                ++histogram[data[(pos + i) & mask]];
            }

            /* Compute bit costs with sliding window. */
            for (i = 0; i < len; ++i)
            {
                nuint histo;
                if (i >= window_half)
                {
                    /* Remove a byte in the past. */
                    --histogram[data[(pos + i - window_half) & mask]];
                    --in_window;
                }
                if (i + window_half < len)
                {
                    /* Add a byte in the future. */
                    ++histogram[data[(pos + i + window_half) & mask]];
                    ++in_window;
                }
                histo = histogram[data[(pos + i) & mask]];
                if (histo == 0)
                {
                    histo = 1;
                }
                {
                    double lit_cost = FastLog2(in_window) - FastLog2(histo);
                    lit_cost += 0.029;
                    if (lit_cost < 1.0)
                    {
                        lit_cost *= 0.5;
                        lit_cost += 0.5;
                    }
                    cost[i] = (float)lit_cost;
                }
            }
        }
    }
}
