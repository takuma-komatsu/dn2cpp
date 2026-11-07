// [UnmanagedCallersOnly] shared-library sample (DN2CPP_SHARED), driven by the
// native dlopen host in driver.cpp. Main is a dummy: the host calls the
// generated `main` only to initialize the runtime, then invokes the exports
// below from the main thread and from a foreign thread.
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace UcoShared;

internal static class Program
{
    private static void Main()
    {
    }
}

// Blittable, passed and returned by value across the unmanaged boundary.
internal struct Pair
{
    public int X;
    public int Y;
}

// Internal with private methods on purpose: nothing reaches these through the
// public-surface root rule or a managed call edge, so emitting them at all
// proves the [UnmanagedCallersOnly] rooting.
internal static unsafe class Exports
{
    private delegate int Unary(int value);

    private const int WaitAnyRegistryReadinessPasses = 256;
    private static readonly Unary s_identity = Identity;
    private static readonly ManualResetEvent s_waitAnySignaled = new(true);
    private static readonly ManualResetEvent s_waitAnyBlocked = new(false);
    private static int s_waitAnyRegistryPasses;

    private static int Identity(int value) => value;

    [UnmanagedCallersOnly(EntryPoint = "uco_add")]
    private static int Add(int a, int b) => a + b;

    [UnmanagedCallersOnly(EntryPoint = "uco_pair_swap")]
    private static Pair PairSwap(Pair p) => new Pair { X = p.Y, Y = p.X };

    [UnmanagedCallersOnly(EntryPoint = "uco_secret")]
    private static int Secret() => 12345;

    // Allocates in a loop; the host calls it from a foreign thread to exercise
    // native-callback GC registration.
    [UnmanagedCallersOnly(EntryPoint = "uco_alloc_sum")]
    private static int AllocSum(int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            int[] arr = new int[16];
            arr[3] = i;
            sum += arr[3] % 7;
        }
        return sum;
    }

    // ldftn on a non-exported [UnmanagedCallersOnly] method, handed out as a raw
    // C function pointer the host then calls.
    [UnmanagedCallersOnly(EntryPoint = "uco_get_mul")]
    private static IntPtr GetMul() => (IntPtr)(delegate* unmanaged<int, int, int>)&Mul;

    [UnmanagedCallersOnly]
    private static int Mul(int a, int b) => a * b;

    // Publishing any delegate thunk makes foreign-thread registration permanent:
    // native code may keep the pointer after the host disables its own opt-in.
    [UnmanagedCallersOnly(EntryPoint = "uco_latch_delegate_registration")]
    private static int LatchDelegateRegistration() =>
        Marshal.GetFunctionPointerForDelegate(s_identity) != IntPtr.Zero ? 1 : 0;

    // Leaves the Task.Run worker pool live inside the library on return, so the
    // host can exercise dn2cpp_runtime_quiesce followed by dlclose.
    [UnmanagedCallersOnly(EntryPoint = "uco_task_run_sum")]
    private static int TaskRunSum(int a, int b)
    {
        return System.Threading.Tasks.Task.Run(() => a + b).Result;
    }

    // The host deliberately returns without stopping this program-owned worker.
    // Hosted processes may run static teardown while such background work is
    // still inside the runtime.
    [UnmanagedCallersOnly(EntryPoint = "uco_start_waitany_worker")]
    private static int StartWaitAnyWorker()
    {
        var worker = new Thread(() =>
        {
            WaitHandle[] handles = { s_waitAnySignaled, s_waitAnyBlocked };
            for (;;)
            {
                WaitHandle.WaitAny(handles);
                Interlocked.Increment(ref s_waitAnyRegistryPasses);
            }
        })
        {
            IsBackground = true,
        };
        worker.Start();
        // Readiness means completed registry traffic, and the same loop remains
        // hot after the host observes it and returns.
        while (Volatile.Read(ref s_waitAnyRegistryPasses) < WaitAnyRegistryReadinessPasses)
            Thread.Yield();
        return 1;
    }
}

// Precise-init (an explicit static constructor), so an entry into its export runs the
// initializer, and the startup pass does not. The host enters it first from a thread the
// collector has never seen, and the initializer allocates enough to collect, so it must
// run on the thread the export registered. The export reads no static of its own: only
// the export's guard triggers the initializer.
internal static class LazyExports
{
    static LazyExports()
    {
        InitializerProbe.Runs++;
        int sum = 0;
        for (int i = 0; i < 400000; i++)
        {
            int[] arr = new int[16];
            arr[5] = i;
            sum += arr[5] % 7;
        }
        InitializerProbe.Sum = sum;
    }

    [UnmanagedCallersOnly(EntryPoint = "uco_lazy_answer")]
    private static int Answer() => 77;
}

// No static constructor: reading the probe triggers nothing.
internal static class InitializerProbe
{
    internal static int Runs;
    internal static int Sum;

    [UnmanagedCallersOnly(EntryPoint = "uco_initializer_runs")]
    private static int GetRuns() => Runs;

    [UnmanagedCallersOnly(EntryPoint = "uco_initializer_sum")]
    private static int GetSum() => Sum;
}
