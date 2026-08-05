using System;
using ConvOpLib;
using System.Globalization;

namespace ConvOpSubset
{
    internal static class Program
    {
        private static int Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            Cell c;
            c.Raw = 7;

            // Each of these binds to a DIFFERENT cross-assembly conversion
            // operator that shares the same (Cell) parameter and differs only by
            // return type. Before the fix, the params-only resolver bound every
            // one of them to the first-declared (int) op_Implicit.
            int i = c;        // -> op_Implicit(Cell):int
            Point p = c;      // -> op_Implicit(Cell):Point  (the trap)
            string s = c;     // -> op_Implicit(Cell):string
            long l = (long)c; // -> op_Explicit(Cell):long
            double d = (double)c; // -> op_Explicit(Cell):double

            Console.WriteLine(i);
            Console.WriteLine(p.X);
            Console.WriteLine(p.Y);
            Console.WriteLine(s);
            Console.WriteLine(l);
            Console.WriteLine(d);
            return 0;
        }
    }
}
