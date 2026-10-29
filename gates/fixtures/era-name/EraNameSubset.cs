using System;
using System.Globalization;
using System.Reflection;

namespace EraNameSubset;

internal static class Program
{
    private delegate string Query(DateTimeFormatInfo info, int era);
    private const string Prefix = "era-name-";
    private const string Header = "== Invariant era names ==";
    private static int checks;
    private static int trace;

    private static string Direct(DateTimeFormatInfo info, int era) => info.GetEraName(era);
    private static string Pointer(DateTimeFormatInfo info, int era) => info.GetEraName(era);
    private static Func<int, string> Address(DateTimeFormatInfo info) => info.GetEraName;
    private static string DelegateRoute(DateTimeFormatInfo info, int era) => Address(info)(era);
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod(nameof(DateTimeFormatInfo.GetEraName))!;
    private static string Reflect(DateTimeFormatInfo info, int era) => (string)Metadata().Invoke(info, new object[] { era })!;
    private static DateTimeFormatInfo Receiver() { trace = trace * 10 + 1; return null!; }
    private static int Argument() { trace = trace * 10 + 2; return 2; }
    private static int ThrowArgument() { trace = trace * 10 + 2; throw new FormatException("era producer"); }

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
        string[] expected = { "A.D.", "A.D." };
        for (int value = 0; value <= 1; value++)
        {
            Check(route + "-" + value, query(invariant, value) == expected[value]);
            Check(route + "-identity-" + value, ReferenceEquals(query(invariant, value), query(invariant, value)));
        }
        foreach (int value in new[] { -1, 2, int.MinValue, int.MaxValue })
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
            string message = "Era value was not valid. (Parameter 'era')"
                + Environment.NewLine + "Actual value was " + value.ToString(CultureInfo.InvariantCulture) + ".";
            Check(route + "-range-" + value, range.ParamName == "era"
                && range.ActualValue is not null && range.ActualValue.GetType() == typeof(int)
                && (int)range.ActualValue == value && range.Message == message);
        }
    }

    private static void ValidateNull(Query query, string label)
    {
        foreach (int era in new[] { 0, 1, 2, int.MinValue })
        {
            bool observed = false;
            try { query(null!, era); }
            catch (NullReferenceException) { observed = true; }
            Check(label + "-" + era, observed);
        }
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        Validate("reflect", Reflect, true);
        Exception nullTarget = null!;
        try { Reflect(null!, 2); }
        catch (Exception error) { nullTarget = error; }
        Check("reflection-null-target", nullTarget is TargetException);
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
        catch (FormatException error) { Check("producer-preempts-null", trace == 12 && error.Message == "era producer"); return; }
        throw new InvalidOperationException("throwing argument did not preempt receiver check");
    }

    public static void RunMinimal()
    {
        PinCulture();
        string name = Direct(DateTimeFormatInfo.InvariantInfo, 0);
        Console.WriteLine("era-name-minimal:" + (name == "A.D."));
        if (name != "A.D.") throw new InvalidOperationException("current era");
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
        var clone = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        clone.DateSeparator = "~";
        Check("separator-clone-neighbor", clone.DateSeparator == "~" && DateTimeFormatInfo.InvariantInfo.DateSeparator == "/");
        Console.WriteLine(Prefix + "end:" + checks);
    }
}
