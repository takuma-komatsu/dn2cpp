using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PrimitiveElementTypeSubset;

internal static class Program
{
    private enum OrdinaryEnum : byte { Value = 2 }
    private struct OrdinaryStruct { }

    private static void Check(string label, bool value)
    {
        Console.WriteLine("primitive-element-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Primitive element type contract ==");
        Check("static-primitive", typeof(int).IsPrimitive && typeof(nint).IsPrimitive && typeof(nuint).IsPrimitive);
        Check("static-nonprimitive", !typeof(TypedReference).IsPrimitive
            && !typeof(OrdinaryEnum).IsPrimitive && !typeof(decimal).IsPrimitive);
        Type[] types = { typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(nint), typeof(nuint),
            typeof(void), typeof(TypedReference), typeof(string), typeof(object), typeof(decimal), typeof(OrdinaryEnum),
            typeof(OrdinaryStruct), typeof(int?), typeof(int[]), typeof(int).MakePointerType(), typeof(int).MakeByRefType() };
        string bits = "";
        foreach (Type type in types) bits += type.IsPrimitive ? "1" : "0";
        Check("public-types", bits == "1111111111111100000000000");
        Console.WriteLine("Primitive element type contract end");
    }
}
