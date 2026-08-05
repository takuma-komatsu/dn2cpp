#nullable enable
using System;
using System.Runtime.InteropServices;
using PInvokeRefLib;

// A delegate method group or function pointer over a [DllImport] ITSELF. A call site
// lowers inline, but taking an address needs a function, so the transpiler synthesizes a
// forwarder from the same P/Invoke lowering — the invoke and the calli must marshal
// identically to a direct call. Covered for the app module's own import and for the
// referenced binding assembly's.
namespace PInvokeFtnDelegateSubset;

internal static class Program
{
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_add(int a, int b);

    [DllImport("dn2cpptest")]
    private static extern double dn2cpptest_scale(double x, int n);

    private delegate int BinOp(int a, int b);

    private static int Apply(Func<int, int, int> f, int a, int b)
    {
        return f(a, b);
    }

    internal static unsafe void __GateEntry()
    {
        // The delegate-adapter path: static target, target slot ignored.
        BinOp add = dn2cpptest_add;
        Console.WriteLine(add(19, 23));                          // 42
        // Through delegate-typed plumbing, so nothing can devirtualize it.
        Console.WriteLine(Apply(dn2cpptest_add, 5, 6));          // 11
        // The raw-address path: calli, no target slot.
        delegate*<int, int, int> p = &dn2cpptest_add;
        Console.WriteLine(p(40, 2));                             // 42
        delegate*<double, int, double> scale = &dn2cpptest_scale;
        Console.WriteLine(scale(2.5, 4));                        // 10
        // The twins created inside the referenced assembly.
        Console.WriteLine(NativeBridge.AddViaDelegate(20, 22));  // 42
        Console.WriteLine(NativeBridge.MulViaFnPtr(6L, 7L));     // 42
    }
}
