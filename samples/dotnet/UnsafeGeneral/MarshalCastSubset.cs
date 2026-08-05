using System;
using System.Runtime.InteropServices;

// MemoryMarshal.Cast / AsBytes over arbitrary element types; the transpiler
// builds the result span's {f__reference, f__length} with storage-width lengths.
// Byte order is host-endian, which is sound because the gate diffs against real
// .NET on the same machine.
namespace MarshalCastSubset;

struct Px { public int Lo; public int Hi; }

class Program
{
    internal static void __GateEntry()
    {
        int[] data = { 0x04030201, 0x08070605 };
        Span<byte> bytes = MemoryMarshal.Cast<int, byte>(data.AsSpan());
        Console.WriteLine(bytes.Length);                                  // 8
        Console.WriteLine($"{bytes[0]},{bytes[1]},{bytes[2]},{bytes[3]}"); // 1,2,3,4 (LE)

        // struct -> byte, mutated through the byte view.
        Px[] px = { new Px { Lo = 0x04030201, Hi = 0x08070605 } };
        Span<byte> pb = MemoryMarshal.Cast<Px, byte>(px.AsSpan());
        Console.WriteLine($"{pb.Length}:{pb[0]},{pb[1]},{pb[2]},{pb[3]}"); // 8:1,2,3,4
        pb[4] = 0x99;                                                      // low byte of Hi
        Console.WriteLine(px[0].Hi);                                      // 0x08070699

        // byte -> struct, over a ReadOnlySpan.
        byte[] raw = { 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 4, 0, 0, 0 };
        ReadOnlySpan<Px> rs = MemoryMarshal.Cast<byte, Px>((ReadOnlySpan<byte>)raw);
        Console.WriteLine(rs.Length);                                     // 2
        Console.WriteLine($"{rs[1].Lo},{rs[1].Hi}");                      // 3,4

        Span<double> ds = stackalloc double[2];
        ds[0] = 1.5; ds[1] = 2.5;
        Span<byte> db = MemoryMarshal.AsBytes(ds);
        Console.WriteLine(db.Length);                                     // 16
        Span<long> dl = MemoryMarshal.Cast<byte, long>(db);
        Console.WriteLine($"{dl[0]},{dl[1]}");                            // double bit patterns
    }
}
