#nullable disable
using System;

namespace EnumParseWsSubset
{
    internal enum Color
    {
        Red = 1,
        Green = 2,
        Blue = 4,
    }

    // Enum.Parse/TryParse whitespace trimming, diffed exactly vs real .NET.
    // .NET trims name tokens with char.IsWhiteSpace — FULL Unicode whitespace
    // (Zs/Zl/Zp plus U+0009-000D and U+0085), not just ASCII: the leading trim
    // is span.TrimStart(), each comma-separated token gets .Trim(). The numeric
    // form is the exception: after the Unicode leading trim, the integer parser
    // accepts trailing *ASCII* whitespace only (NumberStyles.AllowTrailingWhite),
    // so "5" + U+00A0 falls through to the name path and fails while "5\t"
    // parses. The cases below pin both sides of that asymmetry.
    internal static class Program
    {
        private static void Parse(string label, string s, bool ignoreCase = false)
        {
            try
            {
                Color v = (Color)Enum.Parse(typeof(Color), s, ignoreCase);
                Console.WriteLine($"{label} parse: {v} ({(int)v})");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{label} parse: {e.GetType().Name}");
            }
            bool ok = Enum.TryParse<Color>(s, ignoreCase, out Color tv);
            Console.WriteLine($"{label} tryparse: {ok} {tv} ({(int)tv})");
        }

        internal static void __GateEntry()
        {
            // ASCII whitespace around names (worked before the Unicode swap).
            Parse("ascii", " \tRed\r\n ");
            Parse("ascii-list", " Red , Green ");

            // Unicode whitespace around names: NBSP (Latin-1 Zs), EM SPACE
            // (above-Latin-1 Zs), IDEOGRAPHIC SPACE, and NEL (the Cc member).
            Parse("nbsp-lead", "\u00A0Red");
            Parse("emsp-trail", "Red\u2003");
            Parse("ideo-both", "\u3000Red\u3000");
            Parse("nel-both", "\u0085Red\u0085");
            Parse("uni-list", "\u00A0Red\u2003,\u3000Blue\u00A0");
            Parse("uni-icase", "\u00A0rEd\u3000", true);

            // Numeric form: Unicode whitespace may LEAD (span.TrimStart) but a
            // trailing run must be ASCII (AllowTrailingWhite) — trailing NBSP /
            // IDEOGRAPHIC SPACE makes the numeric parse fail and the "5" token
            // is no member name, so the whole parse fails.
            Parse("num", "5");
            Parse("num-ascii", " 5\t");
            Parse("num-sign", " +3");
            Parse("num-neg", "-2 ");
            Parse("num-nbsp-lead", "\u00A05");
            Parse("num-nbsp-trail", "5\u00A0");
            Parse("num-ideo-trail", "5\u3000");

            // Whitespace-only input fails either way.
            Parse("ws-only-ascii", "  ");
            Parse("ws-only-uni", "\u00A0\u3000");
        }
    }
}
