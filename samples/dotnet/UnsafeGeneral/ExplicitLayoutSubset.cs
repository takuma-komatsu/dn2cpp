using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// [StructLayout] layout control: explicit unions, explicit Size, Pack=1/Pack=8,
// on both value and reference types. Asserted by sizeof fidelity, cross-arm
// punning, byte spans and raw pointer reads. Byte punning is host-endian, which
// is sound because the gate diffs against real .NET on the same machine.
namespace ExplicitLayoutSubset;

[StructLayout(LayoutKind.Explicit)]
struct Overlay
{
    [FieldOffset(0)] public int I;
    [FieldOffset(0)] public float F;
    [FieldOffset(0)] public long L;
}

enum ByteTag : byte { None = 0, Some = 7 }

struct Mem
{
    public long A;
    public long B;
    public long C;
}

// godot_variant_data-shaped: all-offset-0 union, sub-word arms plus a nested
// 24-byte sequential struct arm.
[StructLayout(LayoutKind.Explicit)]
struct VariantData
{
    [FieldOffset(0)] public byte B;
    [FieldOffset(0)] public ByteTag Tag;
    [FieldOffset(0)] public long I64;
    [FieldOffset(0)] public double R8;
    [FieldOffset(0)] public Mem Mem;
}

// godot_variant-shaped: a Pack=8 sequential struct wrapping the explicit union.
[StructLayout(LayoutKind.Sequential, Pack = 8)]
struct Variant
{
    public int Type;
    public VariantData Data;
}

// Explicit Size beyond the fields (the cache-line-padded counter shape).
[StructLayout(LayoutKind.Explicit, Size = 32)]
struct Padded
{
    [FieldOffset(0)] public int Head;
    [FieldOffset(16)] public int Tail;
}

// Pack=1: the long is forced to offset 4 instead of 8.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct Packed
{
    public int A;
    public long B;
}

// Disjoint explicit offsets leaving a 2-byte hole.
[StructLayout(LayoutKind.Explicit)]
struct Sparse
{
    [FieldOffset(0)] public short S;
    [FieldOffset(4)] public int M;
    [FieldOffset(8)] public double D;
}

// XmlAtomicValue+Union shape: all arms at offset 0, including a non-primitive
// value type. DateTime is one int64 word (ticks in the low 62 bits, Kind in the
// top two), so cross-punning it against the scalar arms asserts the two
// representations agree bit for bit, not merely on width.
[StructLayout(LayoutKind.Explicit)]
struct XmlUnion
{
    [FieldOffset(0)] public bool BoolVal;
    [FieldOffset(0)] public double DblVal;
    [FieldOffset(0)] public long I64Val;
    [FieldOffset(0)] public int I32Val;
    [FieldOffset(0)] public DateTime DtVal;
}

// Non-primitive value-type arms at a non-zero aligned offset, sharing a slot:
// both are intrinsic-modeled, so their explicit-layout size comes from the
// runtime struct. The discriminant at offset 0 never overlaps them.
[StructLayout(LayoutKind.Explicit)]
struct TaggedValue
{
    [FieldOffset(0)] public int Kind;
    [FieldOffset(8)] public DateTime When;
    [FieldOffset(8)] public decimal Amount;
}

// LayoutKind.Explicit on a reference type: the object header precedes the field
// region and FieldOffset is relative to that region's start, as in a value type.
// Asserts field values and punning only — never Unsafe.SizeOf on the class,
// which is the pointer width and so says nothing about the layout.
[StructLayout(LayoutKind.Explicit, Size = 128)]
class Barrier
{
    [FieldOffset(0)] public int ThreadCount;
    [FieldOffset(4)] public int Remaining;
    [FieldOffset(64)] public int Phase;   // its own cache line, away from Remaining
}

// Reference-type explicit union: primitive arms overlap at offset 0; the CLR
// guarantees object references never overlap, so the string arm sits apart.
[StructLayout(LayoutKind.Explicit)]
class UnionRef
{
    [FieldOffset(0)] public int I;
    [FieldOffset(0)] public float F;
    [FieldOffset(0)] public byte LowByte;
    [FieldOffset(8)] public string Name;
}

class Program
{
    internal static unsafe void __GateEntry()
    {
        // sizeof fidelity vs the CLR.
        Console.WriteLine(Unsafe.SizeOf<Overlay>());     // 8
        Console.WriteLine(Unsafe.SizeOf<VariantData>()); // 24
        Console.WriteLine(Unsafe.SizeOf<Variant>());     // 32
        Console.WriteLine(Unsafe.SizeOf<Padded>());      // 32
        Console.WriteLine(Unsafe.SizeOf<Packed>());      // 12
        Console.WriteLine(Unsafe.SizeOf<Sparse>());      // 16
        Console.WriteLine(sizeof(Variant));              // 32

        // Overlay: write one arm, read another.
        Overlay o = default;
        o.F = 1.0f;
        Console.WriteLine(o.I);                          // 1065353216
        o.I = unchecked((int)0xC0490FDB);
        Console.WriteLine(o.F);                          // -3.1415927
        o.L = 0x1122334455667788;
        Console.WriteLine(o.I);                          // 1432778632 (low half)
        Console.WriteLine(BitConverter.SingleToInt32Bits(o.F));

        // Sub-word union arms: a byte write touches only its own byte.
        VariantData d = default;
        d.I64 = 0x1111111111111111;
        d.B = 0xFF;
        Console.WriteLine(d.I64);                        // 0x11111111111111FF
        Console.WriteLine((int)d.Tag);                   // 255
        d.Tag = ByteTag.Some;
        Console.WriteLine(d.B);                          // 7
        Console.WriteLine(d.I64);                        // 0x1111111111111107

        // The nested-struct arm overlays the scalar arms.
        d.Mem.A = 0x0102030405060708;
        d.Mem.B = 2;
        d.Mem.C = 3;
        Console.WriteLine(d.I64 == d.Mem.A);             // True
        Console.WriteLine(BitConverter.DoubleToInt64Bits(d.R8));

        // godot_variant-shaped roundtrip + byte-level layout check.
        Variant v = default;
        v.Type = 5;
        v.Data.I64 = 0x0102030405060708;
        Span<byte> vb = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref v, 1));
        Console.WriteLine(vb.Length);                    // 32
        Console.WriteLine(vb[0]);                        // 5 (Type low byte)
        Console.WriteLine(vb[8]);                        // 8 (union starts at 8)
        Console.WriteLine(vb[15]);                       // 1
        Variant* pv = &v;
        Console.WriteLine(*(long*)((byte*)pv + 8));      // 72623859790382856

        // Explicit Size: the tail padding really exists and stays zeroed.
        Padded p = default;
        p.Head = 1;
        p.Tail = 2;
        Span<byte> pb = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref p, 1));
        Console.WriteLine(pb.Length);                    // 32
        Console.WriteLine(pb[16]);                       // 2
        Console.WriteLine(pb[31]);                       // 0
        Console.WriteLine(p.Head + p.Tail);              // 3

        // Pack=1: the long sits at offset 4.
        Packed k = default;
        k.A = -1;
        k.B = 0x0102030405060708;
        Span<byte> kb = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref k, 1));
        Console.WriteLine(kb.Length);                    // 12
        Console.WriteLine(kb[3]);                        // 255 (A high byte)
        Console.WriteLine(kb[4]);                        // 8 (B low byte)
        Console.WriteLine(kb[11]);                       // 1 (B high byte)
        Console.WriteLine(k.B);

        // Disjoint offsets with a hole: each field keeps its own bytes.
        Sparse sp = default;
        sp.S = -2;
        sp.M = 0x7F007F00;
        sp.D = 2.5;
        Console.WriteLine(sp.S);
        Console.WriteLine(sp.M);
        Console.WriteLine(sp.D);
        Span<byte> sb = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref sp, 1));
        Console.WriteLine(sb[2]);                        // hole byte, 0
        Console.WriteLine(sb[4]);                        // M low byte, 0
        Console.WriteLine(sb[7]);                        // M high byte, 127

        // Unsafe.As across an explicit-union boundary.
        long raw = 0x4059000000000000;
        ref Overlay ro = ref Unsafe.As<long, Overlay>(ref raw);
        Console.WriteLine(ro.I);                         // 0
        ro.I = 42;
        Console.WriteLine(raw & 0xFFFFFFFF);             // 42

        // DateTime arm roundtrip, then cross-punned against the long arm: .NET's
        // packed _dateData word, Kind=Utc being 0b01 in the top two bits.
        XmlUnion u = default;
        u.DtVal = new DateTime(2026, 7, 22, 13, 45, 30, DateTimeKind.Utc);
        Console.WriteLine(u.DtVal.Ticks);                // 639203247300000000
        Console.WriteLine((int)u.DtVal.Kind);            // 1 (Utc)
        Console.WriteLine(u.DtVal.Year);                 // 2026
        Console.WriteLine(u.I64Val);                     // 0x48DEE7F77E97B900
        // And back: a raw word written through the long arm decodes as a DateTime.
        XmlUnion punned = default;
        punned.I64Val = unchecked((long)0x8000000000000000UL) | 639203247300000000L;
        Console.WriteLine(punned.DtVal.Ticks + " " + punned.DtVal.Kind);

        // Scalar arms alone, on a union never written as DateTime.
        XmlUnion s = default;
        s.I64Val = 0x0102030405060708;
        Console.WriteLine(s.I32Val);                     // 84281096 (low half)
        s.DblVal = 2.5;
        Console.WriteLine(s.I64Val);                     // 4612811918334230528
        s.I64Val = 1;
        Console.WriteLine(s.BoolVal);                    // True

        // Padded arm: each value type read back as what was written.
        TaggedValue tv = default;
        tv.Kind = 1;
        tv.When = new DateTime(1999, 12, 31);
        Console.WriteLine(tv.Kind);                      // 1
        Console.WriteLine(tv.When.Ticks);                // 630821952000000000
        TaggedValue td = default;
        td.Kind = 2;
        td.Amount = 12.75m;
        Console.WriteLine(td.Kind);                      // 2
        Console.WriteLine(td.Amount);                    // 12.75

        // The offset-64 field stays untouched by writes to offsets 0 and 4.
        Barrier bar = new Barrier();
        bar.ThreadCount = 2;
        bar.Remaining = 2;
        bar.Remaining--;
        bar.Phase++;
        Console.WriteLine(bar.ThreadCount);              // 2
        Console.WriteLine(bar.Remaining);                // 1
        Console.WriteLine(bar.Phase);                    // 1

        // Primitive arms pun at offset 0; the reference at offset 8 survives.
        UnionRef uc = new UnionRef();
        uc.I = unchecked((int)0x010001FF);
        Console.WriteLine(uc.LowByte);                   // 255 (low byte of I)
        uc.F = 1.0f;
        Console.WriteLine(uc.I);                         // 1065353216 (float bits)
        uc.Name = "hello";
        Console.WriteLine(uc.Name);                      // hello
        Console.WriteLine(uc.I);                          // 1065353216 (offset-8 write left offset 0 intact)
    }
}
