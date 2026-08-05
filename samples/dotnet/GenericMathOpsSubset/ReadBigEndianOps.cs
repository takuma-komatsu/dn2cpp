using System;
using System.Numerics;

namespace GenericMathReadBE;

// IBinaryInteger<T>.ReadBigEndian(ReadOnlySpan<byte>, bool isUnsigned) closed to
// Int32/Int64 — the shape System.Formats.Tar reaches decoding GNU/PAX base-256 numeric
// header fields (Thrive's save loading). It arrives as a `constrained. call` to a static
// virtual whose default interface body calls the static-abstract TryReadBigEndian (an
// InternalCall with no IL). MethodCompiler.TryEmitGenericMathIntrinsic lowers the whole
// thing to the runtime dn2cpp_read_big_endian<T>, which reads the span big-endian, sign/
// zero-extends per isUnsigned, and raises a catchable OverflowException when the value
// does not fit T. The span overload is called explicitly (T.ReadBigEndian(byte[], …) is a
// separate, unmodeled overload). Output diffs exact vs real .NET across lengths 0..9,
// sign-bit set/clear, and the overflow boundaries.
internal static class ReadBigEndianOps
{
    // Reached through the generic constraint — the `constrained. call` form the
    // transpiler intercepts. (A static-virtual interface member is not callable as
    // `int.ReadBigEndian(...)`; it dispatches through T : IBinaryInteger<T>.)
    static string RB<T>(byte[] b, bool u) where T : IBinaryInteger<T>
    {
        try { return T.ReadBigEndian(new ReadOnlySpan<byte>(b), u).ToString(); }
        catch (OverflowException) { return "OVF"; }
    }

    static void Row(byte[] b)
    {
        Console.WriteLine($"[{BitConverter.ToString(b)}] i32 s={RB<int>(b, false)} u={RB<int>(b, true)}"
            + $" | i64 s={RB<long>(b, false)} u={RB<long>(b, true)}");
    }

    // UNSIGNED self types (uint/ulong are IBinaryInteger too, so the span overload
    // reaches the same intrinsic). A negative signed-source value (s=..., leading bit
    // set) cannot fit an unsigned target at ANY length — .NET throws OverflowException,
    // including for a short span shorter than sizeof(T); the unsigned-source read (u=...)
    // zero-extends and succeeds.
    static void URow(byte[] b)
    {
        Console.WriteLine($"[{BitConverter.ToString(b)}] u32 s={RB<uint>(b, false)} u={RB<uint>(b, true)}"
            + $" | u64 s={RB<ulong>(b, false)} u={RB<ulong>(b, true)}");
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== IBinaryInteger.ReadBigEndian (span overload, Int32/Int64) ==");
        Row(new byte[] { });
        Row(new byte[] { 0x00 });
        Row(new byte[] { 0x7F });
        Row(new byte[] { 0x80 });
        Row(new byte[] { 0xFF });
        Row(new byte[] { 0x01, 0x00 });
        Row(new byte[] { 0xFF, 0xFF });
        Row(new byte[] { 0x80, 0x00 });
        Row(new byte[] { 0x00, 0x00, 0x01, 0x00 });
        Row(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        Row(new byte[] { 0x80, 0x00, 0x00, 0x00 });
        Row(new byte[] { 0x00, 0x80, 0x00, 0x00, 0x00 });
        Row(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00 });
        Row(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 });
        Row(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
        Row(new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });

        Console.WriteLine("== ReadBigEndian into an UNSIGNED self (uint/ulong) ==");
        URow(new byte[] { 0x7F });                         // positive short: fits
        URow(new byte[] { 0x80 });                         // negative short (n<W): s=OVF, u=128
        URow(new byte[] { 0xFF });                         // negative short (n<W): s=OVF, u=255
        URow(new byte[] { 0xFF, 0xFF });                   // negative (n<W): s=OVF, u=65535
        URow(new byte[] { 0x00, 0xFF, 0xFF, 0xFF });       // positive full: fits both
        URow(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });       // u32 s=OVF; u64 u fits
    }
}
