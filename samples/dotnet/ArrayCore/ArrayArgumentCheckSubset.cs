using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ArrayArgumentCheckSubset
{
    internal static class Program
    {
        private static void Probe(string label, Action action)
        {
            try
            {
                action();
                Console.WriteLine(label + ": no exception");
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name);
                Console.WriteLine("  message=" + e.Message.Replace("\r", "").Replace("\n", "|"));
            }
        }

        internal static void Run()
        {
            Console.WriteLine("-- array sort and reverse arguments --");
            int[] values = { 3, 1, 2 };
            int[] absent = null;
            string[] names = { "b", "a" };

            Probe("sort null", () => Array.Sort(absent));
            Probe("sort null with comparer", () => Array.Sort(absent, Comparer<int>.Default));
            Probe("sort comparison null", () => Array.Sort(values, (Comparison<int>)null));
            Probe("sort string comparison null", () => Array.Sort(names, (Comparison<string>)null));
            Probe("sort null before comparison", () => Array.Sort(absent, (Comparison<int>)null));
            Probe("sort range negative index", () => Array.Sort(values, -1, 2));
            Probe("sort range negative length", () => Array.Sort(values, 0, -1));
            Probe("sort range past end", () => Array.Sort(values, 2, 5));
            Probe("sort range null", () => Array.Sort(absent, 0, 1));
            Probe("sort range comparer past end", () => Array.Sort(values, 1, 3, Comparer<int>.Default));
            Probe("sort pair null keys", () => Array.Sort<int, int>(absent, values));
            Probe("sort pair short items", () => Array.Sort(values, new int[1]));
            Probe("sort pair range short items", () => Array.Sort(values, new int[2], 1, 2));
            Probe("sort pair range negative index", () => Array.Sort(values, new int[3], -1, 2));
            Probe("reverse null", () => Array.Reverse(absent));
            Probe("reverse negative index", () => Array.Reverse(values, -1, 1));
            Probe("reverse negative length", () => Array.Reverse(values, 0, -1));
            Probe("reverse past end", () => Array.Reverse(values, 2, 2));
            Probe("span sort comparison null", () => new Span<int>(values).Sort((Comparison<int>)null));
            Probe("span pair short items", () => new Span<int>(values).Sort(new Span<int>(new int[2])));
            Probe("span pair long items", () => new Span<int>(values).Sort(new Span<int>(new int[4])));
            Probe("span pair comparer short items", () => new Span<int>(values).Sort(new Span<int>(new int[2]), Comparer<int>.Default));
            Probe("span pair comparison null", () => new Span<int>(values).Sort(new Span<int>(new int[2]), (Comparison<int>)null));
            Probe("get subarray null", () => RuntimeHelpers.GetSubArray(absent, 0..1));
            Console.WriteLine("untouched by rejected calls: " + string.Join(",", values));
        }
    }
}
