using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ArrayNativeLengthWitness;

internal static class Program
{
    private static int receiverCount;

    private static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "reflection-only") ReflectionOnlyMain();
        else DirectMain();
    }

    private static void DirectMain()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Array.NativeLength direct and calli ==");
        Shape("sz-int", new int[] { 3, 7, 11 }, 3);
        Shape("sz-reference", new object[] { new object(), "x", new object(), "y" }, 4);
        Shape("sz-value", new Payload[] { new(5), new(9) }, 2);
        Shape("sz-empty", Array.Empty<byte>(), 0);
        Shape("md-two", new int[2, 3], 6);
        Shape("md-three", new object[2, 3, 4], 24);
        Shape("md-empty", new int[0, 7], 0);
        Shape("md-nonzero-lower", Array.CreateInstance(typeof(int), new[] { 2, 3 }, new[] { -2, 5 }), 6);
        Shape("rank-one-nonzero-lower", Array.CreateInstance(typeof(object), new[] { 5 }, new[] { -3 }), 5);
        Shape("nonzero-lower-empty", Array.CreateInstance(typeof(int), new[] { 0, 4 }, new[] { 7, -2 }), 0);
        Null(false); Null(true);
        Check(UIntPtr.Size == IntPtr.Size && (UIntPtr.Size == 4 || UIntPtr.Size == 8), "native result width");
        Pass("native-width=" + UIntPtr.Size);
        Console.WriteLine("Array.NativeLength direct and calli end");
    }

    private static void Shape(string label, Array value, nuint expected)
    {
        receiverCount = 0;
        nuint direct = Direct(Receiver(value));
        Check(receiverCount == 1 && direct == expected, label + " direct and evaluation");
        receiverCount = 0;
        nuint address = Calli(Receiver(value));
        Check(receiverCount == 1 && address == expected && address == direct, label + " calli and evaluation");
        Check((ulong)direct == (ulong)value.LongLength, label + " unsigned native result");
        Pass(label + ":length=" + direct + ";receiver-once");
    }

    private static void Null(bool address)
    {
        receiverCount = 0; bool threw = false;
        try
        {
            if (address) _ = Calli(Receiver(null!)); else _ = Direct(Receiver(null!));
        }
        catch (NullReferenceException) { threw = true; }
        Check(threw && receiverCount == 1, "null receiver and single evaluation");
        Pass(address ? "null-calli:NullReferenceException;receiver-once" : "null-direct:NullReferenceException;receiver-once");
    }

    private static void ReflectionOnlyMain()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Array.NativeLength named reflection only ==");
        var method = NamedMethod();
        Check(method.DeclaringType == typeof(Array) && !method.IsStatic && method.ReturnType == typeof(UIntPtr)
            && method.GetParameters().Length == 0, "genuine named declaration");
        receiverCount = 0;
        Check(Named(Receiver(new int[7])) == 7 && receiverCount == 1, "named SZ result and receiver evaluation");
        Pass("named-sz:7;receiver-once");
        Check(Named(new int[2, 3]) == 6, "named MD result"); Pass("named-md:6");
        Check(Named(new int[0, 3]) == 0, "named empty result"); Pass("named-empty:0");
        bool threw = false;
        try { _ = Named(null!); } catch (TargetException) { threw = true; }
        Check(threw, "reflection binder rejects null instance receiver"); Pass("named-null:TargetException");
        Console.WriteLine("Array.NativeLength named reflection only end");
    }

    private static MethodInfo NamedMethod() => typeof(Array).GetMethod("get_NativeLength",
        BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)
        ?? throw new MissingMethodException("System.Array.get_NativeLength");

    private static nuint Named(Array receiver)
    {
        var method = typeof(Array).GetMethod("get_NativeLength", BindingFlags.Instance | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null) ?? throw new MissingMethodException("System.Array.get_NativeLength");
        return (nuint)method.Invoke(receiver, null)!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Array Receiver(Array value) { receiverCount++; return value; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint Direct(Array value) => Oracle(value, false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint Calli(Array value) => Oracle(value, true);

    private static nuint Oracle(Array value, bool address)
    {
        var target = typeof(Array).GetMethod("get_NativeLength", BindingFlags.Instance | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null) ?? throw new MissingMethodException("System.Array.get_NativeLength");
        var method = new DynamicMethod("ActualArrayNativeLength", typeof(nuint), new[] { typeof(Array) }, typeof(Program).Module, true);
        var il = method.GetILGenerator(); il.Emit(OpCodes.Ldarg_0);
        if (address)
        {
            il.Emit(OpCodes.Ldftn, target);
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard | CallingConventions.HasThis, typeof(nuint), Type.EmptyTypes, null);
        }
        else il.Emit(OpCodes.Call, target);
        il.Emit(OpCodes.Ret);
        return ((Func<Array, nuint>)method.CreateDelegate(typeof(Func<Array, nuint>)))(value);
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }
    private static void Pass(string label) => Console.WriteLine("array-native-length:" + label + ":pass");
    private static void UnusedDietSentinel() { }
}

internal readonly struct Payload
{
    internal readonly int Value;
    internal Payload(int value) { Value = value; }
}
