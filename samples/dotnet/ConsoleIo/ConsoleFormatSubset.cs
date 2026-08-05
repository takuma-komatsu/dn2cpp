#nullable disable
using System;

namespace ConsoleFormatSubset
{
    // Console.Write/WriteLine composite-format overloads reuse the
    // string.Format runtime helpers (same {index[,align][:spec]} grammar), then
    // write the composed string. 1-3 explicit object args + explicit object[];
    // the params ReadOnlySpan<object> path (4+ loose args) is future work.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("{0} and {1}", 1, 2);          // 1 and 2
            Console.WriteLine("{0:F2}", 3.14159);            // 3.14
            Console.Write("{0,5}", 42);
            Console.WriteLine();                              // 42
            Console.WriteLine("{0}-{1}-{2}", "a", "b", "c"); // a-b-c
            Console.WriteLine("{0:X}", 255);                 // FF
            Console.Write("[{0}]", "w");
            Console.WriteLine();                              // [w]
            Console.WriteLine("{0,-4}|{1,4}", "x", "y");     // x | y
            Console.WriteLine("{0}-{1}-{2}-{3}", new object[] { 1, 2, 3, 4 }); // 1-2-3-4
        }
    }
}
