#nullable enable
using System;
using System.Collections.Generic;

namespace DictApiSubset
{
    // inc 6: rounding out the real Dictionary<K,V> API surface — collection
    // initializer, TryAdd, Remove(out), ContainsValue, Clear, and a missing-key
    // indexer raising KeyNotFoundException.
    internal static class Program
    {
        internal static int __GateEntry()
        {
            // collection initializer -> Add for each pair
            Dictionary<string, int> d = new Dictionary<string, int>
            {
                { "a", 1 },
                { "b", 2 },
                { "c", 3 },
            };
            Console.WriteLine(d.Count);                 // 3

            Console.WriteLine(d.TryAdd("d", 4));        // True (new)
            Console.WriteLine(d.TryAdd("a", 99));       // False (exists)
            Console.WriteLine(d["d"]);                  // 4
            Console.WriteLine(d["a"]);                  // 1 (unchanged)

            Console.WriteLine(d.ContainsValue(3));      // True
            Console.WriteLine(d.ContainsValue(42));     // False

            int removed;
            bool ok = d.Remove("b", out removed);
            Console.WriteLine(ok);                       // True
            Console.WriteLine(removed);                  // 2
            Console.WriteLine(d.Count);                 // 3

            bool threw = false;
            try
            {
                int missing = d["zzz"];                  // KeyNotFoundException
                Console.WriteLine(missing);
            }
            catch (KeyNotFoundException)
            {
                threw = true;
            }
            Console.WriteLine(threw);                    // True

            d.Clear();
            Console.WriteLine(d.Count);                 // 0
            Console.WriteLine(d.ContainsKey("a"));      // False

            return 0;
        }
    }
}
