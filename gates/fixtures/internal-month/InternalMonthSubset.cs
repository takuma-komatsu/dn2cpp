using System;
using System.Globalization;
using System.Reflection;

namespace InternalMonthSubset;

internal enum Style { Regular, Genitive, Leap }

internal static class Program
{
    private delegate string Query(DateTimeFormatInfo receiver, int month, Style style, bool abbreviated);
    private delegate string ClosedQuery(int month, Style style, bool abbreviated);
    private static int trace;
    private static string Direct(DateTimeFormatInfo receiver, int month, Style style, bool abbreviated) => throw new InvalidOperationException("native fixture required");
    private static string Pointer(DateTimeFormatInfo receiver, int month, Style style, bool abbreviated) => throw new InvalidOperationException("native fixture required");
    private static ClosedQuery Address(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture required");
    private static string DelegateRoute(DateTimeFormatInfo receiver, int month, Style style, bool abbreviated) => Address(receiver)(month, style, abbreviated);
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod("InternalGetMonthName", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static string Reflect(DateTimeFormatInfo receiver, int month, Style style, bool abbreviated) => (string)Metadata().Invoke(receiver, new object[] { month, style, abbreviated })!;
    private static DateTimeFormatInfo Receiver() { trace = trace * 10 + 1; return null!; }
    private static int Month() { trace = trace * 10 + 2; return 0; }
    private static Style Selector() { trace = trace * 10 + 3; return Style.Leap; }
    private static bool Abbreviated() { trace = trace * 10 + 4; return true; }
    private static bool ThrowArgument() { trace = trace * 10 + 4; throw new FormatException("month producer"); }

    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    private static void Check(string label, bool result)
    {
        Console.WriteLine("internal-month-" + label + ":" + result);
        if (!result) throw new InvalidOperationException(label);
    }

    private static void Validate(string route, Query query, bool reflection)
    {
        string[] full = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" };
        string[] shortNames = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" };
        foreach (int style in new[] { 0, 1, 2, 3, -1, int.MinValue, int.MaxValue })
        {
            foreach (bool abbreviated in new[] { false, true })
            {
                bool names = true;
                bool identity = true;
                for (int month = 1; month <= 13; month++)
                {
                    string actual = query(DateTimeFormatInfo.InvariantInfo, month, (Style)style, abbreviated);
                    names &= actual == (style != 2 && abbreviated ? shortNames[month - 1] : full[month - 1]);
                    identity &= ReferenceEquals(actual, query(DateTimeFormatInfo.InvariantInfo, month, (Style)style, abbreviated));
                }
                Check(route + "-names-" + style + "-" + abbreviated, names);
                Check(route + "-identity-" + style + "-" + abbreviated, identity);
                foreach (int month in new[] { 0, 14, -1, int.MinValue, int.MaxValue })
                {
                    Exception observed = null!;
                    try { query(DateTimeFormatInfo.InvariantInfo, month, (Style)style, abbreviated); }
                    catch (Exception error) { observed = error; }
                    if (reflection)
                    {
                        if (observed is not TargetInvocationException wrapped) throw new InvalidOperationException("missing reflection wrapper");
                        observed = wrapped.InnerException!;
                    }
                    string message = "Valid values are between 1 and 13, inclusive. (Parameter 'month')"
                        + Environment.NewLine + "Actual value was " + month.ToString(CultureInfo.InvariantCulture) + ".";
                    if (observed is not ArgumentOutOfRangeException range || range.ParamName != "month"
                        || range.ActualValue is not int actualMonth || actualMonth != month || range.Message != message)
                        throw new InvalidOperationException("wrong original Int32 month payload");
                }
                Check(route + "-range-" + style + "-" + abbreviated, true);
            }
        }
    }

    private static void NullFirst(Query query, string route)
    {
        bool observed = false;
        try { query(null!, 0, Style.Leap, true); }
        catch (NullReferenceException) { observed = true; }
        Check(route + "-null-first", observed);
    }

    public static void RunDirectChecks()
    {
        PinCulture();
        Validate("direct", Direct, false);
        NullFirst(Direct, "direct");
        trace = 0;
        bool observed = false;
        try { Direct(Receiver(), Month(), Selector(), Abbreviated()); }
        catch (NullReferenceException) { observed = true; }
        Check("producer-order", observed && trace == 1234);
        trace = 0;
        observed = false;
        try { Direct(Receiver(), Month(), Selector(), ThrowArgument()); }
        catch (FormatException error) { observed = error.Message == "month producer"; }
        Check("producer-throw", observed && trace == 1234);
        Console.WriteLine("internal-month-direct-end");
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        Validate("reflection", Reflect, true);
        Exception nullTarget = null!;
        try { Reflect(null!, 0, Style.Leap, true); }
        catch (Exception error) { nullTarget = error; }
        Check("reflection-null-target", nullTarget is TargetException);
        Console.WriteLine("internal-month-reflection-end");
    }

    public static void Run()
    {
        PinCulture();
        Console.WriteLine("== Internal invariant month selector ==");
        RunDirectChecks();
        Validate("calli", Pointer, false);
        NullFirst(Pointer, "calli");
        Validate("delegate", DelegateRoute, false);
        var method = Address(DateTimeFormatInfo.InvariantInfo).Method;
        var parameters = method.GetParameters();
        Check("delegate-signature", method.ReturnType == typeof(string) && parameters.Length == 3
            && parameters[0].ParameterType == typeof(int)
            && parameters[1].ParameterType.FullName == "System.Globalization.MonthNameStyles"
            && parameters[2].ParameterType == typeof(bool));
        RunReflectionChecks();
        Console.WriteLine("Internal invariant month selector end");
    }

    public static void RunMinimal()
    {
        PinCulture();
        string name = Direct(DateTimeFormatInfo.InvariantInfo, 1, Style.Leap, true);
        Console.WriteLine("internal-month-minimal:" + (name == "January"));
        if (name != "January") throw new InvalidOperationException("leap style must ignore abbreviation");
    }

    public static void RunPublicOracle()
    {
        PinCulture();
        var info = DateTimeFormatInfo.InvariantInfo;
        string[] full = info.MonthNames;
        string[] abbreviated = info.AbbreviatedMonthNames;
        string[] genitive = info.MonthGenitiveNames;
        string[] abbreviatedGenitive = info.AbbreviatedMonthGenitiveNames;
        Check("public-length", full.Length == 13 && abbreviated.Length == 13 && genitive.Length == 13 && abbreviatedGenitive.Length == 13);
        for (int index = 0; index < 13; index++)
            Check("public-arrays-" + index, full[index] == info.GetMonthName(index + 1)
                && abbreviated[index] == info.GetAbbreviatedMonthName(index + 1)
                && genitive[index] == full[index] && abbreviatedGenitive[index] == abbreviated[index]);
        Check("public-empty", full[12] == "" && abbreviated[12] == "");
        Console.WriteLine("Internal month public arrays oracle end");
    }
}
