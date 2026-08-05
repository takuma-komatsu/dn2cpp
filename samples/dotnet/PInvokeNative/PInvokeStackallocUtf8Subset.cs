#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Text;

// The allocation-averse binding idiom: encode into a stackalloc'd Span<byte>,
// NUL-terminate by hand, pass the fixed byte pointer. No marshaller runs, so the native
// must see exactly the bytes the span holds — pinned by byte length and byte sum.
namespace PInvokeStackallocUtf8Subset;

internal static unsafe class Program
{
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_utf8_len(byte* s);
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_utf8_sum(byte* s);

    internal static void __GateEntry()
    {
        Span<byte> buf = stackalloc byte[32];
        int n = Encoding.UTF8.GetBytes("café ☺", buf);
        buf[n] = 0;
        Console.WriteLine(n);                               // 9
        fixed (byte* p = buf)
        {
            Console.WriteLine(dn2cpptest_utf8_len(p));      // 9
            Console.WriteLine(dn2cpptest_utf8_sum(p));      // 1258
        }

        // ASCII only, with the NUL cutting the buffer short of its capacity.
        Span<byte> id = stackalloc byte[8];
        "cue01"u8.CopyTo(id);
        id[5] = 0;
        fixed (byte* p = id)
        {
            Console.WriteLine(dn2cpptest_utf8_len(p));      // 5
            Console.WriteLine(dn2cpptest_utf8_sum(p));      // 414
        }
    }
}
