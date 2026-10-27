using System;
using System.Globalization;
using System.Reflection;

namespace CompoundTypeHandleSubset;

internal static class Program
{
    private class CompoundHandleOwner<T> { public static void Method<U>() { } }
    private class CompoundHandleSignatures { public static ref int ByRef(ref int value) => ref value; }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("compound-handle-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool InvalidHandle(Exception error) => error is ArgumentNullException argument
        && argument.ParamName is null && error.Message == "Invalid handle."
        && error.HResult == unchecked((int)0x80004003);
    private static bool NullReceiver(Exception error) => error is NullReferenceException
        && error.HResult == unchecked((int)0x80004003);
    private static bool InvalidElement(Exception error) => error is TypeLoadException
        && error.HResult == unchecked((int)0x80131522);
    private static Type PublicPointer(Type type) => type.MakePointerType();
    private static Type PublicByRef(Type type) => type.MakeByRefType();
    private static Type RetainedPointer(Type type) => HandleOracle(type is null ? default : type.TypeHandle, true);
    private static Type RetainedByRef(Type type) => HandleOracle(type is null ? default : type.TypeHandle, false);
    private static Type VirtualPointer(Type type) => PublicPointer(type);
    private static Type VirtualByRef(Type type) => PublicByRef(type);
    private static Type ConstrainedPointer(Type type) => PublicPointer(type);
    private static Type ConstrainedByRef(Type type) => PublicByRef(type);

    private static Func<Type> PublicAddressPointer(Type type) => () => RetainedPointer(type);
    private static Func<Type> PublicAddressByRef(Type type) => () => RetainedByRef(type);
    private static Func<Type> VirtualAddressPointer(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => RetainedPointer(type);
    }
    private static Func<Type> VirtualAddressByRef(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => RetainedByRef(type);
    }
    private static Func<Type> FlowAddressPointer(Type type, bool virtualAddress) =>
        virtualAddress ? VirtualAddressPointer(type) : PublicAddressPointer(type);
    private static Func<Type> FlowAddressByRef(Type type, bool virtualAddress) =>
        virtualAddress ? VirtualAddressByRef(type) : PublicAddressByRef(type);

    private static unsafe Type[] Elements() => new[] { typeof(int), typeof(string), typeof(void), typeof(int[]),
        Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 1 }).GetType(), typeof(int[,]), typeof(int*), typeof(CompoundHandleOwner<>),
        typeof(CompoundHandleOwner<string>), typeof(CompoundHandleOwner<>).GetGenericArguments()[0],
        typeof(CompoundHandleOwner<string>).GetMethod("Method").GetGenericArguments()[0], typeof(delegate*<int, int>) };

    private static unsafe void Route(string route, Func<Type, Type> query, bool pointer)
    {
        Type[] elements = Elements();
        for (int i = 0; i < elements.Length; i++)
        {
            Type element = elements[i];
            Type result = query(element);
            Type expected = pointer ? element.MakePointerType() : element.MakeByRefType();
            Check(route + "-shape-" + i, ReferenceEquals(result, expected)
                && ReferenceEquals(result.GetElementType(), element) && result.IsPointer == pointer && result.IsByRef != pointer
                && result.ContainsGenericParameters == element.ContainsGenericParameters
                && ReferenceEquals(Type.GetTypeFromHandle(result.TypeHandle), result)
                && ReferenceEquals(query(element), result));
        }
        Type signature = pointer ? typeof(int*) : typeof(CompoundHandleSignatures).GetMethod("ByRef").ReturnType;
        Check(route + "-signature", ReferenceEquals(query(typeof(int)), signature));
        Type stable = query(elements[9]);
        bool same = true;
        for (int i = 0; i < 16; i++) same &= ReferenceEquals(stable, query(elements[9]));
        GC.Collect();
        Check(route + "-repeated", same && ReferenceEquals(stable, query(elements[9])));
        Check(route + "-byref-rejected", InvalidElement(Error(() => query(typeof(int).MakeByRefType()))));
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Compound type handle contract ==");
        Route("public-pointer", PublicPointer, true);
        Route("public-byref", PublicByRef, false);
        Check("public-pointer-null", NullReceiver(Error(() => PublicPointer(null))));
        Check("public-byref-null", NullReceiver(Error(() => PublicByRef(null))));
        Check("public-distinct", !ReferenceEquals(PublicPointer(typeof(int)), PublicByRef(typeof(int))));
        Console.WriteLine("Compound type handle contract end");
    }

    // The oracle uses public factories; default validation follows the managed wrapper IL.
    private static Type HandleOracle(RuntimeTypeHandle handle, bool pointer)
    {
        Type type = Type.GetTypeFromHandle(handle);
        if (type is null) throw new ArgumentNullException(null, "Invalid handle.");
        return pointer ? type.MakePointerType() : type.MakeByRefType();
    }
    private static Type PointerDirect(RuntimeTypeHandle handle) => HandleOracle(handle, true);
    private static Type PointerCalli(RuntimeTypeHandle handle) => HandleOracle(handle, true);
    private static Func<Type> PointerAddress(RuntimeTypeHandle handle) => () => HandleOracle(handle, true);
    private static Type PointerDelegate(RuntimeTypeHandle handle) => PointerAddress(handle)();
    private static Type PointerReflection(RuntimeTypeHandle handle) => HandleOracle(handle, true);
    private static Type ByRefDirect(RuntimeTypeHandle handle) => HandleOracle(handle, false);
    private static Type ByRefCalli(RuntimeTypeHandle handle) => HandleOracle(handle, false);
    private static Func<Type> ByRefAddress(RuntimeTypeHandle handle) => () => HandleOracle(handle, false);
    private static Type ByRefDelegate(RuntimeTypeHandle handle) => ByRefAddress(handle)();
    private static Type ByRefReflection(RuntimeTypeHandle handle) => HandleOracle(handle, false);

    private static void HandleRoute(string route, Func<RuntimeTypeHandle, Type> query, bool pointer)
    {
        Route(route, type => query(type.TypeHandle), pointer);
        Check(route + "-default", InvalidHandle(Error(() => query(default))));
    }

    internal static void RunHandleArtifact(string[] args)
    {
        Run();
        Route("retained-pointer", RetainedPointer, true);
        Route("retained-byref", RetainedByRef, false);
        Route("virtual-pointer", VirtualPointer, true);
        Route("virtual-byref", VirtualByRef, false);
        Route("constrained-pointer", ConstrainedPointer, true);
        Route("constrained-byref", ConstrainedByRef, false);
        Route("public-address-pointer", type => PublicAddressPointer(type)(), true);
        Route("public-address-byref", type => PublicAddressByRef(type)(), false);
        Route("virtual-address-pointer", type => VirtualAddressPointer(type)(), true);
        Route("virtual-address-byref", type => VirtualAddressByRef(type)(), false);
        Route("flow-direct-pointer", type => FlowAddressPointer(type, false)(), true);
        Route("flow-direct-byref", type => FlowAddressByRef(type, false)(), false);
        Route("flow-virtual-pointer", type => FlowAddressPointer(type, true)(), true);
        Route("flow-virtual-byref", type => FlowAddressByRef(type, true)(), false);
        Check("virtual-address-pointer-null", NullReceiver(Error(() => VirtualAddressPointer(null))));
        Check("virtual-address-byref-null", NullReceiver(Error(() => VirtualAddressByRef(null))));
        Check("flow-virtual-pointer-null", NullReceiver(Error(() => FlowAddressPointer(null, true))));
        Check("flow-virtual-byref-null", NullReceiver(Error(() => FlowAddressByRef(null, true))));
        Check("retained-pointer-null", InvalidHandle(Error(() => RetainedPointer(null))));
        Check("retained-byref-null", InvalidHandle(Error(() => RetainedByRef(null))));
        Check("virtual-pointer-null", NullReceiver(Error(() => VirtualPointer(null))));
        Check("virtual-byref-null", NullReceiver(Error(() => VirtualByRef(null))));
        Check("constrained-pointer-null", NullReceiver(Error(() => ConstrainedPointer(null))));
        Check("constrained-byref-null", NullReceiver(Error(() => ConstrainedByRef(null))));
        HandleRoute("direct-pointer", PointerDirect, true);
        HandleRoute("direct-byref", ByRefDirect, false);
        HandleRoute("calli-pointer", PointerCalli, true);
        HandleRoute("calli-byref", ByRefCalli, false);
        HandleRoute("delegate-pointer", PointerDelegate, true);
        HandleRoute("delegate-byref", ByRefDelegate, false);
        RunReflectionArtifact(args);
        if (args.Length > 0 && args[0] == "native-boundary")
        {
            foreach (bool pointer in new[] { true, false })
            {
                MethodInfo method = (pointer ? PointerAddress(typeof(int).TypeHandle) : ByRefAddress(typeof(int).TypeHandle)).Method;
                Check("delegate-identity-" + (pointer ? "pointer" : "byref"), method.Name == (pointer ? "MakePointer" : "MakeByRef")
                    && method.DeclaringType == typeof(RuntimeTypeHandle) && method.ReturnType == ((object)typeof(int)).GetType()
                    && !method.IsStatic && !method.IsPublic && method.GetParameters().Length == 0);
                Func<Type>[] publicBindings = pointer
                    ? new[] { PublicAddressPointer(typeof(int)), VirtualAddressPointer(typeof(int)), FlowAddressPointer(typeof(int), false), FlowAddressPointer(typeof(int), true) }
                    : new[] { PublicAddressByRef(typeof(int)), VirtualAddressByRef(typeof(int)), FlowAddressByRef(typeof(int), false), FlowAddressByRef(typeof(int), true) };
                for (int i = 0; i < publicBindings.Length; i++)
                {
                    MethodInfo target = publicBindings[i].Method;
                    Check("public-identity-" + (pointer ? "pointer" : "byref") + "-" + i,
                        target.Name == (pointer ? "MakePointerType" : "MakeByRefType")
                        && target.DeclaringType == ((object)typeof(int)).GetType() && target.ReturnType == typeof(Type)
                        && target.IsPublic && target.IsVirtual && target.GetParameters().Length == 0
                        && ReferenceEquals(target.Invoke(typeof(int), null), publicBindings[i]()));
                }
            }
        }
        Console.WriteLine("Compound type handle routes end");
    }

    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        HandleRoute("reflection-pointer", PointerReflection, true);
        HandleRoute("reflection-byref", ByRefReflection, false);
        Console.WriteLine("Compound type handle reflection end");
    }
}
