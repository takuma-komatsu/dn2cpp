#nullable disable
using System;

namespace ConvertBaseSubset
{
    // System.Convert radix overloads — ToString(int/long, toBase) and
    // ToInt32/ToInt64(string, fromBase) for bases 2/8/10/16. Non-decimal bases
    // use the two's-complement bit pattern, so a negative formats as its
    // full-width unsigned value and round-trips back.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            // ToString(int, toBase).
            Console.WriteLine(Convert.ToString(255, 16));   // ff
            Console.WriteLine(Convert.ToString(255, 2));    // 11111111
            Console.WriteLine(Convert.ToString(255, 8));    // 377
            Console.WriteLine(Convert.ToString(255, 10));   // 255
            Console.WriteLine(Convert.ToString(-1, 16));    // ffffffff
            Console.WriteLine(Convert.ToString(-1, 2));     // 11111111111111111111111111111111
            Console.WriteLine(Convert.ToString(-255, 16));  // ffffff01
            Console.WriteLine(Convert.ToString(0, 16));     // 0
            Console.WriteLine(Convert.ToString(int.MinValue, 16)); // 80000000

            // ToString(long, toBase).
            Console.WriteLine(Convert.ToString(4294967296L, 16)); // 100000000
            Console.WriteLine(Convert.ToString(-1L, 16));         // ffffffffffffffff
            Console.WriteLine(Convert.ToString(255L, 2));         // 11111111

            // ToInt32(string, fromBase) — round-trips and two's-complement.
            Console.WriteLine(Convert.ToInt32("ff", 16));       // 255
            Console.WriteLine(Convert.ToInt32("11111111", 2));  // 255
            Console.WriteLine(Convert.ToInt32("377", 8));       // 255
            Console.WriteLine(Convert.ToInt32("123", 10));      // 123
            Console.WriteLine(Convert.ToInt32("ffffffff", 16)); // -1
            Console.WriteLine(Convert.ToInt32("80000000", 16)); // -2147483648

            // ToInt64(string, fromBase).
            Console.WriteLine(Convert.ToInt64("100000000", 16));        // 4294967296
            Console.WriteLine(Convert.ToInt64("ffffffffffffffff", 16)); // -1
        }
    }
}
