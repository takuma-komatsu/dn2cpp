#nullable disable
using System;
using System.Text;

namespace StringBuilderEditSubset
{
    // StringBuilder Insert/Remove/Replace as in-place ops. Insert formats
    // non-char/string values first (like Append); Replace rewrites all occurrences.
    internal static class Program
    {
        internal static void Run()
        {
            StringBuilder sb = new StringBuilder("Hello World");
            sb.Insert(5, ",");
            Console.WriteLine(sb.ToString());        // Hello, World

            sb.Insert(0, ">> ");
            Console.WriteLine(sb.ToString());        // >> Hello, World

            sb.Insert(sb.Length, '!');
            Console.WriteLine(sb.ToString());        // >> Hello, World!

            sb.Insert(0, 42);
            Console.WriteLine(sb.ToString());        // 42>> Hello, World!

            StringBuilder rm = new StringBuilder("abcdefgh");
            rm.Remove(2, 3);
            Console.WriteLine(rm.ToString());        // abfgh

            rm.Remove(0, 2);
            Console.WriteLine(rm.ToString());        // fgh

            StringBuilder rc = new StringBuilder("a.b.c.d");
            rc.Replace('.', '-');
            Console.WriteLine(rc.ToString());        // a-b-c-d

            StringBuilder rs = new StringBuilder("one fish two fish");
            rs.Replace("fish", "cat");
            Console.WriteLine(rs.ToString());        // one cat two cat

            StringBuilder grow = new StringBuilder("xyz");
            grow.Replace("y", "YYYY");
            Console.WriteLine(grow.ToString());      // xYYYYz

            StringBuilder shrink = new StringBuilder("aXXbXXc");
            shrink.Replace("XX", "");
            Console.WriteLine(shrink.ToString());    // abc

            StringBuilder chain = new StringBuilder("12345");
            Console.WriteLine(chain.Insert(0, "[").Insert(chain.Length, "]").ToString()); // [12345]
        }
    }
}
