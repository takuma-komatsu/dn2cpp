#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqSubset
{
    // LINQ-to-objects over List<T> bound to the real System.Linq.
    // Exercises deferred operators (Where/Select/Take/Skip), materializers
    // (ToList/ToArray), quantifiers/counting (Count/Any/All/Contains), element
    // access (First/FirstOrDefault/Last/LastOrDefault) and numeric aggregates
    // (Sum/Min/Max + selector forms), including chained pipelines.
    internal static class Program
    {
        internal static int Run()
        {
            List<int> xs = new List<int> { 5, 3, 8, 1, 9, 2, 7 };

            // Deferred pipeline + materialize.
            List<int> evensTimes10 = xs.Where(x => x % 2 == 0).Select(x => x * 10).ToList();
            Console.WriteLine(string.Join(",", evensTimes10));        // 80,20

            int[] window = xs.Skip(2).Take(3).ToArray();
            Console.WriteLine(string.Join(",", window));              // 8,1,9

            // Counting / quantifiers.
            Console.WriteLine(xs.Count());                            // 7
            Console.WriteLine(xs.Count(x => x > 4));                  // 4
            Console.WriteLine(xs.Any(x => x > 8));                    // True
            Console.WriteLine(xs.Any(x => x > 100));                  // False
            Console.WriteLine(xs.All(x => x > 0));                    // True
            Console.WriteLine(xs.Contains(9));                        // True
            Console.WriteLine(xs.Contains(42));                       // False

            // Element access.
            Console.WriteLine(xs.First());                            // 5
            Console.WriteLine(xs.First(x => x > 6));                  // 8
            Console.WriteLine(xs.FirstOrDefault(x => x > 100));       // 0
            Console.WriteLine(xs.Last());                             // 7
            Console.WriteLine(xs.LastOrDefault());                    // 7

            // Numeric aggregates.
            Console.WriteLine(xs.Sum());                              // 35
            Console.WriteLine(xs.Min());                              // 1
            Console.WriteLine(xs.Max());                              // 9
            Console.WriteLine(xs.Where(x => x % 2 == 1).Sum());       // 5+3+1+9+7 = 25

            // Aggregate with selector over a reference-element list.
            List<string> ws = new List<string> { "ab", "cde", "f", "ghij" };
            Console.WriteLine(ws.Sum(s => s.Length));                 // 2+3+1+4 = 10
            Console.WriteLine(ws.Select(s => s.Length).Max());        // 4
            // string.Join needs a materialized array/List bound to a local (a List<T>
            // arriving as a direct call result isn't threaded).
            List<string> upper = ws.Where(s => s.Length >= 2).Select(s => s.ToUpper()).ToList();
            Console.WriteLine(string.Join("|", upper));              // AB|CDE|GHIJ

            // Empty-sequence FirstOrDefault on a reference element.
            List<string> empty = new List<string>();
            string? none = empty.FirstOrDefault();
            Console.WriteLine(none is null ? "null" : none);         // null

            return 0;
        }
    }
}
