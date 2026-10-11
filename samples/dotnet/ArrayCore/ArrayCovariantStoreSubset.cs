using System;
using System.Collections.Generic;
using System.Globalization;

namespace ArrayCovariantStoreSubset;

internal static class Program
{
    private interface ITag { }
    private class Animal { }
    private sealed class Dog : Animal, ITag { }
    private sealed class Cat : Animal { }
    private sealed class Box<T> { }
    private interface IOut<out T> { }
    private sealed class Out<T> : IOut<T> { }
    private static object[] s_values;
    private static object[,] s_mdValues;

    private static object[] Returned() => s_values;
    private static void StoreArgument(object[] values, object value) => values[0] = value;
    private static void StoreGeneric<T>(T[] values, T value) where T : class => values[0] = value;
    private static T[] Allocate<T>(int length) => new T[length];
    private static Type ArrayType<T>() => typeof(T[]);
    private static T[] CastArray<T>(object value) => (T[])value;
    private static void StoreMdArgument(object[,] values, object value) => values[0, 0] = value;
    private static void StoreMdGeneric<T>(T[,] values, T value) => values[0, 0] = value;
    private static string ReadMdCulture(ref CultureInfo value) => value.Name;
    private static void WriteMdCulture(ref CultureInfo value, CultureInfo replacement) => value = replacement;

    private static void Observe(string label, object[] values, Action store, object expected, bool message = false)
    {
        string result = "ok";
        try { store(); }
        catch (Exception exception)
        {
            result = exception.GetType().Name + "/" + exception.HResult.ToString("X8");
            if (message) Console.WriteLine("store message=" + exception.Message);
        }
        bool retained = values is null || values.Length == 0 || ReferenceEquals(values[0], expected);
        Console.WriteLine(label + "=" + result + ":identity=" + retained);
    }

    internal static void Run()
    {
        Console.WriteLine("== covariant reference array stores ==");
        object seed = "seed", next = "next", bad = new object();
        object[] strings = new string[] { (string)seed };
        Observe("string rejected", strings, () => strings[0] = bad, seed, true);
        Observe("string compatible", strings, () => strings[0] = next, next);
        Observe("string null", strings, () => strings[0] = null, null);
        strings[0] = seed;
        int negative = -1;
        Observe("negative before type", strings, () => strings[negative] = bad, seed);
        Observe("past end before type", strings, () => strings[1] = bad, seed);
        object[] missing = null;
        Observe("null before type", missing, () => missing[0] = bad, null);
        Observe("argument rejected", strings, () => StoreArgument(strings, bad), seed);
        s_values = new string[] { (string)seed };
        Observe("field rejected", s_values, () => s_values[0] = bad, seed);
        Observe("return rejected", s_values, () => Returned()[0] = bad, seed);
        Observe("shared rejected", strings, () => StoreGeneric<object>(strings, bad), seed);
        Observe("shared compatible", strings, () => StoreGeneric<string>((string[])strings, (string)next), next);

        var dog = new Dog();
        Animal[] animals = new Dog[] { dog };
        Observe("class rejected", animals, () => animals[0] = new Cat(), dog);
        var dog2 = new Dog();
        Observe("class compatible", animals, () => animals[0] = dog2, dog2);
        ITag[] tags = new Dog[] { dog };
        Observe("interface narrowed rejected", tags, () => ((object[])tags)[0] = new Cat(), dog);
        Observe("interface narrowed compatible", tags, () => tags[0] = dog2, dog2);
        object[] interfaces = new ITag[1];
        Observe("interface compatible", interfaces, () => interfaces[0] = dog, dog);
        Observe("interface rejected", interfaces, () => interfaces[0] = bad, dog);
        object[] comparable = new IComparable[1];
        object boxed = 7;
        Observe("boxed interface compatible", comparable, () => comparable[0] = boxed, boxed);
        object[] objects = new object[1];
        Observe("object boxed compatible", objects, () => objects[0] = boxed, boxed);
        Observe("object compatible", objects, () => objects[0] = bad, bad);
        Observe("object null", objects, () => objects[0] = null, null);

        object[] generic = new Box<string>[1];
        var exact = new Box<string>();
        Observe("generic compatible", generic, () => generic[0] = exact, exact);
        Observe("generic invariant rejected", generic, () => generic[0] = new Box<object>(), exact);
        object[] variance = new IOut<Animal>[1];
        var covariant = new Out<Dog>();
        Observe("generic variance compatible", variance, () => variance[0] = covariant, covariant);
        Observe("generic variance rejected", variance, () => variance[0] = new Out<object>(), covariant);
        object[] jagged = new string[1][];
        var leaf = new string[] { "leaf" };
        Observe("jagged compatible", jagged, () => jagged[0] = leaf, leaf);
        Observe("jagged rejected", jagged, () => jagged[0] = new object[1], leaf);
        object[] types = new Type[1];
        object type = typeof(string);
        Observe("intrinsic compatible", types, () => types[0] = type, type);
        Observe("intrinsic rejected", types, () => types[0] = bad, type);
        object[] split = "left,right".Split(',');
        object first = split[0];
        Observe("runtime returned rejected", split, () => split[0] = bad, first);
        IList<object> list = new string[] { (string)seed };
        Observe("generic list rejected", (object[])list, () => list[0] = bad, seed);
        Observe("generic list compatible", (object[])list, () => list[0] = next, next);
        RunArrayElements();
        var constructed = new ArrayTypeMismatchException();
        Console.WriteLine("constructed mismatch=" + constructed.Message + "/" + constructed.HResult.ToString("X8"));
        Console.WriteLine("covariant reference array stores end");
    }

    private static void RunArrayElements()
    {
        var bytes = new byte[] { 1, 2 };
        var ints = new int[] { 3, 4 };
        var strings = new string[] { "leaf" };
        var byteArrays = Allocate<byte[]>(1);
        var intArrays = Allocate<int[]>(1);
        var stringArrays = Allocate<string[]>(1);
        Observe("allocated byte array compatible", byteArrays, () => byteArrays[0] = bytes, bytes);
        Observe("allocated int array compatible", intArrays, () => intArrays[0] = ints, ints);
        Observe("allocated reference array compatible", stringArrays, () => stringArrays[0] = strings, strings);
        object[] erased = byteArrays;
        Observe("allocated array element rejected", erased, () => erased[0] = ints, bytes);
        var deep = Allocate<byte[][]>(1);
        Observe("allocated nested array compatible", deep, () => deep[0] = byteArrays, byteArrays);
        var objects = Allocate<object>(1);
        Observe("allocated object compatible", objects, () => objects[0] = bytes, bytes);
        Console.WriteLine("allocated array identities="
            + (byteArrays.GetType() == typeof(byte[][])) + ":"
            + (intArrays.GetType() == typeof(int[][])) + ":"
            + (stringArrays.GetType() == typeof(string[][])) + ":"
            + (deep.GetType() == typeof(byte[][][])));
        Console.WriteLine("array token identities="
            + (ArrayType<byte[]>() == typeof(byte[][])) + ":"
            + (ArrayType<int[]>() == typeof(int[][])) + ":"
            + (ArrayType<string[]>() == typeof(string[][])));
        Console.WriteLine("array token casts="
            + ReferenceEquals(CastArray<byte[]>(byteArrays), byteArrays) + ":"
            + ReferenceEquals(CastArray<int[]>(intArrays), intArrays) + ":"
            + ReferenceEquals(CastArray<string[]>(stringArrays), stringArrays));
        Observe("array token cast rejected", byteArrays, () => CastArray<byte[]>(intArrays), bytes);
        var byteList = new List<byte[]>();
        for (int i = 0; i < 9; i++) byteList.Add(bytes);
        var emptyBytes = Array.Empty<byte>();
        byteList.Add(emptyBytes);
        Console.WriteLine("list byte array growth=" + byteList.Count + ":"
            + ReferenceEquals(byteList[0], bytes) + ":" + ReferenceEquals(byteList[9], emptyBytes));
        byteList.Clear();
        byteList.TrimExcess();
        byteList.Add(bytes);
        Console.WriteLine("list byte array regrowth=" + ReferenceEquals(byteList[0], bytes));
        var intList = new List<int[]>(1) { ints, Array.Empty<int>() };
        Console.WriteLine("list int array growth=" + intList.Count + ":" + ReferenceEquals(intList[0], ints));
        var stringList = new List<string[]>(1) { strings, Array.Empty<string>() };
        Console.WriteLine("list reference array growth=" + stringList.Count + ":" + ReferenceEquals(stringList[0], strings));
    }

    private static void ObserveMd(string label, Action store, Func<object> read, object expected, bool message = false)
    {
        string result = "ok";
        try { store(); }
        catch (Exception exception)
        {
            result = exception.GetType().Name + "/" + exception.HResult.ToString("X8");
            if (message) Console.WriteLine("md store message=" + exception.Message);
        }
        Console.WriteLine(label + "=" + result + ":identity=" + ReferenceEquals(read(), expected));
    }

    internal static void RunMdStores()
    {
        Console.WriteLine("== covariant multidimensional reference stores ==");
        object seed = "seed", next = "next", bad = new object();
        object[,] two = new string[1, 1];
        two[0, 0] = seed;
        ObserveMd("md rank2 rejected", () => two[0, 0] = bad, () => two[0, 0], seed, true);
        ObserveMd("md rank2 compatible", () => two[0, 0] = next, () => two[0, 0], next);
        ObserveMd("md rank2 null", () => two[0, 0] = null, () => two[0, 0], null);
        two[0, 0] = seed;
        int negative = -1;
        ObserveMd("md rank2 type before negative", () => two[negative, 0] = bad, () => two[0, 0], seed);
        ObserveMd("md rank2 type before past end", () => two[0, 1] = bad, () => two[0, 0], seed);
        ObserveMd("md rank2 compatible negative", () => two[negative, 0] = next, () => two[0, 0], seed);
        ObserveMd("md rank2 null past end", () => two[0, 1] = null, () => two[0, 0], seed);
        object[,] missing = null;
        ObserveMd("md null before type", () => missing[0, 0] = bad, () => null, null);
        ObserveMd("md argument rejected", () => StoreMdArgument(two, bad), () => two[0, 0], seed);
        s_mdValues = new string[1, 1];
        s_mdValues[0, 0] = seed;
        ObserveMd("md field rejected", () => s_mdValues[0, 0] = bad, () => s_mdValues[0, 0], seed);
        ObserveMd("md shared rejected", () => StoreMdGeneric<object>(two, bad), () => two[0, 0], seed);
        ObserveMd("md shared compatible", () => StoreMdGeneric<string>((string[,])two, (string)next), () => two[0, 0], next);
        object[,,] three = new string[1, 1, 1];
        three[0, 0, 0] = seed;
        ObserveMd("md rank3 rejected", () => three[0, 0, 0] = bad, () => three[0, 0, 0], seed);
        ObserveMd("md rank3 compatible", () => three[0, 0, 0] = next, () => three[0, 0, 0], next);
        ObserveMd("md rank3 null", () => three[0, 0, 0] = null, () => three[0, 0, 0], null);
        three[0, 0, 0] = seed;
        ObserveMd("md rank3 type before bounds", () => three[0, 0, 1] = bad, () => three[0, 0, 0], seed);
        ObserveMd("md rank3 compatible bounds", () => three[0, 0, 1] = next, () => three[0, 0, 0], seed);
        object[,,,] four = new string[1, 1, 1, 1];
        four[0, 0, 0, 0] = seed;
        ObserveMd("md rank4 rejected", () => four[0, 0, 0, 0] = bad, () => four[0, 0, 0, 0], seed);
        ObserveMd("md rank4 compatible", () => four[0, 0, 0, 0] = next, () => four[0, 0, 0, 0], next);
        ObserveMd("md rank4 null", () => four[0, 0, 0, 0] = null, () => four[0, 0, 0, 0], null);
        four[0, 0, 0, 0] = seed;
        ObserveMd("md rank4 type before bounds", () => four[0, 1, 0, 0] = bad, () => four[0, 0, 0, 0], seed);
        ObserveMd("md rank4 compatible bounds", () => four[0, 1, 0, 0] = next, () => four[0, 0, 0, 0], seed);
        object[,] created = (object[,])Array.CreateInstance(typeof(string), new int[] { 1, 1 });
        created[0, 0] = seed;
        ObserveMd("md created rejected", () => created[0, 0] = bad, () => created[0, 0], seed);
        ObserveMd("md created compatible", () => created[0, 0] = next, () => created[0, 0], next);
        var dog = new Dog();
        Animal[,] classes = new Dog[1, 1];
        classes[0, 0] = dog;
        ObserveMd("md class rejected", () => classes[0, 0] = new Cat(), () => classes[0, 0], dog);
        object[,] interfaces = new ITag[1, 1];
        ObserveMd("md interface compatible", () => interfaces[0, 0] = dog, () => interfaces[0, 0], dog);
        ObserveMd("md interface rejected", () => interfaces[0, 0] = bad, () => interfaces[0, 0], dog);
        object[,] jagged = new string[1, 1][];
        var leaf = new string[] { "leaf" };
        ObserveMd("md jagged compatible", () => jagged[0, 0] = leaf, () => jagged[0, 0], leaf);
        ObserveMd("md jagged rejected", () => jagged[0, 0] = new object[1], () => jagged[0, 0], leaf);
        object[,] objects = new object[1, 1];
        object boxed = 7;
        ObserveMd("md object boxed compatible", () => objects[0, 0] = boxed, () => objects[0, 0], boxed);
        var culture = new CultureInfo("en-US");
        var cultures = new CultureInfo[1, 1];
        cultures[0, 0] = culture;
        Console.WriteLine("md culture reads=" + cultures[0, 0].Name + ":"
            + ReferenceEquals(cultures[0, 0], culture));
        Console.WriteLine("md culture ref read=" + ReadMdCulture(ref cultures[0, 0]));
        var replacement = new CultureInfo("ja-JP");
        WriteMdCulture(ref cultures[0, 0], replacement);
        Console.WriteLine("md culture ref write=" + cultures[0, 0].Name + ":" + ReferenceEquals(cultures[0, 0], replacement));
        cultures[0, 0] = null;
        Console.WriteLine("md culture null=" + (cultures[0, 0] is null));
        Console.WriteLine("multidimensional reference stores end");
    }
    internal static void RunLowerBoundStores()
    {
        Console.WriteLine("== nonzero covariant array stores ==");
        object seed = "seed", next = "next", bad = new object();
        object[,] two = (object[,])Array.CreateInstance(typeof(string), new[] { 1, 1 }, new[] { -2, 3 });
        two[-2, 3] = seed;
        ObserveMd("lower typed type before bounds", () => two[0, 0] = bad, () => two[-2, 3], seed);
        ObserveMd("lower typed compatible bounds", () => two[0, 0] = next, () => two[-2, 3], seed);
        ObserveMd("lower reflection bounds before type", () => ((Array)two).SetValue(bad, 0, 0), () => two[-2, 3], seed);
        ObserveMd("lower reflection wrong type", () => ((Array)two).SetValue(bad, -2, 3), () => two[-2, 3], seed);
        ObserveMd("lower typed compatible", () => two[-2, 3] = next, () => two[-2, 3], next);
        ObserveMd("lower typed null", () => two[-2, 3] = null, () => two[-2, 3], null);
        Console.WriteLine("nonzero covariant array stores end");
    }

    private static object ReadText(ref TextInfo value) => value;
    private static object ReadModule(ref System.Reflection.Module value) => value;
    private static T ReadDefaultMd<T>() where T : class => (new T[1, 1])[0, 0];
    private static T StoreNullMd<T>() where T : class
    {
        var values = new T[1, 1];
        values[0, 0] = null;
        return values[0, 0];
    }
    private static CultureInfo[,] MdReceiver(CultureInfo[,] value, ref int calls)
    {
        calls++;
        return value;
    }

    internal static void RunHeaderlessMdStores()
    {
        Console.WriteLine("== headerless multidimensional reference identity ==");
        var first = CultureInfo.InvariantCulture;
        var second = new CultureInfo("ja-JP");
        var typed = new CultureInfo[1, 1];
        typed[0, 0] = first;
        Array reflected = typed;
        object[,] erased = typed;
        Console.WriteLine("headerless md typed to reflective=" + ReferenceEquals(reflected.GetValue(0, 0), first));
        Console.WriteLine("headerless md typed to covariant=" + ReferenceEquals(erased[0, 0], first));
        reflected.SetValue(second, 0, 0);
        Console.WriteLine("headerless md reflective to typed=" + typed[0, 0].Name + ":" + ReferenceEquals(typed[0, 0], second));
        Console.WriteLine("headerless md reflective byref=" + ReadMdCulture(ref typed[0, 0]));
        WriteMdCulture(ref typed[0, 0], first);
        Console.WriteLine("headerless md byref to reflective=" + ReferenceEquals(reflected.GetValue(new int[] { 0, 0 }), first));
        erased[0, 0] = second;
        Console.WriteLine("headerless md covariant to typed=" + typed[0, 0].Name + ":" + ReferenceEquals(typed[0, 0], second));
        reflected.SetValue(null, new long[] { 0, 0 });
        Console.WriteLine("headerless md reflective null=" + (typed[0, 0] is null) + ":" + (erased[0, 0] is null));
        reflected.SetValue(first, new int[] { 0, 0 });
        ObserveMd("headerless md incompatible store", () => erased[0, 0] = "wrong", () => reflected.GetValue(0, 0), first);
        var lower = Array.CreateInstance(typeof(CultureInfo), new[] { 1, 1 }, new[] { -2, 3 });
        lower.SetValue(second, -2, 3);
        var lowerTyped = (CultureInfo[,])lower;
        Console.WriteLine("headerless lower md identity=" + lowerTyped[-2, 3].Name + ":" + ReferenceEquals(lower.GetValue(-2, 3), second));
        var assemblies = new System.Reflection.Assembly[1, 1];
        var assembly = typeof(Program).Assembly;
        assemblies[0, 0] = assembly;
        Console.WriteLine("headerless assembly md identity=" + ReferenceEquals(((Array)assemblies).GetValue(0, 0), assembly));
        ((Array)assemblies).SetValue(assembly, 0, 0);
        Console.WriteLine("headerless assembly md typed read=" + ReferenceEquals(assemblies[0, 0], assembly));
        var texts = new TextInfo[1, 1];
        texts[0, 0] = second.TextInfo;
        object text = texts[0, 0];
        Console.WriteLine("headerless text md kinds=" + (text is TextInfo) + ":" + (ReadText(ref texts[0, 0]) is TextInfo)
            + ":" + ReferenceEquals(text, ((Array)texts).GetValue(0, 0)) + ":" + ReferenceEquals(text, second.TextInfo));
        var numbers = new NumberFormatInfo[1, 1];
        numbers[0, 0] = second.NumberFormat;
        object numeric = numbers[0, 0];
        Console.WriteLine("headerless numbers md identity=" + (numeric is NumberFormatInfo) + ":"
            + ReferenceEquals(numeric, ((Array)numbers).GetValue(0, 0)) + ":" + ReferenceEquals(numeric, second.NumberFormat));
        var modules = new System.Reflection.Module[1, 1];
        modules[0, 0] = typeof(Program).Module;
        object module = modules[0, 0];
        Console.WriteLine("headerless module md kinds=" + (module is System.Reflection.Module) + ":"
            + (ReadModule(ref modules[0, 0]) is System.Reflection.Module) + ":"
            + ReferenceEquals(module, ((Array)modules).GetValue(0, 0)));
        int calls = 0;
        Console.WriteLine("headerless md receiver once=" + ReferenceEquals(MdReceiver(typed, ref calls)[0, 0], first) + ":" + calls);
        try { object value = MdReceiver(null, ref calls)[0, 0]; Console.WriteLine("headerless md null=accepted"); }
        catch (NullReferenceException) { Console.WriteLine("headerless md null=NullReferenceException:" + calls); }
        var objects = new object[1, 1];
        Array.Copy(typed, objects, 1);
        Console.WriteLine("headerless md copy to object=" + ReferenceEquals(objects[0, 0], first));
        objects[0, 0] = second;
        Array.Copy(objects, typed, 1);
        Console.WriteLine("headerless md copy to typed=" + ReferenceEquals(typed[0, 0], second) + ":" + typed[0, 0].Name);
        Array one = Array.CreateInstance(typeof(CultureInfo), new[] { 2 }, new[] { -1 });
        one.SetValue(first, -1);
        one.SetValue(second, 0L);
        var sz = new CultureInfo[2];
        Array.Copy(one, -1, sz, 0, 2);
        Console.WriteLine("headerless rank1 copy to SZ=" + ReferenceEquals(sz[0], first) + ":" + ReferenceEquals(sz[1], second));
        sz[0] = second;
        Array.Copy(sz, 0, one, -1, 2);
        Console.WriteLine("headerless rank1 copy to MD=" + ReferenceEquals(one.GetValue(-1L), second) + ":" + ReferenceEquals(one.GetValue(0), second));
        Console.WriteLine("generic md default reads=" + (ReadDefaultMd<string>() is null) + ":" + (ReadDefaultMd<object>() is null));
        Console.WriteLine("generic md null stores=" + (StoreNullMd<string>() is null) + ":" + (StoreNullMd<object>() is null));
        Console.WriteLine("headerless multidimensional reference identity end");
    }

    private static void WriteObject(ref object slot, object value) => slot = value;
    private static T ReadReadonly<T>(in T slot) => slot;
    private static void WriteReference<T>(ref T slot, T value) => slot = value;
    private static void WriteGenericMd<T>(T[,] values, T value) where T : class => WriteReference(ref values[0, 0], value);
    private static T ReadGenericMd<T>(T[,] values) where T : class => ReadReadonly(in values[0, 0]);
    private static T AllocateRefMd<T>(T value) where T : class
    {
        var values = new T[1, 1];
        WriteReference(ref values[0, 0], value);
        return values[0, 0];
    }
    private static void WriteShort(ref short slot) => slot = -123;
    private static void WritePair(ref PairSlot slot) => slot.Value = 37;
    private struct PairSlot { internal int Value; }
    private static object[,] AddressReceiver(object[,] values, ref int calls)
    {
        calls++;
        return values;
    }

    private static T[,] AllocateMd<T>(T value)
    {
        var values = new T[1, 1];
        values[0, 0] = value;
        return values;
    }

    private static T[,] AllocateHeaderlessMd<T>(T value) where T : class
    {
        var values = new T[1, 1];
        values[0, 0] = value;
        return values;
    }

    private enum MdCode { Value = 29 }
    private static bool IsMd<T>(object value) => value is T[,];
    private static T[,] CastMd<T>(object value) => (T[,])value;
    private static Type MdType<T>() => typeof(T[,]);
    private static string CastMdIdentity<T>(object value)
    {
        try { return ReferenceEquals(CastMd<T>(value), value) ? "same" : "different"; }
        catch (InvalidCastException) { return "InvalidCastException"; }
    }

    internal static void RunGenericMdConsumers()
    {
        Console.WriteLine("== generic multidimensional type consumers ==");
        var strings = new string[1, 1];
        var objects = new object[1, 1];
        var nested = new string[1, 1][];
        var rectangular = new string[1, 1][,];
        Console.WriteLine("generic md is=" + IsMd<string>(strings) + ":" + IsMd<object>(strings) + ":" + IsMd<string>(objects) + ":" + IsMd<string>(new string[1]) + ":" + IsMd<string>(new string[1, 1, 1]) + ":" + IsMd<string>(null));
        Console.WriteLine("generic md cast=" + CastMdIdentity<string>(strings) + ":" + CastMdIdentity<object>(strings) + ":" + CastMdIdentity<string>(objects) + ":" + CastMdIdentity<string>(null));
        Console.WriteLine("generic md nested is=" + IsMd<string[]>(nested) + ":" + IsMd<object[]>(nested) + ":" + IsMd<string[,]>(rectangular) + ":" + IsMd<string[,]>(nested));
        Console.WriteLine("generic md nested cast=" + CastMdIdentity<string[]>(nested) + ":" + CastMdIdentity<object[]>(nested) + ":" + CastMdIdentity<string[,]>(rectangular) + ":" + CastMdIdentity<string[,]>(nested));
        Console.WriteLine("generic md tokens=" + (MdType<string>() == strings.GetType()) + ":" + (MdType<object>() == objects.GetType()) + ":" + (MdType<string[]>() == nested.GetType()) + ":" + (MdType<string[,]>() == rectangular.GetType()));
        Console.WriteLine("generic multidimensional type consumers end");
    }

    private sealed class MdOwner<T> where T : class
    {
        internal static T[,] Allocate(T value)
        {
            var values = new T[1, 1];
            values[0, 0] = value;
            return values;
        }

        internal static void Write(T[,] values, T value) => WriteReference(ref values[0, 0], value);
    }

    internal static void RunGenericMdAllocations()
    {
        Console.WriteLine("== shared multidimensional allocation identity ==");
        object seed = new object();
        var strings = AllocateMd("allocated");
        var objects = AllocateMd(seed);
        Console.WriteLine("allocation references=" + strings[0, 0] + ":" + ReferenceEquals(objects[0, 0], seed));
        Console.WriteLine("allocation reference types=" + (strings.GetType() == typeof(string[,])) + ":" + (objects.GetType() == typeof(object[,])));
        Console.WriteLine("allocation shape=" + strings.Rank + ":" + strings.GetLength(0) + ":" + strings.GetLowerBound(1) + ":" + (strings.GetType().GetElementType() == typeof(string)));
        ObserveMd("allocation separate caller covariance", () => WriteGenericMd<object>(strings, seed), () => strings[0, 0], "allocated");
        WriteGenericMd(strings, "changed");
        Console.WriteLine("allocation separate caller exact=" + strings[0, 0]);
        var integers = AllocateMd(29);
        var codes = AllocateMd(MdCode.Value);
        var shorts = AllocateMd((short)-12);
        Console.WriteLine("allocation values=" + integers[0, 0] + ":" + codes[0, 0] + ":" + shorts[0, 0]);
        Console.WriteLine("allocation value types=" + (integers.GetType() == typeof(int[,])) + ":" + (codes.GetType() == typeof(MdCode[,])) + ":" + (shorts.GetType() == typeof(short[,])));
        var nested = AllocateMd(new[] { "nested" });
        var rectangular = AllocateMd(new string[,] { { "rectangle" } });
        Console.WriteLine("allocation nested=" + nested[0, 0][0] + ":" + rectangular[0, 0][0, 0] + ":" + (nested.GetType() == typeof(string[,][])) + ":" + (rectangular.GetType() == typeof(string[,][,])));
        var ownedStrings = MdOwner<string>.Allocate("owned");
        var ownedObjects = MdOwner<object>.Allocate(seed);
        MdOwner<string>.Write(ownedStrings, "changed");
        Console.WriteLine("allocation generic owner=" + ownedStrings[0, 0] + ":" + ReferenceEquals(ownedObjects[0, 0], seed) + ":" + (ownedStrings.GetType() == typeof(string[,])));
        ObserveMd("address generic owner covariance", () => MdOwner<object>.Write(ownedStrings, seed), () => ownedStrings[0, 0], "changed");
        var culture = new CultureInfo("ja-JP");
        var cultures = AllocateHeaderlessMd(culture);
        Console.WriteLine("allocation headerless=" + ReferenceEquals(cultures[0, 0], culture) + ":" + ReferenceEquals(((Array)cultures).GetValue(0, 0), culture) + ":" + (cultures.GetType() == typeof(CultureInfo[,])));
        Console.WriteLine("shared multidimensional allocation identity end");
    }

    internal static void RunMdAddresses()
    {
        Console.WriteLine("== multidimensional writable address identity ==");
        object seed = "seed";
        object replacement = "replacement";
        object[,] covariant = new string[1, 1];
        covariant[0, 0] = seed;
        ObserveMd("address covariance", () => WriteObject(ref covariant[0, 0], replacement), () => covariant[0, 0], seed);
        ObserveMd("address covariance before bounds", () => WriteObject(ref covariant[-1, 0], replacement), () => covariant[0, 0], seed);
        Console.WriteLine("address readonly covariance=" + ReferenceEquals(ReadReadonly(in covariant[0, 0]), seed));
        ObserveMd("address readonly bounds", () => ReadReadonly(in covariant[-1, 0]), () => covariant[0, 0], seed);
        object[,] normal = new object[1, 1];
        WriteObject(ref normal[0, 0], seed);
        Console.WriteLine("address exact=" + ReferenceEquals(normal[0, 0], seed));
        ObserveMd("address exact bounds", () => WriteObject(ref normal[0, 1], replacement), () => normal[0, 0], seed);
        object[,] nil = null;
        ObserveMd("address null before bounds", () => WriteObject(ref nil[-1, 0], replacement), () => null, null);
        ObserveMd("address readonly null", () => ReadReadonly(in nil[-1, 0]), () => null, null);
        object[,,] three = new string[1, 1, 1];
        three[0, 0, 0] = seed;
        ObserveMd("address rank3", () => WriteObject(ref three[0, 0, 0], replacement), () => three[0, 0, 0], seed);
        ObserveMd("address rank3 before bounds", () => WriteObject(ref three[0, 0, -1], replacement), () => three[0, 0, 0], seed);
        Console.WriteLine("address readonly rank3=" + ReferenceEquals(ReadReadonly(in three[0, 0, 0]), seed));
        object[,,,] four = new string[1, 1, 1, 1];
        four[0, 0, 0, 0] = seed;
        ObserveMd("address rank4", () => WriteObject(ref four[0, 0, 0, 0], replacement), () => four[0, 0, 0, 0], seed);
        Console.WriteLine("address readonly rank4=" + ReferenceEquals(ReadReadonly(in four[0, 0, 0, 0]), seed));
        var lower = (object[,])Array.CreateInstance(typeof(string), new[] { 1, 1 }, new[] { -2, 3 });
        lower[-2, 3] = seed;
        ObserveMd("address lower", () => WriteObject(ref lower[-2, 3], replacement), () => lower[-2, 3], seed);
        ObserveMd("address lower before bounds", () => WriteObject(ref lower[0, 0], replacement), () => lower[-2, 3], seed);
        Console.WriteLine("address readonly lower=" + ReferenceEquals(ReadReadonly(in lower[-2, 3]), seed));
        int calls = 0;
        WriteObject(ref AddressReceiver(normal, ref calls)[0, 0], replacement);
        Console.WriteLine("address receiver once=" + calls + ":" + ReferenceEquals(normal[0, 0], replacement));
        var strings = new string[1, 1];
        WriteGenericMd(strings, "text");
        WriteGenericMd(normal, seed);
        Console.WriteLine("address generic exact=" + ReadGenericMd(strings) + ":" + ReferenceEquals(ReadGenericMd(normal), seed));
        ObserveMd("address generic covariance", () => WriteGenericMd<object>(strings, replacement), () => strings[0, 0], "text");
        Console.WriteLine("address generic readonly=" + ReferenceEquals(ReadGenericMd<object>(strings), "text"));
        Console.WriteLine("address generic allocation=" + AllocateRefMd("allocated") + ":" + ReferenceEquals(AllocateRefMd(seed), seed));
        var nested = new string[1, 1][];
        WriteGenericMd(nested, new[] { "nested" });
        var nestedObjects = new object[1, 1][];
        WriteGenericMd(nestedObjects, new object[] { seed });
        Console.WriteLine("address nested exact=" + nested[0, 0][0] + ":" + ReferenceEquals(nestedObjects[0, 0][0], seed));
        ObserveMd("address nested covariance", () => WriteGenericMd<object[]>(nested, new object[] { seed }), () => nested[0, 0], nested[0, 0]);
        var packed = new short[1, 2];
        WriteShort(ref packed[0, 1]);
        Console.WriteLine("address packed=" + packed[0, 0] + ":" + packed[0, 1]);
        var pairs = new PairSlot[1, 1];
        WritePair(ref pairs[0, 0]);
        Console.WriteLine("address value=" + pairs[0, 0].Value);
        var cultures = new CultureInfo[1, 1];
        var culture = new CultureInfo("ja-JP");
        WriteMdCulture(ref cultures[0, 0], culture);
        Console.WriteLine("address headerless=" + ReadMdCulture(ref cultures[0, 0]) + ":" + ReferenceEquals(((Array)cultures).GetValue(0, 0), culture));
        Console.WriteLine("multidimensional writable address identity end");
    }

}
