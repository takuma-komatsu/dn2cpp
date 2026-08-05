#nullable enable
using System;

// Guid parsing through the real CoreLib IL over fixed literals: Parse /
// TryParse / ParseExact / TryParseExact across every format specifier
// (D/N/B/P/X), mixed case, and whitespace trimming. Failure cases print the
// TryParse result, the caught exception type name, AND its .Message — the SR
// resource text is now recovered from the embedded .resources blob, so a
// FormatException's message diffs exact vs real .NET.
namespace GuidParseSubset;

static class Program
{
    const string D = "0f8fad5b-d9cb-469f-a165-70867728950e";
    const string N = "0f8fad5bd9cb469fa16570867728950e";
    const string B = "{0f8fad5b-d9cb-469f-a165-70867728950e}";
    const string P = "(0f8fad5b-d9cb-469f-a165-70867728950e)";
    const string X = "{0x0f8fad5b,0xd9cb,0x469f,{0xa1,0x65,0x70,0x86,0x77,0x28,0x95,0x0e}}";

    internal static void __GateEntry()
    {
        Console.WriteLine("-- Guid.Parse --");
        Console.WriteLine(Guid.Parse(D));
        Console.WriteLine(Guid.Parse(N));
        Console.WriteLine(Guid.Parse(B));
        Console.WriteLine(Guid.Parse(P));
        Console.WriteLine(Guid.Parse(X));
        Console.WriteLine(Guid.Parse(D.ToUpperInvariant()));
        Console.WriteLine(Guid.Parse("  " + D + "  "));
        Console.WriteLine(new Guid(D));

        Console.WriteLine("-- Guid.TryParse --");
        Console.WriteLine(Guid.TryParse(D, out Guid td) + " " + td);
        Console.WriteLine(Guid.TryParse("not-a-guid", out Guid tf) + " " + tf);
        Console.WriteLine(Guid.TryParse(N.Substring(0, 31), out Guid ts) + " " + ts);
        Console.WriteLine(Guid.TryParse((string)null!, out Guid tn) + " " + tn);

        Console.WriteLine("-- Guid.ParseExact --");
        Console.WriteLine(Guid.ParseExact(D, "D"));
        Console.WriteLine(Guid.ParseExact(N, "N"));
        Console.WriteLine(Guid.ParseExact(B, "B"));
        Console.WriteLine(Guid.ParseExact(P, "P"));
        Console.WriteLine(Guid.ParseExact(X, "X"));

        Console.WriteLine("-- Guid.TryParseExact --");
        Console.WriteLine(Guid.TryParseExact(D, "D", out Guid xd) + " " + xd);
        Console.WriteLine(Guid.TryParseExact(D, "N", out Guid xn) + " " + xn);
        Console.WriteLine(Guid.TryParseExact(B, "P", out Guid xp) + " " + xp);

        Console.WriteLine("-- parse failures (exception type name + message) --");
        try { Guid.Parse("xyzzy"); }
        catch (Exception e) { Console.WriteLine(e.GetType().Name + ": " + e.Message); }
        try { Guid.ParseExact(D, "N"); }
        catch (Exception e) { Console.WriteLine(e.GetType().Name + ": " + e.Message); }
        try { Guid.ParseExact(D, "Q"); }
        catch (Exception e) { Console.WriteLine(e.GetType().Name + ": " + e.Message); }
        try { Guid.Parse((string)null!); }
        catch (Exception e) { Console.WriteLine(e.GetType().Name + ": " + e.Message); }

        Console.WriteLine("-- span parse --");
        Console.WriteLine(Guid.Parse(D.AsSpan()));
        Console.WriteLine(Guid.TryParse(N.AsSpan(), out Guid sp) + " " + sp);
    }
}
