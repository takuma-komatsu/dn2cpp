using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeArrayRankSubset;

internal static unsafe class ArrayRankChecks
{
    private enum RankEnum { Value }
    private class RankOwner<T> { public static void Method<U>() { } }

    private static void Check(string name, bool success)
    {
        Console.WriteLine("array-rank-" + name + ":" + success);
        if (!success) throw new InvalidOperationException(name);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static int Public(Type type) => type.GetArrayRank();
    private static int Direct(Type type) => Public(type);
    private static int Pointer(Type type) => Public(type);
    private static int Virtual(Type type) => Public(type);
    private static int VirtualPointer(Type type) => Public(type);
    private static int Constrained(Type type) => Public(type);
    private static Func<int> Address(Type type)
    {
        if (type is null) throw new ArgumentException();
        return () => Public(type);
    }
    private static Func<int> VirtualAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => Public(type);
    }
    private static Func<int> FlowAddress(Type type, bool virt) => virt ? VirtualAddress(type) : Address(type);
    private static int AddressQuery(Type type) => Address(type)();
    private static int VirtualAddressQuery(Type type) => VirtualAddress(type)();
    private static int FlowDirect(Type type) => FlowAddress(type, false)();
    private static int FlowVirtual(Type type) => FlowAddress(type, true)();
    private static int ReflectionLeaf(Type type) => Public(type);
    // The selected local wrapper is rooted only by reflection in the reflection-only artifact.
    private static MethodInfo ReflectionMethod() => typeof(ArrayRankChecks)
        .GetMethod(nameof(ReflectionLeaf), BindingFlags.Static | BindingFlags.NonPublic);
    private static int Reflection(Type type) => (int)typeof(ArrayRankChecks)
        .GetMethod(nameof(ReflectionLeaf), BindingFlags.Static | BindingFlags.NonPublic)
        .Invoke(null, new object[] { type });
    private static int Generic<T>() => typeof(T).GetArrayRank();

    private static void Route(string route, Func<Type, int> query)
    {
        Type formal = typeof(RankOwner<>).GetGenericArguments()[0];
        Type method = typeof(RankOwner<>).GetMethod("Method").GetGenericArguments()[0];
        Type mdOne = Array.CreateInstance(typeof(int), new[] { 1 }, new[] { 1 }).GetType();
        Check(route + "-rank1-shape", mdOne.IsArray && !mdOne.IsSZArray
            && mdOne.GetArrayRank() == 1 && mdOne.GetElementType() == typeof(int) && mdOne != typeof(int[]));
        Type[] arrays = { typeof(int[]), mdOne, typeof(int[,]), typeof(int[,,]),
            typeof(string[]), typeof(RankOwner<int>[]), formal.MakeArrayType(), formal.MakeArrayType(1),
            formal.MakeArrayType(2), method.MakeArrayType(), method.MakeArrayType(3) };
        int[] ranks = { 1, 1, 2, 3, 1, 1, 1, 1, 2, 1, 3 };
        string[] arrayNames = { "sz", "md-one", "md-two", "md-three", "reference", "constructed-array",
            "formal-sz", "formal-md-one", "formal-md-two", "method-sz", "method-md-three" };
        for (int i = 0; i < arrays.Length; i++) Check(route + "-" + arrayNames[i], query(arrays[i]) == ranks[i]);
        Type[] nonarrays = { typeof(object), typeof(Array), typeof(void), typeof(int), typeof(RankEnum),
            typeof(RankOwner<>), typeof(RankOwner<int>), formal, method, typeof(int*),
            typeof(int).MakeByRefType(), typeof(int[]).MakePointerType(),
            typeof(delegate*<int, void>), typeof(delegate* unmanaged<int, void>) };
        string[] names = { "object", "array-base", "void", "int", "enum", "definition", "constructed",
            "formal", "method-formal", "pointer", "byref", "array-pointer", "function", "unmanaged-function" };
        for (int i = 0; i < nonarrays.Length; i++)
        {
            int index = i;
            Exception error = Error(() => query(nonarrays[index]));
            Check(route + "-nonarray-" + names[i], error is ArgumentException argument
                && argument.ParamName is null && argument.Message == "Must be an array type.");
        }
        Check(route + "-handle", query(Type.GetTypeFromHandle(typeof(int[,]).TypeHandle)) == 2);
        Check(route + "-repeat", query(typeof(int[])) == 1 && query(typeof(int[,,])) == 3);
    }

    private static void Nulls(string route, Func<Type, int> query)
    {
        Check(route + "-null", Error(() => query(null)) is NullReferenceException);
        Check(route + "-default-handle", Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    internal static void Run(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime array rank ==");
        Route("public", Public);
        Nulls("public", Public);
        Check("generic", Generic<int[]>() == 1 && Generic<string[,]>() == 2);
        Console.WriteLine("Runtime array rank end");
    }

    internal static void RunArtifact(string[] args)
    {
        Run(args);
        Route("direct", Direct); Nulls("direct", Direct);
        Route("calli", Pointer); Nulls("calli", Pointer);
        Route("virtual", Virtual); Nulls("virtual", Virtual);
        Route("virtual-calli", VirtualPointer); Nulls("virtual-calli", VirtualPointer);
        Route("constrained", Constrained); Nulls("constrained", Constrained);
        Route("address", AddressQuery);
        Route("virtual-address", VirtualAddressQuery); Nulls("virtual-address", VirtualAddressQuery);
        Route("flow-direct", FlowDirect);
        Route("flow-virtual", FlowVirtual); Nulls("flow-virtual", FlowVirtual);
        Check("address-bind-null", Error(() => Address(null)) is ArgumentException);
        Check("virtual-address-bind-null", Error(() => VirtualAddress(null)) is NullReferenceException);
        Check("flow-direct-bind-null", Error(() => FlowAddress(null, false)) is ArgumentException);
        Check("flow-virtual-bind-null", Error(() => FlowAddress(null, true)) is NullReferenceException);
        RunReflectionArtifact(Array.Empty<string>());
        if (args.Length > 0 && args[0] == "native-policy")
        {
            DelegateMetadata("address", Address(typeof(int[])));
            DelegateMetadata("virtual-address", VirtualAddress(typeof(int[])));
            DelegateMetadata("flow-direct", FlowAddress(typeof(int[]), false));
            DelegateMetadata("flow-virtual", FlowAddress(typeof(int[]), true));
        }
        Console.WriteLine("Runtime array rank routes end");
    }

    private static void DelegateMetadata(string route, Func<int> query)
    {
        MethodInfo method = query.Method;
        Check("native-" + route + "-row", method.Name == "GetArrayRank" && method.ReturnType == typeof(int)
            && !method.IsStatic && method.DeclaringType == ((object)typeof(int)).GetType()
            && method.DeclaringType.FullName == "System.RuntimeType");
        Check("native-" + route + "-invoke", (int)method.Invoke(typeof(int[]), null) == 1
            && (int)method.Invoke(typeof(int[,]), null) == 2
            && Error(() => method.Invoke(typeof(int), null)) is ArgumentException);
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime array rank reflection ==");
        Route("reflection", Reflection);
        Nulls("reflection", Reflection);
        if (args.Length > 0 && args[0] == "native-policy")
        {
            MethodInfo method = ReflectionMethod();
            Check("native-reflection-wrapper-row", method.Name == nameof(ReflectionLeaf)
                && method.DeclaringType == typeof(ArrayRankChecks) && method.IsStatic);
            // Native RuntimeType does not expose a general name-lookup method catalogue.
            Check("native-runtime-lookup-unavailable", ((object)typeof(int)).GetType()
                .GetMethod("GetArrayRank", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
        }
        Console.WriteLine("Runtime array rank reflection end");
    }
}
