#nullable enable
using System;
namespace StringCopyToSubset;

// string.CopyTo(Span<char>): an exact copy when the string fits (a larger buffer
// keeps its tail) and ArgumentException when it does not. Drives
// ValueStringBuilder.AppendSlow.
static class Program
{
    internal static void Run()
    {
        string s = "hello world";

        // Exact-size destination round-trip.
        char[] exact = new char[s.Length];
        s.CopyTo(exact);
        Console.WriteLine(new string(exact));                  // hello world

        // Larger destination: the tail keeps its prior content.
        char[] wide = new char[s.Length + 4];
        for (int i = 0; i < wide.Length; i++) wide[i] = '.';
        s.CopyTo(wide);
        Console.WriteLine(new string(wide));                   // hello world....

        // Offset sub-span destination.
        char[] off = new char[16];
        for (int i = 0; i < off.Length; i++) off[i] = '_';
        "core".CopyTo(off.AsSpan(3));
        Console.WriteLine(new string(off));                    // ___core_________

        // Empty string into an empty destination: a no-op, no throw.
        string.Empty.CopyTo(Span<char>.Empty);
        Console.WriteLine("empty-ok");

        // Too-short destination throws ArgumentException.
        char[] tiny = new char[4];
        try
        {
            s.CopyTo(tiny);
            Console.WriteLine("short=NOTHROW");
        }
        catch (ArgumentException)
        {
            Console.WriteLine("short=ArgumentException");
        }
    }
}
