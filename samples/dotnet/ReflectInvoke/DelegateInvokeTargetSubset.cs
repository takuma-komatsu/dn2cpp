using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DelegateInvokeTargetSubset;

// Converting a delegate to another delegate type, or binding a method group over its
// Invoke, compiles to `ldftn <source>::Invoke`: the new delegate's target is the source
// delegate, and invoking it runs the source's whole invocation list.
internal static class Program
{
    private struct Wide
    {
        public long A;
        public long B;
        public long C;
        public string Label;

        public Wide(long a, long b, long c, string label)
        {
            A = a;
            B = b;
            C = c;
            Label = label;
        }

        public override string ToString() => Label + ":" + (A + B + C);
    }

    private delegate void Exchange(ref int value, out string text);
    private delegate void ExchangeView(ref int value, out string text);
    private delegate T Echo<T>(T value);

    private static string log = "";

    private static void First() => log += "1";

    private static void Second() => log += "2";

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<int, string> Missing() => null;

    private static Func<T, T> Rewrap<T>(Func<T, T> source) => new Func<T, T>(source.Invoke);

    private static Converter<T, string> AsConverter<T>(Func<T, string> source) => new Converter<T, string>(source);

    internal static void Run()
    {
        Console.WriteLine("== delegate invoke targets ==");
        Func<int, string> name = i => "n" + i;
        var converter = new Converter<int, string>(name);
        Console.WriteLine("converter=" + string.Join(",", new List<int> { 3, 1, 2 }.ConvertAll(converter))
            + " target=" + ReferenceEquals(converter.Target, name) + " method=" + converter.Method.Name
            + "/" + converter.Method.DeclaringType.Name + " entries=" + converter.GetInvocationList().Length
            + " dynamic=" + converter.DynamicInvoke(9));
        Action first = First;
        Action again = new Action(first.Invoke);
        Action group = first.Invoke;
        log = "";
        again();
        group();
        Console.WriteLine("same type=" + log + " equal=" + again.Equals(group)
            + " target=" + ReferenceEquals(again.Target, first) + " self=" + again.Equals(first));
        Echo<string> shout = s => s + "!";
        Echo<string> echoAgain = new Echo<string>(shout.Invoke);
        Func<string, string> echoFunc = shout.Invoke;
        Echo<int> twice = x => x * 2;
        Func<int, int> twiceFunc = twice.Invoke;
        Console.WriteLine("generic=" + echoAgain("a") + "/" + echoFunc("b") + "/" + twiceFunc(21)
            + "/" + Rewrap<string>(s => s + "?")("c") + "/" + Rewrap<int>(x => x + 1)(41)
            + "/" + AsConverter<long>(v => "L" + v)(7));
        Action both = First;
        both += Second;
        Action wrapped = new Action(both.Invoke);
        log = "";
        wrapped();
        (wrapped + first)();
        Func<int> counts = () => 1;
        counts += () => 2;
        Func<int> lastCount = counts.Invoke;
        Console.WriteLine("multicast=" + log + " entries=" + wrapped.GetInvocationList().Length + "/"
            + both.GetInvocationList().Length + " last=" + lastCount());
        Exchange exchange = (ref int value, out string text) => { value += 10; text = "t" + value; };
        exchange += (ref int value, out string text) => { value *= 2; text = "u" + value; };
        var view = new ExchangeView(exchange.Invoke);
        int number = 1;
        view(ref number, out string written);
        Console.WriteLine("by-ref=" + number + "/" + written);
        Func<int, Wide> make = n => new Wide(n, n * 2, n * 3, "w");
        Func<int, Wide> makeAgain = make.Invoke;
        Func<(int, string)> pair = () => (5, "p");
        Func<(int, string)> pairAgain = new Func<(int, string)>(pair.Invoke);
        Console.WriteLine("struct=" + makeAgain(2) + "/" + pairAgain());
        Func<string> text = () => "covariant";
        Func<object> boxed = new Func<object>(text.Invoke);
        Action<object> sink = o => log = "sink:" + o;
        Action<string> narrowed = new Action<string>(sink.Invoke);
        narrowed("x");
        Console.WriteLine("variance=" + boxed() + "/" + log);
        Func<NumberFormatInfo, string> separator = f => f.NumberDecimalSeparator;
        var separatorConverter = new Converter<NumberFormatInfo, string>(separator);
        Func<CultureInfo, NumberFormatInfo> format = c => c.NumberFormat;
        Func<CultureInfo, NumberFormatInfo> formatAgain = format.Invoke;
        Console.WriteLine("headerless=" + separatorConverter(NumberFormatInfo.InvariantInfo) + "/"
            + formatAgain(CultureInfo.InvariantCulture).NumberGroupSeparator);
        try
        {
            var none = new Converter<int, string>(Missing());
            Console.WriteLine("null source=" + (none is null));
        }
        catch (Exception ex)
        {
            Console.WriteLine("null source=" + ex.GetType().Name);
        }
        Func<int, int> offsets = x => x + 100;
        offsets += x => x + 200;
        Func<int, int> loaded = LdftnLocalSubset.Program.InvokeVirtualLoad(offsets);
        Console.WriteLine("virtual load=" + loaded(1) + " target=" + ReferenceEquals(loaded.Target, offsets)
            + " method=" + loaded.Method.Name + " entries=" + loaded.GetInvocationList().Length);
        try
        {
            Console.WriteLine("virtual load null=" + (LdftnLocalSubset.Program.InvokeVirtualLoad(null) is null));
        }
        catch (Exception ex)
        {
            Console.WriteLine("virtual load null=" + ex.GetType().Name);
        }
        Console.WriteLine("delegate invoke targets end");
    }
}
