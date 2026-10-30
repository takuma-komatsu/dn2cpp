using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DateTimeTrioSubset;

internal static class Program
{
    private const ulong MaxTicks = 3155378975999999999UL;
    private const ulong TickMask = 0x3fffffffffffffffUL;
    private delegate bool AddQuery(ref DateTime receiver, long delta, out DateTime result);
    private delegate bool ClosedAdd(long delta, out DateTime result);
    private delegate bool CreateQuery(int y, int m, int d, int h, int n, int s, int ms, out DateTime result);
    private delegate void CtorQuery(ref DateTime receiver, long ticks, DateTimeKind kind, bool ambiguous);
    private delegate void ClosedCtor(long ticks, DateTimeKind kind, bool ambiguous);
    private static bool AddDirect(ref DateTime receiver, long delta, out DateTime result) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool AddPointer(ref DateTime receiver, long delta, out DateTime result) => throw new InvalidOperationException("native fixture not rewritten");
    private static ClosedAdd AddAddress(DateTime receiver) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool AddDelegate(ref DateTime receiver, long delta, out DateTime result) => AddAddress(receiver)(delta, out result);
    private static bool CreateDirect(int y, int m, int d, int h, int n, int s, int ms, out DateTime result) => throw new InvalidOperationException("native fixture not rewritten");
    private static bool CreatePointer(int y, int m, int d, int h, int n, int s, int ms, out DateTime result) => throw new InvalidOperationException("native fixture not rewritten");
    private static CreateQuery CreateAddress() => throw new InvalidOperationException("native fixture not rewritten");
    private static void CtorDirect(ref DateTime receiver, long ticks, DateTimeKind kind, bool ambiguous) => throw new InvalidOperationException("native fixture not rewritten");
    private static void CtorPointer(ref DateTime receiver, long ticks, DateTimeKind kind, bool ambiguous) => throw new InvalidOperationException("native fixture not rewritten");
    private static DateTime CtorNew(long ticks, DateTimeKind kind, bool ambiguous) => throw new InvalidOperationException("native fixture not rewritten");
    private static ClosedCtor CtorAddress(object boxed) => throw new InvalidOperationException("native fixture not rewritten");
    private static MethodInfo AddMetadata() => typeof(DateTime).GetMethod("TryAddTicks", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static MethodInfo CreateMetadata() => typeof(DateTime).GetMethod("TryCreate", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static ConstructorInfo CtorMetadata() => typeof(DateTime).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(long), typeof(DateTimeKind), typeof(bool) }, null)!;
    private static ulong Bits(DateTime value) => Unsafe.As<DateTime, ulong>(ref value);
    private static DateTime Raw(ulong bits) => Unsafe.As<ulong, DateTime>(ref bits);
    private static void Check(string label, bool result)
    {
        Console.WriteLine("datetime-trio-" + label + ":" + result);
        if (!result) throw new InvalidOperationException(label);
    }
    private static long Tick(int index) => index switch { 0 => 0, 1 => 1, 2 => 638448068967890000L, 3 => (long)MaxTicks - 1, _ => (long)MaxTicks };
    private static long Delta(int index) => index switch { 0 => 0, 1 => 1, 2 => -1, 3 => 10000000, 4 => -10000000, 5 => long.MinValue, _ => long.MaxValue };

    private static void AddCases(AddQuery query, string route, bool aliases)
    {
        int cases = 0;
        for (int kind = 0; kind < 4; kind++)
        for (int tick = 0; tick < 5; tick++)
        for (int delta = 0; delta < 7; delta++)
        {
            ulong original = (ulong)Tick(tick) | ((ulong)kind << 62);
            var receiver = Raw(original);
            var output = Raw(ulong.MaxValue);
            ulong sum = unchecked((ulong)Tick(tick) + (ulong)Delta(delta));
            bool expected = sum <= MaxTicks;
            ulong expectedBits = expected ? sum | ((ulong)kind << 62) : 0;
            bool actual = query(ref receiver, Delta(delta), out output);
            if (actual != expected || Bits(output) != expectedBits || Bits(receiver) != original)
                throw new InvalidOperationException("add " + route + " matrix " + kind + "/" + tick + "/" + delta);
            if (aliases)
            {
                receiver = Raw(original);
                actual = query(ref receiver, Delta(delta), out receiver);
                if (actual != expected || Bits(receiver) != expectedBits) throw new InvalidOperationException("add alias " + route);
            }
            cases++;
        }
        Check("add-" + route + "-matrix", cases == 140);
        if (aliases) Check("add-" + route + "-alias", cases == 140);
        foreach (long delta in new[] { 0L, -1L })
        {
            var receiver = Raw(0x8000000000000000UL);
            bool observed = false;
            try { query(ref receiver, delta, out Unsafe.NullRef<DateTime>()); }
            catch (NullReferenceException) { observed = true; }
            Check("add-" + route + "-null-out-" + delta, observed && Bits(receiver) == 0x8000000000000000UL);
        }
        if (aliases)
        {
            var output = Raw(ulong.MaxValue);
            bool observed = false;
            try { query(ref Unsafe.NullRef<DateTime>(), 0, out output); }
            catch (NullReferenceException) { observed = true; }
            Check("add-" + route + "-null-receiver", observed && Bits(output) == ulong.MaxValue);
            observed = false;
            try { query(ref Unsafe.NullRef<DateTime>(), 0, out Unsafe.NullRef<DateTime>()); }
            catch (NullReferenceException) { observed = true; }
            Check("add-" + route + "-both-null", observed);
        }
    }

    private static readonly int[] Parts = {
        1,1,1,0,0,0,0, 9999,12,31,23,59,59,999, 2024,2,29,12,34,56,789,
        1900,2,28,0,0,0,0, 2000,2,29,0,0,0,0, 1,2,28,0,0,0,0,
        0,1,1,0,0,0,0, 10000,1,1,0,0,0,0, -1,1,1,0,0,0,0,
        int.MinValue,1,1,0,0,0,0, int.MaxValue,1,1,0,0,0,0,
        2024,0,1,0,0,0,0, 2024,13,1,0,0,0,0, 2024,int.MinValue,1,0,0,0,0,
        2024,1,0,0,0,0,0, 2024,1,32,0,0,0,0, 1900,2,29,0,0,0,0,
        2023,2,29,0,0,0,0, 2024,4,31,0,0,0,0,
        2024,1,1,-1,0,0,0, 2024,1,1,24,0,0,0, 2024,1,1,0,-1,0,0,
        2024,1,1,0,60,0,0, 2024,1,1,0,0,-1,0, 2024,1,1,0,0,60,0,
        2024,1,1,0,0,0,-1, 2024,1,1,0,0,0,1000, 2024,1,1,0,0,0,int.MaxValue
    };
    // Public CoreCLR constructor oracle, kept separate from native helper arithmetic.
    private static readonly long[] ExpectedCreateTicks = {
        0L, 3155378975999990000L, 638448068967890000L,
        599316192000000000L, 630873792000000000L, 50112000000000L
    };
    private static bool PublicCreate(int index, out DateTime value)
    {
        int k = index * 7;
        try { value = new DateTime(Parts[k], Parts[k+1], Parts[k+2], Parts[k+3], Parts[k+4], Parts[k+5], Parts[k+6]); return true; }
        catch (ArgumentOutOfRangeException) { value = default; return false; }
    }
    private static void CreateCases(CreateQuery query, string route)
    {
        int cases = 0;
        for (int index = 0; index < Parts.Length / 7; index++)
        {
            int k = index * 7;
            bool expected = index < ExpectedCreateTicks.Length;
            bool publicSuccess = PublicCreate(index, out DateTime publicValue);
            if (publicSuccess != expected || (expected && publicValue.Ticks != ExpectedCreateTicks[index]))
                throw new InvalidOperationException("public create neighbor " + index);
            var result = Raw(ulong.MaxValue);
            bool actual = query(Parts[k], Parts[k+1], Parts[k+2], Parts[k+3], Parts[k+4], Parts[k+5], Parts[k+6], out result);
            if (actual != expected || Bits(result) != (expected ? (ulong)ExpectedCreateTicks[index] : 0)) throw new InvalidOperationException("create " + route + " case " + index);
            cases++;
        }
        Check("create-" + route + "-matrix", cases == 28);
        for (int invalid = 0; invalid < 2; invalid++)
        {
            bool observed = false;
            try { query(invalid == 0 ? 2024 : 0, 1, 1, 0, 0, 0, 0, out Unsafe.NullRef<DateTime>()); }
            catch (NullReferenceException) { observed = true; }
            Check("create-" + route + "-null-out-" + invalid, observed);
        }
    }

    private static long ConstructorTicks(int index) => index switch { 0 => 0, 1 => 1, 2 => (long)MaxTicks, 3 => -1, 4 => (long)MaxTicks+1, 5 => long.MinValue, _ => long.MaxValue };
    private static DateTimeKind ConstructorKind(int index) => (DateTimeKind)(index switch { 0 => 0, 1 => 1, 2 => 2, 3 => -1, 4 => int.MinValue, _ => int.MaxValue });
    private static void CtorCases(CtorQuery query, string route)
    {
        int cases = 0;
        for (int tick = 0; tick < 7; tick++)
        for (int kind = 0; kind < 6; kind++)
        foreach (bool ambiguous in new[] { false, true })
        {
            long ticks = ConstructorTicks(tick);
            bool valid = unchecked((ulong)ticks) <= MaxTicks;
            var storage = Raw(0x1122334455667788UL);
            bool success = false, range = false;
            try { query(ref storage, ticks, ConstructorKind(kind), ambiguous); success = true; }
            catch (ArgumentOutOfRangeException error) { range = error.ParamName == "ticks"; }
            if (success != valid || range == valid || Bits(storage) != (valid ? (ulong)ticks | ((ambiguous ? 3UL : 2UL) << 62) : 0x1122334455667788UL))
                throw new InvalidOperationException("ctor " + route + " matrix " + tick + "/" + kind);
            cases++;
        }
        Check("ctor-" + route + "-matrix", cases == 84);
        foreach (long ticks in new[] { 0L, -1L, (long)MaxTicks + 1 })
        {
            bool expected = false;
            try { query(ref Unsafe.NullRef<DateTime>(), ticks, (DateTimeKind)99, true); }
            catch (NullReferenceException) { expected = ticks == 0; }
            catch (ArgumentOutOfRangeException error) { expected = ticks != 0 && error.ParamName == "ticks"; }
            Check("ctor-" + route + "-null-order-" + ticks, expected);
        }
    }
    private static void CtorValueCases()
    {
        int cases = 0;
        for (int kind = 0; kind < 6; kind++)
        foreach (bool ambiguous in new[] { false, true })
        {
            var value = CtorNew(17, ConstructorKind(kind), ambiguous);
            object boxed = Raw(0UL);
            CtorAddress(boxed)(17, ConstructorKind(kind), ambiguous);
            ulong expected = 17UL | ((ambiguous ? 3UL : 2UL) << 62);
            if (Bits(value) != expected || Bits((DateTime)boxed) != expected) throw new InvalidOperationException("ctor value/delegate");
            cases++;
        }
        Check("ctor-newobj-and-delegate", cases == 12);
        bool observed = false;
        try { CtorNew(-1, (DateTimeKind)99, true); }
        catch (ArgumentOutOfRangeException error) { observed = error.ParamName == "ticks"; }
        Check("ctor-newobj-invalid", observed);
    }

    private static int trace;
    private static int throwing;
    private static bool nullStorage;
    private static DateTime producerReceiver;
    private static DateTime producerOutput;
    private static void Step(int index)
    {
        trace = trace * 10 + index;
        if (throwing == index) throw new FormatException("argument producer");
    }
    private static ref DateTime ReceiverArgument() { Step(1); if (nullStorage) return ref Unsafe.NullRef<DateTime>(); return ref producerReceiver; }
    private static long TickArgument() { Step(2); return nullStorage ? -1 : 17; }
    private static long DeltaArgument() { Step(2); return 1; }
    private static DateTimeKind KindArgument() { Step(3); return (DateTimeKind)99; }
    private static bool AmbiguousArgument() { Step(4); return true; }
    private static int PartArgument(int position) { Step(position); return position switch { 1 => 2024, 2 => 2, 3 => 29, _ => 0 }; }
    private static ref DateTime OutputArgument(int position) { Step(position); if (nullStorage && position == 8) return ref Unsafe.NullRef<DateTime>(); return ref producerOutput; }
    private static bool AddProducer() => AddDirect(ref ReceiverArgument(), DeltaArgument(), out OutputArgument(3));
    private static bool CreateProducer() => CreateDirect(PartArgument(1), PartArgument(2), PartArgument(3), PartArgument(4), PartArgument(5), PartArgument(6), PartArgument(7), out OutputArgument(8));
    private static void CtorProducer() => CtorDirect(ref ReceiverArgument(), TickArgument(), KindArgument(), AmbiguousArgument());
    private static void Producers()
    {
        foreach (string mode in new[] { "add", "create", "ctor" })
        {
            int count = mode == "add" ? 3 : mode == "create" ? 8 : 4;
            for (int failure = -1; failure <= count; failure++)
            {
                trace = 0; throwing = failure; nullStorage = failure == 0;
                producerReceiver = Raw(0x8000000000000000UL);
                producerOutput = Raw(ulong.MaxValue);
                bool observed = false;
                try
                {
                    if (mode == "add") observed = AddProducer() && failure == -1 && Bits(producerOutput) == 0x8000000000000001UL;
                    else if (mode == "create") observed = CreateProducer() && failure == -1 && producerOutput == new DateTime(2024, 2, 29);
                    else { CtorProducer(); observed = failure == -1 && Bits(producerReceiver) == 0xc000000000000011UL; }
                }
                catch (FormatException error) { observed = failure > 0 && error.Message == "argument producer"; }
                catch (NullReferenceException) { observed = failure == 0 && mode != "ctor"; }
                catch (ArgumentOutOfRangeException error) { observed = failure == 0 && mode == "ctor" && error.ParamName == "ticks"; }
                int expectedTrace = 0;
                for (int position = 1; position <= (failure > 0 ? failure : count); position++) expectedTrace = expectedTrace * 10 + position;
                Check("producer-" + mode + "-" + failure, observed && trace == expectedTrace
                    && (failure == -1 || (Bits(producerReceiver) == 0x8000000000000000UL && Bits(producerOutput) == ulong.MaxValue)));
            }
        }
    }

    public static void RunLookupChecks()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        bool add = AddMetadata() is not null;
        bool create = CreateMetadata() is not null;
        bool ctor = CtorMetadata() is not null;
        Console.WriteLine("datetime-trio-reflection-add-lookup:" + add);
        Console.WriteLine("datetime-trio-reflection-create-lookup:" + create);
        Console.WriteLine("datetime-trio-reflection-ctor-lookup:" + ctor);
        Check("reflection-lookups", add && create && ctor);
        const BindingFlags hiddenInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags hiddenStatic = BindingFlags.Static | BindingFlags.NonPublic;
        Type[] addTypes = { typeof(long), typeof(DateTime).MakeByRefType() };
        Type[] createTypes = { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(DateTime).MakeByRefType() };
        Type[] ctorTypes = { typeof(long), typeof(DateTimeKind), typeof(bool) };
        Check("lookup-add-public", typeof(DateTime).GetMethod("TryAddTicks", BindingFlags.Instance | BindingFlags.Public) is null);
        Check("lookup-add-static", typeof(DateTime).GetMethod("TryAddTicks", hiddenStatic) is null);
        Check("lookup-create-public", typeof(DateTime).GetMethod("TryCreate", BindingFlags.Static | BindingFlags.Public) is null);
        Check("lookup-create-instance", typeof(DateTime).GetMethod("TryCreate", hiddenInstance) is null);
        Check("lookup-add-exact", typeof(DateTime).GetMethod("TryAddTicks", hiddenInstance, null, addTypes, null) == AddMetadata());
        Check("lookup-create-exact", typeof(DateTime).GetMethod("TryCreate", hiddenStatic, null, createTypes, null) == CreateMetadata());
        Check("lookup-add-arity", typeof(DateTime).GetMethod("TryAddTicks", hiddenInstance, null, Type.EmptyTypes, null) is null);
        Check("lookup-add-value", typeof(DateTime).GetMethod("TryAddTicks", hiddenInstance, null, new[] { typeof(long), typeof(DateTime) }, null) is null);
        Check("lookup-create-arity", typeof(DateTime).GetMethod("TryCreate", hiddenStatic, null, Type.EmptyTypes, null) is null);
        var wrongCreateTypes = (Type[])createTypes.Clone();
        wrongCreateTypes[7] = typeof(long).MakeByRefType();
        Check("lookup-create-element", typeof(DateTime).GetMethod("TryCreate", hiddenStatic, null, wrongCreateTypes, null) is null);
        Check("lookup-add-generic", typeof(DateTime).GetMethod("TryAddTicks", 1, hiddenInstance, null, addTypes, null) is null);
        Check("lookup-create-generic", typeof(DateTime).GetMethod("TryCreate", 1, hiddenStatic, null, createTypes, null) is null);
        Check("lookup-ctor-exact", typeof(DateTime).GetConstructor(hiddenInstance, null, ctorTypes, null) == CtorMetadata());
        Check("lookup-ctor-public", typeof(DateTime).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, ctorTypes, null) is null);
        Check("lookup-ctor-static", typeof(DateTime).GetConstructor(hiddenStatic, null, ctorTypes, null) is null);
        Check("lookup-ctor-arity", typeof(DateTime).GetConstructor(hiddenInstance, null, Type.EmptyTypes, null) is null);
        Check("lookup-ctor-kind", typeof(DateTime).GetConstructor(hiddenInstance, null, new[] { typeof(long), typeof(int), typeof(bool) }, null) is null);
        var publicCtor = typeof(DateTime).GetConstructor(new[] { typeof(long), typeof(DateTimeKind) });
        Check("lookup-public-ctor-not-private", publicCtor is null || (publicCtor.IsPublic && publicCtor.GetParameters().Length == 2));
        bool rejected = false;
        try { typeof(DateTime).GetMethod("TryAddTicks", hiddenInstance, null, null!, null); }
        catch (ArgumentNullException error) { rejected = error.ParamName == "types"; }
        Check("lookup-method-null-types", rejected);
        rejected = false;
        try { typeof(DateTime).GetConstructor(hiddenInstance, null, null!, null); }
        catch (ArgumentNullException error) { rejected = error.ParamName == "types"; }
        Check("lookup-ctor-null-types", rejected);
        rejected = false;
        try { typeof(DateTime).GetMethod("TryCreate", hiddenStatic, null, new Type[] { null! }, null); }
        catch (ArgumentNullException error) { rejected = error.ParamName == "types"; }
        Check("lookup-method-null-element", rejected);
        rejected = false;
        try { ((Type)null!).GetMethod(null!, hiddenInstance, null, null!, null); }
        catch (NullReferenceException) { rejected = true; }
        Check("lookup-null-type-first", rejected);
    }

    public static void RunReflectionChecks()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        RunLookupChecks();
        foreach (long delta in new[] { 1L, -1L })
        {
            object[] arguments = { delta, Raw(ulong.MaxValue) };
            bool actual = (bool)AddMetadata().Invoke(Raw(0xc000000000000000UL), arguments)!;
            Check("reflection-add-" + delta, actual == (delta == 1) && Bits((DateTime)arguments[1]) == (delta == 1 ? 0xc000000000000001UL : 0));
        }
        bool observed = false;
        try { AddMetadata().Invoke(null, new object[] { 0L, default(DateTime) }); }
        catch (TargetException) { observed = true; }
        Check("reflection-add-null-target", observed);
        foreach (int year in new[] { 2024, 0 })
        {
            object[] arguments = { year, 2, 29, 0, 0, 0, 0, Raw(ulong.MaxValue) };
            bool actual = (bool)CreateMetadata().Invoke(null, arguments)!;
            Check("reflection-create-" + year, actual == (year == 2024) && Bits((DateTime)arguments[7]) == (year == 2024 ? (ulong)new DateTime(2024, 2, 29).Ticks : 0));
        }
        foreach (bool ambiguous in new[] { false, true })
        {
            object value = CtorMetadata().Invoke(new object[] { 17L, (DateTimeKind)99, ambiguous });
            Check("reflection-ctor-" + ambiguous, Bits((DateTime)value) == (17UL | ((ambiguous ? 3UL : 2UL) << 62)));
        }
        observed = false;
        try { CtorMetadata().Invoke(new object[] { -1L, (DateTimeKind)99, true }); }
        catch (TargetInvocationException error) { observed = error.InnerException is ArgumentOutOfRangeException range && range.ParamName == "ticks"; }
        Check("reflection-ctor-range-wrapped", observed);
        var add = AddMetadata().GetParameters();
        var create = CreateMetadata().GetParameters();
        var ctor = CtorMetadata().GetParameters();
        Check("reflection-abi", AddMetadata().ReturnType == typeof(bool) && add.Length == 2 && add[0].ParameterType == typeof(long)
            && add[1].ParameterType == typeof(DateTime).MakeByRefType() && CreateMetadata().ReturnType == typeof(bool)
            && create.Length == 8 && create[7].ParameterType == typeof(DateTime).MakeByRefType()
            && ctor.Length == 3 && ctor[0].ParameterType == typeof(long) && ctor[1].ParameterType == typeof(DateTimeKind) && ctor[2].ParameterType == typeof(bool));
        Console.WriteLine("DateTime trio reflection end");
    }

    public static void RunPublicOracle()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        int success = 0, invalid = 0;
        for (int index = 0; index < Parts.Length / 7; index++)
        {
            if (PublicCreate(index, out DateTime value))
            {
                success++;
                if (index >= ExpectedCreateTicks.Length || value.Kind != DateTimeKind.Unspecified || value.Ticks != ExpectedCreateTicks[index])
                    throw new InvalidOperationException("public tick oracle " + index);
            }
            else
            {
                if (index < ExpectedCreateTicks.Length) throw new InvalidOperationException("public valid date rejected " + index);
                invalid++;
            }
        }
        Check("public-create-table", success == 6 && invalid == 22);
        Check("public-neighbors", new DateTime(2024, 2, 29).AddTicks(1).Ticks == 638447616000000001L
            && new DateTime(17, DateTimeKind.Utc).Kind == DateTimeKind.Utc);
    }

    public static void RunMinimal()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var value = default(DateTime);
        bool add = AddDirect(ref value, 1, out value);
        bool create = CreateDirect(1, 1, 1, 0, 0, 0, 0, out value);
        CtorDirect(ref value, 17, (DateTimeKind)99, true);
        Check("minimal", add && create && Bits(value) == 0xc000000000000011UL);
    }

    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        AddCases(AddDirect, "direct", true);
        AddCases(AddPointer, "calli", true);
        AddCases(AddDelegate, "delegate", false);
        CreateCases(CreateDirect, "direct");
        CreateCases(CreatePointer, "calli");
        CreateCases(CreateAddress(), "delegate");
        CtorCases(CtorDirect, "direct");
        CtorCases(CtorPointer, "calli");
        CtorValueCases();
        Check("add-named-delegate-identity", AddAddress(Raw(0)).Method == AddMetadata());
        Check("create-named-delegate-identity", CreateAddress().Method == CreateMetadata());
        Producers();
        RunReflectionChecks();
        RunPublicOracle();
        Console.WriteLine("DateTime trio end");
    }
    private static void Main() => Run();
}
