using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeModuleSubset;

internal static unsafe class RuntimeModuleChecks
{
    private struct ModuleValue { }
    private enum ModuleEnum { Value }
    private class ModuleOwner<T>
    {
        public static void Method<U>() { }
    }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("runtime-type-module-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static Module Public(Type type) => type.Module;
    private static Module Direct(Type type) => Public(type);
    private static Module Pointer(Type type) => Public(type);
    private static Module Getter(Type type) => Public(type);
    private static Module RuntimeGetter(Type type) => Public(type);
    private static Module RuntimeGetterPointer(Type type) => Public(type);
    private static object ObjectRuntimeGetter(Type type) => Public(type);
    private static Module GetterPointer(Type type) => Public(type);
    private static Module VirtualGetter(Type type) => Public(type);
    private static Module ConstrainedGetter(Type type) => Public(type);
    private static Func<Type, Module> Address() => Public;
    private static Func<Module> GetterAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => Public(type);
    }
    private static Module VirtualAddress(Type type) => GetterAddress(type)();
    private static Func<Module> GetterAddressLocal(Type type) => GetterAddress(type);
    private static Module VirtualAddressLocal(Type type) => GetterAddressLocal(type)();
    private static object ObjectDirect(Type type) => Public(type);
    private static object ObjectPointer(Type type) => Public(type);
    private static object ObjectLocal(Type type) => Public(type);
    private static object ObjectAddress(Type type)
    {
        Func<Type, object> read = Address();
        return read(type);
    }
    private static object ObjectGetterAddress(Type type)
    {
        Func<object> read = GetterAddress(type);
        return read();
    }
    private static object ObjectGetterAddressLocal(Type type)
    {
        Func<object> read = GetterAddressLocal(type);
        return read();
    }
    private static object ObjectDynamicAddress(Type type) => Address().DynamicInvoke(type);
    private static object ObjectDynamicGetterAddress(Type type) => GetterAddress(type).DynamicInvoke();
    private static MethodInfo ReflectionMethod() => typeof(RuntimeModuleChecks)
        .GetMethod(nameof(Public), BindingFlags.Static | BindingFlags.NonPublic);
    private static Module Reflection(Type type) => (Module)ReflectionMethod().Invoke(null, new object[] { type });
    private static object ObjectReflection(Type type) => ReflectionMethod().Invoke(null, new object[] { type });
    private static object Generic<T>() => typeof(T).Module;

    private static void Route(string route, Func<Type, Module> read)
    {
        Module core = typeof(int).Module;
        Module app = typeof(RuntimeModuleChecks).Module;
        Module external = typeof(System.Linq.Enumerable).Module;
        Type formal = typeof(ModuleOwner<>).GetGenericArguments()[0];
        Type methodFormal = typeof(ModuleOwner<int>).GetMethod("Method").GetGenericArguments()[0];
        Type function = typeof(delegate*<int, ModuleValue>);
        Type rankOneArray = Array.CreateInstance(typeof(ModuleValue), new[] { 1 }, new[] { 1 }).GetType();
        Check(route + "-rank-one-shape", !rankOneArray.IsSZArray && rankOneArray.GetArrayRank() == 1);
        Type[] types = { typeof(object), typeof(void), typeof(ModuleValue), typeof(ModuleEnum),
            typeof(System.Linq.Enumerable), typeof(ModuleOwner<>), typeof(ModuleOwner<string>), typeof(List<ModuleValue>),
            formal, methodFormal, typeof(List<>).GetGenericArguments()[0], typeof(ModuleValue[]), typeof(ModuleValue[,]),
            rankOneArray, typeof(ModuleValue*), typeof(ModuleValue).MakeByRefType(),
            formal.MakeArrayType(), formal.MakePointerType(), formal.MakeByRefType(),
            typeof(delegate*<ModuleValue, int>), function, typeof(delegate* unmanaged[Cdecl]<int, ModuleValue>),
            typeof(delegate*<ModuleValue*>), typeof(delegate*<ref ModuleValue>),
            typeof(delegate*<delegate*<ModuleValue>>), typeof(delegate*<List<ModuleValue>>),
            typeof(delegate*<ModuleOwner<string>>), function.MakePointerType(), function.MakeByRefType(), function.MakeArrayType() };
        string[] labels = { "object", "void", "value", "enum", "external", "open", "closed-app", "closed-core",
            "type-formal", "method-formal", "core-formal", "sz-array", "md-array", "rank-one-array", "pointer", "byref",
            "formal-array", "formal-pointer", "formal-byref", "function-argument", "function-return", "function-unmanaged",
            "function-pointer-return", "function-byref-return", "function-nested-return", "function-core-generic-return",
            "function-app-generic-return", "function-pointer-wrapper", "function-byref-wrapper", "function-array-wrapper" };
        for (int i = 0; i < types.Length; i++)
        {
            Module expected = i == 4 ? external : i is 0 or 1 or 7 or 10 or 19 or 25 ? core : app;
            Module actual = read(types[i]);
            object escaped = actual;
            Check(route + "-" + labels[i], actual == expected && ReferenceEquals(escaped, expected)
                && ReferenceEquals(actual, read(types[i])) && escaped is Module
                && escaped.GetType().FullName == "System.Reflection.RuntimeModule"
                && ((Module)escaped).ScopeName == expected.ScopeName
                && actual.Assembly == types[i].Assembly && ReferenceEquals(actual, actual.Assembly.ManifestModule)
                && !ReferenceEquals(escaped, (object)actual.Assembly));
        }
        Check(route + "-handle", ReferenceEquals(read(Type.GetTypeFromHandle(typeof(ModuleValue).TypeHandle)), app));
        Check(route + "-recovery", ReferenceEquals(read(typeof(int)), core)
            && ReferenceEquals(read(typeof(ModuleValue)), app) && ReferenceEquals(read(typeof(int)), core));
    }

    private static void PublicNulls(string route, Func<Type, Module> read)
    {
        Check(route + "-null", Error(() => read(null)) is NullReferenceException);
        Check(route + "-default-handle", Error(() => read(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    private static void NativeNulls(string route, Func<Type, Module> read)
    {
        Check("native-" + route + "-null", InvalidHandle(Error(() => read(null))));
        Check("native-" + route + "-default-handle", InvalidHandle(Error(() => read(Type.GetTypeFromHandle(default)))));
    }

    private static bool InvalidHandle(Exception error) => error is NullReferenceException;

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type module contract ==");
        Check("module-origins", typeof(int).Module.Name == "System.Private.CoreLib.dll"
            && typeof(RuntimeModuleChecks).Module.Name == "SelfHostPrimSubset.dll"
            && typeof(System.Linq.Enumerable).Module.Name == "System.Linq.dll"
            && typeof(RuntimeModuleChecks).Module != typeof(int).Module
            && typeof(System.Linq.Enumerable).Module != typeof(int).Module);
        Route("public", Public);
        PublicNulls("public", Public);
        Check("generic", IsRuntimeModuleObject(Generic<string>(), Generic<string>(), typeof(int).Module)
            && IsRuntimeModuleObject(Generic<ModuleValue>(), Generic<ModuleValue>(), typeof(RuntimeModuleChecks).Module)
            && IsRuntimeModuleObject(Generic<List<ModuleValue>>(), Generic<List<ModuleValue>>(), typeof(int).Module));
        Type function = typeof(delegate*<ModuleValue>);
        Check("function-shape", function.IsFunctionPointer && !function.HasElementType && function.GetElementType() is null);
        Console.WriteLine("Runtime type module contract end");
    }

    // Only the rewritten native input calls private helpers; the CLR oracle stays public.
    internal static void RunArtifact(string[] args)
    {
        Run();
        Route("direct", Direct);
        Route("calli", Pointer);
        Route("getter", Getter);
        Route("runtime-getter", RuntimeGetter);
        Route("runtime-getter-calli", RuntimeGetterPointer);
        Route("getter-calli", GetterPointer);
        PublicNulls("getter-calli", GetterPointer);
        Route("virtual-getter", VirtualGetter);
        PublicNulls("virtual-getter", VirtualGetter);
        Route("constrained-getter", ConstrainedGetter);
        PublicNulls("constrained-getter", ConstrainedGetter);
        Route("address", Address());
        Route("virtual-address", VirtualAddress);
        PublicNulls("virtual-address", VirtualAddress);
        Check("virtual-address-bind-null", Error(() => GetterAddress(null)) is NullReferenceException);
        Route("virtual-address-local", VirtualAddressLocal);
        PublicNulls("virtual-address-local", VirtualAddressLocal);
        Check("virtual-address-local-bind-null", Error(() => GetterAddressLocal(null)) is NullReferenceException);
        ObjectRoute("direct", ObjectDirect);
        ObjectRoute("calli", ObjectPointer);
        ObjectRoute("local", ObjectLocal);
        ObjectRoute("runtime-getter", ObjectRuntimeGetter);
        ObjectRoute("address-covariant", ObjectAddress);
        ObjectRoute("getter-address-covariant", ObjectGetterAddress);
        ObjectRoute("getter-address-local-covariant", ObjectGetterAddressLocal);
        ObjectRoute("address-dynamic", ObjectDynamicAddress);
        ObjectRoute("getter-address-dynamic", ObjectDynamicGetterAddress);
        RunReflectionArtifact(Array.Empty<string>());
        if (args.Length > 0 && args[0] == "native-policy")
        {
            NativeNulls("direct", Direct);
            NativeNulls("calli", Pointer);
            NativeNulls("getter", Getter);
            NativeNulls("runtime-getter", RuntimeGetter);
            NativeNulls("runtime-getter-calli", RuntimeGetterPointer);
            NativeNulls("address", Address());
            NativeReflectionChecks();
            Check("native-address-row", Address().Method.Name == "GetModule"
                && Address().Method.DeclaringType == typeof(RuntimeTypeHandle)
                && Address().Method.ReturnType.FullName == "System.Reflection.RuntimeModule");
            NativeGetterDelegateChecks("getter-address", GetterAddress(typeof(ModuleValue)));
            NativeGetterDelegateChecks("getter-address-local", GetterAddressLocal(typeof(ModuleValue)));
        }
        Console.WriteLine("Runtime type module routes end");
    }

    private static void ObjectRoute(string route, Func<Type, object> read)
    {
        Check("object-" + route, IsRuntimeModuleObject(read(typeof(ModuleValue)), read(typeof(ModuleValue)),
            typeof(RuntimeModuleChecks).Module));
    }

    // Check pointer identity before reading the result's object header.
    private static bool IsRuntimeModuleObject(object first, object repeated, object expected) =>
        ReferenceEquals(first, expected) && ReferenceEquals(first, repeated) && first is Module
        && first.GetType().FullName == "System.Reflection.RuntimeModule"
        && ((Module)first).ScopeName == ((Module)expected).ScopeName;

    private static void NativeGetterDelegateChecks(string route, Func<Module> read)
    {
        MethodInfo method = read.Method;
        Check("native-" + route + "-row", method is not null && method.Name == "get_Module"
            && method.DeclaringType == ((object)typeof(int)).GetType() && method.DeclaringType.FullName == "System.RuntimeType"
            && method.ReturnType == typeof(Module));
        Check("native-" + route + "-invoke", IsRuntimeModuleObject(method.Invoke(typeof(ModuleValue), null),
            method.Invoke(typeof(ModuleValue), null), typeof(RuntimeModuleChecks).Module));
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type module reflection ==");
        Route("reflection", Reflection);
        ObjectRoute("reflection", ObjectReflection);
        if (args.Length > 0 && args[0] == "native-policy") NativeReflectionChecks();
        Console.WriteLine("Runtime type module reflection end");
    }

    private static void NativeReflectionChecks()
    {
        Check("native-getter-lookup-unavailable", ((object)typeof(int)).GetType()
            .GetMethod("get_Module", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
        NativeNulls("reflection", Reflection);
        MethodInfo method = ReflectionMethod();
        Check("native-reflection-row", method.Name == "GetModule" && method.DeclaringType == typeof(RuntimeTypeHandle)
            && method.ReturnType.FullName == "System.Reflection.RuntimeModule"
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.FullName == "System.RuntimeType");
    }
}
