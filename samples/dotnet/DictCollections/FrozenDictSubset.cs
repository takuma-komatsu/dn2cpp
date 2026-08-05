#nullable enable
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace FrozenDictSubset
{
    // FrozenDictionary over the real System.Collections.Immutable IL. A dictionary
    // this size skips the small-count linear specialization and builds a
    // FrozenHashTable, whose CalcNumBuckets reads HashHelpers.Primes (a static
    // ReadOnlySpan<int>) — the intercept this section exists to exercise. Lookups
    // and the key sum are order-independent, so the output diffs exactly against
    // real .NET regardless of the internal bucket arrangement.
    internal static class Program
    {
        internal static int __GateEntry()
        {
            var src = new Dictionary<int, string>();
            for (int i = 0; i < 30; i++)
            {
                int k = i * 7 + 1;
                src[k] = "v" + k;
            }
            FrozenDictionary<int, string> frozen = src.ToFrozenDictionary();

            Console.WriteLine(frozen.Count);            // 30
            Console.WriteLine(frozen[1]);               // v1
            Console.WriteLine(frozen[106]);             // v106 (15*7+1)
            Console.WriteLine(frozen[204]);             // v204 (29*7+1)
            Console.WriteLine(frozen.ContainsKey(204)); // True
            Console.WriteLine(frozen.ContainsKey(203)); // False
            Console.WriteLine(frozen.TryGetValue(1000, out var miss) + " " + (miss ?? "<null>")); // False <null> (1000 is not a key)

            int sum = 0;
            foreach (var k in frozen.Keys)
            {
                sum += k;
            }
            Console.WriteLine(sum);                     // 3075

            return 0;
        }
    }
}
