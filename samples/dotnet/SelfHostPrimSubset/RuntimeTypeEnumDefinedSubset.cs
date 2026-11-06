using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeEnumDefinedSubset;

internal static class EnumDefinedChecks
{
    private enum OtherI4 { One = 1 }
    private enum NameI1 : sbyte { Min = sbyte.MinValue, Neg = -1, Zero = 0, One = 1, Max = sbyte.MaxValue }
    private enum NameU1 : byte { Zero = 0, One = 1, Max = byte.MaxValue }
    private enum NameI2 : short { Min = short.MinValue, Neg = -1, Zero = 0, One = 1, Max = short.MaxValue }
    private enum NameU2 : ushort { Zero = 0, One = 1, Max = ushort.MaxValue }
    private enum NameI4 : int { Alias = 1, 日本語 = 1, Min = int.MinValue, Neg = -1, Zero = 0, One = 1, Max = int.MaxValue }
    private enum NameU4 : uint { Zero = 0, One = 1, Max = uint.MaxValue }
    private enum NameI8 : long { Min = long.MinValue, Neg = -1, Zero = 0, One = 1, Max = long.MaxValue }
    private enum NameU8 : ulong { Zero = 0, One = 1, Max = ulong.MaxValue }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("enum-defined-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool Public(Type type, object value) => type.IsEnumDefined(value);
    // The managed override checks null value before touching this; no private VM is called by the oracle.
    private static bool RawOracle(Type type, object value)
    {
        if (type is not null) return Public(type, value);
        if (value is null) throw new ArgumentNullException("value");
        throw new NullReferenceException();
    }
    private static bool Direct(Type type, object value) => RawOracle(type, value);
    private static bool Pointer(Type type, object value) => RawOracle(type, value);
    private static bool Virtual(Type type, object value) => Public(type, value);
    private static bool VirtualPointer(Type type, object value) => Public(type, value);
    private static bool Constrained(Type type, object value) => Public(type, value);
    private static Func<object, bool> Address(Type type)
    {
        if (type is null) throw new ArgumentException();
        return value => RawOracle(type, value);
    }
    private static Func<object, bool> VirtualAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return value => RawOracle(type, value);
    }
    private static Func<object, bool> FlowAddress(Type type, bool virt) => virt ? VirtualAddress(type) : Address(type);
    private static bool AddressQuery(Type type, object value) => Address(type)(value);
    private static bool VirtualAddressQuery(Type type, object value) => VirtualAddress(type)(value);
    private static bool FlowDirect(Type type, object value) => FlowAddress(type, false)(value);
    private static bool FlowVirtual(Type type, object value) => FlowAddress(type, true)(value);
    private static bool ReflectionLeaf(Type type, object value) => RawOracle(type, value);
    private static bool Reflection(Type type, object value) => (bool)typeof(EnumDefinedChecks)
        .GetMethod(nameof(ReflectionLeaf), BindingFlags.Static | BindingFlags.NonPublic)
        .Invoke(null, new object[] { type, value });

    private static string Result(Func<Type, object, bool> query, Type type, object value)
    {
        try { return query(type, value).ToString(); }
        catch (TargetInvocationException error) { return Failure(error.InnerException); }
        catch (Exception error) { return Failure(error); }
    }
    private static string Failure(Exception error) => error.GetType().Name
        + (error is ArgumentException argument ? "/" + (argument.ParamName ?? "<null>") : "")
        + "/" + error.HResult.ToString("X8");

    private static void Route(string route, Func<Type, object, bool> query)
    {
        Type[] types = { typeof(NameI1), typeof(NameU1), typeof(NameI2), typeof(NameU2), typeof(NameI4),
            typeof(NameU4), typeof(NameI8), typeof(NameU8), typeof(int), typeof(Enum) };
        object[] values = { null, (sbyte)-1, byte.MaxValue, (short)-1, ushort.MaxValue, -1, uint.MaxValue,
            -1L, ulong.MaxValue, 0, 1, 256, 257, long.MinValue, long.MaxValue, (char)1,
            NameI1.Min, NameU1.Max, NameI2.Min, NameU2.Max, NameI4.Min, NameU4.Max, NameI8.Min, NameU8.Max,
            true, (nint)1, (nuint)1, 1f, 1d, 1m, "One", new object(), "Alias", "one", "One\0", "One, Zero", "", "Missing", "Zero", "Max", "日本語", OtherI4.One };
        for (int t = 0; t < types.Length; t++)
            for (int v = 0; v < values.Length; v++)
                Console.WriteLine("enum-defined-" + route + "-" + t + "-" + v + ":" + Result(query, types[t], values[v]));
        object[] undefined = { (sbyte)2, (byte)2, (short)2, (ushort)2, 2, 2u, 2L, 2UL };
        bool undefinedRejected = true;
        for (int i = 0; i < undefined.Length; i++)
            undefinedRejected &= !query(types[i], undefined[i]);
        Check(route + "-undefined", undefinedRejected);
        Check(route + "-range", !query(typeof(NameU1), (byte)2) && !query(typeof(NameI4), 257));
        Check(route + "-wide", query(typeof(NameI1), (sbyte)-1)
            && query(typeof(NameU8), ulong.MaxValue) && query(typeof(NameI8), long.MinValue));
        Check(route + "-aliases", query(typeof(NameI4), "One") && query(typeof(NameI4), "Alias") && query(typeof(NameI4), "日本語")
            && !query(typeof(NameI4), "one") && !query(typeof(NameI4), "One\0") && !query(typeof(NameI4), "One, Zero"));
        Check(route + "-done", true);
    }

    private static void Nulls(string route, Func<Type, object, bool> query, bool raw)
    {
        Check(route + "-null-valid", Error(() => query(null, 1)) is NullReferenceException);
        Check(route + "-null-value", raw
            ? Error(() => query(null, null)) is ArgumentNullException argument && argument.ParamName == "value"
            : Error(() => query(null, null)) is NullReferenceException);
        Check(route + "-null-invalid", Error(() => query(null, "bad")) is NullReferenceException);
    }

    internal static void Run(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime enum defined ==");
        Route("public", Public); Nulls("public", Public, false);
        Console.WriteLine("Runtime enum defined end");
    }

    internal static void RunArtifact(string[] args)
    {
        Run(args);
        Route("direct", Direct); Nulls("direct", Direct, true);
        Route("calli", Pointer); Nulls("calli", Pointer, true);
        Route("virtual", Virtual); Nulls("virtual", Virtual, false);
        Route("virtual-calli", VirtualPointer); Nulls("virtual-calli", VirtualPointer, false);
        Route("constrained", Constrained); Nulls("constrained", Constrained, false);
        Route("address", AddressQuery);
        Route("virtual-address", VirtualAddressQuery); Nulls("virtual-address", VirtualAddressQuery, false);
        Route("flow-direct", FlowDirect);
        Route("flow-virtual", FlowVirtual); Nulls("flow-virtual", FlowVirtual, false);
        Check("address-bind-null", Error(() => Address(null)) is ArgumentException);
        Check("virtual-address-bind-null", Error(() => VirtualAddress(null)) is NullReferenceException);
        Check("flow-direct-bind-null", Error(() => FlowAddress(null, false)) is ArgumentException);
        Check("flow-virtual-bind-null", Error(() => FlowAddress(null, true)) is NullReferenceException);
        RunReflectionArtifact(Array.Empty<string>());
        if (args.Length > 0 && args[0] == "native-policy")
        {
            DelegateMetadata("address", Address(typeof(NameU1)));
            DelegateMetadata("virtual-address", VirtualAddress(typeof(NameU1)));
            DelegateMetadata("flow-direct", FlowAddress(typeof(NameU1), false));
            DelegateMetadata("flow-virtual", FlowAddress(typeof(NameU1), true));
        }
        Console.WriteLine("Runtime enum defined routes end");
    }

    private static void DelegateMetadata(string route, Func<object, bool> query)
    {
        MethodInfo method = query.Method;
        Check("native-" + route + "-row", method.Name == "IsEnumDefined" && method.ReturnType == typeof(bool)
            && !method.IsStatic && method.DeclaringType == ((object)typeof(int)).GetType()
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(object));
        Check("native-" + route + "-invoke", (bool)method.Invoke(typeof(NameU1), new object[] { (byte)1 })
            && !(bool)method.Invoke(typeof(NameU1), new object[] { (byte)2 })
            && Error(() => method.Invoke(typeof(int), new object[] { null })) is ArgumentNullException);
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime enum defined reflection ==");
        Route("reflection", Reflection); Nulls("reflection", Reflection, true);
        if (args.Length > 0 && args[0] == "native-policy")
        {
            MethodInfo method = typeof(EnumDefinedChecks).GetMethod(nameof(ReflectionLeaf), BindingFlags.Static | BindingFlags.NonPublic);
            Check("native-reflection-wrapper-row", method.Name == nameof(ReflectionLeaf)
                && method.DeclaringType == typeof(EnumDefinedChecks) && method.IsStatic);
            Check("native-runtime-lookup-unavailable", ((object)typeof(int)).GetType()
                .GetMethod("IsEnumDefined", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
        }
        Console.WriteLine("Runtime enum defined reflection end");
    }
}
