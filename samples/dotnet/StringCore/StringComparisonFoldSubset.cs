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

        private static string _faultEvaluation = "";

        private static string Text(string step, string value)
        {
            _faultEvaluation += step;
            return value;
        }

        private static StringComparison Comparison(bool fail)
        {
            _faultEvaluation += "C";
            if (fail)
                throw new InvalidOperationException();
            return (StringComparison)6;
        }

        private static void Fault(string label, Func<int> call)
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
            }
        }

        internal static void RunFaults()
        {
            Console.WriteLine("== string comparison faults ==");
            string missing = null;
            foreach (int raw in new[] { -1, 6, int.MinValue, int.MaxValue })
            {
                StringComparison cmp = (StringComparison)raw;
                string tag = raw.ToString();
                Fault("starts " + tag, () => "abc".StartsWith("", cmp) ? 1 : 0);
                Fault("ends " + tag, () => "abc".EndsWith("", cmp) ? 1 : 0);
                Fault("contains " + tag, () => "".Contains("", cmp) ? 1 : 0);
                Fault("index " + tag, () => "".IndexOf("", cmp));
                Fault("last " + tag, () => "".LastIndexOf("", cmp));
                Fault("char index " + tag, () => "".IndexOf('a', cmp));
                Fault("index start " + tag, () => "abc".IndexOf("a", 0, cmp));
                Fault("last start " + tag, () => "abc".LastIndexOf("a", 2, cmp));
                Fault("null starts " + tag, () => "abc".StartsWith(null, cmp) ? 1 : 0);
                Fault("null ends " + tag, () => "abc".EndsWith(null, cmp) ? 1 : 0);
                Fault("null contains " + tag, () => "abc".Contains(null, cmp) ? 1 : 0);
                Fault("null index " + tag, () => "abc".IndexOf(null, cmp));
                Fault("null last " + tag, () => "abc".LastIndexOf(null, cmp));
                Fault("null index start " + tag, () => "abc".IndexOf(null, -1, cmp));
                Fault("null last start " + tag, () => "abc".LastIndexOf(null, -1, cmp));
                Fault("receiver starts " + tag, () => missing.StartsWith(null, cmp) ? 1 : 0);
                Fault("receiver ends " + tag, () => missing.EndsWith(null, cmp) ? 1 : 0);
                Fault("receiver contains " + tag, () => missing.Contains(null, cmp) ? 1 : 0);
                Fault("receiver index " + tag, () => missing.IndexOf(null, cmp));
                Fault("receiver last " + tag, () => missing.LastIndexOf(null, cmp));
                Fault("receiver char " + tag, () => missing.IndexOf('a', cmp));
                Fault("compare null " + tag, () => string.Compare(null, null, cmp));
                Fault("compare window " + tag, () => string.Compare(null, -1, null, -1, -1, cmp));
                Fault("equals null " + tag, () => string.Equals(null, null, cmp) ? 1 : 0);
                Fault("equals same " + tag, () => "abc".Equals("abc", cmp) ? 1 : 0);
                Fault("equals receiver " + tag, () => missing.Equals(null, cmp) ? 1 : 0);
                Fault("replace null " + tag, () => "abc".Replace(null, null, cmp).Length);
                Fault("replace empty " + tag, () => "abc".Replace("", null, cmp).Length);
                Fault("replace receiver " + tag, () => missing.Replace(null, null, cmp).Length);
                Fault("hash empty " + tag, () => string.GetHashCode(ReadOnlySpan<char>.Empty, cmp));
                Fault("span equals " + tag, () => MemoryExtensions.Equals("a".AsSpan(), "a".AsSpan(), cmp) ? 1 : 0);
            }
            Fault("plain starts", () => "abc".StartsWith(null) ? 1 : 0);
            Fault("plain ends", () => "abc".EndsWith(null) ? 1 : 0);
            Fault("plain contains", () => "abc".Contains((string)null) ? 1 : 0);
            Fault("plain index", () => "abc".IndexOf((string)null));
            Fault("plain last", () => "abc".LastIndexOf((string)null));
            Fault("plain index range", () => "abc".IndexOf(null, -1, -1));
            Fault("plain last range", () => "abc".LastIndexOf(null, -1, -1));
            Fault("valid equals receiver", () => missing.Equals(null, StringComparison.Ordinal) ? 1 : 0);
            for (int raw = 0; raw <= 5; raw++)
            {
                StringComparison cmp = (StringComparison)raw;
                Console.WriteLine("valid " + raw + "=" + "abc".StartsWith("a", cmp) + ":"
                    + "abc".EndsWith("c", cmp) + ":" + "abc".Contains("b", cmp) + ":"
                    + "abc".IndexOf("b", cmp) + ":" + "abc".LastIndexOf("b", cmp));
                Console.WriteLine("empty " + raw + "=" + "".StartsWith("", cmp) + ":"
                    + "".EndsWith("", cmp) + ":" + "".Contains("", cmp) + ":"
                    + "".IndexOf("", cmp) + ":" + "".LastIndexOf("", cmp));
            }
            Console.WriteLine("ordinal unicode=" + "\u00c4\0\ud800".IndexOf("\u00e4\0", StringComparison.OrdinalIgnoreCase)
                + ":" + "\u00c4\0\ud800".IndexOf("\u00e4\0", StringComparison.Ordinal));
            _faultEvaluation = "";
            Fault("null evaluation", () => Text("S", null).StartsWith(Text("V", null), Comparison(false)) ? 1 : 0);
            Console.WriteLine("null evaluation=" + _faultEvaluation);
            _faultEvaluation = "";
            Fault("throwing comparison", () => Text("S", null).StartsWith(Text("V", null), Comparison(true)) ? 1 : 0);
            Console.WriteLine("throwing comparison evaluation=" + _faultEvaluation);
            _faultEvaluation = "";
            Fault("equals evaluation", () => Text("S", null).Equals(Text("V", null), Comparison(false)) ? 1 : 0);
            Console.WriteLine("equals evaluation=" + _faultEvaluation);
            _faultEvaluation = "";
            Fault("equals throwing comparison", () => Text("S", null).Equals(Text("V", null), Comparison(true)) ? 1 : 0);
            Console.WriteLine("equals throwing comparison evaluation=" + _faultEvaluation);
            Console.WriteLine("string comparison faults end");
        }
    }
}
