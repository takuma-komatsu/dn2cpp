#nullable disable
using System;
using System.Linq;

namespace StringJoinSubset
{
    // string.Join over arrays: int[]/long[]/double[] bind to the generic
    // Join<T>(separator, IEnumerable<T>), string[]/object[] to the non-generic array
    // overload. Elements format like Object.ToString.
    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine(string.Join(",", new int[] { 1, 2, 3 }));          // 1,2,3
            Console.WriteLine(string.Join("-", new string[] { "a", "b", "c" })); // a-b-c
            Console.WriteLine(string.Join(",", new long[] { 10L, 20L }));        // 10,20
            Console.WriteLine(string.Join("|", new double[] { 0.5, 1.5 }));      // 0.5|1.5
            Console.WriteLine(string.Join(", ", new int[] { }));                 // (empty line)
            Console.WriteLine(string.Join('/', new string[] { "x", "y" }));      // x/y
            Console.WriteLine(string.Join(",", new int[] { 42 }));               // 42 (single, no sep)

            // A lazy LINQ enumerable: Select(...).OrderBy(...) is statically
            // IOrderedEnumerable<T>, not a concrete collection, so Join<T> takes the
            // bare-interface path and enumerates through the IEnumerable<T> map.
            int[] gens = { 5, 1, 3, 2, 4 };
            Console.WriteLine(string.Join(", ", gens.Select(g => g).OrderBy(g => g)));  // 1, 2, 3, 4, 5
            // OrderByDescending is still IOrderedEnumerable<T>; Where narrows first.
            Console.WriteLine(string.Join("-", gens.Where(g => g > 2).OrderByDescending(g => g))); // 5-4-3
            // Empty LINQ result — no separator emitted, empty line.
            Console.WriteLine(string.Join(", ", gens.Where(g => g > 100).OrderBy(g => g)));         // (empty line)
            // String elements, including a null (Join renders a null element as empty),
            // ordered — exercises the reference-element enumerate/format path.
            string[] names = { "bob", null, "alice" };
            Console.WriteLine(string.Join("|", names.OrderBy(s => s)));                 // |alice|bob
            // char separator over an IOrderedEnumerable<int>.
            Console.WriteLine(string.Join('/', gens.OrderBy(g => g)));                  // 1/2/3/4/5
        }
    }
}
