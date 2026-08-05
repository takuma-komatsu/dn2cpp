#nullable disable
using System;

namespace ArraySortSubset
{
    // Array.Sort<T>(T[]) and Array.Reverse<T>(T[]), in place. Sort supports
    // int/long/double/string elements (ordinal for string); Reverse is element-kind
    // agnostic (int / reference / element-sized). Elements are printed one per line
    // (typed Console.WriteLine) to avoid value+string concatenation.
    internal static class Program
    {
        private static void DumpInts(int[] a)
        {
            foreach (int x in a)
            {
                Console.WriteLine(x);
            }
        }

        internal static void Run()
        {
            int[] a = { 5, 3, 8, 1, 9, 2 };
            Array.Sort(a);
            DumpInts(a); // 1 2 3 5 8 9
            Array.Reverse(a);
            DumpInts(a); // 9 8 5 3 2 1

            double[] d = { 2.5, 0.5, 1.5 };
            Array.Sort(d);
            foreach (double x in d)
            {
                Console.WriteLine(x); // 0.5 1.5 2.5
            }

            long[] l = { 30L, 10L, 20L };
            Array.Sort(l);
            foreach (long x in l)
            {
                Console.WriteLine(x); // 10 20 30
            }

            string[] s = { "banana", "apple", "cherry" };
            Array.Sort(s);
            foreach (string x in s)
            {
                Console.WriteLine(x); // apple banana cherry
            }

            // Reverse over a reference array (Dn2CppArrayRef).
            string[] r = { "x", "y", "z" };
            Array.Reverse(r);
            foreach (string x in r)
            {
                Console.WriteLine(x); // z y x
            }
        }
    }
}
