using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UnsafeGeneral.TypedReferenceSubset;

internal static class Program
{
    private enum Number : int { First = 7 }
    private struct Pair
    {
        public int First;
        public long Second;
    }

    private class Parent { public int Value; }
    private sealed class Child : Parent { }
    private sealed class Holder { public string Value = "initial"; }
    private static int reflectionCalls;

    private static void ReflectedArgument(TypedReference reference)
    {
        reflectionCalls++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Replace<T>(ref T value, T replacement)
    {
        TypedReference reference = __makeref(value);
        bool matches = __reftype(reference) == typeof(T);
        __refvalue(reference, T) = replacement;
        return matches;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsType(TypedReference reference, Type expected)
    {
        return __reftype(reference) == expected;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Collect()
    {
        GC.Collect();
        return 17;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string WriteAfterCollection(TypedReference reference, int value)
    {
        __refvalue(reference, string) = "collected" + value;
        GC.Collect();
        return __refvalue(reference, string);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string HeapReference()
    {
        Holder holder = new Holder();
        TypedReference reference = __makeref(holder.Value);
        holder = null;
        GC.Collect();
        return WriteAfterCollection(reference, Collect());
    }

    public static void __GateEntry()
    {
        Console.WriteLine("== typed references ==");
        int number = 12;
        TypedReference integer = __makeref(number);
        Console.WriteLine("typedref int=" + IsType(integer, typeof(int)) + "/" + __refvalue(integer, int));
        __refvalue(integer, int) = 23;
        Console.WriteLine("typedref write=" + number);
        Console.WriteLine("typedref hash=" + (integer.GetHashCode() == typeof(int).GetHashCode()));
        Console.WriteLine("typedref target=" + (TypedReference.GetTargetType(integer) == typeof(int)) + "/"
            + (Type.GetTypeFromHandle(TypedReference.TargetTypeToken(integer)) == typeof(int)));

        Pair pair = new Pair { First = 4, Second = 19 };
        TypedReference aggregate = __makeref(pair);
        __refvalue(aggregate, Pair).Second = 31;
        Console.WriteLine("typedref struct=" + IsType(aggregate, typeof(Pair)) + "/" + pair.First + "/" + pair.Second);

        Parent parent = new Child { Value = 5 };
        TypedReference reference = __makeref(parent);
        Console.WriteLine("typedref declared=" + IsType(reference, typeof(Parent)) + "/" + (__refvalue(reference, Parent) is Child));
        __refvalue(reference, Parent) = new Parent { Value = 8 };
        Console.WriteLine("typedref class write=" + parent.Value + "/" + (parent is Child));

        string empty = null;
        TypedReference nullReference = __makeref(empty);
        Console.WriteLine("typedref null=" + IsType(nullReference, typeof(string)) + "/" + (__refvalue(nullReference, string) == null));
        __refvalue(nullReference, string) = "assigned";
        Console.WriteLine("typedref null write=" + empty);

        int[] array = { 3, 6 };
        TypedReference element = __makeref(array[1]);
        __refvalue(element, int) = 9;
        TypedReference field = __makeref(parent.Value);
        __refvalue(field, int) = 11;
        Console.WriteLine("typedref locations=" + array[1] + "/" + parent.Value);
        byte[] bytes = { 1, 2, 3 };
        TypedReference narrow = __makeref(bytes[1]);
        __refvalue(narrow, byte) = 255;
        char[] chars = { 'A', 'B', 'C' };
        TypedReference character = __makeref(chars[1]);
        __refvalue(character, char) = '\uFFFF';
        Console.WriteLine("typedref narrow=" + bytes[0] + "/" + bytes[1] + "/" + bytes[2]
            + "/" + (int)chars[0] + "/" + (int)chars[1] + "/" + (int)chars[2]);
        TypedReference arrayReference = __makeref(array);
        Console.WriteLine("typedref array=" + IsType(arrayReference, typeof(int[])) + "/"
            + ReferenceEquals(__refvalue(arrayReference, int[]), array));
        try { _ = __refvalue(arrayReference, object); }
        catch (InvalidCastException) { Console.WriteLine("typedref array as object=InvalidCastException"); }
        try { _ = __refvalue(arrayReference, string[]); }
        catch (InvalidCastException) { Console.WriteLine("typedref wrong array=InvalidCastException"); }

        try { _ = __refvalue(integer, long); }
        catch (InvalidCastException) { Console.WriteLine("typedref wrong primitive=InvalidCastException"); }
        Number enumeration = Number.First;
        TypedReference enumReference = __makeref(enumeration);
        try { _ = __refvalue(enumReference, int); }
        catch (InvalidCastException) { Console.WriteLine("typedref enum as underlying=InvalidCastException"); }
        try { _ = __refvalue(integer, Number); }
        catch (InvalidCastException) { Console.WriteLine("typedref underlying as enum=InvalidCastException"); }
        try { _ = __refvalue(reference, Child); }
        catch (InvalidCastException) { Console.WriteLine("typedref parent as child=InvalidCastException"); }
        Child child = new Child();
        TypedReference childReference = __makeref(child);
        try { _ = __refvalue(childReference, Parent); }
        catch (InvalidCastException) { Console.WriteLine("typedref child as parent=InvalidCastException"); }
        string absent = null;
        TypedReference absentReference = __makeref(absent);
        try { _ = __refvalue(absentReference, object); }
        catch (InvalidCastException) { Console.WriteLine("typedref null as object=InvalidCastException"); }

        Console.WriteLine("typedref generic int=" + Replace(ref number, 42) + "/" + number);
        Console.WriteLine("typedref generic struct=" + Replace(ref pair, new Pair { First = 13, Second = 21 }) + "/" + pair.First + "/" + pair.Second);
        Console.WriteLine("typedref generic string=" + Replace(ref empty, "generic") + "/" + empty);
        Console.WriteLine("typedref generic parent=" + Replace(ref parent, new Child { Value = 34 }) + "/" + parent.Value);
        object boxed = null;
        Console.WriteLine("typedref generic object=" + Replace(ref boxed, (object)"boxed") + "/" + (string)boxed);
        Console.WriteLine("typedref heap gc=" + HeapReference());
        MethodInfo reflected = typeof(Program).GetMethod(nameof(ReflectedArgument), BindingFlags.NonPublic | BindingFlags.Static);
        foreach (object argument in new object[] { null, new object(), 3 })
        {
            try
            {
                reflected.Invoke(null, new object[] { argument });
                Console.WriteLine("typedref invoke=returned");
            }
            catch (Exception exception)
            {
                Console.WriteLine("typedref invoke=" + exception.GetType().Name);
            }
        }
        Console.WriteLine("typedref invoke calls=" + reflectionCalls);
        Console.WriteLine("typedref complete");
    }
}
