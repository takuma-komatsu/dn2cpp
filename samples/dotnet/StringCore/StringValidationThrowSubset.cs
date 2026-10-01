#nullable disable
using System;

namespace StringValidationThrowSubset
{
    // String argument faults must unwind normally. Window probes distinguish the
    // unsigned start check's UInt32 value from the signed room and length checks.
    internal static class Program
    {
        private static string _evaluation = "";

        private static string Text(string step, string value)
        {
            _evaluation += step;
            return value;
        }

        private static int Number(string step, int value)
        {
            _evaluation += step;
            return value;
        }

        private static string ThrowingValue()
        {
            _evaluation += "V";
            throw new InvalidOperationException();
        }

        private static int ThrowingLength()
        {
            _evaluation += "L";
            throw new InvalidOperationException();
        }

        private static string Units(string value)
        {
            string result = "";
            foreach (char c in value)
                result += ((int)c).ToString("X4") + " ";
            return result;
        }

        private static void ProbeWindow(string label, Func<string> body)
        {
            try
            {
                Console.WriteLine(label + " result=" + body());
            }
            catch (Exception ex)
            {
                Console.WriteLine(label + " " + ex.GetType().Name);
                if (ex is ArgumentException argument)
                {
                    Console.WriteLine(label + " param=" + argument.ParamName);
                    Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
                    if (ex is ArgumentOutOfRangeException range && range.ActualValue is not null)
                        Console.WriteLine(label + " actual=" + range.ActualValue.GetType().Name + ":" + range.ActualValue);
                }
            }
        }

        internal static void RunWindows()
        {
            Console.WriteLine("== string window faults ==");
            ProbeWindow("insert-negative", () => "abc".Insert(-1, "x"));
            ProbeWindow("insert-minimum", () => "abc".Insert(int.MinValue, "x"));
            ProbeWindow("insert-maximum", () => "abc".Insert(int.MaxValue, "x"));
            ProbeWindow("insert-past-end", () => "abc".Insert(4, "x"));
            ProbeWindow("insert-null-before-index", () => "abc".Insert(-1, null));
            ProbeWindow("insert-empty-bad-index", () => "abc".Insert(-1, ""));
            ProbeWindow("insert-null-receiver", () => ((string)null).Insert(-1, null));
            ProbeWindow("insert-start", () => "abc".Insert(0, ">"));
            ProbeWindow("insert-end", () => "abc".Insert(3, "<"));
            ProbeWindow("insert-empty-identity", () => ReferenceEquals("abc".Insert(1, ""), "abc").ToString());
            ProbeWindow("insert-empty-source-identity", () => ReferenceEquals("".Insert(0, "abc"), "abc").ToString());
            ProbeWindow("insert-utf16", () => Units("\u0000\uD800Z".Insert(2, "\uDC00")));

            ProbeWindow("array-negative-start", () => new string("abc".ToCharArray(-1, -1)));
            ProbeWindow("array-minimum-start", () => new string("abc".ToCharArray(int.MinValue, int.MinValue)));
            ProbeWindow("array-maximum-start", () => new string("abc".ToCharArray(int.MaxValue, -1)));
            ProbeWindow("array-past-end-before-length", () => new string("abc".ToCharArray(4, -1)));
            ProbeWindow("array-window", () => new string("abc".ToCharArray(1, 4)));
            ProbeWindow("array-maximum-length", () => new string("abc".ToCharArray(0, int.MaxValue)));
            ProbeWindow("array-negative-length", () => new string("abc".ToCharArray(0, -1)));
            ProbeWindow("array-minimum-length", () => new string("abc".ToCharArray(0, int.MinValue)));
            ProbeWindow("array-wrapped-room", () => new string("abc".ToCharArray(0, int.MinValue + 1)));
            ProbeWindow("array-end-negative-length", () => new string("abc".ToCharArray(3, -1)));
            ProbeWindow("array-null-receiver", () => new string(((string)null).ToCharArray(-1, -1)));
            ProbeWindow("array-empty-at-end", () => "abc".ToCharArray(3, 0).Length.ToString());
            ProbeWindow("array-empty-source", () => "".ToCharArray(0, 0).Length.ToString());
            char[] empty = Array.Empty<char>();
            Console.WriteLine("empty array identities=" + ReferenceEquals(empty, "abc".ToCharArray(3, 0))
                + ":" + ReferenceEquals(empty, "".ToCharArray(0, 0))
                + ":" + ReferenceEquals(empty, "".ToCharArray()));
            GC.Collect();
            Console.WriteLine("empty array after GC=" + ReferenceEquals(empty, "abc".ToCharArray(1, 0))
                + ":" + (empty.GetType() == typeof(char[])));
            ProbeWindow("array-window-copy", () => new string("abcde".ToCharArray(1, 3)));
            ProbeWindow("array-utf16", () => Units(new string("A\u0000\uD800\uDC00Z".ToCharArray(1, 3))));
            string source = "abc";
            char[] copy = source.ToCharArray(0, 3);
            copy[0] = 'Q';
            Console.WriteLine("independent array=" + source + ":" + new string(copy));

            _evaluation = "";
            ProbeWindow("insert evaluated null", () => Text("S", null).Insert(Number("I", -1), Text("V", null)));
            Console.WriteLine("insert null evaluation=" + _evaluation);
            _evaluation = "";
            ProbeWindow("insert throwing value", () => Text("S", null).Insert(Number("I", -1), ThrowingValue()));
            Console.WriteLine("insert throwing value evaluation=" + _evaluation);
            _evaluation = "";
            ProbeWindow("array evaluated null", () => new string(Text("S", null).ToCharArray(Number("I", -1), Number("L", -1))));
            Console.WriteLine("array null evaluation=" + _evaluation);
            _evaluation = "";
            ProbeWindow("array throwing length", () => new string(Text("S", null).ToCharArray(Number("I", -1), ThrowingLength())));
            Console.WriteLine("array throwing length evaluation=" + _evaluation);
            Console.WriteLine("string window faults end");
        }

        private static void Catches(string what, Action body)
        {
            try
            {
                body();
                Console.WriteLine(what + " -> no throw");
            }
            catch (Exception e)
            {
                Console.WriteLine(what + " -> " + e.GetType().Name);
            }
        }

        internal static void Run()
        {
            string s = "abc";

            Catches("Substring(10)", () => { string r = s.Substring(10); });
            Catches("Substring(-1)", () => { string r = s.Substring(-1); });
            Catches("Substring(1, 5)", () => { string r = s.Substring(1, 5); });
            Catches("Substring(1, -1)", () => { string r = s.Substring(1, -1); });

            Catches("PadLeft(-1)", () => { string r = s.PadLeft(-1); });
            Catches("PadRight(-1, '.')", () => { string r = s.PadRight(-1, '.'); });

            Catches("Remove(5)", () => { string r = s.Remove(5); });
            Catches("Remove(-1)", () => { string r = s.Remove(-1); });
            Catches("Remove(1, 9)", () => { string r = s.Remove(1, 9); });

            Catches("Insert(-1, \"x\")", () => { string r = s.Insert(-1, "x"); });
            Catches("Insert(9, \"x\")", () => { string r = s.Insert(9, "x"); });
            Catches("Insert(0, null)", () => { string r = s.Insert(0, null); });

            Catches("string.Intern(null)", () => { string r = string.Intern(null); });
            Catches("string.IsInterned(null)", () => { string r = string.IsInterned(null); });

            // The indexer is the odd one out: every row above is an
            // ArgumentOutOfRangeException, while `s[i]` out of range is an
            // IndexOutOfRangeException — for a negative index as much as a
            // past-the-end one. The value is consumed so the read is not dropped as a
            // dead store.
            Catches("s[5]", () => { char c = s[5]; Console.Write("[" + c + "] "); });
            Catches("s[3] (== Length)", () => { char c = s[3]; Console.Write("[" + c + "] "); });
            Catches("s[-1]", () => { char c = s[-1]; Console.Write("[" + c + "] "); });
            Catches("\"\"[0]", () => { char c = string.Empty[0]; Console.Write("[" + c + "] "); });

            // A scanner that walks off the end recovers rather than ending the process.
            int scanned = 0, over = 0;
            for (int i = 0; i < 6; i++)
            {
                try
                {
                    if (s[i] != '\0') scanned++;
                }
                catch (IndexOutOfRangeException)
                {
                    over++;
                }
            }
            Console.WriteLine("scan: scanned=" + scanned + " over=" + over);

            // The recovery is real: the same receiver still works afterwards.
            Console.WriteLine("after faults: [" + s.Substring(1) + "]");

            // A caught fault is an ordinary exception object: a typed catch selects it
            // over a broader one.
            try
            {
                string r = s.Substring(10);
                Console.WriteLine("unreachable " + r);
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("typed catch: ArgumentOutOfRangeException");
            }
            catch (Exception)
            {
                Console.WriteLine("typed catch: fell through to Exception");
            }

            try
            {
                string r = s.Insert(0, null);
                Console.WriteLine("unreachable " + r);
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("typed catch: ArgumentNullException");
            }
            catch (Exception)
            {
                Console.WriteLine("typed catch: fell through to Exception");
            }

            // A fault raised inside a finally-guarded region still runs the
            // finally on the way out.
            int ran = 0;
            try
            {
                try
                {
                    string r = s.Substring(99);
                    Console.WriteLine("unreachable " + r);
                }
                finally
                {
                    ran++;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("finally ran " + ran + " time(s) before the catch");
            }
        }

        private static void ArgumentFault(string label, Exception ex)
        {
            Console.WriteLine(label + " type=" + ex.GetType().Name);
            Console.WriteLine(label + " param=" + (ex is ArgumentException arg ? arg.ParamName : null));
            Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
            object actual = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
            Console.WriteLine(label + " actual=" + (actual is null ? "null" : actual.GetType().Name + ":" + actual));
        }

        private static void ArgumentObserve(string label, Action action)
        {
            try { action(); Console.WriteLine(label + " success"); }
            catch (Exception ex) { ArgumentFault(label, ex); }
        }

        private static void ArgumentDump(string value) { Console.WriteLine("value=" + Units(value)); }

        private static void ArgumentDump(string[] values)
        {
            Console.WriteLine("array length=" + values.Length);
            foreach (string value in values)
                ArgumentDump(value);
        }

        internal static void RunArgumentFields()
        {
            Console.WriteLine("== String argument fields ==");
            string[] sources = { "a,b,,a", "", null };
            string[] olds = { null, "", "a" };
            string[] news = { null, "", "X\0\ud800" };
            int[] comparisons = { 4, 5, -1, 6, int.MinValue, int.MaxValue };
            for (int s = 0; s < sources.Length; s++)
                for (int o = 0; o < olds.Length; o++)
                    for (int n = 0; n < news.Length; n++)
                    {
                        string label = "replace:" + s + ":" + o + ":" + n;
                        ArgumentObserve(label, () => ArgumentDump(sources[s].Replace(olds[o], news[n])));
                        foreach (int comparison in comparisons)
                            ArgumentObserve(label + ":" + comparison, () => ArgumentDump(sources[s].Replace(olds[o], news[n], (StringComparison)comparison)));
                    }
            int[] counts = { int.MinValue, -1, 0, 1, 2, 4, int.MaxValue };
            int[] options = { int.MinValue, -1, 0, 1, 2, 3, 4, int.MaxValue };
            char[][] charSeparators = { null, Array.Empty<char>(), new[] { ',' } };
            string[] stringSeparators = { null, "", "," };
            string[][] arraySeparators = { null, Array.Empty<string>(), new[] { null, "", "," } };
            for (int s = 0; s < sources.Length; s++)
                foreach (int count in counts)
                    foreach (int option in options)
                    {
                        string label = "split:" + s + ":" + count + ":" + option;
                        ArgumentObserve(label + ":char", () => ArgumentDump(sources[s].Split(',', count, (StringSplitOptions)option)));
                        for (int sep = 0; sep < 3; sep++)
                        {
                            ArgumentObserve(label + ":chars:" + sep, () => ArgumentDump(sources[s].Split(charSeparators[sep], count, (StringSplitOptions)option)));
                            ArgumentObserve(label + ":string:" + sep, () => ArgumentDump(sources[s].Split(stringSeparators[sep], count, (StringSplitOptions)option)));
                            ArgumentObserve(label + ":strings:" + sep, () => ArgumentDump(sources[s].Split(arraySeparators[sep], count, (StringSplitOptions)option)));
                        }
                    }
            string[] normalSources = { "abc", "", null };
            int[] forms = { int.MinValue, -1, 0, 1, 2, 5, 6, 7, int.MaxValue };
            for (int s = 0; s < normalSources.Length; s++)
                foreach (int form in forms)
                {
                    ArgumentObserve("normalize:" + s + ":" + form, () => ArgumentDump(normalSources[s].Normalize((System.Text.NormalizationForm)form)));
                    ArgumentObserve("is normalized:" + s + ":" + form, () => Console.WriteLine("normalized=" + normalSources[s].IsNormalized((System.Text.NormalizationForm)form)));
                }
            string[][] arrays = { null, Array.Empty<string>(), new[] { null, "a", "" }, new[] { "X\0\ud800", "b" } };
            string[] separators = { null, "|" };
            int[] bounds = { int.MinValue, -1, 0, 1, 2, 3, int.MaxValue };
            for (int a = 0; a < arrays.Length; a++)
                for (int sep = 0; sep < separators.Length; sep++)
                    foreach (int start in bounds)
                        foreach (int count in bounds)
                            ArgumentObserve("join:" + a + ":" + sep + ":" + start + ":" + count, () => ArgumentDump(string.Join(separators[sep], arrays[a], start, count)));
            ArgumentObserve("intern null", () => ArgumentDump(string.Intern(null)));
            ArgumentObserve("is interned null", () => ArgumentDump(string.IsInterned(null)));
            ArgumentException[] saved = new ArgumentException[5];
            try { "abc".Replace((string)null, "x"); } catch (ArgumentException ex) { saved[0] = ex; }
            try { "abc".Split(',', -7, (StringSplitOptions)(-1)); } catch (ArgumentException ex) { saved[1] = ex; }
            try { "abc".Normalize((System.Text.NormalizationForm)(-1)); } catch (ArgumentException ex) { saved[2] = ex; }
            try { string.Join("|", new[] { "a" }, 0, -7); } catch (ArgumentException ex) { saved[3] = ex; }
            try { string.Intern(null); } catch (ArgumentException ex) { saved[4] = ex; }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int i = 0; i < saved.Length; i++)
                ArgumentFault("fault after GC:" + i, saved[i]);
            Console.WriteLine("String argument fields end");
        }
    }
}
