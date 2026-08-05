#nullable enable
using System.Runtime.InteropServices;

// The [DllImport] declarations live in this SEPARATE assembly, pulled in with
// -r, never in the app module. Lowering them to direct native calls is opt-in
// via `--pinvoke-module dn2cpptest`; without the flag the transpile fails
// loudly. The gate asserts both arms.
namespace PInvokeRefLib;

public static class NativeBridge
{
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_add(int a, int b);

    [DllImport("dn2cpptest")]
    private static extern long dn2cpptest_mul(long a, long b);

    [DllImport("dn2cpptest")]
    private static extern double dn2cpptest_scale(double x, int n);

    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_strlen(string s);

    public static int Add(int a, int b) => dn2cpptest_add(a, b);

    public static long Mul(long a, long b) => dn2cpptest_mul(a, b);

    public static double Scale(double x, int n) => dn2cpptest_scale(x, n);

    // dn2cpptest_strlen is plain strlen, so this is the marshalled string's
    // UTF-8 byte length.
    public static int Utf8Length(string s) => dn2cpptest_strlen(s);

    public delegate int BinOpDelegate(int a, int b);

    private static BinOpDelegate? _addDelegate;

    // A delegate over the [DllImport] method group itself: the address-taken
    // import gets a synthesized forwarder body, and invoking the delegate must
    // marshal identically to a direct call.
    public static int AddViaDelegate(int a, int b)
    {
        _addDelegate ??= dn2cpptest_add;
        return _addDelegate(a, b);
    }

    // The calli twin: no target slot, so the forwarder's raw address is what
    // calli invokes.
    public static unsafe long MulViaFnPtr(long a, long b)
    {
        delegate*<long, long, long> f = &dn2cpptest_mul;
        return f(a, b);
    }
}
