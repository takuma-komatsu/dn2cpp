#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: the residual public Regex API through the real IL — EnumerateSplits,
// RegexParseException Error/Offset, CompileToAssembly, Match.Result,
// Match.Synchronized, Capture.ValueSpan, Options/RightToLeft, CacheSize, and
// the startat/length overloads. Message texts are never printed: SR
// placeholders diverge from real .NET.
namespace RegexApiSurfaceSubset;

static class Program
{
    internal static void __GateEntry()
    {
        const RegexOptions NB = RegexOptions.NonBacktracking;

        Console.WriteLine("-- EnumerateSplits --");
        ReadOnlySpan<char> span = "a1b22c333d".AsSpan();
        Regex digits = new Regex(@"\d+");
        foreach (Range r in digits.EnumerateSplits(span))
            Console.WriteLine(r + "=" + span[r].ToString());
        foreach (Range r in digits.EnumerateSplits(span, 2))
            Console.WriteLine(r + "=" + span[r].ToString());
        foreach (Range r in digits.EnumerateSplits(span, 2, 3))
            Console.WriteLine(r + "=" + span[r].ToString());
        foreach (Range r in Regex.EnumerateSplits(span, @"\d+", NB))
            Console.WriteLine(r + "=" + span[r].ToString());

        Console.WriteLine("-- RegexParseException --");
        ParseRow("(abc");        // not enough closing parentheses
        ParseRow("[a-z");        // unterminated set
        ParseRow("*abc");        // quantifier after nothing
        ParseRow("a{2,1}");      // reversed quantifier range
        ParseRow("(?<1a>x)");    // invalid group name
        ParseRow(@"\p{Nope}");   // unrecognized unicode property
        ParseRow(@"a\");         // trailing backslash

        Console.WriteLine("-- CompileToAssembly --");
#pragma warning disable SYSLIB0036
        try
        {
            // The AssemblyName argument is irrelevant: .NET Core throws
            // PlatformNotSupportedException before reading any argument.
            Regex.CompileToAssembly(
                new[] { new RegexCompilationInfo(@"\d+", RegexOptions.None, "D", "N", true) },
                null!);
        }
        catch (Exception e) { Console.WriteLine(e.GetType().Name); }
#pragma warning restore SYSLIB0036

        Console.WriteLine("-- Match.Result --");
        Match m = Regex.Match("john@a.com", @"(?<user>[^@]+)@(.+)");
        Console.WriteLine(m.Result("$1"));
        Console.WriteLine(m.Result("${user}"));
        Console.WriteLine(m.Result("[$&]"));
        Console.WriteLine(m.Result("$$${user}$$"));
        Console.WriteLine(m.Result("$`|$'"));
        Console.WriteLine(Regex.Match("x42y", @"\d+", NB).Result("<$&>"));

        Console.WriteLine("-- Match.Synchronized --");
        Match sync = Match.Synchronized(m);
        Console.WriteLine(sync.Success + " " + sync.Value + " " + sync.Groups["user"].Value);
        Console.WriteLine(object.ReferenceEquals(sync, Match.Synchronized(sync)));

        Console.WriteLine("-- Capture.ValueSpan --");
        Console.WriteLine(m.ValueSpan.ToString());
        Console.WriteLine(m.Groups["user"].ValueSpan.ToString() + "/" + m.Groups[1].ValueSpan.ToString());
        Console.WriteLine(m.Index + ":" + m.Length + ":" + m.Value);
        Console.WriteLine((m.Value == m.ValueSpan.ToString()) + " " + (m.Length == m.ValueSpan.Length));

        Console.WriteLine("-- ToString / Options / RightToLeft --");
        Regex opts = new Regex(@"a.b", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        Console.WriteLine(opts.ToString());
        Console.WriteLine(opts.Options);
        Console.WriteLine(opts.RightToLeft);
        Regex rtl = new Regex(@"\d+", RegexOptions.RightToLeft);
        Console.WriteLine(rtl.RightToLeft + " " + rtl.Match("a1 b22 c333").Value);
        Console.WriteLine(new Regex("x", NB).Options);

        Console.WriteLine("-- CacheSize --");
        Console.WriteLine(Regex.CacheSize);
        Regex.CacheSize = 4;
        Console.WriteLine(Regex.IsMatch("abc", "b"));
        Console.WriteLine(Regex.IsMatch("abc", "c$"));
        Console.WriteLine(Regex.Match("k7", @"\d").Value);
        Console.WriteLine(Regex.Replace("dots", "o", "0"));
        Console.WriteLine(Regex.IsMatch("abc", "b")); // cache hit path
        Console.WriteLine(Regex.CacheSize);
        Regex.CacheSize = 15;

        Console.WriteLine("-- startat / length overloads --");
        Regex an = new Regex("an");
        Console.WriteLine(an.Match("banana", 2).Index);
        Console.WriteLine(an.Match("banana", 2, 3).Success + " " + an.Match("banana", 0, 4).Index);
        Console.WriteLine(an.Matches("banana", 2).Count);
        Console.WriteLine(an.IsMatch("banana", 4) + " " + an.IsMatch("banana", 5));
        Regex d = new Regex(@"\d");
        Console.WriteLine(d.Replace("a1b2c3", "#", 2, 2));
        Console.WriteLine(string.Join("|", d.Split("a1b2c3", 2, 2)));
        Console.WriteLine(new Regex("an", NB).Match("banana", 2).Index);
    }

    private static void ParseRow(string pattern)
    {
        try
        {
            _ = new Regex(pattern);
            Console.WriteLine("no error: " + pattern);
        }
        catch (RegexParseException e)
        {
            Console.WriteLine(e.Error + " @" + e.Offset);
        }
    }
}
