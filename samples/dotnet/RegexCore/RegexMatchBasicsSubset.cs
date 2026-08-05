#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: Regex basics through the real interpreted engine — static and
// instance IsMatch/Match/Matches, Match properties, NextMatch iteration, and
// the startat overloads. Fixed ASCII inputs, diffed exact against real .NET.
namespace RegexMatchBasicsSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- IsMatch --");
        Console.WriteLine(Regex.IsMatch("hello world", @"\bworld\b"));
        Console.WriteLine(Regex.IsMatch("hello world", @"^\d+$"));
        Console.WriteLine(new Regex(@"[a-z]+").IsMatch("abc123"));
        Console.WriteLine(new Regex(@"^\s*$").IsMatch(""));

        Console.WriteLine("-- Match --");
        Match m = Regex.Match("call 555-1234 or 555-9876 now", @"\d{3}-\d{4}");
        Console.WriteLine(m.Success + " " + m.Index + " " + m.Length + " " + m.Value);
        Match m2 = m.NextMatch();
        Console.WriteLine(m2.Success + " " + m2.Index + " " + m2.Value);
        Match m3 = m2.NextMatch();
        Console.WriteLine(m3.Success + " " + (m3 == Match.Empty));
        Console.WriteLine(Regex.Match("nothing here", @"\d+").Success);

        Console.WriteLine("-- Matches --");
        MatchCollection all = Regex.Matches("a1 b22 c333 d4444", @"[a-z]\d+");
        Console.WriteLine(all.Count);
        foreach (Match x in all)
            Console.WriteLine(x.Index + ":" + x.Value);

        Console.WriteLine("-- instance / startat --");
        Regex re = new Regex(@"\d+");
        Console.WriteLine(re.Match("ab 12 cd 34", 5).Value);
        Console.WriteLine(re.IsMatch("ab 12", 3));
        Console.WriteLine(re.Matches("1 2 3", 2).Count);
        Console.WriteLine(re.ToString());
    }
}
