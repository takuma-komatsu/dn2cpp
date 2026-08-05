#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: IgnoreCase through the real case-equivalence tables. Inputs under
// RegexOptions.IgnoreCase / inline (?i) stay ASCII, where the invariant tier
// dn2cpp folds to agrees with the host's NonTurkish one; only the
// CultureInvariant rows may use non-ASCII equivalences, since both sides then
// take RegexCaseBehavior.Invariant.
namespace RegexIgnoreCaseSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- IgnoreCase (ASCII) --");
        Console.WriteLine(Regex.IsMatch("HELLO", "hello", RegexOptions.IgnoreCase));
        Console.WriteLine(Regex.IsMatch("HeLLo WoRLd", @"hello world", RegexOptions.IgnoreCase));
        Console.WriteLine(Regex.Match("Foo BAR baz", "bar", RegexOptions.IgnoreCase).Index);
        Console.WriteLine(Regex.IsMatch("ABC", "[a-c]+", RegexOptions.IgnoreCase));
        Console.WriteLine(Regex.IsMatch("xyz", "[A-C]+", RegexOptions.IgnoreCase));

        Console.WriteLine("-- inline (?i) --");
        Console.WriteLine(Regex.IsMatch("SHOUT", "(?i)shout"));
        Console.WriteLine(Regex.Match("mixedCASE", "(?i:case)").Value);
        Console.WriteLine(Regex.IsMatch("Half", "(?i)h(?-i)alf"));
        Console.WriteLine(Regex.IsMatch("HAlf", "(?i)h(?-i)alf"));

        Console.WriteLine("-- CultureInvariant (non-ASCII rows) --");
        Console.WriteLine(Regex.IsMatch("K", "k", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
        Console.WriteLine(Regex.IsMatch("Äpfel", "äpfel", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
        Console.WriteLine(Regex.IsMatch("\u03A3\u03A9", "\u03c3\u03c9", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)); // sigma/omega
        Console.WriteLine(Regex.IsMatch("\u03c2", "\u03A3", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));   // final sigma
        Console.WriteLine(Regex.IsMatch("\u017F", "s", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));         // long s (Regex equivalence)
        Console.WriteLine(Regex.IsMatch("\uFB01", "FI", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));        // no ligature expansion

        Console.WriteLine("-- backreference IgnoreCase --");
        Console.WriteLine(Regex.IsMatch("Ha ha", @"(\w+) \1", RegexOptions.IgnoreCase));
    }
}
