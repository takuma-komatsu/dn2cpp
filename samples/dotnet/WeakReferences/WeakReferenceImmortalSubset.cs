using System;
using System.Runtime.InteropServices;

namespace WeakReferenceImmortalSubset;

internal static class Program
{
    internal static void __GateEntry()
    {
        object type = typeof(Program);
        object literal = "weak immortal literal";
        foreach (bool trackResurrection in new[] { false, true })
        {
            var weak = new WeakReference(type, trackResurrection);
            var generic = new WeakReference<object>(literal, trackResurrection);
            GCHandle handle = GCHandle.Alloc(type, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
            try
            {
                Collect();
                Require(ReferenceEquals(weak.Target, type), "type weak target");
                Require(generic.TryGetTarget(out object? target) && ReferenceEquals(target, literal), "literal generic target");
                Require(ReferenceEquals(handle.Target, type), "type handle target");
                object heap = new object();
                weak.Target = heap;
                generic.SetTarget(heap);
                handle.Target = heap;
                Collect();
                Require(ReferenceEquals(weak.Target, heap), "heap weak target");
                Require(generic.TryGetTarget(out target) && ReferenceEquals(target, heap), "heap generic target");
                Require(ReferenceEquals(handle.Target, heap), "heap handle target");
                weak.Target = literal;
                generic.SetTarget(type);
                handle.Target = literal;
                Collect();
                Require(ReferenceEquals(weak.Target, literal), "literal weak target");
                Require(generic.TryGetTarget(out target) && ReferenceEquals(target, type), "type generic target");
                Require(ReferenceEquals(handle.Target, literal), "literal handle target");
                weak.Target = null;
                generic.SetTarget(null!);
                handle.Target = null;
                Collect();
                Require(weak.Target is null && !generic.TryGetTarget(out _) && handle.Target is null, "cleared weak targets");
                GC.KeepAlive(heap);
            }
            finally
            {
                handle.Free();
            }
        }
        Console.WriteLine("immortal weak targets: True");
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
