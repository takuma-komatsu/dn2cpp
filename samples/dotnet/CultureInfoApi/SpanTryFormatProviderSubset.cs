using System;
using System.Globalization;
using System.Text;

// The span TryFormat PROVIDER routing on Double/Single/Decimal and the integer
// widths, for BOTH destination element types (ISpanFormattable's char16 writer
// and IUtf8SpanFormattable's UTF-8 twin). Every other bucket's call sites pass a
// provider equal to the pinned CurrentCulture, which makes a dropped provider
// invisible; this section is the only place provider != ambient is asserted, in
// both directions: an explicit provider wins over CurrentCulture, and a null
// provider reads CurrentCulture (.NET's null-provider rule). fr-FR's U+202F
// group separator pins the multi-byte UTF-8 encode, and the capacity probes pin
// that the byte span's fit is measured in UTF-8 BYTES, not chars. Only modeled
// cultures (de-DE, fr-FR) are used and the driver's pin is restored, so the
// host cannot move a line.
namespace SpanTryFormatProviderSubset
{
    internal static class Program
    {
        private static void D(string label, double v, string f, IFormatProvider p)
        {
            char[] cb = new char[64];
            bool okc = v.TryFormat(cb, out int nc, f.AsSpan(), p);
            byte[] bb = new byte[64];
            bool okb = v.TryFormat(bb, out int nb, f.AsSpan(), p);
            Console.WriteLine("  " + label + " c=[" + new string(cb, 0, nc) + "] u8=["
                + Encoding.UTF8.GetString(bb, 0, nb) + "] okc=" + okc + " okb=" + okb
                + " nc=" + nc + " nb=" + nb);
        }

        private static void F(string label, float v, string f, IFormatProvider p)
        {
            char[] cb = new char[64];
            bool okc = v.TryFormat(cb, out int nc, f.AsSpan(), p);
            byte[] bb = new byte[64];
            bool okb = v.TryFormat(bb, out int nb, f.AsSpan(), p);
            Console.WriteLine("  " + label + " c=[" + new string(cb, 0, nc) + "] u8=["
                + Encoding.UTF8.GetString(bb, 0, nb) + "] okc=" + okc + " okb=" + okb
                + " nc=" + nc + " nb=" + nb);
        }

        private static void M(string label, decimal v, string f, IFormatProvider p)
        {
            char[] cb = new char[64];
            bool okc = v.TryFormat(cb, out int nc, f.AsSpan(), p);
            byte[] bb = new byte[64];
            bool okb = v.TryFormat(bb, out int nb, f.AsSpan(), p);
            Console.WriteLine("  " + label + " c=[" + new string(cb, 0, nc) + "] u8=["
                + Encoding.UTF8.GetString(bb, 0, nb) + "] okc=" + okc + " okb=" + okb
                + " nc=" + nc + " nb=" + nb);
        }

        private static void I(string label, int v, string f, IFormatProvider p)
        {
            char[] cb = new char[64];
            bool okc = v.TryFormat(cb, out int nc, f.AsSpan(), p);
            byte[] bb = new byte[64];
            bool okb = v.TryFormat(bb, out int nb, f.AsSpan(), p);
            Console.WriteLine("  " + label + " c=[" + new string(cb, 0, nc) + "] u8=["
                + Encoding.UTF8.GetString(bb, 0, nb) + "] okc=" + okc + " okb=" + okb
                + " nc=" + nc + " nb=" + nb);
        }

        private static void L(string label, long v, string f, IFormatProvider p)
        {
            char[] cb = new char[64];
            bool okc = v.TryFormat(cb, out int nc, f.AsSpan(), p);
            byte[] bb = new byte[64];
            bool okb = v.TryFormat(bb, out int nb, f.AsSpan(), p);
            Console.WriteLine("  " + label + " c=[" + new string(cb, 0, nc) + "] u8=["
                + Encoding.UTF8.GetString(bb, 0, nb) + "] okc=" + okc + " okb=" + okb
                + " nc=" + nc + " nb=" + nb);
        }

        private static void B(string label, byte v, string f, IFormatProvider p)
        {
            char[] cb = new char[64];
            bool okc = v.TryFormat(cb, out int nc, f.AsSpan(), p);
            byte[] bb = new byte[64];
            bool okb = v.TryFormat(bb, out int nb, f.AsSpan(), p);
            Console.WriteLine("  " + label + " c=[" + new string(cb, 0, nc) + "] u8=["
                + Encoding.UTF8.GetString(bb, 0, nb) + "] okc=" + okc + " okb=" + okb
                + " nc=" + nc + " nb=" + nb);
        }

        // Exact-capacity probe: the char span's fit counts chars, the byte span's
        // counts UTF-8 bytes — with a multi-byte separator the two differ for the
        // same rendering, and a too-short destination reports false + 0.
        private static void Probe(string label, int v, string f, IFormatProvider p, int charCap, int byteCap)
        {
            char[] cb = new char[charCap];
            bool okc = v.TryFormat(cb, out int nc, f.AsSpan(), p);
            byte[] bb = new byte[byteCap];
            bool okb = v.TryFormat(bb, out int nb, f.AsSpan(), p);
            Console.WriteLine("  " + label + " c(cap=" + charCap + ")=" + okc + "/" + nc
                + " u8(cap=" + byteCap + ")=" + okb + "/" + nb);
        }

        internal static void Run()
        {
            Console.WriteLine("== span TryFormat provider ==");
            CultureInfo de = new CultureInfo("de-DE");
            CultureInfo fr = new CultureInfo("fr-FR");
            CultureInfo inv = CultureInfo.InvariantCulture;

            // Ambient de-DE: the explicit INVARIANT provider must win over
            // CurrentCulture, and the null provider must read the ambient de-DE.
            CultureInfo.CurrentCulture = de;
            D("d inv@de", 1234.5, "N2", inv);
            F("f inv@de", 1234.5f, "N2", inv);
            M("m inv@de", 1234.5m, "N2", inv);
            I("i inv@de", 1234567, "N0", inv);
            L("l inv@de", 9876543210L, "N0", inv);
            B("b inv@de", (byte)200, "N0", inv);
            D("d null@de", 1234.5, "N2", null);
            I("i null@de", 1234567, "N0", null);
            // Restore the driver's pin; from here every culture is explicit.
            CultureInfo.CurrentCulture = inv;

            // Ambient invariant: the explicit de-DE provider must win.
            D("d de@inv", 1234.5, "N2", de);
            F("f de@inv", 1234.5f, "N2", de);
            M("m de@inv", 1234.5m, "N2", de);
            I("i de@inv", 1234567, "N0", de);
            L("l de@inv", 9876543210L, "N0", de);
            B("b de@inv", (byte)200, "N0", de);

            // fr-FR: U+202F group separator — three UTF-8 bytes per separator, so
            // nb > nc for the same rendering.
            D("d fr@inv", 1234567.5, "N2", fr);
            I("i fr@inv", 1234567, "N0", fr);

            // Specials through the de-DE symbols.
            D("d nan de", double.NaN, "", de);
            D("d +inf de", double.PositiveInfinity, "", de);
            D("d -inf de", double.NegativeInfinity, "", de);

            // "1 234 567": 9 chars, 13 UTF-8 bytes. Exact fit; one short
            // on each axis; and the char count offered as BYTE capacity must fail.
            Probe("i fr fit", 1234567, "N0", fr, 9, 13);
            Probe("i fr short", 1234567, "N0", fr, 8, 12);
            Probe("i fr chars-as-bytes", 1234567, "N0", fr, 9, 9);
        }
    }
}
