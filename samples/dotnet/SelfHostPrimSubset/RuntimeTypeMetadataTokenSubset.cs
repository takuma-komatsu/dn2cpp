using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeMetadataTokenSubset;

internal static unsafe class MetadataTokenChecks
{
    private enum TokenEnum { Value }
    private class TokenOwner<T, U>
    {
        public class Nested<V> { }
        public enum NestedEnum { Value }
        public static void Method<A>() { }
    }
    private class TokenRecursive<T> where T : IComparable<T> { }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("metadata-token-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static int Public(Type type) => type.MetadataToken;
    private static int expectedDefinitionToken;
    private static int expectedEnumToken;

    private static void ConfigureTokens(string[] args)
    {
        if (args.Length == 0 && expectedDefinitionToken != 0 && expectedEnumToken != 0) return;
        if (args.Length != 3) throw new InvalidOperationException("Expected independently read input TypeDef tokens");
        expectedDefinitionToken = int.Parse(args[1], CultureInfo.InvariantCulture);
        expectedEnumToken = int.Parse(args[2], CultureInfo.InvariantCulture);
    }

    private static int Direct(Type type) => Public(type);
    private static int Pointer(Type type) => Public(type);
    private static int Getter(Type type) => Public(type);
    private static int GetterPointer(Type type) => Public(type);
    private static int VirtualGetter(Type type) => Public(type);
    private static int ConstrainedGetter(Type type) => Public(type);
    private static Func<Type, int> Address() => Public;
    private static Func<int> GetterAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => Public(type);
    }
    private static int VirtualAddress(Type type) => GetterAddress(type)();
    private static Func<int> FlowGetterAddress(Type type, bool virtualAddress) =>
        virtualAddress ? GetterAddress(type) : () => Public(type);
    private static int FlowDirect(Type type) => FlowGetterAddress(type, false)();
    private static int FlowVirtual(Type type) => FlowGetterAddress(type, true)();
    private static MethodInfo ReflectionMethod() => typeof(MetadataTokenChecks)
        .GetMethod(nameof(Public), BindingFlags.Static | BindingFlags.NonPublic);
    private static int Reflection(Type type) => (int)ReflectionMethod().Invoke(null, new object[] { type });
    private static int Generic<T>() => typeof(T).MetadataToken;

    private static void Route(string route, Func<Type, int> query)
    {
        Type formal = typeof(TokenOwner<,>).GetGenericArguments()[1];
        Type method = typeof(TokenOwner<,>).GetMethod("Method").GetGenericArguments()[0];
        Type closedMethod = typeof(TokenOwner<int, string>).GetMethod("Method").GetGenericArguments()[0];
        Type recursive = typeof(TokenRecursive<>).GetGenericArguments()[0];
        Type open = typeof(TokenOwner<,>).MakeGenericType(new[] { typeof(int), formal });
        Type function = typeof(delegate*<int, void>);
        Type[] types = { formal, method, closedMethod, recursive,
            typeof(TokenOwner<,>), typeof(TokenOwner<,>.Nested<>), open,
            typeof(TokenOwner<int, string>), formal.MakeArrayType(), formal.MakeArrayType(1),
            formal.MakeArrayType(2), formal.MakePointerType(), formal.MakeByRefType(),
            method.MakeArrayType(), method.MakePointerType(), method.MakeByRefType(),
            open.MakeArrayType(), formal.MakeArrayType().MakePointerType().MakeByRefType(),
            typeof(object), typeof(void), typeof(int), typeof(TokenEnum), typeof(string),
            typeof(int[]), typeof(int*), typeof(int).MakeByRefType(),
            function, typeof(delegate* unmanaged<int, void>), function.MakePointerType(),
            typeof(TokenOwner<int, string>.NestedEnum), typeof(TokenOwner<,>.NestedEnum),
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
        int token = query(typeof(TokenOwner<int, string>));
        Check(route + "-stamped-definition", token == expectedDefinitionToken && (token & 0x00ffffff) != 0
            && (token & unchecked((int)0xff000000)) == 0x02000000
            && token == query(typeof(TokenOwner<,>)) && token == query(open));
        Check(route + "-stamped-enum", query(typeof(TokenEnum)) == expectedEnumToken && (query(typeof(TokenEnum)) & 0x00ffffff) != 0
            && (query(typeof(TokenEnum)) & unchecked((int)0xff000000)) == 0x02000000);
    }

    private static void PublicNulls(string route, Func<Type, int> query)
    {
        Check(route + "-null", Error(() => query(null)) is NullReferenceException);
        Check(route + "-default-handle", Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    private static void NativeNulls(string route, Func<Type, int> query)
    {
        // CoreCLR's raw predicate requires nonnull input; this tests native safety, not CLR parity.
        Check("native-safety-" + route, Error(() => query(null)) is NullReferenceException
            && Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }

    internal static void Run(string[] args)
    {
        ConfigureTokens(args);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime metadata-token predicates ==");
        Route("public", Public);
        PublicNulls("public", Public);
        Check("generic", Generic<TokenOwner<int, string>>() == typeof(TokenOwner<,>).MetadataToken);
        Console.WriteLine("Runtime metadata-token predicates end");
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
            Check("native-address-row", method.Name == "GetToken" && method.DeclaringType == typeof(RuntimeTypeHandle)
                && method.ReturnType == typeof(int) && method.IsStatic);
            Type formal = typeof(TokenOwner<,>).GetGenericArguments()[0];
            Check("native-address-invoke", (int)method.Invoke(null, new object[] { formal }) == Public(formal)
                && (int)method.Invoke(null, new object[] { typeof(int) }) == Public(typeof(int)));
            NativeGetterDelegateChecks("getter-address", GetterAddress(formal));
            NativeGetterDelegateChecks("flow-direct", FlowGetterAddress(formal, false));
            NativeGetterDelegateChecks("flow-virtual", FlowGetterAddress(formal, true));
        }
        Console.WriteLine("Runtime metadata-token routes end");
    }

    private static void NativeGetterDelegateChecks(string route, Func<int> query)
    {
        MethodInfo method = query.Method;
        Check("native-" + route + "-row", method.Name == "get_MetadataToken"
            && method.DeclaringType == ((object)typeof(int)).GetType()
            && method.DeclaringType.FullName == "System.RuntimeType" && method.ReturnType == typeof(int) && !method.IsStatic);
        Type formal = typeof(TokenOwner<,>).GetGenericArguments()[0];
        Check("native-" + route + "-invoke", (int)method.Invoke(formal, null) == Public(formal)
            && (int)method.Invoke(typeof(int), null) == Public(typeof(int)));
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        ConfigureTokens(args);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime metadata-token reflection ==");
        Route("reflection", Reflection);
        if (args.Length > 0 && args[0] == "native-policy")
        {
            NativeReflectionChecks();
            Policy("native-reflection", Reflection, true);
        }
        if (args.Length > 0 && args[0] == "clr-policy") Policy("clr-public", Public, false);
        Console.WriteLine("Runtime metadata-token reflection end");
    }

    private static void Policy(string route, Func<Type, int> query, bool native)
    {
        Type formal = typeof(TokenOwner<,>).GetGenericArguments()[0];
        Type method = typeof(TokenOwner<,>).GetMethod("Method").GetGenericArguments()[0];
        // Native unstamped rows deliberately keep the existing public accessor's zero.
        // CLR assigns GenericParam tokens and a typed-nil TypeDef token to these types.
        Check(route + "-type-formal-policy", native ? query(formal) == 0 : (query(formal) & unchecked((int)0xff000000)) == 0x2a000000);
        Check(route + "-method-formal-policy", native ? query(method) == 0 : (query(method) & unchecked((int)0xff000000)) == 0x2a000000);
        Type[] compounds = { typeof(int[]), typeof(int*), typeof(int).MakeByRefType(),
            typeof(delegate*<int, void>), formal.MakeArrayType(), formal.MakePointerType() };
        for (int i = 0; i < compounds.Length; i++)
            Check(route + "-compound-policy-" + i, query(compounds[i]) == (native ? 0 : 0x02000000));
        Check(route + "-nil-row-guard", (query(typeof(int[])) & 0x00ffffff) == 0);
    }

    private static void NativeReflectionChecks()
    {
        NativeNulls("reflection", Reflection);
        MethodInfo method = ReflectionMethod();
        Check("native-reflection-row", method.Name == "GetToken" && method.DeclaringType == typeof(RuntimeTypeHandle)
            && method.ReturnType == typeof(int) && method.IsStatic && !method.IsPublic
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.FullName == "System.RuntimeType");
        Check("native-getter-lookup-unavailable", ((object)typeof(int)).GetType()
            .GetMethod("get_MetadataToken", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
    }
}
