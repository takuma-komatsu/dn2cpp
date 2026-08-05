#nullable disable
using System;
using System.Collections.Generic;

namespace ListSortComparisonSubset
{
    // + : List<T>.Sort(Comparison<T>) / Array.Sort(T[], Comparison<T>) now
    // run the real BCL introsort (ArraySortHelper). This needs the RVA-span +
    // ref-byte fixes and the
    // MemoryMarshal.GetArrayDataReference intrinsic. The Comparison delegate
    // is invoked directly, so a custom order is honored (was silently ignored / a
    // build error before). Uses int comparisons (string.CompareOrdinal is a separate
    // unsupported intrinsic).
    internal static class Program
    {
        internal static void __GateEntry()
        {
            var xs = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 };
            xs.Sort((a, b) => a - b);
            Console.WriteLine(string.Join(",", xs));   // 1,1,2,3,4,5,6,9
            xs.Sort((a, b) => b - a);
            Console.WriteLine(string.Join(",", xs));   // 9,6,5,4,3,2,1,1

            // > 16 elements exercises the real introsort recursion (not just the
            // insertion-sort threshold). Sort by absolute value.
            var ys = new List<int> { -7, 3, -1, 8, -5, 2, 9, -4, 6, 0, -10, 11, -12, 13, -2, 1, -8 };
            ys.Sort((a, b) => Math.Abs(a) - Math.Abs(b));
            Console.WriteLine(string.Join(",", ys));

            // natural (no-comparer) path still works
            var zs = new List<int> { 30, 10, 20 };
            zs.Sort();
            Console.WriteLine(string.Join(",", zs));   // 10,20,30

            int[] arr = { 9, 3, 7, 1, 5 };
            Array.Sort(arr, (a, b) => a - b);
            Console.WriteLine(string.Join(",", arr));  // 1,3,5,7,9
        }
    }
}
