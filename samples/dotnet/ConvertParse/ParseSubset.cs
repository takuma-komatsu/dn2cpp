#nullable disable
using System;

namespace ParseSubset
{
    // int/long Parse and TryParse (invariant: optional surrounding
    // whitespace, optional sign, decimal digits), plus string.IsNullOrWhiteSpace.
    // These avoid dragging the BCL's globalization/NumberFormatInfo machinery.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine(int.Parse("123"));   // 123
            Console.WriteLine(int.Parse("-45"));    // -45
            Console.WriteLine(int.Parse("  7  "));  // 7 (surrounding whitespace)
            Console.WriteLine(long.Parse("5000000000")); // 5000000000

            int v;
            Console.WriteLine(int.TryParse("99", out v)); // True
            Console.WriteLine(v);                          // 99
            Console.WriteLine(int.TryParse("xx", out v));  // False
            Console.WriteLine(v);                          // 0 (unchanged-on-failure)

            long l;
            Console.WriteLine(long.TryParse("-12345678901", out l)); // True
            Console.WriteLine(l);                                     // -12345678901

            Console.WriteLine(string.IsNullOrWhiteSpace("   "));  // True
            Console.WriteLine(string.IsNullOrWhiteSpace(""));     // True
            Console.WriteLine(string.IsNullOrWhiteSpace("a"));    // False
            Console.WriteLine(string.IsNullOrWhiteSpace(" x "));  // False
        }
    }
}
