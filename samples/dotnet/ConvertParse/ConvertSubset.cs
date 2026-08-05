#nullable disable
using System;

namespace ConvertSubset
{
    // System.Convert conversions. The real bodies dispatch through
    // IConvertible + IFormatProvider (globalization); these single-argument
    // numeric/string/bool/char overloads are intercepted as runtime intrinsics.
    // ToInt32/ToInt64 from a real round to nearest, ties-to-even (banker's
    // rounding) like the BCL; ToString/ToSingle reuse the primitive
    // ToString/parse paths. double/float results are asserted by equality
    // (formatting-independent), except where "%g" agrees with .NET.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            // ToInt32 — banker's rounding from double.
            Console.WriteLine(Convert.ToInt32(2.5));   // 2 (tie -> even)
            Console.WriteLine(Convert.ToInt32(3.5));   // 4 (tie -> even)
            Console.WriteLine(Convert.ToInt32(-2.5));  // -2
            Console.WriteLine(Convert.ToInt32(2.6));   // 3
            Console.WriteLine(Convert.ToInt32(2.4));   // 2
            // ToInt32 — string / long / bool / int.
            Console.WriteLine(Convert.ToInt32("123")); // 123
            Console.WriteLine(Convert.ToInt32(123L));  // 123
            Console.WriteLine(Convert.ToInt32(true));  // 1
            Console.WriteLine(Convert.ToInt32(false)); // 0

            // ToInt64.
            Console.WriteLine(Convert.ToInt64("5000000000")); // 5000000000
            Console.WriteLine(Convert.ToInt64(3.5));          // 4 (tie -> even)
            Console.WriteLine(Convert.ToInt64(42));           // 42

            // ToDouble — asserted by equality to dodge formatting differences.
            Console.WriteLine(Convert.ToDouble("2.5") == 2.5);  // True
            Console.WriteLine(Convert.ToDouble(7) == 7.0);      // True
            Console.WriteLine(Convert.ToDouble(100L) == 100.0); // True

            // ToBoolean — "true"/"false" (case-insensitive, trimmed); numeric != 0.
            Console.WriteLine(Convert.ToBoolean("true"));    // True
            Console.WriteLine(Convert.ToBoolean("False"));   // False
            Console.WriteLine(Convert.ToBoolean("  TRUE ")); // True
            Console.WriteLine(Convert.ToBoolean(0));         // False
            Console.WriteLine(Convert.ToBoolean(5));         // True

            // ToString — format like the primitive's ToString; string is identity.
            Console.WriteLine(Convert.ToString(123));        // 123
            Console.WriteLine(Convert.ToString(-45L));       // -45
            Console.WriteLine(Convert.ToString(2.5));        // 2.5 ("%g" agrees here)
            Console.WriteLine(Convert.ToString(true));       // True
            Console.WriteLine(Convert.ToString('A'));        // A
            Console.WriteLine(Convert.ToString("hi"));       // hi

            // ToSingle — string parse / numeric narrow-widen (asserted by equality).
            Console.WriteLine(Convert.ToSingle(7) == 7.0f);      // True
            Console.WriteLine(Convert.ToSingle("1.5") == 1.5f);  // True
            Console.WriteLine(Convert.ToSingle(2.5) == 2.5f);    // True
        }
    }
}
