#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// A [MarshalAs] on a parameter or return overrides the type+CharSet default: a string
// encoding whatever the method CharSet says, a bool width, LPArray on a blittable
// array, and FunctionPtr on a function-pointer type (the no-op naming the raw pointer
// the type already crosses as — on void* it is refused, asserted by the transpile
// negative arm in gates/build-and-run-pinvoke-native.sh). Integer and enum width
// overrides are carved out — .NET requires the unmanaged width to match the managed
// integer's size — as are COM/BStr/SafeArray.
namespace PInvokeMarshalAsSubset;

internal static unsafe class Program
{
    // Arg encodings that beat the method CharSet, in both directions: an Ansi-default
    // method forced to UTF-16, and a Unicode method forced back to UTF-8.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_wstr_len")]
    private static extern int WLenForcedUtf16([MarshalAs(UnmanagedType.LPWStr)] string s);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_strlen", CharSet = CharSet.Unicode)]
    private static extern int ByteLenForcedUtf8([MarshalAs(UnmanagedType.LPStr)] string s);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_strlen")]
    private static extern int ByteLenUtf8([MarshalAs(UnmanagedType.LPUTF8Str)] string s);
    // The same both ways on the return.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_wstr_make")]
    [return: MarshalAs(UnmanagedType.LPWStr)]
    private static extern string MakeForcedUtf16();
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_greeting", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    private static extern string GreetingForcedUtf8();

    // U1: a single 0/1 byte rather than the 4-byte Win32 BOOL; a non-1 truthy return
    // still normalizes to managed true.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_u1_and")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool U1And([MarshalAs(UnmanagedType.U1)] bool a, [MarshalAs(UnmanagedType.U1)] bool b);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_u1_truthy")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool U1Truthy();

    // LPArray on a blittable array is the default array marshalling.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_iarr_sum")]
    private static extern int LpArraySum([MarshalAs(UnmanagedType.LPArray)] int[] a, int n);

    // FunctionPtr on a function-pointer parameter and return: both cross as the raw
    // pointer the bare type already is (PInvokeUcoReverseSubset carries the bare form).
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_ucb_combine")]
    private static extern int FnPtrCombine(
        [MarshalAs(UnmanagedType.FunctionPtr)] delegate* unmanaged[Cdecl]<int, int, int> fn, int a, int b);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_fnptr_add")]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    private static extern delegate* unmanaged[Cdecl]<int, int, int> FnPtrAddDescribed();

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int Diff(int a, int b) => a - b;

    // The same descriptor as a struct-FIELD no-op: the fields keep the struct blittable,
    // so it crosses raw exactly like PInvokeVtblStructSubset's bare form of this vtable.
    [StructLayout(LayoutKind.Sequential)]
    private struct DescribedVtbl
    {
        [MarshalAs(UnmanagedType.FunctionPtr)] public delegate* unmanaged[Cdecl]<int, int, int> Read;
        [MarshalAs(UnmanagedType.FunctionPtr)] public delegate* unmanaged[Cdecl]<int, int, int, int> Seek;
        [MarshalAs(UnmanagedType.FunctionPtr)] public delegate* unmanaged[Cdecl]<int, void> Close;
    }

    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_io_install")]
    private static extern void InstallDescribed(ref DescribedVtbl v);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_io_exercise")]
    private static extern int ExerciseDescribed(int handle);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int SeekD(int handle, int off, int whence) => handle + off * whence;

    private static int s_closed;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void CloseD(int handle) => s_closed += handle;

    internal static void __GateEntry()
    {
        Console.WriteLine(WLenForcedUtf16("café"));    // 4 (UTF-16 code units)
        Console.WriteLine(WLenForcedUtf16("A☺"));       // 2
        Console.WriteLine(ByteLenForcedUtf8("café"));   // 5 (é = 2 UTF-8 bytes)
        Console.WriteLine(ByteLenUtf8("héllo"));        // 6

        string u16 = MakeForcedUtf16();                 // "Hi" + U+263A
        Console.WriteLine(u16.Length);                  // 3
        Console.WriteLine((int)u16[2]);                 // 9786
        Console.WriteLine(GreetingForcedUtf8());        // dn2cpp says hi

        Console.WriteLine(U1And(true, true));           // True
        Console.WriteLine(U1And(true, false));          // False
        Console.WriteLine(U1Truthy());                  // True (7 normalized)

        int[] xs = { 4, 5, 6, 7 };
        Console.WriteLine(LpArraySum(xs, xs.Length));   // 22

        // The described function pointer crosses in (invoked twice by the native) and
        // back out (invoked here through calli).
        Console.WriteLine(FnPtrCombine(&Diff, 30, 12)); // 17982
        delegate* unmanaged[Cdecl]<int, int, int> add = FnPtrAddDescribed();
        Console.WriteLine(add(19, 23));                 // 42

        // The described struct fields cross raw and dispatch after the install returns.
        var vt = new DescribedVtbl { Read = &Diff, Seek = &SeekD, Close = &CloseD };
        InstallDescribed(ref vt);
        Console.WriteLine(ExerciseDescribed(5));        // 202
        Console.WriteLine(s_closed);                    // 5
    }
}
