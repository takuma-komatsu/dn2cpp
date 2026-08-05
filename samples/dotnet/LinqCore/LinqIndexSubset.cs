#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqIndexSubset
{
    // additive LINQ operators bound to the real System.Linq
    //. Indexed Select/Where, SkipWhile/TakeWhile (+ indexed),
    // ToHashSet, static Range/Repeat, Zip (selector overload).
    // Results are materialized to
    // a List local before string.Join (the call-result threading boundary);
    // sources are List<T>, not T[] (arrays have no managed IEnumerable map).
    internal static class Program
    {
        internal static int Run()
        {
            List<int> xs = new List<int> { 10, 20, 30 };

            List<int> indexed = xs.Select((v, i) => v + i).ToList();
            Console.WriteLine(string.Join(",", indexed));                   // 10,21,32

            List<int> evenPos = Enumerable.Range(0, 10).Where((v, i) => i % 2 == 0).ToList();
            Console.WriteLine(string.Join(",", evenPos));                   // 0,2,4,6,8

            List<int> run = new List<int> { 1, 2, 3, 4, 1 };

            List<int> taken = run.TakeWhile(x => x < 4).ToList();
            Console.WriteLine(string.Join(",", taken));                     // 1,2,3

            List<int> takenIdx = new List<int> { 10, 11, 12, 13 }
                .TakeWhile((x, i) => i < 2).ToList();
            Console.WriteLine(string.Join(",", takenIdx));                  // 10,11

            List<int> skipped = run.SkipWhile(x => x < 3).ToList();
            Console.WriteLine(string.Join(",", skipped));                   // 3,4,1

            List<int> skippedIdx = new List<int> { 10, 11, 12, 13 }
                .SkipWhile((x, i) => i < 2).ToList();
            Console.WriteLine(string.Join(",", skippedIdx));                // 12,13

            List<int> nums = new List<int> { 1, 2, 3 };
            List<string> letters = new List<string> { "a", "b" };
            List<string> zipped = nums.Zip(letters, (n, s) => n + s).ToList();
            Console.WriteLine(string.Join(",", zipped));                    // 1a,2b

            List<int> ranged = Enumerable.Range(5, 4).ToList();
            Console.WriteLine(string.Join(",", ranged));                    // 5,6,7,8

            List<string> repeated = Enumerable.Repeat("x", 3).ToList();
            Console.WriteLine(string.Join(",", repeated));                  // x,x,x

            HashSet<int> set = new List<int> { 1, 2, 2, 3, 3, 3 }.ToHashSet();
            Console.WriteLine(set.Count);                                   // 3

            return 0;
        }
    }
}
