using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GCHandleCheckUninitializedSubset;

// GCHandle.CheckUninitialized guards every typed-handle token read: a zero token
// faults with NullReferenceException and an allocated token passes, whether the
// guard is reached by name or through the public weak handle.
internal static class Program
{
    private sealed class Payload { }

    private static string Named(IntPtr token)
    {
        try
        {
            MethodInfo method = typeof(GCHandle).GetMethod("CheckUninitialized",
                BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(IntPtr) }, null);
            method.Invoke(null, new object[] { token });
            return "passed";
        }
        catch (TargetInvocationException error)
        {
            return error.InnerException?.GetType().Name ?? "null";
        }
    }

    private static string Public(WeakGCHandle<Payload> handle)
    {
        try
        {
            handle.TryGetTarget(out _);
            return "passed";
        }
        catch (NullReferenceException)
        {
            return "NullReferenceException";
        }
    }

    private static void Show(string label, WeakGCHandle<Payload> handle) =>
        Console.WriteLine(label + ":" + Named(WeakGCHandle<Payload>.ToIntPtr(handle)) + ":" + Public(handle));

    internal static void Run()
    {
        Console.WriteLine("== GCHandle.CheckUninitialized ==");
        var target = new Payload();
        Show("default", default);
        var live = new WeakGCHandle<Payload>(target, false);
        Show("allocated", live);
        live.Dispose();
        Show("freed", live);
        var empty = new WeakGCHandle<Payload>(null, true);
        Show("allocated-null-target", empty);
        empty.Dispose();
        GC.KeepAlive(target);
        Console.WriteLine("GCHandle.CheckUninitialized end");
    }
}
