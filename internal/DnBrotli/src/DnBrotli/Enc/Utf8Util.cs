// Port of c/enc/utf8_util.{h,c} (brotli v1.1.0): heuristics for deciding
// about the UTF8-ness of strings. Reachable from ChooseContextMode (q>=10)
// and from the literal cost model (BrotliEstimateBitCostsForLiterals).

namespace DnBrotli.Enc;

internal static unsafe class Utf8Util
{
    /* Minimum fraction of UTF8-encoded data to accept the UTF8 hypothesis
       (utf8_util.h). */
    internal const double kMinUTF8Ratio = 0.75;

    private static nuint BrotliParseAsUTF8(
        int* symbol, byte* input, nuint size)
    {
        /* ASCII */
        if ((input[0] & 0x80) == 0)
        {
            *symbol = input[0];
            if (*symbol > 0)
            {
                return 1;
            }
        }
        /* 2-byte UTF8 */
        if (size > 1u &&
            (input[0] & 0xE0) == 0xC0 &&
            (input[1] & 0xC0) == 0x80)
        {
            *symbol = (((input[0] & 0x1F) << 6) |
                       (input[1] & 0x3F));
            if (*symbol > 0x7F)
            {
                return 2;
            }
        }
        /* 3-byte UFT8 */
        if (size > 2u &&
            (input[0] & 0xF0) == 0xE0 &&
            (input[1] & 0xC0) == 0x80 &&
            (input[2] & 0xC0) == 0x80)
        {
            *symbol = (((input[0] & 0x0F) << 12) |
                       ((input[1] & 0x3F) << 6) |
                       (input[2] & 0x3F));
            if (*symbol > 0x7FF)
            {
                return 3;
            }
        }
        /* 4-byte UFT8 */
        if (size > 3u &&
            (input[0] & 0xF8) == 0xF0 &&
            (input[1] & 0xC0) == 0x80 &&
            (input[2] & 0xC0) == 0x80 &&
            (input[3] & 0xC0) == 0x80)
        {
            *symbol = (((input[0] & 0x07) << 18) |
                       ((input[1] & 0x3F) << 12) |
                       ((input[2] & 0x3F) << 6) |
                       (input[3] & 0x3F));
            if (*symbol > 0xFFFF && *symbol <= 0x10FFFF)
            {
                return 4;
            }
        }
        /* Not UTF8, emit a special symbol above the UTF8-code space */
        *symbol = 0x110000 | input[0];
        return 1;
    }

    /* Returns 1 if at least min_fraction of the data is UTF8-encoded.*/
    internal static int BrotliIsMostlyUTF8(
        byte* data, nuint pos, nuint mask,
        nuint length, double min_fraction)
    {
        nuint size_utf8 = 0;
        nuint i = 0;
        while (i < length)
        {
            int symbol;
            nuint bytes_read =
                BrotliParseAsUTF8(&symbol, &data[(pos + i) & mask], length - i);
            i += bytes_read;
            if (symbol < 0x110000) size_utf8 += bytes_read;
        }
        return ((double)size_utf8 > min_fraction * (double)length) ? 1 : 0;
    }
}
