using System;
using System.Globalization;
using System.Reflection;

namespace DtfINameArraysSubset;

internal static class Program
{
    private delegate string[] GenitiveQuery(DateTimeFormatInfo receiver, bool abbreviated);
    private delegate string[] LeapQuery(DateTimeFormatInfo receiver);
    private delegate string[] ClosedGenitive(bool abbreviated);
    private delegate string[] ClosedLeap();
    private static string[] DirectGenitive(DateTimeFormatInfo receiver, bool abbreviated) => throw new InvalidOperationException("native fixture not rewritten");
    private static string[] VirtualGenitive(DateTimeFormatInfo receiver, bool abbreviated) => throw new InvalidOperationException("native fixture not rewritten");
    private static string[] PointerGenitive(DateTimeFormatInfo receiver, bool abbreviated) => throw new InvalidOperationException("native fixture not rewritten");
    private static ClosedGenitive AddressGenitive(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static string[] DirectLeap(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static string[] VirtualLeap(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static string[] PointerLeap(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static ClosedLeap AddressLeap(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static string InternalMonth(DateTimeFormatInfo receiver, int month, int style, bool abbreviated) => throw new InvalidOperationException("native fixture not rewritten");
    private static int trace;
    private static int throwingPosition;
    private static bool nullReceiver;
    private static DateTimeFormatInfo Receiver()
    {
        trace = trace * 10 + 1;
        if (throwingPosition == 1) throw new FormatException("producer");
        return nullReceiver ? null! : DateTimeFormatInfo.InvariantInfo;
    }
    private static bool Abbreviated()
    {
        trace = trace * 10 + 2;
        if (throwingPosition == 2) throw new FormatException("producer");
        return true;
    }
    private static string[] Producer() => DirectGenitive(Receiver(), Abbreviated());
    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
    private static void Check(string label, bool value)
    {
        Console.WriteLine("dtfi-name-arrays-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }
    private static bool Shape(string[] value) => value.GetType() == typeof(string[]) && value.Length == 13;
    private static bool Data(string[] value, bool abbreviated)
    {
        string[] expected = abbreviated
            ? new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" }
            : new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" };
        if (!Shape(value)) return false;
        for (int i = 0; i < 13; i++) if (value[i] != expected[i]) return false;
        return true;
    }
    private static void GenitiveRoute(string label, GenitiveQuery query)
    {
        var receiver = DateTimeFormatInfo.InvariantInfo;
        var full = query(receiver, false);
        var abbreviated = query(receiver, true);
        Check(label + "-full", Data(full, false));
        Check(label + "-abbreviated", Data(abbreviated, true));
        Check(label + "-identity", ReferenceEquals(full, query(receiver, false)) && ReferenceEquals(abbreviated, query(receiver, true)) && !ReferenceEquals(full, abbreviated));
        bool observed = false;
        try { query(null!, false); } catch (NullReferenceException) { observed = true; }
        Check(label + "-null", observed);
    }
    private static void LeapRoute(string label, LeapQuery query)
    {
        var receiver = DateTimeFormatInfo.InvariantInfo;
        var names = query(receiver);
        Check(label + "-data", Data(names, false));
        Check(label + "-identity", ReferenceEquals(names, query(receiver)));
        bool observed = false;
        try { query(null!); } catch (NullReferenceException) { observed = true; }
        Check(label + "-null", observed);
    }
    private static MethodInfo Metadata(bool genitive) => typeof(DateTimeFormatInfo).GetMethod(
        genitive ? "InternalGetGenitiveMonthNames" : "InternalGetLeapYearMonthNames",
        BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static void ReflectionRow(bool genitive)
    {
        string label = genitive ? "genitive-reflection" : "leap-reflection";
        var method = Metadata(genitive);
        var receiver = DateTimeFormatInfo.InvariantInfo;
        object[]? args = genitive ? new object[] { false } : null;
        var full = (string[])method.Invoke(receiver, args)!;
        Check(label + "-data", Data(full, false));
        Check(label + "-identity", ReferenceEquals(full, method.Invoke(receiver, args)));
        Check(label + "-abi", method.ReturnType == typeof(string[]) && method.GetParameters().Length == (genitive ? 1 : 0));
        bool observed = false;
        try { method.Invoke(null, args); } catch (TargetException) { observed = true; }
        Check(label + "-null", observed);
        if (genitive) Check(label + "-abbreviated", Data((string[])method.Invoke(receiver, new object[] { true })!, true));
    }
    private static void Ownership()
    {
        var receiver = DateTimeFormatInfo.InvariantInfo;
        var before = (DateTimeFormatInfo)receiver.Clone();
        var full = DirectGenitive(receiver, false);
        var abbreviated = DirectGenitive(receiver, true);
        var after = (DateTimeFormatInfo)receiver.Clone();
        Check("literal-may", ReferenceEquals(full[4], abbreviated[4]));
        Check("literal-empty", ReferenceEquals(full[12], abbreviated[12]) && ReferenceEquals(full[12], string.Empty));
        Check("literal-scalar", ReferenceEquals(receiver.GetMonthName(1), "January"));
        Check("full-leap", ReferenceEquals(full, DirectLeap(receiver)));
        Check("cross-receiver", ReferenceEquals(full, DirectGenitive(DateTimeFormatInfo.InvariantInfo, false)) && ReferenceEquals(abbreviated, DirectGenitive(DateTimeFormatInfo.InvariantInfo, true)));
        Check("clone-before", ReferenceEquals(full, DirectLeap(before)) && ReferenceEquals(abbreviated, DirectGenitive(before, true)));
        Check("clone-after", ReferenceEquals(full, DirectLeap(after)) && ReferenceEquals(abbreviated, DirectGenitive(after, true)));
        Check("readonly", ReferenceEquals(full, DirectLeap(DateTimeFormatInfo.ReadOnly(before))));
        PublicClones(receiver, abbreviated, "public-clones");
    }
    private static void PublicClones(DateTimeFormatInfo receiver, string[] backing, string label)
    {
        var first = receiver.AbbreviatedMonthNames;
        var second = receiver.AbbreviatedMonthNames;
        var genitive = receiver.AbbreviatedMonthGenitiveNames;
        Check(label, Shape(first) && Shape(second) && Shape(genitive) && !ReferenceEquals(first, backing)
            && !ReferenceEquals(first, second) && !ReferenceEquals(first, genitive) && !ReferenceEquals(second, genitive)
            && ReferenceEquals(first[0], backing[0]) && ReferenceEquals(genitive[0], backing[0]));
        first[0] = "public-copy-only";
        Check(label + "-isolated", backing[0] != "public-copy-only" && second[0] != "public-copy-only" && genitive[0] != "public-copy-only");
    }
    private static void Mutation()
    {
        var receiver = DateTimeFormatInfo.InvariantInfo;
        var full = DirectGenitive(receiver, false);
        var abbreviated = DirectGenitive(receiver, true);
        foreach (int index in new[] { 0, 12 })
        {
            string savedFull = full[index], savedAbbreviated = abbreviated[index];
            try
            {
                for (int variant = 0; variant < 2; variant++)
                {
                    full[index] = variant == 0 ? "full-mutated" : null!;
                    abbreviated[index] = variant == 0 ? "short-mutated" : null!;
                    int month = index + 1;
                    string label = "mutation-" + index + "-" + variant;
                    Check(label + "-private", ReferenceEquals(DirectGenitive(receiver, false)[index], full[index]) && ReferenceEquals(DirectLeap(receiver)[index], full[index]) && ReferenceEquals(DirectGenitive(receiver, true)[index], abbreviated[index]));
                    Check(label + "-public-clones", ReferenceEquals(receiver.AbbreviatedMonthNames[index], abbreviated[index]) && ReferenceEquals(receiver.AbbreviatedMonthGenitiveNames[index], abbreviated[index]));
                    Check(label + "-scalars", ReferenceEquals(receiver.GetMonthName(month), full[index]) && ReferenceEquals(receiver.GetAbbreviatedMonthName(month), abbreviated[index]));
                    bool internalValues = true;
                    foreach (int style in new[] { 0, 1, 2, 3, -1, int.MinValue, int.MaxValue })
                    {
                        internalValues &= ReferenceEquals(InternalMonth(receiver, month, style, false), full[index]);
                        internalValues &= ReferenceEquals(InternalMonth(receiver, month, style, true), style == 2 ? full[index] : abbreviated[index]);
                    }
                    Check(label + "-internal", internalValues);
                }
            }
            finally
            {
                full[index] = savedFull;
                abbreviated[index] = savedAbbreviated;
            }
        }
        Check("restored", Data(full, false) && Data(abbreviated, true));
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void InstallLifetimeMarker()
    {
        var receiver = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        var value = new string(new[] { '\u03a9', '\u4e2d', 'x' });
        DirectGenitive(receiver, true)[2] = value;
    }
    private static void Lifetime()
    {
        string saved = DirectGenitive(DateTimeFormatInfo.InvariantInfo, true)[2];
        try
        {
            InstallLifetimeMarker();
            GC.Collect();
            for (int i = 0; i < 32; i++) _ = new byte[1024];
            GC.Collect();
            string value = DirectGenitive(DateTimeFormatInfo.InvariantInfo, true)[2];
            Check("gc-root", value.Length == 3 && value[0] == '\u03a9' && value[1] == '\u4e2d' && value[2] == 'x');
        }
        finally { DirectGenitive(DateTimeFormatInfo.InvariantInfo, true)[2] = saved; }
    }
    public static void RunReflectionChecks()
    {
        PinCulture();
        ReflectionRow(true);
        ReflectionRow(false);
        Console.WriteLine("DTFI name arrays reflection end");
    }
    public static void RunMinimal()
    {
        PinCulture();
        var full = DirectGenitive(DateTimeFormatInfo.InvariantInfo, false);
        var abbreviated = DirectGenitive(DateTimeFormatInfo.InvariantInfo, true);
        Check("minimal-shape", Shape(full) && Shape(abbreviated));
        Check("minimal-alias", ReferenceEquals(full, DirectLeap(DateTimeFormatInfo.InvariantInfo)) && !ReferenceEquals(full, abbreviated));
    }
    // Compile-only unsupported setter controls; never execute as native or CLR oracle.
    public static void RunSetterMonth()
    {
        PinCulture();
        var receiver = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        receiver.AbbreviatedMonthNames = receiver.AbbreviatedMonthNames;
    }
    public static void RunSetterGenitive()
    {
        PinCulture();
        var receiver = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        receiver.AbbreviatedMonthGenitiveNames = receiver.AbbreviatedMonthGenitiveNames;
    }
    public static void RunPublicOracle()
    {
        PinCulture();
        var receiver = DateTimeFormatInfo.InvariantInfo;
        Check("oracle-full", Data(receiver.MonthNames, false) && Data(receiver.MonthGenitiveNames, false));
        Check("oracle-short", Data(receiver.AbbreviatedMonthNames, true) && Data(receiver.AbbreviatedMonthGenitiveNames, true));
        var saved = receiver.AbbreviatedMonthNames;
        saved[0] = "not-shared";
        Check("oracle-clone", receiver.AbbreviatedMonthNames[0] == "Jan" && receiver.AbbreviatedMonthGenitiveNames[0] == "Jan");
    }
    public static void Run()
    {
        PinCulture();
        Ownership();
        GenitiveRoute("genitive-direct", DirectGenitive);
        GenitiveRoute("genitive-callvirt", VirtualGenitive);
        GenitiveRoute("genitive-calli", PointerGenitive);
        LeapRoute("leap-direct", DirectLeap);
        LeapRoute("leap-callvirt", VirtualLeap);
        LeapRoute("leap-calli", PointerLeap);
        var receiver = DateTimeFormatInfo.InvariantInfo;
        Check("delegate-genitive", Data(AddressGenitive(receiver)(false), false) && Data(AddressGenitive(receiver)(true), true));
        Check("delegate-leap", Data(AddressLeap(receiver)(), false));
        Check("delegate-abi", AddressGenitive(receiver).Method.ReturnType == typeof(string[]) && AddressLeap(receiver).Method.ReturnType == typeof(string[]));
        for (int mode = 0; mode < 4; mode++)
        {
            trace = 0; throwingPosition = mode >= 2 ? mode - 1 : 0; nullReceiver = mode == 1;
            bool observed = false;
            try { observed = Shape(Producer()) && mode == 0; }
            catch (NullReferenceException) { observed = mode == 1; }
            catch (FormatException error) { observed = mode >= 2 && error.Message == "producer"; }
            Check("producer-" + mode, observed && trace == (mode == 2 ? 1 : 12));
        }
        ReflectionRow(true);
        ReflectionRow(false);
        Mutation();
        Lifetime();
        Console.WriteLine("DTFI name arrays end");
    }
    private static void Main() => Run();
}
