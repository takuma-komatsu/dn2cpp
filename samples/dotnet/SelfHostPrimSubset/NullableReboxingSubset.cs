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

    private static string s_label;

    private static void Check(string name, bool success)
    {
        Console.WriteLine("nullable-rebox-" + s_label + "-" + name + ":" + success);
        if (!success) throw new InvalidOperationException(name);
    }

    public static int? Return(bool hasValue) => hasValue ? 42 : null;
    public static void Copy(ref int? value, bool hasValue) => value = hasValue ? 43 : null;
    public static void CopyFloat(ref float? value) { }
    public static float? EchoFloat(float? value) => value;
    public static void CopyPair<T>(ref Pair<T>? value, T item) => value = new Pair<T> { Item = item, Number = 99 };

    private static void RunCases(string label)
    {
        s_label = label;
        int[] bits = { unchecked((int)0x80000000), 0x7f800001, 0x7fc12345, unchecked((int)0xff812345) };
        for (int i = 0; i < bits.Length; i++)
        {
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
        RunCases("reflection");
        Console.WriteLine("Nullable reboxing contract end");
    }
}
