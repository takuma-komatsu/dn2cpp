using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeHandleAccessorSubset;

internal static class Program
{
    public struct Value { public int Number; }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("handle-accessor-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Type CheckedReflection(RuntimeTypeHandle handle) =>
        (Type)typeof(RuntimeTypeHandle).GetMethod("GetRuntimeTypeChecked",
            BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes).Invoke(handle, null);

    private static Type UncheckedReflection(RuntimeTypeHandle handle) =>
        (Type)typeof(RuntimeTypeHandle).GetMethod("GetRuntimeType",
            BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes).Invoke(handle, null);

    // The fixture replaces these with exact-signature direct and address-taken calls.
    private static Type CheckedDirect(RuntimeTypeHandle handle) => CheckedReflection(handle);
    private static Type UncheckedDirect(RuntimeTypeHandle handle) => UncheckedReflection(handle);
    private static Type CheckedPointer(RuntimeTypeHandle handle) => CheckedReflection(handle);
    private static Type UncheckedPointer(RuntimeTypeHandle handle) => UncheckedReflection(handle);

    private static void RunRoute(string route, Func<RuntimeTypeHandle, Type> checkedGet,
        Func<RuntimeTypeHandle, Type> uncheckedGet)
    {
        Check(route + "-default-unchecked", uncheckedGet(default) is null);
        try
        {
            checkedGet(default);
            throw new InvalidOperationException("default checked handle accepted");
        }
        catch (Exception error)
        {
            Exception inner = error is TargetInvocationException invocation ? invocation.InnerException : error;
            Check(route + "-default-checked", inner is ArgumentNullException argument
                && argument.ParamName is null && argument.Message == "Invalid handle."
                && argument.HResult == unchecked((int)0x80004003));
        }
        Type[] types = { typeof(object), typeof(int), typeof(Value), typeof(int[]),
            typeof(List<string>), typeof(List<object>), typeof(List<>),
            typeof(List<>).GetGenericArguments()[0], typeof(int).MakeByRefType() };
        string[] labels = { "object", "int", "struct", "array", "generic-string", "generic-object",
            "definition", "parameter", "byref" };
        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];
            RuntimeTypeHandle handle = type.TypeHandle;
            Type first = checkedGet(handle);
            Check(route + "-" + labels[i], ReferenceEquals(first, type)
                && ReferenceEquals(first, checkedGet(handle))
                && ReferenceEquals(first, uncheckedGet(handle))
                && ReferenceEquals(first, Type.GetTypeFromHandle(handle)));
        }
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type handle accessors ==");
        MethodInfo checkedMethod = typeof(RuntimeTypeHandle).GetMethod("GetRuntimeTypeChecked",
            BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes);
        MethodInfo uncheckedMethod = typeof(RuntimeTypeHandle).GetMethod("GetRuntimeType",
            BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes);
        Check("shape", checkedMethod is not null && uncheckedMethod is not null
            && checkedMethod.IsAssembly && uncheckedMethod.IsAssembly
            && !checkedMethod.IsStatic && !uncheckedMethod.IsStatic
            && !checkedMethod.IsGenericMethod && !uncheckedMethod.IsGenericMethod
            && checkedMethod.ReturnType == Type.GetType("System.RuntimeType")
            && uncheckedMethod.ReturnType == checkedMethod.ReturnType
            && (checkedMethod.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0
            && (uncheckedMethod.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0);
        RunRoute("reflection", CheckedReflection, UncheckedReflection);
        Check("public-roundtrip", Type.GetTypeFromHandle(default) is null
            && typeof(int).TypeHandle.Equals(typeof(int).TypeHandle)
            && !typeof(List<string>).TypeHandle.Equals(typeof(List<object>).TypeHandle));
        Check("generic-valid", typeof(List<>).MakeGenericType(typeof(string)) == typeof(List<string>));
        try
        {
            typeof(List<>).MakeGenericType(typeof(void));
            throw new InvalidOperationException("invalid generic argument accepted");
        }
        catch (ArgumentException error) { Check("generic-invalid-argument", error.ParamName is null); }
        try
        {
            typeof(List<>).MakeGenericType((Type[])null);
            throw new InvalidOperationException("null generic arguments accepted");
        }
        catch (ArgumentNullException error) { Check("generic-null-arguments", error.ParamName == "typeArguments"); }
        Console.WriteLine("Runtime type handle accessors end");
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        RunRoute("direct", CheckedDirect, UncheckedDirect);
        RunRoute("pointer", CheckedPointer, UncheckedPointer);
        Console.WriteLine("Runtime type handle accessor artifact end");
    }
}
