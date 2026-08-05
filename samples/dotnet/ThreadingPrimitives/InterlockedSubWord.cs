using System;
using System.Threading;

// Non-generic sub-word Interlocked over byte/sbyte/short/ushort. The neighbor-integrity
// probes are the point: the target is packed between live siblings, so an atomic wider
// than the declared width visibly clobbers them. Also pins the returned original's
// sign/zero re-extension and the CAS hit-vs-miss contract.
namespace InterlockedSubWord;

internal static class Program
{
    private struct PackedBytes
    {
        public byte Before;
        public byte Target;   // the ref the atomics operate on
        public byte After;
        public byte After2;
    }

    private struct PackedShorts
    {
        public short Before;
        public short Target;
        public short After;
    }

    private static byte s_staticByte = 40;

    internal static void __GateEntry()
    {
        // ---- byte in a packed struct: Exchange + CAS keep the neighbors intact ----
        var pb = new PackedBytes { Before = 0x11, Target = 5, After = 0x22, After2 = 0x33 };
        Console.WriteLine(Interlocked.Exchange(ref pb.Target, (byte)200));          // 5 (old)
        Console.WriteLine(pb.Target);                                               // 200
        Console.WriteLine(Interlocked.CompareExchange(ref pb.Target, (byte)7, (byte)200)); // 200 (hit)
        Console.WriteLine(pb.Target);                                               // 7
        Console.WriteLine(Interlocked.CompareExchange(ref pb.Target, (byte)9, (byte)8));   // 7 (miss)
        Console.WriteLine(pb.Target);                                               // 7
        Console.WriteLine(pb.Before);                                               // 17
        Console.WriteLine(pb.After);                                                // 34
        Console.WriteLine(pb.After2);                                               // 51

        // ---- sbyte: the returned original is sign-extended ----
        sbyte sb = -1;
        Console.WriteLine(Interlocked.Exchange(ref sb, (sbyte)3));                  // -1
        Console.WriteLine(Interlocked.CompareExchange(ref sb, (sbyte)-128, (sbyte)3)); // 3 (hit)
        Console.WriteLine(Interlocked.Exchange(ref sb, (sbyte)0));                  // -128
        Console.WriteLine(sb);                                                      // 0

        // ---- short in a packed struct ----
        var ps = new PackedShorts { Before = -321, Target = 1000, After = 321 };
        Console.WriteLine(Interlocked.Exchange(ref ps.Target, (short)-2000));       // 1000
        Console.WriteLine(Interlocked.CompareExchange(ref ps.Target, (short)55, (short)-2000)); // -2000 (hit)
        Console.WriteLine(Interlocked.CompareExchange(ref ps.Target, (short)66, (short)-2000)); // 55 (miss)
        Console.WriteLine(ps.Target);                                               // 55
        Console.WriteLine(ps.Before);                                               // -321
        Console.WriteLine(ps.After);                                                // 321

        // ---- ushort: the returned original is zero-extended ----
        ushort us = 0xFFFF;
        Console.WriteLine(Interlocked.Exchange(ref us, (ushort)1));                 // 65535
        Console.WriteLine(Interlocked.CompareExchange(ref us, (ushort)0xFFFE, (ushort)1)); // 1 (hit)
        Console.WriteLine(Interlocked.Exchange(ref us, (ushort)0));                 // 65534
        Console.WriteLine(us);                                                      // 0

        // ---- ref into a byte[] element: the array neighbors stay intact ----
        byte[] arr = { 10, 20, 30, 40 };
        Console.WriteLine(Interlocked.Exchange(ref arr[1], (byte)99));              // 20
        Console.WriteLine(Interlocked.CompareExchange(ref arr[2], (byte)77, (byte)30)); // 30 (hit)
        Console.WriteLine(arr[0]);                                                  // 10
        Console.WriteLine(arr[1]);                                                  // 99
        Console.WriteLine(arr[2]);                                                  // 77
        Console.WriteLine(arr[3]);                                                  // 40

        // ---- ref to a static byte field (int32-widened storage in dn2cpp) ----
        Console.WriteLine(Interlocked.Exchange(ref s_staticByte, (byte)41));        // 40
        Console.WriteLine(Interlocked.CompareExchange(ref s_staticByte, (byte)42, (byte)41)); // 41 (hit)
        Console.WriteLine(s_staticByte);                                            // 42
    }
}
