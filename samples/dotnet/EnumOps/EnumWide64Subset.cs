#nullable enable
// Every lowering that reads an enum's declared member table, driven with a
// long/ulong-underlying enum whose constants do not survive an int32 model width. Each
// block names the reader it pins, because the failure they share is silent: a truncation
// both misnames a member and DEDUPES two distinct members onto one C++ case label, so the
// wrong answer is a plausible one.
//
// Readers covered, in order: the interpolation hole (FormatInterpolationHole), the generic
// Enum.GetName/IsDefined/Parse/TryParse/GetNames lowering (TranslateEnumReflection), the
// packed E[] materialization (EmitPackedEnumValuesArray), the boxed Object.ToString slot
// (EnumToStringFn), the non-generic Type-driven statics over the emitted enummembers_
// table, Convert.ToString's own boxed-enum arm, and Enum.Format / ISpanFormattable.
using System;
namespace EnumWide64Subset;

enum LongE : long { Zero = 0, Small = 7, Big = 5000000000L, Neg = -5000000000L, Max = long.MaxValue, Min = long.MinValue }

[Flags]
enum LongF : ulong { None = 0, A = 1, B = 2, High = 1UL << 40, Top = 1UL << 63 }

enum ULongE : ulong { Zero = 0, Mid = 4294967296UL, Max = ulong.MaxValue }

// The other six underlyings, for section 8 alone: the numeric range Enum.Parse enforces
// is the UNDERLYING's, so the subject is the whole width axis, not just the 64-bit end.
enum ByteU : byte { Zero = 0, Two = 2 }
enum SByteE : sbyte { Zero = 0, Two = 2 }
enum ShortE : short { Zero = 0, Two = 2 }
enum UShortE : ushort { Zero = 0, Two = 2 }
enum IntE : int { Zero = 0, Two = 2 }
enum UIntE : uint { Zero = 0, Two = 2 }

class Program
{
    // Launders the Type so the call site carries no ldtoken the transpiler can read.
    private static Type Hide(Type t) => t;

    internal static void __GateEntry()
    {
        // --- 1. interpolation hole (FormatInterpolationHole) ---
        LongE b = LongE.Big;
        Console.WriteLine($"hole plain={b} D={b:D} X={b:X}");
        Console.WriteLine($"hole neg={LongE.Neg} max={LongE.Max} min={LongE.Min}");
        Console.WriteLine($"hole undef={(LongE)5000000001L}");
        LongF f = LongF.A | LongF.High;
        Console.WriteLine($"hole flags={f} top={LongF.Top} none={LongF.None}");
        Console.WriteLine($"hole umid={ULongE.Mid} umax={ULongE.Max}");

        // --- 2. generic Enum statics (TranslateEnumReflection) ---
        Console.WriteLine("GetName(Big)=" + Enum.GetName<LongE>(LongE.Big));
        Console.WriteLine("GetName(Neg)=" + Enum.GetName<LongE>(LongE.Neg));
        Console.WriteLine("GetName(undef)=" + (Enum.GetName<LongE>((LongE)5000000001L) ?? "<null>"));
        Console.WriteLine("IsDefined(Big)=" + Enum.IsDefined<LongE>(LongE.Big));
        Console.WriteLine("IsDefined(705032704)=" + Enum.IsDefined<LongE>((LongE)705032704L));
        Console.WriteLine("IsDefined(Mid)=" + Enum.IsDefined<ULongE>(ULongE.Mid));
        Console.WriteLine("Parse(Big)=" + (long)Enum.Parse<LongE>("Big"));
        Console.WriteLine("Parse(Min)=" + (long)Enum.Parse<LongE>("Min"));
        Console.WriteLine("TryParse(Neg)=" + Enum.TryParse<LongE>("Neg", out var tv) + " " + (long)tv);
        Console.WriteLine("Parse(num)=" + (long)Enum.Parse<LongE>("5000000000"));
        Console.WriteLine("GetNames=" + string.Join(",", Enum.GetNames<LongE>()));

        // --- 3. Enum.GetValues<T> / GetValues(typeof(E)) (EmitPackedEnumValuesArray) ---
        foreach (var v in Enum.GetValues<LongE>())
            Console.WriteLine("gv " + v + " " + (long)v);
        foreach (LongE v in (LongE[])Enum.GetValues(typeof(LongE)))
            Console.WriteLine("gvt " + v + " " + (long)v);
        foreach (var v in Enum.GetValues<ULongE>())
            Console.WriteLine("gvu " + v + " " + (ulong)v);

        // --- 4. boxed ToString (EnumToStringFn) ---
        object ob = LongE.Big;
        Console.WriteLine("boxed plain=" + ob.ToString());
        object of = LongF.A | LongF.High;
        Console.WriteLine("boxed flags=" + of.ToString());
        object ot = LongF.Top;
        Console.WriteLine("boxed top=" + ot.ToString());
        Console.WriteLine("value ToString=" + LongE.Neg.ToString() + " " + LongF.High.ToString());

        // --- 5. non-generic Type-driven statics (runtime table) ---
        // Mid and 705032704 are the discriminating inputs: each ties on the low word
        // with a DIFFERENT member, so a narrowed lookup answers plausibly and wrongly.
        Console.WriteLine("nGetName=" + (Enum.GetName(typeof(LongE), LongE.Big) ?? "<null>")
            + " " + (Enum.GetName(typeof(ULongE), ULongE.Mid) ?? "<null>"));
        Console.WriteLine("nIsDefined=" + Enum.IsDefined(typeof(LongE), LongE.Big)
            + " " + Enum.IsDefined(typeof(LongE), (LongE)705032704L));
        Console.WriteLine("nParse=" + (long)(LongE)Enum.Parse(typeof(LongE), "Big"));
        Console.WriteLine("nTryParse=" + Enum.TryParse(typeof(LongE), "Neg", out object? nv) + " " + (long)(LongE)nv!);
        Console.WriteLine("nNames=" + string.Join(",", Enum.GetNames(typeof(LongE))));
        Console.WriteLine("nFormat=" + Enum.Format(typeof(LongE), LongE.Big, "G")
            + " " + Enum.Format(typeof(LongE), LongE.Neg, "D")
            + " " + Enum.Format(typeof(LongF), LongF.A | LongF.High, "F"));
        // A RUNTIME-chosen Type: no ldtoken reaches the call, so this takes the boxed
        // object[] route (dn2cpp_enum_get_values_boxed) rather than the packed one.
        foreach (object o in Enum.GetValues(Hide(typeof(ULongE))))
            Console.WriteLine("nValues " + o + " " + (ulong)(ULongE)o);
        Console.WriteLine("nUnderlying=" + string.Join(",", (ulong[])Enum.GetValuesAsUnderlyingType(typeof(ULongE))));

        // --- 6. Convert's own boxed-enum reader (dn2cpp_box_to_string). ChangeType is the
        // mouth that reaches it — Convert.ToString goes through the type-info tostring slot.
        // Mid is the case that discriminates: its low 32 bits collide with Zero's.
        Console.WriteLine("cvt=" + Convert.ToString(LongE.Big) + " " + Convert.ToString(ULongE.Mid));
        Console.WriteLine("cvtChange=" + Convert.ChangeType(LongE.Neg, typeof(string))
            + " " + Convert.ChangeType(ULongE.Mid, typeof(string))
            + " " + Convert.ChangeType(LongF.Top, typeof(string)));

        // --- 7. instance ToString(format) / ISpanFormattable (dn2cpp_enum_format) ---
        Console.WriteLine("fmt=" + LongE.Big.ToString("D") + " " + LongE.Big.ToString("X")
            + " " + LongE.Min.ToString("G") + " " + (LongF.A | LongF.High).ToString("F"));
        Span<char> dst = stackalloc char[64];
        ((ISpanFormattable)LongE.Big).TryFormat(dst, out int wr, default, null);
        Console.WriteLine("span=" + new string(dst[..wr]));

        // --- 8. Enum.Parse's numeric token ranges are the UNDERLYING's, not the model's,
        // so this block spans all eight underlyings and not only the 64-bit pair. 300 fits
        // the int32 every sub-64-bit enum rides and still overflows `: byte`; 4294967295
        // does NOT fit it and is a valid `: uint`. The two failures are different exceptions
        // in real .NET — an out-of-range decimal is OverflowException, anything that is not a
        // decimal at all falls through to the name path and is ArgumentException — and both
        // generic and Type-driven forms answer alike. A negative sign is accepted by an
        // unsigned underlying only on zero, which is why "-0" is a value and "-1" an overflow.
        Range<ByteU>("255"); Range<ByteU>("256"); Range<ByteU>("300"); Range<ByteU>("-1"); Range<ByteU>("-0");
        Range<SByteE>("127"); Range<SByteE>("128"); Range<SByteE>("-128"); Range<SByteE>("-129");
        Range<ShortE>("32767"); Range<ShortE>("32768"); Range<ShortE>("-32768"); Range<ShortE>("-32769");
        Range<UShortE>("65535"); Range<UShortE>("65536"); Range<UShortE>("-1");
        Range<IntE>("2147483647"); Range<IntE>("2147483648"); Range<IntE>("-2147483649");
        Range<UIntE>("4294967295"); Range<UIntE>("4294967296"); Range<UIntE>("2147483648"); Range<UIntE>("-1");
        Range<LongE>("9223372036854775807"); Range<LongE>("9223372036854775808"); Range<LongE>("-9223372036854775809");
        Range<ULongE>("18446744073709551615"); Range<ULongE>("18446744073709551616"); Range<ULongE>("9223372036854775808"); Range<ULongE>("-1");
        // A magnitude past uint64 is still a decimal, so still an overflow; leading zeros
        // are not magnitude; a non-decimal stays the name path's ArgumentException.
        Range<ByteU>("00000000000000000000255"); Range<ByteU>("00000000000000000000300");
        Range<ByteU>("999999999999999999999999999999"); Range<ByteU>("300abc"); Range<ByteU>("A,300");
        Range<ByteU>(" 300 "); Range<ByteU>("+300");

        // --- 9. The boxed payload's WIDTH read off a run-time type handle, which is what
        // sizes the allocation: Activator/GetUninitializedObject size a box from the enum's
        // type-info, and Enum.ToObject boxes through the handle. A 64-bit enum's payload is
        // 8 bytes, so a 4-byte model here loses the high word.
        object act = Activator.CreateInstance(typeof(LongE));
        Console.WriteLine("act=" + act + " " + (long)(LongE)act);
        object uni = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ULongE));
        Console.WriteLine("uninit=" + uni + " " + (ulong)(ULongE)uni);
        Console.WriteLine("toobj=" + Enum.ToObject(Hide(typeof(LongE)), long.MinValue)
            + " " + Enum.ToObject(Hide(typeof(ULongE)), ulong.MaxValue)
            + " " + Enum.ToObject(Hide(typeof(ByteU)), (byte)200));
    }

    // Both Parse forms plus both TryParse forms of one token, printing the exception TYPE
    // (the messages are not modeled — AGENTS.md's intrinsic contract) and the parsed value
    // through the underlying so a truncation cannot hide behind a member name.
    private static void Range<T>(string s) where T : struct, Enum
    {
        string p;
        try { p = Str(Enum.Parse<T>(s)); }
        catch (Exception e) { p = e.GetType().Name; }
        string np;
        try { np = Str((T)Enum.Parse(typeof(T), s)); }
        catch (Exception e) { np = e.GetType().Name; }
        bool ok = Enum.TryParse<T>(s, out T v);
        bool nok = Enum.TryParse(typeof(T), s, out object? nv);
        Console.WriteLine($"rng {typeof(T).Name} \"{s}\" P={p} nP={np} T={(ok ? Str(v) : "false")} nT={(nok ? Str((T)nv!) : "false")}");
    }

    // "D" renders the UNDERLYING decimal, so a truncated parse cannot hide behind a
    // member name the way a plain ToString() would.
    private static string Str<T>(T v) where T : struct, Enum => Enum.Format(typeof(T), v, "D");
}
