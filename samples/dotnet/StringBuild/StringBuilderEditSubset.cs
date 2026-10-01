#nullable disable
using System;
using System.Text;

namespace StringBuilderEditSubset
{
    // StringBuilder Insert/Remove/Replace as in-place ops. Insert formats
    // non-char/string values first (like Append); Replace rewrites all occurrences.
    internal static class Program
    {
        internal static void Run()
        {
            StringBuilder sb = new StringBuilder("Hello World");
            sb.Insert(5, ",");
            Console.WriteLine(sb.ToString());        // Hello, World

            sb.Insert(0, ">> ");
            Console.WriteLine(sb.ToString());        // >> Hello, World

            sb.Insert(sb.Length, '!');
            Console.WriteLine(sb.ToString());        // >> Hello, World!

            sb.Insert(0, 42);
            Console.WriteLine(sb.ToString());        // 42>> Hello, World!

            StringBuilder rm = new StringBuilder("abcdefgh");
            rm.Remove(2, 3);
            Console.WriteLine(rm.ToString());        // abfgh

            rm.Remove(0, 2);
            Console.WriteLine(rm.ToString());        // fgh

            StringBuilder rc = new StringBuilder("a.b.c.d");
            rc.Replace('.', '-');
            Console.WriteLine(rc.ToString());        // a-b-c-d

            StringBuilder rs = new StringBuilder("one fish two fish");
            rs.Replace("fish", "cat");
            Console.WriteLine(rs.ToString());        // one cat two cat

            StringBuilder grow = new StringBuilder("xyz");
            grow.Replace("y", "YYYY");
            Console.WriteLine(grow.ToString());      // xYYYYz

            StringBuilder shrink = new StringBuilder("aXXbXXc");
            shrink.Replace("XX", "");
            Console.WriteLine(shrink.ToString());    // abc

            StringBuilder chain = new StringBuilder("12345");
            Console.WriteLine(chain.Insert(0, "[").Insert(chain.Length, "]").ToString()); // [12345]
        }

        private static string _editEvaluation = "";

        private static string Units(string value)
        {
            if (value is null)
                return "null";
            string result = "";
            foreach (char ch in value)
                result += ((int)ch).ToString("X4") + " ";
            return result;
        }

        private static void Fault(string label, Exception ex)
        {
            Console.WriteLine(label + " type=" + ex.GetType().Name);
            Console.WriteLine(label + " param=" + (ex is ArgumentException arg ? arg.ParamName : null));
            Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
            object actual = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
            Console.WriteLine(label + " actual=" + (actual is null ? "null" : actual.GetType().Name + ":" + actual));
        }

        private static StringBuilder Builder(string source)
        {
            if (source is null)
                return null;
            StringBuilder sb = new StringBuilder(1);
            foreach (char ch in source)
                sb.Append(ch);
            return sb;
        }

        private static void Result(string label, StringBuilder sb, Action action)
        {
            try { action(); Console.WriteLine(label + " success"); }
            catch (Exception ex) { Fault(label, ex); }
            Console.WriteLine(label + " content=" + Units(sb is null ? null : sb.ToString()));
        }

        private static StringBuilder Receiver(StringBuilder value)
        {
            _editEvaluation += "R";
            return value;
        }

        private static string Value(string step, string value)
        {
            _editEvaluation += step;
            return value;
        }

        private static int Number(string step, int value, bool fail)
        {
            _editEvaluation += step;
            if (fail)
                throw new InvalidOperationException();
            return value;
        }

        internal static void RunFaults()
        {
            Console.WriteLine("== StringBuilder edit faults ==");
            string[] sources = { "abaca\0\ud800", "", null };
            string[] oldValues = { null, "", "a", "\ud800" };
            string[] newValues = { null, "", "XY" };
            int[] indices = { int.MinValue, -1, 0, 3, 7, 8, int.MaxValue };
            int[] counts = { int.MinValue, -1, 0, 1, 3, 7, int.MaxValue };
            for (int i = 0; i < sources.Length; i++)
                foreach (int start in indices)
                    foreach (int count in counts)
                    {
                        StringBuilder remove = Builder(sources[i]);
                        Result("remove:" + i + ":" + start + ":" + count, remove, () => remove.Remove(start, count));
                        StringBuilder chars = Builder(sources[i]);
                        Result("replace char:" + i + ":" + start + ":" + count, chars, () => chars.Replace('a', 'X', start, count));
                        for (int o = 0; o < oldValues.Length; o++)
                            for (int n = 0; n < newValues.Length; n++)
                            {
                                StringBuilder strings = Builder(sources[i]);
                                string oldValue = oldValues[o], newValue = newValues[n];
                                Result("replace string:" + i + ":" + o + ":" + n + ":" + start + ":" + count,
                                    strings, () => strings.Replace(oldValue, newValue, start, count));
                            }
                    }
            for (int i = 0; i < sources.Length; i++)
                for (int o = 0; o < oldValues.Length; o++)
                    for (int n = 0; n < newValues.Length; n++)
                    {
                        StringBuilder strings = Builder(sources[i]);
                        string oldValue = oldValues[o], newValue = newValues[n];
                        Result("replace all:" + i + ":" + o + ":" + n, strings,
                            () => strings.Replace(oldValue, newValue));
                    }
            _editEvaluation = "";
            try
            {
                Receiver(null).Replace(Value("O", null), Value("N", "X"), Number("S", -1, false), Number("C", -1, false));
            }
            catch (Exception ex) { Fault("replace null evaluation", ex); }
            Console.WriteLine("replace null evaluation=" + _editEvaluation);
            _editEvaluation = "";
            try
            {
                Receiver(null).Remove(Number("S", -1, false), Number("L", -1, true));
            }
            catch (Exception ex) { Fault("remove throwing evaluation", ex); }
            Console.WriteLine("remove throwing evaluation=" + _editEvaluation);
            ArgumentOutOfRangeException saved = null;
            try { new StringBuilder("abc").Remove(-3, -7); }
            catch (ArgumentOutOfRangeException ex) { saved = ex; }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Fault("edit fault after GC", saved);
            Console.WriteLine("StringBuilder edit faults end");
        }
    }
}
