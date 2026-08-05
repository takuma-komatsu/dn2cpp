#nullable enable
using System;

namespace ArrayResizeSubset
{
    // BCL growable-collection / argument-guard primitives.
    //  * Array.Resize<T> (ref T[], int) — alloc + copy overlap + write back; the
    //    pattern Stack/Queue/etc. Grow use. Grow, shrink, and null source.
    //  * Array.MaxLength — the 0x7FFFFFC7 grow ceiling (exercised indirectly).
    //  * ArgumentOutOfRangeException.ThrowIf* guards, whose generic-math leaves
    //    (INumberBase<T>.IsNegative, IComparisonOperators op_*) are [Intrinsic]
    //    bodyless and lower to the primitive op.
    internal static class Program
    {
        internal static int Run()
        {
            int[] a = { 1, 2, 3 };
            Array.Resize(ref a, 5);
            a[3] = 4;
            a[4] = 5;
            Console.WriteLine(string.Join(",", a));               // 1,2,3,4,5

            Array.Resize(ref a, 2);
            Console.WriteLine(string.Join(",", a));               // 1,2

            string[]? s = null;
            Array.Resize(ref s, 2);                               // null source -> length 0
            s[0] = "x";
            Console.WriteLine(s.Length + ":" + (s[1] ?? "null")); // 2:null

            ArgumentOutOfRangeException.ThrowIfNegative(5);        // ok
            ArgumentOutOfRangeException.ThrowIfGreaterThan(3, 10); // ok
            ArgumentOutOfRangeException.ThrowIfLessThan(7, 2);     // ok
            Console.WriteLine("guards-ok");                       // guards-ok

            try
            {
                ArgumentOutOfRangeException.ThrowIfNegative(-1);
                Console.WriteLine("no throw");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("caught neg");                  // caught neg
            }

            try
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(9, 4);
                Console.WriteLine("no throw");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("caught gt");                   // caught gt
            }

            return 0;
        }
    }
}
