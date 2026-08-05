#nullable disable
using System;

// BMP invariant casing: char.ToUpperInvariant / ToLowerInvariant and the string
// ToUpperInvariant / ToLowerInvariant forms over a representative BMP sweep —
// ASCII, Latin-1 (e-acute, sharp s, micro sign), Greek (capital/small/final
// sigma), Cyrillic, Armenian, Georgian, full-width forms, ligatures, the
// Kelvin sign, and long s — plus the .NET-observable same-instance fast path
// (an already-cased string comes back ReferenceEquals to the input).
// Everything here is culture-independent under real .NET, so the gate diffs
// exactly. To stay ASCII-safe on stdout, char-level results are printed as
// 4-hex-digit code units and string-level results as hex dumps.
namespace CharCasingInvariantSubset;

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

    static void Sweep(char c)
    {
        Console.WriteLine(((int)c).ToString("X4")
            + " up=" + ((int)char.ToUpperInvariant(c)).ToString("X4")
            + " lo=" + ((int)char.ToLowerInvariant(c)).ToString("X4"));
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("-- char invariant sweep --");
        char[] probes =
        {
            'a', 'Z', '5', '_',        // ASCII
            'é', 'É',        // e-acute / E-acute
            'ß',                  // sharp s (stays itself under invariant upper)
            'µ',                  // MICRO SIGN -> GREEK CAPITAL MU
            'ÿ', 'Ÿ',        // y-diaeresis pair (cross-page mapping)
            'ı', 'İ',        // dotless i / dotted I (invariant: identity)
            'ſ',                  // long s -> S (differs from the ordinal fold)
            'Σ', 'σ', 'ς', // capital/small/final sigma
            'Д', 'д',        // Cyrillic De pair
            'Ա', 'ա',        // Armenian Ayb pair
            'Ⴀ', 'ⴀ',        // Georgian An (asymmetric block pair)
            'ẞ',                  // CAPITAL SHARP S -> sharp s on lower
            'Ω',                  // OHM SIGN -> omega on lower
            'K',                  // KELVIN SIGN -> k on lower
            'Ⓐ', 'ⓐ',        // circled A pair
            'ﬁ',                  // fi ligature (identity under simple mapping)
            'Ａ', 'ａ',        // full-width A pair
            'Ꭓ',                  // high-BMP Latin capital chi
            '\uD800', '\uDFFF',        // lone surrogates (identity)
            '￿',                  // top of the BMP (identity)
        };
        foreach (char c in probes)
        {
            Sweep(c);
        }

        Console.WriteLine("-- string invariant casing --");
        string mixed = "Straße σoς дa ａZ ſ 123";
        Console.WriteLine(Dump(mixed.ToUpperInvariant()));
        Console.WriteLine(Dump(mixed.ToLowerInvariant()));
        string greek = "ΟΔΥΣΣΕΙΑ";
        Console.WriteLine(Dump(greek.ToLowerInvariant()));
        Console.WriteLine(Dump(greek.ToLowerInvariant().ToUpperInvariant()));

        Console.WriteLine("-- same-instance fast path --");
        string upperAlready = "ALREADY UPPER 123 ß!"; // sharp s has no upper
        Console.WriteLine(object.ReferenceEquals(upperAlready, upperAlready.ToUpperInvariant()));
        string lowerAlready = "already lower 123!";
        Console.WriteLine(object.ReferenceEquals(lowerAlready, lowerAlready.ToLowerInvariant()));
        Console.WriteLine(object.ReferenceEquals(lowerAlready, lowerAlready.ToUpperInvariant()));
        string empty = "";
        Console.WriteLine(object.ReferenceEquals(empty, empty.ToUpperInvariant()));

        // 0-arg ToUpper()/ToLower() on pure ASCII + Latin-1: the invariant and
        // en-US-ish host-culture simple maps agree on these ranges, so the diff
        // vs real .NET stays exact.
        Console.WriteLine("-- 0-arg ToUpper/ToLower (ASCII + Latin-1) --");
        string latin1 = "Café Déjà Vu Àçø 42";
        Console.WriteLine(Dump(latin1.ToUpper()));
        Console.WriteLine(Dump(latin1.ToLower()));
        Console.WriteLine(char.ToUpper('k'));
        Console.WriteLine(char.ToLower('Q'));
    }
}
