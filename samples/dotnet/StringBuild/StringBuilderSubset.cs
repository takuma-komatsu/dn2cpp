#nullable disable
using System;
using System.Text;

namespace StringBuilderSubset
{
    // StringBuilder as a growable UTF-16 buffer: Append over
    // string/char/int/long/double/bool chains fluently; ToString does not reset.
    internal static class Program
    {
        internal static void Run()
        {
            var sb = new StringBuilder();
            sb.Append("Hello").Append(", ").Append("World").Append('!'); // fluent chain
            sb.Append(' ').Append(42).Append(' ').Append(3.5).Append(' ').Append(true);
            Console.WriteLine(sb.ToString()); // Hello, World! 42 3.5 True
            Console.WriteLine(sb.Length);     // 21

            // ToString does not reset: a second call yields the same content.
            Console.WriteLine(sb.ToString().Length); // 21

            var sb2 = new StringBuilder("init:");
            sb2.Append(100L).Append('/').Append(200L);
            Console.WriteLine(sb2.ToString()); // init:100/200

            // AppendLine appends a newline; "a\nb\n" has length 4.
            var sb3 = new StringBuilder();
            sb3.AppendLine("a").AppendLine("b");
            Console.WriteLine(sb3.Length); // 4

            sb.Clear();
            sb.Append("cleared");
            Console.WriteLine(sb.ToString()); // cleared
            Console.WriteLine(sb.Length);     // 7
        }
    }
}
