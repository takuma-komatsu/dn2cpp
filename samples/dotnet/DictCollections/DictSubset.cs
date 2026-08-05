#nullable enable
using System;
using System.Collections.Generic;

namespace DictSubset
{
    // drive the *real* generic System.Collections.Generic.Dictionary<K,V>
    // from System.Private.CoreLib IL. The hard part is key hashing + equality,
    // which the real type routes through EqualityComparer<TKey>.Default.
    internal static class Program
    {
        internal static int __GateEntry()
        {
            Dictionary<int, int> squares = new Dictionary<int, int>();
            for (int i = 1; i <= 8; i++)
            {
                squares[i] = i * i;            // set_Item -> TryInsert (resize too)
            }

            Console.WriteLine(squares.Count);  // 8
            Console.WriteLine(squares[5]);     // 25 (get_Item -> FindValue)
            squares[5] = 100;                  // overwrite existing key
            Console.WriteLine(squares[5]);     // 100

            Console.WriteLine(squares.ContainsKey(3));  // True
            Console.WriteLine(squares.ContainsKey(99)); // False

            int got;
            bool found = squares.TryGetValue(7, out got);
            Console.WriteLine(found);          // True
            Console.WriteLine(got);            // 49

            bool removed = squares.Remove(2);
            Console.WriteLine(removed);        // True
            Console.WriteLine(squares.Count);  // 7

            return 0;
        }
    }
}
