using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeMethodHandleSubset;

internal static class Program
{
    public sealed class Target
    {
        public Target() { }
        public int Read() => 43;
        public static T Identity<T>(T value) => value;
    }

    internal static void Run()
    {
        Console.WriteLine("== Runtime method handle contract ==");
        RuntimeMethodHandle handle = default;
        RuntimeMethodHandle other = default;
        Console.WriteLine("method-handle-default-value:" + (handle.Value == IntPtr.Zero && RuntimeMethodHandle.ToIntPtr(handle) == IntPtr.Zero));
        Console.WriteLine("method-handle-default-equality:" + (handle.Equals(other) && handle.Equals((object)other) && handle == other && !(handle != other) && !handle.Equals(null) && !handle.Equals(new object())));
        Console.WriteLine("method-handle-default-hash:" + (handle.GetHashCode() == HashCode.Combine(IntPtr.Zero)));
        try { handle.GetFunctionPointer(); Console.WriteLine("method-handle-default-validation:accepted"); }
        catch (ArgumentNullException) { Console.WriteLine("method-handle-default-validation:ArgumentNullException"); }
        MethodInfo method = typeof(Target).GetMethod("Read");
        ConstructorInfo constructor = typeof(Target).GetConstructor(Type.EmptyTypes);
        MethodInfo generic = typeof(Target).GetMethod("Identity");
        Console.WriteLine("method-handle-public-attributes:" + (method.IsPublic && !method.IsStatic && method.Attributes == (MethodAttributes.Public | MethodAttributes.HideBySig)));
        Console.WriteLine("method-handle-public-constructor:" + (constructor.IsConstructor && constructor.IsPublic && !constructor.IsGenericMethod));
        Console.WriteLine("method-handle-public-generic:" + (generic.IsGenericMethod && generic.IsGenericMethodDefinition && generic.GetGenericArguments().Length == 1));
        Console.WriteLine("method-handle-public-invoke:" + ((int)method.Invoke(new Target(), null) == 43));
        RunMetadataChecks();
        Console.WriteLine("Runtime method handle contract end");
    }

    private const BindingFlags QueryFlags = BindingFlags.NonPublic | BindingFlags.Static;

    private static void CheckShape(string name, MethodInfo method, string result, bool isPrivate)
    {
        Console.WriteLine("method-handle-shape-" + name + ":" + (method.IsStatic && !method.IsGenericMethod
            && (isPrivate ? method.IsPrivate : method.IsAssembly) && method.ReturnType.FullName == result));
    }

    internal static void RunMetadataChecks()
    {
        CheckShape("GetAttributes", typeof(RuntimeMethodHandle).GetMethod("GetAttributes", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), "System.Reflection.MethodAttributes", false);
        CheckShape("GetImplAttributes", typeof(RuntimeMethodHandle).GetMethod("GetImplAttributes", QueryFlags, new Type[] { Type.GetType("System.IRuntimeMethodInfo") }), "System.Reflection.MethodImplAttributes", false);
        CheckShape("GetMethodTable", typeof(RuntimeMethodHandle).GetMethod("GetMethodTable", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), "System.Runtime.CompilerServices.MethodTable*", true);
        CheckShape("GetSlot", typeof(RuntimeMethodHandle).GetMethod("GetSlot", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), "System.Int32", false);
        CheckShape("GetMethodDef", typeof(RuntimeMethodHandle).GetMethod("GetMethodDef", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), "System.Int32", false);
        CheckShape("GetUtf8NameInternal", typeof(RuntimeMethodHandle).GetMethod("GetUtf8NameInternal", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), "System.Void*", true);
        CheckShape("HasMethodInstantiation", typeof(RuntimeMethodHandle).GetMethod("HasMethodInstantiation", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), "System.Boolean", false);
        CheckShape("GetStubIfNeededInternal", typeof(RuntimeMethodHandle).GetMethod("GetStubIfNeededInternal", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal"), Type.GetType("System.RuntimeType") }), "System.RuntimeMethodHandleInternal", true);
        CheckShape("GetMethodFromCanonical", typeof(RuntimeMethodHandle).GetMethod("GetMethodFromCanonical", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal"), Type.GetType("System.RuntimeType") }), "System.RuntimeMethodHandleInternal", false);
        CheckShape("IsGenericMethodDefinition", typeof(RuntimeMethodHandle).GetMethod("IsGenericMethodDefinition", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), "System.Boolean", false);
        CheckShape("IsTypicalMethodDefinition", typeof(RuntimeMethodHandle).GetMethod("IsTypicalMethodDefinition", QueryFlags, new Type[] { Type.GetType("System.IRuntimeMethodInfo") }), "System.Boolean", true);
        CheckShape("GetGenericParameterCount", typeof(RuntimeMethodHandle).GetMethod("GetGenericParameterCount", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), "System.Int32", true);
        CheckShape("IsDynamicMethod", typeof(RuntimeMethodHandle).GetMethod("IsDynamicMethod", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), "System.Boolean", false);
        CheckShape("GetResolver", typeof(RuntimeMethodHandle).GetMethod("GetResolver", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), Type.GetType("System.Resolver").FullName, false);
        CheckShape("IsConstructor", typeof(RuntimeMethodHandle).GetMethod("IsConstructor", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), "System.Boolean", false);
        CheckShape("GetLoaderAllocatorInternal", typeof(RuntimeMethodHandle).GetMethod("GetLoaderAllocatorInternal", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }), Type.GetType("System.Reflection.LoaderAllocator").FullName, true);
        Console.WriteLine("Runtime method handle metadata end");
    }
}
