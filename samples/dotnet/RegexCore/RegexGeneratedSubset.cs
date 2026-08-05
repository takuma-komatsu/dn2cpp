#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: the [GeneratedRegex] source generator — the SDK emits pure-C# Regex
// subclasses into this assembly and that IL is transpiled like user code.
// Covers literal, alternation, IgnoreCase, backtracking loop, named capture +
// backreference, matchTimeoutMilliseconds, RightToLeft, and NonBacktracking —
// the one option the generator declines, falling back to a cached plain Regex.
namespace RegexGeneratedSubset;

static partial class Program
{
    [GeneratedRegex("world")]
    private static partial Regex LiteralRegex();

    [GeneratedRegex("cat|dog|bird")]
    private static partial Regex AlternationRegex();

    [GeneratedRegex("hello", RegexOptions.IgnoreCase)]
    private static partial Regex IgnoreCaseRegex();

    [GeneratedRegex(@"(?<word>[a-z]+)\s+\k<word>")]
    private static partial Regex BackrefRegex();

    [GeneratedRegex(@"\w+\d")]
    private static partial Regex LoopBacktrackRegex();

    [GeneratedRegex(@"\d{2,4}", RegexOptions.None, 2000)]
    private static partial Regex TimeoutRegex();

    [GeneratedRegex(@"\d+", RegexOptions.RightToLeft)]
    private static partial Regex RightToLeftRegex();

    [GeneratedRegex("a+b", RegexOptions.NonBacktracking)]
    private static partial Regex NonBacktrackingRegex();

    internal static void __GateEntry()
    {
        Console.WriteLine("-- generated: literal --");
        Console.WriteLine(LiteralRegex().IsMatch("hello world"));
        Console.WriteLine(LiteralRegex().IsMatch("wor ld"));
        Match lit = LiteralRegex().Match("say world, world!");
        Console.WriteLine(lit.Index + ":" + lit.Value);
        Console.WriteLine(lit.NextMatch().Index);
        Console.WriteLine(LiteralRegex().Count("world world world"));
        Console.WriteLine(object.ReferenceEquals(LiteralRegex(), LiteralRegex()));

        Console.WriteLine("-- generated: alternation --");
        foreach (Match m in AlternationRegex().Matches("a cat, a dog and a bird"))
            Console.WriteLine(m.Index + ":" + m.Value);
        Console.WriteLine(AlternationRegex().IsMatch("catalog"));
        Console.WriteLine(AlternationRegex().IsMatch("do g"));
        Console.WriteLine(AlternationRegex().Replace("cat dog bird", "pet"));

        Console.WriteLine("-- generated: ignorecase --");
        Console.WriteLine(IgnoreCaseRegex().IsMatch("say HELLO"));
        Console.WriteLine(IgnoreCaseRegex().IsMatch("HeLLo there"));
        Console.WriteLine(IgnoreCaseRegex().Match("oh Hello world").Value);
        Console.WriteLine(IgnoreCaseRegex().Replace("Hello hELLO", "hi"));

        Console.WriteLine("-- generated: loop + backtrack --");
        Match bt = LoopBacktrackRegex().Match("abc123 xy");
        Console.WriteLine(bt.Success + " " + bt.Value);
        Console.WriteLine(LoopBacktrackRegex().Match("a1b2c3").Value);
        Console.WriteLine(LoopBacktrackRegex().IsMatch("abcdef"));
        Console.WriteLine(LoopBacktrackRegex().Count("no1 no2 yes3"));

        Console.WriteLine("-- generated: named capture + backreference --");
        Match br = BackrefRegex().Match("stop stop go");
        Console.WriteLine(br.Success + " " + br.Value + " [" + br.Groups["word"].Value + "]");
        Console.WriteLine(BackrefRegex().IsMatch("stop go"));
        Console.WriteLine(BackrefRegex().Replace("hey hey you you", "${word}!"));

        Console.WriteLine("-- generated: matchTimeoutMilliseconds --");
        Console.WriteLine(TimeoutRegex().MatchTimeout);
        Console.WriteLine(TimeoutRegex().Match("year 2026 ce").Value);
        Console.WriteLine(TimeoutRegex().IsMatch("only 1 digit"));

        Console.WriteLine("-- generated: RightToLeft --");
        Console.WriteLine(RightToLeftRegex().RightToLeft);
        Console.WriteLine(RightToLeftRegex().Match("a1 b22 c333").Value);
        Console.WriteLine(RightToLeftRegex().Matches("a1 b22 c333").Count);

        Console.WriteLine("-- generated: NonBacktracking fallback --");
        Console.WriteLine((NonBacktrackingRegex().Options & RegexOptions.NonBacktracking) != 0);
        Console.WriteLine(NonBacktrackingRegex().Match("xxaaabyy").Value);
        Console.WriteLine(NonBacktrackingRegex().IsMatch("ab") + " " + NonBacktrackingRegex().IsMatch("ba"));

        Console.WriteLine("-- generated: runner types --");
        // Custom subclass for supported patterns (RightToLeft included), plain
        // cached Regex for NonBacktracking.
        Console.WriteLine(IsGeneratedSubclass(LiteralRegex()));
        Console.WriteLine(IsGeneratedSubclass(IgnoreCaseRegex()));
        Console.WriteLine(IsGeneratedSubclass(BackrefRegex()));
        Console.WriteLine(IsGeneratedSubclass(RightToLeftRegex()));
        Console.WriteLine(IsGeneratedSubclass(NonBacktrackingRegex()));
    }

    private static bool IsGeneratedSubclass(Regex r)
    {
        return r.GetType() != typeof(Regex);
    }
}
