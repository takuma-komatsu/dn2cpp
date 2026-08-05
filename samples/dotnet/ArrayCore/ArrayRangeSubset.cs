#nullable disable
using System;
using System.Collections.Generic;

namespace ArrayRangeSubset
{
    // the (array, index, length) range overloads of Array.Sort/Reverse, plus
    // the trailing-IComparer overloads (the comparer is ignored — natural/ordinal
    // order). This unblocks List<T>.Reverse, which calls Array.Reverse over a range.
    internal static class Program
    {
        private static void Dump(int[] a)
        {
            string s = "";
            foreach (int x in a)
            {
                s += x + " ";
            }
            Console.WriteLine(s);
        }

        internal static void Run()
        {
            int[] a = { 5, 3, 8, 1, 9, 2 };
            Array.Sort(a, 1, 3);    // sort [3,8,1] -> [1,3,8]
            Dump(a);                // 5 1 3 8 9 2

            Array.Reverse(a, 0, 3); // reverse [5,1,3] -> [3,1,5]
            Dump(a);                // 3 1 5 8 9 2

            string[] s = { "d", "b", "c", "a" };
            Array.Sort(s, 1, 3);    // sort ["b","c","a"] -> ["a","b","c"]
            Console.WriteLine(s[0] + s[1] + s[2] + s[3]); // dabc

            // List<T>.Reverse routes through Array.Reverse(array, index, length).
            var l = new List<int> { 3, 1, 2 };
            l.Reverse();
            Console.WriteLine(l[0] + "," + l[1] + "," + l[2]); // 2,1,3
        }
    }
}
