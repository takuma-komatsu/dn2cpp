#nullable enable
using System;
using System.Collections.Generic;

// A raw T[] passed where IList<T> / IReadOnlyList<T> / ICollection<T> /
// IReadOnlyCollection<T> is expected, then dispatching Count / the indexer (get and
// the write-through setter) / Contains / IndexOf / CopyTo through the interface. The
// transpiler boxes the array into a Dn2Cpp.Runtime.SZArrayEnumerable<T> (now also
// implementing the list/collection interfaces) at the boundary. LINQ binds to
// the real System.Linq via -r. Diffed against real .NET.
namespace ArrayCollectionSubset;


static class Program
{
    static int Sum(IReadOnlyList<int> xs)
    {
        int s = 0;
        for (int i = 0; i < xs.Count; i++)
            s += xs[i];
        return s;
    }

    // Write-through: the wrapper's indexer setter writes the backing array.
    static void DoubleEach(IList<int> xs)
    {
        for (int i = 0; i < xs.Count; i++)
            xs[i] = xs[i] * 2;
    }

    static bool Has(ICollection<int> xs, int v)
    {
        return xs.Contains(v);
    }

    static int IdxOf(IList<int> xs, int v)
    {
        return xs.IndexOf(v);
    }

    static int CountOf(IReadOnlyCollection<int> xs)
    {
        return xs.Count;
    }

    static void CopyInto(ICollection<int> xs, int[] dst)
    {
        xs.CopyTo(dst, 1);
    }

    static string Join(IReadOnlyList<string> xs)
    {
        string r = "";
        for (int i = 0; i < xs.Count; i++)
            r += (i == 0 ? "" : ",") + xs[i];
        return r;
    }internal static void Run()
    {
        int[] a = { 5, 10, 15, 20 };

        Console.WriteLine(Sum(a));           // 50
        Console.WriteLine(CountOf(a));       // 4
        Console.WriteLine(Has(a, 15));       // True
        Console.WriteLine(Has(a, 99));       // False
        Console.WriteLine(IdxOf(a, 20));     // 3
        Console.WriteLine(IdxOf(a, 99));     // -1

        // CopyTo into a destination array at offset 1.
        int[] dst = new int[6];
        CopyInto(a, dst);
        Console.WriteLine(string.Join(",", dst));   // 0,5,10,15,20,0

        // Write-through indexer setter: DoubleEach mutates the real array.
        DoubleEach(a);
        Console.WriteLine(string.Join(",", a));     // 10,20,30,40
        Console.WriteLine(Sum(a));                  // 100

        // Reference-element array (ArrayRef) as IReadOnlyList<string>.
        string[] words = { "alpha", "beta", "gamma" };
        Console.WriteLine(Join(words));             // alpha,beta,gamma

        // Bare foreach over (IEnumerable<int>)arr — proves the auto-referenced support
        // shim works with NO LINQ shim present (this sample uses only corelib).
        IEnumerable<int> e = a;
        int viaEnum = 0;
        foreach (int x in e)
            viaEnum += x;
        Console.WriteLine(viaEnum);                 // 100 (a was doubled above)
    }
}
