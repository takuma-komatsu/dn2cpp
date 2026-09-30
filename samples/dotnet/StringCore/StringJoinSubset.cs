#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            // IOrderedEnumerable<T>, not a concrete collection, so Join<T> enumerates
            // through the IEnumerable<T> map.
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

        // string.Join and string.Concat over IEnumerable<string>: a null sequence
        // throws ArgumentNullException, Concat(object) formats its argument, never the
        // argument's elements, and the enumerator is disposed once the join ends. A null
        // array handed to the array overloads of Join, Concat and AppendJoin throws
        // ArgumentNullException naming .NET's parameter.
        internal static void RunSequences()
        {
            Console.WriteLine("== string sequences ==");
            List<string> noList = null;
            IEnumerable<string> noSeq = null;
            HashSet<string> noSet = null;
            Report("join list", () => string.Join(",", noList));
            Report("join ienum", () => string.Join(',', noSeq));
            Report("join set", () => string.Join(",", noSet));
            Report("concat list", () => string.Concat(noList));
            Report("concat ienum", () => string.Concat(noSeq));
            Report("concat set", () => string.Concat(noSet));
            Console.WriteLine("concat object list: " + string.Concat((object)new List<string> { "a", "b" }));
            Console.WriteLine("concat object: " + string.Concat(new Tag()) + " "
                + string.Concat((object)new HashSet<string> { "x" }));
            Console.WriteLine("disposed: " + string.Join("+", new Words()) + " "
                + string.Concat(new Words()) + " " + Words.Disposals);
            string[] noStrings = null;
            object[] noObjects = null;
            Report("join strings", () => string.Join(",", noStrings));
            Report("join objects", () => string.Join('-', noObjects));
            Report("join slice", () => string.Join(",", noStrings, 0, 0));
            Report("concat strings", () => string.Concat(noStrings));
            Report("concat objects", () => string.Concat(noObjects));
            Report("append strings", () => new StringBuilder().AppendJoin(';', noStrings).ToString());
            Report("append objects", () => new StringBuilder().AppendJoin(",", noObjects).ToString());
        }

        // Join, Concat and AppendJoin take an IEnumerable<T> operand of any static type: a
        // null literal throws ArgumentNullException when the call runs, a conditional joins
        // whichever of an array and a list it picked, and a LINQ grouping or a set seen
        // through ISet<T> or IReadOnlySet<T> enumerates through the interface.
        internal static void RunOperandShapes()
        {
            Console.WriteLine("== string sequence operands ==");
            Report("concat null", () => string.Concat((IEnumerable<string>)null));
            Report("join null", () => string.Join(",", (IEnumerable<string>)null));
            Report("join null ints", () => string.Join(",", (IEnumerable<int>)null));
            Report("concat null ints", () => string.Concat((IEnumerable<int>)null));
            Report("append null", () => new StringBuilder().AppendJoin(",", (IEnumerable<string>)null).ToString());
            string[] arr = { "a", "b" };
            var list = new List<string> { "c", "d" };
            int[] ints = { 1, 2 };
            var intList = new List<int> { 3, 4 };
            foreach (bool first in new[] { true, false })
                Console.WriteLine("either: " + Either(first, arr, list) + " " + EitherConcat(first, arr, list) + " "
                    + EitherInts(first, ints, intList));
            foreach (var g in new[] { "apple", "avocado", "banana" }.GroupBy(w => w[0]))
                Console.WriteLine("group " + g.Key + ": " + string.Join("/", g) + " " + string.Concat(g));
            foreach (var g in new[] { 1, 2, 3, 4, 5 }.GroupBy(i => i % 2))
                Console.WriteLine("group " + g.Key + ": " + string.Join(';', g) + " " + string.Concat(g));
            ISet<string> set = new HashSet<string> { "x", "y" };
            IReadOnlySet<string> readOnly = new HashSet<string> { "p", "q" };
            ISet<int> intSet = new HashSet<int> { 7, 8 };
            IReadOnlySet<int> intReadOnly = new HashSet<int> { 9 };
            Console.WriteLine("sets: " + string.Join(",", set) + " " + string.Concat(readOnly) + " "
                + string.Join('|', intSet) + " " + string.Concat(intReadOnly) + " "
                + new StringBuilder().AppendJoin('+', readOnly));
        }

        private static string Either(bool first, string[] arr, List<string> list) =>
            string.Join(',', first ? (IEnumerable<string>)arr : list);

        private static string EitherConcat(bool first, string[] arr, List<string> list) =>
            string.Concat(first ? (IEnumerable<string>)arr : list);

        private static string EitherInts(bool first, int[] ints, List<int> list) =>
            string.Join("-", first ? (IEnumerable<int>)ints : list);

        private static void Report(string what, Func<string> join)
        {
            try
            {
                Console.WriteLine(what + ": " + join());
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine(what + ": " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }

    internal sealed class Tag
    {
        public override string ToString() => "tag";
    }

    // Counts enumerator disposals; yields "w1", null, "w3".
    internal sealed class Words : IEnumerable<string>
    {
        internal static int Disposals;

        public IEnumerator<string> GetEnumerator() => new Cursor();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Cursor : IEnumerator<string>
        {
            private int _index;

            public string Current => _index == 2 ? null : "w" + _index;

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index <= 3;

            public void Reset() => _index = 0;

            public void Dispose() => Disposals++;
        }
    }
}
