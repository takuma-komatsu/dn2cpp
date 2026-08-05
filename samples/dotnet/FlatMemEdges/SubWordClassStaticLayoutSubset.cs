#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Sub-word fields of a REFERENCE type, and sub-word STATICS, lay out at their
// real storage width — the class/static half of the packed-field policy that
// SubWordStructLayoutSubset covers for value types. The hazard this pins is
// the promoted-slot one: with a sub-word field widened to a 4-byte slot, a
// `ref short`/`ref sbyte` obtained by ldflda (class field) or ldsflda (static)
// addresses only the low half, so a narrow store through the ref leaves the
// slot's high bytes carrying whatever a previous SIGN-EXTENDED wider store to
// the same slot left there — and the next field read consumes all four. Every
// section below stores a negative value first (that is what sets the high
// bytes), then writes a small value through the ref, then reads the field.
// CoreLib only; diffed exact vs real .NET.
namespace SubWordClassStaticLayoutSubset;

sealed class Holder
{
    public short S;
    public sbyte SB;
    public ushort U;
    public byte B;
    public char C;
    public bool Bo;
    public SByteEnum E;
    public int Guard;
}

enum SByteEnum : sbyte { Neg = -1, Zero = 0, Two = 2 }

static class Statics
{
    public static short S;
    public static sbyte SB;
    public static ushort U;
    public static char C;
    public static SByteEnum E;
    public static int Guard;
}

internal static class Program
{
    private static void WriteShort(ref short r, short v) => r = v;
    private static void WriteSByte(ref sbyte r, sbyte v) => r = v;

    internal static void __GateEntry()
    {
        // --- class instance field, ldflda ---------------------------------
        var h = new Holder();

        h.S = -1;                       // 4-byte slot would become 0xFFFFFFFF
        ref short rs = ref h.S;
        rs = 5;                         // 2-byte store: low half only
        Console.WriteLine($"cls.S={h.S}");                       // 5

        h.SB = -1;
        ref sbyte rsb = ref h.SB;
        rsb = 7;
        Console.WriteLine($"cls.SB={h.SB}");                     // 7

        // through a call boundary (the ref crosses a frame, so nothing at the
        // store site can see the field it addresses).
        h.S = short.MinValue;
        WriteShort(ref h.S, 3);
        Console.WriteLine($"cls.S(call)={h.S}");                 // 3
        h.SB = sbyte.MinValue;
        WriteSByte(ref h.SB, 4);
        Console.WriteLine($"cls.SB(call)={h.SB}");               // 4

        // unsigned/char/bool: the promoted store zero-extends, so these agree
        // either way — assert them anyway so a narrowing regression that got
        // the SIGNEDNESS wrong is caught too.
        h.U = 0xFFFF;
        ref ushort ru = ref h.U;
        ru = 0x1234;
        Console.WriteLine($"cls.U={h.U:X4}");                    // 1234
        h.B = 0xFF;
        ref byte rb = ref h.B;
        rb = 0x21;
        Console.WriteLine($"cls.B={h.B:X2}");                    // 21
        h.C = '￿';
        ref char rc = ref h.C;
        rc = 'q';
        Console.WriteLine($"cls.C={h.C}");                       // q
        h.Bo = true;
        ref bool rbo = ref h.Bo;
        rbo = false;
        Console.WriteLine($"cls.Bo={h.Bo}");                     // False

        // Enum.TryParse writing its `out T` through a ref to a class field
        // whose enum is SIGNED-backed: the 1-byte store must not leave the
        // sign bytes of the previous Neg behind.
        h.E = SByteEnum.Neg;
        Console.WriteLine(Enum.TryParse("Two", out h.E));        // True
        Console.WriteLine($"cls.E={h.E}");                       // Two

        // a neighbouring wide field must be untouched by any of the above.
        h.Guard = unchecked((int)0xDEADBEEF);
        rs = ref h.S;
        rs = -2;
        Console.WriteLine($"cls.S={h.S},guard={h.Guard:X8}");     // -2,DEADBEEF

        // --- static field, ldsflda ---------------------------------------
        Statics.S = -1;
        ref short ss = ref Statics.S;
        ss = 5;
        Console.WriteLine($"st.S={Statics.S}");                  // 5

        Statics.SB = -1;
        ref sbyte ssb = ref Statics.SB;
        ssb = 7;
        Console.WriteLine($"st.SB={Statics.SB}");                // 7

        Statics.S = short.MinValue;
        WriteShort(ref Statics.S, 3);
        Console.WriteLine($"st.S(call)={Statics.S}");            // 3
        Statics.SB = sbyte.MinValue;
        WriteSByte(ref Statics.SB, 4);
        Console.WriteLine($"st.SB(call)={Statics.SB}");          // 4

        Statics.U = 0xFFFF;
        ref ushort su = ref Statics.U;
        su = 0x1234;
        Console.WriteLine($"st.U={Statics.U:X4}");               // 1234
        Statics.C = '￿';
        ref char sc = ref Statics.C;
        sc = 'w';
        Console.WriteLine($"st.C={Statics.C}");                  // w

        Statics.E = SByteEnum.Neg;
        Console.WriteLine(Enum.TryParse("Two", out Statics.E));  // True
        Console.WriteLine($"st.E={Statics.E}");                  // Two

        Statics.Guard = unchecked((int)0xFEEDFACE);
        ss = ref Statics.S;
        ss = -2;
        Console.WriteLine($"st.S={Statics.S},guard={Statics.Guard:X8}");  // -2,FEEDFACE

        // --- spans over a ref to a sub-word field --------------------------
        // A one-element Span<short> over the field addresses the field's real
        // storage; AsBytes must therefore see exactly 2 bytes.
        h.S = -1;
        Span<short> sp = MemoryMarshal.CreateSpan(ref h.S, 1);
        Span<byte> spb = MemoryMarshal.AsBytes(sp);
        Console.WriteLine($"spanbytes={spb.Length}");            // 2
        sp[0] = 0x0102;
        Console.WriteLine($"cls.S(span)={h.S:X4},{spb[0]:X2}{spb[1]:X2}");  // 0102,0201

        // --- sizes visible from the reflection/marshal surface -------------
        // The primitive sizes themselves: Unsafe.SizeOf is the managed storage
        // width, Marshal.SizeOf the marshalled one. Both are 1/2 for sub-word
        // primitives in .NET. These are the emit-time GENERIC spellings, lowered
        // to a C++ sizeof at the call site — the width this layout policy uses.
        // (The `Marshal.SizeOf(Type)` / reflected spellings read a type-info
        // instanceSize instead and are a separate surface; the box payload — and
        // therefore that instanceSize — is deliberately still int32-promoted.)
        Console.WriteLine($"u8={Unsafe.SizeOf<byte>()},{Unsafe.SizeOf<sbyte>()}");     // 1,1
        Console.WriteLine($"u16={Unsafe.SizeOf<short>()},{Unsafe.SizeOf<ushort>()},{Unsafe.SizeOf<char>()}");  // 2,2,2
        Console.WriteLine($"m8={Marshal.SizeOf<byte>()},{Marshal.SizeOf<sbyte>()}");   // 1,1
        Console.WriteLine($"m16={Marshal.SizeOf<short>()},{Marshal.SizeOf<ushort>()}");// 2,2
    }
}
