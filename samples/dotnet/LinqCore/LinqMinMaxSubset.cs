#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqMinMaxSubset
{
    // /: generic Min<T>/Max<T> (ordinal string compare, ref-type
    // semantics) + int/long/double-selector Min/Max bound to the "fake
    // System.Linq" shim. The existing non-generic int Min/Max overloads stay
    // selected for List<int> (regression check). The int/long/double selector
    // overloads differ only by the Func's return type arg, yet resolve distinctly
    // (closed Func instantiations have distinct mangled names). ASCII-only
    // words so ordinal == culture order.
    internal static class Program
    {
        internal static int Run()
        {
            List<string> words = new List<string> { "pear", "apple", "kiwi", "banana" };
            Console.WriteLine(words.Min());                  // apple
            Console.WriteLine(words.Max());                  // pear
            Console.WriteLine(words.Min(w => w.Length));     // 4 (int selector)
            Console.WriteLine(words.Max(w => w.Length));     // 6 (int selector)
            Console.WriteLine(words.Min(w => (long)w.Length * 1000000000L)); // 4000000000 (long selector)
            Console.WriteLine(words.Max(w => w.Length + 0.5));               // 6.5 (double selector)

            // Non-generic numeric overloads still win for List<int>.
            List<int> nums = new List<int> { 5, 2, 8, 1 };
            Console.WriteLine(nums.Min());                   // 1
            Console.WriteLine(nums.Max());                   // 8

            // Empty reference sequence -> null (generic ref-type semantics).
            List<string> none = new List<string>();
            Console.WriteLine(none.Min() is null);           // True

            return 0;
        }
    }
}
