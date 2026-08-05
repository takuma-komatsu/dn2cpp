#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// &Method on an [UnmanagedCallersOnly] static yields the raw method address — no delegate
// object, no marshalling thunk — and the native re-enters managed code through a plain C
// function pointer. The int, double and void-return ABIs are each covered, and the
// void callback mutates managed static state the section prints afterwards.
namespace PInvokeUcoReverseSubset;

internal static unsafe class Program
{
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_ucb_combine(delegate* unmanaged[Cdecl]<int, int, int> fn, int a, int b);
    [DllImport("dn2cpptest")]
    private static extern double dn2cpptest_ucb_scale(delegate* unmanaged[Cdecl]<double, int, double> fn, double x, int n);
    [DllImport("dn2cpptest")]
    private static extern void dn2cpptest_ucb_notify(delegate* unmanaged[Cdecl]<int, void> fn, int n);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int Diff(int a, int b) => a - b;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static double Scale(double x, int n) => x * n;

    private static int s_noteSum;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void Note(int v) => s_noteSum += v * v;

    internal static void __GateEntry()
    {
        // Two managed re-entries in one native call.
        Console.WriteLine(dn2cpptest_ucb_combine(&Diff, 30, 12));  // 17982

        Console.WriteLine(dn2cpptest_ucb_scale(&Scale, 2.5, 4));   // 135

        // The void callback is invoked with 1..4 and accumulates squares.
        dn2cpptest_ucb_notify(&Note, 4);
        Console.WriteLine(s_noteSum);
    }
}
