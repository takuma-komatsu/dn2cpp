using System;
using System.Globalization;
using System.Reflection;

// SUBJECT: reflective calls that reach CoreLib overrides the image stripped. They
// run in a program of their own: code that compiles one of those overrides, as a
// DateTime format compiles GregorianCalendar's, turns its refusal into a call.
// With DN2CPP_STRIPPED_OVERRIDES=1 (dn2cpp only) each call reports the stripped
// body; without it the program prints the rows the calls go through.
namespace StrippedOverrideRefusals;

public static class Program
{
    private static void Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        if (args.Length != 0 && args[0] == "class-remedies")
        {
            RunClassRemedies();
            return;
        }
        if (args.Length != 0 && args[0] == "interface-remedies")
        {
            RunInterfaceRemedies();
            return;
        }
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

        // A call site compiles each allocated receiver's override of a generic virtual
        // method, so a late-bound call through a base override's row runs the
        // receiver's override instead of refusing it.
        GenericSlotBase[] receivers = { new GenericSlotMiddle(), new GenericSlotLeaf() };
        string direct = "";
        foreach (GenericSlotBase receiver in receivers)
            direct += receiver.Render<int>() + ";";
        Console.WriteLine("retained generic override, direct: " + direct);
        Console.WriteLine("retained generic override, base row: " + InvokeBaseRow(receivers[1]));
        // The leaf override's only call site is in a boxed struct's Equals, which is
        // reached after the base override's row has a late-bound dispatcher.
        var late = new LateSlotLeaf();
        Console.WriteLine("retained late generic override, base call: " + late.CallBase());
        object first = new LateSlotProbe(late), second = new LateSlotProbe(late);
        Console.WriteLine("retained late generic override, equal: " + first.Equals(second));
        Console.WriteLine("retained late generic override, base row: " + InvokeBaseRow(late));
        Console.WriteLine("retained generic overrides end");
        if (args.Length != 0 && args[0] == "before-interface-remedy-rows")
            return;
        Console.WriteLine("== interface remedy rows ==");
        Console.WriteLine("interface remedy control: " + InterfaceRemedyControl.Selected());
        Console.WriteLine("interface remedy rows end");
    }

    public static void RunClassRemedies()
    {
        Console.WriteLine("== stripped class remedies ==");
        object inherited = new ClassRemedyLeaf();
        object decoy = new ClassRemedyDecoyLeaf();
        ClassRemedy("class remedy inherited", typeof(ClassRemedyBase).GetMethod("M")!, inherited);
        ClassRemedy("class remedy decoy", typeof(ClassRemedyBase).GetMethod("M")!, decoy);
        Console.WriteLine("stripped class remedies end");
    }

    // The receiver and row cross a summary boundary before invocation.
    private static void ClassRemedy(string label, MethodInfo row, object receiver)
    {
        try
        {
            Console.WriteLine(label + ": " + row.Invoke(receiver, null));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label}: {ex.GetType().Name} 0x{ex.HResult:X8} {ex.Message}");
        }
    }

    public static void RunInterfaceRemedies()
    {
        Console.WriteLine("== stripped interface remedies ==");
        object receiver = new InterfaceRemedyImplementation();
        InterfaceRemedy("interface remedy alpha", typeof(IInterfaceRemedyAlpha).GetMethod("M")!, receiver);
        InterfaceRemedy("interface remedy beta", typeof(IInterfaceRemedyBeta).GetMethod("M")!, receiver);
        Console.WriteLine("stripped interface remedies end");
    }

    // The row and receiver arrive through parameters: enumeration retains the
    // interface declarations, but this call cannot admit a concrete implementation.
    private static void InterfaceRemedy(string label, MethodInfo row, object receiver)
    {
        try
        {
            Console.WriteLine(label + ": " + row.Invoke(receiver, null));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label}: {ex.GetType().Name} 0x{ex.HResult:X8} {ex.Message}");
        }
    }

    // The receiver arrives as a parameter, so admission cannot name the row the call selects.
    private static object? InvokeBaseRow(object receiver) =>
        receiver.GetType().BaseType!.GetMethod("Render")!.MakeGenericMethod(typeof(int)).Invoke(receiver, null);

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

class GenericSlotBase
{
    public virtual string Render<T>() => "base " + typeof(T).Name;
}

class GenericSlotMiddle : GenericSlotBase
{
    public override string Render<T>() => "middle " + typeof(T).Name;
}

class GenericSlotLeaf : GenericSlotMiddle
{
    public override string Render<T>() => "leaf " + typeof(T).Name;
}

class LateSlotBase
{
    public virtual string Render<T>() => "late base " + typeof(T).Name;
}

class LateSlotMiddle : LateSlotBase
{
    public override string Render<T>() => "late middle " + typeof(T).Name;
}

class LateSlotLeaf : LateSlotMiddle
{
    public override string Render<T>() => "late leaf " + typeof(T).Name;

    public string CallBase() => base.Render<int>();
}

readonly struct LateSlotProbe
{
    private readonly LateSlotBase _slot;

    public LateSlotProbe(LateSlotBase slot) => _slot = slot;

    public override bool Equals(object? other) =>
        other is LateSlotProbe probe && probe._slot.Render<int>() == _slot.Render<int>();

    public override int GetHashCode() => 0;
}

interface IInterfaceRemedyAlpha
{
    int M();
}

interface IInterfaceRemedyBeta
{
    int M();
}

sealed class InterfaceRemedyImplementation : IInterfaceRemedyAlpha, IInterfaceRemedyBeta
{
    public int M(int value) => value + 101;

    public static int M() => 103;

    int IInterfaceRemedyAlpha.M() => 17;

    int IInterfaceRemedyBeta.M() => 29;
}

static class InterfaceRemedyControl
{
    public static int Selected() => 41;
}

abstract class ClassRemedyBase
{
    public abstract int M();
}

class ClassRemedyMiddle : ClassRemedyBase
{
    public override int M() => 7;

    public int Unrelated() => 107;
}

sealed class ClassRemedyLeaf : ClassRemedyMiddle
{
}

sealed class ClassRemedyDecoyLeaf : ClassRemedyMiddle
{
    public int M(int value) => value + 109;
}
