using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeGenericVariableSubset;

internal static unsafe class GenericVariablePredicateChecks
{
    private enum GenericVariableEnum { Value }
    private class GenericVariableOwner<T, U>
    {
        public class Nested<V> { }
        public static void Method<A>() { }
    }
    private class RecursiveVariable<T> where T : IComparable<T> { }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("generic-variable-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool Public(Type type) => type.IsGenericParameter;
    private static bool Direct(Type type) => Public(type);
    private static bool Pointer(Type type) => Public(type);
    private static bool Getter(Type type) => Public(type);
    private static bool GetterPointer(Type type) => Public(type);
    private static bool VirtualGetter(Type type) => Public(type);
    private static bool ConstrainedGetter(Type type) => Public(type);
    private static Func<Type, bool> Address() => Public;
    private static Func<bool> GetterAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => Public(type);
    }
    private static bool VirtualAddress(Type type) => GetterAddress(type)();
    private static Func<bool> FlowGetterAddress(Type type, bool virtualAddress) =>
        virtualAddress ? GetterAddress(type) : () => Public(type);
    private static bool FlowDirect(Type type) => FlowGetterAddress(type, false)();
    private static bool FlowVirtual(Type type) => FlowGetterAddress(type, true)();
    private static MethodInfo ReflectionMethod() => typeof(GenericVariablePredicateChecks)
        .GetMethod(nameof(Public), BindingFlags.Static | BindingFlags.NonPublic);
    private static bool Reflection(Type type) => (bool)ReflectionMethod().Invoke(null, new object[] { type });
    private static bool Generic<T>() => typeof(T).IsGenericParameter;

    private static void Route(string route, Func<Type, bool> query)
    {
        Type formal = typeof(GenericVariableOwner<,>).GetGenericArguments()[1];
        Type method = typeof(GenericVariableOwner<,>).GetMethod("Method").GetGenericArguments()[0];
        Type closedMethod = typeof(GenericVariableOwner<int, string>).GetMethod("Method").GetGenericArguments()[0];
        Type recursive = typeof(RecursiveVariable<>).GetGenericArguments()[0];
        Type open = typeof(GenericVariableOwner<,>).MakeGenericType(new[] { typeof(int), formal });
        Type function = typeof(delegate*<int, void>);
        Type[] types = { formal, method, closedMethod, recursive,
            typeof(GenericVariableOwner<,>), typeof(GenericVariableOwner<,>.Nested<>), open,
            typeof(GenericVariableOwner<int, string>), formal.MakeArrayType(), formal.MakeArrayType(1),
            formal.MakeArrayType(2), formal.MakePointerType(), formal.MakeByRefType(),
            method.MakeArrayType(), method.MakePointerType(), method.MakeByRefType(),
            open.MakeArrayType(), formal.MakeArrayType().MakePointerType().MakeByRefType(),
            typeof(object), typeof(void), typeof(int), typeof(GenericVariableEnum), typeof(string),
            typeof(int[]), typeof(int*), typeof(int).MakeByRefType(),
            function, typeof(delegate* unmanaged<int, void>), function.MakePointerType() };
        string[] labels = { "type-formal", "method-formal", "closed-owner-formal", "recursive-formal",
            "definition", "nested-definition", "open-constructed", "closed-generic", "formal-array", "formal-mdarray-one",
            "formal-mdarray-two", "formal-pointer", "formal-byref", "method-array", "method-pointer", "method-byref",
            "open-array", "nested-compound", "object", "void", "value", "enum", "string", "array", "pointer", "byref",
            "function-managed", "function-unmanaged", "function-wrapper" };
        for (int i = 0; i < types.Length; i++)
            Check(route + "-" + labels[i], query(types[i]) == (i < 4));
        Check(route + "-handle-type", query(Type.GetTypeFromHandle(formal.TypeHandle)));
        Check(route + "-handle-method", query(Type.GetTypeFromHandle(method.TypeHandle)));
        Check(route + "-repeated", query(formal) && !query(open) && query(closedMethod) && !query(typeof(int)));
    }

    private static void PublicNulls(string route, Func<Type, bool> query)
    {
        Check(route + "-null", Error(() => query(null)) is NullReferenceException);
        Check(route + "-default-handle", Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    private static void NativeNulls(string route, Func<Type, bool> query)
    {
        // CoreCLR's raw predicate requires nonnull input; this tests native safety, not CLR parity.
        Check("native-safety-" + route, Error(() => query(null)) is NullReferenceException
            && Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime generic-variable predicates ==");
        Route("public", Public);
        PublicNulls("public", Public);
        Type formal = typeof(GenericVariableOwner<,>).GetGenericArguments()[0];
        Type open = typeof(GenericVariableOwner<,>).MakeGenericType(new[] { typeof(int), formal });
        Check("top-level", !Public(open) && open.ContainsGenericParameters
            && !Public(formal.MakeArrayType()) && formal.MakeArrayType().ContainsGenericParameters);
        Check("generic", !Generic<int>() && !Generic<string>() && !Generic<GenericVariableOwner<int, string>>());
        Console.WriteLine("Runtime generic-variable predicates end");
    }

    // The fixture rewrites only native inputs; the CLR oracle calls the public property.
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
        Route("flow-direct", FlowDirect);
        Route("flow-virtual", FlowVirtual);
        PublicNulls("flow-virtual", FlowVirtual);
        Check("flow-virtual-bind-null", Error(() => FlowGetterAddress(null, true)) is NullReferenceException);
        RunReflectionArtifact(Array.Empty<string>());
        if (args.Length > 0 && args[0] == "native-policy")
        {
            NativeNulls("direct", Direct);
            NativeNulls("calli", Pointer);
            NativeNulls("getter", Getter);
            NativeNulls("address", Address());
            NativeReflectionChecks();
            MethodInfo method = Address().Method;
            Check("native-address-row", method.Name == "IsGenericVariable" && method.DeclaringType == typeof(RuntimeTypeHandle)
                && method.ReturnType == typeof(bool) && method.IsStatic);
            Type formal = typeof(GenericVariableOwner<,>).GetGenericArguments()[0];
            Check("native-address-invoke", (bool)method.Invoke(null, new object[] { formal })
                && !(bool)method.Invoke(null, new object[] { typeof(int) }));
            NativeGetterDelegateChecks("getter-address", GetterAddress(formal));
            NativeGetterDelegateChecks("flow-direct", FlowGetterAddress(formal, false));
            NativeGetterDelegateChecks("flow-virtual", FlowGetterAddress(formal, true));
        }
        Console.WriteLine("Runtime generic-variable routes end");
    }

    private static void NativeGetterDelegateChecks(string route, Func<bool> query)
    {
        MethodInfo method = query.Method;
        Check("native-" + route + "-row", method.Name == "get_IsGenericParameter"
            && method.DeclaringType == ((object)typeof(int)).GetType()
            && method.DeclaringType.FullName == "System.RuntimeType" && method.ReturnType == typeof(bool) && !method.IsStatic);
        Type formal = typeof(GenericVariableOwner<,>).GetGenericArguments()[0];
        Check("native-" + route + "-invoke", (bool)method.Invoke(formal, null)
            && !(bool)method.Invoke(typeof(int), null));
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime generic-variable reflection ==");
        Route("reflection", Reflection);
        if (args.Length > 0 && args[0] == "native-policy") NativeReflectionChecks();
        Console.WriteLine("Runtime generic-variable reflection end");
    }

    private static void NativeReflectionChecks()
    {
        NativeNulls("reflection", Reflection);
        MethodInfo method = ReflectionMethod();
        Check("native-reflection-row", method.Name == "IsGenericVariable" && method.DeclaringType == typeof(RuntimeTypeHandle)
            && method.ReturnType == typeof(bool) && method.IsStatic && !method.IsPublic
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.FullName == "System.RuntimeType");
        Check("native-getter-lookup-unavailable", ((object)typeof(int)).GetType()
            .GetMethod("get_IsGenericParameter", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
    }
}
