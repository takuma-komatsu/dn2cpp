using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RuntimeTypeIsPrimitiveSubset;

internal static unsafe class IsPrimitiveChecks
{
    private enum IsPrimitiveEnum : byte { Value }
    private enum WideEnum : ulong { Value = ulong.MaxValue }
    private class IsPrimitiveOwner<T, U>
    {
        public class Nested<V> { }
        public static void Method<A>() { }
    }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("is-primitive-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    // Runtime Type input avoids literal typeof(void).IsPrimitive JIT folding.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Public(Type type) => type.IsPrimitive;
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
    private static MethodInfo ReflectionMethod() => typeof(IsPrimitiveChecks)
        .GetMethod(nameof(Public), BindingFlags.Static | BindingFlags.NonPublic);
    private static bool Reflection(Type type) => (bool)ReflectionMethod().Invoke(null, new object[] { type });
    private static bool Generic<T>() => Public(typeof(T));

    private static void Route(string route, Func<Type, bool> query)
    {
        Type formal = typeof(IsPrimitiveOwner<,>).GetGenericArguments()[1];
        Type method = typeof(IsPrimitiveOwner<,>).GetMethod("Method").GetGenericArguments()[0];
        Type open = typeof(IsPrimitiveOwner<,>).MakeGenericType(new[] { typeof(int), formal });
        Type function = typeof(delegate*<int, void>);
        Type rankOne = Array.CreateInstance(typeof(int), new[] { 1 }, new[] { 1 }).GetType();
        Check(route + "-rank-one-shape", !rankOne.IsSZArray && rankOne.GetArrayRank() == 1
            && rankOne.GetElementType() == typeof(int) && rankOne != typeof(int[]));
        Type[] plain = { typeof(object), typeof(void), typeof(decimal), typeof(string), typeof(IsPrimitiveEnum), typeof(WideEnum), typeof(TypedReference), typeof(int?), typeof(Array),
            typeof(IsPrimitiveOwner<,>), typeof(IsPrimitiveOwner<,>.Nested<>), typeof(IsPrimitiveOwner<int, string>), open,
            formal, method, function, typeof(delegate* unmanaged<int, void>), typeof(Span<int>), typeof(Span<>),
            typeof(delegate*<ref int, ref int>) };
        string[] plainLabels = { "object", "void", "decimal", "string", "enum", "wide-enum", "typed-reference", "nullable", "array-base", "definition", "nested-definition",
            "closed-generic", "open-constructed", "type-formal", "method-formal", "function-managed", "function-unmanaged", "byref-like", "byref-like-definition", "function-byref-signature" };
        for (int i = 0; i < plain.Length; i++) Check(route + "-" + plainLabels[i], !query(plain[i]));
        Type[] compounds = { typeof(int[]), typeof(string[]), typeof(int[][]), rankOne, typeof(int[,]),
            typeof(int*), typeof(void*), typeof(int).MakeByRefType(),
            formal.MakeArrayType(), formal.MakeArrayType(1), formal.MakeArrayType(2), formal.MakePointerType(), formal.MakeByRefType(),
            method.MakeArrayType(), method.MakePointerType(), method.MakeByRefType(), open.MakeArrayType(),
            formal.MakeArrayType().MakePointerType().MakeByRefType(), function.MakePointerType(), function.MakeByRefType(), function.MakeArrayType(),
            typeof(int[]).MakeByRefType(), typeof(int[,]).MakeByRefType(), typeof(int*).MakeByRefType(),
            typeof(Span<int>).MakeByRefType(), typeof(string).MakeByRefType(),
            formal.MakeArrayType().MakePointerType(), typeof(delegate* unmanaged<int, void>).MakeByRefType() };
        string[] compoundLabels = { "array", "reference-array", "jagged-array", "mdarray-one", "mdarray-two", "pointer", "void-pointer", "byref",
            "formal-array", "formal-mdarray-one", "formal-mdarray-two", "formal-pointer", "formal-byref",
            "method-array", "method-pointer", "method-byref", "open-array", "nested-compound", "function-pointer", "function-byref", "function-array", "array-byref", "mdarray-byref", "pointer-byref", "byref-like-byref", "reference-byref",
            "array-pointer", "unmanaged-function-byref" };
        for (int i = 0; i < compounds.Length; i++) Check(route + "-" + compoundLabels[i], !query(compounds[i]));
        Type[] primitives = { typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(IntPtr), typeof(UIntPtr) };
        string[] primitiveLabels = { "boolean", "char", "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "intptr", "uintptr" };
        for (int i = 0; i < primitives.Length; i++) Check(route + "-" + primitiveLabels[i], query(primitives[i]));
        Check(route + "-category-relations", !query(rankOne) && !query(rankOne.MakeByRefType()) && !rankOne.IsSZArray
            && !query(typeof(int).MakePointerType()) && typeof(int).MakePointerType().HasElementType);
        Check(route + "-handle-type", query(Type.GetTypeFromHandle(typeof(IntPtr).TypeHandle)));
        Check(route + "-handle-formal", !query(Type.GetTypeFromHandle(formal.TypeHandle)));
        Check(route + "-handle-array", !query(Type.GetTypeFromHandle(typeof(int[]).TypeHandle)));
        Check(route + "-handle-byref", !query(Type.GetTypeFromHandle(typeof(int).MakeByRefType().TypeHandle)));
        Check(route + "-repeated", !query(typeof(int[]).MakeByRefType()) && !query(typeof(int[])) && !query(function)
            && !query(function.MakePointerType()) && query(typeof(int)) && !query(typeof(void)));

    }

    private static void PublicNulls(string route, Func<Type, bool> query)
    {
        Check(route + "-null", Error(() => query(null)) is NullReferenceException);
        Check(route + "-default-handle", Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    private static void NativeNulls(string route, Func<Type, bool> query)
    {
        // The managed helper's first callvirt rejects null before native classification.
        Check("native-safety-" + route, Error(() => query(null)) is NullReferenceException
            && Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime is-primitive predicates ==");
        Route("public", Public);
        PublicNulls("public", Public);
        Type formal = typeof(IsPrimitiveOwner<,>).GetGenericArguments()[0];
        Type open = typeof(IsPrimitiveOwner<,>).MakeGenericType(new[] { typeof(int), formal });
        Check("top-level", !Public(open) && open.ContainsGenericParameters
            && !Public(formal.MakeArrayType()) && !Public(formal.MakeArrayType().MakeByRefType())
            && formal.MakeArrayType().ContainsGenericParameters);
        Check("generic", Generic<int>() && !Generic<string>() && !Generic<IsPrimitiveOwner<int, string>>() && !Generic<int[]>());
        Console.WriteLine("Runtime is-primitive predicates end");
    }

    // The fixture rewrites only native inputs; the CLR oracle calls the public property.
    internal static void RunArtifact(string[] args)
    {
        Run();
        Route("direct", Direct);
        PublicNulls("direct", Direct);
        Route("calli", Pointer);
        PublicNulls("calli", Pointer);
        Route("getter", Getter);
        PublicNulls("getter", Getter);
        Route("getter-calli", GetterPointer);
        PublicNulls("getter-calli", GetterPointer);
        Route("virtual-getter", VirtualGetter);
        PublicNulls("virtual-getter", VirtualGetter);
        Route("constrained-getter", ConstrainedGetter);
        PublicNulls("constrained-getter", ConstrainedGetter);
        Route("address", Address());
        PublicNulls("address", Address());
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
            Check("flow-direct-bind-null", Error(() => FlowGetterAddress(null, false)) is ArgumentException);
            NativeNulls("direct", Direct);
            NativeNulls("calli", Pointer);
            NativeNulls("getter", Getter);
            NativeNulls("address", Address());
            NativeReflectionChecks();
            MethodInfo method = Address().Method;
            Check("native-address-row", method.Name == "IsPrimitive" && method.DeclaringType == typeof(RuntimeTypeHandle)
                && method.ReturnType == typeof(bool) && method.IsStatic);
            Type formal = typeof(IntPtr);
            Check("native-address-invoke", (bool)method.Invoke(null, new object[] { formal })
                && !(bool)method.Invoke(null, new object[] { typeof(void) }));
            NativeGetterDelegateChecks("getter-address", GetterAddress(formal));
            NativeGetterDelegateChecks("flow-direct", FlowGetterAddress(formal, false));
            NativeGetterDelegateChecks("flow-virtual", FlowGetterAddress(formal, true));
        }
        Console.WriteLine("Runtime is-primitive routes end");
    }

    private static void NativeGetterDelegateChecks(string route, Func<bool> query)
    {
        MethodInfo method = query.Method;
        Check("native-" + route + "-row", method.Name == "IsPrimitiveImpl"
            && method.DeclaringType == ((object)typeof(int)).GetType()
            && method.DeclaringType.FullName == "System.RuntimeType" && method.ReturnType == typeof(bool) && !method.IsStatic);
        Type formal = typeof(IntPtr);
        Check("native-" + route + "-invoke", (bool)method.Invoke(formal, null)
            && !(bool)method.Invoke(typeof(void), null));
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime is-primitive reflection ==");
        Route("reflection", Reflection);
        PublicNulls("reflection", Reflection);
        if (args.Length > 0 && args[0] == "native-policy") NativeReflectionChecks();
        Console.WriteLine("Runtime is-primitive reflection end");
    }

    private static void NativeReflectionChecks()
    {
        NativeNulls("reflection", Reflection);
        MethodInfo method = ReflectionMethod();
        Check("native-reflection-row", method.Name == "IsPrimitive" && method.DeclaringType == typeof(RuntimeTypeHandle)
            && method.ReturnType == typeof(bool) && method.IsStatic && !method.IsPublic
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.FullName == "System.RuntimeType");
        Check("native-getter-lookup-unavailable", ((object)typeof(int)).GetType()
            .GetMethod("IsPrimitiveImpl", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
    }
}
