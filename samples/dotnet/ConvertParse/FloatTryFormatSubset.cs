using System;
using System.Globalization;

namespace FloatTryFormatSubset
{
    // T2: Single/Double.TryFormat(Span<char> dest, out int charsWritten,
    // ReadOnlySpan<char> format, IFormatProvider) — the round-trip default format and a
    // standard specifier, at dest sizes that fit / are exactly enough / are too short.
    // Diffed exactly vs real .NET (invariant).
    internal static class Program
    {
        private static ReadOnlySpan<char> Fmt(string f) => f.Length == 0 ? default : f.AsSpan();

        private static void D(double v, string f)
        {
            var ci = CultureInfo.InvariantCulture;
            char[] big = new char[64];
            v.TryFormat(big, out int len, Fmt(f), ci);
            foreach (int cap in new[] { len + 2, len, len - 1 < 0 ? 0 : len - 1, 0 })
            {
                char[] buf = new char[cap];
                bool ok = v.TryFormat(buf, out int w, Fmt(f), ci);
                string text = "";
                for (int i = 0; i < w && i < buf.Length; i++) text += buf[i];
                Console.WriteLine($"double {v.ToString(ci)} [{f}] cap={cap}: ok={ok} w={w} [{text}]");
            }
        }

        private static void F(float v, string f)
        {
            var ci = CultureInfo.InvariantCulture;
            char[] big = new char[64];
            v.TryFormat(big, out int len, Fmt(f), ci);
            foreach (int cap in new[] { len + 2, len, len - 1 < 0 ? 0 : len - 1, 0 })
            {
                char[] buf = new char[cap];
                bool ok = v.TryFormat(buf, out int w, Fmt(f), ci);
                string text = "";
                for (int i = 0; i < w && i < buf.Length; i++) text += buf[i];
                Console.WriteLine($"float {v.ToString(ci)} [{f}] cap={cap}: ok={ok} w={w} [{text}]");
            }
        }

        internal static void __GateEntry()
        {
            foreach (string f in new[] { "", "G", "F2" })
            {
                D(0.0, f); D(3.5, f); D(-12.25, f); D(1234.5, f);
                F(0.0f, f); F(0.1f, f); F(3.5f, f); F(-12.25f, f);
            }
        }
    }
}
