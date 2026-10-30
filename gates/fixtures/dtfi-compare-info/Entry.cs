using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DtfICompareInfoSubset;

internal static class Program
{
    private delegate CompareInfo DtfIQuery(DateTimeFormatInfo receiver);
    private delegate CompareInfo CultureQuery(CultureInfo receiver);
    private delegate CompareInfo ClosedQuery();
    private static CompareInfo DirectDtfI(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static CompareInfo VirtualDtfI(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static CompareInfo PointerDtfI(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static ClosedQuery AddressDtfI(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static CompareInfo DirectCulture(CultureInfo receiver) => receiver.CompareInfo;
    private static CompareInfo VirtualCulture(CultureInfo receiver) => receiver.CompareInfo;
    private static CompareInfo PointerCulture(CultureInfo receiver) => receiver.CompareInfo;
    private static ClosedQuery AddressCulture(CultureInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static ClosedQuery VirtualAddressCulture(CultureInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private sealed class OverrideCulture : CultureInfo
    {
        public OverrideCulture() : base("") { }
        public override CompareInfo CompareInfo
        {
            get { overrideCalls++; return null!; }
        }
    }
    private static int overrideCalls;
    private static OverrideCulture MakeOverride() => new OverrideCulture();
    private static MethodInfo Metadata(bool dtfi) => (dtfi ? typeof(DateTimeFormatInfo) : typeof(CultureInfo)).GetMethod(
        "get_CompareInfo", BindingFlags.Instance | (dtfi ? BindingFlags.NonPublic : BindingFlags.Public))!;
    private static int producerMode;
    private static int producerCalls;
    private static DateTimeFormatInfo DtfIReceiver()
    {
        producerCalls++;
        if (producerMode == 2) throw new FormatException("receiver producer");
        return producerMode == 1 ? null! : DateTimeFormatInfo.InvariantInfo;
    }
    private static CultureInfo CultureReceiver()
    {
        producerCalls++;
        if (producerMode == 2) throw new FormatException("receiver producer");
        return producerMode == 1 ? null! : CultureInfo.InvariantCulture;
    }
    private static CompareInfo ProducerDtfI() => DirectDtfI(DtfIReceiver());
    private static CompareInfo ProducerCulture() => DirectCulture(CultureReceiver());
    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
    private static void Check(string label, bool value)
    {
        Console.WriteLine("dtfi-compare-info-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }
    private static bool Initialized(CompareInfo value) => value is not null && value.GetType() == typeof(CompareInfo) && value.Name == string.Empty;
    private static bool Comparisons(CompareInfo value) => value.Compare("a", "b", CompareOptions.Ordinal) < 0
        && value.Compare("A", "a", CompareOptions.OrdinalIgnoreCase) == 0
        && value.IndexOf("abc", "b", CompareOptions.Ordinal) == 1
        && value.IsPrefix("Alpha", "al", CompareOptions.OrdinalIgnoreCase);
    private static void DtfIRoute(string label, DtfIQuery query)
    {
        var receiver = DateTimeFormatInfo.InvariantInfo;
        var first = query(receiver);
        var second = query(receiver);
        Check(label + "-initialized", Initialized(first));
        Check(label + "-identity", ReferenceEquals(first, second));
        Check(label + "-equals-hash", first.Equals(second) && second.Equals(first) && first.GetHashCode() == second.GetHashCode());
        Check(label + "-compare", Comparisons(first));
        bool observed = false;
        try { query(null!); } catch (NullReferenceException) { observed = true; }
        Check(label + "-null", observed);
    }
    private static void CultureRoute(string label, CultureQuery query)
    {
        var receiver = CultureInfo.InvariantCulture;
        var first = query(receiver);
        var second = query(receiver);
        Check(label + "-initialized", Initialized(first));
        Check(label + "-identity", ReferenceEquals(first, second));
        Check(label + "-equals-hash", first.Equals(second) && second.Equals(first) && first.GetHashCode() == second.GetHashCode());
        Check(label + "-compare", Comparisons(first));
        bool observed = false;
        try { query(null!); } catch (NullReferenceException) { observed = true; }
        Check(label + "-null", observed);
    }
    private static void ReflectionRow(bool dtfi)
    {
        string label = dtfi ? "dtfi-reflection" : "culture-reflection";
        var method = Metadata(dtfi);
        object receiver = dtfi ? DateTimeFormatInfo.InvariantInfo : CultureInfo.InvariantCulture;
        var first = (CompareInfo)method.Invoke(receiver, null)!;
        var second = (CompareInfo)method.Invoke(receiver, null)!;
        Check(label + "-initialized", Initialized(first));
        Check(label + "-identity", ReferenceEquals(first, second));
        Check(label + "-equals-hash", first.Equals(second) && first.GetHashCode() == second.GetHashCode());
        Check(label + "-compare", Comparisons(first));
        Check(label + "-abi", method.ReturnType == typeof(CompareInfo) && method.GetParameters().Length == 0);
        bool observed = false;
        try { method.Invoke(null, null); } catch (TargetException) { observed = true; }
        Check(label + "-null", observed);
    }
    private static void Producers()
    {
        foreach (bool dtfi in new[] { true, false })
        for (int mode = 0; mode < 3; mode++)
        {
            producerCalls = 0;
            producerMode = mode;
            bool observed = false;
            try { observed = Initialized(dtfi ? ProducerDtfI() : ProducerCulture()) && mode == 0; }
            catch (NullReferenceException) { observed = mode == 1; }
            catch (FormatException error) { observed = mode == 2 && error.Message == "receiver producer"; }
            Check((dtfi ? "dtfi" : "culture") + "-producer-" + mode, observed && producerCalls == 1);
        }
    }
    private static void OverrideRoutes()
    {
        CultureInfo receiver = MakeOverride();
        overrideCalls = 0;
        Check("override-direct-base", Initialized(DirectCulture(receiver)) && overrideCalls == 0);
        Check("override-base-address", Initialized(AddressCulture(receiver)()) && overrideCalls == 0);
        Check("override-callvirt", VirtualCulture(receiver) is null && overrideCalls == 1);
        Check("override-virtual-address", VirtualAddressCulture(receiver)() is null && overrideCalls == 2);
        Check("override-reflection", Metadata(false).Invoke(receiver, null) is null && overrideCalls == 3);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DropReferences()
    {
        var receiver = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        var value = DirectDtfI(receiver);
        if (!Initialized(value) || !Comparisons(value)) throw new InvalidOperationException("cold value");
        return RuntimeHelpers.GetHashCode(value);
    }
    private static void Lifetime()
    {
        int identity = DropReferences();
        GC.Collect();
        for (int i = 0; i < 32; i++) _ = new byte[1024];
        GC.Collect();
        var first = DirectDtfI(DateTimeFormatInfo.InvariantInfo);
        var second = DirectCulture(CultureInfo.InvariantCulture);
        Check("gc-initialized", Initialized(first) && Comparisons(first));
        Check("gc-identity", identity == RuntimeHelpers.GetHashCode(first) && ReferenceEquals(first, second));
    }
    // Compile-only native subclass boundary probe; never part of the positive runtime entry.
    public static void RunOverridePolicy()
    {
        PinCulture();
        OverrideRoutes();
    }
    public static void RunSubclassVirtual()
    {
        PinCulture();
        _ = VirtualCulture(CultureInfo.InvariantCulture);
        _ = VirtualCulture(MakeOverride());
    }
    public static void RunSubclassAddress()
    {
        PinCulture();
        _ = VirtualAddressCulture(CultureInfo.InvariantCulture);
        _ = VirtualAddressCulture(MakeOverride());
    }
    public static void RunSubclassReflection()
    {
        PinCulture();
        var method = Metadata(false);
        _ = method.Invoke(CultureInfo.InvariantCulture, null);
        _ = method.Invoke(MakeOverride(), null);
    }
    public static void RunSubclassBase()
    {
        PinCulture();
        overrideCalls = 0;
        Check("subclass-explicit-base", Initialized(DirectCulture(MakeOverride())) && overrideCalls == 0);
    }
    public static void RunSubclassLookupOnly()
    {
        PinCulture();
        var method = Metadata(false);
        var receiver = MakeOverride();
        Check("subclass-lookup-only", method is not null && receiver is not null);
    }
    public static void RunSubclassLoaded()
    {
        PinCulture();
        Check("subclass-loaded", typeof(OverrideCulture).BaseType == typeof(CultureInfo)
            && Initialized(VirtualCulture(CultureInfo.InvariantCulture)));
    }
    public static void RunReflectionChecks()
    {
        PinCulture();
        ReflectionRow(true);
        ReflectionRow(false);
        Console.WriteLine("DTFI CompareInfo reflection end");
    }
    public static void RunMinimalDtfI()
    {
        PinCulture();
        var value = DirectDtfI(DateTimeFormatInfo.InvariantInfo);
        Check("minimal-dtfi", value is not null && value.Name.Length == 0);
    }
    public static void RunMinimalCulture()
    {
        PinCulture();
        var value = DirectCulture(CultureInfo.InvariantCulture);
        Check("minimal-culture", value is not null && value.Name.Length == 0);
    }
    public static void RunPublicOracle()
    {
        PinCulture();
        CultureRoute("public", VirtualCulture);
        var receiver = new OverrideCulture();
        overrideCalls = 0;
        Check("public-override", receiver.CompareInfo is null && overrideCalls == 1);
    }
    public static void Run()
    {
        PinCulture();
        DtfIRoute("dtfi-direct", DirectDtfI);
        DtfIRoute("dtfi-callvirt", VirtualDtfI);
        DtfIRoute("dtfi-calli", PointerDtfI);
        CultureRoute("culture-direct", DirectCulture);
        CultureRoute("culture-callvirt", VirtualCulture);
        CultureRoute("culture-calli", PointerCulture);
        var dtfi = DateTimeFormatInfo.InvariantInfo;
        var culture = CultureInfo.InvariantCulture;
        Check("delegate-dtfi", Initialized(AddressDtfI(dtfi)()));
        Check("delegate-culture", Initialized(AddressCulture(culture)()));
        Check("delegate-abi", AddressDtfI(dtfi).Method.ReturnType == typeof(CompareInfo) && AddressCulture(culture).Method.ReturnType == typeof(CompareInfo));
        var shared = DirectDtfI(dtfi);
        Check("cross-getter", ReferenceEquals(shared, DirectCulture(culture)));
        Check("cross-receiver", ReferenceEquals(shared, DirectDtfI(DateTimeFormatInfo.InvariantInfo)) && ReferenceEquals(shared, DirectCulture((CultureInfo)culture.Clone())));
        Check("dtfi-clone-readonly", ReferenceEquals(shared, DirectDtfI((DateTimeFormatInfo)dtfi.Clone())) && ReferenceEquals(shared, DirectDtfI(DateTimeFormatInfo.ReadOnly((DateTimeFormatInfo)dtfi.Clone()))));
        ReflectionRow(true);
        ReflectionRow(false);
        Producers();
        Lifetime();
        Console.WriteLine("DTFI CompareInfo end");
    }
    private static void Main() => Run();
}
