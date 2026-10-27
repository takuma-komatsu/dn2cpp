using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeMakeGenericSubset;

internal static class Program
{
    private class MakeGenericOwner<T> { }
    private class MakeGenericPair<T, U> { }
    private class MakeGenericClass<T> where T : class { }
    private class MakeGenericPlannedField<T> { public T Value; }
    private class MakeGenericForwardedField<T> { public T Value; }

    // No closed-type token roots this layout; the call's known arguments must plan it.
    private static Type Planned() => typeof(MakeGenericPlannedField<>).MakeGenericType(new[] { typeof(int) });
    private static Type Forwarded() => typeof(MakeGenericForwardedField<>).MakeGenericType(new[] { typeof(int) });
    private const string Boundary = "System.RuntimeTypeHandle.Instantiate requires CoreCLR type construction and constraint validation. Use Type.MakeGenericType instead.";

    private static void Check(string label, bool success)
    {
        Console.WriteLine("make-generic-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool Argument(Exception error, Type kind, string parameter = null) =>
        error is ArgumentException argument && error.GetType() == kind && argument.ParamName == parameter;

    private static Type Public(Type type, Type[] arguments) => type.MakeGenericType(arguments);

    // The public override's IL validates the array before dereferencing a nonvirtual receiver.
    private static Type Direct(Type type, Type[] arguments)
    {
        if (arguments is null) throw new ArgumentNullException("typeArguments");
        return type.MakeGenericType(arguments);
    }
    private static Type Virtual(Type type, Type[] arguments) => type.MakeGenericType(arguments);
    private static Type Pointer(Type type, Type[] arguments) => Direct(type, arguments);
    private static MethodInfo PublicMethod() => ((object)typeof(int)).GetType().GetMethod("MakeGenericType",
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, new[] { typeof(Type[]) });
    private static Func<Type[], Type> Address(Type type) => (Func<Type[], Type>)PublicMethod().CreateDelegate(typeof(Func<Type[], Type>), type);
    private static Func<Type[], Type> VirtualAddress(Type type) => Address(type);
    private static Func<Type[], Type> FlowingAddress(Type type) => Address(type);

    private static void PublicRoute(string route, Func<Type, Type[], Type> query)
    {
        Type owner = typeof(MakeGenericOwner<>);
        Type pair = typeof(MakeGenericPair<,>);
        Type formal = owner.GetGenericArguments()[0];
        Check(route + "-single", ReferenceEquals(query(owner, new[] { typeof(int) }), typeof(MakeGenericOwner<int>)));
        Check(route + "-reference", ReferenceEquals(query(owner, new[] { typeof(string) }), typeof(MakeGenericOwner<string>)));
        Check(route + "-pair", ReferenceEquals(query(pair, new[] { typeof(int), typeof(string) }), typeof(MakeGenericPair<int, string>)));
        Check(route + "-constraint", ReferenceEquals(query(typeof(MakeGenericClass<>), new[] { typeof(string) }), typeof(MakeGenericClass<string>)));
        Type open = query(pair, new[] { typeof(int), formal });
        Check(route + "-open", open.ContainsGenericParameters && open.IsConstructedGenericType
            && open.GetGenericArguments()[0] == typeof(int) && ReferenceEquals(open.GetGenericArguments()[1], formal));
        Check(route + "-repeated", ReferenceEquals(open, query(pair, new[] { typeof(int), formal }))
            && ReferenceEquals(query(owner, new[] { typeof(int) }), query(owner, new[] { typeof(int) })));
        Check(route + "-null-array", Argument(Error(() => query(owner, null)), typeof(ArgumentNullException), "typeArguments"));
        Check(route + "-nondefinition", Error(() => query(typeof(int), Type.EmptyTypes)) is InvalidOperationException);
        Check(route + "-nondefinition-null", Argument(Error(() => query(typeof(int), null)), typeof(ArgumentNullException), "typeArguments"));
        Check(route + "-arity", Argument(Error(() => query(owner, Type.EmptyTypes)), typeof(ArgumentException), "typeArguments"));
        Check(route + "-null-element", Argument(Error(() => query(owner, new Type[] { null })), typeof(ArgumentNullException)));
        Check(route + "-arity-before-element", Argument(Error(() => query(pair, new Type[] { null })), typeof(ArgumentException), "typeArguments"));
        Check(route + "-void", Argument(Error(() => query(owner, new[] { typeof(void) })), typeof(ArgumentException)));
        Check(route + "-byref", Argument(Error(() => query(owner, new[] { typeof(int).MakeByRefType() })), typeof(ArgumentException)));
        Check(route + "-pointer", Argument(Error(() => query(owner, new[] { typeof(int).MakePointerType() })), typeof(ArgumentException)));
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type MakeGenericType contract ==");
        PublicRoute("public", Public);
        Type planned = Planned();
        Check("planned-closure", planned.GetGenericArguments()[0] == typeof(int)
            && planned.GetField("Value").FieldType == typeof(int));
        Type forwarded = Forwarded();
        Check("forwarded-closure", forwarded.GetGenericArguments()[0] == typeof(int)
            && forwarded.GetField("Value").FieldType == typeof(int));
        Check("public-null-receiver", Error(() => Public(null, new[] { typeof(int) })) is NullReferenceException);
        Check("public-both-null", Error(() => Public(null, null)) is NullReferenceException);
        Console.WriteLine("Runtime type MakeGenericType contract end");
    }

    internal static void RunPublicArtifact(string[] args)
    {
        Run();
        PublicRoute("direct", Direct);
        PublicRoute("virtual", Virtual);
        PublicRoute("pointer", Pointer);
        PublicRoute("address", (type, arguments) => Address(type)(arguments));
        PublicRoute("virtual-address", (type, arguments) => VirtualAddress(type)(arguments));
        PublicRoute("flowing-address", (type, arguments) => FlowingAddress(type)(arguments));
        Check("direct-null-receiver", Error(() => Direct(null, new[] { typeof(int) })) is NullReferenceException);
        Check("pointer-null-receiver", Error(() => Pointer(null, new[] { typeof(int) })) is NullReferenceException);
        Check("direct-both-null", Argument(Error(() => Direct(null, null)), typeof(ArgumentNullException), "typeArguments"));
        Check("pointer-both-null", Argument(Error(() => Pointer(null, null)), typeof(ArgumentNullException), "typeArguments"));
        Check("virtual-both-null", Error(() => Virtual(null, null)) is NullReferenceException);
        Type definition = typeof(MakeGenericOwner<>);
        MethodInfo method = Address(definition).Method;
        Check("delegate-identity", method.Name == "MakeGenericType" && method.DeclaringType == Type.GetType("System.RuntimeType")
            && method.ReturnType == typeof(Type) && method.GetParameters()[0].ParameterType == typeof(Type[]));
        Check("delegate-reflection", ReferenceEquals(method.Invoke(definition, new object[] { new[] { typeof(int) } }), typeof(MakeGenericOwner<int>)));
        Check("delegate-reflection-null", Argument(Error(() => method.Invoke(definition, new object[] { null })), typeof(ArgumentNullException), "typeArguments"));
        Check("virtual-delegate-identity", VirtualAddress(definition).Method == method && FlowingAddress(definition).Method == method);
        if (args.Length > 0 && args[0] == "native-boundary")
        {
            Check("virtual-address-null", Error(() => VirtualAddress(null)) is NullReferenceException);
            Check("flowing-address-null", Error(() => FlowingAddress(null)) is NullReferenceException);
        }
        Console.WriteLine("Runtime type MakeGenericType public routes end");
    }

    // Only patched native artifacts enter these CoreCLR-private wrappers.
    private static Type ScalarDirect(RuntimeTypeHandle handle, Type argument) => throw new InvalidOperationException("fixture missing");
    private static Type ScalarPointer(RuntimeTypeHandle handle, Type argument) => throw new InvalidOperationException("fixture missing");
    private delegate Type MakeScalarInvoker(Type argument);
    private static MakeScalarInvoker ScalarAddress(RuntimeTypeHandle handle) => throw new InvalidOperationException("fixture missing");
    private static Type ScalarDelegate(RuntimeTypeHandle handle, Type argument) => ScalarAddress(handle)(argument);
    private static Type ArrayDirect(RuntimeTypeHandle handle, Type[] arguments) => throw new InvalidOperationException("fixture missing");
    private static Type ArrayPointer(RuntimeTypeHandle handle, Type[] arguments) => throw new InvalidOperationException("fixture missing");
    private static Func<Type[], Type> ArrayAddress(RuntimeTypeHandle handle) => throw new InvalidOperationException("fixture missing");
    private static Type ArrayDelegate(RuntimeTypeHandle handle, Type[] arguments) => ArrayAddress(handle)(arguments);
    private static Type ScalarReflection(RuntimeTypeHandle handle, Type argument) => (Type)typeof(RuntimeTypeHandle)
        .GetMethod("Instantiate", BindingFlags.NonPublic | BindingFlags.Instance, new[] { Type.GetType("System.RuntimeType") })
        .Invoke(handle, new object[] { argument });
    private static Type ArrayReflection(RuntimeTypeHandle handle, Type[] arguments) => (Type)typeof(RuntimeTypeHandle)
        .GetMethod("Instantiate", BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Type[]) })
        .Invoke(handle, new object[] { arguments });

    private static bool InvalidHandle(Exception error) => Argument(error, typeof(ArgumentNullException))
        && error.Message == "Invalid handle." && error.HResult == unchecked((int)0x80004003);
    private static bool Unsupported(Exception error) => error is PlatformNotSupportedException && error.Message == Boundary;

    private static void PrivateRoute(string route, Func<RuntimeTypeHandle, Type, Type> scalar,
        Func<RuntimeTypeHandle, Type[], Type> array)
    {
        RuntimeTypeHandle handle = typeof(MakeGenericOwner<>).TypeHandle;
        Check(route + "-scalar-valid", Unsupported(Error(() => scalar(handle, typeof(int)))));
        Check(route + "-scalar-null", Error(() => scalar(handle, null)) is NullReferenceException);
        Check(route + "-scalar-default", InvalidHandle(Error(() => scalar(default, typeof(int)))));
        Check(route + "-scalar-both-null", Error(() => scalar(default, null)) is NullReferenceException);
        Check(route + "-array-valid", Unsupported(Error(() => array(handle, new[] { typeof(int) }))));
        Check(route + "-array-null", Unsupported(Error(() => array(handle, null))));
        Check(route + "-array-empty", Unsupported(Error(() => array(handle, Type.EmptyTypes))));
        Check(route + "-array-null-element", Error(() => array(handle, new Type[] { typeof(int), null })) is NullReferenceException);
        Check(route + "-array-default", InvalidHandle(Error(() => array(default, new[] { typeof(int) }))));
        Check(route + "-array-default-null", InvalidHandle(Error(() => array(default, null))));
        Check(route + "-array-default-empty", InvalidHandle(Error(() => array(default, Type.EmptyTypes))));
        Check(route + "-array-default-element", Error(() => array(default, new Type[] { typeof(int), null })) is NullReferenceException);
        Check(route + "-not-definition", Unsupported(Error(() => scalar(typeof(int).TypeHandle, typeof(string)))));
        Check(route + "-constraint", Unsupported(Error(() => scalar(typeof(MakeGenericClass<>).TypeHandle, typeof(int)))));
    }

    internal static void RunPrivateArtifact(string[] args)
    {
        Run();
        PrivateRoute("direct", ScalarDirect, ArrayDirect);
        PrivateRoute("pointer", ScalarPointer, ArrayPointer);
        PrivateRoute("delegate", ScalarDelegate, ArrayDelegate);
        RunReflectionArtifact(args);
        Check("private-delegate-identity", ScalarAddress(typeof(MakeGenericOwner<>).TypeHandle).Method.Name == "Instantiate"
            && ArrayAddress(typeof(MakeGenericOwner<>).TypeHandle).Method.DeclaringType == typeof(RuntimeTypeHandle));
        Console.WriteLine("Runtime type Instantiate private routes end");
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        PrivateRoute("reflection", ScalarReflection, ArrayReflection);
        Console.WriteLine("Runtime type Instantiate reflection end");
    }
}
