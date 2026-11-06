using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeHandleConstructorSubset;

internal static class Program
{
    public struct Value { public int Number; }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("handle-ctor-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static RuntimeTypeHandle CreateReflection(Type type) =>
        (RuntimeTypeHandle)typeof(RuntimeTypeHandle).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new Type[] { Type.GetType("System.RuntimeType") }, null).Invoke(new object[] { type });

    // The fixture imports the exact CoreLib constructor for these native routes.
    private static RuntimeTypeHandle CreateNewobj(Type type) => CreateReflection(type);
    private static void InitializeDirect(ref RuntimeTypeHandle handle, Type type) => handle = CreateReflection(type);
    private static void InitializePointer(ref RuntimeTypeHandle handle, Type type) => handle = CreateReflection(type);
    private static RuntimeTypeHandle CreateDirect(Type type)
    {
        RuntimeTypeHandle handle = default;
        InitializeDirect(ref handle, type);
        return handle;
    }
    private static RuntimeTypeHandle CreatePointer(Type type)
    {
        RuntimeTypeHandle handle = default;
        InitializePointer(ref handle, type);
        return handle;
    }
    private static void Clear(ref RuntimeTypeHandle handle) => handle = default;

    private static void RunRoute(string route, Func<Type, RuntimeTypeHandle> create)
    {
        RuntimeTypeHandle empty = create(null);
        Check(route + "-null", empty.Equals(default(RuntimeTypeHandle))
            && empty.Value == IntPtr.Zero && Type.GetTypeFromHandle(empty) is null && empty.GetHashCode() == 0);
        Type[] types = { typeof(object), typeof(int), typeof(Value), typeof(int[]),
            typeof(List<string>), typeof(List<object>), typeof(List<>),
            typeof(List<>).GetGenericArguments()[0], typeof(int).MakeByRefType(), typeof(int).MakePointerType() };
        string[] labels = { "object", "int", "struct", "array", "generic-string", "generic-object",
            "definition", "parameter", "byref", "pointer" };
        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];
            RuntimeTypeHandle handle = create(type);
            Check(route + "-" + labels[i], handle.Equals(type.TypeHandle)
                && handle.Value == type.TypeHandle.Value && ReferenceEquals(Type.GetTypeFromHandle(handle), type));
        }
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type handle constructor ==");
        ConstructorInfo constructor = typeof(RuntimeTypeHandle).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new Type[] { Type.GetType("System.RuntimeType") }, null);
        Check("shape", constructor is not null && constructor.IsAssembly && !constructor.IsStatic
            && !constructor.IsGenericMethod && constructor.GetParameters().Length == 1
            && (constructor.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0);
        RunRoute("reflection", CreateReflection);
        RuntimeTypeHandle initialized = typeof(int).TypeHandle;
        Clear(ref initialized);
        Check("initobj", initialized.Equals(new RuntimeTypeHandle()) && initialized.Equals(CreateReflection(null)));
        object boxed = typeof(int).TypeHandle;
        object result = constructor.Invoke(boxed, BindingFlags.Default, null, new object[] { typeof(string) }, null);
        Check("boxed-replace", result is null && ((RuntimeTypeHandle)boxed).Equals(typeof(string).TypeHandle));
        result = constructor.Invoke(boxed, BindingFlags.Default, null, new object[] { null }, null);
        Check("boxed-clear", result is null && ((RuntimeTypeHandle)boxed).Equals(default(RuntimeTypeHandle)));
        Check("public-identity", typeof(int).TypeHandle.Equals(typeof(int).TypeHandle)
            && !typeof(List<string>).TypeHandle.Equals(typeof(List<object>).TypeHandle)
            && ReferenceEquals(Type.GetTypeFromHandle(typeof(Value).TypeHandle), typeof(Value)));
        Console.WriteLine("Runtime type handle constructor end");
    }

    internal static void RunReflectionChecks(string[] args) => Run();

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        RunRoute("newobj", CreateNewobj);
        RunRoute("direct", CreateDirect);
        RunRoute("pointer", CreatePointer);
        RuntimeTypeHandle handle = typeof(int).TypeHandle;
        InitializeDirect(ref handle, typeof(string));
        Check("direct-replace", handle.Equals(typeof(string).TypeHandle));
        InitializeDirect(ref handle, null);
        Check("direct-clear", handle.Equals(default(RuntimeTypeHandle)));
        InitializePointer(ref handle, typeof(string));
        Check("pointer-replace", handle.Equals(typeof(string).TypeHandle));
        InitializePointer(ref handle, null);
        Check("pointer-clear", handle.Equals(default(RuntimeTypeHandle)));
        Console.WriteLine("Runtime type handle constructor artifact end");
    }
}
