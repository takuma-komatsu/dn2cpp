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
}
