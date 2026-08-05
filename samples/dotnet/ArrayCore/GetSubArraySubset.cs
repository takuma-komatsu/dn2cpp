#nullable disable
using System;
using System.Runtime.CompilerServices;

namespace GetSubArraySubset
{
    // RuntimeHelpers.GetSubArray<T>(T[], Range)
    // = the C# `array[range]` lowering. The console self-host closure reaches it via
    // System.IO.BinaryReader.ReadBytes, which slices a byte[] with `bytes[..numRead]`
    // (Range.EndAt(numRead) -> GetSubArray). Exercise every element rep (int = I4, byte =
    // N, string + a reference type = Ref) and every Range shape (normal a..b, full .., from-
    // start a.., to-end ..b, empty n..n, from-end ^n.. / ..^n / ^a..^b, whole 0..len) plus
    // the bounds-check throw paths. The shallow-copy contract (reference-element pointers
    // shared) and the precise element type (sub.GetType() == typeof(T[])) are verified too.
    // Exact-diffed vs real .NET.
    internal sealed class Box
    {
        public readonly int V;
        public Box(int v) { V = v; }
        public override string ToString() => "B" + V;
    }

    internal static class Program
    {
        private static string J(int[] a)
        {
            string s = "[";
            for (int i = 0; i < a.Length; i++)
                s += (i == 0 ? "" : ",") + a[i];
            return s + "]";
        }

        private static string JB(byte[] a)
        {
            string s = "[";
            for (int i = 0; i < a.Length; i++)
                s += (i == 0 ? "" : ",") + a[i];
            return s + "]";
        }

        private static string JS(string[] a)
        {
            string s = "[";
            for (int i = 0; i < a.Length; i++)
                s += (i == 0 ? "" : ",") + a[i];
            return s + "]";
        }

        internal static void Run()
        {
            // int[] (I4 rep) — the full range matrix via the `array[range]` syntax.
            int[] a = { 10, 11, 12, 13, 14 }; // length 5
            Console.WriteLine("normal 1..3 " + J(a[1..3]) + " " + a[1..3].GetType().Name);
            Console.WriteLine("full ..     " + J(a[..]));
            Console.WriteLine("fromstart 2.. " + J(a[2..]));
            Console.WriteLine("toend ..3   " + J(a[..3]));
            Console.WriteLine("empty 2..2  " + J(a[2..2]) + " len=" + a[2..2].Length);
            Console.WriteLine("fromEnd ^2.. " + J(a[^2..]));
            Console.WriteLine("fromEnd ..^1 " + J(a[..^1]));
            Console.WriteLine("fromEnd ^3..^1 " + J(a[^3..^1]));
            Console.WriteLine("whole 0..5  " + J(a[0..5]));
            Console.WriteLine("emptyarr .. " + J(Array.Empty<int>()[..]));

            // The actual BinaryReader.ReadBytes shape: array[..n] via Range.EndAt(Index).
            int n = 2;
            Console.WriteLine("EndAt(2)    " + J(RuntimeHelpers.GetSubArray(a, Range.EndAt(n))));

            // byte[] (N rep) — the type BinaryReader actually slices.
            byte[] by = { 1, 2, 3, 4, 5, 6 };
            Console.WriteLine("byte ..4    " + JB(by[..4]) + " " + by[..4].GetType().Name);
            Console.WriteLine("byte 2..    " + JB(by[2..]));
            Console.WriteLine("byte ^3..^1 " + JB(by[^3..^1]));

            // string[] (Ref rep) — shallow copy shares the element references.
            string[] s = { "x", "y", "z", "w" };
            string[] ssub = s[1..3];
            Console.WriteLine("str 1..3    " + JS(ssub) + " " + ssub.GetType().Name);
            Console.WriteLine("str share   " + object.ReferenceEquals(ssub[0], s[1]));

            // Reference type[] — distinct element type, shared pointers.
            Box[] bs = { new Box(1), new Box(2), new Box(3) };
            Box[] bsub = bs[1..];
            Console.WriteLine("box 1..     " + bsub[0] + "," + bsub[1] + " " + bsub.GetType().Name);
            Console.WriteLine("box share   " + object.ReferenceEquals(bsub[0], bs[1]));

            // Independence: mutating the slice does not touch the source (fresh array).
            int[] cp = a[1..3];
            cp[0] = 999;
            Console.WriteLine("indep src   " + J(a) + " slice " + J(cp));

            // Bounds-check throws (ArgumentOutOfRange): end>len, reversed start>end.
            Console.WriteLine("oob end     " + Throws(a, 0, 6));
            Console.WriteLine("oob start   " + Throws(a, 6, 6));
            Console.WriteLine("reversed    " + Throws(a, 3, 1));
        }

        private static string Throws(int[] a, int start, int end)
        {
            try
            {
                int[] r = RuntimeHelpers.GetSubArray(a, start..end);
                return "no-throw(" + r.Length + ")";
            }
            catch (ArgumentOutOfRangeException)
            {
                return "ArgumentOutOfRangeException";
            }
        }
    }
}
