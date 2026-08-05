#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

// A raw T[] used where IEnumerable<T> is expected. Compiled against the real
// System.Linq, which the gate also passes via -r so every operator transpiles
// from the real BCL IL. dn2cpp arrays carry no managed interface
// dispatch table, so without boxing every case here would crash at runtime with
// EntryPointNotFoundException; the transpiler boxes the array into a per-T
// SZArrayEnumerable<T> at the T[] -> IEnumerable<T> boundary. Covers int[] (ArrayI4), string[] (ArrayRef),
// double[]/long[] (ArrayN), chaining, a user IEnumerable<T> parameter, and the bare
// `foreach ((IEnumerable<T>)arr)` receiver form.
namespace ArrayLinqSubset;


static class Program
{
    // A user method whose parameter is IEnumerable<T> — passing an array must wrap.
    static int SumViaParam(IEnumerable<int> xs)
    {
        int s = 0;
        foreach (int x in xs)
            s += x;
        return s;
    }internal static void Run()
    {
        int[] a = { 1, 2, 3, 4, 5 };

        // --- int[] (ArrayI4) through the LINQ operators ---
        Console.WriteLine(string.Join(",", a.Where(v => v > 2)));        // 3,4,5
        Console.WriteLine(string.Join(",", a.Select(v => v * 10)));      // 10,20,30,40,50
        Console.WriteLine(a.Where(v => v % 2 == 1).Sum());              // 1+3+5 = 9
        Console.WriteLine(a.Sum());                                      // 15
        Console.WriteLine(a.Count(v => v >= 3));                         // 3
        Console.WriteLine(a.Any(v => v == 4) ? "yes" : "no");           // yes
        Console.WriteLine(a.First(v => v > 3));                          // 4

        // Chained deferred operators over an array source.
        Console.WriteLine(string.Join(",", a.Where(v => v > 1).Select(v => v + 100).Take(2))); // 102,103

        // OrderByDescending over an array source (materialized — string.Join over a
        // bare IOrderedEnumerable<T> is a separate, unrelated gap).
        Console.WriteLine(string.Join(",", a.OrderByDescending(v => v).ToArray()));   // 5,4,3,2,1

        // --- string[] (ArrayRef) ---
        string[] words = { "apple", "to", "banana", "hi" };
        Console.WriteLine(string.Join(",", words.Where(w => w.Length > 2)));    // apple,banana
        Console.WriteLine(string.Join(",", words.Select(w => w.Length)));        // 5,2,6,2
        Console.WriteLine(words.Count(w => w.Length == 2));                      // 2

        // --- double[] / long[] (ArrayN) ---
        double[] ds = { 1.5, 2.5, 4.0 };
        Console.WriteLine(ds.Sum());                                    // 8
        Console.WriteLine(string.Join(",", ds.Where(d => d >= 2.5)));   // 2.5,4
        long[] ls = { 100L, 200L, 300L };
        Console.WriteLine(ls.Sum());                                    // 600

        // --- user IEnumerable<T> parameter receives an array ---
        Console.WriteLine(SumViaParam(a));                             // 15

        // --- bare foreach over (IEnumerable<T>)arr (Roslyn elides the local) ---
        IEnumerable<int> e = a;
        int t = 0;
        foreach (int x in e)
            t += x;
        Console.WriteLine(t);                                          // 15

        // --- empty array ---
        int[] empty = Array.Empty<int>();
        Console.WriteLine(empty.Where(v => v > 0).Count());            // 0
        Console.WriteLine(empty.Sum());                                // 0

        // --- Enumerable.SequenceEqual's ARRAY fast path ---
        // Two T[] make Enumerable.SequenceEqual hand the whole compare to
        // MemoryExtensions.SequenceEqual, forwarding its own comparer PARAMETER — an
        // ldarg, not an ldnull, so no static test can see its null-ness. The emitted
        // scan hoists a runtime interface probe out of the loop: the comparerless rows
        // prove the forwarded null takes the default-equality arm, and the
        // custom-comparer rows prove a genuine comparer is dispatched through — the
        // mod-10 comparer equates {1,2,3} with {11,12,13} exactly as real .NET does.
        int[] q = { 1, 2, 3 };
        int[] qsame = { 1, 2, 3 };
        int[] qdiff = { 1, 2, 4 };
        int[] qmod = { 11, 12, 13 };
        Console.WriteLine(q.SequenceEqual(qsame));                     // True
        Console.WriteLine(q.SequenceEqual(qdiff));                     // False
        Console.WriteLine(q.SequenceEqual(qsame, null));               // True
        Console.WriteLine(q.SequenceEqual(qsame, EqualityComparer<int>.Default)); // True
        Console.WriteLine(q.SequenceEqual(qmod, new Mod10Comparer())); // True
        Console.WriteLine(q.SequenceEqual(qdiff, new Mod10Comparer())); // False

        // The throwing comparer proves the scan really enters the comparer's own
        // Equals: both sides print the PlatformNotSupportedException it throws out
        // of itself. The equal lengths matter — Enumerable short-circuits a length
        // mismatch before consulting the comparer.
        try { Console.WriteLine(q.SequenceEqual(qsame, new ThrowingComparer())); }
        catch (PlatformNotSupportedException) { Console.WriteLine("PlatformNotSupportedException"); }
    }

    // Custom equality that visibly differs from default: congruence mod 10.
    sealed class Mod10Comparer : IEqualityComparer<int>
    {
        public bool Equals(int a, int b) => ((a % 10) + 10) % 10 == ((b % 10) + 10) % 10;
        public int GetHashCode(int v) => ((v % 10) + 10) % 10;
    }

    // Reaching Equals is the observable.
    sealed class ThrowingComparer : IEqualityComparer<int>
    {
        public bool Equals(int a, int b) => throw new PlatformNotSupportedException("custom comparer reached");
        public int GetHashCode(int v) => v;
    }
}
