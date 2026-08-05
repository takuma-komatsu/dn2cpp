#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: Regex.Replace (substitutions $1 ${name} $$ $& $` $', and a
// MatchEvaluator) and Regex.Split (captured separators, count/startat).
namespace RegexReplaceSplitSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- Replace substitutions --");
        Console.WriteLine(Regex.Replace("2026-07-08", @"(\d{4})-(\d{2})-(\d{2})", "$3/$2/$1"));
        Console.WriteLine(Regex.Replace("john@a.com", @"(?<user>[^@]+)@(?<host>.+)", "${host}:${user}"));
        Console.WriteLine(Regex.Replace("cost 5", @"\d+", "$$$&"));
        Console.WriteLine(Regex.Replace("mid", @"i", "[$`|$']"));
        Console.WriteLine(Regex.Replace("a b  c", @"\s+", "_"));
        Console.WriteLine(Regex.Replace("aaa", "a", "b", RegexOptions.None));

        Console.WriteLine("-- Replace with MatchEvaluator --");
        Console.WriteLine(Regex.Replace("1 2 3", @"\d+", m => (int.Parse(m.Value) * 10).ToString()));
        Console.WriteLine(Regex.Replace("abc def", @"\w+", m => m.Value.ToUpperInvariant()));
        Regex re = new Regex(@"\d");
        Console.WriteLine(re.Replace("a1b2c3", "#", 2));

        Console.WriteLine("-- Split --");
        Console.WriteLine(string.Join("|", Regex.Split("a1b22c333d", @"\d+")));
        Console.WriteLine(string.Join("|", Regex.Split("one, two,three", @",\s*")));
        Console.WriteLine(string.Join("|", Regex.Split("a1b2c3", @"(\d)")));
        Console.WriteLine(string.Join("|", new Regex(@",").Split("a,b,c,d", 3)));
        Console.WriteLine(string.Join("|", Regex.Split("xay", "a|b")));
        Console.WriteLine(Regex.Split("no separators", @"\d").Length);
    }
}
