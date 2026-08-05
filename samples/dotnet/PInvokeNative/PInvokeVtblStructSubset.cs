#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// The vtable pattern: a struct of raw unmanaged function pointers is installed once, and
// LATER calls dispatch through the stored copy — the install itself invokes nothing. The
// fields hold [UnmanagedCallersOnly] method addresses, so no delegate object and no
// lifetime hazard is involved, and the struct crosses byref as a pinned pointer.
namespace PInvokeVtblStructSubset;

internal static unsafe class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct IoVtbl
    {
        public delegate* unmanaged[Cdecl]<int, int, int> Read;
        public delegate* unmanaged[Cdecl]<int, int, int, int> Seek;
        public delegate* unmanaged[Cdecl]<int, void> Close;
    }

    [DllImport("dn2cpptest")]
    private static extern void dn2cpptest_io_install(ref IoVtbl v);
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_io_exercise(int handle);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int ReadImpl(int handle, int n) => handle * 100 + n;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int SeekImpl(int handle, int off, int whence) => handle + off * whence;

    private static int s_closedSum;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void CloseImpl(int handle) => s_closedSum += handle;

    internal static void __GateEntry()
    {
        var v = new IoVtbl { Read = &ReadImpl, Seek = &SeekImpl, Close = &CloseImpl };
        dn2cpptest_io_install(ref v);

        // The install call is over, so each exercise dispatches through the STORED vtable.
        Console.WriteLine(dn2cpptest_io_exercise(7));  // 915
        Console.WriteLine(dn2cpptest_io_exercise(9));  // 1117
        Console.WriteLine(s_closedSum);
    }
}
