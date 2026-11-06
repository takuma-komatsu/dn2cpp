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

    // These native-only stubs are rewritten with the exact loaded CoreLib signatures.
    private static void GetAttributesManaged() => throw new InvalidOperationException("fixture missing");
    private static void GetAttributesDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetAttributesPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetImplAttributesDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetImplAttributesPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetMethodTableDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetMethodTablePointer() => throw new InvalidOperationException("fixture missing");
    private static void GetSlotDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetSlotPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetMethodDefDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetMethodDefPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetUtf8NameInternalDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetUtf8NameInternalPointer() => throw new InvalidOperationException("fixture missing");
    private static void HasMethodInstantiationDirect() => throw new InvalidOperationException("fixture missing");
    private static void HasMethodInstantiationPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetStubIfNeededInternalDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetStubIfNeededInternalPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetMethodFromCanonicalDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetMethodFromCanonicalPointer() => throw new InvalidOperationException("fixture missing");
    private static void IsGenericMethodDefinitionDirect() => throw new InvalidOperationException("fixture missing");
    private static void IsGenericMethodDefinitionPointer() => throw new InvalidOperationException("fixture missing");
    private static void IsTypicalMethodDefinitionDirect() => throw new InvalidOperationException("fixture missing");
    private static void IsTypicalMethodDefinitionPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetGenericParameterCountDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetGenericParameterCountPointer() => throw new InvalidOperationException("fixture missing");
    private static void IsDynamicMethodDirect() => throw new InvalidOperationException("fixture missing");
    private static void IsDynamicMethodPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetResolverDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetResolverPointer() => throw new InvalidOperationException("fixture missing");
    private static void IsConstructorDirect() => throw new InvalidOperationException("fixture missing");
    private static void IsConstructorPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetLoaderAllocatorInternalDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetLoaderAllocatorInternalPointer() => throw new InvalidOperationException("fixture missing");

    private static void Refuse(string label, Action action)
    {
        try { action(); Console.WriteLine(label + ":accepted"); }
        catch (TargetInvocationException error) { Console.WriteLine(label + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine(label + ":" + error.GetType().Name + ":" + error.Message); }
    }

    internal static void RunArtifactChecks(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Run();
        Refuse("method-handle-managed-overload", GetAttributesManaged);
        Refuse("method-handle-GetAttributes-direct", GetAttributesDirect);
        Refuse("method-handle-GetAttributes-pointer", GetAttributesPointer);
        Refuse("method-handle-GetAttributes-reflection", () => typeof(RuntimeMethodHandle).GetMethod("GetAttributes", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-GetImplAttributes-direct", GetImplAttributesDirect);
        Refuse("method-handle-GetImplAttributes-pointer", GetImplAttributesPointer);
        Refuse("method-handle-GetImplAttributes-reflection", () => typeof(RuntimeMethodHandle).GetMethod("GetImplAttributes", QueryFlags, new Type[] { Type.GetType("System.IRuntimeMethodInfo") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-GetMethodTable-direct", GetMethodTableDirect);
        Refuse("method-handle-GetMethodTable-pointer", GetMethodTablePointer);
        Refuse("method-handle-GetMethodTable-reflection", () => typeof(RuntimeMethodHandle).GetMethod("GetMethodTable", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-GetSlot-direct", GetSlotDirect);
        Refuse("method-handle-GetSlot-pointer", GetSlotPointer);
        Refuse("method-handle-GetSlot-reflection", () => typeof(RuntimeMethodHandle).GetMethod("GetSlot", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-GetMethodDef-direct", GetMethodDefDirect);
        Refuse("method-handle-GetMethodDef-pointer", GetMethodDefPointer);
        Refuse("method-handle-GetMethodDef-reflection", () => typeof(RuntimeMethodHandle).GetMethod("GetMethodDef", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-GetUtf8NameInternal-direct", GetUtf8NameInternalDirect);
        Refuse("method-handle-GetUtf8NameInternal-pointer", GetUtf8NameInternalPointer);
        Refuse("method-handle-GetUtf8NameInternal-reflection", () => typeof(RuntimeMethodHandle).GetMethod("GetUtf8NameInternal", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-HasMethodInstantiation-direct", HasMethodInstantiationDirect);
        Refuse("method-handle-HasMethodInstantiation-pointer", HasMethodInstantiationPointer);
        Refuse("method-handle-HasMethodInstantiation-reflection", () => typeof(RuntimeMethodHandle).GetMethod("HasMethodInstantiation", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-GetStubIfNeededInternal-direct", GetStubIfNeededInternalDirect);
        Refuse("method-handle-GetStubIfNeededInternal-pointer", GetStubIfNeededInternalPointer);
        Refuse("method-handle-GetStubIfNeededInternal-reflection", () => typeof(RuntimeMethodHandle).GetMethod("GetStubIfNeededInternal", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal"), Type.GetType("System.RuntimeType") }).Invoke(null, new object[] { null, null }));
        Refuse("method-handle-GetMethodFromCanonical-direct", GetMethodFromCanonicalDirect);
        Refuse("method-handle-GetMethodFromCanonical-pointer", GetMethodFromCanonicalPointer);
        Refuse("method-handle-GetMethodFromCanonical-reflection", () => typeof(RuntimeMethodHandle).GetMethod("GetMethodFromCanonical", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal"), Type.GetType("System.RuntimeType") }).Invoke(null, new object[] { null, null }));
        Refuse("method-handle-IsGenericMethodDefinition-direct", IsGenericMethodDefinitionDirect);
        Refuse("method-handle-IsGenericMethodDefinition-pointer", IsGenericMethodDefinitionPointer);
        Refuse("method-handle-IsGenericMethodDefinition-reflection", () => typeof(RuntimeMethodHandle).GetMethod("IsGenericMethodDefinition", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-IsTypicalMethodDefinition-direct", IsTypicalMethodDefinitionDirect);
        Refuse("method-handle-IsTypicalMethodDefinition-pointer", IsTypicalMethodDefinitionPointer);
        Refuse("method-handle-IsTypicalMethodDefinition-reflection", () => typeof(RuntimeMethodHandle).GetMethod("IsTypicalMethodDefinition", QueryFlags, new Type[] { Type.GetType("System.IRuntimeMethodInfo") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-GetGenericParameterCount-direct", GetGenericParameterCountDirect);
        Refuse("method-handle-GetGenericParameterCount-pointer", GetGenericParameterCountPointer);
        Refuse("method-handle-GetGenericParameterCount-reflection", () => typeof(RuntimeMethodHandle).GetMethod("GetGenericParameterCount", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-IsDynamicMethod-direct", IsDynamicMethodDirect);
        Refuse("method-handle-IsDynamicMethod-pointer", IsDynamicMethodPointer);
        Refuse("method-handle-IsDynamicMethod-reflection", () => typeof(RuntimeMethodHandle).GetMethod("IsDynamicMethod", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-GetResolver-direct", GetResolverDirect);
        Refuse("method-handle-GetResolver-pointer", GetResolverPointer);
        Refuse("method-handle-GetResolver-reflection", () => typeof(RuntimeMethodHandle).GetMethod("GetResolver", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-IsConstructor-direct", IsConstructorDirect);
        Refuse("method-handle-IsConstructor-pointer", IsConstructorPointer);
        Refuse("method-handle-IsConstructor-reflection", () => typeof(RuntimeMethodHandle).GetMethod("IsConstructor", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Refuse("method-handle-GetLoaderAllocatorInternal-direct", GetLoaderAllocatorInternalDirect);
        Refuse("method-handle-GetLoaderAllocatorInternal-pointer", GetLoaderAllocatorInternalPointer);
        Refuse("method-handle-GetLoaderAllocatorInternal-reflection", () => typeof(RuntimeMethodHandle).GetMethod("GetLoaderAllocatorInternal", QueryFlags, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }).Invoke(null, new object[] { null }));
        Console.WriteLine("Runtime method handle boundary end");
    }
}
