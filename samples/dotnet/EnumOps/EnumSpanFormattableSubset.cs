#nullable disable
using System;

namespace EnumSpanFormattableSubset
{
    // The boxed-enum ISpanFormattable::TryFormat path the fresh Thrive measure reaches
    // (EnumConverter.ConvertTo -> Enum.System.ISpanFormattable.TryFormat ->
    // TryFormatPrimitiveDefault -> GetEnumInfo -> GetEnumValuesAndNames, an InternalCall).
    // dn2cpp cuts that body and routes the CONSTRAINED mouth — a generic method whose
    // type parameter is constrained to ISpanFormattable and calls TryFormat on it, i.e.
    // `constrained. <enum> callvirt ISpanFormattable::TryFormat` — by boxing the enum and
    // formatting via dn2cpp_enum_format + dn2cpp_string_try_copy_to_span. Diffed EXACTLY
    // vs real .NET (corelib_diff_gate). Both underlying widths and both [Flags]/plain
    // shapes, empty ("G") / G / D / X / F specifiers, an undefined value, and the
    // too-small-destination false arm.
    internal enum ByteMode : byte
    {
        Off = 0,
        On = 1,
        Auto = 2,
    }

    [Flags]
    internal enum IntPerms
    {
        None = 0,
        Read = 1,
        Write = 2,
        Exec = 4,
    }

    internal enum LongCode : long
    {
        Alpha = 1,
        Beta = 2,
        Gamma = 3,
    }

    internal static class Program
    {
        private static string SpanFmt<T>(T v, string fmt) where T : ISpanFormattable
        {
            Span<char> buf = stackalloc char[64];
            bool ok = v.TryFormat(buf, out int written, fmt.AsSpan(), null);
            return ok ? new string(buf.Slice(0, written)) : "<nofit>";
        }

        // The too-small-destination arm: TryFormat returns false, writes nothing.
        private static string SpanFmtTiny<T>(T v, string fmt) where T : ISpanFormattable
        {
            Span<char> tiny = stackalloc char[1];
            bool ok = v.TryFormat(tiny, out int written, fmt.AsSpan(), null);
            return ok ? new string(tiny.Slice(0, written)) : ("nofit w=" + written);
        }

        internal static void __GateEntry()
        {
            // Empty format defaults to "G".
            Console.WriteLine("sf-plain: " + SpanFmt(ByteMode.On, ""));
            Console.WriteLine("sf-G: " + SpanFmt(ByteMode.Auto, "G"));
            Console.WriteLine("sf-D: " + SpanFmt(ByteMode.Auto, "D"));
            Console.WriteLine("sf-X: " + SpanFmt(ByteMode.Auto, "X"));
            Console.WriteLine("sf-undef: " + SpanFmt((ByteMode)9, "G"));
            Console.WriteLine("sf-flags: " + SpanFmt(IntPerms.Read | IntPerms.Exec, ""));
            Console.WriteLine("sf-flags-F: " + SpanFmt(IntPerms.Read | IntPerms.Write, "F"));
            Console.WriteLine("sf-flags-D: " + SpanFmt(IntPerms.Read | IntPerms.Exec, "D"));
            Console.WriteLine("sf-flags-X: " + SpanFmt(IntPerms.Read | IntPerms.Write | IntPerms.Exec, "X"));
            Console.WriteLine("sf-long: " + SpanFmt(LongCode.Beta, "G"));
            Console.WriteLine("sf-long-X: " + SpanFmt(LongCode.Gamma, "X"));
            Console.WriteLine("sf-tiny: " + SpanFmtTiny(ByteMode.Auto, "G"));
        }
    }
}
