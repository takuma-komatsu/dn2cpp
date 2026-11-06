using System;
using System.Globalization;
using System.Reflection;

namespace DtfIParserFlagsSubset;

internal static class Program
{
    private delegate bool Query(DateTimeFormatInfo receiver);
    private delegate bool ClosedQuery();
    private delegate ClosedQuery AddressQuery(DateTimeFormatInfo receiver);
    private static bool DirectMonth(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool VirtualMonth(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool PointerMonth(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static ClosedQuery AddressMonth(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool DirectDay(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool VirtualDay(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool PointerDay(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static ClosedQuery AddressDay(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool DirectAdjustment(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool VirtualAdjustment(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool PointerAdjustment(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static ClosedQuery AddressAdjustment(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static string Name(int index) => index switch { 0 => "get_HasSpacesInMonthNames", 1 => "get_HasSpacesInDayNames", _ => "get_HasYearMonthAdjustment" };
    private static string Label(int index) => index switch { 0 => "month", 1 => "day", _ => "adjustment" };
    private static MethodInfo Metadata(int index) => typeof(DateTimeFormatInfo).GetMethod(Name(index), BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static int calls;
    private static int producerMode;
    private static DateTimeFormatInfo Receiver()
    {
        calls++;
        if (producerMode == 2) throw new FormatException("receiver producer");
        return producerMode == 1 ? null! : DateTimeFormatInfo.InvariantInfo;
    }
    private static bool ProducerMonth() => DirectMonth(Receiver());
    private static bool ProducerDay() => DirectDay(Receiver());
    private static bool ProducerAdjustment() => DirectAdjustment(Receiver());
    private static void Check(string label, bool value)
    {
        Console.WriteLine("dtfi-parser-flags-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }
    private static bool ActualFalse(object value) => value.GetType() == typeof(bool) && value is bool result && !result;
    private static void Route(Query query, string label)
    {
        Check(label + "-invariant", ActualFalse(query(DateTimeFormatInfo.InvariantInfo)));
        Check(label + "-clone", ActualFalse(query((DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone())));
        bool observed = false;
        try { query(null!); } catch (NullReferenceException) { observed = true; }
        Check(label + "-null", observed);
    }
    private static void ReflectionRow(int index)
    {
        string label = Label(index);
        var method = Metadata(index);
        Check(label + "-reflection-invariant", ActualFalse(method.Invoke(DateTimeFormatInfo.InvariantInfo, null)!));
        Check(label + "-reflection-clone", ActualFalse(method.Invoke((DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone(), null)!));
        Check(label + "-reflection-abi", method.ReturnType == typeof(bool) && method.GetParameters().Length == 0);
        bool observed = false;
        try { method.Invoke(null, null); } catch (TargetException) { observed = true; }
        Check(label + "-reflection-null", observed);
        Check(label + "-reflection-public", typeof(DateTimeFormatInfo).GetMethod(Name(index), BindingFlags.Instance | BindingFlags.Public) is null);
    }
    private static void Family(int index, Query direct, Query virtualCall, Query pointer, AddressQuery address, Func<bool> producer)
    {
        string label = Label(index);
        Route(direct, label + "-direct");
        Route(virtualCall, label + "-callvirt");
        Route(pointer, label + "-calli");
        Check(label + "-delegate-invariant", ActualFalse(address(DateTimeFormatInfo.InvariantInfo)()));
        Check(label + "-delegate-clone", ActualFalse(address((DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone())()));
        Check(label + "-delegate-abi", address(DateTimeFormatInfo.InvariantInfo).Method.ReturnType == typeof(bool));
        for (int mode = 0; mode < 3; mode++)
        {
            calls = 0; producerMode = mode;
            bool observed = false;
            try { observed = !producer() && mode == 0; }
            catch (NullReferenceException) { observed = mode == 1; }
            catch (FormatException error) { observed = mode == 2 && error.Message == "receiver producer"; }
            Check(label + "-producer-" + mode, observed && calls == 1);
        }
        ReflectionRow(index);
    }
    public static void RunReflectionChecks()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        for (int index = 0; index < 3; index++) ReflectionRow(index);
        Console.WriteLine("DTFI parser flags reflection end");
    }
    public static void RunMinimal()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Check("minimal-month", !DirectMonth(DateTimeFormatInfo.InvariantInfo));
        Check("minimal-day", !DirectDay(DateTimeFormatInfo.InvariantInfo));
        Check("minimal-adjustment", !DirectAdjustment(DateTimeFormatInfo.InvariantInfo));
    }
    public static void RunPublicOracle()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        bool month = true, day = true;
        foreach (string name in DateTimeFormatInfo.InvariantInfo.MonthNames) month &= name.IndexOf(' ') < 0;
        foreach (string name in DateTimeFormatInfo.InvariantInfo.DayNames) day &= name.IndexOf(' ') < 0;
        Check("public-month-no-spaces", month);
        Check("public-day-no-spaces", day);
    }
    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Family(0, DirectMonth, VirtualMonth, PointerMonth, AddressMonth, ProducerMonth);
        Family(1, DirectDay, VirtualDay, PointerDay, AddressDay, ProducerDay);
        Family(2, DirectAdjustment, VirtualAdjustment, PointerAdjustment, AddressAdjustment, ProducerAdjustment);
        Console.WriteLine("DTFI parser flags end");
    }
    private static void Main() => Run();
}
