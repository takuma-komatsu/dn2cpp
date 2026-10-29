using System;
using System.Globalization;
using System.Reflection;

namespace FormatFlagsSubset;

internal enum Flag { None = 0 }

internal static class Program
{
    private delegate Flag Query(DateTimeFormatInfo receiver);
    private delegate Flag ClosedQuery();
    private static int trace;
    private static Flag Direct(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture required");
    private static Flag Pointer(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture required");
    private static ClosedQuery Address(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture required");
    private static Flag DelegateRoute(DateTimeFormatInfo receiver) => Address(receiver)();
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod("get_FormatFlags", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static Flag Reflect(DateTimeFormatInfo receiver) => (Flag)Metadata().Invoke(receiver, null)!;
    private static DateTimeFormatInfo Receiver() { trace++; return null!; }
    private static DateTimeFormatInfo ThrowReceiver() { trace++; throw new FormatException("receiver producer"); }

    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    private static void Check(string label, bool result)
    {
        Console.WriteLine("format-flags-" + label + ":" + result);
        if (!result) throw new InvalidOperationException(label);
    }

    private static void Validate(Query query, string route)
    {
        foreach (var info in new[] { DateTimeFormatInfo.InvariantInfo, (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone() })
        {
            Flag value = query(info);
            object boxed = value;
            Check(route + "-zero", (int)value == 0);
            Check(route + "-actual-enum", boxed.GetType().FullName == "System.Globalization.DateTimeFormatFlags"
                && boxed.GetType().IsEnum && boxed.GetType() != typeof(int));
            Check(route + "-name", boxed.ToString() == "None");
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
        Console.WriteLine("format-flags-direct-end");
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        Validate(Reflect, "reflection");
        Check("reflection-return", Metadata().ReturnType.FullName == "System.Globalization.DateTimeFormatFlags");
        Exception error = null!;
        try { _ = Reflect(null!); }
        catch (Exception observed) { error = observed; }
        Check("reflection-null", error is TargetException);
        Console.WriteLine("format-flags-reflection-end");
    }

    public static void RunMinimal()
    {
        PinCulture();
        bool correct = Direct(DateTimeFormatInfo.InvariantInfo) == Flag.None;
        Console.WriteLine("format-flags-minimal:" + correct);
        if (!correct) throw new InvalidOperationException("derived invariant flags");
    }

    public static void Run()
    {
        PinCulture();
        Console.WriteLine("== Invariant format flags ==");
        RunDirectChecks();
        Validate(Pointer, "calli");
        NullFirst(Pointer, "calli");
        Validate(DelegateRoute, "delegate");
        Check("delegate-return", Address(DateTimeFormatInfo.InvariantInfo).Method.ReturnType.FullName == "System.Globalization.DateTimeFormatFlags");
        RunReflectionChecks();
        Console.WriteLine("Invariant format flags end");
    }

    // Public-data derivation only, never invoke the private getter on CLR.
    public static void RunPublicOracle()
    {
        PinCulture();
        var info = DateTimeFormatInfo.InvariantInfo;
        string[] months = info.MonthNames;
        string[] shortMonths = info.AbbreviatedMonthNames;
        string[] genitive = info.MonthGenitiveNames;
        string[] shortGenitive = info.AbbreviatedMonthGenitiveNames;
        bool same = true;
        for (int i = 0; i < months.Length; i++)
            same &= months[i] == genitive[i] && shortMonths[i] == shortGenitive[i];
        Check("public-genitive-equal", same);
        bool spaces = false;
        bool digitPrefix = false;
        foreach (var array in new[] { months, shortMonths, genitive, shortGenitive, info.DayNames, info.AbbreviatedDayNames })
        {
            foreach (string value in array)
            {
                foreach (char character in value) spaces |= char.IsWhiteSpace(character);
                digitPrefix |= value.Length != 0 && value[0] >= '0' && value[0] <= '9';
            }
        }
        Check("public-no-spaces", !spaces);
        Check("public-no-digit-prefix", !digitPrefix);
        Check("public-gregorian", info.Calendar is GregorianCalendar);
        Console.WriteLine("Format flags public data derivation end");
    }
}
