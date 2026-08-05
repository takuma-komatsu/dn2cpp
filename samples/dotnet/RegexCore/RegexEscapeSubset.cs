#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: Regex.Escape / Unescape round-trips, and an escaped literal used as
// a live pattern.
namespace RegexEscapeSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- Escape --");
        Console.WriteLine(Regex.Escape(@"1+1=2? (yes)"));
        Console.WriteLine(Regex.Escape(@"a.b*c[d]"));
        Console.WriteLine(Regex.Escape("tab\there"));
        Console.WriteLine(Regex.Escape("plain"));

        Console.WriteLine("-- Unescape --");
        Console.WriteLine(Regex.Unescape(@"1\+1=2\?"));
        Console.WriteLine(Regex.Unescape(@"line\nbreak") == "line\nbreak");
        Console.WriteLine(Regex.Unescape(@"A\x42\t!"));

        Console.WriteLine("-- escaped literal as pattern --");
        string needle = "price (net)?";
        Console.WriteLine(Regex.IsMatch("the price (net)? is 5", Regex.Escape(needle)));
        Console.WriteLine(Regex.Unescape(Regex.Escape(needle)) == needle);
    }
}
