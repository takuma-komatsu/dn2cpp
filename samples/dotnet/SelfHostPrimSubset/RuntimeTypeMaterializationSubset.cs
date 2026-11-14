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
}
