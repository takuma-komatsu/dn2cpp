#nullable enable
using System;
using System.Collections.Generic;

namespace DictMoreSubset
{
    internal enum Color { Red = 1, Green = 2, Blue = 4 }

    // inc 4: more key shapes on the real Dictionary<K,V> — a capacity ctor,
    // Add (vs the indexer), a 64-bit key (long), and an enum key. Exercises the
    // long GetHashCode fold and the enum constrained GetHashCode / comparer path.
    internal static class Program
    {
        internal static int __GateEntry()
        {
            // long key + capacity ctor + Add
            Dictionary<long, int> big = new Dictionary<long, int>(8);
            big.Add(1L << 40, 11);            // Add (throws on dup, unlike indexer)
            big.Add((1L << 40) + 1, 22);
            big.Add(5_000_000_000L, 33);
            Console.WriteLine(big.Count);                  // 3
            Console.WriteLine(big[1L << 40]);              // 11
            Console.WriteLine(big[5_000_000_000L]);        // 33
            Console.WriteLine(big.ContainsKey((1L << 40) + 1)); // True
            Console.WriteLine(big.ContainsKey(7L));        // False

            bool threw = false;
            try
            {
                big.Add(1L << 40, 99);        // duplicate key -> ArgumentException
            }
            catch (ArgumentException)
            {
                threw = true;
            }
            Console.WriteLine(threw);                       // True

            // enum key
            Dictionary<Color, string> names = new Dictionary<Color, string>();
            names[Color.Red] = "red";
            names[Color.Green] = "green";
            names[Color.Blue] = "blue";
            Console.WriteLine(names[Color.Green]);          // green
            Console.WriteLine(names.ContainsKey(Color.Blue)); // True
            Console.WriteLine(names.Count);                // 3

            return 0;
        }
    }
}
