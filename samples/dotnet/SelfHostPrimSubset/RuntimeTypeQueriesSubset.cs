using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace RuntimeTypeQueriesSubset;

// Public RuntimeType queries over one shared type list. Each query reaches a
// retained private CoreLib helper or sealed RuntimeType override.
internal static unsafe class Program
{
    private enum SmallEnum : byte { Zero, One, Max = byte.MaxValue }
    private enum SignedEnum : sbyte { Neg = -1, Zero, One }
    [Flags] private enum WideEnum : ulong { None = 0, Low = 1, Max = ulong.MaxValue }
    private class Owner<T, U>
    {
        public class Nested<V> { }
        public static void Method<A>() { }
    }
    private struct Value { public int X; }
    private interface IMarker { }
    private interface IBase { }
    private interface IDerived : IBase { }
    private interface IOut<out T> { }
    private interface IIn<in T> { }
    private class Base { }
    private sealed class Child : Base, IDerived, IMarker { }
    private sealed class Producer : IOut<string> { }
    private sealed class Consumer : IIn<object> { }
    private class Open<T> : IOut<T>, IMarker { }
    private class Parameter<T> where T : Base, IOut<string> { }

    private static string Bit(bool value) => value ? "1" : "0";

    private static string Element(Type type)
    {
        Type element = type.GetElementType();
        return element is null ? "null" : element.IsFunctionPointer ? "fnptr" : element.Name;
    }

    private static string Shape(Type type) =>
        Bit(type.IsPointer) + Bit(type.IsPrimitive) + Bit(type.IsByRef) + Bit(type.IsArray) + Bit(type.IsSZArray)
        + Bit(type.HasElementType) + Bit(type.IsGenericParameter) + Bit(type.IsFunctionPointer)
        + Bit(type.IsUnmanagedFunctionPointer) + Bit(type.IsGenericTypeDefinition) + Bit(type.IsEnum)
        + " element=" + Element(type)
        + " rank=" + (type.IsArray ? type.GetArrayRank().ToString(CultureInfo.InvariantCulture) : "-")
        + " code=" + Type.GetTypeCode(type)
        + " position=" + (type.IsGenericParameter ? type.GenericParameterPosition.ToString(CultureInfo.InvariantCulture) : "-");

    private static string Result(Func<object> query)
    {
        try { return Convert.ToString(query(), CultureInfo.InvariantCulture) ?? "null"; }
        catch (Exception error) { return error.GetType().Name; }
    }

    private static void Shapes()
    {
        Type formal = typeof(Owner<,>).GetGenericArguments()[1];
        Type method = typeof(Owner<,>).GetMethod("Method").GetGenericArguments()[0];
        Type open = typeof(Owner<,>).MakeGenericType(typeof(int), formal);
        Type function = typeof(delegate*<int, void>);
        Type rankOne = Array.CreateInstance(typeof(int), new[] { 1 }, new[] { 1 }).GetType();
        var rows = new (string Label, Type Type)[]
        {
            ("object", typeof(object)), ("void", typeof(void)), ("decimal", typeof(decimal)), ("string", typeof(string)),
            ("bool", typeof(bool)), ("char", typeof(char)), ("sbyte", typeof(sbyte)), ("byte", typeof(byte)),
            ("short", typeof(short)), ("ushort", typeof(ushort)), ("int", typeof(int)), ("uint", typeof(uint)),
            ("long", typeof(long)), ("ulong", typeof(ulong)), ("float", typeof(float)), ("double", typeof(double)),
            ("nint", typeof(nint)), ("nuint", typeof(nuint)), ("enum", typeof(SmallEnum)), ("wide-enum", typeof(WideEnum)),
            ("typed-reference", typeof(TypedReference)), ("nullable", typeof(int?)), ("array-base", typeof(Array)),
            ("value", typeof(Value)), ("definition", typeof(Owner<,>)), ("nested-definition", typeof(Owner<,>.Nested<>)),
            ("closed-generic", typeof(Owner<int, string>)), ("open-constructed", open), ("type-formal", formal),
            ("method-formal", method), ("function-managed", function), ("function-unmanaged", typeof(delegate* unmanaged<int, void>)),
            ("byref-like", typeof(Span<int>)), ("byref-like-definition", typeof(Span<>)), ("array", typeof(int[])),
            ("reference-array", typeof(string[])), ("jagged-array", typeof(int[][])), ("mdarray-one", rankOne),
            ("mdarray-two", typeof(int[,])), ("pointer", typeof(int*)), ("void-pointer", typeof(void*)),
            ("pointer-pointer", typeof(int**)), ("pointer-array", typeof(int*[])), ("byref", typeof(int).MakeByRefType()),
            ("formal-array", formal.MakeArrayType()), ("formal-mdarray-one", formal.MakeArrayType(1)),
            ("formal-mdarray-two", formal.MakeArrayType(2)), ("formal-pointer", formal.MakePointerType()),
            ("formal-byref", formal.MakeByRefType()), ("method-array", method.MakeArrayType()),
            ("method-pointer", method.MakePointerType()), ("method-byref", method.MakeByRefType()),
            ("open-array", open.MakeArrayType()), ("nested-compound", formal.MakeArrayType().MakePointerType().MakeByRefType()),
            ("function-pointer", function.MakePointerType()), ("function-byref", function.MakeByRefType()),
            ("function-array", function.MakeArrayType()), ("array-byref", typeof(int[]).MakeByRefType()),
            ("reference-byref", typeof(string).MakeByRefType()), ("handle-array", Type.GetTypeFromHandle(typeof(int[]).TypeHandle)),
        };
        foreach (var row in rows)
            Console.WriteLine("query-" + row.Label + ":" + Shape(row.Type));
        Console.WriteLine("query-generic:" + Shape(Generic<int[]>()) + "|" + Shape(Generic<Owner<int, string>>()));
        Console.WriteLine("query-null-handle:" + (Type.GetTypeFromHandle(default) is null) + ":" + Type.GetTypeCode(null));
    }

    private static Type Generic<T>() => typeof(T);

    private static void Origins()
    {
        Type[] types = { typeof(int), typeof(string), typeof(Value), typeof(Owner<,>), typeof(Owner<int, string>),
            typeof(List<Value>), typeof(Value[]), typeof(System.Linq.Enumerable), typeof(Owner<,>).GetGenericArguments()[0] };
        foreach (Type type in types)
            Console.WriteLine("origin-" + type.Name + ":" + type.Assembly.GetName().Name + ":" + type.Module.Name
                + ":" + ReferenceEquals(type.Assembly, type.Module.Assembly));
        Console.WriteLine("origin-identity:" + ReferenceEquals(typeof(int).Assembly, typeof(string).Assembly)
            + ":" + ReferenceEquals(typeof(Value).Module, typeof(Program).Module)
            + ":" + (typeof(int).Assembly != typeof(Value).Assembly));
    }

    private static void Attributes()
    {
        Console.WriteLine("attributes-zero:" + (int)typeof(AttributesZero).Attributes);
        Console.WriteLine("attributes-public:" + (int)typeof(AttributesPublic).Attributes);
        Console.WriteLine("attributes-sealed:" + (int)typeof(AttributesSealed).Attributes);
        Console.WriteLine("attributes-abstract:" + (int)typeof(AttributesAbstract).Attributes);
        Console.WriteLine("attributes-interface:" + (int)typeof(IAttributesInterface).Attributes);
        Console.WriteLine("attributes-explicit:" + (int)typeof(AttributesExplicit).Attributes);
        Console.WriteLine("attributes-enum:" + (int)typeof(SmallEnum).Attributes);
        Console.WriteLine("attributes-generic:" + (int)typeof(Owner<int, string>).Attributes + ":" + (int)typeof(Owner<,>).Attributes);
    }

    private static void Enums()
    {
        Type[] types = { typeof(SmallEnum), typeof(SignedEnum), typeof(WideEnum), typeof(int) };
        object[] values = { null, 0, 1, (byte)1, (sbyte)-1, 1L, ulong.MaxValue, 256, SmallEnum.One, WideEnum.Max,
            "One", "Missing", true, 1d };
        for (int t = 0; t < types.Length; t++)
        {
            for (int v = 0; v < values.Length; v++)
            {
                Type type = types[t];
                object value = values[v];
                Console.WriteLine("enum-" + t + "-" + v + ":" + Result(() => type.GetEnumName(value))
                    + ":" + Result(() => type.IsEnumDefined(value)));
            }
        }
        Console.WriteLine("enum-names:" + string.Join(",", typeof(SignedEnum).GetEnumNames())
            + ":" + Enum.GetName(SmallEnum.Max) + ":" + Enum.IsDefined(WideEnum.Low));
    }

    private static void Assignability()
    {
        Type parameter = typeof(Parameter<>).GetGenericArguments()[0];
        Type openSource = typeof(Open<>).MakeGenericType(parameter);
        Type openTarget = typeof(IOut<>).MakeGenericType(parameter);
        var pairs = new (Type Target, Type Source)[]
        {
            (typeof(string), typeof(string)), (typeof(Base), typeof(Child)), (typeof(Child), typeof(Base)),
            (typeof(IBase), typeof(Child)), (typeof(IDerived), typeof(IBase)), (typeof(IMarker), typeof(object)),
            (typeof(IOut<object>), typeof(Producer)), (typeof(IIn<string>), typeof(Consumer)), (typeof(IIn<object>), typeof(Producer)),
            (typeof(object[]), typeof(string[])), (typeof(IEnumerable<object>), typeof(string[])), (typeof(IEnumerable), typeof(string[,])),
            (typeof(IEnumerable<string>), typeof(string[,])), (typeof(IEnumerable<uint>), typeof(int[])), (typeof(int?), typeof(int)),
            (typeof(int), typeof(int?)), (typeof(IOut<>), typeof(Open<>)), (typeof(IMarker), typeof(Open<>)), (openTarget, openSource),
            (typeof(Base), parameter), (typeof(IOut<string>), parameter), (typeof(object), null),
            (typeof(int).MakePointerType(), typeof(uint).MakePointerType()), (typeof(int).MakeByRefType(), typeof(uint).MakeByRefType()),
        };
        var results = new List<string>();
        foreach (var pair in pairs) results.Add(Bit(pair.Target.IsAssignableFrom(pair.Source)));
        Console.WriteLine("assignable:" + string.Join("", results));
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type queries ==");
        Shapes();
        Origins();
        Attributes();
        Enums();
        Assignability();
        Console.WriteLine("Runtime type queries end");
    }
}

internal class AttributesZero { static AttributesZero() { } }
public class AttributesPublic { }
internal sealed class AttributesSealed { }
internal abstract class AttributesAbstract { }
internal interface IAttributesInterface { }
[StructLayout(LayoutKind.Explicit)]
internal struct AttributesExplicit
{
    [FieldOffset(0)] public int Value;
}
