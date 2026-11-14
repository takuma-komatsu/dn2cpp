using System;
using System.Globalization;
using System.Reflection;

namespace CompoundTypeHandleSubset;

internal static class Program
{
    private class CompoundHandleOwner<T> { public static void Method<U>() { } }
    private class CompoundHandleSignatures { public static ref int ByRef(ref int value) => ref value; }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("compound-handle-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool NullReceiver(Exception error) => error is NullReferenceException
        && error.HResult == unchecked((int)0x80004003);
    private static bool InvalidElement(Exception error) => error is TypeLoadException
        && error.HResult == unchecked((int)0x80131522);
    private static Type PublicPointer(Type type) => type.MakePointerType();
    private static Type PublicByRef(Type type) => type.MakeByRefType();

    private static unsafe Type[] Elements() => new[] { typeof(int), typeof(string), typeof(void), typeof(int[]),
        Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 1 }).GetType(), typeof(int[,]), typeof(int*), typeof(CompoundHandleOwner<>),
        typeof(CompoundHandleOwner<string>), typeof(CompoundHandleOwner<>).GetGenericArguments()[0],
        typeof(CompoundHandleOwner<string>).GetMethod("Method").GetGenericArguments()[0], typeof(delegate*<int, int>) };

    private static unsafe void Route(string route, Func<Type, Type> query, bool pointer)
    {
        Type[] elements = Elements();
        for (int i = 0; i < elements.Length; i++)
        {
            Type element = elements[i];
            Type result = query(element);
            Type expected = pointer ? element.MakePointerType() : element.MakeByRefType();
            Check(route + "-shape-" + i, ReferenceEquals(result, expected)
                && ReferenceEquals(result.GetElementType(), element) && result.IsPointer == pointer && result.IsByRef != pointer
                && result.ContainsGenericParameters == element.ContainsGenericParameters
                && ReferenceEquals(Type.GetTypeFromHandle(result.TypeHandle), result)
                && ReferenceEquals(query(element), result));
        }
        Type signature = pointer ? typeof(int*) : typeof(CompoundHandleSignatures).GetMethod("ByRef").ReturnType;
        Check(route + "-signature", ReferenceEquals(query(typeof(int)), signature));
        Type stable = query(elements[9]);
        bool same = true;
        for (int i = 0; i < 16; i++) same &= ReferenceEquals(stable, query(elements[9]));
        GC.Collect();
        Check(route + "-repeated", same && ReferenceEquals(stable, query(elements[9])));
        Check(route + "-byref-rejected", InvalidElement(Error(() => query(typeof(int).MakeByRefType()))));
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Compound type handle contract ==");
        Route("public-pointer", PublicPointer, true);
        Route("public-byref", PublicByRef, false);
        Check("public-pointer-null", NullReceiver(Error(() => PublicPointer(null))));
        Check("public-byref-null", NullReceiver(Error(() => PublicByRef(null))));
        Check("public-distinct", !ReferenceEquals(PublicPointer(typeof(int)), PublicByRef(typeof(int))));
        Console.WriteLine("Compound type handle contract end");
    }
}
