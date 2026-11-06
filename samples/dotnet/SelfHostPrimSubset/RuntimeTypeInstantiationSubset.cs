using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeInstantiationSubset;

internal static class Program
{
    private class InstantiationOwner<T> { public class Nested<U> { } public void Method<U>() { } }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("type-instantiation-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    // Public-only oracle for the private wrappers' native metadata contract.
    private static Type[] PublicHandle(RuntimeTypeHandle handle)
    {
        Type type = Type.GetTypeFromHandle(handle);
        if (type is null) throw new ArgumentNullException(null, "Invalid handle.");
        return type.IsGenericType ? type.GetGenericArguments() : null;
    }

    private static Type[] InternalHandle(RuntimeTypeHandle handle)
    {
        Type[] arguments = PublicHandle(handle);
        if (arguments is null) return null;
        Type[] result = (Type[])Array.CreateInstance(((object)typeof(int)).GetType(), arguments.Length);
        for (int i = 0; i < arguments.Length; i++) result[i] = arguments[i];
        return result;
    }

    private static Type[] InternalDirect(RuntimeTypeHandle handle) => InternalHandle(handle);
    private static Type[] PublicDirect(RuntimeTypeHandle handle) => PublicHandle(handle);
    private static Type[] InternalPointer(RuntimeTypeHandle handle) => InternalHandle(handle);
    private static Type[] PublicPointer(RuntimeTypeHandle handle) => PublicHandle(handle);
    private static Func<Type[]> InternalAddress(RuntimeTypeHandle handle) => () => InternalHandle(handle);
    private static Func<Type[]> PublicAddress(RuntimeTypeHandle handle) => () => PublicHandle(handle);
    private static Type[] InternalDelegate(RuntimeTypeHandle handle) => InternalAddress(handle)();
    private static Type[] PublicDelegate(RuntimeTypeHandle handle) => PublicAddress(handle)();
    private static Type[] InternalReflection(RuntimeTypeHandle handle) => InternalHandle(handle);
    private static Type[] PublicReflection(RuntimeTypeHandle handle) => PublicHandle(handle);
    private static Type[] Generic<T>() => PublicHandle(typeof(T).TypeHandle);
    private static bool IsRuntimeArray(Type[] array) => ((object)typeof(int)).GetType().MakeArrayType().IsInstanceOfType(array);
    private static Type[] InternalGetter(Type type)
    {
        while (type.HasElementType) type = type.GetElementType();
        return InternalHandle(type.TypeHandle);
    }
    private static Type[] PublicGetter(Type type) => type.GetGenericArguments();

    private static void Route(string route, Func<RuntimeTypeHandle, Type[]> internalQuery,
        Func<RuntimeTypeHandle, Type[]> publicQuery)
    {
        Type formal = typeof(InstantiationOwner<>).GetGenericArguments()[0];
        Type methodFormal = typeof(InstantiationOwner<>).GetMethod("Method").GetGenericArguments()[0];
        Type[] empty = { typeof(int), typeof(string), typeof(IDisposable), formal, methodFormal,
            typeof(InstantiationOwner<int>[]), typeof(InstantiationOwner<int>[,]), typeof(InstantiationOwner<int>).MakeByRefType(), typeof(InstantiationOwner<int>).MakePointerType() };
        for (int i = 0; i < empty.Length; i++)
            Check(route + "-empty-" + i, internalQuery(empty[i].TypeHandle) is null && publicQuery(empty[i].TypeHandle) is null);
        Type[] owners = { typeof(InstantiationOwner<int>), typeof(InstantiationOwner<string>), typeof(InstantiationOwner<>), typeof(InstantiationOwner<int>.Nested<string>), typeof(InstantiationOwner<>.Nested<>),
            typeof(InstantiationOwner<>.Nested<>).MakeGenericType(typeof(int), formal) };
        for (int i = 0; i < owners.Length; i++)
        {
            Type[] expected = owners[i].GetGenericArguments();
            Type[] a = internalQuery(owners[i].TypeHandle);
            Type[] b = publicQuery(owners[i].TypeHandle);
            Check(route + "-length-" + i, a.Length == expected.Length && b.Length == expected.Length);
            Check(route + "-array-" + i, a.GetType().GetElementType() == ((object)typeof(int)).GetType()
                && b.GetType() == typeof(Type[]) && IsRuntimeArray(a) && !IsRuntimeArray(b));
            for (int j = 0; j < expected.Length; j++)
                Check(route + "-identity-" + i + "-" + j, ReferenceEquals(a[j], expected[j]) && ReferenceEquals(b[j], expected[j]));
            Check(route + "-fresh-" + i, !ReferenceEquals(a, internalQuery(owners[i].TypeHandle))
                && !ReferenceEquals(b, publicQuery(owners[i].TypeHandle)) && !ReferenceEquals(a, b));
            a[0] = typeof(string); a[0] = null; b[0] = null;
            Check(route + "-mutation-" + i, internalQuery(owners[i].TypeHandle)[0] == expected[0]
                && publicQuery(owners[i].TypeHandle)[0] == expected[0]);
            object[] objects = a;
            Check(route + "-store-" + i, Error(() => objects[0] = new object()) is ArrayTypeMismatchException);
        }
        Type[] parameters = internalQuery(typeof(InstantiationOwner<>.Nested<>).TypeHandle);
        Check(route + "-formals", parameters[0].IsGenericParameter && parameters[1].IsGenericParameter
            && !ReferenceEquals(parameters[0], parameters[1])
            && parameters[0].DeclaringType == typeof(InstantiationOwner<>.Nested<>) && parameters[1].DeclaringType == typeof(InstantiationOwner<>.Nested<>));
        Exception first = Error(() => internalQuery(default));
        Exception second = Error(() => publicQuery(default));
        Check(route + "-default", first is ArgumentNullException aex && second is ArgumentNullException bex
            && aex.ParamName is null && bex.ParamName is null && aex.Message == "Invalid handle." && bex.Message == aex.Message
            && aex.HResult == unchecked((int)0x80004003) && bex.HResult == aex.HResult);
        Check(route + "-emptytypes", Type.EmptyTypes.GetType() == typeof(Type[]) && Type.EmptyTypes.Length == 0 && !IsRuntimeArray(Type.EmptyTypes));
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type instantiation contract ==");
        Route("public-oracle", InternalHandle, PublicHandle);
        Check("generic", Generic<InstantiationOwner<int>>()[0] == typeof(int) && Generic<InstantiationOwner<string>>()[0] == typeof(string) && Generic<int>() is null);
        Console.WriteLine("Runtime type instantiation contract end");
    }

    internal static void RunArtifact(string[] args)
    {
        Run();
        Route("direct", InternalDirect, PublicDirect);
        Route("pointer", InternalPointer, PublicPointer);
        Route("delegate", InternalDelegate, PublicDelegate);
        RunReflectionArtifact(args);
        Check("getter-empty", InternalGetter(typeof(int)) is null && PublicGetter(typeof(int)).Length == 0);
        Check("getter-closed", InternalGetter(typeof(InstantiationOwner<int>))[0] == typeof(int) && PublicGetter(typeof(InstantiationOwner<int>))[0] == typeof(int));
        Check("getter-formal", InternalGetter(typeof(InstantiationOwner<>))[0] == typeof(InstantiationOwner<>).GetGenericArguments()[0]);
        Check("getter-root", InternalGetter(typeof(InstantiationOwner<int>[]))[0] == typeof(int) && PublicGetter(typeof(InstantiationOwner<int>[]))[0] == typeof(int));
        if (args.Length > 0 && args[0] == "native-identity")
            Check("address-identity", InternalAddress(typeof(int).TypeHandle).Method.Name == "GetInstantiationInternal"
                && PublicAddress(typeof(int).TypeHandle).Method.Name == "GetInstantiationPublic"
                && InternalAddress(typeof(int).TypeHandle).Method.DeclaringType == typeof(RuntimeTypeHandle));
        Console.WriteLine("Runtime type instantiation routes end");
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Route("reflection", InternalReflection, PublicReflection);
        Console.WriteLine("Runtime type instantiation reflection end");
    }
}
