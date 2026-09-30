#nullable enable
using System;
namespace StringCopyToSubset;

// string.CopyTo(Span<char>): an exact copy when the string fits (a larger buffer
// keeps its tail) and ArgumentException when it does not. Drives
// ValueStringBuilder.AppendSlow.
static class Program
{
    private static string _evaluation = "";

    private static string? Text(string? value)
    {
        _evaluation += "S";
        return value;
    }

    private static int Number(string step, int value)
    {
        _evaluation += step;
        return value;
    }

    private static char[]? Destination(char[]? value)
    {
        _evaluation += "D";
        return value;
    }

    private static int ThrowingCount()
    {
        _evaluation += "C";
        throw new InvalidOperationException();
    }

    private static string Contents(char[]? value)
    {
        if (value is null)
            return "null";
        string result = "";
        foreach (char c in value)
        {
            if (c == '\0' || c >= '\uD800' && c <= '\uDFFF')
                result += "\\u" + ((int)c).ToString("X4");
            else
                result += c;
        }
        return result;
    }

    private static void Probe(string label, string? source, int sourceIndex,
        char[]? destination, int destinationIndex, int count)
    {
        try
        {
            source!.CopyTo(sourceIndex, destination!, destinationIndex, count);
            Console.WriteLine(label + " copied");
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + " " + ex.GetType().Name);
            if (ex is ArgumentException argument)
            {
                Console.WriteLine(label + " param=" + argument.ParamName);
                Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
                if (ex is ArgumentOutOfRangeException range && range.ActualValue is not null)
                    Console.WriteLine(label + " actual=" + range.ActualValue.GetType().Name + ":" + range.ActualValue);
            }
        }
        Console.WriteLine(label + " destination=" + Contents(destination));
    }

    internal static void RunFaults()
    {
        Console.WriteLine("== string array copy faults ==");
        Probe("source-window", "abcd", 1, new char[] { '.', '.', '.', '.' }, 0, 4);
        Probe("source-past-end", "abcd", 5, new char[4], 0, 0);
        Probe("source-negative", "abcd", -1, new char[4], 0, 1);
        Probe("count-negative", "abcd", -1, new char[4], -1, -1);
        Probe("destination-null", "abcd", -1, null, -1, -1);
        Probe("receiver-null", null, -1, null, -1, -1);
        Probe("source-before-destination", "abcd", 1, new char[1], -1, 4);
        Probe("destination-window", "abcd", 0, new char[] { '.', '.', '.' }, 1, 3);
        Probe("destination-past-end", "abcd", 0, new char[4], 5, 0);
        Probe("destination-negative", "abcd", 0, new char[4], -1, 1);
        Probe("destination-room-before-sign", "abcd", 0, Array.Empty<char>(), -1, 4);
        Probe("count-minimum", "abcd", int.MaxValue, new char[4], int.MinValue, int.MinValue);
        Probe("count-maximum", "abcd", 0, new char[4], 0, int.MaxValue);
        Probe("source-minimum", "abcd", int.MinValue, new char[4], 0, 0);
        Probe("source-maximum", "abcd", int.MaxValue, new char[4], 0, 0);
        Probe("destination-minimum", "abcd", 0, new char[4], int.MinValue, 0);
        Probe("destination-maximum", "abcd", 0, new char[4], int.MaxValue, 0);
        Probe("empty-at-end", "abcd", 4, new char[] { '.', '.', '.', '.' }, 4, 0);
        Probe("empty-source", "", 0, Array.Empty<char>(), 0, 0);
        Probe("copy-window", "abcdef", 1, new char[] { '.', '.', '.', '.', '.' }, 1, 3);
        Probe("copy-utf16", "\u0000\uD800\uDC00Z", 0, new char[4], 0, 4);

        _evaluation = "";
        try
        {
            Text(null)!.CopyTo(Number("I", -1), Destination(null)!, Number("J", -1), Number("C", -1));
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("null receiver evaluation=" + _evaluation);
        }
        _evaluation = "";
        try
        {
            Text(null)!.CopyTo(Number("I", -1), Destination(null)!, Number("J", -1), ThrowingCount());
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("throwing count evaluation=" + _evaluation);
        }
        Console.WriteLine("string array copy faults end");
    }

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
