using System;
using System.Globalization;
using System.Reflection;

namespace ArrayTypeHandleSubset;

internal static class Program
{
    private class ArrayHandleKnown { }
    private class ArrayHandleClosed<T> { }
    private class ArrayHandleNeverArrayed { }
    private class ArrayHandleOwner<T> { public static void Method<U>() { } }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("array-handle-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool NullReceiver(Exception error) => error is NullReferenceException && error.HResult == unchecked((int)0x80004003);
    private static bool InvalidRank(Exception error) => error is IndexOutOfRangeException && error.HResult == unchecked((int)0x80131508);
    private static bool InvalidElement(Exception error) => error is TypeLoadException && error.HResult == unchecked((int)0x80131522);

    private static Type PublicSz(Type type, int rank) => type.MakeArrayType();
    private static Type PublicRank(Type type, int rank) => type.MakeArrayType(rank);
    private static Type PublicRankUnsigned(Type type, uint rank) => type.MakeArrayType(unchecked((int)rank));

    private static unsafe Type[] Elements(bool sz)
    {
        Type open = typeof(ArrayHandleOwner<>);
        Type formal = open.GetGenericArguments()[0];
        Type method = typeof(ArrayHandleOwner<int>).GetMethod("Method").GetGenericArguments()[0];
        return sz ? new[] { typeof(int), typeof(string), typeof(ArrayHandleKnown), typeof(ArrayHandleClosed<int>),
            typeof(int[]), typeof(int[,]), open, formal, method, typeof(int*), typeof(delegate*<int, int>) }
            : new[] { open, formal, method, typeof(int*), typeof(delegate*<int, int>), typeof(int*[]) };
    }

    private static void Route(string route, Func<Type, int, Type> query, bool sz)
    {
        Type[] elements = Elements(sz);
        Type[] known = { typeof(int[]), typeof(string[]), typeof(ArrayHandleKnown[]), typeof(ArrayHandleClosed<int>[]) };
        int[] ranks = sz ? new[] { 1 } : new[] { 1, 2, 32 };
        for (int i = 0; i < elements.Length; i++)
        {
            Type element = elements[i];
            foreach (int rank in ranks)
            {
                Type result = query(element, rank);
                Type expected = sz ? element.MakeArrayType() : element.MakeArrayType(rank);
                Check(route + "-shape-" + i + "-" + rank, ReferenceEquals(result, expected)
                    && ReferenceEquals(result.GetElementType(), element) && result.IsArray && result.IsSZArray == sz
                    && result.GetArrayRank() == rank && result.ContainsGenericParameters == element.ContainsGenericParameters
                    && ReferenceEquals(Type.GetTypeFromHandle(result.TypeHandle), result) && ReferenceEquals(query(element, rank), result)
                    && (!sz || i >= known.Length || ReferenceEquals(result, known[i])));
            }
        }
        Type stableElement = typeof(ArrayHandleOwner<>).GetGenericArguments()[0];
        Type stable = query(stableElement, 1);
        GC.Collect();
        Check(route + "-repeated", ReferenceEquals(stable, query(stableElement, 1)));
        Check(route + "-byref", InvalidElement(Error(() => query(typeof(int).MakeByRefType(), 1))));
        Check(route + "-byreflike", InvalidElement(Error(() => query(typeof(Span<int>), 1))));
        Check(route + "-open-byreflike", InvalidElement(Error(() => query(typeof(Span<>), 1))));
        if (!sz)
        {
            Check(route + "-rank33", InvalidElement(Error(() => query(stableElement, 33))));
            Check(route + "-rank-max", InvalidElement(Error(() => query(stableElement, int.MaxValue))));
            Check(route + "-non-sz", !ReferenceEquals(query(stableElement, 1), stableElement.MakeArrayType()));
        }
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Array type handle contract ==");
        Route("public-sz", PublicSz, true);
        Route("public-rank", PublicRank, false);
        Check("public-sz-null", NullReceiver(Error(() => PublicSz(null, 1))));
        Check("public-rank-null", NullReceiver(Error(() => PublicRank(null, 1))));
        Check("public-rank-null-zero", NullReceiver(Error(() => PublicRank(null, 0))));
        foreach (int rank in new[] { 0, -1, int.MinValue })
            Check("public-rank-invalid-" + rank, InvalidRank(Error(() => PublicRank(typeof(Span<int>), rank))));
        Check("public-rank-unsigned", InvalidRank(Error(() => PublicRankUnsigned(typeof(int), 0x80000000))));
        Console.WriteLine("Array type handle contract end");
    }
}
