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

    // The fixture copies inspected token-free IL for the CLR oracle; native routes use the real helper.
    private static bool CopiedPredicate(byte code) => throw new InvalidOperationException("fixture missing");
    private static bool Direct(byte code) => throw new InvalidOperationException("fixture missing");
    private static bool Pointer(byte code) => throw new InvalidOperationException("fixture missing");
    private static bool DirectStack(int code) => throw new InvalidOperationException("fixture missing");

    private static bool Reflection(byte code)
    {
        MethodInfo method = typeof(RuntimeHelpers).GetMethod("IsPrimitiveType", BindingFlags.NonPublic | BindingFlags.Static);
        object value = Enum.ToObject(method.GetParameters()[0].ParameterType, code);
        return (bool)method.Invoke(null, new object[] { value });
    }

    private static void RunRoute(string route, Func<byte, bool> predicate)
    {
        string bits = "";
        int count = 0;
        for (int i = 0; i < 256; i++)
        {
            bool value = predicate((byte)i);
            bits += value ? "1" : "0";
            if (value) count++;
        }
        Console.WriteLine("primitive-element-" + route + "-bits:" + bits);
        Check(route + "-count", count == 112);
        Check(route + "-aliases", predicate(2) && predicate(34) && predicate(130) && predicate(226)
            && !predicate(1) && !predicate(33) && !predicate(129) && !predicate(255));
    }

    internal static void RunArtifact(string[] args)
    {
        Run();
        RunRoute("direct", Direct);
        RunRoute("calli", Pointer);
        int[] values = { int.MinValue, int.MinValue + 2, -33, -32, -31, -30, -8, -7, -1,
            0, 1, 2, 31, 32, 34, 127, 128, 130, 255, 256, 258, int.MaxValue };
        bool same = true;
        foreach (int value in values) same &= DirectStack(value) == Direct(unchecked((byte)value));
        Check("stack-normalization", same);
        RunReflectionArtifact(args);
        Console.WriteLine("Primitive element type routes end");
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        MethodInfo method = typeof(RuntimeHelpers).GetMethod("IsPrimitiveType", BindingFlags.NonPublic | BindingFlags.Static);
        Check("reflection-row", method.DeclaringType == typeof(RuntimeHelpers) && method.IsStatic && method.IsAssembly
            && method.ReturnType == typeof(bool) && method.GetParameters().Length == 1
            && method.GetParameters()[0].ParameterType.FullName == "System.Reflection.CorElementType");
        RunRoute("reflection", Reflection);
        Console.WriteLine("Primitive element type reflection end");
    }
}
