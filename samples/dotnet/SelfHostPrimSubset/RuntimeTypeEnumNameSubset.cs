using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeEnumNameSubset;

internal static class EnumNameChecks
{
    private enum NameI1 : sbyte { Min = sbyte.MinValue, Neg = -1, Zero = 0, One = 1, Max = sbyte.MaxValue }
    private enum NameU1 : byte { Zero = 0, One = 1, Max = byte.MaxValue }
    private enum NameI2 : short { Min = short.MinValue, Neg = -1, Zero = 0, One = 1, Max = short.MaxValue }
    private enum NameU2 : ushort { Zero = 0, One = 1, Max = ushort.MaxValue }
    private enum NameI4 : int { Min = int.MinValue, Neg = -1, Zero = 0, One = 1, Max = int.MaxValue }
    private enum NameU4 : uint { Zero = 0, One = 1, Max = uint.MaxValue }
    private enum NameI8 : long { Min = long.MinValue, Neg = -1, Zero = 0, One = 1, Max = long.MaxValue }
    private enum NameU8 : ulong { Zero = 0, One = 1, Max = ulong.MaxValue }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("enum-name-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static string Public(Type type, object value) => type.GetEnumName(value);
    // The managed override validates value before touching this; no private VM is called by the oracle.
    private static string RawOracle(Type type, object value)
    {
        if (type is not null) return Public(type, value);
        if (value is null) throw new ArgumentNullException("value");
        Type actual = value.GetType();
        if (!actual.IsEnum && actual != typeof(sbyte) && actual != typeof(byte)
            && actual != typeof(short) && actual != typeof(ushort) && actual != typeof(int)
            && actual != typeof(uint) && actual != typeof(long) && actual != typeof(ulong) && actual != typeof(char))
            throw new ArgumentException("Value must be an enum or integral value.", "value");
        throw new NullReferenceException();
    }
    private static string Direct(Type type, object value) => RawOracle(type, value);
    private static string Pointer(Type type, object value) => RawOracle(type, value);
    private static string Virtual(Type type, object value) => Public(type, value);
    private static string VirtualPointer(Type type, object value) => Public(type, value);
    private static string Constrained(Type type, object value) => Public(type, value);
    private static Func<object, string> Address(Type type)
    {
        if (type is null) throw new ArgumentException();
        return value => RawOracle(type, value);
    }
    private static Func<object, string> VirtualAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return value => RawOracle(type, value);
    }
    private static Func<object, string> FlowAddress(Type type, bool virt) => virt ? VirtualAddress(type) : Address(type);
    private static string AddressQuery(Type type, object value) => Address(type)(value);
    private static string VirtualAddressQuery(Type type, object value) => VirtualAddress(type)(value);
    private static string FlowDirect(Type type, object value) => FlowAddress(type, false)(value);
    private static string FlowVirtual(Type type, object value) => FlowAddress(type, true)(value);
    private static string ReflectionLeaf(Type type, object value) => RawOracle(type, value);
    private static string Reflection(Type type, object value) => (string)typeof(EnumNameChecks)
        .GetMethod(nameof(ReflectionLeaf), BindingFlags.Static | BindingFlags.NonPublic)
        .Invoke(null, new object[] { type, value });

    private static string Result(Func<Type, object, string> query, Type type, object value)
    {
        try { return query(type, value) ?? "null"; }
        catch (TargetInvocationException error) { return Failure(error.InnerException); }
        catch (Exception error) { return Failure(error); }
    }
    private static string Failure(Exception error) => error.GetType().Name
        + (error is ArgumentException argument ? "/" + (argument.ParamName ?? "<null>") : "")
        + "/" + error.HResult.ToString("X8");

    private static void Route(string route, Func<Type, object, string> query)
    {
        Type[] types = { typeof(NameI1), typeof(NameU1), typeof(NameI2), typeof(NameU2), typeof(NameI4),
            typeof(NameU4), typeof(NameI8), typeof(NameU8), typeof(int), typeof(Enum) };
        object[] values = { null, (sbyte)-1, byte.MaxValue, (short)-1, ushort.MaxValue, -1, uint.MaxValue,
            -1L, ulong.MaxValue, 0, 1, 256, 257, long.MinValue, long.MaxValue, (char)1,
            NameI1.Min, NameU1.Max, NameI2.Min, NameU2.Max, NameI4.Min, NameU4.Max, NameI8.Min, NameU8.Max,
            true, (nint)1, (nuint)1, 1f, 1d, 1m, "One", new object() };
        for (int t = 0; t < types.Length; t++)
            for (int v = 0; v < values.Length; v++)
                Console.WriteLine("enum-name-" + route + "-" + t + "-" + v + ":" + Result(query, types[t], values[v]));
        Check(route + "-range", query(typeof(NameU1), 256) is null && query(typeof(NameU1), 257) is null);
        Check(route + "-wide", query(typeof(NameI1), ulong.MaxValue) == "Neg"
            && query(typeof(NameU8), ulong.MaxValue) == "Max" && query(typeof(NameI8), long.MinValue) == "Min");
        Check(route + "-done", true);
    }

    private static void Nulls(string route, Func<Type, object, string> query, bool raw)
    {
        Check(route + "-null-valid", Error(() => query(null, 1)) is NullReferenceException);
        Check(route + "-null-value", raw
            ? Error(() => query(null, null)) is ArgumentNullException argument && argument.ParamName == "value"
            : Error(() => query(null, null)) is NullReferenceException);
        Check(route + "-null-invalid", raw
            ? Error(() => query(null, "bad")) is ArgumentException invalid && invalid.ParamName == "value"
            : Error(() => query(null, "bad")) is NullReferenceException);
    }

    internal static void Run(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime enum name ==");
        Route("public", Public); Nulls("public", Public, false);
        Console.WriteLine("Runtime enum name end");
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
        Console.WriteLine("Runtime enum name routes end");
    }

    private static void DelegateMetadata(string route, Func<object, string> query)
    {
        MethodInfo method = query.Method;
        Check("native-" + route + "-row", method.Name == "GetEnumName" && method.ReturnType == typeof(string)
            && !method.IsStatic && method.DeclaringType == ((object)typeof(int)).GetType()
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(object));
        Check("native-" + route + "-invoke", (string)method.Invoke(typeof(NameU1), new object[] { 1 }) == "One"
            && method.Invoke(typeof(NameU1), new object[] { 256 }) is null
            && Error(() => method.Invoke(typeof(int), new object[] { null })) is ArgumentNullException);
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime enum name reflection ==");
        Route("reflection", Reflection); Nulls("reflection", Reflection, true);
        if (args.Length > 0 && args[0] == "native-policy")
        {
            MethodInfo method = typeof(EnumNameChecks).GetMethod(nameof(ReflectionLeaf), BindingFlags.Static | BindingFlags.NonPublic);
            Check("native-reflection-wrapper-row", method.Name == nameof(ReflectionLeaf)
                && method.DeclaringType == typeof(EnumNameChecks) && method.IsStatic);
            Check("native-runtime-lookup-unavailable", ((object)typeof(int)).GetType()
                .GetMethod("GetEnumName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
        }
        Console.WriteLine("Runtime enum name reflection end");
    }
}
