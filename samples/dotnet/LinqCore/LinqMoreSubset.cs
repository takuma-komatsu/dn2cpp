#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqMoreSubset
{
    // comparison-free LINQ operators bound to the real System.Linq
    //. Distinct/Concat/Append/Prepend/Reverse/SelectMany/DefaultIfEmpty
    // (deferred), Single(OrDefault)/ElementAt(OrDefault)/Aggregate/Average/
    // ToDictionary. Ordering operators (OrderBy/ThenBy) are intentionally absent —
    // they live in the LinqOrdering bucket.
    internal static class Program
    {
        internal static int Run()
        {
            List<int> xs = new List<int> { 3, 1, 3, 2, 1, 3, 2 };

            List<int> distinct = xs.Distinct().ToList();
            Console.WriteLine(string.Join(",", distinct));                       // 3,1,2

            List<int> tail = new List<int> { 10, 20 };
            List<int> joined = xs.Distinct().Concat(tail).ToList();
            Console.WriteLine(string.Join(",", joined));                         // 3,1,2,10,20

            List<int> bracketed = tail.Append(30).Prepend(5).ToList();
            Console.WriteLine(string.Join(",", bracketed));                      // 5,10,20,30

            // Reverse the deferred Distinct result (List<T>.Reverse shadows the
            // extension when the receiver is a List, so use an IEnumerable source).
            List<int> reversed = xs.Distinct().Reverse().ToList();
            Console.WriteLine(string.Join(",", reversed));                       // 2,1,3

            List<int> flattened = new List<int> { 1, 2, 3 }
                .SelectMany(n => new List<int> { n, n * 10 }).ToList();
            Console.WriteLine(string.Join(",", flattened));                      // 1,10,2,20,3,30

            Console.WriteLine(tail.ElementAt(1));                                // 20
            Console.WriteLine(xs.ElementAtOrDefault(99));                        // 0

            List<string> one = new List<string> { "only" };
            Console.WriteLine(one.Single());                                     // only
            Console.WriteLine(new List<int>().SingleOrDefault());               // 0

            Console.WriteLine(xs.Aggregate(0, (acc, n) => acc + n));            // 15
            Console.WriteLine(tail.Average());                                   // 15
            Console.WriteLine(new List<double> { 1.0, 2.0, 6.0 }.Average());    // 3

            List<string> words = new List<string> { "ab", "cde", "f" };
            Dictionary<int, string> byLen = words.ToDictionary(s => s.Length);
            Console.WriteLine(byLen[3]);                                         // cde

            List<int> defaulted = new List<int>().DefaultIfEmpty().ToList();
            Console.WriteLine(string.Join(",", defaulted));                      // 0

            return 0;
        }
    }
}
