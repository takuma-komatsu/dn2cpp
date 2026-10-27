using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RuntimeTypeFunctionPointerSubset;

internal static unsafe class FunctionPointerPredicateChecks
{
    private enum FunctionPointerEnum : byte { Value }
    private class FunctionPointerOwner<T>
    {
        public static void Method<U>() { }
    }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("function-pointer-predicate-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool FunctionPublic(Type type) => type.IsFunctionPointer;
    private static bool UnmanagedPublic(Type type) => type.IsUnmanagedFunctionPointer;
    private static bool FunctionDirect(Type type) => FunctionPublic(type);
    private static bool UnmanagedDirect(Type type) => UnmanagedPublic(type);
    private static bool FunctionPointer(Type type) => FunctionPublic(type);
    private static bool UnmanagedPointer(Type type) => UnmanagedPublic(type);
    private static bool FunctionGetter(Type type) => FunctionPublic(type);
    private static bool UnmanagedGetter(Type type) => UnmanagedPublic(type);
    private static bool FunctionVirtualGetter(Type type) => FunctionPublic(type);
    private static bool UnmanagedVirtualGetter(Type type) => UnmanagedPublic(type);
    private static bool FunctionConstrainedGetter(Type type) => FunctionPublic(type);
    private static bool UnmanagedConstrainedGetter(Type type) => UnmanagedPublic(type);
    private static Func<Type, bool> FunctionAddress() => FunctionPublic;
    private static Func<Type, bool> UnmanagedAddress() => UnmanagedPublic;
    private static Func<bool> FunctionGetterAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => FunctionPublic(type);
    }
    private static Func<bool> UnmanagedGetterAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => UnmanagedPublic(type);
    }
    private static Func<bool> FunctionFlowGetterAddress(Type type, bool virtualAddress) =>
        virtualAddress ? FunctionGetterAddress(type) : () => FunctionPublic(type);
    private static Func<bool> UnmanagedFlowGetterAddress(Type type, bool virtualAddress) =>
        virtualAddress ? UnmanagedGetterAddress(type) : () => UnmanagedPublic(type);
    private static bool FunctionFlowDirect(Type type) => FunctionFlowGetterAddress(type, false)();
    private static bool UnmanagedFlowDirect(Type type) => UnmanagedFlowGetterAddress(type, false)();
    private static bool FunctionFlowVirtual(Type type) => FunctionFlowGetterAddress(type, true)();
    private static bool UnmanagedFlowVirtual(Type type) => UnmanagedFlowGetterAddress(type, true)();
    private static bool FunctionVirtualAddress(Type type) => FunctionGetterAddress(type)();
    private static bool UnmanagedVirtualAddress(Type type) => UnmanagedGetterAddress(type)();
    private static MethodInfo FunctionReflectionMethod() => typeof(FunctionPointerPredicateChecks)
        .GetMethod(nameof(FunctionPublic), BindingFlags.NonPublic | BindingFlags.Static);
    private static MethodInfo UnmanagedReflectionMethod() => typeof(FunctionPointerPredicateChecks)
        .GetMethod(nameof(UnmanagedPublic), BindingFlags.NonPublic | BindingFlags.Static);
    private static bool FunctionReflection(Type type) => (bool)FunctionReflectionMethod().Invoke(null, new object[] { type });
    private static bool UnmanagedReflection(Type type) => (bool)UnmanagedReflectionMethod().Invoke(null, new object[] { type });
    private static bool GenericFunction<T>() => typeof(T).IsFunctionPointer;
    private static bool GenericUnmanaged<T>() => typeof(T).IsUnmanagedFunctionPointer;

    private static void Route(string route, Func<Type, bool> function, Func<Type, bool> unmanaged)
    {
        Type formal = typeof(FunctionPointerOwner<>).GetGenericArguments()[0];
        Type methodFormal = typeof(FunctionPointerOwner<int>).GetMethod("Method").GetGenericArguments()[0];
        Type managed = typeof(delegate*<int, void>);
        Type native = typeof(delegate* unmanaged<int, void>);
        Type[] types = { managed, typeof(delegate* managed<int, void>), native,
            typeof(delegate* unmanaged[Cdecl]<int, void>), typeof(delegate* unmanaged[Stdcall]<int, void>),
            typeof(delegate* unmanaged[Thiscall]<int, void>), typeof(delegate* unmanaged[Fastcall]<int, void>),
            typeof(delegate* unmanaged[SuppressGCTransition]<int, void>),
            typeof(delegate* unmanaged[Cdecl, SuppressGCTransition]<int, void>),
            typeof(delegate*<delegate* unmanaged<int>, void>), typeof(delegate* unmanaged<int, void>*),
            native.MakeByRefType(), typeof(delegate* unmanaged<int, void>[]), typeof(int*), typeof(int).MakeByRefType(),
            typeof(FunctionPointerEnum), typeof(int), typeof(void), typeof(string), typeof(int[]),
            typeof(FunctionPointerOwner<>), formal, methodFormal, typeof(FunctionPointerOwner<int>) };
        string[] labels = { "managed", "managed-explicit", "unmanaged", "cdecl", "stdcall", "thiscall", "fastcall",
            "suppress-gc", "cdecl-suppress-gc", "nested-managed", "pointer-wrapper", "byref-wrapper", "array-wrapper",
            "pointer", "byref", "enum", "value", "void", "class", "array", "open", "type-formal", "method-formal", "closed" };
        for (int i = 0; i < types.Length; i++)
            Check(route + "-" + labels[i], function(types[i]) == (i < 10) && unmanaged(types[i]) == (i >= 2 && i <= 8));
        Check(route + "-handle-managed", function(Type.GetTypeFromHandle(managed.TypeHandle))
            && !unmanaged(Type.GetTypeFromHandle(managed.TypeHandle)));
        Check(route + "-handle-unmanaged", function(Type.GetTypeFromHandle(native.TypeHandle))
            && unmanaged(Type.GetTypeFromHandle(native.TypeHandle)));
        Check(route + "-repeated", function(managed) && !function(typeof(int)) && function(managed)
            && unmanaged(native) && !unmanaged(managed) && unmanaged(native));
    }

    private static void ManagedNulls(string route, Func<Type, bool> function)
    {
        Check(route + "-managed-null", Error(() => function(null)) is NullReferenceException);
        Check(route + "-managed-default-handle", Error(() => function(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    private static void PublicNulls(string route, Func<Type, bool> function, Func<Type, bool> unmanaged)
    {
        ManagedNulls(route, function);
        Check(route + "-unmanaged-null", Error(() => unmanaged(null)) is NullReferenceException);
        Check(route + "-unmanaged-default-handle", Error(() => unmanaged(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    private static void NativeUnmanagedNulls(string route, Func<Type, bool> unmanaged)
    {
        // The raw VM predicate requires nonnull input; this is the native safety policy, not a CLR oracle.
        Check("native-safety-" + route, Error(() => unmanaged(null)) is NullReferenceException
            && Error(() => unmanaged(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime function-pointer predicates ==");
        Route("public", FunctionPublic, UnmanagedPublic);
        PublicNulls("public", FunctionPublic, UnmanagedPublic);
        Check("generic", !GenericFunction<int>() && !GenericFunction<string>() && !GenericFunction<FunctionPointerOwner<int>>()
            && !GenericUnmanaged<int>() && !GenericUnmanaged<string>() && !GenericUnmanaged<FunctionPointerOwner<int>>());
        Console.WriteLine("Runtime function-pointer predicates end");
    }

    // The fixture changes only native inputs. The CLR oracle uses the public flag properties.
    internal static void RunArtifact(string[] args)
    {
        Run();
        Route("direct", FunctionDirect, UnmanagedDirect);
        ManagedNulls("direct", FunctionDirect);
        Route("calli", FunctionPointer, UnmanagedPointer);
        ManagedNulls("calli", FunctionPointer);
        Route("getter", FunctionGetter, UnmanagedGetter);
        ManagedNulls("getter", FunctionGetter);
        Route("virtual-getter", FunctionVirtualGetter, UnmanagedVirtualGetter);
        PublicNulls("virtual-getter", FunctionVirtualGetter, UnmanagedVirtualGetter);
        Route("constrained-getter", FunctionConstrainedGetter, UnmanagedConstrainedGetter);
        PublicNulls("constrained-getter", FunctionConstrainedGetter, UnmanagedConstrainedGetter);
        Route("address", FunctionAddress(), UnmanagedAddress());
        ManagedNulls("address", FunctionAddress());
        Route("virtual-address", FunctionVirtualAddress, UnmanagedVirtualAddress);
        PublicNulls("virtual-address", FunctionVirtualAddress, UnmanagedVirtualAddress);
        Check("virtual-address-bind-null", Error(() => FunctionGetterAddress(null)) is NullReferenceException
            && Error(() => UnmanagedGetterAddress(null)) is NullReferenceException);
        Route("flow-direct", FunctionFlowDirect, UnmanagedFlowDirect);
        Route("flow-virtual", FunctionFlowVirtual, UnmanagedFlowVirtual);
        PublicNulls("flow-virtual", FunctionFlowVirtual, UnmanagedFlowVirtual);
        Check("flow-virtual-bind-null", Error(() => FunctionFlowGetterAddress(null, true)) is NullReferenceException
            && Error(() => UnmanagedFlowGetterAddress(null, true)) is NullReferenceException);
        RunReflectionArtifact(Array.Empty<string>());
        if (args.Length > 0 && args[0] == "native-policy")
        {
            NativeUnmanagedNulls("direct", UnmanagedDirect);
            NativeUnmanagedNulls("calli", UnmanagedPointer);
            NativeUnmanagedNulls("getter", UnmanagedGetter);
            NativeUnmanagedNulls("address", UnmanagedAddress());
            NativeReflectionChecks();
            Check("native-address-row", FunctionAddress().Method.Name == "IsFunctionPointer"
                && UnmanagedAddress().Method.Name == "IsUnmanagedFunctionPointer"
                && FunctionAddress().Method.DeclaringType == typeof(RuntimeTypeHandle)
                && UnmanagedAddress().Method.DeclaringType == typeof(RuntimeTypeHandle));
            Check("native-getter-address-row", FunctionGetterAddress(typeof(int)).Method.Name == "get_IsFunctionPointer"
                && UnmanagedGetterAddress(typeof(int)).Method.Name == "get_IsUnmanagedFunctionPointer"
                && FunctionGetterAddress(typeof(int)).Method.DeclaringType.FullName == "System.RuntimeType"
                && UnmanagedGetterAddress(typeof(int)).Method.DeclaringType.FullName == "System.RuntimeType");
            Check("native-flow-address-row", GetterIdentity(FunctionFlowGetterAddress(typeof(int), false), "get_IsFunctionPointer")
                && GetterIdentity(FunctionFlowGetterAddress(typeof(int), true), "get_IsFunctionPointer")
                && GetterIdentity(UnmanagedFlowGetterAddress(typeof(int), false), "get_IsUnmanagedFunctionPointer")
                && GetterIdentity(UnmanagedFlowGetterAddress(typeof(int), true), "get_IsUnmanagedFunctionPointer"));
        }
        Console.WriteLine("Runtime function-pointer predicate routes end");
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime function-pointer reflection ==");
        Route("reflection", FunctionReflection, UnmanagedReflection);
        ManagedNulls("reflection", FunctionReflection);
        if (args.Length > 0 && args[0] == "native-policy") NativeReflectionChecks();
        Console.WriteLine("Runtime function-pointer reflection end");
    }

    private static bool GetterIdentity(Func<bool> getter, string name) =>
        getter.Method.Name == name && getter.Method.DeclaringType.FullName == "System.RuntimeType";

    private static void NativeReflectionChecks()
    {
        NativeUnmanagedNulls("reflection", UnmanagedReflection);
        Check("native-reflection-row", FunctionReflectionMethod().Name == "IsFunctionPointer"
            && UnmanagedReflectionMethod().Name == "IsUnmanagedFunctionPointer"
            && FunctionReflectionMethod().DeclaringType == typeof(RuntimeTypeHandle)
            && UnmanagedReflectionMethod().DeclaringType == typeof(RuntimeTypeHandle));
    }
}
