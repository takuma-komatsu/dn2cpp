#nullable disable
using System;
using System.Text;

namespace CharSubset
{
    // System.Char classification/casing (IsDigit/IsLetter/IsWhiteSpace/
    // IsLetterOrDigit/IsUpper/IsLower/ToUpper/ToLower), ASCII/invariant, plus the
    // 1-char ToString. These avoid the BCL globalization closure the real methods
    // pull in.
    internal static class Program
    {
        // Count digits and build an upper-cased copy, exercising char in a loop.
        private static void Analyze(string s)
        {
            int digits = 0;
            var up = new StringBuilder();
            foreach (char c in s)
            {
                if (char.IsDigit(c))
                {
                    digits++;
                }

                up.Append(char.ToUpper(c));
            }
            Console.WriteLine(digits);
            Console.WriteLine(up.ToString());
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(char.IsDigit('5'));        // True
            Console.WriteLine(char.IsLetter('A'));       // True
            Console.WriteLine(char.IsLetterOrDigit('_'));// False
            Console.WriteLine(char.IsWhiteSpace('\t'));  // True
            Console.WriteLine(char.IsUpper('a'));        // False
            Console.WriteLine(char.IsLower('a'));        // True
            Console.WriteLine(char.ToUpper('a'));        // A
            Console.WriteLine(char.ToLower('Z'));        // z
            Console.WriteLine('q'.ToString());           // q

            Analyze("abc123XY");                         // 3 / ABC123XY
        }
    }
}
