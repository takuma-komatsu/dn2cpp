using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GenericConstraintArraySubset;

internal static class Program
{
    private interface ConstraintArrayLink<T> { }
    private class ConstraintArrayRoot { }
    private class ConstraintArrayHolder<T> : ConstraintArrayRoot { }
    private class ConstraintArrayOwner<T> where T : ConstraintArrayRoot
    {
        public static void Empty<U>() { }
        public static void Reference<U>() where U : class { }
        public static void Constructor<U>() where U : new() { }
        public static void Value<U>() where U : struct { }
        public static void Owner<U>() where U : T { }
        public static void Dependent<U, V>() where U : V where V : ConstraintArrayRoot { }
        public static void Composed<U, V>() where U : ConstraintArrayHolder<V>, ConstraintArrayLink<T[]> where V : T { }
        public static void Recursive<U>() where U : ConstraintArrayLink<U> { }
        public static void Mutual<U, V>() where U : ConstraintArrayLink<V> where V : ConstraintArrayLink<U> { }
    }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("constraint-array-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static Type Parameter(string method, int index = 0) =>
        typeof(ConstraintArrayOwner<ConstraintArrayRoot>).GetMethod(method).GetGenericArguments()[index];
    private static Type[] Public(Type type) => type.GetGenericParameterConstraints();
    private static Type[] RetainedGetter(Type type) => Public(type);
    private static bool InvalidHandle(Exception error, bool empty) => error is ArgumentException argument
        && (empty ? error is ArgumentNullException : error.GetType() == typeof(ArgumentException))
        && argument.ParamName is null && error.Message == "Invalid handle."
        && error.HResult == (empty ? unchecked((int)0x80004003) : unchecked((int)0x80070057));
    private static bool InvalidReceiver(Exception error) => error is InvalidOperationException
        && error.Message == "Method may only be called on a Type for which Type.IsGenericParameter is true."
        && error.HResult == unchecked((int)0x80131509);

    private static void Route(string route, Func<Type, Type[]> query, bool privateWrapper)
    {
        foreach (string method in new[] { "Empty", "Reference", "Constructor" })
        {
            Type[] empty = query(Parameter(method));
            Check(route + "-empty-" + method, privateWrapper ? empty is null : empty is not null && empty.Length == 0);
        }
        Type owner = typeof(ConstraintArrayOwner<>).GetGenericArguments()[0];
        Check(route + "-type-formal", query(owner)[0] == typeof(ConstraintArrayRoot));
        Check(route + "-method-formal", ReferenceEquals(query(Parameter("Owner"))[0], owner));
        Check(route + "-value", query(Parameter("Value"))[0] == typeof(ValueType));
        Type dependent = Parameter("Dependent", 1);
        Check(route + "-dependent", ReferenceEquals(query(Parameter("Dependent"))[0], dependent)
            && query(dependent)[0] == typeof(ConstraintArrayRoot));
        Type[] composed = query(Parameter("Composed"));
        Check(route + "-composed", composed.Length == 2 && composed[0].GetGenericTypeDefinition() == typeof(ConstraintArrayHolder<>)
            && ReferenceEquals(composed[0].GetGenericArguments()[0], Parameter("Composed", 1))
            && composed[1].GetGenericTypeDefinition() == typeof(ConstraintArrayLink<>)
            && ReferenceEquals(composed[1].GetGenericArguments()[0].GetElementType(), owner));
        Type recursive = Parameter("Recursive");
        Type first = Parameter("Mutual"), second = Parameter("Mutual", 1);
        Check(route + "-recursive", ReferenceEquals(query(recursive)[0].GetGenericArguments()[0], recursive)
            && ReferenceEquals(query(first)[0].GetGenericArguments()[0], second)
            && ReferenceEquals(query(second)[0].GetGenericArguments()[0], first));
        Type saved = composed[0];
        composed[0] = null;
        Type[] again = query(Parameter("Composed"));
        Check(route + "-fresh", !ReferenceEquals(composed, again) && ReferenceEquals(saved, again[0]));
        bool stable = true;
        for (int i = 0; i < 16; i++) stable &= ReferenceEquals(saved, query(Parameter("Composed"))[0]);
        GC.Collect();
        Check(route + "-repeated", stable && ReferenceEquals(saved, query(Parameter("Composed"))[0]));
    }

    private static void ArrayRoute(string route, Func<Type, Type[]> query)
    {
        Type[] array = query(Parameter("Composed"));
        Type runtimeArray = ((object)typeof(int)).GetType().MakeArrayType();
        Check(route + "-array-type", array.GetType() == typeof(Type[]) && !runtimeArray.IsInstanceOfType(array));
        object[] objects = array;
        // Only object identity is observed; no delegated Type operation is called.
        Type delegated = (Type)RuntimeHelpers.GetUninitializedObject(typeof(TypeDelegator));
        objects[0] = delegated;
        Check(route + "-store-type", ReferenceEquals(array[0], delegated));
        Check(route + "-store-object", Error(() => objects[0] = new object()) is ArrayTypeMismatchException);
        Check(route + "-store-isolation", !ReferenceEquals(query(Parameter("Composed"))[0], delegated));
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Generic constraint array contract ==");
        Route("public-payload", Public, false);
        Console.WriteLine("Generic constraint array contract end");
    }

    // The CLR oracle uses public metadata; private VM calls run only natively.
    private static Type[] HandleOracle(RuntimeTypeHandle handle)
    {
        Type type = Type.GetTypeFromHandle(handle);
        if (type is null) throw new ArgumentNullException(null, "Invalid handle.");
        if (!type.IsGenericParameter) throw new ArgumentException("Invalid handle.");
        Type[] result = type.GetGenericParameterConstraints();
        return result.Length == 0 ? null : result;
    }
    private static Type[] HandleDirect(RuntimeTypeHandle handle) => HandleOracle(handle);
    private static Type[] HandlePointer(RuntimeTypeHandle handle) => HandleOracle(handle);
    private static Func<Type[]> HandleAddress(RuntimeTypeHandle handle) => () => HandleOracle(handle);
    private static Type[] HandleDelegate(RuntimeTypeHandle handle) => HandleAddress(handle)();
    private static Type[] HandleReflection(RuntimeTypeHandle handle) => HandleOracle(handle);

    private static Type[] NonParameters() => new[] { typeof(object), typeof(ConstraintArrayOwner<>),
        typeof(ConstraintArrayOwner<ConstraintArrayRoot>), Parameter("Empty").MakeArrayType(),
        Parameter("Empty").MakeByRefType(), Parameter("Empty").MakePointerType() };

    private static void HandleRoute(string route, Func<RuntimeTypeHandle, Type[]> query)
    {
        Route(route, type => query(type.TypeHandle), true);
        ArrayRoute(route, type => query(type.TypeHandle));
        Check(route + "-default", InvalidHandle(Error(() => query(default)), true));
        Type[] invalid = NonParameters();
        for (int i = 0; i < invalid.Length; i++)
            Check(route + "-nonformal-" + i, InvalidHandle(Error(() => query(invalid[i].TypeHandle)), false));
    }

    internal static void RunHandleArtifact(string[] args)
    {
        Run();
        Route("retained-getter", RetainedGetter, false);
        ArrayRoute("retained-getter", RetainedGetter);
        Type[] empty = RetainedGetter(Parameter("Empty"));
        Check("retained-getter-emptytypes", ReferenceEquals(empty, Type.EmptyTypes)
            && ReferenceEquals(empty, RetainedGetter(Parameter("Empty"))) && empty.GetType() == typeof(Type[]));
        Type[] invalid = NonParameters();
        for (int i = 0; i < invalid.Length; i++)
            Check("retained-getter-nonformal-" + i, InvalidReceiver(Error(() => RetainedGetter(invalid[i]))));
        Exception nullError = Error(() => RetainedGetter(null));
        Check("retained-getter-null", nullError is NullReferenceException
            && nullError.HResult == unchecked((int)0x80004003)
            && nullError.Message == "Object reference not set to an instance of an object.");
        HandleRoute("direct", HandleDirect);
        HandleRoute("pointer", HandlePointer);
        HandleRoute("delegate", HandleDelegate);
        RunReflectionArtifact(args);
        if (args.Length > 0 && args[0] == "native-boundary")
        {
            MethodInfo method = HandleAddress(Parameter("Empty").TypeHandle).Method;
            Check("delegate-identity", method.Name == "GetConstraints" && method.DeclaringType == typeof(RuntimeTypeHandle)
                && method.ReturnType == typeof(Type[]) && !method.IsStatic && !method.IsPublic && method.GetParameters().Length == 0);
        }
        Console.WriteLine("Generic constraint array handle routes end");
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        HandleRoute("reflection", HandleReflection);
        Console.WriteLine("Generic constraint array reflection end");
    }
}
