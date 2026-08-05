#nullable enable
using System;
using System.Runtime.InteropServices;

// A delegate with a blittable Invoke signature marshals as a native function pointer the
// native invokes synchronously during the call — the qsort/apply/reduce idiom. dn2cpp
// recovers the delegate through a per-delegate-type thread-local slot, so a capturing
// lambda dispatches as correctly as a static-method delegate. Contrast MarshalFnPtrSubset,
// where the pointer outlives the call.
namespace PInvokeCallbackSubset;

internal static class Program
{
    private delegate int Transform(int x);
    private delegate int Combine(int a, int b);
    private delegate double TransformD(double x);

    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_apply_sum(int[] a, int n, Transform fn);
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_reduce(int[] a, int n, Combine fn, int seed);
    [DllImport("dn2cpptest")]
    private static extern double dn2cpptest_apply_sumd(double[] a, int n, TransformD fn);

    private static int Doubler(int x) => x * 2;

    internal static void __GateEntry()
    {
        int[] a = { 1, 2, 3, 4 };

        // A static-method delegate.
        Console.WriteLine(dn2cpptest_apply_sum(a, a.Length, Doubler));

        // A capturing lambda: the slot must recover this instance's captured k.
        int k = 10;
        Transform addK = x => x + k;
        Console.WriteLine(dn2cpptest_apply_sum(a, a.Length, addK));

        // A second instance of the SAME delegate type in a consecutive call.
        int k2 = 100;
        Transform addK2 = x => x + k2;
        Console.WriteLine(dn2cpptest_apply_sum(a, a.Length, addK2));

        // A two-argument callback, with a zero and a non-zero seed.
        Combine sum = (x, y) => x + y;
        Console.WriteLine(dn2cpptest_reduce(a, a.Length, sum, 0));

        Combine max = (x, y) => x > y ? x : y;
        Console.WriteLine(dn2cpptest_reduce(a, a.Length, max, 7));

        // The double native ABI.
        double[] d = { 1.0, 2.0, 3.0, 4.0 };
        TransformD scale = x => x * 1.5;
        Console.WriteLine(dn2cpptest_apply_sumd(d, d.Length, scale));

        double bias = 0.25;
        TransformD addBias = x => x + bias;
        Console.WriteLine(dn2cpptest_apply_sumd(d, d.Length, addBias));
    }
}
