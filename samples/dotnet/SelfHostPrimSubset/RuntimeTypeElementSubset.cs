using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeElementSubset;

internal static unsafe class ElementChecks
{
    private enum ElementEnum { Value }
    private class ElementOwner<T, U>
    {
        public class Nested<V> { }
        public static void Method<A>() { }
    }
    private static void Check(string label, bool success)
    {
        Console.WriteLine("element-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }
    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }
    private static Type Public(Type type) => type.GetElementType();
    private static Type Direct(Type type) => Public(type);
    private static Type Pointer(Type type) => Public(type);
    private static Type Getter(Type type) => Public(type);
    private static Type GetterPointer(Type type) => Public(type);
    private static Type VirtualGetter(Type type) => Public(type);
    private static Type ConstrainedGetter(Type type) => Public(type);
    private static Func<Type, Type> Address() => Public;
    private static Func<Type> GetterAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => Public(type);
    }
    private static Type VirtualAddress(Type type) => GetterAddress(type)();
    private static Func<Type> FlowGetterAddress(Type type, bool virtualAddress) =>
        virtualAddress ? GetterAddress(type) : () => Public(type);
    private static Type FlowDirect(Type type) => FlowGetterAddress(type, false)();
    private static Type FlowVirtual(Type type) => FlowGetterAddress(type, true)();
    private static MethodInfo ReflectionMethod() => typeof(ElementChecks)
        .GetMethod(nameof(Public), BindingFlags.Static | BindingFlags.NonPublic);
    private static Type Reflection(Type type) => (Type)ReflectionMethod().Invoke(null, new object[] { type });
    private static Type Generic<T>() => typeof(T[]).GetElementType();

    private static void Route(string route, Func<Type, Type> query)
    {
        Type formal = typeof(ElementOwner<,>).GetGenericArguments()[1];
        Type method = typeof(ElementOwner<,>).GetMethod("Method").GetGenericArguments()[0];
        Type closedMethod = typeof(ElementOwner<int, string>).GetMethod("Method").GetGenericArguments()[0];
        Type open = typeof(ElementOwner<,>).MakeGenericType(new[] { typeof(int), formal });
        Type function = typeof(delegate*<int, void>);
        Type rankOne = Array.CreateInstance(typeof(int), new[] { 1 }, new[] { 1 }).GetType();
        Check(route + "-rank-one-shape", rankOne.IsArray && !rankOne.IsSZArray && rankOne.GetArrayRank() == 1 && rankOne != typeof(int[]));
        Type[] types = { typeof(object), typeof(void), typeof(int), typeof(string), typeof(ElementEnum), typeof(Array),
            typeof(ElementOwner<,>), typeof(ElementOwner<,>.Nested<>), typeof(ElementOwner<int, string>), open,
            formal, method, closedMethod, function, typeof(delegate* unmanaged<int, void>),
            typeof(int[]), typeof(string[]), typeof(ElementEnum[]), typeof(int[,]), rankOne, typeof(int[][]),
            typeof(int*), typeof(void*), typeof(int**), typeof(int).MakeByRefType(),
            formal.MakeArrayType(), formal.MakeArrayType(1), formal.MakeArrayType(2), formal.MakePointerType(), formal.MakeByRefType(),
            method.MakeArrayType(), method.MakePointerType(), method.MakeByRefType(), open.MakeArrayType(),
            function.MakePointerType(), function.MakeArrayType(), formal.MakeArrayType().MakePointerType().MakeByRefType() };
        Type[] expected = { null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null,
            typeof(int), typeof(string), typeof(ElementEnum), typeof(int), typeof(int), typeof(int[]),
            typeof(int), typeof(void), typeof(int*), typeof(int),
            formal, formal, formal, formal, formal, method, method, method, open,
            function, function, formal.MakeArrayType().MakePointerType() };
        string[] labels = { "object", "void", "value", "string", "enum", "array-base",
            "definition", "nested-definition", "closed-generic", "open-constructed",
            "type-formal", "method-formal", "closed-owner-formal", "function-managed", "function-unmanaged",
            "int-array", "string-array", "enum-array", "mdarray-two", "mdarray-one", "jagged-array",
            "pointer", "void-pointer", "nested-pointer", "byref",
            "formal-array", "formal-mdarray-one", "formal-mdarray-two", "formal-pointer", "formal-byref",
            "method-array", "method-pointer", "method-byref", "open-array",
            "function-pointer", "function-array", "nested-compound" };
        for (int i = 0; i < types.Length; i++)
        {
            Type element = query(types[i]);
            Check(route + "-" + labels[i], ReferenceEquals(element, expected[i])
                && ReferenceEquals(element, query(types[i])) && ReferenceEquals(element, Public(types[i])));
        }
        Check(route + "-handle-element", ReferenceEquals(query(Type.GetTypeFromHandle(typeof(int[]).TypeHandle)), typeof(int)));
        Check(route + "-object-escape", (object)query(typeof(int[])) is Type result && result == typeof(int));
        Check(route + "-null-result", query(typeof(ElementOwner<int, string>)) is null);
    }

    private static void Nulls(string route, Func<Type, Type> query)
    {
        // The managed wrapper dereferences the receiver before entering CoreCLR.
        Check(route + "-null", Error(() => query(null)) is NullReferenceException);
        Check(route + "-default-handle", Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    internal static void Run(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime element types ==");
        Route("public", Public);
        Nulls("public", Public);
        Check("generic", Generic<int>() == typeof(int) && Generic<string>() == typeof(string));
        Console.WriteLine("Runtime element types end");
    }

    // Only native inputs are rewritten to private CoreLib targets by the fixture.
    internal static void RunArtifact(string[] args)
    {
        Run(args);
        Route("direct", Direct); Nulls("direct", Direct);
        Route("calli", Pointer); Nulls("calli", Pointer);
        Route("getter", Getter); Nulls("getter", Getter);
        Route("getter-calli", GetterPointer); Nulls("getter-calli", GetterPointer);
        Route("virtual-getter", VirtualGetter); Nulls("virtual-getter", VirtualGetter);
        Route("constrained-getter", ConstrainedGetter); Nulls("constrained-getter", ConstrainedGetter);
        Route("address", Address()); Nulls("address", Address());
        Route("virtual-address", VirtualAddress); Nulls("virtual-address", VirtualAddress);
        Route("flow-direct", FlowDirect);
        Route("flow-virtual", FlowVirtual); Nulls("flow-virtual", FlowVirtual);
        Check("virtual-address-bind-null", Error(() => GetterAddress(null)) is NullReferenceException);
        Check("flow-virtual-bind-null", Error(() => FlowGetterAddress(null, true)) is NullReferenceException);
        RunReflectionArtifact(Array.Empty<string>());
        if (args.Length > 0 && args[0] == "native-policy")
        {
            NativeReflectionChecks();
            Check("flow-direct-bind-null", Error(() => FlowGetterAddress(null, false)) is ArgumentException);
            MethodInfo method = Address().Method;
            Check("native-address-row", method.Name == "GetElementType" && method.DeclaringType == typeof(RuntimeTypeHandle)
                && method.ReturnType.FullName == "System.RuntimeType" && method.IsStatic);
            Check("native-address-invoke", ReferenceEquals(method.Invoke(null, new object[] { typeof(int[]) }), typeof(int))
                && method.Invoke(null, new object[] { typeof(int) }) is null);
            NativeGetterDelegateChecks("getter-address", GetterAddress(typeof(int[])));
            NativeGetterDelegateChecks("flow-direct", FlowGetterAddress(typeof(int[]), false));
            NativeGetterDelegateChecks("flow-virtual", FlowGetterAddress(typeof(int[]), true));
            Func<object> covariant = GetterAddress(typeof(int[]));
            Check("native-covariant-object", ReferenceEquals(covariant(), typeof(int)));
            Check("native-covariant-null", ((Func<object>)GetterAddress(typeof(int)))() is null);
        }
        Console.WriteLine("Runtime element routes end");
    }

    private static void NativeGetterDelegateChecks(string route, Func<Type> query)
    {
        MethodInfo method = query.Method;
        Check("native-" + route + "-row", method.Name == "GetElementType"
            && method.DeclaringType == ((object)typeof(int)).GetType()
            && method.DeclaringType.FullName == "System.RuntimeType" && method.ReturnType == typeof(Type) && !method.IsStatic);
        Check("native-" + route + "-invoke", ReferenceEquals(method.Invoke(typeof(int[]), null), typeof(int))
            && method.Invoke(typeof(int), null) is null);
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime element reflection ==");
        Route("reflection", Reflection);
        Nulls("reflection", Reflection);
        if (args.Length > 0 && args[0] == "native-policy") NativeReflectionChecks();
        Console.WriteLine("Runtime element reflection end");
    }

    private static void NativeReflectionChecks()
    {
        MethodInfo method = ReflectionMethod();
        Check("native-reflection-row", method.Name == "GetElementType" && method.DeclaringType == typeof(RuntimeTypeHandle)
            && method.ReturnType.FullName == "System.RuntimeType" && method.IsStatic && !method.IsPublic
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.FullName == "System.RuntimeType");
        Check("native-getter-lookup-unavailable", ((object)typeof(int)).GetType()
            .GetMethod("GetElementType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
    }
}
