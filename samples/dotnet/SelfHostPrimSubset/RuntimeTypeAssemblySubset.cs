using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeAssemblySubset;

internal static unsafe class RuntimeAssemblyChecks
{
    private struct AssemblyValue { }
    private enum AssemblyEnum { Value }
    private class AssemblyOwner<T>
    {
        public static void Method<U>() { }
    }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("runtime-type-assembly-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static Assembly Public(Type type) => type.Assembly;
    private static Assembly Direct(Type type) => Public(type);
    private static Assembly Pointer(Type type) => Public(type);
    private static Assembly Getter(Type type) => Public(type);
    private static Assembly GetterPointer(Type type) => Public(type);
    private static Assembly VirtualGetter(Type type) => Public(type);
    private static Assembly ConstrainedGetter(Type type) => Public(type);
    private static Func<Type, Assembly> Address() => Public;
    private static Func<Assembly> GetterAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => Public(type);
    }
    private static Assembly VirtualAddress(Type type) => GetterAddress(type)();
    private static Func<Assembly> GetterAddressLocal(Type type) => GetterAddress(type);
    private static Assembly VirtualAddressLocal(Type type) => GetterAddressLocal(type)();
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
    private static MethodInfo ReflectionMethod() => typeof(RuntimeAssemblyChecks)
        .GetMethod(nameof(Public), BindingFlags.Static | BindingFlags.NonPublic);
    private static Assembly Reflection(Type type) => (Assembly)ReflectionMethod().Invoke(null, new object[] { type });
    private static object ObjectReflection(Type type) => ReflectionMethod().Invoke(null, new object[] { type });
    private static object Generic<T>() => typeof(T).Assembly;

    private static void Route(string route, Func<Type, Assembly> read)
    {
        Assembly core = typeof(int).Assembly;
        Assembly app = typeof(RuntimeAssemblyChecks).Assembly;
        Assembly external = typeof(System.Linq.Enumerable).Assembly;
        Type formal = typeof(AssemblyOwner<>).GetGenericArguments()[0];
        Type methodFormal = typeof(AssemblyOwner<int>).GetMethod("Method").GetGenericArguments()[0];
        Type function = typeof(delegate*<int, AssemblyValue>);
        Type rankOneArray = Array.CreateInstance(typeof(AssemblyValue), new[] { 1 }, new[] { 1 }).GetType();
        Check(route + "-rank-one-shape", !rankOneArray.IsSZArray && rankOneArray.GetArrayRank() == 1);
        Type[] types = { typeof(object), typeof(void), typeof(AssemblyValue), typeof(AssemblyEnum),
            typeof(System.Linq.Enumerable), typeof(AssemblyOwner<>), typeof(AssemblyOwner<string>), typeof(List<AssemblyValue>),
            formal, methodFormal, typeof(List<>).GetGenericArguments()[0], typeof(AssemblyValue[]), typeof(AssemblyValue[,]),
            rankOneArray, typeof(AssemblyValue*), typeof(AssemblyValue).MakeByRefType(),
            formal.MakeArrayType(), formal.MakePointerType(), formal.MakeByRefType(),
            typeof(delegate*<AssemblyValue, int>), function, typeof(delegate* unmanaged[Cdecl]<int, AssemblyValue>),
            typeof(delegate*<AssemblyValue*>), typeof(delegate*<ref AssemblyValue>),
            typeof(delegate*<delegate*<AssemblyValue>>), typeof(delegate*<List<AssemblyValue>>),
            typeof(delegate*<AssemblyOwner<string>>), function.MakePointerType(), function.MakeByRefType(), function.MakeArrayType() };
        string[] labels = { "object", "void", "value", "enum", "external", "open", "closed-app", "closed-core",
            "type-formal", "method-formal", "core-formal", "sz-array", "md-array", "rank-one-array", "pointer", "byref",
            "formal-array", "formal-pointer", "formal-byref", "function-argument", "function-return", "function-unmanaged",
            "function-pointer-return", "function-byref-return", "function-nested-return", "function-core-generic-return",
            "function-app-generic-return", "function-pointer-wrapper", "function-byref-wrapper", "function-array-wrapper" };
        for (int i = 0; i < types.Length; i++)
        {
            Assembly expected = i == 4 ? external : i is 0 or 1 or 7 or 10 or 19 or 25 ? core : app;
            Assembly actual = read(types[i]);
            object escaped = actual;
            Check(route + "-" + labels[i], actual == expected && ReferenceEquals(escaped, expected)
                && ReferenceEquals(actual, read(types[i])) && escaped is Assembly
                && escaped.GetType().FullName == "System.Reflection.RuntimeAssembly"
                && ((Assembly)escaped).FullName == expected.FullName);
        }
        Check(route + "-handle", ReferenceEquals(read(Type.GetTypeFromHandle(typeof(AssemblyValue).TypeHandle)), app));
        Check(route + "-recovery", ReferenceEquals(read(typeof(int)), core)
            && ReferenceEquals(read(typeof(AssemblyValue)), app) && ReferenceEquals(read(typeof(int)), core));
    }

    private static void PublicNulls(string route, Func<Type, Assembly> read)
    {
        Check(route + "-null", Error(() => read(null)) is NullReferenceException);
        Check(route + "-default-handle", Error(() => read(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    private static void NativeNulls(string route, Func<Type, Assembly> read)
    {
        Check("native-" + route + "-null", InvalidHandle(Error(() => read(null))));
        Check("native-" + route + "-default-handle", InvalidHandle(Error(() => read(Type.GetTypeFromHandle(default)))));
    }

    private static bool InvalidHandle(Exception error) => error is ArgumentNullException argument
        && argument.ParamName is null && argument.Message == "Invalid handle."
        && argument.HResult == unchecked((int)0x80004003);

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type assembly contract ==");
        Check("assembly-origins", typeof(int).Assembly.GetName().Name == "System.Private.CoreLib"
            && typeof(RuntimeAssemblyChecks).Assembly.GetName().Name == "SelfHostPrimSubset"
            && typeof(System.Linq.Enumerable).Assembly.GetName().Name == "System.Linq"
            && typeof(RuntimeAssemblyChecks).Assembly != typeof(int).Assembly
            && typeof(System.Linq.Enumerable).Assembly != typeof(int).Assembly);
        Route("public", Public);
        PublicNulls("public", Public);
        Check("generic", IsRuntimeAssemblyObject(Generic<string>(), Generic<string>(), typeof(int).Assembly)
            && IsRuntimeAssemblyObject(Generic<AssemblyValue>(), Generic<AssemblyValue>(), typeof(RuntimeAssemblyChecks).Assembly)
            && IsRuntimeAssemblyObject(Generic<List<AssemblyValue>>(), Generic<List<AssemblyValue>>(), typeof(int).Assembly));
        Type function = typeof(delegate*<AssemblyValue>);
        Check("function-shape", function.IsFunctionPointer && !function.HasElementType && function.GetElementType() is null);
        Console.WriteLine("Runtime type assembly contract end");
    }

    // Only the rewritten native input calls private helpers; the CLR oracle stays public.
    internal static void RunArtifact(string[] args)
    {
        Run();
        Route("direct", Direct);
        Route("calli", Pointer);
        Route("getter", Getter);
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
            NativeNulls("address", Address());
            NativeReflectionChecks();
            Check("native-address-row", Address().Method.Name == "GetAssembly"
                && Address().Method.DeclaringType == typeof(RuntimeTypeHandle)
                && Address().Method.ReturnType.FullName == "System.Reflection.RuntimeAssembly");
            NativeGetterDelegateChecks("getter-address", GetterAddress(typeof(AssemblyValue)));
            NativeGetterDelegateChecks("getter-address-local", GetterAddressLocal(typeof(AssemblyValue)));
        }
        Console.WriteLine("Runtime type assembly routes end");
    }

    private static void ObjectRoute(string route, Func<Type, object> read)
    {
        Check("object-" + route, IsRuntimeAssemblyObject(read(typeof(AssemblyValue)), read(typeof(AssemblyValue)),
            typeof(RuntimeAssemblyChecks).Assembly));
    }

    // Check pointer identity before reading the result's object header.
    private static bool IsRuntimeAssemblyObject(object first, object repeated, object expected) =>
        ReferenceEquals(first, expected) && ReferenceEquals(first, repeated) && first is Assembly
        && first.GetType().FullName == "System.Reflection.RuntimeAssembly"
        && ((Assembly)first).FullName == ((Assembly)expected).FullName;

    private static void NativeGetterDelegateChecks(string route, Func<Assembly> read)
    {
        MethodInfo method = read.Method;
        Check("native-" + route + "-row", method is not null && method.Name == "get_Assembly"
            && method.DeclaringType == ((object)typeof(int)).GetType() && method.DeclaringType.FullName == "System.RuntimeType"
            && method.ReturnType == typeof(Assembly));
        Check("native-" + route + "-invoke", IsRuntimeAssemblyObject(method.Invoke(typeof(AssemblyValue), null),
            method.Invoke(typeof(AssemblyValue), null), typeof(RuntimeAssemblyChecks).Assembly));
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type assembly reflection ==");
        Route("reflection", Reflection);
        ObjectRoute("reflection", ObjectReflection);
        if (args.Length > 0 && args[0] == "native-policy") NativeReflectionChecks();
        Console.WriteLine("Runtime type assembly reflection end");
    }

    private static void NativeReflectionChecks()
    {
        Check("native-getter-lookup-unavailable", ((object)typeof(int)).GetType()
            .GetMethod("get_Assembly", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
        NativeNulls("reflection", Reflection);
        MethodInfo method = ReflectionMethod();
        Check("native-reflection-row", method.Name == "GetAssembly" && method.DeclaringType == typeof(RuntimeTypeHandle)
            && method.ReturnType.FullName == "System.Reflection.RuntimeAssembly"
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.FullName == "System.RuntimeType");
    }
}
