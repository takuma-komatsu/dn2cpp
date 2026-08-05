using System;
using System.Globalization;
using SampleSupport;

// The sample carried no namespace of its own (it was a whole gate project, driving
// the three sections below from its own tail); it gets one here so the section keeps
// its identity beside the bucket's other Programs. Nothing it prints is a type name.
namespace TryFormatSubset;

// Integer-primitive TryFormat — the
// span-write formatter `<T>.TryFormat(Span<char> dest, out int charsWritten,
// ReadOnlySpan<char> format = default, IFormatProvider provider = null)`. dn2cpp's
// own Compilation.Build SRM-decodes field signatures; SignatureDecoder.CheckHeader
// reaches Byte.TryFormat, so without an emit interception + reachability cut every
// integer primitive's real body routes through System.Number.TryFormat* — the
// dominant remaining console-self-host cascade. MethodCompiler lowers all eight
// integer primitives' TryFormat to dn2cpp_try_format_int|uint (the same byteWidth-
// aware formatters as ToString(format)), copying into the Span<char> buffer with
// .NET semantics (fits => write code units + charsWritten + true; too short =>
// buffer untouched + charsWritten=0 + false). This sample exercises every type
// across hex/decimal/grouped/general specifiers and the default (empty) format, at
// dest sizes that are plenty / exactly enough / one too short / zero. The result
// bool and charsWritten are printed alongside the written text, exact-diffed vs real
// .NET. CoreLib only (no Linq shim).
internal static class Program
{
    // Prints one TryFormat call: the bool, the out charsWritten, the written prefix,
    // and the count of trailing buffer chars left untouched (still the '.' fill — this
    // is how we verify a too-short dest is never partially written). The "full buffer"
    // is reconstructed char-by-char (avoiding `new string(char[])`, which is a separate
    // unmodelled empty-array overload) so the untouched tail is visible in the diff.
    private static void Run(string label, bool ok, int written, char[] buf)
    {
        int w = written < 0 ? 0 : written;
        // Written prefix.
        string text = "";
        for (int i = 0; i < w && i < buf.Length; i++) text += buf[i];
        // Count of trailing dots still in place after the written prefix.
        int untouched = 0;
        for (int i = w; i < buf.Length; i++) if (buf[i] == '.') untouched++;
        Console.WriteLine($"{label}: ok={ok} written={written} cap={buf.Length} w=[{text}] dots={untouched}");
    }

    // The dest capacities to probe for a value whose formatted length is `len`:
    // plenty (+2), exact, one too short, and zero.
    private static int[] Caps(int len)
    {
        int shortCap = len - 1 >= 0 ? len - 1 : 0;
        return new[] { len + 2, len, shortCap, 0 };
    }

    private static ReadOnlySpan<char> Fmt(string f) => f.Length == 0 ? default : f.AsSpan();

    private static void Byte_(byte v, string f)
    {
        char[] big = new char[64];
        v.TryFormat(big, out int len, Fmt(f));
        foreach (int cap in Caps(len))
        {
            char[] buf = FilledDots(cap);
            bool ok = v.TryFormat(buf, out int n, Fmt(f));
            Run($"byte {v} [{f}] cap={cap}", ok, n, buf);
        }
    }

    private static void SByte_(sbyte v, string f)
    {
        char[] big = new char[64];
        v.TryFormat(big, out int len, Fmt(f));
        foreach (int cap in Caps(len))
        {
            char[] buf = FilledDots(cap);
            bool ok = v.TryFormat(buf, out int n, Fmt(f));
            Run($"sbyte {v} [{f}] cap={cap}", ok, n, buf);
        }
    }

    private static void Short_(short v, string f)
    {
        char[] big = new char[64];
        v.TryFormat(big, out int len, Fmt(f));
        foreach (int cap in Caps(len))
        {
            char[] buf = FilledDots(cap);
            bool ok = v.TryFormat(buf, out int n, Fmt(f));
            Run($"short {v} [{f}] cap={cap}", ok, n, buf);
        }
    }

    private static void UShort_(ushort v, string f)
    {
        char[] big = new char[64];
        v.TryFormat(big, out int len, Fmt(f));
        foreach (int cap in Caps(len))
        {
            char[] buf = FilledDots(cap);
            bool ok = v.TryFormat(buf, out int n, Fmt(f));
            Run($"ushort {v} [{f}] cap={cap}", ok, n, buf);
        }
    }

    private static void Int_(int v, string f)
    {
        char[] big = new char[64];
        v.TryFormat(big, out int len, Fmt(f));
        foreach (int cap in Caps(len))
        {
            char[] buf = FilledDots(cap);
            bool ok = v.TryFormat(buf, out int n, Fmt(f));
            Run($"int {v} [{f}] cap={cap}", ok, n, buf);
        }
    }

    private static void UInt_(uint v, string f)
    {
        char[] big = new char[64];
        v.TryFormat(big, out int len, Fmt(f));
        foreach (int cap in Caps(len))
        {
            char[] buf = FilledDots(cap);
            bool ok = v.TryFormat(buf, out int n, Fmt(f));
            Run($"uint {v} [{f}] cap={cap}", ok, n, buf);
        }
    }

    private static void Long_(long v, string f)
    {
        char[] big = new char[64];
        v.TryFormat(big, out int len, Fmt(f));
        foreach (int cap in Caps(len))
        {
            char[] buf = FilledDots(cap);
            bool ok = v.TryFormat(buf, out int n, Fmt(f));
            Run($"long {v} [{f}] cap={cap}", ok, n, buf);
        }
    }

    private static void ULong_(ulong v, string f)
    {
        char[] big = new char[64];
        v.TryFormat(big, out int len, Fmt(f));
        foreach (int cap in Caps(len))
        {
            char[] buf = FilledDots(cap);
            bool ok = v.TryFormat(buf, out int n, Fmt(f));
            Run($"ulong {v} [{f}] cap={cap}", ok, n, buf);
        }
    }

    private static char[] FilledDots(int n)
    {
        char[] b = new char[n < 0 ? 0 : n];
        for (int i = 0; i < b.Length; i++) b[i] = '.';
        return b;
    }

    internal static void __GateEntry()
    {
        // The bucket cannot set <InvariantGlobalization>: its NumberStyles/FloatParse
        // sections assert real de-DE and fr-FR separators, and that property turns ICU off.
        // Only "N0" below is culture-sensitive (real .NET takes its group separator from
        // CurrentCulture when the provider argument is null), so the pin is scoped to this
        // section. dn2cpp's own formatting is invariant unconditionally, so this moves the
        // ORACLE only.
        //
        // SCOPED, not a hand-written save/restore: the restore owes the bucket its ambient
        // culture back, and a `finally` is the only form of that which survives an early
        // return or a throw. The driver's pin already makes the entering culture
        // invariant, so this changes nothing today — it stays because it states THIS
        // section's own requirement and so cannot be broken by a driver that stops
        // stating it.
        using (CultureScope.Use(CultureInfo.InvariantCulture))
        {
            // Specifiers own-code and tests use: default(empty), hex (width-correct two's-
            // complement masking, lower/upper, padded), decimal (padded), grouped, general.
            string[] fmts = { "", "X2", "x", "x2", "X4", "X", "D3", "N0", "G", "D", "g" };

            foreach (string f in fmts)
            {
                Byte_(0, f); Byte_(15, f); Byte_(127, f); Byte_(200, f); Byte_(255, f);
                SByte_(0, f); SByte_(127, f); SByte_(-1, f); SByte_(-128, f); SByte_(-100, f);
                Short_(0, f); Short_(4660, f); Short_(32767, f); Short_(-1, f); Short_(-32768, f);
                UShort_(0, f); UShort_(4660, f); UShort_(32768, f); UShort_(65535, f);
                Int_(0, f); Int_(305419896, f); Int_(2147483647, f); Int_(-1, f); Int_(int.MinValue, f);
                UInt_(0u, f); UInt_(305419896u, f); UInt_(4294967295u, f);
                Long_(0L, f); Long_(1234567890123L, f); Long_(-1L, f); Long_(long.MinValue, f);
                ULong_(0UL, f); ULong_(1234567890123UL, f); ULong_(ulong.MaxValue, f);
            }

            BinaryFormatSubset.Program.__GateEntry();
            Utf8TryFormatSubset.Program.__GateEntry();
            FloatTryFormatSubset.Program.__GateEntry();
        }
    }
}
