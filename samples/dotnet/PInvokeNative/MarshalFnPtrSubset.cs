#nullable enable
using System;
using System.Runtime.InteropServices;

// Explicit delegate <-> raw function pointer conversion. Unlike a delegate parameter, the
// pointer has no bounded lifetime, so dn2cpp parks the delegate in a statically-rooted
// per-delegate-type slot pool and hands out that slot's thunk address. The .NET identity
// guarantees hold: one delegate instance always yields one pointer, and a thunk pointer
// round-trips back to the original instance. Pointer values are never printed, only
// compared, so the output is deterministic on both sides.
namespace MarshalFnPtrSubset;

internal static class Program
{
    private delegate int Transform(int x);
    private delegate int Combine(int a, int b);

    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_apply_sum_fp(int[] a, int n, IntPtr fn);
    [DllImport("dn2cpptest")]
    private static extern IntPtr dn2cpptest_fnptr_add();

    private static int Tripler(int x) => x * 3;

    internal static void __GateEntry()
    {
        int[] a = { 1, 2, 3, 4 };

        // A static-method delegate -> function pointer.
        Transform triple = Tripler;
        IntPtr pTriple = Marshal.GetFunctionPointerForDelegate<Transform>(triple);
        Console.WriteLine(dn2cpptest_apply_sum_fp(a, a.Length, pTriple));

        // A capturing lambda: the pool slot must recover this instance's captured k.
        int k = 7;
        Transform addK = x => x + k;
        IntPtr pAddK = Marshal.GetFunctionPointerForDelegate<Transform>(addK);
        Console.WriteLine(dn2cpptest_apply_sum_fp(a, a.Length, pAddK));

        // One instance -> one pointer; distinct instances -> distinct pointers.
        IntPtr pTriple2 = Marshal.GetFunctionPointerForDelegate<Transform>(triple);
        Console.WriteLine(pTriple == pTriple2);

        Console.WriteLine(pTriple == pAddK);

        // A raw native pointer rehydrates as an invokable managed delegate.
        IntPtr pAdd = dn2cpptest_fnptr_add();
        Combine add = Marshal.GetDelegateForFunctionPointer<Combine>(pAdd);
        Console.WriteLine(add(19, 23));

        // delegate -> pointer -> delegate recovers the ORIGINAL instance.
        Transform back = Marshal.GetDelegateForFunctionPointer<Transform>(pTriple);
        Console.WriteLine(ReferenceEquals(back, triple));

        // GetFunctionPointerForDelegate's contract: the delegate must stay reachable for
        // the whole window the pointer is live.
        GC.KeepAlive(triple);
        GC.KeepAlive(addK);
    }
}
