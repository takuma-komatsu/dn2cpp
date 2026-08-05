#nullable disable
using System;

namespace StringIndexOfCmpSubset
{
    // IndexOf(string, StringComparison) for Ordinal and the BMP-exact
    // OrdinalIgnoreCase. Also pins the intrinsic's eval-stack discipline: the receiver
    // must be consumed even when the result feeds a branch before a loop, or it
    // strands at the loop-head stack merge.
    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("hello world".IndexOf("world", StringComparison.Ordinal));          // 6
            Console.WriteLine("hello world".IndexOf("World", StringComparison.Ordinal));          // -1
            Console.WriteLine("hello world".IndexOf("World", StringComparison.OrdinalIgnoreCase)); // 6
            Console.WriteLine("hello".IndexOf("", StringComparison.Ordinal));                      // 0
            Console.WriteLine("abcabc".IndexOf("bc", StringComparison.Ordinal));                   // 1
            Console.WriteLine("abc".IndexOf("abcd", StringComparison.OrdinalIgnoreCase));          // -1

            // Branch on the result, then loop over the tail — the stack-merge shape.
            string json = "{ \"hash\": \"0x00ff\" }";
            int at = json.IndexOf("0x", StringComparison.Ordinal);
            if (at < 0 || at + 6 > json.Length)
            {
                Console.WriteLine("missing");
                return;
            }
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                char c = json[at + 2 + i];
                int nibble;
                if (c >= '0' && c <= '9')
                    nibble = c - '0';
                else if (c >= 'a' && c <= 'f')
                    nibble = c - 'a' + 10;
                else
                    nibble = 0;
                value = (value << 4) | nibble;
            }
            Console.WriteLine(value);                                                              // 255

            // IndexOf(string, int startIndex, StringComparison), the GodotSharp
            // StringExtensions.Find/FindN shape.
            Console.WriteLine("abcabc".IndexOf("bc", 2, StringComparison.Ordinal));                 // 4
            Console.WriteLine("abcabc".IndexOf("bc", 0, StringComparison.Ordinal));                 // 1
            Console.WriteLine("hello world".IndexOf("WORLD", 3, StringComparison.OrdinalIgnoreCase)); // 6
            Console.WriteLine("hello world".IndexOf("World", 3, StringComparison.Ordinal));         // -1
            Console.WriteLine("abcabc".IndexOf("", 3, StringComparison.Ordinal));                   // 3
            Console.WriteLine("abcabc".IndexOf("abc", 6, StringComparison.Ordinal));                // -1

            // Non-ASCII OrdinalIgnoreCase (exact BMP fold).
            Console.WriteLine("grüße WELT".IndexOf("welt", StringComparison.OrdinalIgnoreCase));    // 6
            Console.WriteLine("die ÄPFEL hier".IndexOf("äpfel", 2, StringComparison.OrdinalIgnoreCase)); // 4
            Console.WriteLine("ΣΩΜΑ".IndexOf("ωμα", StringComparison.OrdinalIgnoreCase));           // 1
        }
    }
}
