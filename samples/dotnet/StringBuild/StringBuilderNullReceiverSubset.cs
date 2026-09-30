using System;
using System.Globalization;
using System.Text;

namespace StringBuilderNullReceiverSubset;

internal static class Program
{
    private static string _order = "";
    private static int _formatted;

    internal static void Run()
    {
        Console.WriteLine("== StringBuilder null receivers ==");
        StringBuilder missing = null;
        string noString = null;
        var value = new FormattedValue();

        Probe("append string", () => missing.Append("x"));
        Probe("append null string", () => missing.Append(noString));
        Probe("append object", () => missing.Append((object)value));
        Probe("append null object", () => missing.Append((object)null));
        Probe("append character", () => missing.Append('x'));
        Probe("append int", () => missing.Append(7));
        Probe("append uint", () => missing.Append(uint.MaxValue));
        Probe("append long", () => missing.Append(7L));
        Probe("append ulong", () => missing.Append(7UL));
        Probe("append float", () => missing.Append(1.5f));
        Probe("append double", () => missing.Append(1.5));
        Probe("append decimal", () => missing.Append(1.5m));
        Probe("append bool", () => missing.Append(true));
        Probe("append repeat zero", () => missing.Append('x', 0));
        Probe("append repeat negative", () => missing.Append('x', -1));
        Probe("append null chars", () => missing.Append((char[])null));
        Probe("append chars range", () => missing.Append((char[])null, -1, 1));
        Probe("append span", () => missing.Append("x".AsSpan()));
        Probe("append empty span", () => missing.Append(ReadOnlySpan<char>.Empty));
        Probe("append string range", () => missing.Append("x", 5, 1));
        Probe("append null string range", () => missing.Append(noString, 0, 0));
        Probe("append builder", () => missing.Append(new StringBuilder("x")));
        Probe("append null builder", () => missing.Append((StringBuilder)null));
        Probe("append builder range", () => missing.Append((StringBuilder)null, -1, 1));
        Probe("append format array", () => missing.AppendFormat((string)null, (object[])null));
        Probe("append format object", () => missing.AppendFormat("{", (object)value));
        Probe("append format provider", () => missing.AppendFormat(CultureInfo.InvariantCulture, (string)null, (object)value));
        Probe("append format span", () => missing.AppendFormat("{", new ReadOnlySpan<object>(new object[] { value })));
        Probe("append format provider span", () => missing.AppendFormat(CultureInfo.InvariantCulture, "{", new ReadOnlySpan<object>(new object[] { value })));
        Probe("append line", () => missing.AppendLine());
        Probe("append line null", () => missing.AppendLine(noString));
        Probe("to string", () => missing.ToString());
        Probe("to string range", () => missing.ToString(-1, 1));
        Probe("copy to", () => missing.CopyTo(0, new char[1], 0, 0));
        Probe("copy to null", () => missing.CopyTo(-1, null, -1, -1));
        Probe("chunks", () => missing.GetChunks());
        Probe("length", () => { int length = missing.Length; });
        Probe("capacity", () => { int capacity = missing.Capacity; });
        Probe("clear", () => missing.Clear());
        Probe("insert chars", () => missing.Insert(-1, (char[])null));
        Probe("insert chars range", () => missing.Insert(-1, (char[])null, -1, 1));
        Probe("insert string count", () => missing.Insert(-1, noString, -1));
        Probe("insert string", () => missing.Insert(-1, "x"));
        Probe("insert object", () => missing.Insert(-1, (object)value));
        Probe("insert null object", () => missing.Insert(-1, (object)null));
        Probe("remove", () => missing.Remove(-1, -1));
        Probe("replace strings", () => missing.Replace((string)null, "x"));
        Probe("replace strings range", () => missing.Replace((string)null, "x", -1, -1));
        Probe("replace characters", () => missing.Replace('x', 'y'));
        Probe("replace characters range", () => missing.Replace('x', 'y', -1, -1));
        Probe("set length", () => missing.Length = -1);
        Probe("get character", () => { char character = missing[-1]; });
        Probe("set character", () => missing[-1] = 'x');
        Probe("ensure capacity", () => missing.EnsureCapacity(-1));

        Probe("interpolated literal", () => missing.Append($"a{EvaluateInt()}b"));
        Console.WriteLine("literal argument evaluation: [" + _order + "]");
        Probe("interpolated int", () => missing.Append($"{EvaluateInt()}"));
        Probe("interpolated null string", () => missing.Append($"{noString}"));
        Probe("interpolated aligned string", () => missing.Append($"{noString,3}"));
        Probe("interpolated aligned int", () => missing.Append($"{7,3}"));
        Probe("interpolated provider", () => missing.Append(CultureInfo.InvariantCulture, $"{7}"));
        Probe("interpolated line", () => missing.AppendLine($"{7}"));
        Console.WriteLine("formatted values: " + _formatted);

        Probe("interpolated custom value", () => missing.Append($"{value}"));
        Probe("interpolated throwing value", () => missing.Append($"{new FormattedValue(true)}"));
        Probe("interpolated aligned value", () => missing.Append($"{value,3}"));
        Console.WriteLine("interpolated formatters: " + _formatted);
        Probe("interpolated invalid int format", () => missing.Append($"{7:Q}"));
        Probe("interpolated zero alignment", () => missing.Append($"{7,0:Q}"));
        Probe("interpolated span formatter", () => missing.Append($"{new SpanValue()}"));
        object boxedSpan = new SpanValue();
        Probe("interpolated boxed span formatter", () => missing.Append($"{boxedSpan}"));
        object boxedInt = 7;
        Probe("interpolated boxed int", () => missing.Append($"{boxedInt}"));
        Probe("interpolated enum", () => missing.Append($"{DayOfWeek.Sunday:Q}"));
        Probe("interpolated value type", () => missing.Append($"{new StructValue()}"));
        Console.WriteLine("struct formatters: " + _formatted);

        ProbeNullHole<object>("generic object null", null);
        ProbeNullHole<string>("generic string null", null);
        ProbeNullHole<FormattedValue>("generic reference null", null);
        ProbeNullHole<int?>("generic nullable null", null);
        ProbeAlignedNullHole<object>("generic object zero alignment", null, 0);
        ProbeAlignedNullHole<object>("generic object positive alignment", null, 3);
        ProbeAlignedNullHole<string>("generic string negative alignment", null, -3);
        ProbeAlignedNullHole<int?>("generic nullable zero alignment", null, 0);
        ProbeAlignedNullHole<int?>("generic nullable positive alignment", null, 3);

        _order = "";
        Probe("evaluated arguments", () => EvaluateReceiver().Append(EvaluateString(), EvaluateIndex(), EvaluateCount()));
        Console.WriteLine("argument evaluation: " + _order);
        _order = "";
        Probe("throwing argument", () => EvaluateReceiver().Append(ThrowArgument()));
        Console.WriteLine("throwing argument evaluation: " + _order);
        Console.WriteLine("recovery: " + new StringBuilder("ok").Append('!'));
    }

    private static void Probe(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine(label + ": returned");
        }
        catch (Exception exception)
        {
            Console.WriteLine(label + ": " + exception.GetType().Name);
        }
    }

    private static void ProbeNullHole<T>(string label, T value)
    {
        _order = "";
        Probe(label, () => AppendHole(value));
        Console.WriteLine(label + " next evaluation: " + _order);
    }

    private static void AppendHole<T>(T value)
    {
        StringBuilder missing = null;
        missing.Append($"{value}{EvaluateInt()}");
    }

    private static void ProbeAlignedNullHole<T>(string label, T value, int alignment)
    {
        _order = "";
        Probe(label, () => AppendAlignedHole(value, alignment));
        Console.WriteLine(label + " next evaluation: [" + _order + "]");
    }

    private static void AppendAlignedHole<T>(T value, int alignment)
    {
        StringBuilder missing = null;
        var handler = new StringBuilder.AppendInterpolatedStringHandler(0, 2, missing);
        handler.AppendFormatted(value, alignment);
        handler.AppendFormatted(EvaluateInt());
        missing.Append(ref handler);
    }

    private static StringBuilder EvaluateReceiver()
    {
        _order += "R";
        return null;
    }

    private static string EvaluateString()
    {
        _order += "S";
        return "x";
    }

    private static int EvaluateIndex()
    {
        _order += "I";
        return -1;
    }

    private static int EvaluateCount()
    {
        _order += "C";
        return 1;
    }

    private static int EvaluateInt()
    {
        _order += "H";
        return 7;
    }

    private static string ThrowArgument()
    {
        _order += "T";
        throw new InvalidOperationException();
    }

    private sealed class FormattedValue
    {
        private readonly bool _throws;

        internal FormattedValue(bool throws = false)
        {
            _throws = throws;
        }

        public override string ToString()
        {
            _formatted++;
            if (_throws)
                throw new InvalidOperationException();
            return "value";
        }
    }

    private sealed class SpanValue : ISpanFormattable
    {
        public override string ToString() => throw new InvalidOperationException();
        public string ToString(string format, IFormatProvider provider) => throw new InvalidOperationException();
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
        {
            charsWritten = 0;
            throw new InvalidOperationException();
        }
    }

    private struct StructValue
    {
        public override string ToString()
        {
            _formatted++;
            return "struct";
        }
    }
}
