using System;
using System.Globalization;
using System.Reflection;

// SUBJECT: reflective calls that reach CoreLib overrides the image stripped. They
// run in a program of their own: code that compiles one of those overrides, as a
// DateTime format compiles GregorianCalendar's, turns its refusal into a call.
// With DN2CPP_STRIPPED_OVERRIDES=1 (dn2cpp only) each call reports the stripped
// body; without it the program prints the rows the calls go through.
namespace StrippedOverrideRefusals;

static class Program
{
    private static void Main()
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        if (Environment.GetEnvironmentVariable("DN2CPP_STRIPPED_OVERRIDES") == "1")
        {
            RunStripped();
            return;
        }
        Type calendarType = typeof(Calendar);
        Type convertibleType = typeof(IConvertible);
        Console.WriteLine("rows: " + Describe(Named(calendarType, "AddYears")) + "/"
            + Describe(Named(calendarType, "GetDayOfMonth")) + "/"
            + Describe(Named(convertibleType, "ToDateTime")) + "/"
            + Describe(Named(convertibleType, "ToInt32")) + "/"
            + Describe(typeof(Program).GetMethod(nameof(StrippedDay), BindingFlags.NonPublic | BindingFlags.Static)!));
        Console.WriteLine("stripped override rows end");
    }

    private static string Describe(MethodInfo method) => method.DeclaringType!.Name + "." + method.Name;

    private static void Fault(string label, Action invoke)
    {
        try
        {
            invoke();
            Console.WriteLine($"{label}: no fault");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label}: {ex.GetType().Name} 0x{ex.HResult:X8} {ex.Message}");
        }
    }

    private static MethodInfo Named(Type type, string name)
    {
        foreach (var method in type.GetMethods())
            if (method.Name == name)
                return method;
        throw new MissingMethodException(type.Name, name);
    }

    private sealed class StrippedDayText
    {
        public override string ToString() => StrippedDay(new GregorianCalendar(), new DateTime(2020, 2, 29)).ToString();
    }

    private sealed class StrippedDayHolder
    {
        public readonly int Day = StrippedDay(new GregorianCalendar(), new DateTime(2020, 2, 29));
    }

    // Set before a nested call runs: a literal after typeof would root the override.
    private static MethodInfo? s_strippedDayRow;

    private static int StrippedDay(Calendar calendar, DateTime day) =>
        (int)s_strippedDayRow!.Invoke(calendar, new object[] { day })!;

    private static void FaultInner(string label, Action invoke)
    {
        try
        {
            invoke();
            Console.WriteLine($"{label}: no fault");
        }
        catch (Exception ex)
        {
            Exception? inner = ex.InnerException;
            Console.WriteLine($"{label}: {ex.GetType().Name} 0x{ex.HResult:X8} "
                + (inner is null ? ex.Message : $"inner {inner.GetType().Name} 0x{inner.HResult:X8} {inner.Message}"));
        }
    }

    // dn2cpp only: GetMethods names no member, so these overrides are compiled for
    // no other reason and stay stripped. Each slot shape reports that as a
    // catchable NotSupportedException; .NET runs the bodies. A refusal a nested
    // reflective call raises is a fault of the outer call's target.
    internal static void RunStripped()
    {
        Type calendarType = typeof(Calendar);
        Type convertibleType = typeof(IConvertible);
        var calendar = new GregorianCalendar();
        var leap = new DateTime(2020, 2, 29);
        Fault("stripped struct-returning slot", () => Named(calendarType, "AddYears").Invoke(calendar, new object[] { leap, 1 }));
        Fault("stripped slot", () => Named(calendarType, "GetDayOfMonth").Invoke(calendar, new object[] { leap }));
        Fault("stripped interface struct-returning slot", () => Named(convertibleType, "ToDateTime")
            .Invoke(DBNull.Value, new object?[] { null }));
        Fault("stripped interface slot", () => Named(convertibleType, "ToInt32").Invoke(DBNull.Value, new object?[] { null }));
        var day = (Func<DateTime, int>)Delegate.CreateDelegate(typeof(Func<DateTime, int>), calendar,
            Named(calendarType, "GetDayOfMonth"));
        Fault("stripped slot, delegate", () => day(leap));
        Console.WriteLine("stripped end");
        s_strippedDayRow = Named(calendarType, "GetDayOfMonth");
        MethodInfo nested = typeof(Program).GetMethod(nameof(StrippedDay), BindingFlags.NonPublic | BindingFlags.Static)!;
        FaultInner("stripped slot, nested invoke", () => nested.Invoke(null, new object[] { calendar, leap }));
        FaultInner("stripped slot, nested invoke unwrapped", () => nested.Invoke(null,
            BindingFlags.DoNotWrapExceptions, null, new object[] { calendar, leap }, null));
        FaultInner("stripped slot, nested object virtual", () => typeof(object).GetMethod("ToString")!
            .Invoke(new StrippedDayText(), null));
        FaultInner("stripped slot, nested constructor", () => typeof(StrippedDayHolder)
            .GetConstructor(Type.EmptyTypes)!.Invoke(null));
        Console.WriteLine("stripped nested end");
    }
}
