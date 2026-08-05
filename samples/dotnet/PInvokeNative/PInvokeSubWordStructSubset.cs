#nullable enable
using System;
using System.Runtime.InteropServices;

// A blittable struct with SUB-WORD fields. The managed layout is packed at the real
// storage widths, so it must cross by value, by ref — where the native's 8- and 16-bit
// writes land directly in the managed storage — and as an array, exactly like the
// equivalent C struct.
namespace PInvokeSubWordStructSubset;

internal static class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Huff { public byte Bits; public ushort Value; }

    [DllImport("dn2cpptest")] private static extern Huff dn2cpptest_huff_make(int bits, int value);
    [DllImport("dn2cpptest")] private static extern int dn2cpptest_huff_sum(Huff h);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_huff_bump(ref Huff h);

    // One function under two directions: struct arrays copy-marshal, so the default
    // [In] must NOT write back.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_huff_arr_sum_bump")]
    private static extern int huff_arr_default(Huff[] a, int n);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_huff_arr_sum_bump")]
    private static extern int huff_arr_inout([In, Out] Huff[] a, int n);

    internal static void __GateEntry()
    {
        Huff h = dn2cpptest_huff_make(7, 0x1234);
        Console.WriteLine($"{h.Bits},{h.Value}");           // 7,4660
        Console.WriteLine(dn2cpptest_huff_sum(h));          // 4667

        dn2cpptest_huff_bump(ref h);
        Console.WriteLine($"{h.Bits},{h.Value}");           // 8,4917

        var arr = new Huff[]
        {
            new Huff { Bits = 1, Value = 0x0100 },
            new Huff { Bits = 2, Value = 0x0200 },
            new Huff { Bits = 3, Value = 0x0300 },
        };
        Console.WriteLine(huff_arr_default(arr, arr.Length));  // 1542
        Console.WriteLine($"{arr[0].Bits},{arr[1].Bits},{arr[2].Bits}");  // 1,2,3 (no write-back)

        Console.WriteLine(huff_arr_inout(arr, arr.Length));     // 1542
        Console.WriteLine($"{arr[0].Bits},{arr[1].Bits},{arr[2].Bits}");  // 2,3,4
    }
}
