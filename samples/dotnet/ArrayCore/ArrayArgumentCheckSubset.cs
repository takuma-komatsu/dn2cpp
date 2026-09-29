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

        private static void Check(string label, Func<int> call)
        {
            try
            {
                Console.WriteLine(label + ": " + call());
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name);
                Console.WriteLine("  message=" + e.Message.Replace("\r", "").Replace("\n", "|"));
            }
        }

        private static void Check(string label, Action call) => Check(label, () => { call(); return 0; });

        // The searches and fills the emitter lowers to inline loops, generic and over
        // System.Array: a bad range must be refused before the loop reads or writes
        // past the array. The tail pins the text of the System.Array Sort, Reverse and
        // Copy faults.
        internal static void RunSearch()
        {
            Console.WriteLine("-- array search and fill argument checks --");
            int[] a = { 5, 6, 7 };
            int[] sorted = { 1, 2, 3 };
            int[] fill = { 1, 2, 3 };
            int[] none = null;
            int[] empty = new int[0];
            string[] names = { "x", "y" };
            long[] wide = { 8, 9 };
            int[,] grid = new int[2, 2];
            Array bare = null;
            Array boxed = a;
            Array boxedSorted = sorted;
            System.Collections.IComparer plain = System.Collections.Comparer.Default;

            Check("indexof null", () => Array.IndexOf(none, 0));
            Check("indexof start null", () => Array.IndexOf(none, 0, 0));
            Check("indexof range null", () => Array.IndexOf(none, 0, 0, 0));
            Check("indexof negative start", () => Array.IndexOf(a, 6, -1));
            Check("indexof start past end", () => Array.IndexOf(a, 6, 4));
            Check("indexof start at end", () => Array.IndexOf(a, 6, 3));
            Check("indexof negative count", () => Array.IndexOf(a, 6, 0, -1));
            Check("indexof count past end", () => Array.IndexOf(new int[3], 0, 1, 10));
            Check("indexof string count past end", () => Array.IndexOf(names, "y", 1, 5));
            Check("indexof long count past end", () => Array.IndexOf(wide, 9L, 1, 5));
            Check("indexof range", () => Array.IndexOf(a, 7, 1, 2));

            Check("lastindexof null", () => Array.LastIndexOf(none, 0));
            Check("lastindexof start null", () => Array.LastIndexOf(none, 0, 0));
            Check("lastindexof range null", () => Array.LastIndexOf(none, 0, 0, 0));
            Check("lastindexof empty", () => Array.LastIndexOf(empty, 0));
            Check("lastindexof empty start 0", () => Array.LastIndexOf(empty, 0, 0));
            Check("lastindexof empty start -1", () => Array.LastIndexOf(empty, 0, -1));
            Check("lastindexof empty start 1", () => Array.LastIndexOf(empty, 0, 1));
            Check("lastindexof empty count 1", () => Array.LastIndexOf(empty, 0, 0, 1));
            Check("lastindexof empty start -1 count 0", () => Array.LastIndexOf(empty, 0, -1, 0));
            Check("lastindexof start past end", () => Array.LastIndexOf(a, 6, 3));
            Check("lastindexof negative start", () => Array.LastIndexOf(a, 6, -1));
            Check("lastindexof negative count", () => Array.LastIndexOf(a, 6, 2, -1));
            Check("lastindexof count past start", () => Array.LastIndexOf(a, 6, 1, 3));
            Check("lastindexof string count past start", () => Array.LastIndexOf(names, "x", 0, 2));
            Check("lastindexof long count past start", () => Array.LastIndexOf(wide, 8L, 1, 3));
            Check("lastindexof range", () => Array.LastIndexOf(a, 5, 1, 2));

            Check("fill null", () => Array.Fill(none, 7));
            Check("fill range null", () => Array.Fill(none, 7, 0, 0));
            Check("fill negative start", () => Array.Fill(fill, 7, -1, 1));
            Check("fill start past end", () => Array.Fill(fill, 7, 4, 0));
            Check("fill negative count", () => Array.Fill(fill, 7, 0, -1));
            Check("fill count past end", () => Array.Fill(fill, 7, 2, 5));
            Check("fill string count past end", () => Array.Fill(names, "z", 1, 2));
            Check("fill long count past end", () => Array.Fill(wide, 1L, 1, 2));
            Console.WriteLine("untouched by the rejected fills: " + string.Join(",", fill)
                + " " + string.Join(",", names) + " " + string.Join(",", wide));
            Check("fill range", () => Array.Fill(fill, 7, 1, 2));
            Check("fill empty range at end", () => Array.Fill(fill, 9, 3, 0));
            Console.WriteLine("filled: " + string.Join(",", fill));

            Check("binarysearch null", () => Array.BinarySearch(none, 5));
            Check("binarysearch comparer null", () => Array.BinarySearch(none, 5, Comparer<int>.Default));
            Check("binarysearch range null", () => Array.BinarySearch(none, 0, 0, 5));
            Check("binarysearch negative index", () => Array.BinarySearch(sorted, -1, 1, 2));
            Check("binarysearch negative length", () => Array.BinarySearch(sorted, 0, -1, 2));
            Check("binarysearch past end", () => Array.BinarySearch(sorted, 1, 5, 9));
            Check("binarysearch comparer past end", () => Array.BinarySearch(sorted, 2, 2, 9, Comparer<int>.Default));
            Check("binarysearch string past end", () => Array.BinarySearch(names, 1, 2, "y"));
            Check("binarysearch range", () => Array.BinarySearch(sorted, 1, 2, 3));
            Check("binarysearch range miss", () => Array.BinarySearch(sorted, 0, 2, 3));

            Check("Array indexof null", () => Array.IndexOf(bare, 6));
            Check("Array indexof start null", () => Array.IndexOf(bare, 6, 0));
            Check("Array indexof range null", () => Array.IndexOf(bare, 6, 0, 0));
            Check("Array indexof rank", () => Array.IndexOf(grid, 0));
            Check("Array indexof start rank", () => Array.IndexOf(grid, 0, 9));
            Check("Array indexof range rank", () => Array.IndexOf(grid, 0, 0, 9));
            Check("Array indexof negative start", () => Array.IndexOf(boxed, 6, -1));
            Check("Array indexof start past end", () => Array.IndexOf(boxed, 6, 4));
            Check("Array indexof negative count", () => Array.IndexOf(boxed, 6, 0, -1));
            Check("Array indexof count past end", () => Array.IndexOf(boxed, 6, 1, 10));
            Check("Array indexof objects count past end", () => Array.IndexOf((Array)names, "y", 1, 5));
            Check("Array indexof range", () => Array.IndexOf(boxed, 7, 1, 2));

            Check("Array lastindexof null", () => Array.LastIndexOf(bare, 6));
            Check("Array lastindexof start null", () => Array.LastIndexOf(bare, 6, 0));
            Check("Array lastindexof range null", () => Array.LastIndexOf(bare, 6, 0, 0));
            Check("Array lastindexof empty", () => Array.LastIndexOf((Array)empty, 6, 5, 9));
            Check("Array lastindexof rank", () => Array.LastIndexOf(grid, 0));
            Check("Array lastindexof start past end rank", () => Array.LastIndexOf(grid, 0, 4));
            Check("Array lastindexof start past end", () => Array.LastIndexOf(boxed, 6, 3));
            Check("Array lastindexof negative start", () => Array.LastIndexOf(boxed, 6, -1));
            Check("Array lastindexof negative count", () => Array.LastIndexOf(boxed, 6, 2, -1));
            Check("Array lastindexof count past start", () => Array.LastIndexOf(boxed, 6, 1, 3));
            Check("Array lastindexof range", () => Array.LastIndexOf(boxed, 5, 1, 2));

            Check("Array binarysearch null", () => Array.BinarySearch(bare, 2));
            Check("Array binarysearch comparer null", () => Array.BinarySearch(bare, 2, plain));
            Check("Array binarysearch range null", () => Array.BinarySearch(bare, 0, 0, 2));
            Check("Array binarysearch range comparer null", () => Array.BinarySearch(bare, 0, 0, 2, plain));
            Check("Array binarysearch rank", () => Array.BinarySearch(grid, 0));
            Check("Array binarysearch past end rank", () => Array.BinarySearch(grid, 0, 5, 0));
            Check("Array binarysearch negative index", () => Array.BinarySearch(boxedSorted, -1, 1, 2));
            Check("Array binarysearch negative length", () => Array.BinarySearch(boxedSorted, 0, -1, 2));
            Check("Array binarysearch past end", () => Array.BinarySearch(boxedSorted, 1, 5, 9));
            Check("Array binarysearch comparer past end", () => Array.BinarySearch(boxedSorted, 2, 2, 9, plain));
            Check("Array binarysearch range", () => Array.BinarySearch(boxedSorted, 1, 2, 3));

            Check("Array sort rank", () => Array.Sort(grid));
            Check("Array sort past end", () => Array.Sort(boxed, 1, 5));
            Check("Array reverse rank", () => Array.Reverse(grid));
            Check("Array reverse past end", () => Array.Reverse(boxed, 2, 2));
            Check("Array reverse empty", () => Array.Reverse((Array)empty));
            Check("Array reverse empty end", () => Array.Reverse(boxed, 3, 0));
            Check("Array copy rank mismatch", () => Array.Copy(grid, a, 1));
            Check("copy to a grid", () => Array.Copy(a, grid, 1));
            Console.WriteLine("untouched by the rejected searches: " + string.Join(",", a)
                + " " + string.Join(",", sorted));
        }
    }
}
