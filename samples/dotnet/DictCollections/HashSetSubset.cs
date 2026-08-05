#nullable enable
using System;
using System.Collections.Generic;

namespace HashSetSubset
{
    // inc 5: the real HashSet<T> from CoreLib — same HashHelpers + comparer
    // machinery as Dictionary, a different container. Exercises Add (dedup),
    // Contains, Remove, Count, and foreach, for an int set and a string set.
    internal static class Program
    {
        internal static int __GateEntry()
        {
            HashSet<int> seen = new HashSet<int>();
            int[] data = { 3, 1, 4, 1, 5, 9, 2, 6, 5, 3, 5 };
            int added = 0;
            foreach (int x in data)
            {
                if (seen.Add(x))
                {
                    added++;
                }
            }
            Console.WriteLine(added);          // 7 distinct: 3,1,4,5,9,2,6
            Console.WriteLine(seen.Count);     // 7
            Console.WriteLine(seen.Contains(4));   // True
            Console.WriteLine(seen.Contains(7));   // False
            Console.WriteLine(seen.Remove(4));     // True
            Console.WriteLine(seen.Remove(4));     // False (already gone)
            Console.WriteLine(seen.Count);     // 6

            int sum = 0;
            foreach (int x in seen)
            {
                sum += x;
            }
            Console.WriteLine(sum);            // 1+5+9+2+6+3 = 26

            HashSet<string> words = new HashSet<string>();
            words.Add("alpha");
            words.Add("beta");
            words.Add("alpha");
            Console.WriteLine(words.Count);    // 2
            Console.WriteLine(words.Contains(string.Concat("al", "pha"))); // True

            return 0;
        }
    }
}
