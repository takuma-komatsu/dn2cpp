#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: character classes, quantifiers, anchors, alternation,
// backreferences, lookarounds and atomic groups through the real interpreted
// engine. ASCII inputs only; \p{...} exercises char.GetUnicodeCategory.
namespace RegexClassesQuantifiersSubset;

static class Program
{
    static void P(string input, string pattern)
    {
        Match m = Regex.Match(input, pattern);
        Console.WriteLine((m.Success ? m.Value : "<no>"));
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("-- classes --");
        P("order #42!", @"\d+");
        P("order #42!", @"\w+");
        P("a b", @"\s");
        P("Hello", @"\p{Lu}\p{Ll}+");
        P("x=5", @"[^=]+");
        P("bag beg big", @"b[ei]g");
        P("a-f0-9", @"[a-c]+");

        Console.WriteLine("-- quantifiers --");
        P("aaa", "a*");
        P("", "a*");
        P("aaab", "a+?");
        P("aaab", "a+");
        P("color colour", "colou?r");
        P("xx xxx xxxx", "x{3}");
        P("xx xxx xxxx", "x{3,}");
        P("aaaa", "a{2,3}?");

        Console.WriteLine("-- anchors / alternation --");
        P("start middle end", "^start");
        P("start middle end", "end$");
        P("one two three", @"\btwo\b");
        P("cat dog bird", "dog|bird");
        P("catfish", "^(cat|catfish)$");

        Console.WriteLine("-- backreferences --");
        P("hello hello world", @"(\w+) \1");
        P("abab", @"(ab)\1");
        Console.WriteLine(Regex.IsMatch("abac", @"(ab)\1"));
        P("<b>bold</b>", @"<(\w+)>.*?</\1>");

        Console.WriteLine("-- lookarounds --");
        P("price: 100yen", @"\d+(?=yen)");
        P("price: 100yen", @"(?<=price: )\d+");
        P("foo1 bar2", @"\w+(?<!1)\b");
        Console.WriteLine(Regex.IsMatch("foobar", @"foo(?!bar)"));
        Console.WriteLine(Regex.IsMatch("foobaz", @"foo(?!bar)"));

        Console.WriteLine("-- atomic / conditionals --");
        Console.WriteLine(Regex.IsMatch("aaab", @"(?>a+)ab"));
        Console.WriteLine(Regex.IsMatch("aaab", @"a+ab"));
        P("ab", @"(?(1)b|a)(b)?");
    }
}
