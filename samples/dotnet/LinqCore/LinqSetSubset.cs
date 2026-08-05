#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqSetSubset
{
    // LINQ set operators bound to the real System.Linq —
    // Union/Intersect/Except (HashSet-backed, first-appearance dedup) +
    // DistinctBy. Results materialized to a List local before string.Join.
    internal static class Program
    {
        internal static int Run()
        {
            List<int> a = new List<int> { 1, 2, 2, 3, 4 };
            List<int> b = new List<int> { 3, 4, 4, 5, 6 };

            List<int> u = a.Union(b).ToList();
            Console.WriteLine(string.Join(",", u));          // 1,2,3,4,5,6

            List<int> i = a.Intersect(b).ToList();
            Console.WriteLine(string.Join(",", i));          // 3,4

            List<int> e = a.Except(b).ToList();
            Console.WriteLine(string.Join(",", e));          // 1,2

            List<string> words = new List<string> { "apple", "avocado", "banana", "cherry", "blueberry" };
            List<string> byFirst = words.DistinctBy(w => w[0].ToString()).ToList();
            Console.WriteLine(string.Join(",", byFirst));    // apple,banana,cherry

            return 0;
        }
    }
}
