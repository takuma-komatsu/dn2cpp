#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqGroupSubset
{
    // LINQ GroupBy bound to the real System.Linq. Exercises the
    // IGrouping<out TKey, TElement> covariant interface + the Grouping class
    // (Dictionary-backed, first-appearance order). Groups are enumerated through
    // the IGrouping interface (g.Key + foreach/ToList over the group). Sources are
    // List<T>; group members materialized to a List local before string.Join (the
    // call-result boundary). Keys are string/int — char Dictionary keys hit a
    // separate pre-existing gap (box-of-char), unrelated to GroupBy.
    internal static class Program
    {
        internal static int Run()
        {
            List<string> words = new List<string>
            {
                "apple", "banana", "avocado", "cherry", "blueberry", "apricot"
            };

            // Key-only overload: group by first letter (string key).
            foreach (IGrouping<string, string> g in words.GroupBy(w => w.Substring(0, 1)))
            {
                List<string> members = g.ToList();
                Console.WriteLine(g.Key + ":" + string.Join(",", members));
            }
            // a:apple,avocado,apricot
            // b:banana,blueberry
            // c:cherry

            // Key + element-selector overload: group by parity, project to *10.
            List<int> nums = new List<int> { 1, 2, 3, 4, 5, 6 };
            foreach (IGrouping<int, int> g in nums.GroupBy(n => n % 2, n => n * 10))
            {
                List<int> members = g.ToList();
                Console.WriteLine(g.Key + ":" + string.Join(",", members));
            }
            // 1:10,30,50
            // 0:20,40,60

            // Lazy GroupBy result consumed by another shim operator.
            int groupCount = words.GroupBy(w => w.Substring(0, 1)).Count();
            Console.WriteLine(groupCount);                                  // 3

            return 0;
        }
    }
}
