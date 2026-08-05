#nullable disable
using System;
using System.Globalization;

namespace StringCompareSubset
{
    // Ordinal string comparison: String.CompareOrdinal, Compare(a, b, StringComparison)
    // and Equals(StringComparison), under Ordinal and the exact BMP OrdinalIgnoreCase
    // fold.
    internal static class Program
    {
        internal static void Run()
        {
            // CompareOrdinal returns the.NET code-unit difference (not just sign).
            Console.WriteLine(string.CompareOrdinal("abc", "abc"));   // 0
            Console.WriteLine(string.CompareOrdinal("abc", "abd"));   // -1
            Console.WriteLine(string.CompareOrdinal("abc", "ab"));    // 1
            Console.WriteLine(string.CompareOrdinal("Apple", "apple")); // -32

            // Compare with an explicit StringComparison.
            Console.WriteLine(string.Compare("abc", "abc", StringComparison.Ordinal));            // 0
            Console.WriteLine(string.Compare("Apple", "apple", StringComparison.Ordinal));        // -32
            Console.WriteLine(string.Compare("Apple", "apple", StringComparison.OrdinalIgnoreCase)); // 0
            Console.WriteLine(string.Compare("hello", "World", StringComparison.OrdinalIgnoreCase)); // -15

            // Equals: instance ordinal, instance ignore-case, static ordinal.
            Console.WriteLine("abc".Equals("abc"));                                       // True
            Console.WriteLine("abc".Equals("ABC"));                                       // False
            Console.WriteLine("abc".Equals("ABC", StringComparison.OrdinalIgnoreCase));   // True
            Console.WriteLine("abc".Equals("abd", StringComparison.Ordinal));             // False
            Console.WriteLine(string.Equals("foo", "foo"));                               // True
            Console.WriteLine(string.Equals("foo", "FOO", StringComparison.OrdinalIgnoreCase)); // True

            // Non-ASCII OrdinalIgnoreCase. .NET's OrdinalCasing deliberately does NOT
            // fold Kelvin (U+212A) or long s (U+017F), and has no full ß -> SS fold.
            Console.WriteLine("ÄPFEL".Equals("äpfel", StringComparison.OrdinalIgnoreCase));    // True
            Console.WriteLine("σ".Equals("Σ", StringComparison.OrdinalIgnoreCase));            // True
            Console.WriteLine("ς".Equals("Σ", StringComparison.OrdinalIgnoreCase));            // True (final sigma)
            Console.WriteLine("\u017F".Equals("s", StringComparison.OrdinalIgnoreCase));  // False (long s)
            Console.WriteLine("\u212A".Equals("k", StringComparison.OrdinalIgnoreCase));  // False (Kelvin sign)
            Console.WriteLine("Straße".Equals("STRASSE", StringComparison.OrdinalIgnoreCase)); // False
            Console.WriteLine(string.Compare("Ä", "ä", StringComparison.OrdinalIgnoreCase));   // 0
            Console.WriteLine(string.Compare("\u044F", "\u042F", StringComparison.OrdinalIgnoreCase)); // 0 (Cyrillic Ya)

            CompareOrdinalSubstring();
            CompareOrdinalSubstringBounds();
            CultureOverloadsOrdinalApprox();
        }

        // The three culture-sensitive Compare overloads all lower to the same ordinal
        // approximation (culture dropped). Real .NET compares with ICU, whose magnitudes
        // differ and whose mixed-case ordering diverges from ordinal, so every input is
        // one where the SIGN agrees across the two models and Math.Sign is printed. The
        // CompareOptions.Ordinal row is the exception — both models are exactly ordinal
        // there, so the raw code-unit difference is asserted.
        private static void CultureOverloadsOrdinalApprox()
        {
            var inv = CultureInfo.InvariantCulture;

            // Compare(String, String, CultureInfo, CompareOptions).
            Console.WriteLine(Math.Sign(string.Compare("same", "same", inv, CompareOptions.None)));         // 0
            Console.WriteLine(Math.Sign(string.Compare("apple", "banana", inv, CompareOptions.None)));      // -1
            Console.WriteLine(Math.Sign(string.Compare("zoo", "app", inv, CompareOptions.None)));           // 1
            Console.WriteLine(Math.Sign(string.Compare("abc", "abcd", inv, CompareOptions.None)));          // -1 (prefix)
            Console.WriteLine(Math.Sign(string.Compare("Apple", "APPLE", inv, CompareOptions.IgnoreCase))); // 0
            Console.WriteLine(Math.Sign(string.Compare("HELLO", "world", inv, CompareOptions.IgnoreCase))); // -1
            Console.WriteLine(Math.Sign(string.Compare("Ä", "ä", inv, CompareOptions.IgnoreCase)));         // 0
            Console.WriteLine(Math.Sign(string.Compare("Apple", "apple", inv, CompareOptions.OrdinalIgnoreCase))); // 0
            Console.WriteLine(string.Compare("Apple", "apple", inv, CompareOptions.Ordinal));               // -32 (raw, exact both sides)

            // Compare(String, Int32, String, Int32, Int32) — same-case ASCII only, so the
            // sign agrees with the ordinal substring helper, per-side clamp and all. No
            // null operands: real .NET's culture path range-checks a null side where the
            // shared ordinal lowering answers -1/0/1.
            Console.WriteLine(Math.Sign(string.Compare("hello world", 6, "worlds", 0, 5))); // 0
            Console.WriteLine(Math.Sign(string.Compare("abcdef", 4, "abcxyz", 4, 100)));    // -1 ('e' < 'y', clamped)
            Console.WriteLine(Math.Sign(string.Compare("ab", 0, "abcd", 0, 4)));            // -1 (A clamps shorter)
            Console.WriteLine(Math.Sign(string.Compare("abcd", 0, "ab", 0, 4)));            // 1
            Console.WriteLine(Math.Sign(string.Compare("abc", 0, "xyz", 0, 0)));            // 0 (zero length)

            // Compare(String, String, Boolean, CultureInfo).
            Console.WriteLine(Math.Sign(string.Compare("same", "same", false, inv)));       // 0
            Console.WriteLine(Math.Sign(string.Compare("Apple", "apple", true, inv)));      // 0
            Console.WriteLine(Math.Sign(string.Compare("HELLO", "world", true, inv)));      // -1
            Console.WriteLine(Math.Sign(string.Compare("apple", "banana", false, inv)));    // -1
            Console.WriteLine(Math.Sign(string.Compare("zoo", "app", true, inv)));          // 1
            Console.WriteLine(Math.Sign(string.Compare(null, "a", false, inv)));            // -1
        }

        // CompareOrdinal's substring form clamps `length` against EACH side separately:
        // where the two clamped lengths differ the result is their DIFFERENCE, which a
        // single clamp against the minimum would answer 0 for. The result is a raw
        // difference throughout, never a -1/0/1 sign.
        private static void CompareOrdinalSubstring()
        {
            // length longer than what remains: the clamp is what stops the read.
            Console.WriteLine(string.CompareOrdinal("abc", 0, "abd", 0, 100));        // -1
            Console.WriteLine(string.CompareOrdinal("abc", 1, "abc", 1, 100));        // 0
            Console.WriteLine(string.CompareOrdinal("abcdef", 4, "abcxyz", 4, 100));  // -20 ('e'-'y'), a RAW diff

            // The separate-clamp cases: A and B clamp to different lengths, so the
            // length difference decides. One clamp against a single min would say 0.
            Console.WriteLine(string.CompareOrdinal("ab", 0, "abcd", 0, 4));          // -2 (2-4)
            Console.WriteLine(string.CompareOrdinal("abcd", 0, "ab", 0, 4));          // 2  (4-2)
            Console.WriteLine(string.CompareOrdinal("ab", 0, "abcd", 0, 2));          // 0  (both clamp to 2)
            Console.WriteLine(string.CompareOrdinal("hello world", 6, "worlds", 0, 6)); // -1 (5-6)

            // index at the very end -> that side contributes nothing.
            Console.WriteLine(string.CompareOrdinal("abc", 3, "abc", 3, 5));          // 0
            Console.WriteLine(string.CompareOrdinal("abc", 3, "xyz", 0, 3));          // -3 (0-3)

            // empty strings and zero length.
            Console.WriteLine(string.CompareOrdinal("", 0, "", 0, 0));                // 0
            Console.WriteLine(string.CompareOrdinal("", 0, "", 0, 5));                // 0
            Console.WriteLine(string.CompareOrdinal("", 0, "a", 0, 1));               // -1
            Console.WriteLine(string.CompareOrdinal("abc", 0, "xyz", 0, 0));          // 0

            // asymmetric indices, and equal substrings out of unequal strings.
            Console.WriteLine(string.CompareOrdinal("abcdef", 5, "ab", 1, 10));       // 4 ('f'-'b')
            Console.WriteLine(string.CompareOrdinal("hello world", 6, "world", 0, 5)); // 0

            // null: this overload answers rather than throwing.
            Console.WriteLine(string.CompareOrdinal(null, 0, "a", 0, 1));             // -1
            Console.WriteLine(string.CompareOrdinal("a", 0, null, 0, 1));             // 1
            Console.WriteLine(string.CompareOrdinal(null, 0, null, 0, 1));            // 0

            // It agrees with the Compare(..., Ordinal) sibling it shares a lowering with \u2014
            // the property that makes sharing correct.
            Console.WriteLine(string.CompareOrdinal("abcdef", 4, "abcxyz", 4, 100)
                == string.Compare("abcdef", 4, "abcxyz", 4, 100, StringComparison.Ordinal)); // True
        }

        // Argument validation of the substring form, in lock-step across the 5-arg
        // CompareOrdinal and the 6-arg Compare(..., StringComparison) that share its
        // lowering. `index == Length`, `index + length > Length` and `length == 0` are
        // valid and must NOT throw; a null operand answers rather than throwing even
        // alongside a nonsense index, since the null short-circuit precedes the range
        // checks. Each line prints both overloads so the diff asserts they agree.
        private static string Ord(string a, int ia, string b, int ib, int len)
        {
            try { return string.CompareOrdinal(a, ia, b, ib, len).ToString(); }
            catch (ArgumentOutOfRangeException) { return "AOORE"; }
        }

        private static string Cmp(string a, int ia, string b, int ib, int len)
        {
            try { return string.Compare(a, ia, b, ib, len, StringComparison.Ordinal).ToString(); }
            catch (ArgumentOutOfRangeException) { return "AOORE"; }
        }

        private static void CompareOrdinalSubstringBounds()
        {
            // Valid boundaries — no throw, unchanged behavior.
            Console.WriteLine(Ord("abc", 3, "abc", 3, 5) + " " + Cmp("abc", 3, "abc", 3, 5));   // 0 0   (index == Length)
            Console.WriteLine(Ord("abc", 1, "xyz", 1, 100) + " " + Cmp("abc", 1, "xyz", 1, 100)); // clamps
            Console.WriteLine(Ord("abc", 0, "xyz", 0, 0) + " " + Cmp("abc", 0, "xyz", 0, 0));   // 0 0   (length 0)
            Console.WriteLine(Ord("", 0, "", 0, 0) + " " + Cmp("", 0, "", 0, 0));               // 0 0
            Console.WriteLine(Ord("", 0, "", 0, 5) + " " + Cmp("", 0, "", 0, 5));               // 0 0   (length past empty)
            Console.WriteLine(Ord("", 0, "a", 0, 1) + " " + Cmp("", 0, "a", 0, 1));             // -1 -1

            // Negative length -> throws.
            Console.WriteLine(Ord("abc", 0, "xyz", 0, -1) + " " + Cmp("abc", 0, "xyz", 0, -1)); // AOORE AOORE
            Console.WriteLine(Ord("", 0, "", 0, -1) + " " + Cmp("", 0, "", 0, -1));             // AOORE AOORE

            // Negative index -> throws.
            Console.WriteLine(Ord("abc", -1, "xyz", 0, 2) + " " + Cmp("abc", -1, "xyz", 0, 2)); // AOORE AOORE
            Console.WriteLine(Ord("abc", 0, "xyz", -1, 2) + " " + Cmp("abc", 0, "xyz", -1, 2)); // AOORE AOORE
            Console.WriteLine(Ord("abc", -1, "xyz", -1, 2) + " " + Cmp("abc", -1, "xyz", -1, 2)); // AOORE AOORE

            // index > Length -> throws (index == Length above is fine).
            Console.WriteLine(Ord("abc", 4, "xyz", 0, 2) + " " + Cmp("abc", 4, "xyz", 0, 2));   // AOORE AOORE
            Console.WriteLine(Ord("abc", 0, "xyz", 4, 2) + " " + Cmp("abc", 0, "xyz", 4, 2));   // AOORE AOORE
            Console.WriteLine(Ord("abc", 5, "xyz", 9, 2) + " " + Cmp("abc", 5, "xyz", 9, 2));   // AOORE AOORE
            Console.WriteLine(Ord("", 1, "", 0, 0) + " " + Cmp("", 1, "", 0, 0));               // AOORE AOORE (index 1 on empty)

            // Mixed bad arguments — still AOORE.
            Console.WriteLine(Ord("abc", -1, "xyz", 9, 2) + " " + Cmp("abc", -1, "xyz", 9, 2)); // AOORE AOORE
            Console.WriteLine(Ord("abc", 5, "xyz", 0, -1) + " " + Cmp("abc", 5, "xyz", 0, -1)); // AOORE AOORE

            // null operands answer rather than throwing, even with bad index/length.
            Console.WriteLine(Ord(null, 0, "a", 0, 1) + " " + Cmp(null, 0, "a", 0, 1));         // -1 -1
            Console.WriteLine(Ord("a", 0, null, 0, 1) + " " + Cmp("a", 0, null, 0, 1));         // 1 1
            Console.WriteLine(Ord(null, 0, null, 0, 1) + " " + Cmp(null, 0, null, 0, 1));       // 0 0
            Console.WriteLine(Ord(null, -5, "a", 0, -1) + " " + Cmp(null, -5, "a", 0, -1));     // -1 -1 (null beats bad args)
            Console.WriteLine(Ord(null, 0, null, 0, -9) + " " + Cmp(null, 0, null, 0, -9));     // 0 0
        }
    }
}
