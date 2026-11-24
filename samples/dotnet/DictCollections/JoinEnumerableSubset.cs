#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace JoinEnumerableSubset
{
    // string.Join / string.Concat over a concrete
    // IEnumerable<T> collection that is neither a Dn2CppArray nor a List<T> backing
    // — a SortedSet<T>, a SortedDictionary Keys/Values view, or a Queue<T>. The
    // intrinsic emits an interface-enumeration loop (cast to IEnumerable<T>,
    // callvirt GetEnumerator/MoveNext/Current) accumulating into a StringBuilder,
    // exactly what a hand-written foreach would. The Sorted* types come from the
    // real System.Collections red-black-tree IL (passed via -r), so this also
    // exercises cross-assembly enumerator reachability. Diffed vs real .NET.
    internal static class Program
    {
        private sealed class Bytes : List<byte>, IEnumerable<byte>
        {
            IEnumerator<byte> IEnumerable<byte>.GetEnumerator() =>
                ((IEnumerable<byte>)new byte[] { 7, 255 }).GetEnumerator();
        }

        private static string JoinList(List<byte> values) => string.Join(':', values);

        internal static void RunUnsignedJoins()
        {
            Console.WriteLine("byte join=" + string.Join(", ", new byte[] { 0, 128, 255 })
                + "/" + JoinList(new Bytes { 1 }));
            Console.WriteLine("ushort join=" + string.Join('|', new ushort[] { 1, 32768, 65535 }));
        }

        private static IEnumerable<float> ChangeCulture(CultureInfo captured, CultureInfo original)
        {
            yield return -1.5f;
            CultureInfo.CurrentCulture = captured;
            yield return -2.5f;
            CultureInfo.CurrentCulture = original;
            yield return -3.5f;
        }

        internal static void RunSingleJoins()
        {
            var original = CultureInfo.CurrentCulture;
            var captured = (CultureInfo)original.Clone();
            captured.NumberFormat.NegativeSign = "~";
            var symbol = new string('?', 3);
            captured.NumberFormat.NaNSymbol = symbol;
            try
            {
                Console.WriteLine("float join=" + string.Join('|', ChangeCulture(captured, original)));
                CultureInfo.CurrentCulture = captured;
                Console.WriteLine("float singleton=" + ReferenceEquals(symbol, string.Concat(new[] { float.NaN })));
                captured.NumberFormat.NaNSymbol = string.Empty;
                Console.WriteLine("float empty=" + ReferenceEquals(string.Empty, string.Concat(new[] { float.NaN, float.NaN })));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        private readonly struct Label(string text) : IFormattable
        {
            public override string ToString() => text;
            string IFormattable.ToString(string format, IFormatProvider provider) => "formatted";
        }

        internal static void RunStructJoins()
        {
            var text = new string('x', 3);
            var label = new Label(text);
            Console.WriteLine("struct join=" + string.Join('|', new[] { label, default, label })
                + ", singleton=" + ReferenceEquals(text, string.Concat(new[] { label })));
        }

        internal static void __GateEntry()
        {
            // SortedSet<int>: value-element generic Join<int> + Concat<int>.
            SortedSet<int> si = new SortedSet<int> { 8, 3, 1, 5, 3 };
            Console.WriteLine(string.Join(",", si));      // 1,3,5,8
            Console.WriteLine(string.Join("-", si));      // 1-3-5-8
            Console.WriteLine(string.Concat(si));         // 1358

            // SortedSet<string>: reference-element non-generic Join + Concat.
            SortedSet<string> ss = new SortedSet<string> { "pear", "apple", "fig" };
            Console.WriteLine(string.Join("/", ss));      // apple/fig/pear
            Console.WriteLine(string.Concat(ss));         // applefigpear

            // SortedDictionary Keys (string -> non-generic Join) and Values
            // (int -> generic Join<int>) views over the tree.
            SortedDictionary<string, int> d = new SortedDictionary<string, int>();
            d["banana"] = 2;
            d["apple"] = 1;
            d["cherry"] = 3;
            Console.WriteLine(string.Join(",", d.Keys));    // apple,banana,cherry
            Console.WriteLine(string.Join(";", d.Values));  // 1;2;3

            // Queue<T>: another concrete IEnumerable<T> (FIFO order preserved).
            Queue<int> q = new Queue<int>();
            q.Enqueue(10);
            q.Enqueue(20);
            q.Enqueue(30);
            Console.WriteLine(string.Join("|", q));         // 10|20|30

            // A char separator (not just a string) over a concrete collection.
            Console.WriteLine(string.Join('+', si));        // 1+3+5+8

            // Bare IEnumerable<T> static type — the runtime value could be a raw
            // array (no managed interface map) OR a managed collection, so the
            // intrinsic discriminates at runtime by the array type-info: an array
            // takes the array Join/Concat helper, anything else enumerates.
            int[] arr = { 3, 1, 2 };
            IEnumerable<int> eArr = arr;                     // -> array branch
            Console.WriteLine(string.Join(",", eArr));      // 3,1,2
            Console.WriteLine(string.Concat(eArr));         // 312
            IEnumerable<int> eList = new List<int> { 7, 8, 9 };  // -> enumerate
            Console.WriteLine(string.Join("-", eList));     // 7-8-9
            IEnumerable<int> eSet = si;                      // SortedSet -> enumerate
            Console.WriteLine(string.Join("|", eSet));      // 1|3|5|8
            long[] longs = { 100, 200 };
            IEnumerable<long> eLong = longs;                 // ArrayN repr -> array branch
            Console.WriteLine(string.Join(";", eLong));     // 100;200
            string[] strs = { "x", "y" };
            IEnumerable<string> eStr = strs;                 // -> array branch
            Console.WriteLine(string.Join("/", eStr));      // x/y
            Console.WriteLine(string.Concat(eStr));         // xy
        }
    }
}
