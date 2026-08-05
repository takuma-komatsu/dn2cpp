#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StringReplaceNormalizeSubset
{
    // Replace with a StringComparison — BMP Greek/Cyrillic fold, non-overlapping
    // left-to-right scan, no-match same-instance contract — and its
    // (bool ignoreCase, culture) sibling; Normalize/IsNormalized; the index/count
    // ToCharArray, CopyTo, IndexOf and Join forms; the span casing forms; and unsigned
    // elements through the enumerable Join formatter.
    internal static class Program
    {
        private static void E(string label, Action a)
        {
            try
            {
                a();
                Console.WriteLine(label + ": ok");
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name);
            }
        }

        internal static void Run()
        {
            // --- Replace(string, string, StringComparison) ---
            string s = "aXbXc";
            Console.WriteLine(s.Replace("X", "yy", StringComparison.Ordinal));
            Console.WriteLine(s.Replace("x", "yy", StringComparison.OrdinalIgnoreCase));
            Console.WriteLine("AbΣcσdΣe".Replace("σ", "-", StringComparison.OrdinalIgnoreCase));
            Console.WriteLine("ЖивоЙ жИвой обед".Replace("живой", "*", StringComparison.OrdinalIgnoreCase));
            Console.WriteLine("aAaA".Replace("aa", "!", StringComparison.OrdinalIgnoreCase));
            Console.WriteLine(ReferenceEquals(s.Replace("Q", "z", StringComparison.Ordinal), s));
            Console.WriteLine(ReferenceEquals(s.Replace("q", "z", StringComparison.OrdinalIgnoreCase), s));
            Console.WriteLine(s.Replace("x", null, StringComparison.OrdinalIgnoreCase));
            E("replace-null-old", () => s.Replace(null, "z", StringComparison.Ordinal));
            E("replace-empty-old", () => s.Replace("", "z", StringComparison.OrdinalIgnoreCase));
            E("replace-bad-cmp", () => s.Replace(null, "z", (StringComparison)99));

            // --- Replace(string, string, bool ignoreCase, CultureInfo) ---
            Console.WriteLine("AbΣcσd".Replace("σ", "-", false, CultureInfo.InvariantCulture));
            Console.WriteLine("AbΣcσdΣ".Replace("σ", "-", true, CultureInfo.InvariantCulture));
            Console.WriteLine("AbCabc".Replace("ab", "-", true, null));
            Console.WriteLine(ReferenceEquals(s.Replace("q", "z", true, CultureInfo.InvariantCulture), s));
            E("replace-bool-null-old", () => s.Replace(null, "z", true, CultureInfo.InvariantCulture));
            E("replace-bool-empty-old", () => s.Replace("", "z", true, CultureInfo.InvariantCulture));

            // --- Normalize / IsNormalized. Inputs are already normalized: dn2cpp models
            // the invariant-globalization "treated as already normalized" posture, exact
            // only for ASCII and pre-composed NFC. ---
            string ascii = "normal text";
            string pre = "héllo"; // pre-composed U+00E9
            Console.WriteLine(ReferenceEquals(ascii.Normalize(), ascii));
            Console.WriteLine(ascii.IsNormalized());
            Console.WriteLine(ReferenceEquals(pre.Normalize(NormalizationForm.FormC), pre));
            Console.WriteLine(pre.IsNormalized(NormalizationForm.FormC));
            Console.WriteLine("plain".Normalize(NormalizationForm.FormKC));
            Console.WriteLine("plain".IsNormalized(NormalizationForm.FormKD));
            E("normalize-bad-form", () => ascii.Normalize((NormalizationForm)99));
            E("isnormalized-bad-form", () => pre.IsNormalized((NormalizationForm)99));

            // --- ToCharArray(startIndex, length) ---
            Console.WriteLine(new string("abcdef".ToCharArray(2, 3)));
            Console.WriteLine("".ToCharArray(0, 0).Length);
            Console.WriteLine("abc".ToCharArray(3, 0).Length);
            E("tochararray-neg-len", () => "abc".ToCharArray(0, -1));
            E("tochararray-past-end", () => "abc".ToCharArray(2, 2));

            // --- CopyTo(sourceIndex, char[], destinationIndex, count) ---
            char[] dst = new char[6];
            for (int i = 0; i < dst.Length; i++)
            {
                dst[i] = '.';
            }
            "hello".CopyTo(1, dst, 2, 3);
            Console.WriteLine(new string(dst));
            E("copyto-null-dst", () => "abc".CopyTo(0, null, 0, -1));
            E("copyto-src-over", () => "abc".CopyTo(2, dst, 0, 2));
            E("copyto-dst-over", () => "abc".CopyTo(0, dst, 5, 2));
            E("copyto-neg-count", () => "abc".CopyTo(0, dst, 0, -1));

            // --- IndexOf(string, startIndex, count), ordinal ---
            Console.WriteLine("abcabc".IndexOf("bc", 2, 4));
            Console.WriteLine("abcabc".IndexOf("bc", 2, 2));
            Console.WriteLine("abcabc".IndexOf("bc", 3, 3));
            Console.WriteLine("abcabc".IndexOf("", 3, 2));
            Console.WriteLine("abc".IndexOf("", 3, 0));
            E("indexof-null-needle", () => "abc".IndexOf(null, 0, 1));
            E("indexof-count-big", () => "abc".IndexOf("b", 0, 9));
            E("indexof-start-past", () => "abc".IndexOf("b", 4, 0));

            // --- Join(separator, string[], startIndex, count) ---
            string[] parts = { "alpha", "beta", null, "delta" };
            Console.WriteLine(string.Join("-", parts, 1, 3));
            Console.WriteLine("[" + string.Join("-", parts, 4, 0) + "]");
            Console.WriteLine(string.Join(null, parts, 0, 2));
            E("join-null-arr", () => string.Join("-", (string[])null, 0, 0));
            E("join-count-big", () => string.Join("-", parts, 2, 3));
            E("join-neg-start", () => string.Join("-", parts, -1, 1));

            // --- MemoryExtensions.ToUpperInvariant/ToLowerInvariant, disjoint buffers
            // only: .NET throws for overlapping spans, which dn2cpp does not model. ---
            ReadOnlySpan<char> src = "aBcΔδЖж";
            Span<char> destBuf = new char[7];
            Console.WriteLine(MemoryExtensions.ToUpperInvariant(src, destBuf));
            Console.WriteLine(destBuf.ToString());
            Console.WriteLine(MemoryExtensions.ToLowerInvariant(src, destBuf));
            Console.WriteLine(destBuf.ToString());
            Span<char> tiny = new char[2];
            Console.WriteLine(MemoryExtensions.ToUpperInvariant(src, tiny));
            Console.WriteLine(MemoryExtensions.ToLowerInvariant(src, tiny));
            Console.WriteLine(MemoryExtensions.ToUpperInvariant(ReadOnlySpan<char>.Empty, tiny));

            // --- unsigned elements through the enumerable Join formatter ---
            var uints = new List<uint> { uint.MaxValue, 7 };
            Console.WriteLine(string.Join(",", uints));
            var ulongs = new List<ulong> { ulong.MaxValue, 7 };
            Console.WriteLine(string.Join(",", ulongs));
            uint[] uarr = { uint.MaxValue, 7 };
            Console.WriteLine(string.Join(",", uarr));
            ulong[] ularr = { ulong.MaxValue };
            Console.WriteLine(string.Join(",", ularr));
            Console.WriteLine(uarr[0]);
        }
    }
}
