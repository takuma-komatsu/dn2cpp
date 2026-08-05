#nullable enable
using System;

// Array.Fill<T> and MemoryExtensions.SequenceEqual<T>. The BCL implements both
// with SIMD, untranspilable, so the transpiler emits scalar loops instead.
// Diffed exact vs .NET.
namespace SpanBulkSubset;

class Program
{internal static void __GateEntry()
    {
        // --- Array.Fill: whole-array, various element widths ---
        int[] a = new int[4];
        Array.Fill(a, 7);
        Console.WriteLine($"{a[0]},{a[1]},{a[2]},{a[3]}");   // 7,7,7,7

        long[] l = new long[3];
        Array.Fill(l, 9L);
        Console.WriteLine($"{l[0]},{l[2]}");                 // 9,9

        byte[] by = new byte[3];
        Array.Fill(by, (byte)5);
        Console.WriteLine($"{by[0]},{by[1]},{by[2]}");       // 5,5,5

        string[] s = new string[3];
        Array.Fill(s, "x");
        Console.WriteLine($"{s[0]},{s[1]},{s[2]}");          // x,x,x

        // --- Array.Fill: index/count range overload (leaves the rest untouched) ---
        int[] r = { 1, 2, 3, 4, 5 };
        Array.Fill(r, 0, 1, 3);
        Console.WriteLine($"{r[0]},{r[1]},{r[2]},{r[3]},{r[4]}"); // 1,0,0,0,5

        // --- MemoryExtensions.SequenceEqual ---
        int[] p = { 1, 2, 3 };
        int[] q = { 1, 2, 3 };
        int[] u = { 1, 2, 4 };
        int[] shorter = { 1, 2 };
        Console.WriteLine(p.AsSpan().SequenceEqual(q));        // True
        Console.WriteLine(p.AsSpan().SequenceEqual(u));        // False (value differs)
        Console.WriteLine(p.AsSpan().SequenceEqual(shorter));  // False (length differs)

        string[] ss = { "a", "b" };
        string[] st = { "a", "b" };
        string[] sx = { "a", "c" };
        Console.WriteLine(ss.AsSpan().SequenceEqual(st));      // True
        Console.WriteLine(ss.AsSpan().SequenceEqual(sx));      // False

        double[] d1 = { 1.5, 2.5 };
        double[] d2 = { 1.5, 2.5 };
        Console.WriteLine(d1.AsSpan().SequenceEqual(d2));      // True

        int[] e1 = Array.Empty<int>();
        int[] e2 = Array.Empty<int>();
        Console.WriteLine(e1.AsSpan().SequenceEqual(e2));      // True (both empty)
    }
}
