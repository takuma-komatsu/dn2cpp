using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeAttributesSubset;

internal static unsafe class AttributeChecks
{
    private enum AttributesEnum { Value }
    private class AttributesOwner<T, U>
    {
        public class Nested<V> { }
        public enum NestedEnum { Value }
        public static void Method<A>() { }
    }
    private class AttributesRecursive<T> where T : IComparable<T> { }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("attributes-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static TypeAttributes Public(Type type) => type.Attributes;
    private static TypeAttributes Direct(Type type) => Public(type);
    private static TypeAttributes Pointer(Type type) => Public(type);
    private static TypeAttributes Getter(Type type) => Public(type);
    private static TypeAttributes GetterPointer(Type type) => Public(type);
    private static TypeAttributes VirtualGetter(Type type) => Public(type);
    private static TypeAttributes ConstrainedGetter(Type type) => Public(type);
    private static Func<Type, TypeAttributes> Address() => Public;
    private static Func<TypeAttributes> GetterAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => Public(type);
    }
    private static TypeAttributes VirtualAddress(Type type) => GetterAddress(type)();
    private static Func<TypeAttributes> FlowGetterAddress(Type type, bool virtualAddress) =>
        virtualAddress ? GetterAddress(type) : () => Public(type);
    private static TypeAttributes FlowDirect(Type type) => FlowGetterAddress(type, false)();
    private static TypeAttributes FlowVirtual(Type type) => FlowGetterAddress(type, true)();
    private static MethodInfo ReflectionMethod() => typeof(AttributeChecks)
        .GetMethod(nameof(Public), BindingFlags.Static | BindingFlags.NonPublic);
    private static TypeAttributes Reflection(Type type) => (TypeAttributes)ReflectionMethod().Invoke(null, new object[] { type });
    private static TypeAttributes Generic<T>() => typeof(T).Attributes;

    private static void Route(string route, Func<Type, TypeAttributes> query)
    {
        Type formal = typeof(AttributesOwner<,>).GetGenericArguments()[1];
        Type method = typeof(AttributesOwner<,>).GetMethod("Method").GetGenericArguments()[0];
        Type closedMethod = typeof(AttributesOwner<int, string>).GetMethod("Method").GetGenericArguments()[0];
        Type recursive = typeof(AttributesRecursive<>).GetGenericArguments()[0];
        Type open = typeof(AttributesOwner<,>).MakeGenericType(new[] { typeof(int), formal });
        Type function = typeof(delegate*<int, void>);
        Type[] types = { formal, method, closedMethod, recursive,
            typeof(AttributesOwner<,>), typeof(AttributesOwner<,>.Nested<>), open,
            typeof(AttributesOwner<int, string>), formal.MakeArrayType(), formal.MakeArrayType(1),
            formal.MakeArrayType(2), formal.MakePointerType(), formal.MakeByRefType(),
            method.MakeArrayType(), method.MakePointerType(), method.MakeByRefType(),
            open.MakeArrayType(), formal.MakeArrayType().MakePointerType().MakeByRefType(),
            typeof(object), typeof(void), typeof(int), typeof(AttributesEnum), typeof(string),
            typeof(int[]), typeof(int*), typeof(int).MakeByRefType(),
            function, typeof(delegate* unmanaged<int, void>), function.MakePointerType(),
            typeof(AttributesOwner<int, string>.NestedEnum), typeof(AttributesOwner<,>.NestedEnum),
            typeof(System.Threading.ThreadLocal<int>), typeof(System.Threading.ThreadLocal<string>),
            typeof(System.Collections.Concurrent.BlockingCollection<int>),
            System.Collections.Generic.EqualityComparer<int>.Default.GetType(),
            System.Collections.Generic.EqualityComparer<string>.Default.GetType() };
        string[] labels = { "type-formal", "method-formal", "closed-owner-formal", "recursive-formal",
            "definition", "nested-definition", "open-constructed", "closed-generic", "formal-array", "formal-mdarray-one",
            "formal-mdarray-two", "formal-pointer", "formal-byref", "method-array", "method-pointer", "method-byref",
            "open-array", "nested-compound", "object", "void", "value", "enum", "string", "array", "pointer", "byref",
            "function-managed", "function-unmanaged", "function-wrapper", "nested-enum", "nested-enum-definition",
            "threadlocal-int", "threadlocal-string", "blockingcollection", "int-comparer", "string-comparer" };
        for (int i = 0; i < types.Length; i++)
            Check(route + "-" + labels[i], query(types[i]) == Public(types[i]));
        Check(route + "-handle-type", query(Type.GetTypeFromHandle(formal.TypeHandle)) == Public(formal));
        Check(route + "-handle-method", query(Type.GetTypeFromHandle(method.TypeHandle)) == Public(method));
        Check(route + "-repeated", query(formal) == Public(formal) && query(open) == Public(open)
            && query(closedMethod) == Public(closedMethod) && query(typeof(int)) == Public(typeof(int)));
        Check(route + "-stamped-definition", query(typeof(AttributesOwner<int, string>)) == (TypeAttributes)0x00100003
            && query(typeof(AttributesOwner<,>)) == (TypeAttributes)0x00100003 && query(open) == (TypeAttributes)0x00100003);
        Check(route + "-stamped-enum", query(typeof(AttributesEnum)) == (TypeAttributes)0x00000103);
        Check(route + "-stamped-zero", query(typeof(AttributesZero)) == 0);
        Check(route + "-stamped-public", query(typeof(AttributesPublic)) == (TypeAttributes)0x00100001);
        Check(route + "-stamped-sealed", query(typeof(AttributesSealed)) == (TypeAttributes)0x00100100);
        Check(route + "-stamped-abstract", query(typeof(AttributesAbstract)) == (TypeAttributes)0x00100080);
        Check(route + "-stamped-interface", query(typeof(IAttributesInterface)) == (TypeAttributes)0x001000a0);
        Check(route + "-stamped-layout", query(typeof(AttributesExplicit)) == (TypeAttributes)0x00100110);

    }

    private static void PublicNulls(string route, Func<Type, TypeAttributes> query)
    {
        Check(route + "-null", Error(() => query(null)) is NullReferenceException);
        Check(route + "-default-handle", Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    private static void NativeNulls(string route, Func<Type, TypeAttributes> query)
    {
        // CoreCLR's raw predicate requires nonnull input; this tests native safety, not CLR parity.
        Check("native-safety-" + route, Error(() => query(null)) is NullReferenceException
            && Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    internal static void Run(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime attributes predicates ==");
        Route("public", Public);
        PublicNulls("public", Public);
        Check("generic", Generic<AttributesOwner<int, string>>() == typeof(AttributesOwner<,>).Attributes);
        Console.WriteLine("Runtime attributes predicates end");
    }

    // The fixture rewrites only native inputs; the CLR oracle calls the public property.
    internal static void RunArtifact(string[] args)
    {
        Run(args);
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
            Policy("native-public", Public, true);
            Policy("native-direct", Direct, true);
            Policy("native-reflection", Reflection, true);
            MethodInfo method = Address().Method;
            Check("native-address-row", method.Name == "GetAttributes" && method.DeclaringType == typeof(RuntimeTypeHandle)
                && method.ReturnType == typeof(TypeAttributes) && method.IsStatic);
            Type formal = typeof(AttributesOwner<,>).GetGenericArguments()[0];
            Check("native-address-invoke", (TypeAttributes)method.Invoke(null, new object[] { formal }) == Public(formal)
                && (TypeAttributes)method.Invoke(null, new object[] { typeof(int) }) == Public(typeof(int)));
            NativeGetterDelegateChecks("getter-address", GetterAddress(formal));
            NativeGetterDelegateChecks("flow-direct", FlowGetterAddress(formal, false));
            NativeGetterDelegateChecks("flow-virtual", FlowGetterAddress(formal, true));
        }
        Console.WriteLine("Runtime attributes routes end");
    }

    private static void NativeGetterDelegateChecks(string route, Func<TypeAttributes> query)
    {
        MethodInfo method = query.Method;
        Check("native-" + route + "-row", method.Name == "GetAttributeFlagsImpl"
            && method.DeclaringType == ((object)typeof(int)).GetType()
            && method.DeclaringType.FullName == "System.RuntimeType" && method.ReturnType == typeof(TypeAttributes) && !method.IsStatic);
        Type formal = typeof(AttributesOwner<,>).GetGenericArguments()[0];
        Check("native-" + route + "-invoke", (TypeAttributes)method.Invoke(formal, null) == Public(formal)
            && (TypeAttributes)method.Invoke(typeof(int), null) == Public(typeof(int)));
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime attributes reflection ==");
        Route("reflection", Reflection);
        if (args.Length > 0 && args[0] == "native-policy")
        {
            NativeReflectionChecks();
            Policy("native-reflection", Reflection, true);
        }
        if (args.Length > 0 && args[0] == "clr-policy") Policy("clr-public", Public, false);
        Console.WriteLine("Runtime attributes reflection end");
    }

    private static void Policy(string route, Func<Type, TypeAttributes> query, bool native)
    {
        Type formal = typeof(AttributesOwner<,>).GetGenericArguments()[0];
        Type method = typeof(AttributesOwner<,>).GetMethod("Method").GetGenericArguments()[0];
        // Unstamped native rows use the existing flag synthesis, not all CLR class bits.
        Check(route + "-type-formal-policy", query(formal) == TypeAttributes.Public);
        Check(route + "-method-formal-policy", query(method) == TypeAttributes.Public);
        Type[] signatures = { typeof(int*), typeof(int).MakeByRefType(), typeof(delegate*<int, void>), formal.MakePointerType() };
        for (int i = 0; i < signatures.Length; i++)
            Check(route + "-signature-policy-" + i, query(signatures[i]) == TypeAttributes.Public);
        Check(route + "-array-policy", query(typeof(int[])) == (TypeAttributes)(native ? 0x101 : 0x2101));
        Check(route + "-formal-array-policy", query(formal.MakeArrayType()) == (TypeAttributes)(native ? 0x101 : 0x2101));
        Check(route + "-signature-array-policy", query(typeof(int*).MakeArrayType()) == (TypeAttributes)0x2101);

    }

    private static void NativeReflectionChecks()
    {
        NativeNulls("reflection", Reflection);
        MethodInfo method = ReflectionMethod();
        Check("native-reflection-row", method.Name == "GetAttributes" && method.DeclaringType == typeof(RuntimeTypeHandle)
            && method.ReturnType == typeof(TypeAttributes) && method.IsStatic && !method.IsPublic
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.FullName == "System.RuntimeType");
        Check("native-getter-lookup-unavailable", ((object)typeof(int)).GetType()
            .GetMethod("GetAttributeFlagsImpl", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
    }
}

internal class AttributesZero { static AttributesZero() { } }
public class AttributesPublic { }
internal sealed class AttributesSealed { }
internal abstract class AttributesAbstract { }
internal interface IAttributesInterface { }
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
internal struct AttributesExplicit
{
    [System.Runtime.InteropServices.FieldOffset(0)] public int Value;
}
