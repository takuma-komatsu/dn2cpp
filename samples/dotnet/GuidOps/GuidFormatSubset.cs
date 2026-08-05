#nullable enable
using System;

// Guid formatting through the real CoreLib IL over a fixed literal: ToString
// with every specifier (D/N/B/P/X + "" + null), string interpolation
// (ISpanFormattable via DefaultInterpolatedStringHandler), TryFormat into a
// Span<char> per specifier, and the invalid-specifier failure path (type name
// only — SR message carve-out).
namespace GuidFormatSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Guid g = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e");

        Console.WriteLine("-- ToString specifiers --");
        Console.WriteLine(g.ToString());
        Console.WriteLine(g.ToString("D"));
        Console.WriteLine(g.ToString("N"));
        Console.WriteLine(g.ToString("B"));
        Console.WriteLine(g.ToString("P"));
        Console.WriteLine(g.ToString("X"));
        Console.WriteLine(g.ToString(""));
        Console.WriteLine(g.ToString((string)null!));
        Console.WriteLine(g.ToString("d"));   // lowercase specifiers allowed
        Console.WriteLine(g.ToString("n"));

        Console.WriteLine("-- interpolation / Empty --");
        Console.WriteLine($"guid={g}");
        Console.WriteLine($"guid={g:N}");
        Console.WriteLine(Guid.Empty);
        Console.WriteLine(Guid.Empty.ToString("B"));

        Console.WriteLine("-- TryFormat --");
        Span<char> buf = new char[68];
        Console.WriteLine(g.TryFormat(buf, out int w1, "D") + " " + w1 + " " + new string(buf.Slice(0, w1)));
        Console.WriteLine(g.TryFormat(buf, out int w2, "N") + " " + w2 + " " + new string(buf.Slice(0, w2)));
        Console.WriteLine(g.TryFormat(buf, out int w3, "X") + " " + w3 + " " + new string(buf.Slice(0, w3)));
        Span<char> tiny = new char[8];
        Console.WriteLine(g.TryFormat(tiny, out int w4, "D") + " " + w4);   // False 0

        Console.WriteLine("-- invalid specifier (exception type name) --");
        try { g.ToString("Z"); }
        catch (Exception e) { Console.WriteLine(e.GetType().Name); }
    }
}
