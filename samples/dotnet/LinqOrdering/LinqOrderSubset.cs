#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqOrderSubset
{
    // LINQ ordering (OrderBy/OrderByDescending/ThenBy/ThenByDescending) over
    // the real System.Linq, unblocked by the core generic-IComparable<T>
    // dispatch fix (constrained. callvirt CompareTo devirtualized for primitive/
    // string keys). Covers int and string keys, ascending/descending, multi-key
    // tie-breaks, and a chained Where -> OrderBy -> Select pipeline.
    internal static class Program
    {
        internal static int Run()
        {
            List<int> xs = new List<int> { 5, 3, 8, 1, 9, 2, 7 };

            List<int> asc = xs.OrderBy(x => x).ToList();
            Console.WriteLine(string.Join(",", asc));                            // 1,2,3,5,7,8,9

            List<int> desc = xs.OrderByDescending(x => x).ToList();
            Console.WriteLine(string.Join(",", desc));                           // 9,8,7,5,3,2,1

            List<string> ws = new List<string> { "bb", "aa", "ccc", "a", "bb" };

            // Order by length, tie-break alphabetically (stable on the duplicate "bb").
            List<string> byLenThenName = ws.OrderBy(s => s.Length).ThenBy(s => s).ToList();
            Console.WriteLine(string.Join(",", byLenThenName));                  // a,aa,bb,bb,ccc

            List<string> byLenDescThenNameDesc = ws.OrderByDescending(s => s.Length).ThenByDescending(s => s).ToList();
            Console.WriteLine(string.Join(",", byLenDescThenNameDesc));          // ccc,bb,bb,aa,a

            List<string> alpha = ws.OrderBy(s => s).ToList();
            Console.WriteLine(string.Join(",", alpha));                          // a,aa,bb,bb,ccc

            // Chained pipeline: filter, order, project.
            List<int> pipeline = xs.Where(x => x > 2).OrderByDescending(x => x).Select(x => x * 10).ToList();
            Console.WriteLine(string.Join(",", pipeline));                       // 90,80,70,50,30

            // Order by a computed double key.
            List<int> byHalf = new List<int> { 4, 1, 3, 2 }.OrderBy(x => x / 2.0).ToList();
            Console.WriteLine(string.Join(",", byHalf));                         // 1,2,3,4

            return 0;
        }
    }
}
