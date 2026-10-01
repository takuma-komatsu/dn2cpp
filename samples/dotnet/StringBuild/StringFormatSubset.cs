#nullable disable
using System;

namespace StringFormatSubset
{
    // string.Format composite formatting: holes are {index[,alignment][:spec]},
    // {{ }} are literal braces. Covers the 1-3 object overloads and object[].
    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine(string.Format("{0} and {1}", 1, 2));   // 1 and 2
            Console.WriteLine(string.Format("{0:F2}", 3.14159));     // 3.14
            Console.WriteLine(string.Format("{0,5}", 42));           // 42
            Console.WriteLine(string.Format("{0,-5}|", 42));         // 42 |
            Console.WriteLine(string.Format("{0:X}", 255));          // FF
            Console.WriteLine(string.Format("{0:D3}", 7));           // 007
            Console.WriteLine(string.Format("{0,8:F2}", 3.5));       // 3.50
            Console.WriteLine(string.Format("{{literal}} {0}", "x")); // {literal} x
            Console.WriteLine(string.Format("{0} {1} {2}", "a", "b", "c")); // a b c
            Console.WriteLine(string.Format("{0}={1}", "n", 10L));   // n=10
            // Explicit object[] overload.
            Console.WriteLine(string.Format("{0}-{1}-{2}-{3}", new object[] { 1, 2, 3, 4 })); // 1-2-3-4
        }

        private static string FormatUnits(string value)
        {
            if (value is null)
                return "null";
            string result = "";
            foreach (char ch in value)
                result += ((int)ch).ToString("X4") + " ";
            return result;
        }

        private static void FormatFault(string label, Exception ex)
        {
            Console.WriteLine(label + " type=" + ex.GetType().Name);
            Console.WriteLine(label + " param=" + (ex is ArgumentException arg ? arg.ParamName : null));
            Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
            object actual = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
            Console.WriteLine(label + " actual=" + (actual is null ? "null" : actual.GetType().Name + ":" + actual));
        }

        private static void FormatObserve(string label, Action action)
        {
            try { action(); Console.WriteLine(label + " success"); }
            catch (Exception ex) { FormatFault(label, ex); }
        }

        private static void FormatDump(string value) { Console.WriteLine("value=" + FormatUnits(value)); }

        private static void FormatDump(string[] values)
        {
            Console.WriteLine("array length=" + values.Length);
            foreach (string value in values)
                FormatDump(value);
        }

        internal static void RunFaults()
        {
            Console.WriteLine("== String format faults ==");
            string[] formats = { null, "", "{0}", "{0} {1}", "{9}" };
            object[][] args = { null, Array.Empty<object>(), new object[] { null }, new object[] { 1, null } };
            for (int f = 0; f < formats.Length; f++)
                for (int a = 0; a < args.Length; a++)
                {
                    FormatObserve("format:" + f + ":" + a, () => FormatDump(string.Format(formats[f], args[a])));
                    FormatObserve("format provider:" + f + ":" + a, () => FormatDump(string.Format(System.Globalization.CultureInfo.InvariantCulture, formats[f], args[a])));
                }
            Exception[] saved = new Exception[2];
            try { string.Format((string)null, (object[])null); } catch (Exception ex) { saved[0] = ex; }
            try { string.Format("{9}", new object[] { 1 }); } catch (Exception ex) { saved[1] = ex; }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int i = 0; i < saved.Length; i++)
                FormatFault("format fault after GC:" + i, saved[i]);
            Console.WriteLine("String format faults end");
        }
    }
}
