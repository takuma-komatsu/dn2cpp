#nullable disable
using System;

// Category-backed char classification over a representative BMP sweep:
// IsLetter/IsUpper/IsLower/IsDigit/IsNumber/IsLetterOrDigit/IsSeparator/
// IsWhiteSpace/IsPunctuation/IsSymbol/IsControl plus GetNumericValue, and the
// string-level whitespace consumers (Trim / IsNullOrWhiteSpace / split
// TrimEntries) with non-ASCII padding. Everything here is culture-independent
// under real .NET, so the gate diffs exactly. Char-level probes print
// booleans/doubles only; trimmed strings print as hex dumps.
namespace CharClassifySubset;

class Program
{
    static string Dump(string s)
    {
        string r = "n=" + s.Length + " [";
        for (int i = 0; i < s.Length; i++)
        {
            if (i != 0) r = r + " ";
            r = r + ((int)s[i]).ToString("X4");
        }
        return r + "]";
    }

    static void Classify(char c)
    {
        Console.WriteLine(((int)c).ToString("X4")
            + " L=" + char.IsLetter(c)
            + " U=" + char.IsUpper(c)
            + " l=" + char.IsLower(c)
            + " D=" + char.IsDigit(c)
            + " N=" + char.IsNumber(c)
            + " LD=" + char.IsLetterOrDigit(c)
            + " Sep=" + char.IsSeparator(c)
            + " WS=" + char.IsWhiteSpace(c)
            + " P=" + char.IsPunctuation(c)
            + " Sym=" + char.IsSymbol(c)
            + " C=" + char.IsControl(c));
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("-- classification sweep --");
        char[] probes =
        {
            'A', 'a', '0', '_', ' ', '\t', '$', '!', '\u007F',
            '\u00A0',        // NBSP: Zs -> separator + whitespace, not control
            '\u3000',        // ideographic space (Zs)
            '\u2028',        // LINE SEPARATOR (Zl)
            '\u2029',        // PARAGRAPH SEPARATOR (Zp)
            '\u0085',        // NEL: whitespace but Cc (control), not a separator
            '\u200B',        // ZERO WIDTH SPACE: Cf, NOT whitespace
            'Σ', 'σ',  // Greek capital/small sigma
            'д',              // Cyrillic small de
            'ʰ',              // MODIFIER LETTER SMALL H (Lm -> letter)
            'א',              // Hebrew alef (Lo -> letter, no case)
            'Ǆ',              // DZ WITH CARON (Lu); titlecase sibling below
            'ǅ',              // Lt: letter but neither upper nor lower
            '٣',              // ARABIC-INDIC THREE (Nd -> digit)
            '²',              // SUPERSCRIPT TWO (No: number, not digit)
            'Ⅻ',              // ROMAN NUMERAL TWELVE (Nl: number, not letter/digit)
            '½',              // VULGAR FRACTION ONE HALF (No)
            '«',              // Pi (initial-quote punctuation)
            '—',              // EM DASH (Pd)
            '‿',              // UNDERTIE (Pc)
            '€',              // EURO SIGN (Sc)
            '±',              // PLUS-MINUS (Sm)
            '©',              // COPYRIGHT (So)
            '˜',              // SMALL TILDE (Sk)
            '\uFFFF',        // top of the BMP (Cn)
        };
        foreach (char c in probes)
        {
            Classify(c);
        }

        Console.WriteLine("-- GetNumericValue --");
        char[] nums = { '7', '٣', 'Ⅻ', '½', '²', '〇', '¾', '⅓', 'A', '\u00A0' };
        foreach (char c in nums)
        {
            Console.WriteLine(((int)c).ToString("X4") + " -> " + char.GetNumericValue(c));
        }

        Console.WriteLine("-- string whitespace consumers --");
        string padded = "\u00A0\u3000 hello\u00A0world \u2028\u0085";
        Console.WriteLine(Dump(padded.Trim()));
        string noTrim = "abc";
        Console.WriteLine(object.ReferenceEquals(noTrim, noTrim.Trim()));
        Console.WriteLine(string.IsNullOrWhiteSpace("\u00A0\u3000\u2028\u2029\u0085 \t"));
        Console.WriteLine(string.IsNullOrWhiteSpace("\u00A0\u200B\u00A0"));
        string csv = "a,\u3000b\u00A0, ,c";
        string[] parts = csv.Split(',', StringSplitOptions.TrimEntries);
        Console.WriteLine(parts.Length);
        foreach (string p in parts)
        {
            Console.WriteLine(Dump(p));
        }
        parts = csv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Console.WriteLine(parts.Length);
        foreach (string p in parts)
        {
            Console.WriteLine(Dump(p));
        }
    }
}
