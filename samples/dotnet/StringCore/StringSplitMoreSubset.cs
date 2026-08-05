#nullable disable
using System;

namespace StringSplitMoreSubset
{
    // The full String.Split overload family: char / char[] / string / string[]
    // separators, the count budget, the count x RemoveEmptyEntries x TrimEntries matrix
    // including the remainder-entry rule, whitespace split over full-Unicode
    // whitespace, first-match-wins string-array separators, and options validation.
    internal static class Program
    {
        private static string Dump(string[] a)
        {
            string body = "";
            for (int i = 0; i < a.Length; i++)
            {
                if (i > 0)
                {
                    body += "|";
                }
                body += "\"" + a[i] + "\"";
            }
            return "[" + a.Length + "](" + body + ")";
        }

        private static void P(string label, string[] a)
        {
            Console.WriteLine(label + " " + Dump(a));
        }

        internal static void Run()
        {
            const StringSplitOptions None = StringSplitOptions.None;
            const StringSplitOptions Ree = StringSplitOptions.RemoveEmptyEntries;
            const StringSplitOptions Trim = StringSplitOptions.TrimEntries;

            // ---- every overload, one straightforward call each ----
            P("ov-char", "a,b,c".Split(','));                                    // (char, opts=None)
            P("ov-char-opts", "a,,b".Split(',', Ree));                           // (char, opts)
            P("ov-char-count", "a,b,c".Split(',', 2));                           // (char, int, opts=None)
            P("ov-char-count-opts", "a,,b,c".Split(',', 2, Ree));                // (char, int, opts)
            P("ov-chararr", "a,b;c".Split(new[] { ',', ';' }));                  // (params char[])
            P("ov-chararr-count", "a,b,c".Split(new[] { ',' }, 2));              // (char[], int)
            P("ov-chararr-opts", "a,,b".Split(new[] { ',' }, Ree));              // (char[], opts)
            P("ov-chararr-count-opts", "a,,b,c".Split(new[] { ',' }, 2, Ree));   // (char[], int, opts)
            P("ov-str", "a::b:c".Split("::"));                                   // (string, opts=None)
            P("ov-str-opts", "a::::b".Split("::", Ree));                         // (string, opts)
            P("ov-str-count", "a::b::c".Split("::", 2));                         // (string, int, opts=None)
            P("ov-str-count-opts", "a::::b::c".Split("::", 2, Ree));             // (string, int, opts)
            P("ov-strarr", "a,b;c".Split(new[] { ",", ";" }, None));             // (string[], opts)
            P("ov-strarr-count", "a,b,c,d".Split(new[] { "," }, 2, None));       // (string[], int, opts)

            // ---- count semantics ----
            P("count0", "a,b".Split(',', 0));
            P("count0-ree", "a,b".Split(',', 0, Ree));
            P("count0-empty", "".Split(',', 0));
            P("count1", "a,b".Split(',', 1));
            P("count1-trim", " a,b ".Split(',', 1, Trim));
            P("count1-trim-allws", "   ".Split(',', 1, Trim));
            P("count1-trim-ree-allws", "   ".Split(',', 1, Trim | Ree));
            P("count1-ree-empty", "".Split(',', 1, Ree));
            P("count-big", "a,b".Split(',', 99));
            P("count-str0", "a::b".Split("::", 0));
            try
            {
                P("count-neg", "a,b".Split(',', -1));
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("count-neg throws ArgumentOutOfRangeException");
            }
            try
            {
                P("count-neg-chararr", "a,b".Split(new[] { ',' }, -3));
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("count-neg-chararr throws ArgumentOutOfRangeException");
            }
            // count < 0 is rejected before the invalid options value.
            try
            {
                P("count-neg-opts4", "a,b".Split(',', -1, (StringSplitOptions)4));
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("count-neg-opts4 throws ArgumentOutOfRangeException");
            }

            // ---- options validation: outside [0, 3] is ArgumentException,
            // checked even when count == 0 ----
            try
            {
                P("opts4", "a,b".Split(',', (StringSplitOptions)4));
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("opts4 throws ArgumentOutOfRangeException");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("opts4 throws ArgumentException");
            }
            try
            {
                P("opts4-count0", "a,b".Split(',', 0, (StringSplitOptions)4));
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("opts4-count0 throws ArgumentOutOfRangeException");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("opts4-count0 throws ArgumentException");
            }
            try
            {
                P("opts-neg", "a,b".Split(new[] { "," }, (StringSplitOptions)(-1)));
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("opts-neg throws ArgumentOutOfRangeException");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("opts-neg throws ArgumentException");
            }

            // ---- count x options interaction: the remainder entry ----
            // Empty entries removed by RemoveEmptyEntries do not consume the
            // count budget; right before the remainder, would-be-removed
            // entries are skipped (their separators consumed) first.
            P("rem-ree", "a,,b,c".Split(',', 2, Ree));
            P("rem-ree-lead", ",,a,,b,c".Split(',', 2, Ree));
            P("rem-ree-tailempty", "a,,,".Split(',', 2, Ree));
            P("rem-ree-mid", "a,,b".Split(',', 3, Ree));
            // TrimEntries trims the remainder's outer edges only; inner
            // separators stay.
            P("rem-trim", " a , b , c ".Split(',', 2, Trim));
            P("rem-trim-ree", " a ,  , b , c ".Split(',', 2, Trim | Ree));
            P("rem-trim-ree-tailws", "a, b ,  ".Split(',', 2, Trim | Ree));
            P("rem-str-count-trim", " a :: b :: c ".Split("::", 2, Trim));

            // ---- whitespace split: null/empty separator arrays, full-Unicode
            // whitespace (U+00A0 NBSP, U+3000 ideographic space) ----
            P("ws-null-chararr", "a b\tc".Split((char[])null));
            P("ws-empty-chararr", "a b c".Split(new char[0]));
            P("ws-null-chararr-count", "a b c".Split((char[])null, 2));
            P("ws-null-strarr", "a b".Split((string[])null, None));
            P("ws-empty-strarr", "a b".Split(new string[0], None));
            P("ws-null-strarr-count", "a b c".Split((string[])null, 2, None));
            P("ws-unicode", "a b　c".Split((char[])null));
            P("ws-nbsp", "a\u00A0b c".Split((char[])null));
            P("ws-nbsp-trim", "\u00A0a,b\u00A0".Split(',', StringSplitOptions.TrimEntries));
            P("ws-unicode-ree", " a　　b ".Split((char[])null, Ree));
            P("ws-allws-none", " \t ".Split((char[])null));
            P("ws-allws-ree", " \t　".Split((char[])null, Ree));

            // ---- single string separator: null/empty is NOT whitespace mode,
            // the whole string comes back as the sole entry ----
            P("strsep-null", "a b".Split((string)null, None));
            P("strsep-empty", "a b".Split("", None));
            P("strsep-longer", "ab".Split("abc", None));
            P("strsep-self", "abc".Split("abc", None));
            P("strsep-self-ree", "abc".Split("abc", Ree));
            P("strsep-overlap", "aaa".Split("aa", None));

            // ---- string[] separators: first matching entry in array order
            // wins at each position, null/empty entries skipped ----
            P("strarr-firstwins", "aXYb".Split(new[] { "XY", "X" }, None));
            P("strarr-firstwins2", "aXYb".Split(new[] { "X", "XY" }, None));
            P("strarr-poswins", "abcd".Split(new[] { "cd", "bcd" }, None));
            P("strarr-nullentry", "a,b;c".Split(new[] { null, ",", "", ";" }, None));
            P("strarr-allnull", "a b".Split(new string[] { null, "" }, None));
            P("strarr-trim-ree", " a ,, b , ".Split(new[] { ",," }, Trim | Ree));

            // ---- empty input / no separator present ----
            P("empty-none", "".Split(','));
            P("empty-ree", "".Split(',', Ree));
            P("empty-trim", "".Split(',', Trim));
            P("nosep", "abc".Split(','));
            P("nosep-trim", " abc ".Split(',', Trim));
            P("lead-trail", ",a,".Split(','));

            // Splitting still dispatches IEnumerable<string> on the result.
            foreach (string p in "x;y".Split(new[] { ";" }, None))
            {
                Console.WriteLine("iter [" + p + "]");
            }
        }
    }
}
