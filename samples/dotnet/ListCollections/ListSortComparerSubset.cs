#nullable disable
using System;
using System.Collections.Generic;

namespace ListSortComparerSubset
{
    // List<T>.Sort(IComparer<T>) / Array.Sort(T[], IComparer<T>) now honor the
    // comparer (was silently sorted in natural order). The Array.Sort intrinsic emits
    // a callback sort + a thunk that dispatches IComparer<T>.Compare; reachability
    // marks Compare used so the impl is emitted. Uses distinct keys so the result is
    // fully determined (sort stability is unspecified for equal keys).
    internal sealed class DescInt : IComparer<int>
    {
        public int Compare(int a, int b) => b - a;
    }

    // Order longs by their last decimal digit (distinct here).
    internal sealed class ByLastDigit : IComparer<long>
    {
        public int Compare(long a, long b) => (int)(a % 10) - (int)(b % 10);
    }

    internal static class Program
    {
        internal static void __GateEntry()
        {
            var xs = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 };
            xs.Sort(new DescInt());
            Console.WriteLine(string.Join(",", xs));     // 9,6,5,4,3,2,1,1

            // > 16 distinct values to exercise the callback sort over a larger set.
            var big = new List<int>();
            for (int i = 1; i <= 20; i++) big.Add((i * 7) % 23);
            big.Sort(new DescInt());
            Console.WriteLine(string.Join(",", big));

            var longs = new List<long> { 91, 13, 47, 25, 60 };
            longs.Sort(new ByLastDigit());
            Console.WriteLine(string.Join(",", longs));  // 60,91,13,25,47

            int[] arr = { 9, 3, 7, 1, 5 };
            Array.Sort(arr, new DescInt());
            Console.WriteLine(string.Join(",", arr));    // 9,7,5,3,1

            // natural (no-comparer) path still works
            var nat = new List<int> { 30, 10, 20 };
            nat.Sort();
            Console.WriteLine(string.Join(",", nat));    // 10,20,30
        }
    }
}
