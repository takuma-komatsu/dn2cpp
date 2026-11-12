using System;
using System.Reflection;

namespace GcFinalizerWorkerSubset;

internal static class Program
{
    private static MethodInfo Worker() => typeof(GC).GetMethod("RunFinalizers",
        BindingFlags.Static | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

    // CoreCLR owns this worker; executing it is a native-only refusal contract.
    internal static void RunNativeRefusal()
    {
        try
        {
            typeof(GC).GetMethod("RunFinalizers", BindingFlags.Static | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null).Invoke(null, null);
        }
        catch (TargetInvocationException error)
        {
            if (error.InnerException is PlatformNotSupportedException unsupported)
            {
                Console.WriteLine("gc-worker-refused:" + unsupported.Message);
                Console.WriteLine("GC finalizer worker refusal end");
                return;
            }
            throw;
        }
        throw new InvalidOperationException("GC.RunFinalizers did not refuse");
    }

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
}
