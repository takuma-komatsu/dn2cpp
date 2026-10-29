using System;
using System.Globalization;
using System.Reflection;

namespace AbbreviatedDaySubset;

internal static class Program
{
    private delegate string Query(DateTimeFormatInfo info, DayOfWeek day);
    private static int checks;
    private static int trace;

    private static string Direct(DateTimeFormatInfo info, DayOfWeek day) => info.GetAbbreviatedDayName(day);
    private static string Pointer(DateTimeFormatInfo info, DayOfWeek day) => info.GetAbbreviatedDayName(day);
    private static Func<DayOfWeek, string> Address(DateTimeFormatInfo info) => info.GetAbbreviatedDayName;
    private static string DelegateRoute(DateTimeFormatInfo info, DayOfWeek day) => Address(info)(day);
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod(nameof(DateTimeFormatInfo.GetAbbreviatedDayName))!;
    private static string Reflect(DateTimeFormatInfo info, DayOfWeek day) => (string)Metadata().Invoke(info, new object[] { day })!;
    private static DateTimeFormatInfo Receiver() { trace = trace * 10 + 1; return null!; }
    private static DayOfWeek Argument() { trace = trace * 10 + 2; return (DayOfWeek)7; }
    private static DayOfWeek ThrowArgument() { trace = trace * 10 + 2; throw new FormatException("day producer"); }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("abbreviated-day-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
        checks++;
    }

    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    private static void Validate(string route, Query query, bool reflection)
    {
        var invariant = DateTimeFormatInfo.InvariantInfo;
        string[] expected = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        for (int value = 0; value < 7; value++)
        {
            Check(route + "-" + value, query(invariant, (DayOfWeek)value) == expected[value]);
            Check(route + "-identity-" + value, ReferenceEquals(query(invariant, (DayOfWeek)value), query(invariant, (DayOfWeek)value)));
        }
        foreach (int value in new[] { -1, 7, int.MinValue, int.MaxValue })
        {
            Exception observed = null!;
            try { query(invariant, (DayOfWeek)value); }
            catch (Exception error) { observed = error; }
            if (reflection)
            {
                Check(route + "-wrapped-" + value, observed is TargetInvocationException);
                observed = ((TargetInvocationException)observed).InnerException!;
            }
            if (observed is not ArgumentOutOfRangeException range)
                throw new InvalidOperationException(route + " wrong range category");
            string message = "Valid values are between Sunday and Saturday, inclusive. (Parameter 'dayofweek')"
                + Environment.NewLine + "Actual value was " + value.ToString(CultureInfo.InvariantCulture) + ".";
            Check(route + "-range-" + value, range.ParamName == "dayofweek"
                && range.ActualValue is not null && range.ActualValue.GetType() == typeof(DayOfWeek)
                && (int)(DayOfWeek)range.ActualValue == value && range.Message == message);
        }
    }

    private static void ValidateNull(Query query, string label)
    {
        try { query(null!, (DayOfWeek)7); }
        catch (NullReferenceException) { Check(label, true); return; }
        throw new InvalidOperationException(label + " did not check receiver first");
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        Validate("reflect", Reflect, true);
        Console.WriteLine("abbreviated-day-reflection-end");
    }

    public static void RunDirectChecks()
    {
        PinCulture();
        Validate("direct", Direct, false);
        ValidateNull(Direct, "null-first");
        trace = 0;
        bool nullObserved = false;
        try { Direct(Receiver(), Argument()); }
        catch (NullReferenceException) { nullObserved = true; }
        Check("producer-order", nullObserved && trace == 12);
        trace = 0;
        try { Direct(Receiver(), ThrowArgument()); }
        catch (FormatException error) { Check("producer-preempts-null", trace == 12 && error.Message == "day producer"); return; }
        throw new InvalidOperationException("throwing argument did not preempt receiver check");
    }

    public static void Run()
    {
        PinCulture();
        Console.WriteLine("== Abbreviated invariant day names ==");
        RunDirectChecks();
        Validate("pointer", Pointer, false);
        ValidateNull(Pointer, "pointer-null-first");
        Validate("delegate", DelegateRoute, false);
        RunReflectionChecks();
        var names = DateTimeFormatInfo.InvariantInfo.AbbreviatedDayNames;
        names[0] = "changed";
        Check("array-copy-neighbor", Direct(DateTimeFormatInfo.InvariantInfo, DayOfWeek.Sunday) == "Sun"
            && DateTimeFormatInfo.InvariantInfo.AbbreviatedDayNames[0] == "Sun");
        var clone = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        clone.DateSeparator = "~";
        Check("separator-clone-neighbor", clone.DateSeparator == "~" && DateTimeFormatInfo.InvariantInfo.DateSeparator == "/");
        Console.WriteLine("abbreviated-day-end:" + checks);
    }
}
