#nullable disable
using System;

namespace StringTrimIndexSubset
{
    // Trim/TrimStart/TrimEnd (whitespace, single char, char[] set, null set),
    // IndexOfAny/LastIndexOfAny, the explicit IndexOf arities, and the LastIndexOf
    // family under the .NET 5+ range contract: a match must END at or before
    // startIndex, and an empty needle is found at startIndex + 1 / Length.
    internal static class Program
    {
        private static string IdxChar(string s, char c, int start)
        {
            try { return s.IndexOf(c, start).ToString(); }
            catch (ArgumentOutOfRangeException) { return "AOORE"; }
        }

        private static string IdxCharRange(string s, char c, int start, int count)
        {
            try { return s.IndexOf(c, start, count).ToString(); }
            catch (ArgumentOutOfRangeException) { return "AOORE"; }
        }

        private static string IdxStr(string s, string sub, int start)
        {
            try { return s.IndexOf(sub, start).ToString(); }
            catch (ArgumentOutOfRangeException) { return "AOORE"; }
        }

        private static string LastChar(string s, char c, int start)
        {
            try { return s.LastIndexOf(c, start).ToString(); }
            catch (ArgumentOutOfRangeException) { return "AOORE"; }
        }

        private static string LastCharRange(string s, char c, int start, int count)
        {
            try { return s.LastIndexOf(c, start, count).ToString(); }
            catch (ArgumentOutOfRangeException) { return "AOORE"; }
        }

        private static string LastStr(string s, string sub, int start)
        {
            try { return s.LastIndexOf(sub, start).ToString(); }
            catch (ArgumentOutOfRangeException) { return "AOORE"; }
        }

        private static string LastStrRange(string s, string sub, int start, int count)
        {
            try { return s.LastIndexOf(sub, start, count).ToString(); }
            catch (ArgumentOutOfRangeException) { return "AOORE"; }
        }

        private static string AnyOf(string s, char[] set, int start, int count)
        {
            try { return s.IndexOfAny(set, start, count).ToString(); }
            catch (ArgumentNullException) { return "ANE"; }
            catch (ArgumentOutOfRangeException) { return "AOORE"; }
        }

        private static string LastAnyOf(string s, char[] set, int start, int count)
        {
            try { return s.LastIndexOfAny(set, start, count).ToString(); }
            catch (ArgumentNullException) { return "ANE"; }
            catch (ArgumentOutOfRangeException) { return "AOORE"; }
        }

        internal static void Run()
        {
            // --- Trim family: whitespace defaults, single char, char[] sets ---
            Console.WriteLine($"trim=[{" ab ".Trim()}] [{" ab ".TrimStart()}] [{" ab ".TrimEnd()}]");
            Console.WriteLine($"trimc=[{"xxabxx".Trim('x')}] [{"xxab".TrimStart('x')}] [{"abxx".TrimEnd('x')}]");
            Console.WriteLine($"trimset=[{"xyabyx".Trim('x', 'y')}] [{"xyab".TrimStart('x', 'y')}] [{"abyx".TrimEnd('x', 'y')}]");
            // Null / empty set means whitespace, full .NET set (NBSP, ideographic space).
            Console.WriteLine($"trimnull=[{" ab ".Trim((char[])null)}] [{" ab ".Trim(new char[0])}]");
            Console.WriteLine($"trimuni=[{" a b\u3000".Trim((char[])null)}] [{"\u00A0ab\t".TrimEnd((char[])null)}]");
            Console.WriteLine($"trimall=[{"xx".Trim('x')}] [{"".Trim('x')}]");
            // Nothing trimmed returns the original instance, ReferenceEquals-visible.
            string noTrim = "ab";
            Console.WriteLine($"trimsame={ReferenceEquals(noTrim, noTrim.Trim('x'))}"
                + $"/{ReferenceEquals(noTrim, noTrim.TrimStart())}"
                + $"/{ReferenceEquals(noTrim, noTrim.TrimEnd('z', 'q'))}");
            string endOnly = " ab";
            Console.WriteLine($"trimend-same={ReferenceEquals(endOnly, endOnly.TrimEnd())} [{endOnly.TrimStart()}]");

            // --- IndexOf(char[, startIndex[, count]]): the explicit arities ---
            Console.WriteLine($"idx={"abcabc".IndexOf('b')}/{"abcabc".IndexOf('z')}");
            Console.WriteLine($"idxs={IdxChar("abcabc", 'b', 2)}/{IdxChar("abcabc", 'b', 6)}"
                + $"/{IdxChar("abcabc", 'b', 7)}/{IdxChar("abcabc", 'b', -1)}");
            // The fixed 3-arg form: multiple hits, count-limited miss, bounds.
            Console.WriteLine($"idxr={IdxCharRange("abcabc", 'b', 0, 6)}/{IdxCharRange("abcabc", 'b', 2, 4)}"
                + $"/{IdxCharRange("abcabc", 'b', 2, 3)}/{IdxCharRange("abcabc", 'b', 2, 2)}");
            Console.WriteLine($"idxr2={IdxCharRange("abcabc", 'b', 6, 0)}/{IdxCharRange("abcabc", 'b', 7, 0)}"
                + $"/{IdxCharRange("abcabc", 'b', 2, 5)}/{IdxCharRange("abcabc", 'b', 2, -1)}/{IdxCharRange("abcabc", 'b', -1, 2)}");
            // IndexOf(char, StringComparison), ordinal arms.
            Console.WriteLine($"idxcmp={"abcABC".IndexOf('B', StringComparison.Ordinal)}"
                + $"/{"abcABC".IndexOf('B', StringComparison.OrdinalIgnoreCase)}"
                + $"/{"abc".IndexOf('z', StringComparison.Ordinal)}");
            // IndexOf(string, startIndex): empty needle -> startIndex; bounds.
            Console.WriteLine($"idxstr={IdxStr("abcabc", "bc", 2)}/{IdxStr("abcabc", "bc", 6)}"
                + $"/{IdxStr("abcabc", "", 3)}/{IdxStr("abcabc", "", 6)}"
                + $"/{IdxStr("abcabc", "bc", 7)}/{IdxStr("abcabc", "bc", -1)}");

            // --- IndexOfAny / LastIndexOfAny: hit, miss, empty set, null set ---
            Console.WriteLine($"any={"abcd".IndexOfAny(new[] { 'c', 'b' })}/{"abcd".IndexOfAny(new[] { 'z' })}"
                + $"/{"abcd".IndexOfAny(new char[0])}/{"".IndexOfAny(new[] { 'a' })}");
            Console.WriteLine($"any2={"abcd".IndexOfAny(new[] { 'd' }, 2)}/{"abcd".IndexOfAny(new[] { 'a' }, 2)}"
                + $"/{"abcd".IndexOfAny(new[] { 'a' }, 4)}");
            Console.WriteLine($"any3={AnyOf("abcd", new[] { 'd' }, 2, 2)}/{AnyOf("abcd", new[] { 'a' }, 2, 3)}"
                + $"/{AnyOf("abcd", new[] { 'a' }, 5, 0)}/{AnyOf("abcd", null, 1, 1)}");
            Console.WriteLine($"lany={"abcd".LastIndexOfAny(new[] { 'b', 'c' })}/{"abcd".LastIndexOfAny(new char[0])}"
                + $"/{"".LastIndexOfAny(new[] { 'a' })}/{"abcab".LastIndexOfAny(new[] { 'a' }, 3)}");
            Console.WriteLine($"lany3={LastAnyOf("abcab", new[] { 'a' }, 3, 3)}/{LastAnyOf("abcab", new[] { 'a' }, 3, 4)}"
                + $"/{LastAnyOf("abcab", new[] { 'a' }, 3, 5)}/{LastAnyOf("abcab", new[] { 'a' }, 5, 1)}"
                + $"/{LastAnyOf("abcab", new[] { 'a' }, 4, 0)}/{LastAnyOf("", new[] { 'a' }, 0, 0)}/{LastAnyOf("abcd", null, 1, 1)}");

            // --- LastIndexOf(char, startIndex[, count]): backward from
            // startIndex inclusive; empty source -1 before validation ---
            Console.WriteLine($"lidx={LastChar("abc", 'b', 2)}/{LastChar("abc", 'b', 1)}/{LastChar("abc", 'b', 0)}"
                + $"/{LastChar("abc", 'c', 3)}/{LastChar("abc", 'c', -1)}/{LastChar("", 'a', 0)}/{LastChar("", 'a', -1)}");
            Console.WriteLine($"lidxr={LastCharRange("abcabc", 'b', 4, 4)}/{LastCharRange("abcabc", 'b', 3, 3)}"
                + $"/{LastCharRange("abcabc", 'b', 3, 2)}/{LastCharRange("abc", 'b', 2, 0)}"
                + $"/{LastCharRange("abcabc", 'b', 5, 7)}/{LastCharRange("abcabc", 'b', 6, 1)}/{LastCharRange("", 'a', 0, 0)}");

            // --- LastIndexOf(string ...): empty needle at Length / startIndex+1,
            // matches must end at or before startIndex ---
            Console.WriteLine($"lstr={"abcabc".LastIndexOf("bc")}/{"abcabc".LastIndexOf("abcabcd")}"
                + $"/{"abc".LastIndexOf("")}/{"".LastIndexOf("")}");
            Console.WriteLine($"lstrcmp={"abcABC".LastIndexOf("bc", StringComparison.Ordinal)}"
                + $"/{"abcABC".LastIndexOf("bc", StringComparison.OrdinalIgnoreCase)}"
                + $"/{"abcABC".LastIndexOf("", StringComparison.Ordinal)}");
            Console.WriteLine($"lstrs={LastStr("abcabc", "bc", 4)}/{LastStr("abcabc", "bc", 6)}"
                + $"/{LastStr("abc", "abc", 1)}/{LastStr("abc", "abc", 2)}/{LastStr("", "a", 0)}"
                + $"/{LastStr("abcabc", "bc", 7)}/{LastStr("abcabc", "bc", -1)}");
            Console.WriteLine($"lstre={LastStr("abc", "", 0)}/{LastStr("abc", "", 1)}/{LastStr("abc", "", 3)}/{LastStr("abc", "", 4)}");
            Console.WriteLine($"lstrr={LastStrRange("abcabc", "bc", 4, 4)}/{LastStrRange("abcabc", "bc", 4, 3)}"
                + $"/{LastStrRange("abcabc", "bc", 5, 2)}/{LastStrRange("abcabc", "bc", 6, 7)}"
                + $"/{LastStrRange("abcabc", "bc", 6, 0)}/{LastStrRange("abcabc", "bc", 5, 7)}");
            Console.WriteLine($"lstrr2={LastStrRange("abc", "c", 3, 4)}/{LastStrRange("abc", "c", 3, 5)}"
                + $"/{LastStrRange("abc", "c", 2, 3)}/{LastStrRange("abc", "", 2, 2)}/{LastStrRange("abc", "", 2, 0)}");
            // (string, startIndex, StringComparison): OrdinalIgnoreCase backward scan.
            Console.WriteLine($"lstrscmp={"abcABC".LastIndexOf("BC", 5, StringComparison.OrdinalIgnoreCase)}"
                + $"/{"abcABC".LastIndexOf("BC", 3, StringComparison.OrdinalIgnoreCase)}"
                + $"/{"abcabc".LastIndexOf("bc", 4, StringComparison.Ordinal)}");
        }

        private static string _rangeEvaluation = "";

        private static string Source(string value)
        {
            _rangeEvaluation += "S";
            return value;
        }

        private static char[] Set(char[] value)
        {
            _rangeEvaluation += "A";
            return value;
        }

        private static int Number(string step, int value, bool fail)
        {
            _rangeEvaluation += step;
            if (fail)
                throw new InvalidOperationException();
            return value;
        }

        private static void RangeFault(string label, Func<int> call)
        {
            try
            {
                Console.WriteLine(label + " result=" + call());
            }
            catch (Exception ex)
            {
                Console.WriteLine(label + " type=" + ex.GetType().Name);
                Console.WriteLine(label + " param=" + (ex is ArgumentException arg ? arg.ParamName : null));
                Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
                object actual = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
                Console.WriteLine(label + " actual=" + (actual is null ? "null" : actual.GetType().Name + ":" + actual));
            }
        }

        internal static void RunFaults()
        {
            Console.WriteLine("== string search range faults ==");
            string[] sources = { "abca", "", null };
            int[] starts = { int.MinValue, -1, 0, 3, 4, 5, int.MaxValue };
            int[] counts = { int.MinValue, -1, 0, 1, 4, int.MaxValue };
            char[] set = { 'a' };
            for (int i = 0; i < sources.Length; i++)
            {
                string source = sources[i];
                foreach (int start in starts)
                {
                    string tag = i + ":" + start;
                    RangeFault("index char start " + tag, () => source.IndexOf('a', start));
                    RangeFault("last char start " + tag, () => source.LastIndexOf('a', start));
                    RangeFault("index string start " + tag, () => source.IndexOf("a", start));
                    RangeFault("last string start " + tag, () => source.LastIndexOf("a", start));
                    RangeFault("index comparison start " + tag, () => source.IndexOf("a", start, StringComparison.Ordinal));
                    RangeFault("last comparison start " + tag, () => source.LastIndexOf("a", start, StringComparison.Ordinal));
                    RangeFault("index set start " + tag, () => source.IndexOfAny(set, start));
                    RangeFault("last set start " + tag, () => source.LastIndexOfAny(set, start));
                    foreach (int count in counts)
                    {
                        string window = tag + ":" + count;
                        RangeFault("index char range " + window, () => source.IndexOf('a', start, count));
                        RangeFault("last char range " + window, () => source.LastIndexOf('a', start, count));
                        RangeFault("index string range " + window, () => source.IndexOf("a", start, count));
                        RangeFault("last string range " + window, () => source.LastIndexOf("a", start, count));
                        RangeFault("last empty range " + window, () => source.LastIndexOf("", start, count));
                        RangeFault("index set range " + window, () => source.IndexOfAny(set, start, count));
                        RangeFault("last set range " + window, () => source.LastIndexOfAny(set, start, count));
                    }
                }
            }
            RangeFault("index null set", () => "abc".IndexOfAny(null, int.MinValue));
            RangeFault("index null set range", () => "abc".IndexOfAny(null, int.MinValue, int.MinValue));
            RangeFault("last null set", () => "abc".LastIndexOfAny(null, int.MaxValue));
            RangeFault("last null set range", () => "abc".LastIndexOfAny(null, int.MaxValue, int.MinValue));
            RangeFault("empty last null set", () => "".LastIndexOfAny(null, -1, -1));
            RangeFault("empty last set", () => "".LastIndexOfAny(Array.Empty<char>(), int.MaxValue, int.MinValue));
            RangeFault("empty index set", () => "abc".IndexOfAny(Array.Empty<char>(), 3, 0));
            RangeFault("empty index bad start", () => "abc".IndexOfAny(Array.Empty<char>(), 4, -1));
            RangeFault("index null value", () => "abc".IndexOf(null, -1, -1));
            RangeFault("last null value", () => "".LastIndexOf(null, -1, -1));
            Console.WriteLine("range utf16=" + "A\0\ud800a".IndexOf('\ud800', 1, 2) + ":"
                + "A\0\ud800a".LastIndexOf('\0', 2, 2));
            _rangeEvaluation = "";
            RangeFault("range null evaluation", () => Source(null).IndexOfAny(Set(null), Number("I", -1, false), Number("C", -1, false)));
            Console.WriteLine("range null evaluation=" + _rangeEvaluation);
            _rangeEvaluation = "";
            RangeFault("range throwing count", () => Source(null).IndexOfAny(Set(null), Number("I", -1, false), Number("C", -1, true)));
            Console.WriteLine("range throwing count evaluation=" + _rangeEvaluation);
            Console.WriteLine("string search range faults end");
        }
    }
}
