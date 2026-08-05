#nullable enable
using System;

// Span<T>/ReadOnlySpan<T> instance bulk methods. The BCL bodies go through
// SpanHelpers/Buffer.Memmove and are untranspilable, so the transpiler emits
// element loops over the span's {reference, length}.
namespace SpanInstanceSubset;

class Program
{internal static void __GateEntry()
    {
        // --- Span.Clear (whole + a Slice view; the rest is untouched) ---
        int[] a = { 1, 2, 3, 4, 5 };
        ((Span<int>)a).Slice(1, 3).Clear();
        Console.WriteLine($"{a[0]},{a[1]},{a[2]},{a[3]},{a[4]}");  // 1,0,0,0,5

        // --- Span.Fill (int / long / byte / string) ---
        int[] f = new int[3];
        ((Span<int>)f).Fill(7);
        Console.WriteLine($"{f[0]},{f[1]},{f[2]}");               // 7,7,7

        long[] fl = new long[2];
        ((Span<long>)fl).Fill(99L);
        Console.WriteLine($"{fl[0]},{fl[1]}");                   // 99,99

        byte[] fb = new byte[3];
        ((Span<byte>)fb).Fill((byte)4);
        Console.WriteLine($"{fb[0]},{fb[2]}");                   // 4,4

        string[] fs = new string[2];
        ((Span<string>)fs).Fill("z");
        Console.WriteLine($"{fs[0]},{fs[1]}");                   // z,z

        // --- Span.CopyTo (Span and ReadOnlySpan source) ---
        int[] src = { 10, 20, 30 };
        int[] dst = new int[3];
        ((Span<int>)src).CopyTo(dst);
        Console.WriteLine($"{dst[0]},{dst[1]},{dst[2]}");        // 10,20,30

        int[] dst2 = new int[3];
        ((ReadOnlySpan<int>)src).CopyTo(dst2);
        Console.WriteLine($"{dst2[2]}");                          // 30

        string[] ssrc = { "p", "q" };
        string[] sdst = new string[2];
        ((Span<string>)ssrc).CopyTo(sdst);
        Console.WriteLine($"{sdst[0]},{sdst[1]}");              // p,q

        // --- Span.ToArray (a fresh copy, independent of the source) ---
        int[] orig = { 1, 2, 3 };
        int[] copy = ((Span<int>)orig).ToArray();
        copy[0] = 99;
        Console.WriteLine($"{orig[0]},{copy[0]},{copy[2]}");     // 1,99,3

        string[] sorig = { "a", "b" };
        string[] scopy = ((ReadOnlySpan<string>)sorig).ToArray();
        Console.WriteLine($"{scopy[0]},{scopy[1]}");            // a,b

        // --- empty-span edge cases ---
        int[] empty = Array.Empty<int>();
        ((Span<int>)empty).Clear();                              // no-op
        int[] ecopy = ((ReadOnlySpan<int>)empty).ToArray();
        Console.WriteLine(ecopy.Length);                         // 0
    }
}
