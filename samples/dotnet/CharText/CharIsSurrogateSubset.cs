#nullable disable
using System;

namespace CharIsSurrogateSubset
{
    // The UTF-16 surrogate-range predicates char.IsSurrogate / IsHighSurrogate /
    // IsLowSurrogate, intercepted as inline range checks. IsSurrogate covers the whole
    // surrogate block U+D800..U+DFFF; IsHighSurrogate the lead half U+D800..U+DBFF;
    // IsLowSurrogate the trail half U+DC00..U+DFFF. The three are reached together
    // by the BCL string-encoding fallback paths dn2cpp itself transpiles. Diffs
    // exact vs real.NET at every block boundary. CoreLib only (no Linq shim).
    internal static class Program
    {
        internal static void __GateEntry()
        {
            // Boundary sweep: just below the block, both endpoints of each half,
            // the high/low split, and just above the block.
            ushort[] cps =
            {
                0x0000, // NUL — not a surrogate
                0x0041, // 'A' — not a surrogate
                0xD7FF, // just below the surrogate block
                0xD800, // first high surrogate
                0xDBFF, // last high surrogate
                0xDC00, // first low surrogate
                0xDFFF, // last low surrogate (and last surrogate)
                0xE000, // just above the surrogate block
                0xFFFF, // max BMP — not a surrogate
            };

            foreach (ushort u in cps)
            {
                char c = (char)u;
                Console.WriteLine(
                    "U+" + u.ToString("X4") +
                    " IsSurrogate=" + char.IsSurrogate(c) +
                    " IsHigh=" + char.IsHighSurrogate(c) +
                    " IsLow=" + char.IsLowSurrogate(c));
            }

            // The (char high, char low) surrogate-pair overloads — reached by the BCL
            // encoder fallback (EncoderExceptionFallbackBuffer.Fallback). IsSurrogatePair
            // tests the lead/trail ranges; ConvertToUtf32 combines the pair into its
            // UTF-32 scalar. Sweep both endpoints of the astral plane plus a non-pair.
            (char, char)[] pairs =
            {
                ('\uD800', '\uDC00'), // U+10000 (first astral)
                ('\uD83D', '\uDE00'), // U+1F600
                ('\uDBFF', '\uDFFF'), // U+10FFFF (last astral)
                ('\uD800', '\uDFFF'), // high + last low
                ('A', '\uDC00'),      // not a high surrogate -> IsSurrogatePair false
            };
            foreach (var (hi, lo) in pairs)
            {
                Console.Write("pair " + ((int)hi).ToString("X4") + "," + ((int)lo).ToString("X4") +
                    " IsPair=" + char.IsSurrogatePair(hi, lo));
                if (char.IsSurrogatePair(hi, lo))
                    Console.Write(" U+" + char.ConvertToUtf32(hi, lo).ToString("X"));
                Console.WriteLine();
            }
        }
    }
}
