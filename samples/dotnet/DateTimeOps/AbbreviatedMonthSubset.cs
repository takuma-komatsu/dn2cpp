using System;
using System.Globalization;
using System.Reflection;

#if FULL_MONTH
namespace FullMonthSubset;
#else
namespace AbbreviatedMonthSubset;
#endif

internal static class Program
{
    private delegate string Query(DateTimeFormatInfo info, int month);
#if FULL_MONTH
    private const string Prefix = "full-month-";
    private const string Header = "== Full invariant month names ==";
#else
    private const string Prefix = "abbreviated-month-";
    private const string Header = "== Abbreviated invariant month names ==";
#endif
    private static int checks;
    private static int trace;

#if FULL_MONTH
    private static string Direct(DateTimeFormatInfo info, int month) => info.GetMonthName(month);
    private static string Pointer(DateTimeFormatInfo info, int month) => info.GetMonthName(month);
    private static Func<int, string> Address(DateTimeFormatInfo info) => info.GetMonthName;
#else
    private static string Direct(DateTimeFormatInfo info, int month) => info.GetAbbreviatedMonthName(month);
    private static string Pointer(DateTimeFormatInfo info, int month) => info.GetAbbreviatedMonthName(month);
    private static Func<int, string> Address(DateTimeFormatInfo info) => info.GetAbbreviatedMonthName;
#endif
    private static string DelegateRoute(DateTimeFormatInfo info, int month) => Address(info)(month);
#if FULL_MONTH
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod(nameof(DateTimeFormatInfo.GetMonthName))!;
#else
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod(nameof(DateTimeFormatInfo.GetAbbreviatedMonthName))!;
#endif
    private static string Reflect(DateTimeFormatInfo info, int month) => (string)Metadata().Invoke(info, new object[] { month })!;
    private static DateTimeFormatInfo Receiver() { trace = trace * 10 + 1; return null!; }
    private static int Argument() { trace = trace * 10 + 2; return 14; }
    private static int ThrowArgument() { trace = trace * 10 + 2; throw new FormatException("month producer"); }

    private static void Check(string label, bool success)
    {
        Console.WriteLine(Prefix + label + ":" + success);
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
#if FULL_MONTH
        string[] expected = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" };
#else
        string[] expected = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" };
#endif
        for (int value = 1; value <= 13; value++)
        {
            Check(route + "-" + value, query(invariant, value) == expected[value - 1]);
            Check(route + "-identity-" + value, ReferenceEquals(query(invariant, value), query(invariant, value)));
        }
        foreach (int value in new[] { 0, 14, -1, int.MinValue, int.MaxValue })
        {
            Exception observed = null!;
            try { query(invariant, value); }
            catch (Exception error) { observed = error; }
            if (reflection)
            {
                Check(route + "-wrapped-" + value, observed is TargetInvocationException);
                observed = ((TargetInvocationException)observed).InnerException!;
            }
            if (observed is not ArgumentOutOfRangeException range)
                throw new InvalidOperationException(route + " wrong range category");
            string message = "Valid values are between 1 and 13, inclusive. (Parameter 'month')"
                + Environment.NewLine + "Actual value was " + value.ToString(CultureInfo.InvariantCulture) + ".";
            Check(route + "-range-" + value, range.ParamName == "month"
                && range.ActualValue is not null && range.ActualValue.GetType() == typeof(int)
                && (int)range.ActualValue == value && range.Message == message);
        }
    }

    private static void ValidateNull(Query query, string label)
    {
        try { query(null!, 14); }
        catch (NullReferenceException) { Check(label, true); return; }
        throw new InvalidOperationException(label + " did not check receiver first");
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        Validate("reflect", Reflect, true);
        Console.WriteLine(Prefix + "reflection-end");
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
        catch (FormatException error) { Check("producer-preempts-null", trace == 12 && error.Message == "month producer"); return; }
        throw new InvalidOperationException("throwing argument did not preempt receiver check");
    }

    public static void Run()
    {
        PinCulture();
        Console.WriteLine(Header);
        RunDirectChecks();
        Validate("pointer", Pointer, false);
        ValidateNull(Pointer, "pointer-null-first");
        Validate("delegate", DelegateRoute, false);
        RunReflectionChecks();
        var names = DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthNames;
        names[0] = "changed";
#if FULL_MONTH
        Check("array-copy-neighbor", Direct(DateTimeFormatInfo.InvariantInfo, 1) == "January"
            && DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthNames[0] == "Jan");
#else
        Check("array-copy-neighbor", Direct(DateTimeFormatInfo.InvariantInfo, 1) == "Jan"
            && DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthNames[0] == "Jan");
#endif
        var clone = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        clone.DateSeparator = "~";
        Check("separator-clone-neighbor", clone.DateSeparator == "~" && DateTimeFormatInfo.InvariantInfo.DateSeparator == "/");
        Console.WriteLine(Prefix + "end:" + checks);
    }
}
