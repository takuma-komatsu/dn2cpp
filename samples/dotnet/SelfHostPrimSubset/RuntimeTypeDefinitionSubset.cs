using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace RuntimeTypeDefinitionSubset;

internal enum DefinitionEnum { Value }

internal static unsafe class TypeDefinitionChecks
{
    private enum NestedDefinitionEnum { Value }
    private class DefinitionEnumOwner<T>
    {
        public enum Kind : byte { Value = 3 }
    }
    private class DefinitionOwner<T, U>
    {
        public class Nested<V> { }
        public static void Method<A>() { }
    }
    private class RecursiveDefinition<T> where T : IComparable<T> { }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("type-definition-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool Public(Type type) => type.IsTypeDefinition;
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
    private static Func<bool> FlowGetterAddress(Type type, bool virtualAddress)
    {
        if (virtualAddress) return GetterAddress(type);
        DefinitionReceiver receiver = type is null ? null : new DefinitionReceiver(type);
        return receiver.Query;
    }
    private sealed class DefinitionReceiver
    {
        private readonly Type _type;
        internal DefinitionReceiver(Type type) => _type = type;
        internal bool Query() => Public(_type);
    }

    private static bool FlowDirect(Type type) => FlowGetterAddress(type, false)();
    private static bool FlowVirtual(Type type) => FlowGetterAddress(type, true)();
    private static MethodInfo ReflectionMethod() => typeof(TypeDefinitionChecks)
        .GetMethod(nameof(Public), BindingFlags.Static | BindingFlags.NonPublic);
    private static bool Reflection(Type type) => (bool)ReflectionMethod().Invoke(null, new object[] { type });
    private static bool Generic<T>() => Public(typeof(T));

    private static void Route(string route, Func<Type, bool> query)
    {
        Type formal = typeof(DefinitionOwner<,>).GetGenericArguments()[1];
        Type method = typeof(DefinitionOwner<,>).GetMethod("Method").GetGenericArguments()[0];
        Type closedMethod = typeof(DefinitionOwner<int, string>).GetMethod("Method").GetGenericArguments()[0];
        Type recursive = typeof(RecursiveDefinition<>).GetGenericArguments()[0];
        Type open = typeof(DefinitionOwner<,>).MakeGenericType(new[] { typeof(int), formal });
        Type function = typeof(delegate*<int, void>);
        Type[] definitions = { typeof(object), typeof(void), typeof(bool), typeof(char), typeof(sbyte), typeof(byte),
            typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
            typeof(nint), typeof(nuint), typeof(string), typeof(decimal), typeof(TypeDefinitionChecks),
            typeof(IDisposable), typeof(Array), typeof(TypedReference), typeof(DefinitionOwner<,>),
            typeof(DefinitionOwner<,>.Nested<>), typeof(Nullable<>), typeof(DefinitionEnumOwner<>.Kind),
            typeof(ThreadLocal<>), typeof(BlockingCollection<>) };
        string[] definitionLabels = { "object", "void", "bool", "char", "sbyte", "byte", "short", "ushort", "int", "uint",
            "long", "ulong", "float", "double", "nint", "nuint", "string", "decimal", "class", "interface", "array-base",
            "typed-reference", "generic-definition", "nested-definition", "nullable-definition", "enum-definition",
            "threadlocal-definition", "blockingcollection-definition" };
        for (int i = 0; i < definitions.Length; i++)
            Check(route + "-" + definitionLabels[i], query(definitions[i]));
        Type nonSz = Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 1 }).GetType();
        Type[] compounds = { formal, method, closedMethod, recursive, open, typeof(DefinitionOwner<int, string>), typeof(int?),
            typeof(DefinitionOwner<int, string>.Nested<long>), formal.MakeArrayType(), formal.MakeArrayType(1),
            formal.MakeArrayType(2), formal.MakePointerType(), formal.MakeByRefType(), method.MakeArrayType(),
            method.MakePointerType(), method.MakeByRefType(), open.MakeArrayType(),
            formal.MakeArrayType().MakePointerType().MakeByRefType(), typeof(int[]), nonSz,
            typeof(int[,]), typeof(int*), typeof(int).MakeByRefType(), function,
            typeof(delegate* unmanaged<int, void>), function.MakePointerType(),
            typeof(ThreadLocal<int>), typeof(ThreadLocal<string>), typeof(BlockingCollection<int>), typeof(BlockingCollection<string>),
            new ThreadLocal<int>().GetType(), new BlockingCollection<string>().GetType() };
        string[] compoundLabels = { "type-formal", "method-formal", "closed-owner-formal", "recursive-formal", "open-constructed",
            "closed-generic", "nullable-closed", "nested-closed", "formal-array", "formal-mdarray-one", "formal-mdarray-two",
            "formal-pointer", "formal-byref", "method-array", "method-pointer", "method-byref", "open-array", "nested-compound",
            "array", "mdarray-one", "mdarray-two", "pointer", "byref", "function-managed", "function-unmanaged", "function-wrapper",
            "threadlocal-int", "threadlocal-string", "blockingcollection-int", "blockingcollection-string",
            "threadlocal-object", "blockingcollection-object" };
        for (int i = 0; i < compounds.Length; i++)
            Check(route + "-" + compoundLabels[i], !query(compounds[i]));
        Check(route + "-mdarray-one-shape", nonSz.IsArray && !nonSz.IsSZArray && nonSz.GetArrayRank() == 1
            && nonSz.GetElementType() == typeof(int) && nonSz != typeof(int[]));
        Check(route + "-handle-definition", query(Type.GetTypeFromHandle(typeof(DefinitionOwner<,>).TypeHandle)));
        Check(route + "-handle-formal", !query(Type.GetTypeFromHandle(formal.TypeHandle)));
        Check(route + "-repeated", query(typeof(int)) && !query(open) && !query(closedMethod) && query(typeof(void)));
        Exception nullError = Error(() => query(null));
        Exception handleError = Error(() => query(Type.GetTypeFromHandle(default)));
        Check(route + "-null", route == "flow-direct" ? nullError is ArgumentException : nullError is NullReferenceException);
        Check(route + "-default-handle", route == "flow-direct" ? handleError is ArgumentException : handleError is NullReferenceException);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type-definition predicates ==");
        Route("public", Public);
        Type formal = typeof(DefinitionOwner<,>).GetGenericArguments()[0];
        Type open = typeof(DefinitionOwner<,>).MakeGenericType(new[] { typeof(int), formal });
        Check("top-level", Public(typeof(DefinitionOwner<,>)) && !Public(open) && open.ContainsGenericParameters
            && !Public(formal) && !Public(formal.MakeArrayType()) && !formal.MakeArrayType().IsConstructedGenericType);
        Check("generic", Generic<int>() && Generic<string>() && !Generic<int[]>() && !Generic<DefinitionOwner<int, string>>());
        Console.WriteLine("Runtime type-definition predicates end");
    }

    // The fixture selects these entries; only native copies replace the public property calls.
    internal static void RunArtifact(string[] args)
    {
        Run();
        Route("direct", Direct);
        Route("calli", Pointer);
        Route("getter", Getter);
        Route("getter-calli", GetterPointer);
        Route("virtual-getter", VirtualGetter);
        Route("constrained-getter", ConstrainedGetter);
        Route("address", Address());
        Route("virtual-address", VirtualAddress);
        Check("virtual-address-bind-null", Error(() => GetterAddress(null)) is NullReferenceException);
        Route("flow-direct", FlowDirect);
        Route("flow-virtual", FlowVirtual);
        Check("flow-virtual-bind-null", Error(() => FlowGetterAddress(null, true)) is NullReferenceException);
        Check("flow-direct-bind-null", Error(() => FlowGetterAddress(null, false)) is ArgumentException);
        RunReflectionArtifact(Array.Empty<string>());
        if (args.Length > 0 && args[0] == "native-policy")
        {
            NativeReflectionChecks();
            NativeIdentityChecks("reflection", Reflection);
            string[] routes = { "public", "direct", "calli", "getter", "getter-calli", "virtual-getter", "constrained-getter",
                "address", "virtual-address", "flow-direct", "flow-virtual" };
            Func<Type, bool>[] queries = { Public, Direct, Pointer, Getter, GetterPointer, VirtualGetter, ConstrainedGetter,
                Address(), VirtualAddress, FlowDirect, FlowVirtual };
            for (int i = 0; i < routes.Length; i++) NativeIdentityChecks(routes[i], queries[i]);
            MethodInfo method = Address().Method;
            Check("native-address-row", method.Name == "IsTypeDefinition" && method.DeclaringType == typeof(RuntimeTypeHandle)
                && method.ReturnType == typeof(bool) && method.IsStatic);
            Check("native-address-invoke", (bool)method.Invoke(null, new object[] { typeof(int) })
                && !(bool)method.Invoke(null, new object[] { typeof(int[]) }));
            Type formal = typeof(DefinitionOwner<,>).GetGenericArguments()[0];
            NativeGetterDelegateChecks("getter-address", GetterAddress(formal));
            NativeGetterDelegateChecks("flow-direct", FlowGetterAddress(formal, false));
            NativeGetterDelegateChecks("flow-virtual", FlowGetterAddress(formal, true));
        }
        Console.WriteLine("Runtime type-definition routes end");
    }

    private static void NativeGetterDelegateChecks(string route, Func<bool> query)
    {
        MethodInfo method = query.Method;
        Check("native-" + route + "-row", method.Name == "get_IsTypeDefinition"
            && method.DeclaringType == ((object)typeof(int)).GetType()
            && method.DeclaringType.FullName == "System.RuntimeType" && method.ReturnType == typeof(bool) && !method.IsStatic);
        Type formal = typeof(DefinitionOwner<,>).GetGenericArguments()[0];
        Check("native-" + route + "-invoke", !(bool)method.Invoke(formal, null)
            && (bool)method.Invoke(typeof(int), null));
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type-definition reflection ==");
        Route("reflection", Reflection);
        if (args.Length > 0 && args[0] == "native-policy")
        {
            NativeReflectionChecks();
            NativeIdentityChecks("reflection", Reflection);
        }
        Console.WriteLine("Runtime type-definition reflection end");
    }

    private static void NativeReflectionChecks()
    {
        MethodInfo method = ReflectionMethod();
        Check("native-reflection-row", method.Name == "IsTypeDefinition" && method.DeclaringType == typeof(RuntimeTypeHandle)
            && method.ReturnType == typeof(bool) && method.IsStatic && !method.IsPublic
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.FullName == "System.RuntimeType");
        Check("native-getter-lookup-unavailable", ((object)typeof(int)).GetType()
            .GetMethod("get_IsTypeDefinition", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
    }

    private static Type[] UnmodeledIdentities() => new[] {
        typeof(DefinitionEnum), typeof(NestedDefinitionEnum), typeof(DefinitionEnumOwner<int>.Kind),
        typeof(DefinitionEnumOwner<int[]>.Kind), typeof(DefinitionEnumOwner<DefinitionEnumOwner<int[]>.Kind>.Kind),
        EqualityComparer<int>.Default.GetType(), EqualityComparer<string>.Default.GetType(),
    };

    private static readonly string[] UnmodeledLabels = {
        "enum", "nested-enum", "closed-enum", "array-argument-enum", "enum-argument-enum", "int-comparer", "string-comparer",
    };

    // These runtime identities lack enough generic provenance for this new query.
    // Refusal is deliberate; do not turn missing metadata into a guessed Boolean.
    private static void NativeIdentityChecks(string route, Func<Type, bool> query)
    {
        Type[] identities = UnmodeledIdentities();
        for (int i = 0; i < identities.Length; i++)
        {
            Type type = identities[i];
            Check("native-" + route + "-unsupported-" + UnmodeledLabels[i],
                Error(() => query(type)) is PlatformNotSupportedException);
        }
    }

    private sealed class FalseDefinition : TypeDelegator
    {
        public FalseDefinition() : base(typeof(int)) { }
        public override bool IsTypeDefinition => false;
    }

    private sealed class TrueDefinition : TypeDelegator
    {
        public TrueDefinition() : base(typeof(int[])) { }
        public override bool IsTypeDefinition => true;
    }

    // CLR-only evidence: the private RuntimeType bridge must never replace arbitrary Type overrides.
    internal static void RunPublicScope()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Check("public-scope-delegator", Public(new TypeDelegator(typeof(int))));
        Check("public-scope-custom-false", !Public(new FalseDefinition()));
        Check("public-scope-custom-true", Public(new TrueDefinition()));
        Type[] identities = UnmodeledIdentities();
        bool[] definitions = { true, true, false, false, false, false, true };
        for (int i = 0; i < identities.Length; i++)
            Check("public-scope-" + UnmodeledLabels[i], Public(identities[i]) == definitions[i]);
    }
}
