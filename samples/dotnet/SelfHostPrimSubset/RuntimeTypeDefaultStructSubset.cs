using System;
using DefaultStructCtorLib;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RuntimeTypeDefaultStructSubset;

internal static class StructChecks
{
    private static int _calls, _throwCalls, _privateCalls, _planCctors, _genericCctors, _privateCctors, _implicitCctors;
    private struct Plain { public int Value; }
    private enum Choice : int { One = 1 }
    private struct Explicit { public int Value; public Explicit() { _calls++; Value = 42; } }
    private struct Throwing { public int Value; public Throwing() { Value = 91; _throwCalls++; throw new InvalidOperationException("ctor"); } }
    private struct PrivateCtor { public int Value; public PrivateCtor() { _privateCalls++; Value = 73; } }
    private struct ByteCtor { public byte Value; public ByteCtor() { Value = 0x5a; } }
    private ref struct RefValue { public int Value; public RefValue() { Value = 55; } }
    private struct Guarded { public int Before; public Explicit Value; public int After; }
    private struct PlanPublic { public int Value; static PlanPublic() { _planCctors++; } public PlanPublic() { Value = 17; } public PlanPublic(int n) { Value = n; } }
    private struct PlanGeneric { public int Value; static PlanGeneric() { _genericCctors++; } public PlanGeneric() { Value = 19; } public PlanGeneric(int n) { Value = n; } }
    private struct PlanImplicit { public int Value; static PlanImplicit() { _implicitCctors++; } }
    private struct PlanPrivate { public int Value; static PlanPrivate() { _privateCctors++; } public PlanPrivate() { Value = 23; } public PlanPrivate(int n) { Value = n; } }
    private delegate void Caller(Type type, ref byte data);
    private delegate void Bound(ref byte data);
    private static void Check(string label, bool value)
    {
        Console.WriteLine("runtime-type-default-struct-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }
    // Oracle destinations start at default: assignment is not an in-place oracle for prefilled storage.
    private static void Public(Type type, ref byte data)
    {
        if (type is null) throw new NullReferenceException();
        if (type == typeof(int)) Unsafe.As<byte, int>(ref data) = Activator.CreateInstance<int>();
        else if (type == typeof(Choice)) Unsafe.As<byte, Choice>(ref data) = Activator.CreateInstance<Choice>();
        else if (type == typeof(Plain)) Unsafe.As<byte, Plain>(ref data) = Activator.CreateInstance<Plain>();
        else if (type == typeof(Explicit)) Unsafe.As<byte, Explicit>(ref data) = Activator.CreateInstance<Explicit>();
        else if (type == typeof(Explicit?)) Unsafe.As<byte, Explicit?>(ref data) = Activator.CreateInstance<Explicit?>();
        else if (type == typeof(Throwing)) Unsafe.As<byte, Throwing>(ref data) = Activator.CreateInstance<Throwing>();
        else if (type == typeof(RefValue)) Unsafe.As<byte, RefValue>(ref data) = Activator.CreateInstance<RefValue>();
        else if (type == typeof(ByteCtor)) Unsafe.As<byte, ByteCtor>(ref data) = Activator.CreateInstance<ByteCtor>();
        else throw new InvalidOperationException("Unknown public oracle type");
    }
    // Private generic factory calls belong only to the CLR oracle.
    // The native fixture replaces each caller with exact instance IL; this helper is oracle-only.
    private static void OraclePrivate(Type type, ref byte data)
    {
        if (type == typeof(PrivateCtor)) Unsafe.As<byte, PrivateCtor>(ref data) = Activator.CreateInstance<PrivateCtor>();
        else if (type == typeof(ExternalPrivate)) Unsafe.As<byte, ExternalPrivate>(ref data) = Activator.CreateInstance<ExternalPrivate>();
        else Public(type, ref data);
    }
    private static void Direct(Type type, ref byte data) => OraclePrivate(type, ref data);
    private static void Virtual(Type type, ref byte data) => OraclePrivate(type, ref data);
    private static void Pointer(Type type, ref byte data) => OraclePrivate(type, ref data);
    private static Bound Address(Type type) => (ref byte data) => OraclePrivate(type, ref data);
    private static void AddressQuery(Type type, ref byte data)
    {
        if (type is null) throw new NullReferenceException();
        Address(type)(ref data);
    }
    // Only byte-sized ByteCtor goes through reflection's one-byte ref argument backing storage.
    private static void ReflectionLeaf(Type type, ref byte data) => OraclePrivate(type, ref data);
    private static void ReflectionCheck()
    {
        object[] args = { typeof(ByteCtor), (byte)0 };
        typeof(StructChecks).GetMethod(nameof(ReflectionLeaf), BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
        Check("reflection-byte-writeback", args[1] is byte value && value == 0x5a);
    }
    private static void Roots()
    {
        Activator.CreateInstance<Explicit>();
        Activator.CreateInstance<ByteCtor>();
        Activator.CreateInstance<RefValue>();
        try { Activator.CreateInstance<Throwing>(); } catch (TargetInvocationException) { }
    }
    private static void Route(string route, Caller call, bool privateCase)
    {
        int primitive = 0; call(typeof(int), ref Unsafe.As<int, byte>(ref primitive)); Check(route + "-int", primitive == 0);
        Choice choice = default; call(typeof(Choice), ref Unsafe.As<Choice, byte>(ref choice)); Check(route + "-enum", (int)choice == 0);
        Plain plain = default; call(typeof(Plain), ref Unsafe.As<Plain, byte>(ref plain)); Check(route + "-plain", plain.Value == 0);
        Explicit value = default; int before = _calls;
        call(typeof(Explicit), ref Unsafe.As<Explicit, byte>(ref value)); Check(route + "-explicit", value.Value == 42 && _calls == before + 1);
        Explicit? nullable = default; before = _calls;
        call(typeof(Explicit?), ref Unsafe.As<Explicit?, byte>(ref nullable)); Check(route + "-nullable", !nullable.HasValue && _calls == before);
        RefValue byrefLike = default; call(typeof(RefValue), ref Unsafe.As<RefValue, byte>(ref byrefLike)); Check(route + "-refstruct", byrefLike.Value == 55);
        Throwing throwing = default; bool wrapped = false; before = _throwCalls;
        try { call(typeof(Throwing), ref Unsafe.As<Throwing, byte>(ref throwing)); }
        catch (TargetInvocationException error) { wrapped = error.InnerException is InvalidOperationException; }
        Check(route + "-throw", wrapped && _throwCalls == before + 1);
        byte data = 0; bool nullError = false;
        try { call(null, ref data); } catch (NullReferenceException) { nullError = true; }
        Check(route + "-null", nullError && data == 0);
        if (privateCase)
        {
            PrivateCtor hidden = default; bool missing = false; before = _privateCalls;
            try { call(typeof(PrivateCtor), ref Unsafe.As<PrivateCtor, byte>(ref hidden)); } catch (MissingMethodException) { missing = true; }
            Check(route + "-private", missing && _privateCalls == before);
            ExternalPrivate external = default; missing = false;
            try { call(typeof(ExternalPrivate), ref Unsafe.As<ExternalPrivate, byte>(ref external)); } catch (MissingMethodException) { missing = true; }
            Check(route + "-external-private", missing && external.Value == 0);
        }
    }
    private static void PlannerPublic(ref byte data) => Unsafe.As<byte, PlanPublic>(ref data) = Activator.CreateInstance<PlanPublic>();
    private static void PlannerImplicit(ref byte data) => Unsafe.As<byte, PlanImplicit>(ref data) = Activator.CreateInstance<PlanImplicit>();
    private static void PlannerPrivate(ref byte data) => Unsafe.As<byte, PlanPrivate>(ref data) = Activator.CreateInstance<PlanPrivate>();
    private static void GenericPlanner<T>(ref byte data) => Unsafe.As<byte, T>(ref data) = Activator.CreateInstance<T>();
    private static unsafe void PlannerChecks()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        // Direct addresses avoid opaque generic-call demands in the isolated planner test.
        PlanPublic value = default; PlannerPublic(ref *(byte*)&value);
        Check("planner-public", value.Value == 17 && _planCctors == 1);
        PlanGeneric generic = default; GenericPlanner<PlanGeneric>(ref *(byte*)&generic);
        Check("planner-generic", generic.Value == 19 && _genericCctors == 1);
        PlanPrivate hidden = default; bool missing = false;
        try { PlannerPrivate(ref *(byte*)&hidden); } catch (MissingMethodException) { missing = true; }
        Check("planner-private", missing && hidden.Value == 0 && _privateCctors == 0);
        PlanImplicit noop = default; PlannerImplicit(ref *(byte*)&noop);
        Check("planner-implicit", noop.Value == 0 && _implicitCctors == 0);
        GenericPlanner<PlanImplicit>(ref *(byte*)&noop);
        Check("planner-generic-implicit", noop.Value == 0 && _implicitCctors == 0);
        ExternalPrivate external = default; missing = false;
        try { GenericPlanner<ExternalPrivate>(ref *(byte*)&external); } catch (MissingMethodException) { missing = true; }
        Check("planner-external-private", missing && external.Value == 0);
    }
    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== RuntimeType default struct constructor ==");
        Roots(); Route("public", Public, false);
        Console.WriteLine("RuntimeType default struct constructor end");
    }
    internal static void RunArtifact(string[] args)
    {
        Run(); Route("direct", Direct, true); Route("virtual", Virtual, true); Route("calli", Pointer, true); Route("address", AddressQuery, true);
        PlannerChecks(); RunReflectionArtifact(args);
        if (args.Length > 0 && args[0] == "native-policy") NativePolicy();
    }
    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Roots(); ReflectionCheck();
    }
    private static unsafe void NativePolicy()
    {
        byte* nil = null;
        Direct(typeof(int), ref *nil); Check("policy-null-noop", true);
        bool refused = false;
        try { Direct(typeof(Explicit), ref *nil); } catch (PlatformNotSupportedException) { refused = true; }
        Check("policy-null-explicit", refused);
        refused = false;
        try { Direct(typeof(PrivateCtor), ref *nil); } catch (MissingMethodException) { refused = true; }
        Check("policy-visibility-before-storage", refused);
        int primitive = 123; Direct(typeof(int), ref Unsafe.As<int, byte>(ref primitive)); Check("policy-int-preserved", primitive == 123);
        Plain plain = default; plain.Value = 37; Direct(typeof(Plain), ref Unsafe.As<Plain, byte>(ref plain)); Check("policy-plain-preserved", plain.Value == 37);
        Explicit? nullable = new Explicit { Value = 61 }; int before = _calls;
        Direct(typeof(Explicit?), ref Unsafe.As<Explicit?, byte>(ref nullable)); Check("policy-nullable-preserved", nullable.HasValue && nullable.Value.Value == 61 && _calls == before);
        Guarded guard = default; guard.Before = 0x12345678; guard.After = 0x23456789; guard.Value.Value = 67;
        Direct(typeof(Explicit), ref Unsafe.As<Explicit, byte>(ref guard.Value));
        Check("policy-inplace-guards", guard.Before == 0x12345678 && guard.After == 0x23456789 && guard.Value.Value == 42);
        Throwing partial = default; partial.Value = 7;
        try { Direct(typeof(Throwing), ref Unsafe.As<Throwing, byte>(ref partial)); } catch (TargetInvocationException) { }
        Check("policy-partial-write", partial.Value == 91);
        var method = Address(typeof(ByteCtor)).Method;
        Check("policy-row", method.Name == "CallDefaultStructConstructor" && !method.IsStatic && method.ReturnType == typeof(void)
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(byte).MakeByRefType());
        object[] bytes = { (byte)0 }; method.Invoke(typeof(ByteCtor), bytes); Check("policy-invoke-writeback", bytes[0] is byte b && b == 0x5a);
        var bound = (Bound)method.CreateDelegate(typeof(Bound), typeof(ByteCtor)); byte direct = 0; bound(ref direct); Check("policy-bound", direct == 0x5a);
        object[] dynamicBytes = { (byte)0 }; ((Delegate)bound).DynamicInvoke(dynamicBytes); Check("policy-dynamic-writeback", dynamicBytes[0] is byte d && d == 0x5a);
    }
}
