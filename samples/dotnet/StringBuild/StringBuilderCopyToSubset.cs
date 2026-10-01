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

        private static string _copyEvaluation = "";

        private static string Units(char[] value)
        {
            if (value is null)
                return "null";
            string result = "";
            foreach (char ch in value)
                result += ((int)ch).ToString("X4") + " ";
            return result;
        }

        private static void CopyFault(string label, Exception ex)
        {
            Console.WriteLine(label + " type=" + ex.GetType().Name);
            Console.WriteLine(label + " param=" + (ex is ArgumentException arg ? arg.ParamName : null));
            Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
            object actual = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
            Console.WriteLine(label + " actual=" + (actual is null ? "null" : actual.GetType().Name + ":" + actual));
        }

        private static StringBuilder Receiver(StringBuilder value)
        {
            _copyEvaluation += "R";
            return value;
        }

        private static char[] Destination(char[] value)
        {
            _copyEvaluation += "A";
            return value;
        }

        private static int Number(string step, int value, bool fail)
        {
            _copyEvaluation += step;
            if (fail)
                throw new InvalidOperationException();
            return value;
        }

        internal static void RunFaults()
        {
            Console.WriteLine("== StringBuilder copy faults ==");
            string[] sources = { "abca", "", null };
            int[] sizes = { -1, 0, 4 };
            int[] indices = { int.MinValue, -1, 0, 3, 4, 5, int.MaxValue };
            int[] counts = { int.MinValue, -1, 0, 1, 4, int.MaxValue };
            for (int i = 0; i < sources.Length; i++)
                foreach (int size in sizes)
                    foreach (int sourceIndex in indices)
                        foreach (int destinationIndex in indices)
                            foreach (int count in counts)
                            {
                                StringBuilder sb = sources[i] is null ? null : new StringBuilder(sources[i]);
                                char[] destination = size < 0 ? null : new string('.', size).ToCharArray();
                                string label = i + ":" + size + ":" + sourceIndex + ":" + destinationIndex + ":" + count;
                                try
                                {
                                    sb.CopyTo(sourceIndex, destination, destinationIndex, count);
                                    Console.WriteLine(label + " copied");
                                }
                                catch (Exception ex)
                                {
                                    CopyFault(label, ex);
                                }
                                Console.WriteLine(label + " destination=" + Units(destination));
                            }
            char[] utf16 = new char[4];
            new StringBuilder("A\0\ud800Z").CopyTo(0, utf16, 0, 4);
            Console.WriteLine("copy utf16=" + Units(utf16));
            _copyEvaluation = "";
            try
            {
                Receiver(null).CopyTo(Number("S", -1, false), Destination(null), Number("D", -1, false), Number("C", -1, false));
            }
            catch (Exception ex)
            {
                CopyFault("copy null evaluation", ex);
            }
            Console.WriteLine("copy null evaluation=" + _copyEvaluation);
            _copyEvaluation = "";
            try
            {
                Receiver(null).CopyTo(Number("S", -1, false), Destination(null), Number("D", -1, false), Number("C", -1, true));
            }
            catch (Exception ex)
            {
                CopyFault("copy throwing count", ex);
            }
            Console.WriteLine("copy throwing count evaluation=" + _copyEvaluation);
            ArgumentOutOfRangeException saved = null;
            try
            {
                new StringBuilder("abc").CopyTo(0, new char[3], -7, 0);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                saved = ex;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            CopyFault("copy fault after GC", saved);
            Console.WriteLine("StringBuilder copy faults end");
        }
    }
}
