#nullable disable
using System;

namespace StringSplitSubset
{
    // String.Split(char, StringSplitOptions): None, RemoveEmptyEntries and
    // TrimEntries. Each entry is trimmed before the empty check.
    internal static class Program
    {
        internal static void Run()
        {
            string csv = "a,bb,,ccc";
            string[] parts = csv.Split(',');
            Console.WriteLine(parts.Length);  // 4
            foreach (string p in parts)
            {
                Console.WriteLine("[" + p + "]"); // [a] [bb] [] [ccc]
            }

            string[] nonEmpty = csv.Split(',', StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine(nonEmpty.Length); // 3
            Console.WriteLine(nonEmpty[2]);     // ccc

            string[] words = "one  two   three".Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine(words.Length);    // 3
            Console.WriteLine(words[0] + words[1] + words[2]); // onetwothree

            string[] trimmed = " a , b ,, c ".Split(',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine(trimmed.Length);  // 3
            Console.WriteLine(trimmed[0] + trimmed[1] + trimmed[2]); // abc
        }
    }
}
