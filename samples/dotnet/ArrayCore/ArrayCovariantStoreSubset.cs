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

}
