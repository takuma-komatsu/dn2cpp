#nullable enable
using System;
using System.Collections;

namespace NonGenericSortedListSubset
{
    // The int-keyed NON-generic System.Collections.SortedList (in
    // System.Collections.NonGeneric, referenced via -r). Add/IndexOfKey/GetByIndex/ContainsKey,
    // the indexer, and key-order enumeration all route through
    // Array.BinarySearch(keys, 0, size, key, Comparer.Default) over BOXED INT keys — the same
    // System.Collections.Comparer.Default-over-boxed-primitive path ArrayList.Sort() over boxed ints
    // uses. Before the Comparer.Compare body intercept the real Comparer.Default did `x as
    // IComparable` and threw "At least one object must implement IComparable" on a boxed int; the
    // intercept lowers it to dn2cpp_object_compare, which orders boxed ints inline, so the whole
    // SortedList works and matches real .NET. Keys are distinct.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            var sl = new SortedList();
            sl.Add(30, "c");
            sl.Add(10, "a");
            sl.Add(20, "b");
            sl.Add(40, "d");
            sl.Add(25, "x");

            Console.WriteLine("nongeneric-sortedlist:");
            Console.WriteLine(sl.Count);                 // 5
            Console.WriteLine(sl.IndexOfKey(20));        // 1  (sorted keys: 10,20,25,30,40)
            Console.WriteLine((string)sl.GetByIndex(1)); // b
            Console.WriteLine(sl.ContainsKey(25));       // True
            Console.WriteLine(sl.ContainsKey(99));       // False
            Console.WriteLine((string)sl[30]);           // c   (indexer -> BinarySearch over keys)
            Console.WriteLine(sl.IndexOfKey(99));        // -1  (~insertion complement, not found)
            for (int i = 0; i < sl.Count; i++)
            {
                Console.Write((int)sl.GetKey(i));
                Console.Write("=");
                Console.WriteLine((string)sl.GetByIndex(i)); // 10=a 20=b 25=x 30=c 40=d
            }
        }
    }
}
