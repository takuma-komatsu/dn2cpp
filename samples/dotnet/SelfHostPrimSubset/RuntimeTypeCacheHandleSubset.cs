using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RuntimeTypeCacheHandleSubset;

internal static class Program
{
    private enum Value : byte { None, First, Second }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("type-cache-handle-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type cache handle contract ==");
        MethodInfo getBridge = typeof(RuntimeTypeHandle).GetMethod("GetGCHandle",
            BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(GCHandleType) });
        MethodInfo freeBridge = typeof(RuntimeTypeHandle).GetMethod("FreeGCHandle",
            BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(IntPtr) });
        foreach (MethodInfo bridge in new[] { getBridge, freeBridge })
            Check(bridge.Name + "-shape", bridge.IsAssembly && !bridge.IsStatic && !bridge.IsGenericMethod
                && bridge.ReturnType == typeof(IntPtr) && (bridge.Attributes & MethodAttributes.PinvokeImpl) == 0
                && (bridge.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0);
        byte[] target = { 3, 7 };
        foreach (GCHandleType kind in new[] { GCHandleType.Weak, GCHandleType.WeakTrackResurrection,
            GCHandleType.Normal, GCHandleType.Pinned })
        {
            GCHandle handle = GCHandle.Alloc(null, kind);
            Check("public-null-" + kind, handle.IsAllocated && handle.Target is null);
            handle.Target = target;
            IntPtr token = GCHandle.ToIntPtr(handle);
            IntPtr address = kind == GCHandleType.Pinned ? handle.AddrOfPinnedObject() : IntPtr.Zero;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Check("public-live-" + kind, ReferenceEquals(handle.Target, target)
                && ReferenceEquals(GCHandle.FromIntPtr(token).Target, target)
                && (kind != GCHandleType.Pinned || address != IntPtr.Zero && address == handle.AddrOfPinnedObject()));
            handle.Free();
            Check("public-free-" + kind, !handle.IsAllocated);
        }
        string before = string.Join(",", Enum.GetNames<Value>());
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Check("enum-names", before == "None,First,Second" && string.Join(",", Enum.GetNames(typeof(Value))) == before);
        Check("type-identity", ReferenceEquals(typeof(Value), Type.GetTypeFromHandle(typeof(Value).TypeHandle)));
        GC.KeepAlive(target);
        Console.WriteLine("Runtime type cache handle contract end");
    }

    // Rewritten entry points are native-only; no private allocator is invoked on CLR.
    private static IntPtr GetDirect(RuntimeTypeHandle handle, GCHandleType kind) => throw new InvalidOperationException("fixture missing");
    private static IntPtr GetPointer(RuntimeTypeHandle handle, GCHandleType kind) => throw new InvalidOperationException("fixture missing");
    private static IntPtr FreeDirect(RuntimeTypeHandle handle, IntPtr slot) => throw new InvalidOperationException("fixture missing");
    private static IntPtr FreePointer(RuntimeTypeHandle handle, IntPtr slot) => throw new InvalidOperationException("fixture missing");
    private static IntPtr GetReflection(RuntimeTypeHandle handle, GCHandleType kind) => (IntPtr)typeof(RuntimeTypeHandle).GetMethod("GetGCHandle",
        BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(GCHandleType) }).Invoke(handle, new object[] { kind });
    private static IntPtr FreeReflection(RuntimeTypeHandle handle, IntPtr slot) => (IntPtr)typeof(RuntimeTypeHandle).GetMethod("FreeGCHandle",
        BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(IntPtr) }).Invoke(handle, new object[] { slot });

    private static void Expect(string member, string label, Func<IntPtr> action, bool isDefault)
    {
        try { action(); throw new InvalidOperationException("private cache handle accepted"); }
        catch (Exception error)
        {
            Exception inner = error is TargetInvocationException invocation ? invocation.InnerException : error;
            if (isDefault)
                Check(member + "-" + label, inner is ArgumentNullException argument && argument.ParamName is null
                    && argument.Message == "Invalid handle." && argument.HResult == unchecked((int)0x80004003));
            else
            {
                string message = "System.RuntimeTypeHandle." + member
                    + " requires CoreCLR-owned cache handles. Use public GCHandle, Type and Enum APIs instead.";
                Check(member + "-" + label, inner is PlatformNotSupportedException && inner.Message == message);
            }
        }
    }

    private static void RunRoute(string route, Func<RuntimeTypeHandle, GCHandleType, IntPtr> get,
        Func<RuntimeTypeHandle, IntPtr, IntPtr> free)
    {
        Expect("GetGCHandle", route + "-default", () => get(default, GCHandleType.WeakTrackResurrection), true);
        foreach (GCHandleType kind in new[] { GCHandleType.Weak, GCHandleType.WeakTrackResurrection,
            GCHandleType.Normal, GCHandleType.Pinned })
            Expect("GetGCHandle", route + "-" + kind, () => get(typeof(Value).TypeHandle, kind), false);
        object target = new object();
        GCHandle publicHandle = GCHandle.Alloc(target);
        IntPtr slot = GCHandle.ToIntPtr(publicHandle);
        // A live public slot must not be mistaken for a private cache allocation.
        Expect("FreeGCHandle", route + "-default", () => free(default, slot), true);
        Expect("FreeGCHandle", route + "-public-slot", () => free(typeof(Value).TypeHandle, slot), false);
        Check(route + "-public-slot-intact", ReferenceEquals(publicHandle.Target, target));
        publicHandle.Free();
    }

    private static void RunBoundary(bool reflectionOnly)
    {
        Run();
        if (!reflectionOnly)
        {
            RunRoute("direct", GetDirect, FreeDirect);
            RunRoute("pointer", GetPointer, FreePointer);
        }
        RunRoute("reflection", GetReflection, FreeReflection);
        Console.WriteLine("Runtime type cache handle boundary end");
    }

    internal static void RunArtifactChecks(string[] args) => RunBoundary(false);
    internal static void RunReflectionChecks(string[] args) => RunBoundary(true);
}
