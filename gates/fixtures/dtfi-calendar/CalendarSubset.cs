using System;
using System.Globalization;
using System.Reflection;

namespace DtfICalendarSubset;

internal static class Program
{
    private const string Message = "DateTimeFormatInfo.Calendar is not supported";
    private delegate Calendar Query(DateTimeFormatInfo value);
    private static Calendar Direct(DateTimeFormatInfo value) => value.Calendar;
    private static Calendar Virtual(DateTimeFormatInfo value) => value.Calendar;
    private static Calendar Pointer(DateTimeFormatInfo value) => value.Calendar;
    private static Func<Calendar> Address(DateTimeFormatInfo value) => () => value.Calendar;
    private static Calendar DelegateRoute(DateTimeFormatInfo value) => Address(value)();
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod("get_Calendar")!;
    private static Calendar Reflect(DateTimeFormatInfo value) => (Calendar)Metadata().Invoke(value, null)!;

    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    private static void Check(string name, bool result)
    {
        Console.WriteLine("dtfi-calendar-" + name + ":" + result);
        if (!result) throw new InvalidOperationException(name);
    }

    private static void Refuses(Query query, string name, bool reflection)
    {
        Exception observed = null!;
        try { _ = query(DateTimeFormatInfo.InvariantInfo); }
        catch (Exception error) { observed = error; }
        if (reflection)
        {
            Check(name + "-wrapped", observed is TargetInvocationException);
            observed = ((TargetInvocationException)observed).InnerException!;
        }
        Check(name + "-refused", observed is PlatformNotSupportedException && observed.Message == Message);
    }

    private static void NullFirst(Query query, string name)
    {
        bool observed = false;
        try { _ = query(null!); }
        catch (NullReferenceException) { observed = true; }
        Check(name + "-null", observed);
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        Refuses(Reflect, "reflection", true);
        Exception observed = null!;
        try { _ = Reflect(null!); }
        catch (Exception error) { observed = error; }
        Check("reflection-null-target", observed is TargetException);
        Check("reflection-return-abi", Metadata().ReturnType == typeof(Calendar));
        Console.WriteLine("dtfi-calendar-reflection-end");
    }

    public static void RunDirectChecks()
    {
        PinCulture();
        Refuses(Direct, "direct", false);
        NullFirst(Direct, "direct");
        Console.WriteLine("dtfi-calendar-direct-end");
    }

    public static void Run()
    {
        PinCulture();
        Console.WriteLine("== DTFI Calendar native capability ==");
        RunDirectChecks();
        Refuses(Virtual, "callvirt", false);
        NullFirst(Virtual, "callvirt");
        Refuses(Pointer, "calli", false);
        NullFirst(Pointer, "calli");
        Refuses(DelegateRoute, "delegate", false);
        Check("delegate-return-abi", Address(DateTimeFormatInfo.InvariantInfo).Method.ReturnType == typeof(Calendar));
        RunReflectionChecks();
        Exception neighbor = null!;
        try { _ = CultureInfo.InvariantCulture.Calendar; }
        catch (Exception error) { neighbor = error; }
        Check("culture-calendar-neighbor", neighbor is PlatformNotSupportedException
            && neighbor.Message == "CultureInfo.Calendar: dn2cpp does not construct a Calendar object (globalization is invariant-only); the GregorianCalendar members are unavailable");
        Console.WriteLine("DTFI Calendar native capability end");
    }

    // Safe public CLR oracle only; its successful calendar objects are NOT native policy.
    public static void RunPublicOracle()
    {
        PinCulture();
        var info = DateTimeFormatInfo.InvariantInfo;
        var calendar = info.Calendar;
        Check("public-gregorian", calendar is GregorianCalendar);
        Check("public-readonly", calendar.IsReadOnly);
        Check("public-identity", ReferenceEquals(calendar, info.Calendar));
        var clone = (DateTimeFormatInfo)info.Clone();
        Check("public-clone", clone.Calendar is GregorianCalendar && !clone.Calendar.IsReadOnly
            && !ReferenceEquals(calendar, clone.Calendar));
        Check("public-reflection", Reflect(info) is GregorianCalendar);
        NullFirst(Direct, "public");
        Console.WriteLine("DTFI Calendar public oracle end");
    }
}
