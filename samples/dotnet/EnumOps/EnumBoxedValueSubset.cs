#nullable enable
// A System.Enum-typed (boxed) receiver reaching Enum's own value-comparison overrides —
// Equals(object), GetHashCode(), CompareTo(object) — plus the IConvertible/IComparable/
// IFormattable interface-cast forms and Enum's private GetValue(). All of them switch on the
// bodyless InternalGetCorElementType FCall in real CoreLib IL, so all are BodyReplace'd onto
// runtime helpers that read the boxed payload at its underlying width. Every storage width
// those helpers discriminate is covered (byte / sbyte / short / int / long / ulong), plus the
// List<System.Enum> path. Real System.Private.CoreLib (-r) -> run vs .NET, exact diff.
//
// The hashed-container and generic-box sections are regression cover for the MODEL, not for a
// container: System.Enum's metadata base is System.ValueType, but the CLR calls it an abstract
// REFERENCE type (ECMA-335 II.13, typeof(System.Enum).IsValueType is false) and dn2cpp must
// agree. Model it as a value type and two things go silently wrong — a devirt fold passes the
// ADDRESS of the reference slot where the boxed receiver belongs, and `box !!T` at
// T=System.Enum wraps a second box around an already-boxed reference.
using System;
using System.Collections.Generic;

namespace EnumBoxedValueSubset
{
    internal enum ByteMode : byte { A = 1, B = 2, C = 200 }

    internal enum SByteMode : sbyte { N = -5, Z = 0, P = 7 }

    internal enum ShortMode : short { S1 = -300, S2 = 300 }

    internal enum IntMode { I0, I1, I2 }                 // int underlying (default)

    internal enum LongMode : long { L0 = 0, LBig = 5_000_000_000L }

    internal enum ULongMode : ulong { U0 = 0, UBig = 18_000_000_000_000_000_000UL }

    internal static class Program
    {
        // Boxed as System.Enum: e.Equals / e.GetHashCode / e.CompareTo dispatch to
        // System.Enum's real overrides (not the value type's), each reaching
        // InternalGetCorElementType().
        private static void Probe(string tag, Enum a, Enum b)
        {
            // CompareTo here is the public System.Enum.CompareTo(object) — a DIRECT callvirt
            // on a System.Enum receiver (devirtualized to the BodyReplace shim). The
            // interface-cast forms are a different lowering, asserted by
            // ExerciseEnumInterfaceCasts / ExerciseEnumIConvertible below.
            Console.WriteLine($"{tag}: eq(a,a)={a.Equals(a)} eq(a,b)={a.Equals(b)} "
                + $"hashEq(a,a)={a.GetHashCode() == a.GetHashCode()} "
                + $"cmp(a,b)={Math.Sign(a.CompareTo(b))} "
                + $"cmp(b,a)={Math.Sign(b.CompareTo(a))} "
                + $"cmp(a,a)={a.CompareTo(a)}");
            Console.WriteLine($"{tag}: hash(a)={a.GetHashCode()} hash(b)={b.GetHashCode()}");
        }

        internal static int __GateEntry()
        {
            Probe("byte", ByteMode.A, ByteMode.C);
            Probe("sbyte", SByteMode.N, SByteMode.P);
            Probe("short", ShortMode.S1, ShortMode.S2);
            Probe("int", IntMode.I0, IntMode.I2);
            Probe("long", LongMode.L0, LongMode.LBig);
            Probe("ulong", ULongMode.U0, ULongMode.UBig);

            Enum x = IntMode.I1;
            Enum y = LongMode.LBig;
            Console.WriteLine($"cross: {x.Equals(y)} {y.Equals((Enum)IntMode.I1)}");

            // List<System.Enum>: Contains/IndexOf go through EqualityComparer<Enum>.Default ->
            // dn2cpp_object_equals, and the list's Enumerator (a struct with a System.Enum
            // field) exercises the synthesized ValueType field walk over a boxed-reference field.
            var list = new List<Enum> { ByteMode.B, IntMode.I2, LongMode.LBig };
            Console.WriteLine($"list: contains_B={list.Contains(ByteMode.B)} "
                + $"contains_A={list.Contains(ByteMode.A)} idx_I2={list.IndexOf(IntMode.I2)}");

            HashedContainers();
            GenericBox<Enum>("enum", IntMode.I2);
            GenericBox<object>("object", IntMode.I2);   // the control: T already a reference type

            // The cut ⟹ route assert for Enum's private GetValue(), reached through
            // ((IConvertible)e).ToInt32(null) — whose real body is
            // `Convert.ToInt32(GetValue(), CurrentCulture)`. GetValue is BodyReplace'd
            // (CoreIntrinsics.BrEnumInstanceFormat) to box the underlying primitive under the
            // underlying's type-info, which closes the whole IConvertible.To* family in one row.
            ExerciseEnumIConvertible();
            ExerciseEnumInterfaceCasts();
            return 0;
        }

        // The hashed containers over a System.Enum key (see the file header). Keys of three
        // different underlying widths, so a hash that reads the slot rather than the boxed
        // payload cannot pass by coincidence; the get_Item miss is asserted too, because a
        // KeyNotFoundException on a key that IS present is the shape the failure surfaces as.
        private static void HashedContainers()
        {
            var hs = new HashSet<Enum>();
            Console.WriteLine($"hs: add1={hs.Add(IntMode.I1)} add1again={hs.Add(IntMode.I1)} "
                + $"addB={hs.Add(ByteMode.B)} count={hs.Count}");
            Console.WriteLine($"hs: contains1={hs.Contains(IntMode.I1)} "
                + $"containsB={hs.Contains(ByteMode.B)} containsC={hs.Contains(ByteMode.C)}");
            Console.WriteLine($"hs: remove1={hs.Remove(IntMode.I1)} count={hs.Count}");

            var d = new Dictionary<Enum, string>();
            d[IntMode.I2] = "i2";
            d[LongMode.LBig] = "big";
            d[ByteMode.C] = "c";
            d[IntMode.I2] = "i2b";              // an overwrite, not a second entry
            Console.WriteLine($"d: count={d.Count} i2={d[IntMode.I2]} big={d[LongMode.LBig]}");
            Console.WriteLine($"d: tryC={d.TryGetValue(ByteMode.C, out var vc)}/{vc} "
                + $"tryA={d.TryGetValue(ByteMode.A, out var va)}/{va ?? "null"} "
                + $"hasKeyBig={d.ContainsKey(LongMode.LBig)}");
            try
            {
                Console.WriteLine($"d: missing={d[ByteMode.A]}");
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("d: missing=KeyNotFound");
            }
        }

        // `object o = item` inside a generic method is `box !!T`, and for a reference-typed T
        // the CLR makes it a no-op. A second box around an already-boxed enum is visible in all
        // four answers below; the T=object call is the control that pins the expected shape.
        private static void GenericBox<T>(string tag, T item)
        {
            object o = item!;
            Console.WriteLine($"gbox {tag}: same={ReferenceEquals(o, item)} type={o.GetType().Name} "
                + $"eq={o.Equals(item)} hashEq={o.GetHashCode() == item!.GetHashCode()}");
        }

        // Each ((IConvertible)e).ToXxx dispatches through the shared System.Enum interface map
        // on the boxed enum's base type-info, landing on Enum's real
        // System.IConvertible.ToXxx body (Convert.ToXxx(GetValue(), …)).
        private static void ExerciseEnumIConvertible()
        {
            // Widths chosen to avoid Convert overflow (long via ToInt64, ulong via ToUInt64).
            Console.WriteLine($"gv_byte: i32={Convert.ToInt32((Enum)ByteMode.C)} "
                + $"i64={Convert.ToInt64((Enum)ByteMode.C)} ic={((IConvertible)ByteMode.C).ToInt32(null)}");
            Console.WriteLine($"gv_sbyte: i32={Convert.ToInt32((Enum)SByteMode.N)} "
                + $"dbl={Convert.ToDouble((Enum)SByteMode.N)} ic64={((IConvertible)SByteMode.N).ToInt64(null)}");
            Console.WriteLine($"gv_short: i32={Convert.ToInt32((Enum)ShortMode.S1)} "
                + $"ic16={((IConvertible)ShortMode.S1).ToInt16(null)}");
            Console.WriteLine($"gv_int: i32={Convert.ToInt32((Enum)IntMode.I2)} "
                + $"ic32={((IConvertible)IntMode.I2).ToInt32(null)}");
            Console.WriteLine($"gv_long: i64={Convert.ToInt64((Enum)LongMode.LBig)} "
                + $"ic64={((IConvertible)LongMode.LBig).ToInt64(null)}");
            Console.WriteLine($"gv_ulong: u64={Convert.ToUInt64((Enum)ULongMode.UBig)} "
                + $"ic_u64={((IConvertible)ULongMode.UBig).ToUInt64(null)}");
        }

        // The IComparable / IFormattable interface-cast forms, plus the object-hiding shape —
        // a boxed enum found behind `object` by a runtime cast, which is the
        // Comparer<object>.Default / TypeDescriptor route the castclass trigger wires.
        private static void ExerciseEnumInterfaceCasts()
        {
            Console.WriteLine($"icast_cmp: bc={Math.Sign(((IComparable)ByteMode.B).CompareTo(ByteMode.C))} "
                + $"cb={Math.Sign(((IComparable)ByteMode.C).CompareTo(ByteMode.B))} "
                + $"bb={((IComparable)ByteMode.B).CompareTo(ByteMode.B)}");
            Console.WriteLine($"icast_fmt: g={((IFormattable)IntMode.I2).ToString("G", null)} "
                + $"d={((IFormattable)LongMode.LBig).ToString("D", null)} "
                + $"x={((IFormattable)ByteMode.C).ToString("X", null)}");
            Console.WriteLine($"icast_tc: {((IConvertible)ShortMode.S1).GetTypeCode()} "
                + $"{((IConvertible)ULongMode.UBig).GetTypeCode()}");
            object hidden = ShortMode.S1;
            Console.WriteLine($"icast_obj: is_conv={hidden is IConvertible} is_cmp={hidden is IComparable} "
                + $"is_fmt={hidden is IFormattable} i16={((IConvertible)hidden).ToInt16(null)} "
                + $"cmp={Math.Sign(((IComparable)hidden).CompareTo(ShortMode.S2))} "
                + $"fmt={((IFormattable)hidden).ToString("D", null)}");
            // The two NON-ordering arms of the same interface slot, neither reachable through
            // the direct System.Enum.CompareTo(object) form the Probe sections use: a null
            // target sorts first (this > null -> 1), and a DIFFERENT enum type is an
            // ArgumentException rather than an order. Only the exception's TYPE is printed —
            // dn2cpp's runtime-raised ArgumentException carries no message.
            string mismatch;
            try
            {
                _ = ((IComparable)ByteMode.B).CompareTo(IntMode.I1);
                mismatch = "no-throw";
            }
            catch (ArgumentException)
            {
                mismatch = "AE";
            }
            Console.WriteLine($"icast_cmp_edge: null={((IComparable)ByteMode.B).CompareTo(null)} "
                + $"mismatch={mismatch}");
        }
    }
}
