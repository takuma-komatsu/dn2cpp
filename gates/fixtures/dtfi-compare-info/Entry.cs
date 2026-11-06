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
    // Rewritten only for native input; the public CLR oracle never calls these private targets.
    private static CompareInfo CreateCompareInfo(CultureInfo culture) => throw new InvalidOperationException("native fixture not rewritten");
    private static void CallCtor(CompareInfo receiver, CultureInfo culture) => throw new InvalidOperationException("native fixture not rewritten");
    private static void DirectInitSort(CompareInfo receiver, CultureInfo culture) => throw new InvalidOperationException("native fixture not rewritten");
    private static void PointerInitSort(CompareInfo receiver, CultureInfo culture) => throw new InvalidOperationException("native fixture not rewritten");
    private static void SeedCompareInfo(CompareInfo receiver, string name, string sortName, IntPtr handle) => throw new InvalidOperationException("native fixture not rewritten");
    private static string ReadLegacyName(CompareInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static string ReadSortName(CompareInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool ReadAsciiOrdinal(CompareInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static IntPtr ReadSortHandle(CompareInfo receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private sealed class OverrideCulture : CultureInfo
    {
        public OverrideCulture() : base("") { }
        public override CompareInfo CompareInfo
        {
            get { overrideCalls++; return null!; }
        }
    }
    private sealed class InitializerCulture : CultureInfo
    {
        public InitializerCulture() : base("") { }
    }
    private static InitializerCulture MakeInitializerCulture() => new InitializerCulture();
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
    private static bool InitializerState(CompareInfo value, string name, string sortName, IntPtr handle, bool ascii) =>
        value.GetType() == typeof(CompareInfo) && ReadLegacyName(value) == name && ReadSortName(value) == sortName
        && ReadSortHandle(value) == handle && ReadAsciiOrdinal(value) == ascii;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CompareInfo InitializerForCollection(bool initialize)
    {
        var value = CreateCompareInfo(new CultureInfo(new string(new[] { 'd', 'e', '-', 'D', 'E', '_', 'p', 'h', 'o', 'n', 'e', 'b' })));
        if (initialize)
        {
            SeedCompareInfo(value, new string(new[] { 's', 'e', 'e', 'd' }), string.Empty, new IntPtr(0x1234));
            DirectInitSort(value, new CultureInfo(new string(new[] { 'z', 'h', '-', 'C', 'H', 'T' })));
        }
        return value;
    }
    private static string[] InitializerCultureNames() => new[]
    {
        "", "en-US", "de-DE_phoneb", "zh-CHS", "zh-CHT", "zh-CHS_phoneb", "zh-CHT_phoneb"
    };
    private static CultureInfo? ObserveInitializerCulture(int index, string name)
    {
        CultureInfo culture;
        try { culture = new CultureInfo(name); }
        catch (CultureNotFoundException error)
        {
            bool permitted = (index == 5 && name == "zh-CHS_phoneb")
                || (index == 6 && name == "zh-CHT_phoneb") || (index == 7 && name == "en/US");
            if (!permitted || error.GetType() != typeof(CultureNotFoundException)
                || error.ParamName != "name" || error.InvalidCultureName != name) throw;
            Console.WriteLine("initializer-culture:" + index + "|" + name
                + "|rejected|System.Globalization.CultureNotFoundException|name|" + error.InvalidCultureName);
            return null;
        }
        if (index == 7) throw new InvalidOperationException("malformed initializer culture was accepted");
        Console.WriteLine("initializer-culture:" + index + "|" + name + "|accepted");
        return culture;
    }
    public static void RunInitializerCultureOracle()
    {
        PinCulture();
        string[] names = InitializerCultureNames();
        for (int i = 0; i < names.Length; i++) _ = ObserveInitializerCulture(i, names[i]);
        _ = ObserveInitializerCulture(7, "en/US");
    }
    private static void InitializerChecks()
    {
        string[] names = InitializerCultureNames();
        for (int i = 0; i < names.Length; i++)
        {
            var culture = ObserveInitializerCulture(i, names[i]);
            if (culture is null) continue;
            string sortName = i == 3 ? "zh-Hans" : i == 4 ? "zh-Hant" : names[i];
            var value = CreateCompareInfo(culture);
            Check("initializer-newobj-" + i, InitializerState(value, names[i], sortName, IntPtr.Zero, true));
            SeedCompareInfo(value, "before ctor", "old sort", new IntPtr(0x1234));
            CallCtor(value, culture);
            Check("initializer-call-ctor-" + i, InitializerState(value, names[i], sortName, new IntPtr(0x1234), true));
            SeedCompareInfo(value, "keep name", "old sort", new IntPtr(0x5678));
            Check("initializer-seed-" + i, InitializerState(value, "keep name", "old sort", new IntPtr(0x5678), false));
            DirectInitSort(value, culture);
            Check("initializer-direct-" + i, InitializerState(value, "keep name", sortName, new IntPtr(0x5678), true));
            SeedCompareInfo(value, "keep address name", "old sort", new IntPtr(0x4321));
            PointerInitSort(value, culture);
            Check("initializer-calli-" + i, InitializerState(value, "keep address name", sortName, new IntPtr(0x4321), true));
        }
        _ = ObserveInitializerCulture(7, "en/US");
        bool observed = false;
        try { CreateCompareInfo(null!); } catch (NullReferenceException) { observed = true; }
        Check("initializer-newobj-null", observed);
        var receiver = CreateCompareInfo(CultureInfo.InvariantCulture);
        for (int route = 0; route < 3; route++)
        {
            SeedCompareInfo(receiver, "unchanged name", "unchanged sort", new IntPtr(0x1234));
            observed = false;
            try
            {
                if (route == 0) CallCtor(receiver, null!);
                else if (route == 1) DirectInitSort(receiver, null!);
                else PointerInitSort(receiver, null!);
            }
            catch (NullReferenceException) { observed = true; }
            Check("initializer-null-culture-" + route, observed
                && InitializerState(receiver, "unchanged name", "unchanged sort", new IntPtr(0x1234), false));
            observed = false;
            try
            {
                if (route == 0) CallCtor(null!, CultureInfo.InvariantCulture);
                else if (route == 1) DirectInitSort(null!, CultureInfo.InvariantCulture);
                else PointerInitSort(null!, CultureInfo.InvariantCulture);
            }
            catch (NullReferenceException) { observed = true; }
            Check("initializer-null-receiver-" + route, observed);
        }
        var constructed = InitializerForCollection(false);
        var initialized = InitializerForCollection(true);
        GC.Collect();
        for (int i = 0; i < 32; i++) _ = new byte[1024];
        GC.Collect();
        Check("initializer-gc-ctor", InitializerState(constructed, "de-DE_phoneb", "de-DE_phoneb", IntPtr.Zero, true));
        Check("initializer-gc-initsort", InitializerState(initialized, "seed", "zh-Hant", new IntPtr(0x1234), true));
        var managedCulture = MakeInitializerCulture();
        Check("initializer-subclass-type", managedCulture.GetType() == typeof(InitializerCulture)
            && typeof(InitializerCulture).BaseType == typeof(CultureInfo));
        Check("initializer-subclass-metadata", typeof(OverrideCulture).BaseType == typeof(CultureInfo));
        const string unsupported = "CompareInfo initialization requires a native CultureInfo payload";
        observed = false;
        try { CreateCompareInfo(managedCulture); }
        catch (PlatformNotSupportedException error) { observed = error.Message == unsupported; }
        Check("initializer-subclass-newobj", observed);
        for (int route = 0; route < 3; route++)
        {
            SeedCompareInfo(receiver, "subclass name", "subclass sort", new IntPtr(0x5678));
            observed = false;
            try
            {
                if (route == 0) CallCtor(receiver, managedCulture);
                else if (route == 1) DirectInitSort(receiver, managedCulture);
                else PointerInitSort(receiver, managedCulture);
            }
            catch (PlatformNotSupportedException error) { observed = error.Message == unsupported; }
            Check("initializer-subclass-" + route, observed
                && InitializerState(receiver, "subclass name", "subclass sort", new IntPtr(0x5678), false));
        }
        Console.WriteLine("DTFI CompareInfo initializers end");
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
    private static bool DirectValidateCultureName(string name, bool throwException) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool PointerValidateCultureName(string name, bool throwException) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool ReflectionValidateCultureName(string name, bool throwException)
    {
        if (typeof(CultureInfo).GetMethod("VerifyCultureName", BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(string), typeof(bool) }, null) is not null
            || typeof(CultureInfo).GetMethod("VerifyCultureName", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(string), typeof(bool) }, null) is not null
            || typeof(CultureInfo).GetMethod("VerifyCultureName", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(string), typeof(int) }, null) is not null)
            throw new InvalidOperationException("culture validator metadata lookup scope changed");
        var method = typeof(CultureInfo).GetMethod("VerifyCultureName", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(string), typeof(bool) }, null)!;
        if (method is null) throw new InvalidOperationException("culture validator metadata is missing");
        if (method.DeclaringType != typeof(CultureInfo) || method.ReturnType != typeof(bool)
            || !method.IsStatic || method.IsGenericMethod || method.GetParameters().Length != 2)
            throw new InvalidOperationException("culture validator metadata changed");
        return (bool)method.Invoke(null, new object?[] { name, throwException })!;
    }
    private static string ObserveCultureName(int route, string name, bool throwException, bool oracle)
    {
        try
        {
            bool value = oracle || route == 2 ? ReflectionValidateCultureName(name, throwException)
                : route == 0 ? DirectValidateCultureName(name, throwException) : PointerValidateCultureName(name, throwException);
            return "value:" + value;
        }
        catch (Exception error)
        {
            var actual = error is TargetInvocationException invocation && invocation.InnerException is not null ? invocation.InnerException : error;
            string? parameter = actual is ArgumentException argument ? argument.ParamName : null;
            return "exception:" + actual.GetType().FullName + "|" + (parameter ?? "<null>") + "|" + actual.Message;
        }
    }
    private static void CultureNameChecks(bool oracle)
    {
        Console.WriteLine("== CultureInfo name validation ==");
        string[] names = { null!, "", "-_", "en-US", "de_DE", "日本語", "é١", "en.US", "a b", "\u0301" };
        for (int index = 0; index < names.Length; index++)
        for (int fail = 0; fail < 2; fail++)
        for (int route = 0; route < 3; route++)
        {
            string observed = ObserveCultureName(route, names[index], fail != 0, oracle);
            bool valid = index >= 1 && index <= 6;
            string expected = index == 0 ? "exception:System.NullReferenceException|<null>|"
                : !valid && fail != 0 ? "exception:System.ArgumentException|<null>|" : "value:" + valid;
            bool matches = expected.StartsWith("exception:", StringComparison.Ordinal)
                ? observed.StartsWith(expected, StringComparison.Ordinal) && observed.Length > expected.Length : observed == expected;
            string label = "validator-" + index + "-" + fail + "-" + route;
            if (!matches) Console.Error.WriteLine(label + ": " + observed);
            Check(label, matches);
            Console.WriteLine("culture-name-validator:" + index + ":" + fail + ":" + route + ":" + observed + "|end");
        }
        Console.WriteLine("CultureInfo name validation end");
    }
    public static void RunValidatorOracle()
    {
        PinCulture();
        CultureNameChecks(true);
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
        InitializerChecks();
        CultureNameChecks(false);
    }
    private static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "validator-oracle") RunValidatorOracle();
        else if (args.Length == 1 && args[0] == "initializer-culture-oracle") RunInitializerCultureOracle();
        else Run();
    }
}
