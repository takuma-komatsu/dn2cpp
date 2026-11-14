using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace DateTimeFormatInfoSubset;

internal static class Program
{
    private static int trace;
    internal delegate ReadOnlySpan<T> SpanAccessor<T>();

    // The IL fixture replaces only address/private calls unavailable in C#.
    internal static string DayDirect(DateTimeFormatInfo info, DayOfWeek day) => info.GetAbbreviatedDayName(day);
    internal static string DayAddress(DateTimeFormatInfo info, DayOfWeek day) => throw new InvalidOperationException("IL fixture required");
    internal static ReadOnlySpan<T> AmSpan<T>(DateTimeFormatInfo info) => throw new InvalidOperationException("IL fixture required");
    internal static ReadOnlySpan<T> PmSpan<T>(DateTimeFormatInfo info) => throw new InvalidOperationException("IL fixture required");
    internal static ReadOnlySpan<T> DateSpan<T>(DateTimeFormatInfo info) => throw new InvalidOperationException("IL fixture required");
    internal static ReadOnlySpan<T> TimeSpan<T>(DateTimeFormatInfo info) => throw new InvalidOperationException("IL fixture required");
    internal static ReadOnlySpan<T> DecimalSpan<T>(DateTimeFormatInfo info) => throw new InvalidOperationException("IL fixture required");
    internal static SpanAccessor<T> DateSpanDelegate<T>(DateTimeFormatInfo info) => throw new InvalidOperationException("IL fixture required");
    internal static string InternalMonth(DateTimeFormatInfo info, int month, int style, bool abbreviated) => throw new InvalidOperationException("IL fixture required");
    internal static bool Adjust(DateTimeFormatInfo info, ref int year, ref int month, bool parsed) => throw new InvalidOperationException("IL fixture required");
    internal static bool Tokenize(DateTimeFormatInfo info, ref int token, ref int value) => throw new InvalidOperationException("IL fixture required");

    private static DateTimeFormatInfo Receiver() { trace = trace * 10 + 1; return null; }
    private static DayOfWeek Argument() { trace = trace * 10 + 2; return (DayOfWeek)7; }
    private static DayOfWeek ThrowArgument() { trace = trace * 10 + 2; throw new FormatException("day producer"); }

    internal static string Describe(Exception error)
    {
        if (error is TargetInvocationException { InnerException: { } inner })
            return "wrapped " + Describe(inner);
        string text = error.GetType().Name;
        if (error is ArgumentOutOfRangeException { ActualValue: { } actual } range)
            text += " param=" + range.ParamName + " actual=" + actual.GetType().Name + ":"
                + Convert.ToInt64(actual, CultureInfo.InvariantCulture);
        else if (error is ArgumentException argument)
            text += " param=" + argument.ParamName;
        return text + " message=" + error.Message.Replace(Environment.NewLine, " | ");
    }

    private static string Escape(string text)
    {
        var builder = new StringBuilder();
        foreach (char c in text)
            builder.Append(c is >= ' ' and <= '~' ? c.ToString() : "U+" + ((int)c).ToString("X4", CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    internal static string Show(Func<string> query)
    {
        try { return query(); }
        catch (Exception error) { return Describe(error); }
    }

    private static void Names(string label, Func<DateTimeFormatInfo, object, string> reflected, Func<DateTimeFormatInfo, int, string> direct,
        Func<int, object> box, int[] values, bool includeNull = true)
    {
        var info = DateTimeFormatInfo.InvariantInfo;
        foreach (int value in values)
            Console.WriteLine(label + " " + value + " direct=" + Show(() => direct(info, value))
                + " reflect=" + Show(() => reflected(info, box(value))));
        if (includeNull)
            Console.WriteLine(label + " null direct=" + Show(() => direct(null, values[^1]))
                + " reflect=" + Show(() => reflected(null, box(values[0]))));
    }

    private static void NameArray(string label, Func<DateTimeFormatInfo, string[]> read)
    {
        var invariant = DateTimeFormatInfo.InvariantInfo;
        var clone = (DateTimeFormatInfo)invariant.Clone();
        foreach (var info in new[] { invariant, clone })
        {
            string[] first = read(info), second = read(info);
            string before = string.Join(",", first);
            first[0] = "changed";
            Console.WriteLine(label + (info == clone ? " clone " : " invariant ") + before
                + " shared=" + ReferenceEquals(first, second) + " after=" + read(info)[0]);
        }
    }

    public static void Run()
    {
        Console.WriteLine("== DateTimeFormatInfo names ==");
        NameArray("AbbreviatedMonthNames", info => info.AbbreviatedMonthNames);
        NameArray("AbbreviatedMonthGenitiveNames", info => info.AbbreviatedMonthGenitiveNames);
        Console.WriteLine("DateTimeFormatInfo names end");
    }

    private static void Patterns()
    {
        var info = DateTimeFormatInfo.InvariantInfo;
        MethodInfo metadata = typeof(DateTimeFormatInfo).GetMethod("GetAllDateTimePatterns", new[] { typeof(char) });
        foreach (char format in "dDfFgGmMoOrRstTuUyY")
        {
            string[] first = info.GetAllDateTimePatterns(format), second = info.GetAllDateTimePatterns(format);
            string text = string.Join(" | ", first);
            first[0] = "changed";
            var reflected = (string[])metadata.Invoke(info, new object[] { format });
            Console.WriteLine("patterns " + format + " " + text + " shared=" + ReferenceEquals(first, second)
                + " after=" + (info.GetAllDateTimePatterns(format)[0] == second[0])
                + " reflect=" + (string.Join(" | ", reflected) == string.Join(" | ", second)));
        }
        Func<char, string[]> callable = info.GetAllDateTimePatterns;
        Console.WriteLine("patterns callable=" + string.Join(" | ", callable('g')));
        string[] all = info.GetAllDateTimePatterns();
        Console.WriteLine("all patterns=" + string.Join(" | ", all));
        all[0] = "changed";
        Console.WriteLine("all patterns detached=" + (info.GetAllDateTimePatterns()[0] != "changed"));
        foreach (char format in new[] { 'x', '\0', '\uffff', '\ud800' })
            Console.WriteLine(Escape("patterns " + format + " direct=" + Show(() => info.GetAllDateTimePatterns(format)[0])
                + " reflect=" + Show(() => ((string[])metadata.Invoke(info, new object[] { format }))[0])));
        Console.WriteLine("patterns null=" + Show(() => ((DateTimeFormatInfo)null).GetAllDateTimePatterns('s')[0]));
    }

    private static void Helpers()
    {
        var info = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        foreach (int style in new[] { 0, 1, 2, 7 })
            foreach (bool abbreviated in new[] { false, true })
                Console.WriteLine("internal month " + style + "/" + abbreviated + "="
                    + InternalMonth(info, 1, style, abbreviated) + "/" + InternalMonth(info, 13, style, abbreviated)
                    + " invalid=" + Show(() => InternalMonth(info, 0, style, abbreviated)));
        int year = -1, month = 13;
        Console.WriteLine("adjust=" + Adjust(info, ref year, ref month, true) + ":" + year + "/" + month);
        Console.WriteLine("adjust alias=" + Adjust(info, ref year, ref year, false) + ":" + year);
        Console.WriteLine("adjust null=" + Adjust(info, ref Unsafe.NullRef<int>(), ref Unsafe.NullRef<int>(), true));
        Console.WriteLine("span constants=" + AmSpan<char>(info).ToString() + "/" + PmSpan<char>(info).ToString()
            + "/" + TimeSpan<char>(info).ToString() + "/" + DecimalSpan<char>(info).ToString());
        Console.WriteLine("span UTF8 constants=" + Encoding.UTF8.GetString(AmSpan<byte>(info)) + "/"
            + Encoding.UTF8.GetString(PmSpan<byte>(info)) + "/" + Encoding.UTF8.GetString(TimeSpan<byte>(info))
            + "/" + Encoding.UTF8.GetString(DecimalSpan<byte>(info)));
        var readChars = DateSpanDelegate<char>(info);
        var readBytes = DateSpanDelegate<byte>(info);
        foreach (string separator in new[] { "~", "\u00e9\U0001f600", "\ud800" })
        {
            info.DateSeparator = separator;
            ReadOnlySpan<char> chars = DateSpan<char>(info);
            ReadOnlySpan<byte> bytes = readBytes();
            info.DateSeparator = "changed";
            GC.Collect();
            Console.WriteLine("span separator=" + Escape(chars.ToString()) + " bytes=" + Convert.ToHexString(bytes)
                + " now=" + readChars().ToString() + "/" + Encoding.UTF8.GetString(DateSpan<byte>(info)));
        }
        Console.WriteLine("DTFI helpers end");
    }

    public static void RunIntercepts()
    {
        Console.WriteLine("== DateTimeFormatInfo intercepts ==");
        int[] days = { 0, 1, 2, 3, 4, 5, 6, -1, 7, int.MinValue, int.MaxValue };
        int[] months = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 0, 14 };
        int[] eras = { 0, 1, -1, 2 };
        Names("abbreviated-day", (info, value) => (string)typeof(DateTimeFormatInfo).GetMethod("GetAbbreviatedDayName").Invoke(info, new[] { value }), (info, value) => DayDirect(info, (DayOfWeek)value),
            value => (DayOfWeek)value, days);
        Names("day", (info, value) => (string)typeof(DateTimeFormatInfo).GetMethod("GetDayName").Invoke(info, new[] { value }),
            (info, value) => info.GetDayName((DayOfWeek)value), value => (DayOfWeek)value, days);
        Names("abbreviated-month", (info, value) => (string)typeof(DateTimeFormatInfo).GetMethod("GetAbbreviatedMonthName").Invoke(info, new[] { value }),
            (info, value) => info.GetAbbreviatedMonthName(value), value => value, months);
        Names("month", (info, value) => (string)typeof(DateTimeFormatInfo).GetMethod("GetMonthName").Invoke(info, new[] { value }),
            (info, value) => info.GetMonthName(value), value => value, months);
        Names("era", (info, value) => (string)typeof(DateTimeFormatInfo).GetMethod("GetEraName").Invoke(info, new[] { value }),
            (info, value) => info.GetEraName(value), value => value, eras);
        Names("abbreviated-era", (info, value) => (string)typeof(DateTimeFormatInfo).GetMethod("GetAbbreviatedEraName").Invoke(info, new[] { value }),
            (info, value) => info.GetAbbreviatedEraName(value), value => value, eras);
        foreach (int day in new[] { 1, -1, 7 })
            Console.WriteLine("day calli " + day + "=" + Show(() => DayAddress(DateTimeFormatInfo.InvariantInfo, (DayOfWeek)day)));
        trace = 0;
        Console.WriteLine("producer null receiver=" + Show(() => DayDirect(Receiver(), Argument())) + " trace=" + trace);
        trace = 0;
        Console.WriteLine("producer throwing argument=" + Show(() => DayDirect(Receiver(), ThrowArgument())) + " trace=" + trace);
        NameArray("AbbreviatedDayNames", info => info.AbbreviatedDayNames);
        Patterns();
        Helpers();
        Console.WriteLine("DateTimeFormatInfo intercepts end");
    }

    public static void RunRefusals()
    {
        Console.WriteLine("== DateTimeFormatInfo explicit refusals ==");
        var info = DateTimeFormatInfo.InvariantInfo;
        var writable = (DateTimeFormatInfo)info.Clone();
        var calendar = (Calendar)RuntimeHelpers.GetUninitializedObject(typeof(GregorianCalendar));
        Console.WriteLine("calendar get=" + Show(() => info.Calendar.GetType().Name));
        Console.WriteLine("calendar read-only null=" + Show(() => { info.Calendar = null; return "ok"; }));
        Console.WriteLine("calendar null=" + Show(() => { writable.Calendar = null; return "ok"; }));
        Console.WriteLine("calendar set=" + Show(() => { writable.Calendar = calendar; return "ok"; }));
        Console.WriteLine("calendar null receiver=" + Show(() => ((DateTimeFormatInfo)null).Calendar.GetType().Name));
        int token = 123, value = 456;
        Console.WriteLine("tokenize=" + Show(() => Tokenize(info, ref token, ref value).ToString()) + " outputs=" + token + "/" + value);
        Console.WriteLine("tokenize null=" + Show(() => Tokenize(null, ref token, ref value).ToString()) + " outputs=" + token + "/" + value);
        Console.WriteLine("DateTimeFormatInfo explicit refusals end");
    }

    private static void NameIdentity(string label, Func<string> read) =>
        Console.WriteLine(label + " identity=" + ReferenceEquals(read(), read()));

    public static void RunNameEdges()
    {
        Console.WriteLine("== DateTimeFormatInfo name edges ==");
        var info = DateTimeFormatInfo.InvariantInfo;
        NameIdentity("abbreviated-day", () => DayDirect(info, DayOfWeek.Sunday));
        NameIdentity("day", () => info.GetDayName(DayOfWeek.Sunday));
        NameIdentity("abbreviated-month", () => info.GetAbbreviatedMonthName(1));
        NameIdentity("month", () => info.GetMonthName(1));
        NameIdentity("era", () => info.GetEraName(0));
        NameIdentity("abbreviated-era", () => info.GetAbbreviatedEraName(0));
        int[] months = { -1, int.MinValue, int.MaxValue };
        int[] eras = { int.MinValue, int.MaxValue };
        Names("abbreviated-month", (target, value) => (string)typeof(DateTimeFormatInfo).GetMethod("GetAbbreviatedMonthName").Invoke(target, new[] { value }),
            (target, value) => target.GetAbbreviatedMonthName(value), value => value, months, includeNull: false);
        Names("month", (target, value) => (string)typeof(DateTimeFormatInfo).GetMethod("GetMonthName").Invoke(target, new[] { value }),
            (target, value) => target.GetMonthName(value), value => value, months, includeNull: false);
        Names("era", (target, value) => (string)typeof(DateTimeFormatInfo).GetMethod("GetEraName").Invoke(target, new[] { value }),
            (target, value) => target.GetEraName(value), value => value, eras, includeNull: false);
        Names("abbreviated-era", (target, value) => (string)typeof(DateTimeFormatInfo).GetMethod("GetAbbreviatedEraName").Invoke(target, new[] { value }),
            (target, value) => target.GetAbbreviatedEraName(value), value => value, eras, includeNull: false);
        MethodInfo metadata = typeof(DateTimeFormatInfo).GetMethod("GetAllDateTimePatterns", new[] { typeof(char) });
        foreach (char format in new[] { 'a', 'z', '\udfff' })
            Console.WriteLine(Escape("patterns " + format + " direct=" + Show(() => info.GetAllDateTimePatterns(format)[0])
                + " reflect=" + Show(() => ((string[])metadata.Invoke(info, new object[] { format }))[0])));
        Console.WriteLine("patterns null reflect=" + Show(() => ((string[])metadata.Invoke(null, new object[] { 's' }))[0]));
        var clone = (DateTimeFormatInfo)info.Clone();
        clone.DateSeparator = "~";
        Console.WriteLine("clone separator=" + clone.DateSeparator + " invariant separator=" + info.DateSeparator
            + " clone day=" + clone.GetAbbreviatedDayName(DayOfWeek.Monday));
        Console.WriteLine("DateTimeFormatInfo name edges end");
    }
}
