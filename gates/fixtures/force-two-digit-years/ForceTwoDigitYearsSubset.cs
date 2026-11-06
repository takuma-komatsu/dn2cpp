using System;
using System.Globalization;
using System.Reflection;

namespace ForceTwoDigitYearsSubset;

internal static class Program
{
    private delegate bool Query(DateTimeFormatInfo receiver);
    private delegate bool ClosedQuery();
    private static int trace;
    private static bool Direct(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture required");
    private static bool Pointer(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture required");
    private static ClosedQuery Address(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture required");
    private static bool DelegateRoute(DateTimeFormatInfo receiver) => Address(receiver)();
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod("get_HasForceTwoDigitYears", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static bool Reflect(DateTimeFormatInfo receiver) => (bool)Metadata().Invoke(receiver, null)!;
    private static DateTimeFormatInfo Receiver() { trace++; return null!; }
    private static DateTimeFormatInfo ThrowReceiver() { trace++; throw new FormatException("receiver producer"); }

    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    private static void Check(string label, bool result)
    {
        Console.WriteLine("force-two-digit-years-" + label + ":" + result);
        if (!result) throw new InvalidOperationException(label);
    }

    private static void Validate(Query query, string route)
    {
        foreach (var info in new[] { DateTimeFormatInfo.InvariantInfo, (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone() })
        {
            bool value = query(info);
            object boxed = value;
            Check(route + "-zero", value == false);
            Check(route + "-actual-bool", boxed.GetType().FullName == "System.Boolean"
                && !boxed.GetType().IsEnum && boxed.GetType() != typeof(int));
            Check(route + "-name", boxed.ToString() == "False");
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
        Console.WriteLine("force-two-digit-years-direct-end");
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        Validate(Reflect, "reflection");
        Check("reflection-return", Metadata().ReturnType.FullName == "System.Boolean");
        Exception error = null!;
        try { _ = Reflect(null!); }
        catch (Exception observed) { error = observed; }
        Check("reflection-null", error is TargetException);
        Console.WriteLine("force-two-digit-years-reflection-end");
    }

    public static void RunMinimal()
    {
        PinCulture();
        bool correct = Direct(DateTimeFormatInfo.InvariantInfo) == false;
        Console.WriteLine("force-two-digit-years-minimal:" + correct);
        if (!correct) throw new InvalidOperationException("derived invariant year policy");
    }

    public static void Run()
    {
        PinCulture();
        Console.WriteLine("== Invariant force-two-digit years ==");
        RunDirectChecks();
        Validate(Pointer, "calli");
        NullFirst(Pointer, "calli");
        Validate(DelegateRoute, "delegate");
        Check("delegate-return", Address(DateTimeFormatInfo.InvariantInfo).Method.ReturnType.FullName == "System.Boolean");
        RunReflectionChecks();
        Console.WriteLine("Invariant force-two-digit years end");
    }

    // Public formatting only: never invoke the private getter on CLR.
    public static void RunPublicOracle()
    {
        PinCulture();
        var info = DateTimeFormatInfo.InvariantInfo;
        var year125 = new DateTime(125, 1, 1);
        var year5 = new DateTime(5, 1, 1);
        Check("public-gregorian", info.Calendar is GregorianCalendar);
        Check("public-short", year125.ToString("%y", info) == "25" && year125.ToString("yy", info) == "25");
        Check("public-long", year125.ToString("yyyy", info) == "0125" && year5.ToString("yyyy", info) == "0005");
        var clone = (DateTimeFormatInfo)info.Clone();
        Check("public-clone", year125.ToString("yy", clone) == "25");
        Console.WriteLine("Force-two-digit years public formatting oracle end");
    }
}
