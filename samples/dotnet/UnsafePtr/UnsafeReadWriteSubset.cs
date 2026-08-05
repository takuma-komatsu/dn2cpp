#nullable enable
using System;
using System.Runtime.CompilerServices;

// Typed Unsafe read/write/copy through raw addresses. Accesses happen at T's
// real StorageOf width, so sub-word loads sign/zero-extend correctly; the
// Unaligned forms go through memcpy, the aligned ones through a direct deref.
namespace UnsafeReadWriteSubset;

struct Point { public int X; public int Y; }

class Program
{
    internal static unsafe void __GateEntry()
    {
        byte* buf = stackalloc byte[40];

        // Deliberately unaligned offsets, void* form.
        Unsafe.WriteUnaligned(buf + 1, 305419896);
        Console.WriteLine(Unsafe.ReadUnaligned<int>(buf + 1));  // 305419896

        Unsafe.WriteUnaligned(buf + 5, 1234567890123456789L);
        Console.WriteLine(Unsafe.ReadUnaligned<long>(buf + 5)); // 1234567890123456789

        // The same 2 bytes read signed and unsigned, through the int32 promote.
        Unsafe.WriteUnaligned(buf + 13, (short)-2);
        Console.WriteLine(Unsafe.ReadUnaligned<short>(buf + 13));  // -2
        Console.WriteLine(Unsafe.ReadUnaligned<ushort>(buf + 13)); // 65534

        Unsafe.WriteUnaligned(buf + 17, 3.5);
        Console.WriteLine(Unsafe.ReadUnaligned<double>(buf + 17)); // 3.5

        // The ref-byte overload, over a managed array.
        byte[] arr = new byte[8];
        Unsafe.WriteUnaligned(ref arr[0], 1000);
        Console.WriteLine(Unsafe.ReadUnaligned<int>(ref arr[0])); // 1000

        // Aligned Read/Write.
        int x = 0;
        Unsafe.Write(&x, 999);
        Console.WriteLine(Unsafe.Read<int>(&x));               // 999

        Point p = new Point { X = 11, Y = 22 };
        byte* pbuf = stackalloc byte[8];
        Unsafe.WriteUnaligned(pbuf, p);
        Point q = Unsafe.ReadUnaligned<Point>(pbuf);
        Console.WriteLine($"{q.X},{q.Y}");                     // 11,22

        // Unsafe.Copy, both operand dispositions.
        int srcVal = 4242;
        int dstVal = 0;
        Unsafe.Copy(&dstVal, ref srcVal);
        Console.WriteLine(dstVal);                             // 4242
        int dstVal2 = 0;
        Unsafe.Copy(ref dstVal2, (void*)(&srcVal));
        Console.WriteLine(dstVal2);                            // 4242
    }
}
