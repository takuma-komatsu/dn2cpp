#nullable enable
// ECMA-335 unbox compatibility (III.4.32 verifier-assignable-to) beyond the exact
// type match, diffed against the real-.NET oracle:
//
//   - boxed enum -> its underlying primitive,
//   - boxed underlying primitive -> enum over it,
//   - enum -> enum sharing one underlying type,
//   - Unsafe.Unbox<int> on a boxed int-underlying enum (the same dn2cpp_unbox
//     lowering, so one runtime fix closes both).
//
// And what stays REJECTED, decided by the oracle rather than by hand:
//   - width mismatch (enum : ushort as int) and signedness mismatch (as short),
//   - pure primitive cross-unbox at equal width (int ↔ uint) — real .NET's
//     CastHelpers rejects it even though the bit pattern would fit,
//   - the isinst asymmetry: `boxedEnum is ushort` is false even though
//     `(ushort)boxedEnum` succeeds — the widening is unbox-only.
//
// All widths that matter to the box payload convention are covered: ushort
// (int32 payload), int (int32), and long/ulong (int64 payload).
using System;
using System.Runtime.CompilerServices;

namespace EnumUnboxCompatSubset
{
    internal enum CompoundU16 : ushort { None = 0, Atp = 3, Glucose = 700 }

    internal enum OtherU16 : ushort { X = 0, Y = 3, Z = 9000 }

    internal enum PlainInt { A = 0, B = 41, C = 1234567 }

    internal enum LongMode : long { L0 = 0, LBig = 5_000_000_000L }

    internal enum ULongMode : ulong { U0 = 0, UBig = 18_000_000_000_000_000_000UL }

    internal static class Program
    {
        private static string Try(Func<object> f)
        {
            try
            {
                return f().ToString() ?? "<null>";
            }
            catch (InvalidCastException)
            {
                return "ICE";
            }
        }

        internal static int __GateEntry()
        {
            // Boxed enum -> underlying primitive (both directions), ushort width —
            // the exact Thrive path.
            object boxedEnum = CompoundU16.Glucose;
            Console.WriteLine($"u16_from_enum: {Try(() => (ushort)boxedEnum)}");
            object boxedU16 = (ushort)3;
            Console.WriteLine($"enum_from_u16: {Try(() => (CompoundU16)boxedU16)}");

            // Enum -> enum over the same underlying type.
            Console.WriteLine($"enum_from_enum: {Try(() => (OtherU16)boxedEnum)}");

            // Width / signedness mismatches stay rejected: enum : ushort refuses
            // int (wider) and short (same width, other sign).
            Console.WriteLine($"i32_from_u16enum: {Try(() => (int)boxedEnum)}");
            Console.WriteLine($"i16_from_u16enum: {Try(() => (short)boxedEnum)}");

            // Pure primitive cross-unbox at equal width — oracle-decided.
            object boxedInt = 1;
            object boxedUInt = 2u;
            Console.WriteLine($"u32_from_i32: {Try(() => (uint)boxedInt)}");
            Console.WriteLine($"i32_from_u32: {Try(() => (int)boxedUInt)}");

            // The signedness rule holds through an enum too: enum : int refuses uint.
            object boxedPlain = PlainInt.C;
            Console.WriteLine($"i32_from_i32enum: {Try(() => (int)boxedPlain)}");
            Console.WriteLine($"u32_from_i32enum: {Try(() => (uint)boxedPlain)}");

            // 64-bit payloads round-trip both ways at both signs.
            object boxedLongEnum = LongMode.LBig;
            object boxedULongEnum = ULongMode.UBig;
            Console.WriteLine($"i64_from_i64enum: {Try(() => (long)boxedLongEnum)}");
            Console.WriteLine($"u64_from_u64enum: {Try(() => (ulong)boxedULongEnum)}");
            Console.WriteLine($"i64enum_from_i64: {Try(() => (LongMode)(object)5_000_000_000L)}");
            Console.WriteLine($"u64_from_i64enum: {Try(() => (ulong)boxedLongEnum)}");

            // isinst stays exact — the CLR's unbox/isinst asymmetry.
            Console.WriteLine($"is_u16: {boxedEnum is ushort} is_enum: {boxedEnum is CompoundU16} "
                + $"is_other: {boxedEnum is OtherU16} u16_is_enum: {boxedU16 is CompoundU16}");

            // Unsafe.Unbox<T> lowers through the same dn2cpp_unbox — a ref into the
            // box's payload, writable, under enum/underlying identity.
            object mutBox = PlainInt.B;
            Unsafe.Unbox<int>(mutBox) = 42;
            Console.WriteLine($"unsafe_unbox: {(PlainInt)mutBox} {Try(() => (int)mutBox)}");
            return 0;
        }
    }
}
