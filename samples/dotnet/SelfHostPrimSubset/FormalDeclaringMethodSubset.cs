using System;
using System.Globalization;
using System.Reflection;

namespace FormalDeclaringMethodSubset;

internal static class Program
{
    private class FormalMethodOwner<T>
    {
        public static U Pick<U, V>(U value) => value;
        public static U Other<U>(U value) => value;
    }
    private class FormalMethodChild<T> : FormalMethodOwner<T> { }
    private const string NotParameter = "Method may only be called on a Type for which Type.IsGenericParameter is true.";
    private const string Boundary = "System.RuntimeTypeHandle.GetDeclaringMethodForGenericParameter requires CoreCLR method descriptors. Use Type.DeclaringMethod instead.";

    private static void Check(string label, bool success)
    {
        Console.WriteLine("formal-declaring-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (Exception error) { return error; }
        return null;
    }

    private static MethodInfo Getter() => ((object)typeof(int)).GetType().GetMethod("get_DeclaringMethod",
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    private static Type Formal() => typeof(FormalMethodOwner<int>).GetMethod("Pick").GetGenericArguments()[0];
    private static MethodBase Typical() => typeof(FormalMethodOwner<>).GetMethod("Pick");

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Formal declaring method contract ==");
        _ = FormalMethodOwner<int>.Pick<int, string>(3);
        Type formal = Formal();
        MethodBase method = formal.DeclaringMethod;
        Check("public-typical", ReferenceEquals(method, Typical())
            && ReferenceEquals(method.DeclaringType, typeof(FormalMethodOwner<>))
            && ReferenceEquals(method.ReflectedType, typeof(FormalMethodOwner<>)) && method.IsGenericMethodDefinition);
        Check("public-contexts", ReferenceEquals(method, typeof(FormalMethodOwner<string>).GetMethod("Pick").GetGenericArguments()[0].DeclaringMethod)
            && ReferenceEquals(method, typeof(FormalMethodChild<int>).GetMethod("Pick", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetGenericArguments()[0].DeclaringMethod));
        Check("public-positions", ReferenceEquals(method, typeof(FormalMethodOwner<int>).GetMethod("Pick").GetGenericArguments()[1].DeclaringMethod)
            && !ReferenceEquals(method, typeof(FormalMethodOwner<int>).GetMethod("Other").GetGenericArguments()[0].DeclaringMethod));
        Check("public-type-formal", typeof(FormalMethodOwner<>).GetGenericArguments()[0].DeclaringMethod is null);
        Check("public-nonformal", Error(() => _ = typeof(int).DeclaringMethod) is InvalidOperationException);
        Check("public-null", Error(() => _ = ((Type)null).DeclaringMethod) is NullReferenceException);
        Check("public-repeated", ReferenceEquals(method, formal.DeclaringMethod));
        Console.WriteLine("Formal declaring method contract end");
    }

    // CLR uses the safe public getter; the artifact imports its exact RuntimeType declaration.
    private static MethodBase Direct(Type type)
    {
        if (type is null) return type.DeclaringMethod;
        try { return (MethodBase)Getter().Invoke(type, null); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    private static MethodBase Pointer(Type type) => Direct(type);
    private static Func<MethodBase> Address(Type type) => (Func<MethodBase>)Getter().CreateDelegate(typeof(Func<MethodBase>), type);
    private static Func<MethodBase> VirtualAddress(Type type) => Address(type);
    private static Func<MethodBase> FlowingVirtualAddress(Type type) => Address(type);

    private static void PublicRoute(string route, Func<Type, MethodBase> query)
    {
        Type formal = Formal();
        Check(route + "-typical", ReferenceEquals(query(formal), Typical()));
        Check(route + "-type-formal", query(typeof(FormalMethodOwner<>).GetGenericArguments()[0]) is null);
        Type[] types = { typeof(int), typeof(FormalMethodOwner<>), typeof(FormalMethodOwner<int>), formal.MakeArrayType(), formal.MakePointerType(), formal.MakeByRefType() };
        string[] names = { "ordinary", "open", "closed", "array", "pointer", "byref" };
        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];
            Exception error = Error(() => query(type));
            Check(route + "-" + names[i], error is InvalidOperationException && error.Message == NotParameter);
        }
        Check(route + "-repeated", ReferenceEquals(query(formal), query(formal)));
    }

    internal static void RunPublicArtifact(string[] args)
    {
        Run();
        PublicRoute("direct", Direct);
        PublicRoute("pointer", Pointer);
        PublicRoute("address", type => Address(type)());
        PublicRoute("virtual-address", type => VirtualAddress(type)());
        PublicRoute("flowing-virtual-address", type => FlowingVirtualAddress(type)());
        Check("direct-null", Error(() => Direct(null)) is NullReferenceException);
        Check("pointer-null", Error(() => Pointer(null)) is NullReferenceException);
        RunDelegateReflection();
        // ldvirtftn checks its receiver before a delegate can be constructed.
        if (args.Length > 0 && args[0] == "native-boundary")
        {
            Check("address-null", Error(() => Address(null)) is ArgumentException);
            Check("virtual-address-null", Error(() => VirtualAddress(null)) is NullReferenceException);
            Check("flowing-virtual-address-null", Error(() => FlowingVirtualAddress(null)) is NullReferenceException);
            RunPublicReflectionBoundary();
        }
        Console.WriteLine("Formal declaring method public routes end");
    }

    private static void RunDelegateReflection()
    {
        MethodInfo method = Address(Formal()).Method;
        Check("delegate-reflection-owner", method.DeclaringType == Type.GetType("System.RuntimeType"));
        Check("delegate-reflection-typical", ReferenceEquals(method.Invoke(Formal(), null), Typical()));
        Check("delegate-reflection-type-formal", method.Invoke(typeof(FormalMethodOwner<>).GetGenericArguments()[0], null) is null);
        Exception error = Error(() => method.Invoke(typeof(int), null));
        Check("delegate-reflection-ordinary", error is TargetInvocationException invocation
            && invocation.InnerException is InvalidOperationException && invocation.InnerException.Message == NotParameter);
        Check("delegate-reflection-null", Error(() => method.Invoke(null, null)) is TargetException);
        Check("virtual-delegate-reflection", ReferenceEquals(VirtualAddress(Formal()).Method.Invoke(Formal(), null), Typical()));
        Check("flowing-delegate-reflection", ReferenceEquals(FlowingVirtualAddress(Formal()).Method.Invoke(Formal(), null), Typical()));
    }

    private static void RunPublicReflectionBoundary()
    {
        MethodInfo getter = null;
        Exception error = Error(() => getter = Getter());
        Check("named-reflection-unavailable", error is PlatformNotSupportedException || error is null && getter is null);
    }

    // Private CoreCLR method-context calls execute only in native fixture artifacts.
    private static void PrivateDirect(Type type) => throw new InvalidOperationException("fixture missing");
    private static void PrivatePointer(Type type) => throw new InvalidOperationException("fixture missing");
    private static void PrivateReflection(Type type) => typeof(RuntimeTypeHandle).GetMethod("GetDeclaringMethodForGenericParameter",
        BindingFlags.Static | BindingFlags.NonPublic, new[] { Type.GetType("System.RuntimeType") }).Invoke(null, new object[] { type });

    private static void PrivateRoute(string route, Action<Type> query)
    {
        Type[] types = { null, typeof(int), typeof(FormalMethodOwner<>).GetGenericArguments()[0], Formal() };
        string[] names = { "null", "ordinary", "type-formal", "method-formal" };
        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];
            Exception error = Error(() => query(type));
            if (error is TargetInvocationException invocation) error = invocation.InnerException;
            Check("private-" + route + "-" + names[i], error is PlatformNotSupportedException && error.Message == Boundary);
        }
    }

    internal static void RunPrivateArtifact(string[] args)
    {
        Run();
        PrivateRoute("direct", PrivateDirect);
        PrivateRoute("pointer", PrivatePointer);
        PrivateRoute("reflection", PrivateReflection);
        Console.WriteLine("Formal declaring method private boundary end");
    }
}
