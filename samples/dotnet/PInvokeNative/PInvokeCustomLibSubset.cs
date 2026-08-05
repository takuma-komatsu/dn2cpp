#nullable enable
using System;
using System.Runtime.InteropServices;

// A non-system shared library is not linked by default: the transpiler emits
// `-ldn2cpptest` into the link manifest (pinvoke-libs.txt) and the build supplies
// `-L<dir>`. libc-only calls stay manifest-free.
namespace PInvokeCustomLibSubset;

internal static unsafe class Program
{
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_add(int a, int b);

    [DllImport("dn2cpptest")]
    private static extern long dn2cpptest_mul(long a, long b);

    [DllImport("dn2cpptest")]
    private static extern double dn2cpptest_scale(double x, int n);

    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_sum(int* p, int n);

    [DllImport("dn2cpptest")]
    private static extern byte dn2cpptest_lowbyte(ulong v);

    internal static void __GateEntry()
    {
        Console.WriteLine(dn2cpptest_add(3, 4));          // 7
        Console.WriteLine(dn2cpptest_mul(100000L, 100000L)); // 10000000000
        Console.WriteLine(dn2cpptest_scale(1.5, 4));      // 6
        Console.WriteLine(dn2cpptest_lowbyte(0x1234_5678_9ABC_DEF0UL)); // 240

        int* a = stackalloc int[4];
        a[0] = 10; a[1] = 20; a[2] = 30; a[3] = 40;
        Console.WriteLine(dn2cpptest_sum(a, 4));          // 100
    }
}
