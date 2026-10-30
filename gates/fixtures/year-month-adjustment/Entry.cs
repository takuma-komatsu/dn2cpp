using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace YearMonthAdjustmentSubset;

internal static class Program
{
    private delegate bool Query(DateTimeFormatInfo receiver, ref int year, ref int month, bool parsed);
    private delegate bool ClosedQuery(ref int year, ref int month, bool parsed);
    private static bool Direct(DateTimeFormatInfo receiver, ref int year, ref int month, bool parsed) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool Virtual(DateTimeFormatInfo receiver, ref int year, ref int month, bool parsed) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool Pointer(DateTimeFormatInfo receiver, ref int year, ref int month, bool parsed) => throw new InvalidOperationException("native fixture not rewritten");
    private static ClosedQuery Address(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool DelegateRoute(DateTimeFormatInfo receiver, ref int year, ref int month, bool parsed) => Address(receiver)(ref year, ref month, parsed);
    private static bool HasDirect(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool HasPointer(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod("YearMonthAdjustment", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static MethodInfo HasMetadata() => typeof(DateTimeFormatInfo).GetMethod("get_HasYearMonthAdjustment", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static int checks;
    private static int trace;
    private static int throwAt;
    private static bool nullReceiver;
    private static int producerYear;
    private static int producerMonth;

    private static void Check(string name, bool condition)
    {
        Console.WriteLine("year-month-adjustment-" + name + ":" + condition);
        if (!condition) throw new InvalidOperationException(name);
        checks++;
    }

    private static int Year(int index) => index switch { 0 => 0, 1 => -1, 2 => int.MinValue, 3 => int.MaxValue, _ => 2024 };
    private static int Month(int index) => index switch { 0 => 0, 1 => 13, 2 => int.MaxValue, 3 => int.MinValue, _ => -99 };

    private static void Cases(Query query, string route)
    {
        var receiver = DateTimeFormatInfo.InvariantInfo;
        foreach (bool parsed in new[] { false, true })
        {
            for (int index = 0; index < 5; index++)
            {
                int year = Year(index), month = Month(index);
                Check(route + "-values-" + index + "-" + parsed, query(receiver, ref year, ref month, parsed)
                    && year == Year(index) && month == Month(index));
                int shared = Year(index);
                Check(route + "-alias-" + index + "-" + parsed, query(receiver, ref shared, ref shared, parsed) && shared == Year(index));
            }
            int sentinel = int.MinValue;
            Check(route + "-null-year-" + parsed, query(receiver, ref Unsafe.NullRef<int>(), ref sentinel, parsed) && sentinel == int.MinValue);
            Check(route + "-null-month-" + parsed, query(receiver, ref sentinel, ref Unsafe.NullRef<int>(), parsed) && sentinel == int.MinValue);
            Check(route + "-null-both-" + parsed, query(receiver, ref Unsafe.NullRef<int>(), ref Unsafe.NullRef<int>(), parsed));
            if (route != "delegate")
            {
                bool observed = false;
                try { query(null!, ref sentinel, ref sentinel, parsed); }
                catch (NullReferenceException) { observed = true; }
                Check(route + "-null-receiver-" + parsed, observed && sentinel == int.MinValue);
            }
        }
    }

    private static void Step(int value)
    {
        trace = trace * 10 + value;
        if (throwAt == value) throw new FormatException("argument producer");
    }
    private static DateTimeFormatInfo Receiver() { Step(1); return nullReceiver ? null! : DateTimeFormatInfo.InvariantInfo; }
    private static ref int YearArgument() { Step(2); return ref producerYear; }
    private static ref int MonthArgument() { Step(3); return ref producerMonth; }
    private static bool ParsedArgument() { Step(4); return true; }
    private static bool ProducerRoute() => Direct(Receiver(), ref YearArgument(), ref MonthArgument(), ParsedArgument());

    private static void Producers()
    {
        for (int failure = -1; failure <= 4; failure++)
        {
            trace = 0; throwAt = failure; nullReceiver = failure != -1;
            producerYear = int.MaxValue; producerMonth = int.MinValue;
            bool observed = false;
            try { observed = ProducerRoute() && failure == -1; }
            catch (NullReferenceException) { observed = failure == 0; }
            catch (FormatException error) { observed = failure > 0 && error.Message == "argument producer"; }
            int expectedTrace = failure switch { 1 => 1, 2 => 12, 3 => 123, _ => 1234 };
            Check("producer-" + failure, observed && trace == expectedTrace && producerYear == int.MaxValue && producerMonth == int.MinValue);
        }
    }

    public static void RunReflectionChecks()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        foreach (bool parsed in new[] { false, true })
        {
            for (int index = 0; index < 5; index++)
            {
                object[] arguments = { Year(index), Month(index), parsed };
                object result = Metadata().Invoke(DateTimeFormatInfo.InvariantInfo, arguments)!;
                Check("reflection-values-" + index + "-" + parsed, result is bool && (bool)result
                    && (int)arguments[0] == Year(index) && (int)arguments[1] == Month(index) && (bool)arguments[2] == parsed);
            }
        }
        var parameters = Metadata().GetParameters();
        Check("reflection-abi", Metadata().ReturnType == typeof(bool) && parameters.Length == 3
            && parameters[0].ParameterType == typeof(int).MakeByRefType()
            && parameters[1].ParameterType == typeof(int).MakeByRefType() && parameters[2].ParameterType == typeof(bool));
        bool observed = false;
        try { Metadata().Invoke(null, new object[] { 0, 0, false }); }
        catch (TargetException) { observed = true; }
        Check("reflection-null-target", observed);
        Check("has-reflection", HasMetadata().ReturnType == typeof(bool) && HasMetadata().Invoke(DateTimeFormatInfo.InvariantInfo, null) is bool value && !value);
        observed = false;
        try { HasMetadata().Invoke(null, null); }
        catch (TargetException) { observed = true; }
        Check("has-reflection-null-target", observed);
        Console.WriteLine("Year month adjustment reflection end");
    }

    public static void RunMinimal()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Check("minimal", Direct(DateTimeFormatInfo.InvariantInfo, ref Unsafe.NullRef<int>(), ref Unsafe.NullRef<int>(), true));
    }

    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Cases(Direct, "direct");
        Cases(Virtual, "callvirt");
        Cases(Pointer, "calli");
        // A null closed-delegate receiver is an existing binding policy, not a raw call.
        Cases(DelegateRoute, "delegate");
        Check("delegate-abi", Address(DateTimeFormatInfo.InvariantInfo).Method.ReturnType == typeof(bool));
        Check("has-direct", !HasDirect(DateTimeFormatInfo.InvariantInfo));
        Check("has-calli", !HasPointer(DateTimeFormatInfo.InvariantInfo));
        bool observed = false;
        try { HasDirect(null!); } catch (NullReferenceException) { observed = true; }
        Check("has-direct-null", observed);
        observed = false;
        try { HasPointer(null!); } catch (NullReferenceException) { observed = true; }
        Check("has-calli-null", observed);
        Producers();
        RunReflectionChecks();
        Console.WriteLine("Year month adjustment end:" + checks);
    }

    private static void Main() => Run();
}
