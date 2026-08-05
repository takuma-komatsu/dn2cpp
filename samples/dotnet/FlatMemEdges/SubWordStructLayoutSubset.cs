#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Sub-word struct fields (byte/sbyte/short/ushort, bool/char, byte-backed
// enums) lay out at their real storage width, so a struct's C++ layout is
// byte-identical to .NET's packed sequential layout. Covers sizeof /
// Unsafe.SizeOf / Marshal.SizeOf and Marshal.OffsetOf over sub-word-field
// structs (incl. a nested struct and a byte-backed-enum field), a 16-bit store
// through a `ref ushort` obtained from a struct field (the ldflda
// widened-slot-garbage repro), memcpy-as-image decoding (MemoryMarshal
// Read/Write/Cast over a byte blob), a struct-array <-> byte-array round-trip
// via Unsafe.CopyBlock, and a fixed-buffer flattening reinterpret (byte buffer
// punned to a sub-word struct pointer with byte-exact stride). Reinterpret byte
// order is host-endian; CoreLib only; diffed exact vs real .NET.
namespace SubWordStructLayoutSubset;

struct HuffmanCode { public byte Bits; public ushort Value; }        // 4 bytes (.NET packed)
struct MixedWide { public uint A; public short B; public short C; }  // 8 bytes
struct Packed6
{
    public byte A; public byte B; public sbyte C; public byte D;
    public ushort E; public ushort F;                                 // 8 bytes
}
struct Nested { public HuffmanCode H; public byte Tail; }             // 6 bytes (align 2)
enum ByteEnum : byte { Zero = 0, One = 1, Two = 2 }
struct WithEnum { public ByteEnum E; public ushort V; }               // 4 bytes
struct WithBoolChar { public bool B; public char C; public byte D; }  // 6 bytes

unsafe struct FlatTable { public fixed byte Flat[4 * 3]; }            // 3 flattened HuffmanCodes

unsafe class Program
{
    internal static void __GateEntry()
    {
        // sizeof / Unsafe.SizeOf / Marshal.SizeOf — the managed and marshalled
        // sizes agree for these all-blittable-field structs.
        Console.WriteLine($"{sizeof(HuffmanCode)},{Unsafe.SizeOf<HuffmanCode>()},{Marshal.SizeOf<HuffmanCode>()}");  // 4,4,4
        Console.WriteLine($"{sizeof(MixedWide)},{Unsafe.SizeOf<MixedWide>()},{Marshal.SizeOf<MixedWide>()}");        // 8,8,8
        Console.WriteLine($"{sizeof(Packed6)},{Unsafe.SizeOf<Packed6>()},{Marshal.SizeOf<Packed6>()}");              // 8,8,8
        Console.WriteLine($"{sizeof(Nested)},{Unsafe.SizeOf<Nested>()},{Marshal.SizeOf<Nested>()}");                 // 6,6,6
        Console.WriteLine($"{sizeof(WithEnum)},{Unsafe.SizeOf<WithEnum>()},{Marshal.SizeOf<WithEnum>()}");           // 4,4,4
        // bool/char marshal at different widths (Win32 BOOL / Ansi byte), so only
        // the managed-layout sizes are asserted for this one.
        Console.WriteLine($"{sizeof(WithBoolChar)},{Unsafe.SizeOf<WithBoolChar>()}");                                // 6,6

        // Marshal.OffsetOf on sub-word fields.
        Console.WriteLine((long)Marshal.OffsetOf<HuffmanCode>("Value"));  // 2
        Console.WriteLine((long)Marshal.OffsetOf<MixedWide>("B"));        // 4
        Console.WriteLine((long)Marshal.OffsetOf<MixedWide>("C"));        // 6
        Console.WriteLine((long)Marshal.OffsetOf<Packed6>("E"));          // 4
        Console.WriteLine((long)Marshal.OffsetOf<Packed6>("F"));          // 6
        Console.WriteLine((long)Marshal.OffsetOf<Nested>("Tail"));        // 4
        Console.WriteLine((long)Marshal.OffsetOf<WithEnum>("V"));         // 2

        // 16-bit store through a `ref ushort` into a struct field (ldflda), then
        // read the whole struct back — both the sibling field and the raw bytes.
        var hc = default(HuffmanCode);
        hc.Bits = 0xAB;
        ref ushort v = ref hc.Value;
        v = 0x1234;
        Console.WriteLine($"{hc.Bits:X2},{hc.Value:X4}");                 // AB,1234
        Span<byte> raw = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref hc, 1));
        for (int i = 0; i < raw.Length; i++)
            Console.Write($"{raw[i]:X2}");                                // AB001234 (LE, zeroed pad)
        Console.WriteLine();

        // memcpy-as-image: decode structs straight out of a byte blob.
        byte[] blob = { 0x07, 0x00, 0x34, 0x12, 0x09, 0x00, 0x78, 0x56 };
        var h0 = MemoryMarshal.Read<HuffmanCode>(blob);
        Console.WriteLine($"{h0.Bits},{h0.Value:X4}");                    // 7,1234
        ReadOnlySpan<HuffmanCode> hs = MemoryMarshal.Cast<byte, HuffmanCode>(blob);
        Console.WriteLine($"{hs.Length}:{hs[0].Bits},{hs[0].Value:X4}:{hs[1].Bits},{hs[1].Value:X4}");
        var h1 = new HuffmanCode { Bits = 5, Value = 0x0F0E };
        MemoryMarshal.Write(blob.AsSpan(4), in h1);
        Console.WriteLine($"{blob[4]:X2}{blob[5]:X2}{blob[6]:X2}{blob[7]:X2}");  // 05000E0F

        // struct-array <-> byte-array round-trip via Unsafe.CopyBlock (packed
        // 4-byte stride on both sides).
        var src = new HuffmanCode[4];
        for (int i = 0; i < src.Length; i++)
            src[i] = new HuffmanCode { Bits = (byte)(i + 1), Value = (ushort)(0x1010 * (i + 1)) };
        var bytes = new byte[src.Length * Unsafe.SizeOf<HuffmanCode>()];
        Unsafe.CopyBlock(ref bytes[0], ref Unsafe.As<HuffmanCode, byte>(ref src[0]), (uint)bytes.Length);
        var back = new HuffmanCode[4];
        Unsafe.CopyBlock(ref Unsafe.As<HuffmanCode, byte>(ref back[0]), ref bytes[0], (uint)bytes.Length);
        for (int i = 0; i < back.Length; i++)
            Console.Write($"{back[i].Bits},{back[i].Value:X4};");
        Console.WriteLine();

        // fixed-buffer flattening reinterpret: a flat byte buffer punned to a
        // HuffmanCode* strides 4 bytes per element (the DnBrotli scratch-table
        // pattern).
        FlatTable t = default;
        HuffmanCode* p = (HuffmanCode*)t.Flat;
        for (int i = 0; i < 3; i++)
        {
            p[i].Bits = (byte)(i + 1);
            p[i].Value = (ushort)(0x0100 * (i + 1));
        }
        Console.WriteLine($"{p[2].Bits},{p[2].Value:X4}");                // 3,0300
        Console.WriteLine($"{t.Flat[8]},{t.Flat[10]:X2}{t.Flat[11]:X2}"); // 3,0003 (LE bytes of 0x0300)

        // byte-backed enum field: packed next to a ushort, raw bytes exact.
        var we = new WithEnum { E = ByteEnum.Two, V = 0xBEEF };
        Span<byte> web = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref we, 1));
        Console.WriteLine($"{we.E},{we.V:X4}:{web[0]:X2}{web[2]:X2}{web[3]:X2}");  // Two,BEEF:02EFBE

        // bool/char fields: 1- and 2-byte packed storage.
        var bc = new WithBoolChar { B = true, C = 'Z', D = 9 };
        Span<byte> bcb = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref bc, 1));
        Console.WriteLine($"{bc.B},{bc.C},{bc.D}:{bcb[0]:X2}{bcb[2]:X2}{bcb[4]:X2}");  // True,Z,9:015A09

        // Primitive instance intrinsics whose receiver is a pointer INTO the
        // narrowed layout (ldflda of a sub-word field): char.CompareTo /
        // char.ToString / bool.ToString must read exactly one element, not an
        // int32 that splices in the neighboring field (the RegexCharClass
        // Canonicalize sort-lambda shape that broke multi-range char classes).
        var cc = new WithBoolChar { B = false, C = 'x', D = 0xEE };
        Console.WriteLine(cc.C.CompareTo('y'));                       // -1
        Console.WriteLine($"{cc.C.ToString()},{cc.B.ToString()},{bc.B.ToString()}");  // x,False,True
        var ranges = new System.Collections.Generic.List<(char, char)> { ('m', 'o'), ('a', 'c') };
        ranges.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        foreach (var r in ranges)
            Console.Write($"{r.Item1}{r.Item2};");                    // ac;mo;
        Console.WriteLine();

        // Enum.TryParse writing its `out T` through a ref to a narrowed
        // byte-backed-enum field: the 1-byte store must not clobber the
        // neighboring ushort.
        var we2 = new WithEnum { E = ByteEnum.Zero, V = 0xABCD };
        Console.WriteLine(Enum.TryParse("Two", out we2.E));           // True
        Console.WriteLine($"{we2.E},{we2.V:X4}");                     // Two,ABCD
    }
}
