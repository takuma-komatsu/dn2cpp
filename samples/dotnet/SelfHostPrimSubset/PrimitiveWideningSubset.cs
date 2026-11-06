using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PrimitiveWideningSubset;

internal static class Program
{
    private static void Check(string label, bool value)
    {
        Console.WriteLine("primitive-widening-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Primitive widening contract ==");
        int[] integers = new int[1];
        ((Array)integers).SetValue((byte)7, 0);
        Check("public-widen", integers[0] == 7);
        ((Array)integers).SetValue(9, 0);
        Check("public-identity", integers[0] == 9);
        bool refused = false;
        try { ((Array)integers).SetValue(3L, 0); }
        catch (ArgumentException) { refused = true; }
        catch (InvalidCastException) { refused = true; }
        Check("public-reject", refused && integers[0] == 9);
        Console.WriteLine("Primitive widening contract end");
    }

    // The CLR oracle uses a local RVA/public-span copy; native routes use the input helper.
    private static bool CopiedPredicate(byte source, byte target) => throw new InvalidOperationException("fixture missing");
    private static bool Direct(byte source, byte target) => throw new InvalidOperationException("fixture missing");
    private static bool Pointer(byte source, byte target) => throw new InvalidOperationException("fixture missing");
    private static bool DirectStack(int source, int target) => throw new InvalidOperationException("fixture missing");
    private static bool PointerStack(int source, int target) => throw new InvalidOperationException("fixture missing");

    private static bool Reflection(byte source, byte target)
    {
        MethodInfo method = typeof(RuntimeHelpers).GetMethod("CanPrimitiveWiden", BindingFlags.NonPublic | BindingFlags.Static);
        Type elementType = method.GetParameters()[0].ParameterType;
        object from = Enum.ToObject(elementType, source);
        object to = Enum.ToObject(elementType, target);
        return (bool)method.Invoke(null, new object[] { from, to });
    }

    private static void RunRoute(string route, Func<byte, byte, bool> predicate)
    {
        int count = 0;
        int pairs = 0;
        uint hash = 2166136261u;
        for (int source = 0; source < 256; source++)
        {
            string bits = "";
            for (int target = 0; target < 256; target++)
            {
                bool value = predicate((byte)source, (byte)target);
                bits += value ? "1" : "0";
                if (value) count++;
                pairs++;
                hash = unchecked((hash ^ (value ? 1u : 0u)) * 16777619u);
            }
            Console.WriteLine("primitive-widening-" + route + "-row-" + source + ":" + bits);
        }
        Console.WriteLine("primitive-widening-" + route + "-pairs:" + pairs);
        Console.WriteLine("primitive-widening-" + route + "-true:" + count);
        Console.WriteLine("primitive-widening-" + route + "-hash:" + hash);
        byte[] from = { 0, 1, 2, 2, 2, 2, 3, 3, 5, 13, 13, 14, 14, 24, 24, 255, 255 };
        byte[] to = { 0, 1, 2, 34, 226, 255, 4, 7, 3, 13, 45, 14, 46, 24, 56, 255, 31 };
        bool[] expected = { false, false, true, true, true, false, false, true, true,
            true, true, true, false, true, false, true, false };
        bool edges = true;
        for (int i = 0; i < from.Length; i++)
        {
            bool value = predicate(from[i], to[i]);
            edges &= value == expected[i];
        }
        Check(route + "-edges", edges);
    }

    internal static void RunArtifact(string[] args)
    {
        Run();
        NarrowEnumArgumentSubset.EnumArguments451.Run();
        RunRoute("direct", Direct);
        RunRoute("calli", Pointer);
        int[] values = { int.MinValue, int.MinValue + 2, -258, -256, -254, -242, -1,
            0, 1, 2, 13, 14, 24, 31, 32, 34, 127, 128, 255, 256, 258, 270, 65536, 65538, int.MaxValue };
        bool directSame = true;
        bool pointerSame = true;
        foreach (int source in values)
            foreach (int target in values)
            {
                bool expected = Direct(unchecked((byte)source), unchecked((byte)target));
                directSame &= DirectStack(source, target) == expected;
                pointerSame &= PointerStack(source, target) == expected;
            }
        Check("direct-stack-normalization", directSame);
        Check("calli-stack-normalization", pointerSame);
        RunReflectionArtifact(args);
        Console.WriteLine("Primitive widening routes end");
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        MethodInfo method = typeof(RuntimeHelpers).GetMethod("CanPrimitiveWiden", BindingFlags.NonPublic | BindingFlags.Static);
        ParameterInfo[] parameters = method.GetParameters();
        Check("reflection-row", method.DeclaringType == typeof(RuntimeHelpers) && method.IsStatic && method.IsAssembly
            && method.ReturnType == typeof(bool) && parameters.Length == 2
            && parameters[0].ParameterType.FullName == "System.Reflection.CorElementType"
            && parameters[0].ParameterType == parameters[1].ParameterType);
        RunRoute("reflection", Reflection);
        Console.WriteLine("Primitive widening reflection end");
    }
}
