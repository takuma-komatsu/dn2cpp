using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace IntegralUnionSubset;

internal static class Program
{
    private static int checks;
    private static void Check(string label, bool ok)
    {
        Console.WriteLine("integral-union-" + label + ":" + ok);
        if (!ok) throw new InvalidOperationException(label);
        checks++;
    }
    public static void __GateEntry() => Run();
    public static unsafe void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        checks = 0;
        // Public user-code mirror of the installed field offsets, not private GuidResult execution.
        var x = new GuidMirror { A = 0x12345678, BC = 0xABCD1020, DEFG = 2, HIJK = 3, Style = ParseStyle.All };
        var y = x;
        Check("mirror-equal", EqualityComparer<GuidMirror>.Default.Equals(x, y));
        Check("mirror-hash", x.GetHashCode() == y.GetHashCode());
        y.B = 0;
        Check("alias-different", !x.Equals(y));
        y = x; y.Style = ParseStyle.None;
        Check("enum-different", !x.Equals(y));
        Check("default", default(GuidMirror).Equals(new GuidMirror()));
        var values = new[] { y, x, x };
        Check("index", Array.IndexOf(values, x) == 1);
        Check("last", Array.LastIndexOf(values, x) == 2);
        Check("index-range", Array.IndexOf(values, x, 2, 1) == 2);
        Check("last-range", Array.LastIndexOf(values, x, 1, 2) == 1);
        Check("empty-range", Array.IndexOf(values, x, 3, 0) == -1);
        IList<GuidMirror> segment = new ArraySegment<GuidMirror>(values, 1, 2);
        Check("segment-relative", segment.IndexOf(x) == 0);
        Check("segment-miss", segment.IndexOf(y) == -1);
        Check("nullable", EqualityComparer<GuidMirror?>.Default.Equals(x, x));
        Check("nullable-absent", !EqualityComparer<GuidMirror?>.Default.Equals(x, null));
        object first = x, second = x;
        Check("boxed-equal", first.Equals(second));
        Check("boxed-hash", first.GetHashCode() == second.GetHashCode());
        Check("nested", new Holder(x).Equals(new Holder(x)));
        Check("nested-different", !new Holder(x).Equals(new Holder(y)));
        Check("nested-hash", new Holder(x).GetHashCode() == new Holder(x).GetHashCode());
        var dictionary = new Dictionary<GuidMirror, int>(); dictionary[x] = 7;
        Check("dictionary", dictionary.TryGetValue(x, out int value) && value == 7 && !dictionary.ContainsKey(y));
        var set = new HashSet<GuidMirror>(); set.Add(x);
        Check("hashset", set.Contains(x) && !set.Contains(y) && !set.Add(x));
        GuidMirror pad1 = x, pad2 = x;
        byte* p1 = (byte*)&pad1; byte* p2 = (byte*)&pad2;
        for (int offset = 17; offset < sizeof(GuidMirror); offset++) { p1[offset] = 0x11; p2[offset] = 0x22; }
        Check("tail-padding-exists", sizeof(GuidMirror) > 17);
        Check("padding-ignored", pad1.Equals(pad2));
        Check("padding-hash", pad1.GetHashCode() == pad2.GetHashCode());
        var widths = new WidthAliases { S8 = -1, S16 = short.MinValue, S32 = int.MinValue, S64 = long.MinValue };
        var sameWidths = widths;
        Check("signed-widths", widths.Equals(sameWidths));
        Check("signed-widths-hash", widths.GetHashCode() == sameWidths.GetHashCode());
        Check("enum-alias", widths.Equals(new WidthAliases { S8 = -1, S16 = short.MinValue, S32 = int.MinValue, SignedEnum = E64.Min }));
        sameWidths.U64 = 1;
        Check("unsigned-alias-change", !widths.Equals(sameWidths));
        var enums = new SmallEnums { S8 = E8.Min, U8 = U8.Max, S16 = E16.Min, U16 = U16.Max, S32 = E32.Min, U32 = U32.Max };
        var sameEnums = enums;
        Check("small-enums", enums.Equals(sameEnums));
        Check("small-enums-hash", enums.GetHashCode() == sameEnums.GetHashCode());
        sameEnums.U32 = 0;
        Check("disjoint-enum-change", !enums.Equals(sameEnums));
        var wideEnums = new WideEnums { Signed = E64.Min, Unsigned = U64.Max };
        Check("wide-enums", wideEnums.Equals(wideEnums));
        Check("wide-enums-hash", wideEnums.GetHashCode() == new WideEnums { Signed = E64.Min, Unsigned = U64.Max }.GetHashCode());
        Check("signed-enum-high-bits", !wideEnums.Equals(new WideEnums { Signed = 0, Unsigned = U64.Max }));
        Check("unsigned-enum-high-bits", !wideEnums.Equals(new WideEnums { Signed = E64.Min, Unsigned = (U64)uint.MaxValue }));
        var packed = new PackedIntegral { Tag = 1, Value = int.MinValue };
        var samePacked = packed;
        Check("packed-equal", packed.Equals(samePacked));
        Check("packed-hash", packed.GetHashCode() == samePacked.GetHashCode());
        samePacked.Value = 0;
        Check("packed-different", !packed.Equals(samePacked));
        Console.WriteLine("integral-union-checks:" + checks);
    }
}
[StructLayout(LayoutKind.Explicit)]
internal struct GuidMirror
{
    [FieldOffset(0)] public uint A;
    [FieldOffset(4)] public uint BC;
    [FieldOffset(4)] public ushort B;
    [FieldOffset(6)] public ushort C;
    [FieldOffset(8)] public uint DEFG;
    [FieldOffset(8)] public ushort DE;
    [FieldOffset(8)] public byte D;
    [FieldOffset(10)] public ushort FG;
    [FieldOffset(12)] public uint HIJK;
    [FieldOffset(16)] public ParseStyle Style;
}
internal enum ParseStyle : byte { None, All, AllButOverflow }
[StructLayout(LayoutKind.Explicit, Size = 16)]
internal struct WidthAliases
{
    [FieldOffset(0)] public sbyte S8; [FieldOffset(0)] public byte U8;
    [FieldOffset(2)] public short S16; [FieldOffset(2)] public ushort U16;
    [FieldOffset(4)] public int S32; [FieldOffset(4)] public uint U32;
    [FieldOffset(8)] public long S64; [FieldOffset(8)] public ulong U64;
    [FieldOffset(8)] public E64 SignedEnum;
}
internal enum E8 : sbyte { Min = sbyte.MinValue }
internal enum U8 : byte { Max = byte.MaxValue }
internal enum E16 : short { Min = short.MinValue }
internal enum U16 : ushort { Max = ushort.MaxValue }
internal enum E32 : int { Min = int.MinValue }
internal enum U32 : uint { Max = uint.MaxValue }
internal enum E64 : long { Min = long.MinValue }
internal enum U64 : ulong { Max = ulong.MaxValue }
[StructLayout(LayoutKind.Explicit, Size = 16)]
internal struct SmallEnums
{
    [FieldOffset(0)] public E8 S8; [FieldOffset(1)] public U8 U8;
    [FieldOffset(2)] public E16 S16; [FieldOffset(4)] public U16 U16;
    [FieldOffset(8)] public E32 S32; [FieldOffset(12)] public U32 U32;
}
[StructLayout(LayoutKind.Explicit, Size = 16)]
internal struct WideEnums { [FieldOffset(0)] public E64 Signed; [FieldOffset(8)] public U64 Unsigned; }
internal readonly struct Holder { public readonly GuidMirror Value; public Holder(GuidMirror value) { Value = value; } }

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 5)]
internal struct PackedIntegral { [FieldOffset(0)] public byte Tag; [FieldOffset(1)] public int Value; }
