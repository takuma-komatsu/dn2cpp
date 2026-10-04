using System;
using System.Collections.Generic;

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

    private static object[] Returned() => s_values;
    private static void StoreArgument(object[] values, object value) => values[0] = value;
    private static void StoreGeneric<T>(T[] values, T value) where T : class => values[0] = value;
    private static T[] Allocate<T>(int length) => new T[length];
    private static Type ArrayType<T>() => typeof(T[]);
    private static T[] CastArray<T>(object value) => (T[])value;

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
}
