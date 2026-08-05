#nullable disable
using System;
using System.Collections.Generic;

namespace ListSortStringSubset
{
    // sorting reference-element (string) collections via the real BCL introsort
    // now works. The Span<T> ctor's array-covariance guard (array.GetType !=
    // typeof(T[])) used to false-positive for ref types — typeof(string[]) was null —
    // and throw; typeof(refT[]) now reports the shared array_ref_type that every ref
    // array carries, so the guard passes. Covers Comparison<string> (List + Array),
    // IComparer<string> (the callback path), and the natural string sort.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            var a = new List<string> { "banana", "apple", "cherry", "date" };
            a.Sort((x, y) => string.CompareOrdinal(x, y));
            Console.WriteLine(string.Join(",", a));        // apple,banana,cherry,date

            a.Sort((x, y) => string.CompareOrdinal(y, x)); // reverse
            Console.WriteLine(string.Join(",", a));        // date,cherry,banana,apple

            // sort by length (distinct lengths -> determined order)
            var b = new List<string> { "ccc", "a", "dddd", "bb" };
            b.Sort((x, y) => x.Length - y.Length);
            Console.WriteLine(string.Join(",", b));        // a,bb,ccc,dddd

            string[] arr = { "pear", "fig", "kiwi", "apple" };
            Array.Sort(arr, (x, y) => string.CompareOrdinal(x, y));
            Console.WriteLine(string.Join(",", arr));      // apple,fig,kiwi,pear

            var c = new List<string> { "Bb", "aa", "Cc" };
            c.Sort(StringComparer.Ordinal);                // IComparer<string>
            Console.WriteLine(string.Join(",", c));        // Bb,Cc,aa

            var d = new List<string> { "pear", "fig", "kiwi" };
            d.Sort();                                       // natural
            Console.WriteLine(string.Join(",", d));        // fig,kiwi,pear
        }
    }
}
