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

    private static bool InvalidHandle(Exception error) => error is ArgumentNullException argument
        && argument.ParamName is null && error.Message == "Invalid handle." && error.HResult == unchecked((int)0x80004003);
    private static bool NullReceiver(Exception error) => error is NullReferenceException && error.HResult == unchecked((int)0x80004003);
    private static bool InvalidRank(Exception error) => error is IndexOutOfRangeException && error.HResult == unchecked((int)0x80131508);
    private static bool InvalidElement(Exception error) => error is TypeLoadException && error.HResult == unchecked((int)0x80131522);
    private static bool UnsupportedShape(Exception error) => error is NotSupportedException && error.GetType() == typeof(NotSupportedException);
    private static bool PrivateRankBoundary(Exception error) => error is PlatformNotSupportedException
        && error.Message == "System.RuntimeTypeHandle.MakeArray with a nonpositive rank requires CoreCLR's private array loader. Use Type.MakeArrayType instead.";

    private static Type PublicSz(Type type, int rank) => type.MakeArrayType();
    private static Type PublicRank(Type type, int rank) => type.MakeArrayType(rank);
    private static Type PublicRankUnsigned(Type type, uint rank) => type.MakeArrayType(unchecked((int)rank));
    private static Type RetainedSz(Type type, int rank) => HandleOracle(type is null ? default : type.TypeHandle, rank, true);
    private static Type RetainedRank(Type type, int rank)
    {
        if (rank <= 0) throw new IndexOutOfRangeException();
        return HandleOracle(type is null ? default : type.TypeHandle, rank, false);
    }
    private static Type RetainedRankUnsigned(Type type, uint rank) => RetainedRank(type, unchecked((int)rank));
    private static Type VirtualSz(Type type, int rank) => PublicSz(type, rank);
    private static Type VirtualRank(Type type, int rank) => PublicRank(type, rank);
    private static Type VirtualRankUnsigned(Type type, uint rank) => PublicRank(type, unchecked((int)rank));
    private static Type ConstrainedSz(Type type, int rank) => PublicSz(type, rank);
    private static Type ConstrainedRank(Type type, int rank) => PublicRank(type, rank);
    private static Func<Type> PublicAddressSz(Type type) => () => RetainedSz(type, 1);
    private static Func<int, Type> PublicAddressRank(Type type) => rank => RetainedRank(type, rank);
    private static Func<Type> VirtualAddressSz(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => RetainedSz(type, 1);
    }
    private static Func<int, Type> VirtualAddressRank(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return rank => RetainedRank(type, rank);
    }
    private static Func<Type> FlowAddressSz(Type type, bool virtualAddress) => virtualAddress ? VirtualAddressSz(type) : PublicAddressSz(type);
    private static Func<int, Type> FlowAddressRank(Type type, bool virtualAddress) => virtualAddress ? VirtualAddressRank(type) : PublicAddressRank(type);

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

    // Public factories supply the oracle; checked-handle behavior comes from managed IL.
    private static Type HandleOracle(RuntimeTypeHandle handle, int rank, bool sz)
    {
        Type type = Type.GetTypeFromHandle(handle);
        if (type is null) throw new ArgumentNullException(null, "Invalid handle.");
        return sz ? type.MakeArrayType() : type.MakeArrayType(rank);
    }
    private static Type SzDirect(RuntimeTypeHandle handle, int rank) => HandleOracle(handle, rank, true);
    private static Type SzCalli(RuntimeTypeHandle handle, int rank) => HandleOracle(handle, rank, true);
    private static Func<Type> SzAddress(RuntimeTypeHandle handle) => () => HandleOracle(handle, 1, true);
    private static Type SzDelegate(RuntimeTypeHandle handle, int rank) => SzAddress(handle)();
    private static Type SzReflection(RuntimeTypeHandle handle, int rank) => HandleOracle(handle, rank, true);
    private static Type RankDirect(RuntimeTypeHandle handle, int rank) => HandleOracle(handle, rank, false);
    private static Type RankUnsigned(RuntimeTypeHandle handle, uint rank) => HandleOracle(handle, unchecked((int)rank), false);
    private static Type RankCalli(RuntimeTypeHandle handle, int rank) => HandleOracle(handle, rank, false);
    private static Func<int, Type> RankAddress(RuntimeTypeHandle handle) => rank => HandleOracle(handle, rank, false);
    private static Type RankDelegate(RuntimeTypeHandle handle, int rank) => RankAddress(handle)(rank);
    private static Type RankReflection(RuntimeTypeHandle handle, int rank) => HandleOracle(handle, rank, false);

    private static void HandleRoute(string route, Func<RuntimeTypeHandle, int, Type> query, bool sz)
    {
        Route(route, (type, rank) => query(type.TypeHandle, rank), sz);
        Check(route + "-default", InvalidHandle(Error(() => query(default, 1))));
    }

    private static void PublicNulls(string route, Func<Type, int, Type> query, bool sz, bool nonvirtual)
    {
        Check(route + "-null", nonvirtual ? InvalidHandle(Error(() => query(null, 1))) : NullReceiver(Error(() => query(null, 1))));
        if (!sz)
            Check(route + "-null-zero", nonvirtual ? InvalidRank(Error(() => query(null, 0))) : NullReceiver(Error(() => query(null, 0))));
    }

    private static void NativeShapeBoundary(string route, Func<RuntimeTypeHandle, int, Type> query, bool sz)
    {
        Check(route + "-missing", UnsupportedShape(Error(() => query(typeof(ArrayHandleNeverArrayed).TypeHandle, 1))));
        if (!sz)
        {
            foreach (int rank in new[] { 1, 2, 32, 33 })
                Check(route + "-concrete-" + rank, UnsupportedShape(Error(() => query(typeof(int).TypeHandle, rank))));
            foreach (int rank in new[] { 0, -1, int.MinValue })
            {
                Check(route + "-private-rank-" + rank, PrivateRankBoundary(Error(() => query(typeof(int).TypeHandle, rank))));
                Check(route + "-default-rank-" + rank, InvalidHandle(Error(() => query(default, rank))));
                Check(route + "-element-rank-" + rank, PrivateRankBoundary(Error(() => query(typeof(int).MakeByRefType().TypeHandle, rank))));
            }
        }
    }

    internal static void RunHandleArtifact(string[] args)
    {
        Run();
        string[] names = { "retained", "virtual", "constrained", "public-address", "virtual-address", "flow-direct", "flow-virtual" };
        Func<Type, int, Type>[] szRoutes = { RetainedSz, VirtualSz, ConstrainedSz, (t, r) => PublicAddressSz(t)(),
            (t, r) => VirtualAddressSz(t)(), (t, r) => FlowAddressSz(t, false)(), (t, r) => FlowAddressSz(t, true)() };
        Func<Type, int, Type>[] rankRoutes = { RetainedRank, VirtualRank, ConstrainedRank, (t, r) => PublicAddressRank(t)(r),
            (t, r) => VirtualAddressRank(t)(r), (t, r) => FlowAddressRank(t, false)(r), (t, r) => FlowAddressRank(t, true)(r) };
        for (int i = 0; i < names.Length; i++)
        {
            Route(names[i] + "-sz", szRoutes[i], true);
            Route(names[i] + "-rank", rankRoutes[i], false);
            // A null closed nonvirtual delegate is not the same shape as a direct call.
            if (i != 3 && i != 5)
            {
                PublicNulls(names[i] + "-sz", szRoutes[i], true, i == 0);
                PublicNulls(names[i] + "-rank", rankRoutes[i], false, i == 0);
            }
        }
        Check("retained-rank-unsigned", InvalidRank(Error(() => RetainedRankUnsigned(null, 0x80000000))));
        Check("virtual-rank-unsigned", NullReceiver(Error(() => VirtualRankUnsigned(null, 0xffffffff))));
        HandleRoute("direct-sz", SzDirect, true); HandleRoute("direct-rank", RankDirect, false);
        HandleRoute("calli-sz", SzCalli, true); HandleRoute("calli-rank", RankCalli, false);
        HandleRoute("delegate-sz", SzDelegate, true); HandleRoute("delegate-rank", RankDelegate, false);
        RunReflectionArtifact(args);
        if (args.Length > 0 && args[0] == "native-boundary")
        {
            NativeShapeBoundary("direct-sz", SzDirect, true); NativeShapeBoundary("direct-rank", RankDirect, false);
            NativeShapeBoundary("calli-sz", SzCalli, true); NativeShapeBoundary("calli-rank", RankCalli, false);
            NativeShapeBoundary("delegate-sz", SzDelegate, true); NativeShapeBoundary("delegate-rank", RankDelegate, false);
            Check("private-rank-unsigned", PrivateRankBoundary(Error(() => RankUnsigned(typeof(int).TypeHandle, 0x80000000))));
            Check("private-default-unsigned", InvalidHandle(Error(() => RankUnsigned(default, 0xffffffff))));
            foreach (bool sz in new[] { true, false })
            {
                Delegate binding = sz ? SzAddress(typeof(int).TypeHandle) : RankAddress(typeof(int).TypeHandle);
                MethodInfo method = binding.Method;
                Check("delegate-identity-" + (sz ? "sz" : "rank"), method.Name == (sz ? "MakeSZArray" : "MakeArray")
                    && method.DeclaringType == typeof(RuntimeTypeHandle) && method.ReturnType == ((object)typeof(int)).GetType()
                    && !method.IsStatic && !method.IsPublic && method.GetParameters().Length == (sz ? 0 : 1));
                Type element = typeof(ArrayHandleOwner<>).GetGenericArguments()[0];
                Delegate[] bindings = sz ? new Delegate[] { PublicAddressSz(element), VirtualAddressSz(element), FlowAddressSz(element, false), FlowAddressSz(element, true) }
                    : new Delegate[] { PublicAddressRank(element), VirtualAddressRank(element), FlowAddressRank(element, false), FlowAddressRank(element, true) };
                for (int i = 0; i < bindings.Length; i++)
                {
                    MethodInfo target = bindings[i].Method;
                    Check("public-identity-" + (sz ? "sz" : "rank") + "-" + i, target.Name == "MakeArrayType"
                        && target.DeclaringType == ((object)typeof(int)).GetType() && target.ReturnType == typeof(Type)
                        && target.IsPublic && target.IsVirtual && target.GetParameters().Length == (sz ? 0 : 1)
                        && ReferenceEquals(target.Invoke(element, sz ? null : new object[] { 1 }), sz ? element.MakeArrayType() : element.MakeArrayType(1)));
                }
            }
        }
        Console.WriteLine("Array type handle routes end");
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        HandleRoute("reflection-sz", SzReflection, true);
        HandleRoute("reflection-rank", RankReflection, false);
        if (args.Length > 0 && args[0] == "native-boundary")
        {
            NativeShapeBoundary("reflection-sz", SzReflection, true);
            NativeShapeBoundary("reflection-rank", RankReflection, false);
        }
        Console.WriteLine("Array type handle reflection end");
    }
}
