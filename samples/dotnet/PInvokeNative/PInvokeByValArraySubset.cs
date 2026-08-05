#nullable disable
using System;
using System.Runtime.InteropServices;

// A [MarshalAs(ByValArray, SizeConst = N)] field embeds its elements INLINE in the native
// struct while the managed struct holds an array reference. Copy-in fills the inline slots
// — a null array zeroes them, a longer one is truncated, a shorter one throws — and
// copy-back allocates a FRESH managed array of N elements. Only blittable scalar and enum
// elements are supported; strings and structs are a carve-out.
namespace PInvokeByValArraySubset;

internal static class Program
{
    // int elements (managed Dn2CppArrayI4).
    [StructLayout(LayoutKind.Sequential)]
    private struct FixedVec
    {
        public int N;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public int[] Vals;
    }

    // short elements (managed Dn2CppArrayN, 2-byte packed stride).
    [StructLayout(LayoutKind.Sequential)]
    private struct ShortVec
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] public short[] Vals;
    }

    // double elements (managed Dn2CppArrayN, 8-byte packed stride).
    [StructLayout(LayoutKind.Sequential)]
    private struct DblVec
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] Vals;
    }

    [DllImport("dn2cpptest")] private static extern int dn2cpptest_fixedvec_sum(FixedVec v);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_fixedvec_scale(ref FixedVec v, int k);
    [DllImport("dn2cpptest")] private static extern int dn2cpptest_shortvec_sum(ShortVec v);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_shortvec_negate(ref ShortVec v);
    [DllImport("dn2cpptest")] private static extern double dn2cpptest_dblvec_sum(DblVec v);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_dblvec_fill(out DblVec v, double basis);

    internal static void __GateEntry()
    {
        var a = new FixedVec { N = 4, Vals = new[] { 1, 2, 3, 4 } };
        Console.WriteLine(dn2cpptest_fixedvec_sum(a));                       // 10

        // Longer than SizeConst: truncated.
        var lng = new FixedVec { N = 4, Vals = new[] { 1, 2, 3, 4, 5, 6 } };
        Console.WriteLine(dn2cpptest_fixedvec_sum(lng));                     // 10

        // Null: the inline buffer is zero-filled.
        var nul = new FixedVec { N = 0, Vals = null };
        Console.WriteLine(dn2cpptest_fixedvec_sum(nul));                     // 0

        // Shorter than SizeConst: real .NET refuses rather than zero-padding.
        try
        {
            var shrt = new FixedVec { N = 2, Vals = new[] { 10, 20 } };
            Console.WriteLine(dn2cpptest_fixedvec_sum(shrt));
        }
        catch (ArgumentException)
        {
            Console.WriteLine("short-in throws");                           // short-in throws
        }

        // by-ref [In,Out]: a fresh length-4 array comes back, never the input instance.
        var r = new FixedVec { N = 1, Vals = new[] { 1, 2, 3, 4 } };
        dn2cpptest_fixedvec_scale(ref r, 10);
        Console.WriteLine($"{r.N} {string.Join(",", r.Vals)} {r.Vals.Length}");  // 2 10,20,30,40 4

        var sv = new ShortVec { Vals = new short[] { 1, 2, 3, 4, 5 } };
        Console.WriteLine(dn2cpptest_shortvec_sum(sv));                     // 15

        // Printed by index: the string.Join intrinsic does not cover a short element type.
        var svn = new ShortVec { Vals = new short[] { 1, -2, 3, -4, 5 } };
        dn2cpptest_shortvec_negate(ref svn);
        Console.WriteLine($"{svn.Vals[0]},{svn.Vals[1]},{svn.Vals[2]},{svn.Vals[3]},{svn.Vals[4]} {svn.Vals.Length}");  // -1,2,-3,4,-5 5

        var dv = new DblVec { Vals = new[] { 1.5, 2.25, 0.25 } };
        Console.WriteLine(dn2cpptest_dblvec_sum(dv));                       // 4

        // [Out]-only skips the copy-in, so the native sees a zeroed struct.
        dn2cpptest_dblvec_fill(out DblVec df, 2.0);
        Console.WriteLine($"{string.Join(",", df.Vals)} {df.Vals.Length}"); // 2,4,6 3
    }
}
