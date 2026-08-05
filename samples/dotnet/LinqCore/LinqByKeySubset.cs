#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqByKeySubset
{
    // finish LINQ — MinBy/MaxBy, Order/OrderDescending, UnionBy/IntersectBy/
    // ExceptBy, SequenceEqual, Chunk, LongCount, Aggregate(seed,func,resultSelector).
    // Materialized sequences are bound to a List local before string.Join (the
    // call-result boundary); Chunk yields a real int[], joined directly.
    internal static class Program
    {
        internal static int Run()
        {
            List<string> words = new List<string> { "pear", "apple", "kiwi", "banana", "fig" };

            Console.WriteLine(words.MinBy(w => w.Length));         // fig (len 3)
            Console.WriteLine(words.MaxBy(w => w.Length));         // banana (len 6)

            List<int> nums = new List<int> { 5, 2, 8, 2, 1 };
            List<int> ordered = nums.Order().ToList();
            Console.WriteLine(string.Join(",", ordered));          // 1,2,2,5,8
            List<int> orderedDesc = nums.OrderDescending().ToList();
            Console.WriteLine(string.Join(",", orderedDesc));      // 8,5,2,2,1

            List<string> a = new List<string> { "apple", "avocado", "banana" };
            List<string> b = new List<string> { "cherry", "blueberry", "apricot" };
            List<string> unioned = a.UnionBy(b, w => w[0].ToString()).ToList();
            Console.WriteLine(string.Join(",", unioned));          // apple,banana,cherry
            List<string> keys = new List<string> { "a", "b" };
            List<string> intersected = a.IntersectBy(keys, w => w[0].ToString()).ToList();
            Console.WriteLine(string.Join(",", intersected));      // apple,banana
            List<string> ex = new List<string> { "a" };
            List<string> excepted = a.ExceptBy(ex, w => w[0].ToString()).ToList();
            Console.WriteLine(string.Join(",", excepted));         // banana

            List<int> s1 = new List<int> { 1, 2, 3 };
            List<int> s2 = new List<int> { 1, 2, 3 };
            List<int> s3 = new List<int> { 1, 2, 4 };
            Console.WriteLine(s1.SequenceEqual(s2));               // True
            Console.WriteLine(s1.SequenceEqual(s3));               // False

            List<int> ten = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
            foreach (int[] chunk in ten.Chunk(3))
                Console.WriteLine(string.Join("-", chunk));        // 1-2-3 / 4-5-6 / 7

            Console.WriteLine(words.LongCount());                  // 5
            int total = nums.Aggregate(0, (acc, n) => acc + n, acc => acc * 2); // (18)*2 = 36
            Console.WriteLine(total);

            return 0;
        }
    }
}
