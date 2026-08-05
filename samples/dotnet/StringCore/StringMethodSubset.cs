#nullable disable
using System;

namespace StringMethodSubset
{
    // Common System.String members over the UTF-16 Dn2CppString: ordinal semantics,
    // ASCII case mapping for ToUpper/ToLower.
    internal static class Program
    {
        internal static void Run()
        {
            string s = "Hello, World";

            Console.WriteLine(s.Substring(7));         // World
            Console.WriteLine(s.Substring(0, 5));      // Hello
            Console.WriteLine(s.IndexOf('o'));         // 4
            Console.WriteLine(s.IndexOf("World"));     // 7
            Console.WriteLine(s.IndexOf('z'));         // -1
            Console.WriteLine(s.Contains("World"));    // True
            Console.WriteLine(s.Contains('z'));        // False
            Console.WriteLine(s.StartsWith("Hello"));  // True
            Console.WriteLine(s.EndsWith("World"));    // True
            Console.WriteLine(s.ToUpper());            // HELLO, WORLD
            Console.WriteLine(s.ToLowerInvariant());   // hello, world
            Console.WriteLine(s.Replace('o', '0'));    // Hell0, W0rld
            Console.WriteLine("  trimmed  ".Trim());   // trimmed
            Console.WriteLine(s != "other");           // True
            Console.WriteLine(s == "Hello, World");    // True
            Console.WriteLine(string.IsNullOrEmpty("")); // True
            Console.WriteLine(string.IsNullOrEmpty(s));  // False
        }
    }
}
