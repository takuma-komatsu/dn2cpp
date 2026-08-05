#nullable disable
using System;
using System.Collections.Generic;
using System.Text;

namespace AppendJoinSubset
{
    // StringBuilder.AppendJoin: the string[]/object[] array forms (string or char
    // separator; null elements contribute nothing) and the generic
    // AppendJoin<T>(sep, IEnumerable<T>) forms over arrays and a List<T>.
    internal static class Program
    {
        internal static void Run()
        {
            // string separator + object[] / string[]
            var sb = new StringBuilder("J:");
            sb.AppendJoin(", ", new object[] { 1, "two", 3.5, null });
            Console.WriteLine(sb.ToString()); // J:1, two, 3.5,

            var s2 = new StringBuilder();
            s2.AppendJoin("-", new string[] { "a", null, "c" }).Append('!');
            Console.WriteLine(s2.ToString()); // a--c!

            // char separator + object[] / string[]
            var c1 = new StringBuilder("<");
            c1.AppendJoin('|', new object[] { true, 7L });
            Console.WriteLine(c1.ToString()); // <True|7

            var c2 = new StringBuilder();
            c2.AppendJoin('/', new string[] { "x", "y", "z" });
            Console.WriteLine(c2.ToString()); // x/y/z

            // generic AppendJoin<T> over value-element arrays (string and char sep)
            var g1 = new StringBuilder("ints:");
            g1.AppendJoin(",", new int[] { 1, -2, 3 });
            Console.WriteLine(g1.ToString()); // ints:1,-2,3

            var g2 = new StringBuilder();
            g2.AppendJoin('+', new long[] { 10L, 20L }).Append('=').Append(30);
            Console.WriteLine(g2.ToString()); // 10+20=30

            var g3 = new StringBuilder();
            g3.AppendJoin("; ", new double[] { 1.5, -0.25 });
            Console.WriteLine(g3.ToString()); // 1.5; -0.25

            var g4 = new StringBuilder();
            g4.AppendJoin("", new char[] { 'd', 'n' });
            Console.WriteLine(g4.ToString()); // dn

            var g5 = new StringBuilder();
            g5.AppendJoin(",", new uint[] { uint.MaxValue, 1u });
            Console.WriteLine(g5.ToString()); // 4294967295,1

            // generic AppendJoin<T> over a List<T>
            var li = new List<int> { 4, 5, 6 };
            var g6 = new StringBuilder();
            g6.AppendJoin("..", li);
            Console.WriteLine(g6.ToString()); // 4..5..6

            var ls = new List<string> { "p", "q" };
            var g7 = new StringBuilder("L:");
            g7.AppendJoin('#', ls);
            Console.WriteLine(g7.ToString()); // L:p#q

            // empty and single-element inputs
            var e1 = new StringBuilder("E:");
            e1.AppendJoin(",", new string[] { }).AppendJoin(",", new object[] { 9 });
            Console.WriteLine(e1.ToString() + "|" + e1.Length); // E:9|3
        }
    }
}
