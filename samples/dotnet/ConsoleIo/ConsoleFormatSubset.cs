#nullable disable
using System;

namespace ConsoleFormatSubset
{
    // Console.Write/WriteLine composite-format overloads reuse the
    // string.Format runtime helpers (same {index[,align][:spec]} grammar), then
    // write the composed string. 1-3 explicit object args + explicit object[];
    // the params ReadOnlySpan<object> path (4+ loose args) is future work.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("{0} and {1}", 1, 2);          // 1 and 2
            Console.WriteLine("{0:F2}", 3.14159);            // 3.14
            Console.Write("{0,5}", 42);
            Console.WriteLine();                              // 42
            Console.WriteLine("{0}-{1}-{2}", "a", "b", "c"); // a-b-c
            Console.WriteLine("{0:X}", 255);                 // FF
            Console.Write("[{0}]", "w");
            Console.WriteLine();                              // [w]
            Console.WriteLine("{0,-4}|{1,4}", "x", "y");     // x | y
            Console.WriteLine("{0}-{1}-{2}-{3}", new object[] { 1, 2, 3, 4 }); // 1-2-3-4
        }

        private static string _trace = "";
        private static int _calls;

        private sealed class Value
        {
            private readonly int _id;
            private readonly bool _throws;

            internal Value(int id, bool throws = false)
            {
                _id = id;
                _throws = throws;
            }

            public override string ToString()
            {
                _trace += "T" + _id;
                _calls++;
                if (_throws)
                    throw new InvalidOperationException("value-fault-" + _id);
                return "value" + _id;
            }
        }

        private static void Check(string label, Action body)
        {
            _trace = "";
            _calls = 0;
            Console.WriteLine(label + " begin");
            string outcome = "success";
            try
            {
                body();
            }
            catch (Exception ex)
            {
                string parameter = ex is ArgumentException argument ? " param=" + (argument.ParamName ?? "<null>") : "";
                outcome = "fault=" + ex.GetType().Name + ":" + ex.Message.Replace("\r", "\\r").Replace("\n", "\\n") + parameter;
            }
            Console.WriteLine();
            Console.WriteLine(label + " " + outcome + " trace=" + _trace + " calls=" + _calls);
        }

        private static string FormatValue(string format)
        {
            _trace += "F";
            return format;
        }

        private static object Argument(int id, object value)
        {
            _trace += id;
            return value;
        }

        private static object[] Arguments(object[] values)
        {
            _trace += "A";
            return values;
        }

        private static object ThrowArgument()
        {
            _trace += "X";
            throw new InvalidOperationException("argument-fault");
        }

        internal static void RunDiagnostics()
        {
            Console.WriteLine("== Console composite format diagnostics ==");
            Check("write-valid-one", () => Console.Write("[{0}]", (object)7));
            Check("write-valid-two", () => Console.Write("[{0}/{1}]", (object)7, 11));
            Check("write-valid-three", () => Console.Write("[{0}/{1}/{2}]", (object)7, 11, 13));
            Check("write-valid-array", () => Console.Write("[{0}/{1}/{2}/{3}]", new object[] { 7, 11, 13, 17 }));
            Check("line-valid-one", () => Console.WriteLine("[{0}]", (object)7));
            Check("line-valid-two", () => Console.WriteLine("[{0}/{1}]", (object)7, 11));
            Check("line-valid-three", () => Console.WriteLine("[{0}/{1}/{2}]", (object)7, 11, 13));
            Check("line-valid-array", () => Console.WriteLine("[{0}/{1}/{2}/{3}]", new object[] { 7, 11, 13, 17 }));
            Check("write-valid-spaces", () => Console.Write("[{0   ,   -4   }][{1 , 4 :X2}]", (object)7, 11));
            Check("line-valid-escapes", () => Console.WriteLine("{{{0}}} {{{{}}}} {1:}}}", (object)7, 11));
            Check("write-invalid-one", () => Console.Write("}x", (object)7));
            Check("write-invalid-two", () => Console.Write("{0,}", (object)7, 11));
            Check("write-invalid-three", () => Console.Write("{0:X{Y}", (object)7, 11, 13));
            Check("write-invalid-array", () => Console.Write("\ud83d\ude00{\u0660}", new object[] { 7 }));
            Check("line-invalid-one", () => Console.WriteLine("}", (object)7));
            Check("line-invalid-two", () => Console.WriteLine("{0,-}", (object)7, 11));
            Check("line-invalid-three", () => Console.WriteLine("{0\t}", (object)7, 11, 13));
            Check("line-invalid-array", () => Console.WriteLine("\ud83d\ude00{0", new object[] { 7 }));
            Check("write-range", () => Console.Write("{1}", (object)7));
            Check("line-range-invalid-item", () => Console.WriteLine("{1,}", (object)7));
            Check("write-null-format-one", () => Console.Write((string)null, (object)7));
            Check("write-null-format-two", () => Console.Write((string)null, (object)7, 11));
            Check("write-null-format-three", () => Console.Write((string)null, (object)7, 11, 13));
            Check("write-null-format-array", () => Console.Write((string)null, new object[] { 7 }));
            Check("line-null-format-one", () => Console.WriteLine((string)null, (object)7));
            Check("line-null-format-two", () => Console.WriteLine((string)null, (object)7, 11));
            Check("line-null-format-three", () => Console.WriteLine((string)null, (object)7, 11, 13));
            Check("line-null-format-array", () => Console.WriteLine((string)null, new object[] { 7 }));
            Check("write-null-array", () => Console.Write("[{0}|{1}]", (object[])null));
            Check("line-null-array", () => Console.WriteLine("[{0}|{1}]", (object[])null));
            Check("write-null-array-range", () => Console.Write("{2}", (object[])null));
            Check("line-null-array-range", () => Console.WriteLine("{2}", (object[])null));
            Check("write-null-array-grammar", () => Console.Write("}x", (object[])null));
            Check("line-null-array-grammar", () => Console.WriteLine("{0,}", (object[])null));
            Check("write-both-null", () => Console.Write((string)null, (object[])null));
            Check("line-both-null", () => Console.WriteLine((string)null, (object[])null));
            Check("write-null-value", () => Console.Write("prefix{0}tail", (object)null));
            Check("line-null-value", () => Console.WriteLine("prefix{0}tail", (object)null));
            Check("write-null-value-spec", () => Console.Write("[{0:F2}]", (object)null));
            Check("line-null-array-spec", () => Console.WriteLine("[{0:X}|{1:F2}]", (object[])null));

            Check("write-value-repeated", () => Console.Write("{0}/{0}", new Value(1)));
            Check("line-value-reordered", () => Console.WriteLine("{1}/{0}", new Value(1), new Value(2)));
            Check("write-grammar-before-value", () => Console.Write("}x{0}", new Value(1)));
            Check("line-grammar-after-value", () => Console.WriteLine("{0}{", new Value(1)));
            Check("write-value-throw-before-grammar", () => Console.Write("{0}{", new Value(1, true)));
            Check("line-value-throw-second", () => Console.WriteLine("{0}/{1}", new Value(1), new Value(2, true)));
            Check("write-evaluation", () => Console.Write(FormatValue("{2}/{0}/{1}"),
                Argument(1, new Value(1)), Argument(2, new Value(2)), Argument(3, new Value(3))));
            Check("line-evaluation-before-grammar", () => Console.WriteLine(FormatValue("}x"),
                Argument(1, new Value(1)), Argument(2, new Value(2)), Argument(3, new Value(3))));
            Check("write-argument-throw-before-grammar", () => Console.Write(FormatValue("}x"),
                Argument(1, new Value(1)), ThrowArgument(), Argument(3, new Value(3))));
            Check("line-argument-throw-before-null", () => Console.WriteLine(FormatValue(null),
                Argument(1, new Value(1)), ThrowArgument()));
            Check("write-array-evaluation", () => Console.Write(FormatValue("{0}"), Arguments(new object[] { new Value(1) })));
            Console.WriteLine("Console composite format diagnostics end");
        }
    }
}
