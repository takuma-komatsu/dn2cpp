#nullable enable
// the non-generic Enum.ToString() / GetTypeCode() / GetValuesAsUnderlyingType
// mouth the fresh Thrive measure reaches. A System.Enum-typed receiver (not
// `object`) makes the C# compiler emit `callvirt System.Enum::ToString()` — a token
// the boxed-object path never produces — whose real body reaches ToStringInlined ->
// GetEnumInfo -> GetEnumValuesAndNames (an InternalCall with no IL) and the
// RuntimeType-cache cascade. The exact Thrive shape is EnumConverter.ConvertTo over a
// List<System.Enum> element. dn2cpp cuts that body across every reach mouth
// (call-site, class-virtual, interface, constrained) and routes ToString to
// dn2cpp_object_tostring / dn2cpp_enum_format, GetTypeCode to dn2cpp_type_get_type_code,
// GetValuesAsUnderlyingType to dn2cpp_enum_get_values_underlying. Diffed EXACTLY vs
// real .NET (corelib_diff_gate). Real System.Private.CoreLib (-r).
using System;
using System.Collections.Generic;
namespace EnumToStringVirtualSubset;

enum ByteMode : byte { Off = 0, On = 1, Auto = 200 }

[Flags]
enum IntPerms { None = 0, Read = 1, Write = 2, Exec = 4 }

enum SignedCode : int { NegTwo = -2, NegOne = -1, Zero = 0, One = 1 }

enum LongCode : long { Alpha = 1, Beta = 2, Gamma = 3 }

enum ULongBig : ulong { Lo = 1, Hi = 18446744073709551615UL }

class Program
{
    internal static void __GateEntry()
    {
        // A heterogeneous List<System.Enum> — the exact EnumConverter shape: each
        // element is a boxed concrete enum reached through a System.Enum-typed slot,
        // so `.ToString()` binds System.Enum::ToString (not Object::ToString).
        var items = new List<System.Enum>
        {
            ByteMode.On,
            ByteMode.Auto,
            (ByteMode)9,               // undefined value
            IntPerms.Read | IntPerms.Exec,
            IntPerms.None,
            (IntPerms)0,
            SignedCode.NegTwo,
            (SignedCode)7,             // undefined
            LongCode.Beta,
            ULongBig.Hi,
        };
        foreach (System.Enum e in items)
            Console.WriteLine("vtos: " + e.ToString());

        // ToString(format) overloads on a System.Enum-typed receiver.
        System.Enum a = ByteMode.Auto;
        System.Enum f = IntPerms.Read | IntPerms.Write;
        System.Enum s = SignedCode.NegTwo;
        System.Enum g = LongCode.Gamma;
        System.Enum u = ULongBig.Hi;
        Console.WriteLine("fmt-G: " + a.ToString("G"));
        Console.WriteLine("fmt-D: " + a.ToString("D"));
        Console.WriteLine("fmt-X: " + a.ToString("X"));
        Console.WriteLine("fmt-F: " + a.ToString("F"));
        Console.WriteLine("fmt-null: " + a.ToString((string)null));
        Console.WriteLine("fmt-flags-G: " + f.ToString("G"));
        Console.WriteLine("fmt-flags-F: " + f.ToString("F"));
        Console.WriteLine("fmt-flags-D: " + f.ToString("D"));
        Console.WriteLine("fmt-flags-X: " + f.ToString("X"));
        Console.WriteLine("fmt-signed-D: " + s.ToString("D"));
        Console.WriteLine("fmt-signed-X: " + s.ToString("X"));
        Console.WriteLine("fmt-long-X: " + g.ToString("X"));
        Console.WriteLine("fmt-ulong-D: " + u.ToString("D"));
        Console.WriteLine("fmt-ulong-X: " + u.ToString("X"));

        // The two [Obsolete] overloads that carry an IFormatProvider — reached as DIRECT
        // virtual calls on a System.Enum-typed receiver (the provider is ignored — enum
        // formatting is culture-independent). Enums carry no C++ vtable, so these
        // virtual-final slots are devirtualized to the BodyReplace shim.
#pragma warning disable CS0618 // Enum.ToString(IFormatProvider) overloads are [Obsolete]; still live slots.
        Console.WriteLine("fmt-prov: " + a.ToString((IFormatProvider)null));
        Console.WriteLine("s2-D: " + f.ToString("D", null));
        Console.WriteLine("s2-X: " + a.ToString("X", null));
        Console.WriteLine("s2-G: " + g.ToString("G", null));
#pragma warning restore CS0618
        // NOTE: this section stays on the DIRECT (devirtualized) forms on purpose — they are a
        // different lowering from the interface-cast forms, and both need coverage. The
        // interface-cast forms are asserted by
        // EnumBoxedValueSubset.ExerciseEnumInterfaceCasts / ExerciseEnumIConvertible; do not
        // duplicate them here.

        // GetTypeCode() on a System.Enum-typed receiver (-> underlying's TypeCode). A
        // virtual-final slot, devirtualized to the BodyReplace shim like the ToString forms.
        Console.WriteLine("tc-byte: " + a.GetTypeCode());
        Console.WriteLine("tc-int: " + f.GetTypeCode());
        Console.WriteLine("tc-int-signed: " + s.GetTypeCode());
        Console.WriteLine("tc-long: " + g.GetTypeCode());
        Console.WriteLine("tc-ulong: " + u.GetTypeCode());

        // Enum.GetValuesAsUnderlyingType(Type) — an Array of the underlying primitive.
        DumpUnderlying("byte", typeof(ByteMode));
        DumpUnderlying("flags", typeof(IntPerms));
        DumpUnderlying("signed", typeof(SignedCode));
        DumpUnderlying("long", typeof(LongCode));
        DumpUnderlying("ulong", typeof(ULongBig));
    }

    private static void DumpUnderlying(string tag, Type t)
    {
        Array arr = Enum.GetValuesAsUnderlyingType(t);
        Console.WriteLine("gvut-" + tag + "-type: " + arr.GetType().FullName + " len=" + arr.Length);
        // Index with Array.GetValue rather than foreach: the element type is a runtime
        // Type (the underlying primitive), so the runtime-typed array need not carry the
        // SZArray-enumerator dispatch map a foreach would require.
        for (int i = 0; i < arr.Length; i++)
            Console.WriteLine("gvut-" + tag + "-val: " + arr.GetValue(i));
    }
}
