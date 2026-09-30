using System;
using System.Text;

namespace StringBuilderRangeSubset;

internal static class Program
{
    private static string _evaluation;
    private static int _formatted;

    private sealed class FormattedValue
    {
        private readonly bool _throws;

        internal FormattedValue(bool throws)
        {
            _throws = throws;
        }

        public override string ToString()
        {
            _formatted++;
            if (_throws)
                throw new InvalidOperationException("formatter");
            return null;
        }
    }

    private static char EvaluateChar()
    {
        _evaluation += "C";
        return 'x';
    }

    private static int EvaluateCount()
    {
        _evaluation += "N";
        return -1;
    }

    private static string EvaluateString()
    {
        _evaluation += "S";
        return null;
    }

    private static int EvaluateIndex()
    {
        _evaluation += "I";
        return -1;
    }

    private static void Probe(string label, Action action, bool message = true)
    {
        try
        {
            action();
            Console.WriteLine(label + ": ok");
        }
        catch (Exception exception)
        {
            Console.WriteLine(label + ": " + exception.GetType().Name);
            if (message)
                Console.WriteLine("message: " + exception.Message.Replace(Environment.NewLine, "|"));
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== StringBuilder ranges ==");
        var builder = new StringBuilder("ab");
        StringBuilder missing = null;

        Probe("repeat negative", () => builder.Append('x', -1));
        Probe("repeat minimum", () => builder.Append('x', int.MinValue));
        Probe("repeat evaluated", () => builder.Append(EvaluateChar(), EvaluateCount()));
        Console.WriteLine("repeat evaluation: " + _evaluation);
        _evaluation = "";
        Probe("repeat null receiver", () => missing.Append(EvaluateChar(), EvaluateCount()), false);
        Console.WriteLine("repeat null evaluation: " + _evaluation);
        Probe("repeat zero", () => builder.Append('x', 0));
        Probe("repeat positive", () => builder.Append('x', 2));
        Console.WriteLine("repeat content: " + builder);

        Probe("window null zero", () => builder.Append((string)null, 0, 0));
        Probe("window null start", () => builder.Append((string)null, 1, 0));
        Probe("window null count", () => builder.Append((string)null, 0, 1));
        Probe("window null negative start", () => builder.Append((string)null, -1, -2));
        Probe("window null negative count", () => builder.Append((string)null, 0, -1));
        Probe("window negative start", () => builder.Append("ab", int.MinValue, -1));
        Probe("window negative count", () => builder.Append("ab", 0, int.MinValue));
        Probe("window zero beyond end", () => builder.Append("ab", int.MaxValue, 0));
        Probe("window empty zero beyond end", () => builder.Append("", 1, 0));
        Probe("window beyond end", () => builder.Append("ab", 1, 2));
        Probe("window maximum count", () => builder.Append("ab", 0, int.MaxValue));
        Probe("window maximum start", () => builder.Append("ab", int.MaxValue, 1));
        _evaluation = "";
        Probe("window evaluated", () => builder.Append(EvaluateString(), EvaluateIndex(), EvaluateCount()));
        Console.WriteLine("window evaluation: " + _evaluation);
        _evaluation = "";
        Probe("window null receiver", () => missing.Append(EvaluateString(), EvaluateIndex(), EvaluateCount()), false);
        Console.WriteLine("window null evaluation: " + _evaluation);
        Console.WriteLine("failed window content: " + builder);
        Probe("window whole", () => builder.Append("cd", 0, 2));
        Probe("window suffix", () => builder.Append("!e", 1, 1));
        Probe("window self string", () => builder.Append(builder.ToString(), 2, 2));
        Console.WriteLine("window content: " + builder);

        Console.WriteLine("insert null identity: " + ReferenceEquals(builder, builder.Insert(-1, (object)null)));
        Probe("insert null maximum index", () => builder.Insert(int.MaxValue, (object)null));
        _evaluation = "";
        Probe("insert null evaluated index", () => builder.Insert(EvaluateIndex(), (object)null));
        Console.WriteLine("insert evaluation: " + _evaluation);
        Probe("insert null receiver", () => missing.Insert(-1, (object)null), false);
        Probe("insert string null negative", () => builder.Insert(-1, (string)null), false);
        Probe("insert chars null negative", () => builder.Insert(-1, (char[])null), false);
        Probe("insert formatted null negative", () => builder.Insert(-1, new FormattedValue(false)), false);
        Probe("insert throwing formatter", () => builder.Insert(-1, new FormattedValue(true)), false);
        Probe("insert null receiver formatter", () => missing.Insert(-1, new FormattedValue(true)), false);
        Console.WriteLine("insert formatters: " + _formatted);
        Console.WriteLine("final content: " + builder);
        Console.WriteLine("recovery identity: " + ReferenceEquals(builder, builder.Append('!')));
        Console.WriteLine("recovery content: " + builder);
    }
}
