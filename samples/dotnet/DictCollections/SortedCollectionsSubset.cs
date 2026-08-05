#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace SortedCollectionsSubset
{
    // SortedDictionary<K,V> / SortedSet<T> over the real System.Collections
    // red-black-tree IL (referenced via -r). The key was resolving Comparer<T>'s
    // subclass-vs-sentinel conflict: Comparer<T>.Default synthesizes a real
    // GenericComparer<T> (whose Compare uses x.CompareTo(y)), so the tree's
    // KeyValuePairComparer (a Comparer<T> subclass) and IComparer dispatch work.
    internal static class Program
    {
        // Comma-join an int sequence without a trailing separator (string.Join over
        // a non-List IEnumerable is a separate boundary, out of scope here).
        private static string Join(IEnumerable<int> xs)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (int x in xs)
            {
                if (!first) sb.Append(',');
                sb.Append(x);
                first = false;
            }
            return sb.ToString();
        }

        internal static int __GateEntry()
        {
            SortedDictionary<int, string> d = new SortedDictionary<int, string>();
            d[3] = "c";
            d[1] = "a";
            d[2] = "b";
            d[5] = "e";
            d[4] = "d";
            StringBuilder pairs = new StringBuilder();
            foreach (KeyValuePair<int, string> kv in d)
                pairs.Append(kv.Key).Append(':').Append(kv.Value).Append(';');
            Console.WriteLine(pairs.ToString());                  // 1:a;2:b;3:c;4:d;5:e;
            Console.WriteLine(d.Count);                           // 5
            Console.WriteLine(d.ContainsKey(4));                  // True
            Console.WriteLine(d.ContainsKey(9));                  // False
            d[2] = "B";                                           // overwrite
            Console.WriteLine(d[2]);                              // B
            d.Remove(3);
            Console.WriteLine(Join(d.Keys));                      // 1,2,4,5

            SortedDictionary<string, int> sd = new SortedDictionary<string, int>();
            sd["pear"] = 1;
            sd["apple"] = 2;
            sd["fig"] = 3;
            StringBuilder keys = new StringBuilder();
            foreach (string k in sd.Keys)
                keys.Append(k).Append(';');
            Console.WriteLine(keys.ToString());                   // apple;fig;pear;

            SortedSet<int> s = new SortedSet<int>();
            s.Add(8);
            s.Add(3);
            s.Add(8);   // dup ignored
            s.Add(1);
            s.Add(5);
            Console.WriteLine(Join(s));                           // 1,3,5,8
            Console.WriteLine(s.Count);                           // 4
            Console.WriteLine(s.Contains(5));                     // True
            Console.WriteLine(s.Min + "," + s.Max);              // 1,8

            return 0;
        }
    }
}
