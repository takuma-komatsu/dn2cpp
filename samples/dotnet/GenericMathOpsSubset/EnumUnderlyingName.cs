using System;

namespace GenericMathEnumName;

// Enum.GetName / ToString / GetNames over enums with non-int underlying types (byte,
// long, ulong). In real .NET these drive Enum.GetNameInlined / Enum.AreSequentialFromZero,
// which use uint/ulong.CreateTruncating<TUnderlying>, i.e. the generic-math conversion
// arm. dn2cpp lowers the generic Enum.*<T> statics inline from the
// per-enum (name, value) table (TranslateEnumReflection), so the output must still diff
// exact vs real .NET across the underlying-type widths.
internal static class EnumUnderlyingName
{
    enum ByteFlags : byte { None = 0, A = 1, B = 2, C = 200 }
    enum LongState : long { Lo = -5, Zero = 0, Hi = 9000000000L }
    enum ULongBig : ulong { X = 0, Y = 1, Big = 18000000000000000000UL }

    internal static void __GateEntry()
    {
        Console.WriteLine("== Enum.GetName over non-int underlying types ==");
        Console.WriteLine($"byte C   = {Enum.GetName(ByteFlags.C)}");            // C
        Console.WriteLine($"byte 200 = {Enum.GetName((ByteFlags)200)}");         // C
        Console.WriteLine($"byte 5   = {Enum.GetName((ByteFlags)5) ?? "null"}"); // null
        Console.WriteLine($"long Hi  = {Enum.GetName(LongState.Hi)}");           // Hi
        Console.WriteLine($"long Lo  = {Enum.GetName(LongState.Lo)}");           // Lo
        Console.WriteLine($"ulong Big= {Enum.GetName(ULongBig.Big)}");           // Big

        Console.WriteLine("== Enum.ToString / GetNames ==");
        Console.WriteLine($"byte C.ToString  = {ByteFlags.C}");                  // C
        Console.WriteLine($"long Hi.ToString = {LongState.Hi}");                 // Hi
        Console.WriteLine($"ulong Big.ToStr  = {ULongBig.Big}");                 // Big
        Console.WriteLine($"byte names  = {string.Join(",", Enum.GetNames<ByteFlags>())}");
        Console.WriteLine($"long names  = {string.Join(",", Enum.GetNames<LongState>())}");
        Console.WriteLine($"ulong names = {string.Join(",", Enum.GetNames<ULongBig>())}");
    }
}
