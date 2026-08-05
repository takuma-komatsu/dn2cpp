#nullable disable
using System;
using System.Globalization;

// The culture-taking / (string,int) / static-and-provider Char & String overloads
// that a real game (Thrive) reaches. dn2cpp is invariant-only, so every
// culture-taking form is exercised with CultureInfo.InvariantCulture and must diff
// exactly vs real .NET. Inputs stay in the ASCII + Latin-1 range where the
// invariant simple case fold and the host culture agree, so the diff is stable.
// Char / string results are printed as 4-hex-digit code units and hex dumps to
// keep stdout ASCII-safe.
namespace CultureInvariantOverloadsSubset;

class Program
{
    static string Dump(string s)
    {
        if (s is null)
            return "<null>";
        string r = "n=" + s.Length + " [";
        for (int i = 0; i < s.Length; i++)
        {
            if (i != 0) r = r + " ";
            r = r + ((int)s[i]).ToString("X4");
        }
        return r + "]";
    }

    static string H(char c) => ((int)c).ToString("X4");

    internal static void __GateEntry()
    {
        CultureInfo inv = CultureInfo.InvariantCulture;

        Console.WriteLine("-- String.ToLower/ToUpper(CultureInfo) --");
        foreach (string s in new[] { "Café Déjà 42", "ABCxyz", "MiXeD Àçø" })
        {
            Console.WriteLine(Dump(s.ToLower(inv)));
            Console.WriteLine(Dump(s.ToUpper(inv)));
        }

        Console.WriteLine("-- String.ToString(IFormatProvider) --");
        string self = "identity 123";
        Console.WriteLine(self.ToString(inv));
        Console.WriteLine(object.ReferenceEquals(self, self.ToString(inv)));

        Console.WriteLine("-- String.GetHashCode(span, StringComparison) --");
        // Ordinal and OrdinalIgnoreCase only. Values are non-deterministic across
        // runtimes, so assert the equalities dn2cpp guarantees, not the raw numbers:
        // an equal span hashes equally; OrdinalIgnoreCase folds case.
        string a = "HelloWorld";
        string b = "helloworld";
        ReadOnlySpan<char> sa = a.AsSpan();
        ReadOnlySpan<char> sb = b.AsSpan();
        int hOrdA = string.GetHashCode(sa, StringComparison.Ordinal);
        int hOrdA2 = string.GetHashCode(a.AsSpan(), StringComparison.Ordinal);
        int hOrdB = string.GetHashCode(sb, StringComparison.Ordinal);
        int hOicA = string.GetHashCode(sa, StringComparison.OrdinalIgnoreCase);
        int hOicB = string.GetHashCode(sb, StringComparison.OrdinalIgnoreCase);
        Console.WriteLine("ord equal-self: " + (hOrdA == hOrdA2));
        Console.WriteLine("ord case-differs: " + (hOrdA != hOrdB));
        Console.WriteLine("oic case-folds: " + (hOicA == hOicB));

        // NOTE: String.TryGetSpan(int, int, out ReadOnlySpan<char>) is also mapped
        // (an internal CoreLib helper Thrive reaches transitively), but it cannot be
        // exercised here: it is internal (no managed caller can name it) and every
        // public route to its only callers — CompareInfo.Compare/IndexOf/LastIndexOf
        // and Ordinal.IndexOf — is intercepted by dn2cpp before the real body
        // transpiles, so a diffing section could neither reach it nor (being ICU-backed
        // culture comparison) diff exactly against real .NET. Its correctness rides on
        // the inline implementation's documented semantics, verified by review.

        Console.WriteLine("-- Char.ToLower/ToUpper(char, CultureInfo) --");
        foreach (char c in new[] { 'a', 'Z', '5', 'é', 'É', 'à', 'ç' })
        {
            Console.WriteLine(H(c)
                + " lo=" + H(char.ToLower(c, inv))
                + " up=" + H(char.ToUpper(c, inv)));
        }

        Console.WriteLine("-- Char.IsUpper/IsWhiteSpace/IsDigit(string, int) --");
        string probe = "A b9\tZ";
        for (int i = 0; i < probe.Length; i++)
        {
            Console.WriteLine(i
                + " up=" + char.IsUpper(probe, i)
                + " ws=" + char.IsWhiteSpace(probe, i)
                + " dg=" + char.IsDigit(probe, i));
        }

        Console.WriteLine("-- Char.ToString(char) static + ToString(IFormatProvider) --");
        Console.WriteLine(char.ToString('Q'));
        Console.WriteLine(char.ToString('é'));
        char inst = 'Z';
        Console.WriteLine(inst.ToString(inv));

        Console.WriteLine("-- Char.TryParse(string, out char) --");
        PrintTryParse("Q");
        PrintTryParse("é");
        PrintTryParse("");
        PrintTryParse("ab");
        PrintTryParse(null);

        Console.WriteLine("-- TextInfo.ToUpper(string) --");
        Console.WriteLine(Dump(inv.TextInfo.ToUpper("Café Déjà 42")));
        Console.WriteLine(Dump(inv.TextInfo.ToUpper("abc")));
    }

    static void PrintTryParse(string s)
    {
        bool ok = char.TryParse(s, out char c);
        Console.WriteLine((s ?? "<null>") + " -> ok=" + ok + " c=" + H(c));
    }
}
