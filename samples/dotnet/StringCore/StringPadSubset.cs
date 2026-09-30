#nullable disable
using System;

namespace StringPadSubset
{
    // PadLeft/PadRight (default ' ' or an explicit pad char), Remove (to-end or a
    // range) and Insert — ordinal/code-unit based, matching .NET.
    internal static class Program
    {
        private static string _evaluation = "";

        private static string Text(string value)
        {
            _evaluation += "S";
            return value;
        }

        private static int Number(string step, int value)
        {
            _evaluation += step;
            return value;
        }

        private static char Padding()
        {
            _evaluation += "P";
            return '.';
        }

        private static int ThrowingCount()
        {
            _evaluation += "C";
            throw new InvalidOperationException();
        }

        private static char ThrowingPadding()
        {
            _evaluation += "P";
            throw new InvalidOperationException();
        }

        private static string Units(string value)
        {
            string result = "";
            foreach (char c in value)
                result += ((int)c).ToString("X4") + " ";
            return result;
        }

        private static void Probe(string label, Func<string> body)
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
                    if (ex is ArgumentOutOfRangeException range)
                        Console.WriteLine(label + " actual=" + (range.ActualValue is null
                            ? "null" : range.ActualValue.GetType().Name + ":" + range.ActualValue));
                }
            }
        }

        internal static void RunFaults()
        {
            Console.WriteLine("== string remove and padding faults ==");
            string source = new string(new[] { 'a', 'b', 'c' });
            Probe("remove-tail-negative", () => source.Remove(-1));
            Probe("remove-tail-minimum", () => source.Remove(int.MinValue));
            Probe("remove-tail-past-end", () => source.Remove(4));
            Probe("remove-tail-maximum", () => source.Remove(int.MaxValue));
            Probe("remove-start-before-count", () => source.Remove(-1, -1));
            Probe("remove-start-minimum", () => source.Remove(int.MinValue, int.MinValue));
            Probe("remove-count-before-window", () => source.Remove(int.MaxValue, -1));
            Probe("remove-count-minimum", () => source.Remove(0, int.MinValue));
            Probe("remove-window", () => source.Remove(2, 2));
            Probe("remove-past-end-zero", () => source.Remove(4, 0));
            Probe("remove-start-maximum", () => source.Remove(int.MaxValue, 0));
            Probe("remove-count-maximum", () => source.Remove(0, int.MaxValue));
            Probe("remove-empty-window", () => "".Remove(0, 1));
            Probe("remove-null-tail", () => ((string)null).Remove(int.MinValue));
            Probe("remove-null-range", () => ((string)null).Remove(-1, -1));
            Probe("pad-left-default", () => source.PadLeft(-1));
            Probe("pad-left-explicit", () => source.PadLeft(int.MinValue, '.'));
            Probe("pad-right-default", () => source.PadRight(int.MinValue));
            Probe("pad-right-explicit", () => source.PadRight(-1, '.'));
            Probe("pad-null", () => ((string)null).PadLeft(-1, '.'));
            Console.WriteLine("remove unchanged=" + ReferenceEquals(source, source.Remove(3))
                + ":" + ReferenceEquals(source, source.Remove(0, 0))
                + ":" + ReferenceEquals(source, source.Remove(3, 0)));
            Console.WriteLine("remove empty identities=" + ReferenceEquals(source.Remove(0), string.Empty)
                + ":" + ReferenceEquals(source.Remove(0, 3), string.Empty)
                + ":" + ReferenceEquals("".Remove(0), string.Empty)
                + ":" + ReferenceEquals("".Remove(0, 0), string.Empty));
            string[] emptySources = { new string('x', 0), new string(ReadOnlySpan<char>.Empty) };
            for (int i = 0; i < emptySources.Length; i++)
                Console.WriteLine("constructed empty " + i + "="
                    + ReferenceEquals(emptySources[i].Remove(0), string.Empty) + ":"
                    + ReferenceEquals(emptySources[i].Remove(0, 0), emptySources[i]));
            Console.WriteLine("pad unchanged=" + ReferenceEquals(source, source.PadLeft(0))
                + ":" + ReferenceEquals(source, source.PadLeft(3, '.'))
                + ":" + ReferenceEquals(source, source.PadRight(2))
                + ":" + ReferenceEquals(source, source.PadRight(3, '.')));
            Probe("remove-copy", () => source.Remove(1, 1));
            Probe("remove-fresh", () => ReferenceEquals(source.Remove(1, 1), source.Remove(1, 1)).ToString());
            string units = "A\u0000\uD800\uDC00Z";
            Probe("remove-utf16", () => Units(units.Remove(2, 1)));
            Probe("pad-left-utf16", () => Units(units.PadLeft(7, '\uD800')));
            Probe("pad-right-utf16", () => Units(units.PadRight(6, '\u0000')));
            GC.Collect();
            Console.WriteLine("remove identity after GC=" + ReferenceEquals(source, source.Remove(3, 0))
                + ":" + ReferenceEquals(source.Remove(0), string.Empty));
            _evaluation = "";
            Probe("remove evaluated null", () => Text(null).Remove(Number("I", -1), Number("C", -1)));
            Console.WriteLine("remove null evaluation=" + _evaluation);
            _evaluation = "";
            Probe("remove evaluated copy", () => Text(source).Remove(Number("I", 1), Number("C", 1)));
            Console.WriteLine("remove copy evaluation=" + _evaluation);
            _evaluation = "";
            Probe("remove throwing count", () => Text(null).Remove(Number("I", -1), ThrowingCount()));
            Console.WriteLine("remove throwing count evaluation=" + _evaluation);
            _evaluation = "";
            Probe("pad evaluated null", () => Text(null).PadRight(Number("W", -1), Padding()));
            Console.WriteLine("pad null evaluation=" + _evaluation);
            _evaluation = "";
            Probe("pad throwing padding", () => Text(null).PadLeft(Number("W", -1), ThrowingPadding()));
            Console.WriteLine("pad throwing padding evaluation=" + _evaluation);
            Console.WriteLine("string remove and padding faults end");
        }

        internal static void Run()
        {
            Console.WriteLine("[" + "abc".PadLeft(5) + "]");        // [ abc]
            Console.WriteLine("[" + "abc".PadLeft(5, '0') + "]");   // [00abc]
            Console.WriteLine("[" + "abc".PadLeft(2) + "]");        // [abc] (no widening)
            Console.WriteLine("[" + "abc".PadRight(5, '-') + "]");  // [abc--]
            Console.WriteLine("[" + "abc".PadRight(3) + "]");       // [abc]

            Console.WriteLine("[" + "hello".Remove(2) + "]");       // [he]
            Console.WriteLine("[" + "hello".Remove(1, 2) + "]");    // [hlo]
            Console.WriteLine("[" + "hello".Remove(0, 5) + "]");    // []

            Console.WriteLine("[" + "abc".Insert(1, "XY") + "]");   // [aXYbc]
            Console.WriteLine("[" + "abc".Insert(0, ">>") + "]");   // [>>abc]
            Console.WriteLine("[" + "abc".Insert(3, "!") + "]");    // [abc!]
        }
    }
}
