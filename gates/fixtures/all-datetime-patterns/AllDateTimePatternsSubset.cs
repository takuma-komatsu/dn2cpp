using System;
using System.Globalization;
using System.Reflection;

namespace AllDateTimePatternsSubset;

internal static class Program
{
    private delegate string[] Query(DateTimeFormatInfo receiver, char format);
    private static string[] Direct(DateTimeFormatInfo receiver, char format) => receiver.GetAllDateTimePatterns(format);
    private static string[] Virtual(DateTimeFormatInfo receiver, char format) => receiver.GetAllDateTimePatterns(format);
    private static string[] Pointer(DateTimeFormatInfo receiver, char format) => receiver.GetAllDateTimePatterns(format);
    private static Func<char, string[]> Address(DateTimeFormatInfo receiver) => receiver.GetAllDateTimePatterns;
    private static string[] DelegateRoute(DateTimeFormatInfo receiver, char format) => Address(receiver)(format);
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod("GetAllDateTimePatterns", new[] { typeof(char) })!;
    private static string[] Reflect(DateTimeFormatInfo receiver, char format) => (string[])Metadata().Invoke(receiver, new object[] { format })!;
    private static string[] CastArray(object value) => (string[])value;
    private static int trace;

    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    private static void Check(string name, bool condition)
    {
        Console.WriteLine("all-datetime-patterns-" + name + ":" + condition);
        if (!condition) throw new InvalidOperationException(name);
    }

    private static string[] Expected(char format) => format switch
    {
            'd' => new[] { "MM/dd/yyyy", "yyyy-MM-dd" },
            'D' => new[] { "dddd, dd MMMM yyyy" },
            'f' => new[] { "dddd, dd MMMM yyyy HH:mm", "dddd, dd MMMM yyyy hh:mm tt", "dddd, dd MMMM yyyy H:mm", "dddd, dd MMMM yyyy h:mm tt" },
            'F' => new[] { "dddd, dd MMMM yyyy HH:mm:ss" },
            'g' => new[] { "MM/dd/yyyy HH:mm", "MM/dd/yyyy hh:mm tt", "MM/dd/yyyy H:mm", "MM/dd/yyyy h:mm tt", "yyyy-MM-dd HH:mm", "yyyy-MM-dd hh:mm tt", "yyyy-MM-dd H:mm", "yyyy-MM-dd h:mm tt" },
            'G' => new[] { "MM/dd/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss" },
            'm' => new[] { "MMMM dd" },
            'M' => new[] { "MMMM dd" },
            'o' => new[] { "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK" },
            'O' => new[] { "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK" },
            'r' => new[] { "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" },
            'R' => new[] { "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" },
            's' => new[] { "yyyy'-'MM'-'dd'T'HH':'mm':'ss" },
            't' => new[] { "HH:mm", "hh:mm tt", "H:mm", "h:mm tt" },
            'T' => new[] { "HH:mm:ss" },
            'u' => new[] { "yyyy'-'MM'-'dd HH':'mm':'ss'Z'" },
            'U' => new[] { "dddd, dd MMMM yyyy HH:mm:ss" },
            'y' => new[] { "yyyy MMMM" },
            'Y' => new[] { "yyyy MMMM" },
        _ => throw new InvalidOperationException("unexpected test format")
    };

    private static bool Same(string[] left, string[] right)
    {
        if (left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++)
            if (left[i] != right[i]) return false;
        return true;
    }

    private static void Valid(Query query, string route)
    {
        foreach (char format in "dDfFgGmMoOrRstTuUyY")
        {
            var info = DateTimeFormatInfo.InvariantInfo;
            var expected = Expected(format);
            var first = query(info, format);
            var second = query(info, format);
            Check(route + "-ordered-" + format, Same(first, expected));
            Check(route + "-array-" + format, first.GetType() == typeof(string[])
                && ReferenceEquals(CastArray(first), first) && !ReferenceEquals(first, second));
            first[0] = "mutated";
            var third = query(info, format);
            Check(route + "-isolated-" + format, Same(second, expected) && Same(third, expected));
            GC.Collect();
            Check(route + "-gc-" + format, Same(second, expected) && Same(third, expected));
        }
    }

    private static Exception Failure(Query query, DateTimeFormatInfo receiver, char format, bool reflection)
    {
        Exception observed = null!;
        try { _ = query(receiver, format); }
        catch (Exception error) { observed = error; }
        if (reflection)
        {
            Check("reflection-wrapped", observed is TargetInvocationException);
            observed = ((TargetInvocationException)observed).InnerException!;
        }
        return observed;
    }

    private static void Invalid(Query query, string route, bool reflection)
    {
        foreach (char format in new[] { 'x', 'a', 'z', '\0', '\uffff', '\ud800', '\udfff' })
        {
            var error = Failure(query, DateTimeFormatInfo.InvariantInfo, format, reflection);
            string message = "Format specifier '" + format + "' was invalid. (Parameter 'format')";
            Check(route + "-invalid-" + (int)format, error is ArgumentException argument
                && argument.ParamName == "format" && error.Message == message);
        }
    }

    private static void RawNull(Query query, string route)
    {
        foreach (char format in "oOrRsu")
            Check(route + "-null-constant-" + format, Same(query(null!, format), Expected(format)));
        foreach (char format in "dDfFgGmMtTUyY")
            Check(route + "-null-dependent-" + format, Failure(query, null!, format, false) is NullReferenceException);
        foreach (char format in new[] { 'x', '\0', '\uffff', '\ud800' })
        {
            var error = Failure(query, null!, format, false);
            Check(route + "-null-invalid-" + (int)format, error is ArgumentException argument
                && argument.ParamName == "format" && error.Message == "Format specifier '" + format + "' was invalid. (Parameter 'format')");
        }
    }

    private static DateTimeFormatInfo Receiver()
    {
        trace = trace * 10 + 1;
        return null!;
    }

    private static char Format(bool throws)
    {
        trace = trace * 10 + 2;
        if (throws) throw new FormatException("format producer");
        return 's';
    }

    private static string[] ProducerRoute(bool throws) => Direct(Receiver(), Format(throws));

    private static void Producers()
    {
        trace = 0;
        Check("producer-constant", Same(ProducerRoute(false), Expected('s')) && trace == 12);
        trace = 0;
        Exception observed = null!;
        try { _ = ProducerRoute(true); }
        catch (Exception error) { observed = error; }
        Check("producer-throws", trace == 12 && observed is FormatException && observed.Message == "format producer");
    }

    public static void RunDirectChecks()
    {
        PinCulture();
        Valid(Direct, "direct");
        Invalid(Direct, "direct", false);
        RawNull(Direct, "direct");
        Producers();
        Console.WriteLine("all-datetime-patterns-direct-end");
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        Valid(Reflect, "reflection");
        Invalid(Reflect, "reflection", true);
        foreach (char format in "dsx")
            Check("reflection-null-" + format, Failure(Reflect, null!, format, false) is TargetException);
        Check("reflection-abi", Metadata().ReturnType == typeof(string[])
            && Metadata().GetParameters().Length == 1 && Metadata().GetParameters()[0].ParameterType == typeof(char));
        Console.WriteLine("all-datetime-patterns-reflection-end");
    }

    public static void RunNoAllocChecks()
    {
        PinCulture();
        _ = Direct(DateTimeFormatInfo.InvariantInfo, 'd');
    }

    public static void RunMinimal()
    {
        PinCulture();
        var result = Direct(null!, 's');
        GC.Collect();
        Check("minimal", result.Length == 1 && result[0].Length == 29
            && result[0][0] == (char)121
            && result[0][1] == (char)121
            && result[0][2] == (char)121
            && result[0][3] == (char)121
            && result[0][4] == (char)39
            && result[0][5] == (char)45
            && result[0][6] == (char)39
            && result[0][7] == (char)77
            && result[0][8] == (char)77
            && result[0][9] == (char)39
            && result[0][10] == (char)45
            && result[0][11] == (char)39
            && result[0][12] == (char)100
            && result[0][13] == (char)100
            && result[0][14] == (char)39
            && result[0][15] == (char)84
            && result[0][16] == (char)39
            && result[0][17] == (char)72
            && result[0][18] == (char)72
            && result[0][19] == (char)39
            && result[0][20] == (char)58
            && result[0][21] == (char)39
            && result[0][22] == (char)109
            && result[0][23] == (char)109
            && result[0][24] == (char)39
            && result[0][25] == (char)58
            && result[0][26] == (char)39
            && result[0][27] == (char)115
            && result[0][28] == (char)115);
    }

    public static void Run()
    {
        PinCulture();
        Console.WriteLine("== Invariant all DateTime patterns ==");
        RunDirectChecks();
        Valid(Virtual, "callvirt");
        Invalid(Virtual, "callvirt", false);
        foreach (char format in "dsx")
            Check("callvirt-null-" + format, Failure(Virtual, null!, format, false) is NullReferenceException);
        Valid(Pointer, "calli");
        Invalid(Pointer, "calli", false);
        RawNull(Pointer, "calli");
        Valid(DelegateRoute, "delegate");
        Invalid(DelegateRoute, "delegate", false);
        Check("delegate-abi", Address(DateTimeFormatInfo.InvariantInfo).Method.ReturnType == typeof(string[]));
        RunReflectionChecks();
        Console.WriteLine("Invariant all DateTime patterns end");
    }

    // Public API oracle; mutable data is an explicit counterexample, not native support.
    public static void RunPublicOracle()
    {
        PinCulture();
        Valid(Virtual, "public");
        Invalid(Virtual, "public", false);
        var mutable = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        mutable.ShortDatePattern = "yyyy*MM*dd";
        Check("public-mutable-date", Same(mutable.GetAllDateTimePatterns('d'), new[] { "yyyy*MM*dd", "MM/dd/yyyy", "yyyy-MM-dd" }));
        mutable.SetAllDateTimePatterns(new[] { "HH~mm", "HH_mm" }, 't');
        Check("public-mutable-product", Same(mutable.GetAllDateTimePatterns('g'), new[]
        {
            "yyyy*MM*dd HH~mm", "yyyy*MM*dd HH_mm", "MM/dd/yyyy HH~mm", "MM/dd/yyyy HH_mm", "yyyy-MM-dd HH~mm", "yyyy-MM-dd HH_mm"
        }));
        Console.WriteLine("All DateTime patterns public oracle end");
    }
}
