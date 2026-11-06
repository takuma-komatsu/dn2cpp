using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NullableReboxingSubset;

internal static class Program
{
    public struct Pair<T>
    {
        public T Item;
        public long Number;
    }

    public enum Small : byte { Max = 255 }
    public sealed class Payload
    {
        public string Text = "retained";
        public int Number = 73;
    }

    public sealed class CopyConstructor
    {
        public CopyConstructor(ref int? value, bool hasValue) => value = hasValue ? 45 : null;
    }

    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly MethodInfo From = typeof(RuntimeMethodHandle).GetMethod("ReboxFromNullable", Flags);
    private static readonly MethodInfo To = typeof(RuntimeMethodHandle).GetMethod("ReboxToNullable", Flags);
    private static int s_route;
    private static string s_label;

    private static object FromReflection(object value) => typeof(RuntimeMethodHandle)
        .GetMethod("ReboxFromNullable", Flags).Invoke(null, new object[] { value });
    private static object ToReflection(object value, Type type) => typeof(RuntimeMethodHandle)
        .GetMethod("ReboxToNullable", Flags).Invoke(null, new object[] { value, type });

    // The native fixture replaces these calls with exact CoreLib direct/calli sites.
    private static object FromDirect(object value) => FromReflection(value);
    private static object FromPointer(object value) => FromReflection(value);
    private static object ToDirect(object value, Type type) => ToReflection(value, type);
    private static object ToPointer(object value, Type type) => ToReflection(value, type);

    private static object Normalize(object value) => s_route == 1 ? FromDirect(value)
        : s_route == 2 ? FromPointer(value) : FromReflection(value);
    private static object Rebox(object value, Type type) => s_route == 1 ? ToDirect(value, type)
        : s_route == 2 ? ToPointer(value, type) : ToReflection(value, type);

    private static void Check(string name, bool success)
    {
        Console.WriteLine("nullable-rebox-" + s_label + "-" + name + ":" + success);
        if (!success) throw new InvalidOperationException(name);
    }

    private static void CheckValue<T>(string label, T value) where T : struct
    {
        object boxed = value;
        object raw = Rebox(boxed, typeof(T?));
        object back = Normalize(raw);
        Check(label + "-raw", raw.GetType() == typeof(T?));
        Check(label + "-copy", !ReferenceEquals(boxed, back) && !ReferenceEquals(raw, back));
        Check(label + "-value", value.Equals(back));
        object empty = Rebox(null, typeof(T?));
        Check(label + "-empty", empty.GetType() == typeof(T?) && Normalize(empty) is null);
    }

    public static int? Return(bool hasValue) => hasValue ? 42 : null;
    public static void Copy(ref int? value, bool hasValue) => value = hasValue ? 43 : null;
    public static void CopyFloat(ref float? value) { }
    public static float? EchoFloat(float? value) => value;
    public static void CopyPair<T>(ref Pair<T>? value, T item) => value = new Pair<T> { Item = item, Number = 99 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object RetainRaw() => Rebox(new Pair<Payload> { Item = new Payload(), Number = 19 }, typeof(Pair<Payload>?));

    private static void RunCases(string label)
    {
        s_label = label;
        Check("null-identity", Normalize(null) is null);
        object number = 42;
        object text = "reference";
        object instance = new object();
        Check("int-identity", ReferenceEquals(number, Normalize(number)));
        Check("string-identity", ReferenceEquals(text, Normalize(text)));
        Check("object-identity", ReferenceEquals(instance, Normalize(instance)));
        CheckValue("byte", (byte)255);
        CheckValue("sbyte", (sbyte)-128);
        CheckValue("ulong", ulong.MaxValue);
        CheckValue("enum", Small.Max);
        CheckValue("decimal", 123.25m);
        CheckValue("generic-ref", new Pair<string> { Item = "payload", Number = 19 });
        CheckValue("generic-value", new Pair<long> { Item = long.MinValue, Number = 23 });
        int[] bits = { unchecked((int)0x80000000), 0x7f800001, 0x7fc12345, unchecked((int)0xff812345) };
        for (int i = 0; i < bits.Length; i++)
        {
            object raw = Rebox(BitConverter.Int32BitsToSingle(bits[i]), typeof(float?));
            float result = (float)Normalize(raw);
            Check("float-bits-" + i, BitConverter.SingleToInt32Bits(result) == bits[i]);
            object[] floatArgs = { BitConverter.Int32BitsToSingle(bits[i]) };
            typeof(Program).GetMethod("CopyFloat").Invoke(null, floatArgs);
            Check("public-float-copyback-" + i, BitConverter.SingleToInt32Bits((float)floatArgs[0]) == bits[i]);
            object echoed = typeof(Program).GetMethod("EchoFloat").Invoke(null,
                new object[] { BitConverter.Int32BitsToSingle(bits[i]) });
            Check("public-float-echo-" + i, BitConverter.SingleToInt32Bits((float)echoed) == bits[i]);
            Array nullableArray = new float?[1];
            nullableArray.SetValue(BitConverter.Int32BitsToSingle(bits[i]), 0);
            Check("array-float-bits-" + i, BitConverter.SingleToInt32Bits((float)nullableArray.GetValue(0)) == bits[i]);
            nullableArray.SetValue(null, 0);
            Check("array-float-empty-" + i, nullableArray.GetValue(0) is null);
        }
        object source = new Pair<string> { Item = "before", Number = 1 };
        object copy = Rebox(source, typeof(Pair<string>?));
        typeof(Pair<string>).GetField("Item").SetValue(source, "after");
        Check("raw-copy-independent", ((Pair<string>)Normalize(copy)).Item == "before");
        object retained = RetainRaw();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Pair<Payload> pair = (Pair<Payload>)Normalize(retained);
        Check("gc-payload", pair.Item.Text == "retained" && pair.Item.Number == 73 && pair.Number == 19);
        GC.KeepAlive(retained);
        for (int i = 0; i < 2; i++)
        {
            bool hasValue = i != 0;
            object result = typeof(Program).GetMethod("Return").Invoke(null, new object[] { hasValue });
            Check("public-return-" + i, hasValue ? (int)result == 42 : result is null);
            object[] args = { 42, hasValue };
            typeof(Program).GetMethod("Copy").Invoke(null, args);
            Check("public-copyback-" + i, hasValue ? (int)args[0] == 43 : args[0] is null);
            args = new object[] { null, hasValue };
            typeof(CopyConstructor).GetConstructors()[0].Invoke(args);
            Check("constructor-copyback-" + i, hasValue ? (int)args[0] == 45 : args[0] is null);
        }
        object item = new object();
        Pair<object>? seed = null;
        CopyPair(ref seed, item);
        object[] genericArgs = { null, item };
        typeof(Program).GetMethod("CopyPair").MakeGenericMethod(typeof(object)).Invoke(null, genericArgs);
        Pair<object> copied = (Pair<object>)genericArgs[0];
        Check("public-generic-copyback", ReferenceEquals(copied.Item, item) && copied.Number == 99);
        Console.WriteLine("Nullable reboxing " + label + " end");
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Nullable reboxing contract ==");
        s_route = 0;
        s_label = "reflection";
        Check("from-shape", From.IsStatic && From.IsAssembly && !From.IsGenericMethod
            && From.ReturnType == typeof(object) && From.GetParameters().Length == 1
            && From.GetParameters()[0].ParameterType == typeof(object));
        Check("to-shape", To.IsStatic && To.IsAssembly && !To.IsGenericMethod
            && To.ReturnType == typeof(object) && To.GetParameters().Length == 2
            && To.GetParameters()[0].ParameterType == typeof(object)
            && To.GetParameters()[1].ParameterType.FullName == "System.RuntimeType");
        RunCases("reflection");
        Console.WriteLine("Nullable reboxing contract end");
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        s_route = 1;
        RunCases("direct");
        s_route = 2;
        RunCases("pointer");
        Console.WriteLine("Nullable reboxing artifact end");
    }
}
