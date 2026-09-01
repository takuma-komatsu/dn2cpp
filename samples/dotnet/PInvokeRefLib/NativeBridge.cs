#nullable enable
using System;
using System.Runtime.InteropServices;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.LegacyBehavior)]

// The [DllImport] declarations live in this SEPARATE assembly, pulled in with
// -r, never in the app module. They use the same lazy resolver/OS-loader path
// as imports declared by the application assembly.
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

    [DllImport("logical_dn2cpptest", EntryPoint = "dn2cpptest_add")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
    private static extern int redirected_add(int a, int b);

    [DllImport("dn2cpptest_assembly", EntryPoint = "dn2cpptest_add")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
    private static extern int assembly_directory_add(int a, int b);

    [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcessId")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint get_current_process_id();

    [DllImport("libc", EntryPoint = "setattrlist")]
    private static extern int darwin_selector_probe();

    [DllImport("resolver_throw", EntryPoint = "never_reached")]
    private static extern int resolver_throw();

    public static int Add(int a, int b) => dn2cpptest_add(a, b);

    public static long Mul(long a, long b) => dn2cpptest_mul(a, b);

    public static double Scale(double x, int n) => dn2cpptest_scale(x, n);

    // dn2cpptest_strlen is plain strlen, so this is the marshalled string's
    // UTF-8 byte length.
    public static int Utf8Length(string s) => dn2cpptest_strlen(s);

    public static int RedirectedAdd(int a, int b) => redirected_add(a, b);

    public static int RedirectedAddAgain(int a, int b) => redirected_add(a, b);

    private static BinOpDelegate? _redirectedDelegate;

    public static int RedirectedAddViaDelegate(int a, int b)
    {
        _redirectedDelegate ??= redirected_add;
        return _redirectedDelegate(a, b);
    }

    public static int AssemblyDirectoryAdd(int a, int b) => assembly_directory_add(a, b);

    public static bool System32Probe() => !OperatingSystem.IsWindows()
        || get_current_process_id() != 0;

    public static void ReachDarwinSelectorProbe()
    {
        _ = (Func<int>)darwin_selector_probe;
    }

    public static int ResolverThrows() => resolver_throw();

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
