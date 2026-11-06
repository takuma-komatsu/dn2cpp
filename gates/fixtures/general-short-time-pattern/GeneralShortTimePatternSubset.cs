using System;
using System.Globalization;
using System.Reflection;

#if OFFSET_PATTERN
namespace DateTimeOffsetPatternSubset;
#elif LONG_PATTERN
namespace GeneralLongTimePatternSubset;
#else
namespace GeneralShortTimePatternSubset;
#endif

internal static class Program
{
    private delegate string Query(DateTimeFormatInfo receiver);
    private delegate string ClosedQuery();
#if OFFSET_PATTERN
    private const string Prefix = "datetime-offset-pattern-";
    private const string Title = "Invariant datetime offset pattern";
    private const string Getter = "get_DateTimeOffsetPattern";
#elif LONG_PATTERN
    private const string Prefix = "general-long-time-pattern-";
    private const string Title = "Invariant general long-time pattern";
    private const string Getter = "get_GeneralLongTimePattern";
#else
    private const string Prefix = "general-short-time-pattern-";
    private const string Title = "Invariant general short-time pattern";
    private const string Getter = "get_GeneralShortTimePattern";
#endif
#if OFFSET_PATTERN
    private static string ExpectedPattern() => "MM/dd/yyyy HH:mm:ss zzz";
#elif LONG_PATTERN
    private static string ExpectedPattern() => "MM/dd/yyyy HH:mm:ss";
#else
    private static string ExpectedPattern() => "MM/dd/yyyy HH:mm";
#endif
    private static int trace;
    private static string Direct(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture required");
    private static string Pointer(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture required");
    private static ClosedQuery Address(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture required");
    private static string DelegateRoute(DateTimeFormatInfo receiver) => Address(receiver)();
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod(Getter, BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static string Reflect(DateTimeFormatInfo receiver) => (string)Metadata().Invoke(receiver, null)!;
    private static DateTimeFormatInfo Receiver() { trace++; return null!; }
    private static DateTimeFormatInfo ThrowReceiver() { trace++; throw new FormatException("receiver producer"); }

    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    private static void Check(string label, bool result)
    {
        Console.WriteLine(Prefix + label + ":" + result);
        if (!result) throw new InvalidOperationException(label);
    }

    private static void Validate(Query query, string route)
    {
        foreach (var info in new[] { DateTimeFormatInfo.InvariantInfo, (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone() })
        {
            string value = query(info);
            Check(route + "-content", value == ExpectedPattern());
            Check(route + "-actual-string", value.GetType() == typeof(string));
            // Native rooted identity only; not CLR per-instance cache/clone parity.
            Check(route + "-rooted-identity", ReferenceEquals(value, query(info)));
        }
    }

    private static void NullFirst(Query query, string route)
    {
        bool observed = false;
        try { _ = query(null!); }
        catch (NullReferenceException) { observed = true; }
        Check(route + "-null", observed);
    }

    public static void RunDirectChecks()
    {
        PinCulture();
        Validate(Direct, "direct");
        NullFirst(Direct, "direct");
        string kept = Direct(DateTimeFormatInfo.InvariantInfo);
        GC.Collect();
        Check("rooted-after-collect", kept == ExpectedPattern()
            && ReferenceEquals(kept, Direct(DateTimeFormatInfo.InvariantInfo)));
        trace = 0;
        bool observed = false;
        try { _ = Direct(Receiver()); }
        catch (NullReferenceException) { observed = true; }
        Check("receiver-once", observed && trace == 1);
        trace = 0;
        observed = false;
        try { _ = Direct(ThrowReceiver()); }
        catch (FormatException error) { observed = error.Message == "receiver producer"; }
        Check("receiver-throws", observed && trace == 1);
        Console.WriteLine(Prefix + "direct-end");
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        Validate(Reflect, "reflection");
        Check("reflection-return", Metadata().ReturnType.FullName == "System.String");
        Exception error = null!;
        try { _ = Reflect(null!); }
        catch (Exception observed) { error = observed; }
        Check("reflection-null", error is TargetException);
        Console.WriteLine(Prefix + "reflection-end");
    }

    public static void RunMinimal()
    {
        PinCulture();
        string value = Direct(DateTimeFormatInfo.InvariantInfo);
        // Character checks do not independently root the expected full literal.
#if OFFSET_PATTERN
        bool correct = value.Length == 23
#elif LONG_PATTERN
        bool correct = value.Length == 19
#else
        bool correct = value.Length == 16
#endif
            && value[0] == 'M' && value[1] == 'M' && value[2] == '/'
            && value[3] == 'd' && value[4] == 'd' && value[5] == '/'
            && value[6] == 'y' && value[7] == 'y' && value[8] == 'y' && value[9] == 'y'
            && value[10] == ' ' && value[11] == 'H' && value[12] == 'H'
            && value[13] == ':' && value[14] == 'm' && value[15] == 'm'
#if LONG_PATTERN || OFFSET_PATTERN
            && value[16] == ':' && value[17] == 's' && value[18] == 's'
#endif
#if OFFSET_PATTERN
            && value[19] == ' ' && value[20] == 'z' && value[21] == 'z' && value[22] == 'z'
#endif
            ;
        Console.WriteLine(Prefix + "minimal:" + correct);
        if (!correct) throw new InvalidOperationException("derived invariant composite pattern");
    }

    public static void Run()
    {
        PinCulture();
        Console.WriteLine("== " + Title + " ==");
        RunDirectChecks();
        Validate(Pointer, "calli");
        NullFirst(Pointer, "calli");
        Validate(DelegateRoute, "delegate");
        Check("delegate-return", Address(DateTimeFormatInfo.InvariantInfo).Method.ReturnType.FullName == "System.String");
        RunReflectionChecks();
        Console.WriteLine(Title + " end");
    }

    // Safe public content and mutable counterexamples; never invoke the private getter.
    public static void RunPublicOracle()
    {
        PinCulture();
#if OFFSET_PATTERN
        var info = DateTimeFormatInfo.InvariantInfo;
        var date = new DateTimeOffset(2024, 2, 3, 4, 5, 6, new TimeSpan(5, 30, 0));
        Check("public-composition", info.ShortDatePattern + " " + info.LongTimePattern + " zzz" == "MM/dd/yyyy HH:mm:ss zzz");
        Check("public-default", date.ToString(null, info) == "02/03/2024 04:05:06 +05:30");
        var clone = (DateTimeFormatInfo)info.Clone();
        clone.ShortDatePattern = "yyyy*MM*dd";
        Check("outside-native-date-setter", date.ToString(null, clone) == "2024*02*03 04:05:06 +05:30");
        clone.ShortDatePattern = info.ShortDatePattern;
        string[] patterns = { "HH:mm:ss zzz", "HH:mm:ss 'z'", "HH:mm:ss \"z\"", "HH:mm:ss \\z", "HH:mm:ss %z", "HH:mm:ss z", "" };
        string[] expected = { "04:05:06 +05:30", "04:05:06 z +05:30", "04:05:06 z +05:30", "04:05:06 z +05:30", "04:05:06 +5 +05:30", "04:05:06 +5", " +05:30" };
        for (int index = 0; index < patterns.Length; index++)
        {
            clone.LongTimePattern = patterns[index];
            Check("outside-native-time-setter-" + index, date.ToString(null, clone) == "02/03/2024 " + expected[index]);
        }
        Console.WriteLine("DateTimeOffset pattern public oracle end");
#elif LONG_PATTERN
        var info = DateTimeFormatInfo.InvariantInfo;
        var date = new DateTime(2024, 2, 3, 4, 5, 6);
        Check("public-composition", info.ShortDatePattern + " " + info.LongTimePattern == "MM/dd/yyyy HH:mm:ss");
        Check("public-g", date.ToString("G", info) == "02/03/2024 04:05:06");
        var clone = (DateTimeFormatInfo)info.Clone();
        clone.ShortDatePattern = "yyyy*MM*dd";
        Check("outside-native-date-setter", date.ToString("G", clone) == "2024*02*03 04:05:06");
        clone.LongTimePattern = "HH~mm";
        Check("outside-native-time-setter", date.ToString("G", clone) == "2024*02*03 04~05");
        clone.ShortDatePattern = "";
        clone.LongTimePattern = "";
        Check("outside-native-empty-patterns", date.ToString("G", clone) == " ");
        Console.WriteLine("General long-time pattern public oracle end");
#else
        var info = DateTimeFormatInfo.InvariantInfo;
        var date = new DateTime(2024, 2, 3, 4, 5, 0);
        Check("public-composition", info.ShortDatePattern + " " + info.ShortTimePattern == "MM/dd/yyyy HH:mm");
        Check("public-g", date.ToString("g", info) == "02/03/2024 04:05");
        var clone = (DateTimeFormatInfo)info.Clone();
        clone.ShortDatePattern = "yyyy*MM*dd";
        Check("outside-native-date-setter", date.ToString("g", clone) == "2024*02*03 04:05");
        clone.ShortTimePattern = "HH~mm";
        Check("outside-native-time-setter", date.ToString("g", clone) == "2024*02*03 04~05");
        clone.ShortDatePattern = "";
        clone.ShortTimePattern = "";
        Check("outside-native-empty-patterns", date.ToString("g", clone) == " ");
        Console.WriteLine("General short-time pattern public oracle end");
#endif
    }
}
