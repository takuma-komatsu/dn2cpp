using System;
using System.Globalization;
using System.Reflection;

namespace ContainsGenericVariablesSubset;

internal static class Program
{
    private class ContainsVariableContainer<T, U>
    {
        public class Nested<V> { }
        public static void Method<A>() { }
    }
    private class ContainsVariableRecursive<T> where T : IComparable<T> { }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("contains-generic-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool NullReceiver(Exception error) => error is NullReferenceException
        && error.HResult == unchecked((int)0x80004003) && error.Message == "Object reference not set to an instance of an object.";
    private static bool InvalidHandle(Exception error) => error is ArgumentNullException argument
        && argument.ParamName is null && error.Message == "Invalid handle." && error.HResult == unchecked((int)0x80004003);
    private static bool Public(Type type) => type.ContainsGenericParameters;
    private static bool RetainedGetter(Type type) => Public(type);
    private static unsafe Type ClosedFunctionPointer() => typeof(delegate*<int, int>);

    private static void Route(string route, Func<Type, bool> query)
    {
        Type formal = typeof(ContainsVariableContainer<,>).GetGenericArguments()[1];
        Type method = typeof(ContainsVariableContainer<int, string>).GetMethod("Method").GetGenericArguments()[0];
        Check(route + "-type-formal", query(formal));
        Check(route + "-method-formal", query(method));
        Check(route + "-recursive-formal", query(typeof(ContainsVariableRecursive<>).GetGenericArguments()[0]));
        Check(route + "-definition", query(typeof(ContainsVariableContainer<,>)));
        Check(route + "-nested-definition", query(typeof(ContainsVariableContainer<,>.Nested<>)));
        Type open = typeof(ContainsVariableContainer<,>).MakeGenericType(new[] { typeof(int), formal });
        Check(route + "-open-constructed", query(open) && !open.IsGenericTypeDefinition);
        Type nested = typeof(ContainsVariableContainer<,>.Nested<>).MakeGenericType(new[] { typeof(int), typeof(string), method });
        Check(route + "-nested-constructed", query(nested) && !nested.IsGenericTypeDefinition);
        Type[] openElements = { formal.MakeArrayType(), formal.MakeArrayType(1), formal.MakeArrayType(2),
            formal.MakePointerType(), formal.MakeByRefType(), formal.MakeArrayType().MakePointerType().MakeByRefType(),
            method.MakeArrayType(), open.MakeArrayType(2), typeof(ContainsVariableContainer<,>).MakeArrayType() };
        for (int i = 0; i < openElements.Length; i++)
            Check(route + "-open-element-" + i, query(openElements[i]));
        Type[] closed = { typeof(int), typeof(object), typeof(void), typeof(ContainsVariableContainer<int, string>),
            typeof(ContainsVariableContainer<int, string>.Nested<double>), typeof(int[]), typeof(int[,]),
            Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 1 }).GetType(), typeof(int).MakePointerType(), typeof(int).MakeByRefType(), ClosedFunctionPointer() };
        for (int i = 0; i < closed.Length; i++)
            Check(route + "-closed-" + i, !query(closed[i]));
        Check(route + "-repeated", query(formal) && !query(typeof(int)) && query(open));
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Contains generic variables contract ==");
        Route("public", Public);
        Check("public-null", NullReceiver(Error(() => Public(null))));
        Console.WriteLine("Contains generic variables contract end");
    }

    // The CLR oracle uses public Type queries; private wrappers run only natively.
    private static bool HandleOracle(RuntimeTypeHandle handle)
    {
        Type type = Type.GetTypeFromHandle(handle);
        if (type is null) throw new ArgumentNullException(null, "Invalid handle.");
        return type.ContainsGenericParameters;
    }
    private static bool HandleDirect(RuntimeTypeHandle handle) => HandleOracle(handle);
    private static bool HandlePointer(RuntimeTypeHandle handle) => HandleOracle(handle);
    private static Func<bool> HandleAddress(RuntimeTypeHandle handle) => () => HandleOracle(handle);
    private static bool HandleDelegate(RuntimeTypeHandle handle) => HandleAddress(handle)();
    private static bool HandleReflection(RuntimeTypeHandle handle) => HandleOracle(handle);

    private static void HandleRoute(string route, Func<RuntimeTypeHandle, bool> query)
    {
        Route(route, type => query(type.TypeHandle));
        Check(route + "-default", InvalidHandle(Error(() => query(default))));
    }

    internal static void RunHandleArtifact(string[] args)
    {
        Run();
        Route("retained-getter", RetainedGetter);
        Check("retained-getter-null", NullReceiver(Error(() => RetainedGetter(null))));
        HandleRoute("direct", HandleDirect);
        HandleRoute("pointer", HandlePointer);
        HandleRoute("delegate", HandleDelegate);
        RunReflectionArtifact(args);
        if (args.Length > 0 && args[0] == "native-boundary")
        {
            MethodInfo method = HandleAddress(typeof(int).TypeHandle).Method;
            Check("delegate-identity", method.Name == "ContainsGenericVariables"
                && method.DeclaringType == typeof(RuntimeTypeHandle) && method.ReturnType == typeof(bool)
                && !method.IsStatic && !method.IsPublic && method.GetParameters().Length == 0);
        }
        Console.WriteLine("Contains generic variables handle routes end");
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        HandleRoute("reflection", HandleReflection);
        Console.WriteLine("Contains generic variables reflection end");
    }
}
