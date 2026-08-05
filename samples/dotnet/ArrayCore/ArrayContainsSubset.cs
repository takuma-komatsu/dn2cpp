#nullable enable
using System;

// arr.Contains(x) binds to MemoryExtensions.Contains<T>(ReadOnlySpan<T>, T)
// via the implicit array -> ReadOnlySpan conversion. The real BCL body vectorizes
// with SIMD + Unsafe.BitCast (untranspilable); the transpiler intercepts the
// (span, value) shape of Contains/IndexOf and emits a scalar loop over the span's
// {reference, length}. CoreLib only (no Linq shim). Diffed exact vs.NET.
namespace ArrayContainsSubset;

class Program
{internal static void Run()
    {
        // --- int[] ---
        int[] a = { 1, 2, 3, 4, 5 };
        Console.WriteLine(a.Contains(3));   // True
        Console.WriteLine(a.Contains(9));   // False
        Console.WriteLine(a.AsSpan().IndexOf(4));  // 3 (MemoryExtensions.IndexOf)
        Console.WriteLine(a.AsSpan().IndexOf(9));  // -1
        Console.WriteLine(Array.IndexOf(a, 2));    // 1 (Array.IndexOf, sanity)

        // --- string[] (ordinal equality via the runtime helper) ---
        string[] s = { "alpha", "beta", "gamma" };
        Console.WriteLine(s.Contains("beta"));   // True
        Console.WriteLine(s.Contains("delta"));  // False
        Console.WriteLine(s.AsSpan().IndexOf("gamma")); // 2

        // --- other element widths ---
        long[] l = { 10L, 20L, 30L };
        Console.WriteLine(l.Contains(20L));  // True
        double[] d = { 1.5, 2.5, 3.5 };
        Console.WriteLine(d.Contains(2.5));  // True
        Console.WriteLine(d.Contains(2.6));  // False
        char[] c = { 'a', 'b', 'c' };
        Console.WriteLine(c.Contains('b'));  // True
        byte[] b = { 7, 8, 9 };
        Console.WriteLine(b.Contains((byte)8)); // True

        // --- a span local + empty array edge cases ---
        ReadOnlySpan<int> sp = a;
        Console.WriteLine(sp.Contains(5));   // True
        Console.WriteLine(sp.IndexOf(1));    // 0
        int[] empty = Array.Empty<int>();
        Console.WriteLine(empty.Contains(0)); // False
        Console.WriteLine(empty.AsSpan().IndexOf(0)); // -1
    }
}
