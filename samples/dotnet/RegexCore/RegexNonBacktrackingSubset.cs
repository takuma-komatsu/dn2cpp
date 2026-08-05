#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: RegexOptions.NonBacktracking — the symbolic DFA engine
// (shared-generic SymbolicRegexMatcher, BDD/minterm machinery). Patterns the
// engine rejects (lookarounds, backreferences, atomic groups, RightToLeft) are
// avoided except for the one construction-reject row at the tail.
namespace RegexNonBacktrackingSubset;

static class Program
{
    internal static void __GateEntry()
    {
        const RegexOptions NB = RegexOptions.NonBacktracking;

        Console.WriteLine("-- NonBacktracking basics --");
        Console.WriteLine(Regex.IsMatch("hello world", @"wor\w+", NB));
        Console.WriteLine(Regex.IsMatch("hello world", @"xyz", NB));
        Match m = Regex.Match("call 555-1234 now", @"\d{3}-\d{4}", NB);
        Console.WriteLine(m.Success + " " + m.Value + " " + m.Index + " " + m.Length);
        foreach (Match mm in Regex.Matches("cat bat rat", @"[cbr]at", NB))
            Console.WriteLine(mm.Index + ":" + mm.Value);
        Console.WriteLine(Regex.Count("a1b22c333", @"\d+", NB));

        Console.WriteLine("-- anchors / classes / quantifiers --");
        Console.WriteLine(Regex.IsMatch("first\nsecond", @"^second", NB | RegexOptions.Multiline));
        Console.WriteLine(Regex.Match("foo123bar456", @"\d+$", NB).Value);
        Console.WriteLine(Regex.Match("abc def", @"\b\w+\b", NB).Value);
        Console.WriteLine(Regex.Match("aaa bbb", @"a{2,}", NB).Value);
        Console.WriteLine(Regex.Match("xy z", @"[a-w]+", NB).Value);
        Console.WriteLine(Regex.Match("one  two", @"\s+", NB).Index);

        Console.WriteLine("-- alternation / groups (implicit capture 0) --");
        Console.WriteLine(Regex.Match("the quick fox", "quick|slow", NB).Value);
        Console.WriteLine(Regex.Match("2026-07-08", @"\d{4}-\d{2}-\d{2}", NB).Value);

        Console.WriteLine("-- IgnoreCase --");
        Console.WriteLine(Regex.IsMatch("HELLO", "hello", NB | RegexOptions.IgnoreCase));
        Console.WriteLine(Regex.IsMatch("ÄPFEL", "äpfel", NB | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

        Console.WriteLine("-- Replace / Split --");
        Console.WriteLine(Regex.Replace("one two  three", @"\s+", "_", NB));
        Console.WriteLine(string.Join("|", Regex.Split("a1b22c333d", @"\d+", NB)));

        Console.WriteLine("-- construction rejects (parity with real .NET) --");
        try { _ = new Regex(@"(?<=a)b", NB); }
        catch (Exception e) { Console.WriteLine(e.GetType().Name); } // NotSupportedException
    }
}
