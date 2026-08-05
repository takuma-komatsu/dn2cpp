#nullable disable
using System;

namespace StringFormatSubset
{
    // string.Format composite formatting: holes are {index[,alignment][:spec]},
    // {{ }} are literal braces. Covers the 1-3 object overloads and object[].
    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine(string.Format("{0} and {1}", 1, 2));   // 1 and 2
            Console.WriteLine(string.Format("{0:F2}", 3.14159));     // 3.14
            Console.WriteLine(string.Format("{0,5}", 42));           // 42
            Console.WriteLine(string.Format("{0,-5}|", 42));         // 42 |
            Console.WriteLine(string.Format("{0:X}", 255));          // FF
            Console.WriteLine(string.Format("{0:D3}", 7));           // 007
            Console.WriteLine(string.Format("{0,8:F2}", 3.5));       // 3.50
            Console.WriteLine(string.Format("{{literal}} {0}", "x")); // {literal} x
            Console.WriteLine(string.Format("{0} {1} {2}", "a", "b", "c")); // a b c
            Console.WriteLine(string.Format("{0}={1}", "n", 10L));   // n=10
            // Explicit object[] overload.
            Console.WriteLine(string.Format("{0}-{1}-{2}-{3}", new object[] { 1, 2, 3, 4 })); // 1-2-3-4
        }
    }
}
