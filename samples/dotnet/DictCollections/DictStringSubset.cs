#nullable enable
using System;
using System.Collections.Generic;

namespace DictStringSubset
{
    // inc 2: drive the *real* Dictionary<string,int> from CoreLib IL. A
    // reference key routes hashing/equality through the IEqualityComparer<string>
    // interface on _comparer (not the value-type devirtualized path), so this
    // exercises interface-comparer devirtualization + string content hashing.
    internal static class Program
    {
        internal static int __GateEntry()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            string[] words = { "alpha", "beta", "alpha", "gamma", "beta", "alpha" };
            foreach (string w in words)
            {
                counts.TryGetValue(w, out int n);
                counts[w] = n + 1;            // set_Item -> TryInsert (+ resize)
            }

            Console.WriteLine(counts.Count);          // 3
            Console.WriteLine(counts["alpha"]);       // 3
            Console.WriteLine(counts["beta"]);        // 2
            Console.WriteLine(counts["gamma"]);       // 1

            Console.WriteLine(counts.ContainsKey("beta"));   // True
            Console.WriteLine(counts.ContainsKey("delta"));  // False

            // A freshly built key (not the literal) must hash/compare by content.
            string probe = string.Concat("al", "pha");
            Console.WriteLine(counts.ContainsKey(probe));     // True
            Console.WriteLine(counts[probe]);                 // 3

            bool removed = counts.Remove("beta");
            Console.WriteLine(removed);                        // True
            Console.WriteLine(counts.Count);                  // 2

            return 0;
        }
    }
}
