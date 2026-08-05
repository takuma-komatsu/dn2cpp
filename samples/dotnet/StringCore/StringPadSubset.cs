#nullable disable
using System;

namespace StringPadSubset
{
    // PadLeft/PadRight (default ' ' or an explicit pad char), Remove (to-end or a
    // range) and Insert — ordinal/code-unit based, matching .NET.
    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("[" + "abc".PadLeft(5) + "]");        // [ abc]
            Console.WriteLine("[" + "abc".PadLeft(5, '0') + "]");   // [00abc]
            Console.WriteLine("[" + "abc".PadLeft(2) + "]");        // [abc] (no widening)
            Console.WriteLine("[" + "abc".PadRight(5, '-') + "]");  // [abc--]
            Console.WriteLine("[" + "abc".PadRight(3) + "]");       // [abc]

            Console.WriteLine("[" + "hello".Remove(2) + "]");       // [he]
            Console.WriteLine("[" + "hello".Remove(1, 2) + "]");    // [hlo]
            Console.WriteLine("[" + "hello".Remove(0, 5) + "]");    // []

            Console.WriteLine("[" + "abc".Insert(1, "XY") + "]");   // [aXYbc]
            Console.WriteLine("[" + "abc".Insert(0, ">>") + "]");   // [>>abc]
            Console.WriteLine("[" + "abc".Insert(3, "!") + "]");    // [abc!]
        }
    }
}
