using System;
using System.Globalization;
using System.Reflection;

namespace GenericParameterPositionSubset;

internal static class Program
{
    private class PositionOwner<T, U>
    {
        public class Nested<V> { public static void Other<A, B>() { } }
        public static void Method<A, B>() { }
    }
    private class PositionChild : PositionOwner<int, string> { }
    private const string NotParameter = "Method may only be called on a Type for which Type.IsGenericParameter is true.";

    private static void Check(string label, bool success)
    {
        Console.WriteLine("generic-position-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool Nonformal(Exception error) => error is InvalidOperationException
        && error.Message == NotParameter && error.HResult == unchecked((int)0x80131509);
    private static bool NullReceiver(Exception error) => error is NullReferenceException
        && error.HResult == unchecked((int)0x80004003) && error.Message == "Object reference not set to an instance of an object.";
    private static bool InvalidHandle(Exception error) => error is ArgumentNullException argument
        && argument.ParamName is null && error.Message == "Invalid handle." && error.HResult == unchecked((int)0x80004003);
    private static int Public(Type type) => type.GenericParameterPosition;
    private static int Direct(Type type) => Public(type);
    private static int Virtual(Type type) => Public(type);
    private static int Pointer(Type type) => Public(type);
    private static MethodInfo Getter() => ((object)typeof(int)).GetType().GetMethod("get_GenericParameterPosition",
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    private static Func<int> Address(Type type) => (Func<int>)Getter().CreateDelegate(typeof(Func<int>), type);
    private static Func<int> VirtualAddress(Type type) => Address(type);
    private static Func<int> FlowingAddress(Type type) => Address(type);

    private static void Route(string route, Func<Type, int> query)
    {
        Type[] closed = typeof(PositionOwner<int, string>).GetMethod("Method").GetGenericArguments();
        Type[] inherited = typeof(PositionChild).GetMethod("Method", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetGenericArguments();
        Type[] typical = typeof(PositionOwner<,>).GetMethod("Method").GetGenericArguments();
        Check(route + "-method", query(closed[0]) == 0 && query(closed[1]) == 1);
        Check(route + "-typical", ReferenceEquals(closed[0], typical[0]) && ReferenceEquals(inherited[1], typical[1])
            && query(inherited[1]) == 1 && closed[0].DeclaringType == typeof(PositionOwner<,>));
        Type[] formals = typeof(PositionOwner<,>).GetGenericArguments();
        Check(route + "-type", query(formals[0]) == 0 && query(formals[1]) == 1);
        Type[] nested = typeof(PositionOwner<,>.Nested<>).GetGenericArguments();
        Check(route + "-nested", query(nested[0]) == 0 && query(nested[1]) == 1 && query(nested[2]) == 2
            && nested[0].DeclaringType == typeof(PositionOwner<,>.Nested<>));
        Type[] method = typeof(PositionOwner<int, string>.Nested<double>).GetMethod("Other").GetGenericArguments();
        Check(route + "-nested-method", query(method[0]) == 0 && query(method[1]) == 1);
        Type reordered = typeof(PositionOwner<,>).MakeGenericType(new[] { formals[1], formals[0] });
        Type[] arguments = reordered.GetGenericArguments();
        Check(route + "-reordered", query(arguments[0]) == 1 && query(arguments[1]) == 0
            && ReferenceEquals(arguments[0], formals[1]) && ReferenceEquals(arguments[1], formals[0]));
        Check(route + "-repeated", query(formals[1]) == query(formals[1]) && query(closed[1]) == query(typical[1]));
        Type[] nonformals = { typeof(int), typeof(void), typeof(PositionOwner<,>), typeof(PositionOwner<int, string>), reordered,
            typeof(int[]), typeof(int[,]), typeof(int).MakePointerType(), typeof(int).MakeByRefType(),
            formals[0].MakeArrayType(), formals[0].MakePointerType(), formals[0].MakeByRefType() };
        for (int i = 0; i < nonformals.Length; i++)
        {
            Type type = nonformals[i];
            Check(route + "-nonformal-" + i, Nonformal(Error(() => query(type))));
        }
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Generic parameter position contract ==");
        Route("public", Public);
        Check("public-null", NullReceiver(Error(() => Public(null))));
        Console.WriteLine("Generic parameter position contract end");
    }

    internal static void RunPublicArtifact(string[] args)
    {
        Run();
        Route("direct", Direct);
        Route("virtual", Virtual);
        Route("pointer", Pointer);
        Route("address", type => Address(type)());
        Route("virtual-address", type => VirtualAddress(type)());
        Route("flowing-address", type => FlowingAddress(type)());
        Check("direct-null", NullReceiver(Error(() => Direct(null))));
        Check("pointer-null", NullReceiver(Error(() => Pointer(null))));
        Check("virtual-null", NullReceiver(Error(() => Virtual(null))));
        Type formal = typeof(PositionOwner<,>).GetGenericArguments()[1];
        MethodInfo getter = Address(formal).Method;
        Check("delegate-identity", getter.Name == "get_GenericParameterPosition"
            && getter.DeclaringType == Type.GetType("System.RuntimeType") && getter.ReturnType == typeof(int));
        Check("delegate-reflection", (int)getter.Invoke(formal, null) == 1
            && Nonformal(Error(() => getter.Invoke(typeof(int), null))));
        Check("delegate-reflection-null", Error(() => getter.Invoke(null, null)) is TargetException);
        Check("virtual-delegate-identity", VirtualAddress(formal).Method == getter && FlowingAddress(formal).Method == getter);
        if (args.Length > 0 && args[0] == "native-boundary")
        {
            Check("virtual-address-null", NullReceiver(Error(() => VirtualAddress(null))));
            Check("flowing-address-null", NullReceiver(Error(() => FlowingAddress(null))));
        }
        Console.WriteLine("Generic parameter position public routes end");
    }

    // The CLR oracle uses only the public API; patched private wrappers run natively.
    private static int HandleOracle(RuntimeTypeHandle handle)
    {
        Type type = Type.GetTypeFromHandle(handle);
        if (type is null) throw new ArgumentNullException(null, "Invalid handle.");
        return type.GenericParameterPosition;
    }
    private static int HandleDirect(RuntimeTypeHandle handle) => HandleOracle(handle);
    private static int HandlePointer(RuntimeTypeHandle handle) => HandleOracle(handle);
    private static Func<int> HandleAddress(RuntimeTypeHandle handle) => () => HandleOracle(handle);
    private static int HandleDelegate(RuntimeTypeHandle handle) => HandleAddress(handle)();
    private static int HandleReflection(RuntimeTypeHandle handle) => HandleOracle(handle);

    private static void HandleRoute(string route, Func<RuntimeTypeHandle, int> query)
    {
        Route(route, type => query(type.TypeHandle));
        Check(route + "-default", InvalidHandle(Error(() => query(default))));
    }

    internal static void RunHandleArtifact(string[] args)
    {
        Run();
        HandleRoute("handle-direct", HandleDirect);
        HandleRoute("handle-pointer", HandlePointer);
        HandleRoute("handle-delegate", HandleDelegate);
        RunReflectionArtifact(args);
        if (args.Length > 0 && args[0] == "native-boundary")
            Check("handle-delegate-identity", HandleAddress(typeof(int).TypeHandle).Method.Name == "GetGenericVariableIndex"
                && HandleAddress(typeof(int).TypeHandle).Method.DeclaringType == typeof(RuntimeTypeHandle));
        Console.WriteLine("Generic parameter position handle routes end");
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        HandleRoute("handle-reflection", HandleReflection);
        Console.WriteLine("Generic parameter position reflection end");
    }
}
