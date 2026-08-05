#nullable enable
using System;
namespace StringSpanSubset;


static class Program
{
    // Sum char codes through a span to prove the span reads the string buffer directly.
    static int CodeSum(ReadOnlySpan<char> s)
    {
        int t = 0;
        foreach (char c in s) t += c;
        return t;
    }internal static void Run()
    {
        string s = "hello world";

        // Whole-string span: length, indexer, IndexOf, Contains.
        ReadOnlySpan<char> sp = s.AsSpan();
        Console.WriteLine(sp.Length);          // 11
        Console.WriteLine((int)sp[0]);         // 104
        Console.WriteLine(sp.IndexOf('w'));    // 6
        Console.WriteLine(sp.IndexOf('z'));    // -1
        Console.WriteLine(sp.Contains('o'));   // True
        Console.WriteLine(sp.LastIndexOf('o')); // 7

        // AsSpan(start) and AsSpan(start, length) sub-spans.
        ReadOnlySpan<char> tail = s.AsSpan(6);     // "world"
        Console.WriteLine(tail.Length);            // 5
        Console.WriteLine((int)tail[0]);           // 119
        ReadOnlySpan<char> head = s.AsSpan(0, 5);  // "hello"
        Console.WriteLine(head.Length);            // 5
        Console.WriteLine(CodeSum(head));          // 532

        // span-vs-span structural ops (both spans come from strings).
        Console.WriteLine(head.StartsWith("he".AsSpan()));        // True
        Console.WriteLine(head.EndsWith("lo".AsSpan()));          // True
        Console.WriteLine(head.SequenceEqual("hello".AsSpan()));  // True
        Console.WriteLine(head.SequenceEqual(tail));              // False

        // A string literal's own span.
        Console.WriteLine("abc".AsSpan().Length); // 3

        // Empty string span edge.
        Console.WriteLine("".AsSpan().Length);    // 0
        Console.WriteLine("".AsSpan().IndexOf('x')); // -1
    }
}
