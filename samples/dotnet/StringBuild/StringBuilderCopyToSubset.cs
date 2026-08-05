#nullable disable
using System;
using System.Text;

namespace StringBuilderCopyToSubset
{
    // StringBuilder.ToString(start, length) and CopyTo(srcIndex, dest, destIndex, count),
    // including their bounds-check faults.
    internal static class Program
    {
        private static void E(string label, Action a)
        {
            try { a(); Console.WriteLine(label + ": ok"); }
            catch (Exception e) { Console.WriteLine(label + ": " + e.GetType().Name); }
        }

        internal static void Run()
        {
            var sb = new StringBuilder("Hello, World!");

            // ToString(start, length).
            Console.WriteLine(sb.ToString(0, 5));    // Hello
            Console.WriteLine(sb.ToString(7, 5));    // World
            Console.WriteLine("[" + sb.ToString(5, 0) + "]"); // []
            Console.WriteLine(sb.ToString(0, sb.Length)); // whole
            E("ToString oob", () => sb.ToString(10, 10));
            E("ToString neg", () => sb.ToString(-1, 3));

            // CopyTo(sourceIndex, char[] dest, destIndex, count).
            char[] dst = new char[10];
            for (int i = 0; i < dst.Length; i++) dst[i] = '.';
            sb.CopyTo(7, dst, 0, 5); // World -> dst[0..5]
            Console.WriteLine(new string(dst)); // World.....
            sb.CopyTo(0, dst, 5, 3);  // Hel -> dst[5..8]
            Console.WriteLine(new string(dst)); // WorldHel..
            E("CopyTo srcOob", () => sb.CopyTo(10, new char[10], 0, 5));
            E("CopyTo destShort", () => sb.CopyTo(0, new char[3], 0, 5));
            E("CopyTo neg", () => sb.CopyTo(-1, new char[10], 0, 3));
        }
    }
}
