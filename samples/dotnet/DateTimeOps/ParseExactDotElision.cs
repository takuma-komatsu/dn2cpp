using System;
using System.Globalization;

// DateTime.TryParseExact custom-format '.' elision: a '.' absent from the input
// elides together with its 'F' run, in .NET 10's exact shape -- one format char
// after the '.' skips unconditionally, then an 'F' must stand right there. That
// shape is SDK-sensitive (dotnet/runtime main tightened it to an immediate 'F'),
// so a red diff here after an SDK bump means the rule moved. CoreLib only.
namespace ParseExactDotElision;

static class Program
{
    static readonly CultureInfo ci = CultureInfo.InvariantCulture;

    static void P(string s, string f)
    {
        bool ok = DateTime.TryParseExact(s, f, ci, DateTimeStyles.None, out var v);
        Console.WriteLine(s + " | " + f + " -> " + ok + " " + v.ToString("HH:mm:ss.fffffff", ci));
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("-- DateTime.TryParseExact '.' elision --");
        P("12:34:56", "HH:mm:ss.FFF");     // fraction absent: dot + F run elide
        P("12:34:56.5", "HH:mm:ss.FFF");   // partial F digits
        P("12:34:56.", "HH:mm:ss.FFF");    // dot present, zero F digits
        P("12:34:56", "HH:mm:ss.FF");      // two F's: elides
        P("12:34:56", "HH:mm:ss.F");       // one F: does NOT elide
        P("12:34:56.5", "HH:mm:ss.F");
        P("12:34:56", "HH:mm:ss.");        // no F at all
        P("12:34:56", "HH:mm:ss..FFF");    // double dot still elides
        P("12:34:56.", "HH:mm:ss..FFF");   // first dot matches, second elides
        P("12:34:56..5", "HH:mm:ss..FFF"); // dots match the input 1:1
        P("12:34:56.5", "HH:mm:ss..FFF");  // digit where the second dot must stand
        P("12:34:56..", "HH:mm:ss..FFF");  // both dots present, no digits
        P("12:34:56", "HH:mm:ss...FFF");   // triple dot: does not elide
        P("12:34:56", "HH:mm:ss..fff");    // lowercase 'f' never elides
        P("12:34:56", "HH:mm:ss.FFF.FFF"); // each unmatched dot elides its own run
        P("12:34:56.1", "HH:mm:ss.FFF.FFF");
        P("12:34:56.56", "HH:mm:ss.FF.FF");
    }
}
