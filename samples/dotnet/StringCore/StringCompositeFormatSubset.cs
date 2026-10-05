using System;
using System.Globalization;

namespace StringCompositeFormatSubset;

internal static class Program
{
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

    private static string Visible(string text) => text.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");

    private static void Check(string label, Func<string> body)
    {
        _trace = "";
        _calls = 0;
        try
        {
            string result = body();
            Console.WriteLine(label + " result=[" + Visible(result) + "] trace=" + _trace + " calls=" + _calls);
        }
        catch (Exception ex)
        {
            string parameter = ex is ArgumentException argument ? " param=" + (argument.ParamName ?? "<null>") : "";
            Console.WriteLine(label + " fault=" + ex.GetType().Name + ":" + Visible(ex.Message)
                + parameter + " trace=" + _trace + " calls=" + _calls);
        }
    }

    private static void Grammar(string label, string format) =>
        Check(label, () => string.Format(format, new object[] { 7, 11, 13 }));

    private static string FormatSpan(string format, object[] values) =>
        string.Format(format, new ReadOnlySpan<object>(values));

    private static string FormatProviderSpan(string format, object[] values) =>
        string.Format(CultureInfo.InvariantCulture, format, new ReadOnlySpan<object>(values));

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

    private static IFormatProvider Provider()
    {
        _trace += "P";
        return CultureInfo.InvariantCulture;
    }

    private static object ThrowArgument()
    {
        _trace += "X";
        throw new InvalidOperationException("argument-fault");
    }

    internal static void Run()
    {
        Console.WriteLine("== String composite format diagnostics ==");
        Grammar("closing-end", "}");
        Grammar("closing-middle", "x}y");
        Grammar("closing-after-escape", "}}}");
        Grammar("opening-end", "{");
        Grammar("index-end", "{0");
        Grammar("index-missing", "{}");
        Grammar("index-arabic", "{\u0660}");
        Grammar("index-fullwidth", "{\uff10}");
        Grammar("index-minus", "{-1}");
        Grammar("index-plus", "{+0}");
        Grammar("index-space", "{ 0}");
        Grammar("index-tab", "{\t0}");
        Grammar("index-character", "{0x}");
        Grammar("index-separated", "{0 1}");
        Grammar("index-trailing-tab", "{0\t}");
        Grammar("index-trailing-newline", "{0\n}");
        Grammar("alignment-end", "{0,");
        Grammar("alignment-minus-end", "{0,-");
        Grammar("alignment-missing", "{0,}");
        Grammar("alignment-spaces", "{0, }");
        Grammar("alignment-minus-missing", "{0,-}");
        Grammar("alignment-minus-space", "{0,- 2}");
        Grammar("alignment-plus", "{0,+2}");
        Grammar("alignment-double-minus", "{0,--2}");
        Grammar("alignment-character", "{0,a}");
        Grammar("alignment-tab", "{0,\t2}");
        Grammar("alignment-trailing-tab", "{0,2\t}");
        Grammar("alignment-trailing-character", "{0,2 x}");
        Grammar("alignment-second-comma", "{0,2,3}");
        Grammar("spec-end", "{0:");
        Grammar("spec-unclosed", "{0:X");
        Grammar("spec-opening", "{0:X{Y}");
        Grammar("spec-escaped-opening", "{0:{{}");
        Grammar("utf16-closing", "\ud83d\ude00}x");
        Grammar("utf16-index", "\ud83d\ude00{\u0660}");
        Grammar("utf16-unclosed", "\ud83d\ude00{0");
        Grammar("index-seven-digits", "{1000000}");
        Grammar("index-extra-digit", "{10000000}");
        Grammar("index-limit-tail", "{1000000x}");
        Grammar("alignment-below-limit-tail", "{0,999999x}");
        Grammar("alignment-limit-tail", "{0,1000000x}");
        Grammar("alignment-extra-digit-tail", "{0,10000000x}");
        Grammar("range-valid-item", "{3}");
        Grammar("range-before-closing", "{3}}");
        Grammar("range-invalid-item", "{3x}");
        Grammar("range-invalid-alignment", "{3,}");
        Grammar("range-invalid-spec", "{3:X{Y}");

        Check("valid-one", () => string.Format("{0}", (object)7));
        Check("valid-null-spec", () => string.Format("[{0:F2}]", (object)null));
        Check("valid-two", () => string.Format("{0}-{1}", (object)7, 11));
        Check("valid-three", () => string.Format("{0}-{1}-{2}", (object)7, 11, 13));
        Grammar("valid-array", "{2}/{0}/{1}");
        Grammar("valid-leading-zero", "{0000000}");
        Grammar("valid-spaces", "[{0   ,   -4   }][{1 , 4 :X2}]");
        Grammar("valid-escapes", "{{{0}}} {{{{}}}} {1:}}}");
        Check("valid-provider-one", () => string.Format(CultureInfo.InvariantCulture, "{0:F2}", (object)1.25));
        Check("valid-provider-two", () => string.Format(CultureInfo.InvariantCulture, "{0}/{1}", (object)7, 11));
        Check("valid-provider-three", () => string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", (object)7, 11, 13));
        Check("valid-provider-array", () => string.Format(CultureInfo.InvariantCulture, "{0}/{1}", new object[] { 7, 11 }));
        Check("valid-span", () => FormatSpan("{0}/{1}/{2}/{3}", new object[] { 7, 11, 13, 17 }));
        Check("valid-provider-span", () => FormatProviderSpan("{0:F2}/{1}", new object[] { 1.25, 11 }));
        Check("invalid-one", () => string.Format("}", (object)7));
        Check("invalid-two", () => string.Format("{0,}", (object)7, 11));
        Check("invalid-three", () => string.Format("{0:X{Y}", (object)7, 11, 13));
        Check("invalid-provider-one", () => string.Format(CultureInfo.InvariantCulture, "}", (object)7));
        Check("invalid-provider-two", () => string.Format(CultureInfo.InvariantCulture, "{0,}", (object)7, 11));
        Check("invalid-provider-three", () => string.Format(CultureInfo.InvariantCulture, "{0:X{Y}", (object)7, 11, 13));
        Check("invalid-provider-array", () => string.Format(CultureInfo.InvariantCulture, "{0\t}", new object[] { 7 }));
        Check("invalid-span", () => FormatSpan("{0,-}", new object[] { 7 }));
        Check("invalid-provider-span", () => FormatProviderSpan("\ud83d\ude00}x", new object[] { 7 }));

        Check("null-format-one", () => string.Format((string)null, (object)7));
        Check("null-format-two", () => string.Format((string)null, (object)7, 11));
        Check("null-format-three", () => string.Format((string)null, (object)7, 11, 13));
        Check("null-format-array", () => string.Format((string)null, new object[] { 7 }));
        Check("null-args", () => string.Format("text", (object[])null));
        Check("null-args-before-grammar", () => string.Format("}", (object[])null));
        Check("both-null", () => string.Format((string)null, (object[])null));
        Check("null-provider-format", () => string.Format(CultureInfo.InvariantCulture, (string)null, (object)7));
        Check("null-provider-args", () => string.Format(CultureInfo.InvariantCulture, "}", (object[])null));
        Check("both-provider-null", () => string.Format(CultureInfo.InvariantCulture, (string)null, (object[])null));
        Check("null-span-format", () => FormatSpan(null, new object[] { 7 }));
        Check("null-provider-span-format", () => FormatProviderSpan(null, new object[] { 7 }));
        Check("null-array-span", () => FormatSpan("literal", null));
        Check("empty-span-range", () => FormatSpan("{0}", Array.Empty<object>()));

        Check("value-repeated", () => string.Format("{0}/{0}", new Value(1)));
        Check("value-reordered", () => string.Format("{1}/{0}", new Value(1), new Value(2)));
        Check("grammar-before-value", () => string.Format("}x{0}", new Value(1)));
        Check("grammar-after-value", () => string.Format("{0}{", new Value(1)));
        Check("range-after-value", () => string.Format("{0}/{2}", new Value(1)));
        Check("value-throw-before-grammar", () => string.Format("{0}{", new Value(1, true)));
        Check("value-throw-second", () => string.Format("{0}/{1}", new Value(1), new Value(2, true)));
        Check("evaluation-order", () => string.Format(FormatValue("{2}/{0}/{1}"),
            Argument(1, new Value(1)), Argument(2, new Value(2)), Argument(3, new Value(3))));
        Check("evaluation-before-grammar", () => string.Format(FormatValue("}x"),
            Argument(1, new Value(1)), Argument(2, new Value(2)), Argument(3, new Value(3))));
        Check("argument-throw-before-grammar", () => string.Format(FormatValue("}x"),
            Argument(1, new Value(1)), ThrowArgument(), Argument(3, new Value(3))));
        Check("argument-throw-before-null", () => string.Format(FormatValue(null),
            Argument(1, new Value(1)), ThrowArgument()));
        Check("array-evaluation", () => string.Format(FormatValue("{0}"), Arguments(new object[] { new Value(1) })));
        Check("provider-evaluation", () => string.Format(Provider(), FormatValue("{0}"), Argument(1, new Value(1))));
        Console.WriteLine("String composite format diagnostics end");
    }
}
