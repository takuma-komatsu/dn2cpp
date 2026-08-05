#nullable enable
using System;
using System.Collections.Generic;

// string.Concat<T>(IEnumerable<T>) over a List<T> with value elements. Concatenation is
// Join with an empty separator and each element's invariant formatting equals its
// ToString, so the count-aware Join helpers are reused — no per-element boxing.
namespace ConcatValueSubset;


class Program
{
    internal static void __GateEntry()
    {
        // int elements.
        List<int> ints = new List<int> { 1, 2, 3, 42 };
        Console.WriteLine(string.Concat(ints));

        // long elements.
        List<long> longs = new List<long> { 100L, 200L, 300L };
        Console.WriteLine(string.Concat(longs));

        // double elements (integer-valued so formatting is exact, not the :G concern).
        List<double> dbls = new List<double> { 5.0, 6.0, 7.0 };
        Console.WriteLine(string.Concat(dbls));

        // Empty and single-element lists.
        Console.WriteLine("[" + string.Concat(new List<int>()) + "]");
        Console.WriteLine(string.Concat(new List<int> { 9 }));

        // Reference elements still work.
        Console.WriteLine(string.Concat(new List<string> { "a", "b", "c" }));

        // Value-element Concat over a call-result list.
        Console.WriteLine(string.Concat(Range(3)));
    }

    static List<int> Range(int n)
    {
        List<int> r = new List<int>();
        for (int i = 0; i < n; i++)
            r.Add(i);
        return r;
    }
}
