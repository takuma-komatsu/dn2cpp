// [UnmanagedCallersOnly] shared-library sample (DN2CPP_SHARED), driven by the
// native dlopen host in driver.cpp. Main is a dummy: the host calls the
// generated `main` only to initialize the runtime, then invokes the exports
// below from the main thread and from a foreign thread.
using System;
using System.Runtime.InteropServices;

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

    private static readonly Unary s_identity = Identity;

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
}
