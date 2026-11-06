using System;
using System.Reflection;

namespace GcFinalizerWorkerSubset;

internal static class Program
{
    // The native-only fixture rewrites the three call/address sites to the actual
    // CoreLib private worker. The CLR oracle never executes that worker.
    private static uint WorkerEntry() => throw new InvalidOperationException("worker fixture patch missing");
    private static MethodInfo Worker() => typeof(GC).GetMethod("RunFinalizers",
        BindingFlags.Static | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

    internal static void Run()
    {
        MethodInfo worker = Worker();
        Console.WriteLine("== GC finalizer worker contract ==");
        Console.WriteLine("gc-worker-private:" + worker.IsPrivate);
        Console.WriteLine("gc-worker-static:" + worker.IsStatic);
        Console.WriteLine("gc-worker-nongeneric:" + !worker.IsGenericMethod);
        Console.WriteLine("gc-worker-uint32:" + (worker.ReturnType == typeof(uint)));
        Console.WriteLine("gc-worker-zero:" + (worker.GetParameters().Length == 0));
        Console.WriteLine("GC finalizer worker contract end");
    }
    private static void Refuse(string label, Action action)
    {
        try { action(); Console.WriteLine(label + ":accepted"); }
        catch (Exception error) { Console.WriteLine(label + ":" + error.GetType().Name + ":" + error.Message); }
    }
    internal static unsafe void RunNativeBoundary()
    {
        Refuse("worker-direct", () => { WorkerEntry(); });
        Func<uint> group = WorkerEntry;
        Refuse("worker-delegate", () => { group(); });
        delegate* managed<uint> pointer = &WorkerEntry;
        try { pointer(); Console.WriteLine("worker-pointer:accepted"); }
        catch (Exception error) { Console.WriteLine("worker-pointer:" + error.GetType().Name + ":" + error.Message); }
        MethodInfo worker = Worker();
        Refuse("worker-reflected-delegate", () => { ((Func<uint>)worker.CreateDelegate(typeof(Func<uint>)))(); });
        Refuse("worker-reflection", () =>
        {
            try { worker.Invoke(null, Array.Empty<object>()); }
            catch (TargetInvocationException error) { throw error.InnerException; }
        });
        Console.WriteLine("GC finalizer worker boundary end");
    }
}
