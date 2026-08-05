#nullable disable
using System;

namespace StringComparisonFoldSubset
{
    // The four culture-sensitive StringComparison values folded onto
    // Ordinal/OrdinalIgnoreCase by dn2cpp_str_comparison_fold. This diffs against
    // ICU-backed real .NET, so every row is chosen where fold and ICU agree: ASCII only
    // (ICU treats some punctuation as ignorable), equality rather than ordering wherever
    // case is mixed, and no i/I under the CurrentCulture rows, whose host locale is live
    // and carries the Turkish-I hazard.
    internal static class Program
    {
        internal static void Run()
        {
            const StringComparison IC = StringComparison.InvariantCulture;
            const StringComparison ICIC = StringComparison.InvariantCultureIgnoreCase;
            const StringComparison CC = StringComparison.CurrentCulture;
            const StringComparison CCIC = StringComparison.CurrentCultureIgnoreCase;

            // StartsWith/EndsWith.
            Console.WriteLine("[thrive]".StartsWith("[", IC));            // True
            Console.WriteLine("[thrive]".EndsWith("]", IC));              // True
            Console.WriteLine("[thrive]".StartsWith("]", IC));            // False
            Console.WriteLine("Hello World".StartsWith("hello", IC));     // False (case-sensitive)
            Console.WriteLine("Hello World".StartsWith("HELLO", ICIC));   // True
            Console.WriteLine("Hello World".EndsWith("WORLD", ICIC));     // True
            Console.WriteLine("Hello World".EndsWith("word", ICIC));      // False
            Console.WriteLine("abc".StartsWith("", IC));                  // True (empty prefix)
            Console.WriteLine("abc".StartsWith("abc", IC));               // True (whole string)
            Console.WriteLine("ab".StartsWith("abc", IC));                // False (prefix longer)

            // Contains / IndexOf / LastIndexOf — ASCII positions agree with ICU.
            Console.WriteLine("Hello World".Contains("o W", IC));         // True
            Console.WriteLine("Hello World".Contains("O w", ICIC));       // True
            Console.WriteLine("Hello World".Contains("xyz", ICIC));       // False
            Console.WriteLine("Hello World".IndexOf("World", IC));        // 6
            Console.WriteLine("Hello World".IndexOf("world", IC));        // -1
            Console.WriteLine("Hello World".IndexOf("WORLD", ICIC));      // 6
            Console.WriteLine("Hello World".IndexOf("", IC));             // 0 (empty needle)
            Console.WriteLine("Hello World Hello".IndexOf("Hello", 1, IC));      // 12
            Console.WriteLine("Hello World Hello".IndexOf("HELLO", 1, ICIC));    // 12
            Console.WriteLine("Hello World Hello".LastIndexOf("Hello", IC));     // 12
            Console.WriteLine("Hello World Hello".LastIndexOf("HELLO", ICIC));   // 12
            Console.WriteLine("Hello World Hello".LastIndexOf("hello", IC));     // -1
            Console.WriteLine("aXbXc".IndexOf('x', ICIC));                // 1 (IndexOf(char, cmp))
            Console.WriteLine("aXbXc".IndexOf('x', IC));                  // -1

            // Replace(string, string, StringComparison) — ASCII, left-to-right,
            // non-overlapping; positions agree with ICU on these inputs.
            Console.WriteLine("Hello World hello".Replace("hello", "X", ICIC)); // X World X
            Console.WriteLine("Hello World hello".Replace("hello", "X", IC));   // Hello World X
            Console.WriteLine("aaa".Replace("aa", "b", ICIC));                  // ba

            // Equals — case-mixed rows assert EQUALITY only (never ordering).
            Console.WriteLine("abc".Equals("abc", IC));                   // True
            Console.WriteLine("abc".Equals("ABC", IC));                   // False
            Console.WriteLine("abc".Equals("ABC", ICIC));                 // True
            Console.WriteLine(string.Equals("Apple", "APPLE", ICIC));     // True
            Console.WriteLine(string.Equals("Apple", "APPLES", ICIC));    // False

            // Compare — Math.Sign, same-case operands (mixed-case ORDERING is a
            // real ICU/ordinal divergence, so it stays out); case-mixed rows
            // only under IgnoreCase where the fold answers equality (0).
            Console.WriteLine(Math.Sign(string.Compare("same", "same", IC)));      // 0
            Console.WriteLine(Math.Sign(string.Compare("apple", "banana", IC)));   // -1
            Console.WriteLine(Math.Sign(string.Compare("zoo", "app", IC)));        // 1
            Console.WriteLine(Math.Sign(string.Compare("abc", "abcd", IC)));       // -1 (prefix)
            Console.WriteLine(Math.Sign(string.Compare("Apple", "APPLE", ICIC)));  // 0
            Console.WriteLine(Math.Sign(string.Compare("HELLO", "world", ICIC)));  // -1
            // The 6-arg substring form with an invariant comparison.
            Console.WriteLine(Math.Sign(string.Compare("hello world", 6, "worlds", 0, 5, IC)));   // 0
            Console.WriteLine(Math.Sign(string.Compare("hello WORLD", 6, "worlds", 0, 5, ICIC))); // 0

            // CurrentCulture folds the same way (dn2cpp's current culture IS invariant),
            // but real .NET runs these against the host locale — so only rows every
            // shipped locale answers alike.
            Console.WriteLine("Hello World".StartsWith("Hello", CC));     // True
            Console.WriteLine("Hello World".StartsWith("HELLO", CCIC));   // True
            Console.WriteLine("Hello World".EndsWith("World", CC));       // True
            Console.WriteLine("Hello World".EndsWith("WORLD", CCIC));     // True
            Console.WriteLine("Hello World".Contains("o W", CC));         // True
            Console.WriteLine("Hello World".IndexOf("World", CC));        // 6
            Console.WriteLine("Hello World".IndexOf("WORLD", CCIC));      // 6
            Console.WriteLine("abc".Equals("ABC", CCIC));                 // True
            Console.WriteLine("abc".Equals("ABC", CC));                   // False
            Console.WriteLine(Math.Sign(string.Compare("apple", "banana", CC)));  // -1
            Console.WriteLine(Math.Sign(string.Compare("Apple", "APPLE", CCIC))); // 0

            // Hash VALUES differ from .NET (randomized Marvin vs deterministic FNV), so
            // assert the contract instead: equal-under-the-comparison inputs hash alike.
            Console.WriteLine(string.GetHashCode("Hello".AsSpan(), IC)
                == string.GetHashCode("Hello".AsSpan(), IC));             // True
            Console.WriteLine(string.GetHashCode("Hello".AsSpan(), ICIC)
                == string.GetHashCode("HELLO".AsSpan(), ICIC));           // True

            // An out-of-range StringComparison is a catchable ArgumentException.
            try { string.Compare("a", "b", (StringComparison)42); Console.WriteLine("no-throw"); }
            catch (ArgumentException) { Console.WriteLine("AE"); }        // AE
            try { "a".StartsWith("a", (StringComparison)7); Console.WriteLine("no-throw"); }
            catch (ArgumentException) { Console.WriteLine("AE"); }        // AE
        }
    }
}
