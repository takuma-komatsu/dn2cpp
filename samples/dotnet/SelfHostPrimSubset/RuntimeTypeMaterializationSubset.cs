using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeMaterializationSubset;

internal static class Program
{
    public class Parent { }
    public sealed class Target : Parent
    {
        public int Read() => 47;
    }

    private const BindingFlags StaticFlags = BindingFlags.NonPublic | BindingFlags.Static;

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type materialization contract ==");
        Type target = typeof(Target);
        RuntimeTypeHandle handle = target.TypeHandle;
        Console.WriteLine("type-materialize-public-roundtrip:" + (Type.GetTypeFromHandle(handle) == target));
        Console.WriteLine("type-materialize-public-array-roundtrip:" + (Type.GetTypeFromHandle(typeof(Target[]).TypeHandle) == typeof(Target[])));
        Console.WriteLine("type-materialize-public-default:" + (Type.GetTypeFromHandle(default(RuntimeTypeHandle)) == null));
        Console.WriteLine("type-materialize-public-value:" + (handle.Value != IntPtr.Zero));
        Console.WriteLine("type-materialize-public-object:" + (new Target().GetType() == target));
        Console.WriteLine("type-materialize-public-base:" + (target.BaseType == typeof(Parent)));
        Console.WriteLine("type-materialize-public-invoke:" + ((int)target.GetMethod("Read").Invoke(new Target(), null) == 47));
        RunMetadataChecks();
        // The inspected managed wrapper returns before touching VM state for zero.
        Console.WriteLine("type-materialize-maybe-null-zero:" + (typeof(RuntimeTypeHandle).GetMethod("GetRuntimeTypeFromHandleMaybeNull", StaticFlags, new Type[] { typeof(IntPtr) }).Invoke(null, new object[] { IntPtr.Zero }) == null));
        Console.WriteLine("Runtime type materialization contract end");
    }

    private static MethodInfo MaybeNullMethod() => typeof(RuntimeTypeHandle).GetMethod("GetRuntimeTypeFromHandleMaybeNull", StaticFlags, new Type[] { typeof(IntPtr) });

    private static void CheckShape(string label, MethodInfo method, bool isPrivate, string result)
    {
        Console.WriteLine("type-materialize-shape-" + label + ":" + (method.IsStatic && !method.IsGenericMethod
            && (isPrivate ? method.IsPrivate : method.IsAssembly) && method.ReturnType.FullName == result));
    }

    internal static void RunMetadataChecks()
    {
        Type owner = typeof(RuntimeTypeHandle);
        MethodInfo methodTable = owner.GetMethod("GetRuntimeType", StaticFlags);
        CheckShape("method-table", methodTable, false, "System.RuntimeType");
        Console.WriteLine("type-materialize-shape-method-table-parameter:" + (methodTable.GetParameters().Length == 1 && methodTable.GetParameters()[0].ParameterType.FullName == "System.Runtime.CompilerServices.MethodTable*"));
        CheckShape("handle", owner.GetMethod("GetRuntimeTypeFromHandle", StaticFlags, new Type[] { typeof(IntPtr) }), false, "System.RuntimeType");
        CheckShape("slow", owner.GetMethod("GetRuntimeTypeFromHandleSlow", StaticFlags, new Type[] { typeof(IntPtr) }), true, "System.RuntimeType");
        CheckShape("qcall", owner.GetMethod("GetRuntimeTypeFromHandleSlow", StaticFlags, new Type[] { typeof(IntPtr), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack") }), true, "System.Void");
        CheckShape("maybe-null", MaybeNullMethod(), false, "System.RuntimeType");
        Console.WriteLine("Runtime type materialization metadata end");
    }

    // Only the native artifact replaces these stubs with exact CoreLib call signatures.
    private static void MethodTableDirect(int value) => throw new InvalidOperationException("fixture missing");
    private static void MethodTablePointer(int value) => throw new InvalidOperationException("fixture missing");
    private static void HandleDirect(int value) => throw new InvalidOperationException("fixture missing");
    private static void HandlePointer(int value) => throw new InvalidOperationException("fixture missing");
    private static void SlowDirect(int value) => throw new InvalidOperationException("fixture missing");
    private static void SlowPointer(int value) => throw new InvalidOperationException("fixture missing");
    private static void QCallDirect(int value) => throw new InvalidOperationException("fixture missing");
    private static void QCallPointer(int value) => throw new InvalidOperationException("fixture missing");
    private static bool MaybeNullDirect(int value) => throw new InvalidOperationException("fixture missing");
    private static bool MaybeNullPointer(int value) => throw new InvalidOperationException("fixture missing");

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
        Refuse("type-materialize-method-table-direct-0", () => MethodTableDirect(0));
        Refuse("type-materialize-method-table-direct-1", () => MethodTableDirect(1));
        Refuse("type-materialize-method-table-pointer-0", () => MethodTablePointer(0));
        Refuse("type-materialize-method-table-pointer-1", () => MethodTablePointer(1));
        Refuse("type-materialize-handle-direct-0", () => HandleDirect(0));
        Refuse("type-materialize-handle-direct-1", () => HandleDirect(1));
        Refuse("type-materialize-handle-pointer-0", () => HandlePointer(0));
        Refuse("type-materialize-handle-pointer-1", () => HandlePointer(1));
        Refuse("type-materialize-slow-direct-0", () => SlowDirect(0));
        Refuse("type-materialize-slow-direct-1", () => SlowDirect(1));
        Refuse("type-materialize-slow-pointer-0", () => SlowPointer(0));
        Refuse("type-materialize-slow-pointer-1", () => SlowPointer(1));
        Refuse("type-materialize-qcall-direct-0", () => QCallDirect(0));
        Refuse("type-materialize-qcall-direct-1", () => QCallDirect(1));
        Refuse("type-materialize-qcall-pointer-0", () => QCallPointer(0));
        Refuse("type-materialize-qcall-pointer-1", () => QCallPointer(1));
        Console.WriteLine("type-materialize-maybe-null-direct-0:" + MaybeNullDirect(0));
        Refuse("type-materialize-maybe-null-direct-1", () => MaybeNullDirect(1));
        Console.WriteLine("type-materialize-maybe-null-pointer-0:" + MaybeNullPointer(0));
        Refuse("type-materialize-maybe-null-pointer-1", () => MaybeNullPointer(1));
        Refuse("type-materialize-handle-reflection", () => typeof(RuntimeTypeHandle).GetMethod("GetRuntimeTypeFromHandle", StaticFlags, new Type[] { typeof(IntPtr) }).Invoke(null, new object[] { new IntPtr(1) }));
        Refuse("type-materialize-slow-reflection", () => typeof(RuntimeTypeHandle).GetMethod("GetRuntimeTypeFromHandleSlow", StaticFlags, new Type[] { typeof(IntPtr) }).Invoke(null, new object[] { new IntPtr(1) }));
        Refuse("type-materialize-qcall-reflection", () => typeof(RuntimeTypeHandle).GetMethod("GetRuntimeTypeFromHandleSlow", StaticFlags, new Type[] { typeof(IntPtr), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack") }).Invoke(null, new object[] { new IntPtr(1), null }));
        Refuse("type-materialize-maybe-null-reflection", () => typeof(RuntimeTypeHandle).GetMethod("GetRuntimeTypeFromHandleMaybeNull", StaticFlags, new Type[] { typeof(IntPtr) }).Invoke(null, new object[] { new IntPtr(1) }));
        Console.WriteLine("Runtime type materialization boundary end");
    }
}
