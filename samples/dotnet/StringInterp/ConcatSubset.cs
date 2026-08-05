#nullable disable
using System;

namespace ConcatSubset
{
    internal sealed class Tag
    {
    }

    // Value + string concatenation: Roslyn lowers `"x=" + value` to
    // String.Concat(object, ...) with the value boxed, and each operand formats via
    // Object.ToString — a plain object yielding its type name.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            int n = 42;
            double d = 2.5;
            bool b = true;
            long l = 5_000_000_000L;

            Console.WriteLine("n=" + n);          // n=42
            Console.WriteLine("d=" + d);          // d=2.5
            Console.WriteLine("b=" + b);          // b=True
            Console.WriteLine("l=" + l);          // l=5000000000
            Console.WriteLine(n + ":" + d + ":" + b); // 42:2.5:True

            // Five operands -> Concat(object[]) (params array).
            Console.WriteLine("[" + n + "," + d + "," + b + "]"); // [42,2.5,True]

            // A plain object (no ToString override) formats as its type name.
            var t = new Tag();
            Console.WriteLine("t=" + t);          // t=ConcatSubset.Tag
        }
    }
}
