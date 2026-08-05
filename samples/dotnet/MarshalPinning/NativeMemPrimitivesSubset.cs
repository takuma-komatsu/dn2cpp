#nullable enable
using System;
using System.Runtime.InteropServices;

// Native-memory primitive accessors over an AllocHGlobal buffer: the typed
// Marshal.Write{Byte,Int16,Int32,Int64,IntPtr} / Read{...} accessors at the base
// and at byte offsets (round-trip the exact written values), plus the native-heap
// reallocators ReAllocHGlobal (content-preserving grow) and the CoTaskMem allocator
// (AllocCoTaskMem / ReAllocCoTaskMem / FreeCoTaskMem, which share the same process
// heap here). All offsets are naturally aligned. CoreLib only; diffed exact vs real
// .NET.
namespace NativeMemPrimitivesSubset;

unsafe class Program
{
    internal static void __GateEntry()
    {
        IntPtr buf = Marshal.AllocHGlobal(64);

        // Write* / Read* round-trips at the base and at byte offsets.
        Marshal.WriteByte(buf, 0xAB);
        Marshal.WriteByte(buf, 1, 0xCD);
        Console.WriteLine(Marshal.ReadByte(buf));        // 171
        Console.WriteLine(Marshal.ReadByte(buf, 1));     // 205

        Marshal.WriteInt16(buf, 2, 0x1234);
        Console.WriteLine(Marshal.ReadInt16(buf, 2));    // 4660

        Marshal.WriteInt32(buf, 4, 0x01020304);
        Console.WriteLine(Marshal.ReadInt32(buf, 4));    // 16909060

        Marshal.WriteInt64(buf, 8, 0x1122334455667788L);
        Console.WriteLine(Marshal.ReadInt64(buf, 8));    // 1234605616436508552

        Marshal.WriteIntPtr(buf, 16, (IntPtr)0x0A0B0C0D);
        Console.WriteLine((long)Marshal.ReadIntPtr(buf, 16)); // 168496141

        // No-offset overloads write/read at the base.
        Marshal.WriteInt32(buf, 999);
        Console.WriteLine(Marshal.ReadInt32(buf));       // 999

        // ReAllocHGlobal preserves the leading bytes across the grow.
        Marshal.WriteInt32(buf, 0, 0x7F6E5D4C);
        IntPtr buf2 = Marshal.ReAllocHGlobal(buf, (IntPtr)256);
        Console.WriteLine(Marshal.ReadInt32(buf2, 0));   // 2137939276
        Marshal.FreeHGlobal(buf2);

        // CoTaskMem allocator: alloc / write / read / realloc / free.
        IntPtr co = Marshal.AllocCoTaskMem(32);
        Marshal.WriteInt64(co, 0, 42L);
        Console.WriteLine(Marshal.ReadInt64(co, 0));     // 42
        IntPtr co2 = Marshal.ReAllocCoTaskMem(co, 64);
        Console.WriteLine(Marshal.ReadInt64(co2, 0));    // 42
        Marshal.FreeCoTaskMem(co2);
    }
}
