#nullable enable
using System;
using System.Collections.Generic;

namespace DictEnumSubset
{
    // inc 3: enumerate the *real* Dictionary<K,V> from CoreLib IL — the
    // struct Dictionary.Enumerator yielding KeyValuePair<K,V>, plus the Keys and
    // Values collections. Order-independent sums keep the test robust.
    internal static class Program
    {
        internal static int __GateEntry()
        {
            Dictionary<int, int> squares = new Dictionary<int, int>();
            for (int i = 1; i <= 6; i++)
            {
                squares[i] = i * i;
            }
            squares.Remove(4); // leave a hole in the entries array

            int keySum = 0;
            int valSum = 0;
            foreach (KeyValuePair<int, int> kv in squares)
            {
                keySum += kv.Key;
                valSum += kv.Value;
            }
            Console.WriteLine(keySum);   // 1+2+3+5+6 = 17
            Console.WriteLine(valSum);   // 1+4+9+25+36 = 75

            int ksum2 = 0;
            foreach (int k in squares.Keys)
            {
                ksum2 += k;
            }
            Console.WriteLine(ksum2);    // 17

            int vsum2 = 0;
            foreach (int v in squares.Values)
            {
                vsum2 += v;
            }
            Console.WriteLine(vsum2);    // 75

            Console.WriteLine(squares.Count); // 5

            return 0;
        }
    }
}
