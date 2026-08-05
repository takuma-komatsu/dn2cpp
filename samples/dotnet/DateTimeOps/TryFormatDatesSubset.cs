using System;
using System.Globalization;

namespace TryFormatDatesSubset
{
    // T2: DateTime/DateOnly/TimeOnly.TryFormat(Span<char>, out charsWritten,
    // ReadOnlySpan<char> format, IFormatProvider) — explicit invariant format specifiers
    // (dn2cpp drops the provider = invariant) at fitting / exact / too-short dest sizes,
    // plus TimeOnly.Microsecond. Diffed exactly vs real .NET.
    internal static class Program
    {
        private static readonly CultureInfo ci = CultureInfo.InvariantCulture;

        private static void ShowDT(string label, DateTime v, string f)
        {
            char[] big = new char[64];
            v.TryFormat(big, out int len, f.AsSpan(), ci);
            foreach (int cap in new[] { len + 2, len, len - 1 < 0 ? 0 : len - 1 })
            {
                char[] buf = new char[cap];
                bool ok = v.TryFormat(buf, out int w, f.AsSpan(), ci);
                string t = ""; for (int i = 0; i < w && i < buf.Length; i++) t += buf[i];
                Console.WriteLine($"{label} [{f}] cap={cap}: ok={ok} w={w} [{t}]");
            }
        }

        private static void ShowDate(string label, DateOnly v, string f)
        {
            char[] big = new char[64];
            v.TryFormat(big, out int len, f.AsSpan(), ci);
            foreach (int cap in new[] { len + 2, len, len - 1 < 0 ? 0 : len - 1 })
            {
                char[] buf = new char[cap];
                bool ok = v.TryFormat(buf, out int w, f.AsSpan(), ci);
                string t = ""; for (int i = 0; i < w && i < buf.Length; i++) t += buf[i];
                Console.WriteLine($"{label} [{f}] cap={cap}: ok={ok} w={w} [{t}]");
            }
        }

        private static void ShowTime(string label, TimeOnly v, string f)
        {
            char[] big = new char[64];
            v.TryFormat(big, out int len, f.AsSpan(), ci);
            foreach (int cap in new[] { len + 2, len, len - 1 < 0 ? 0 : len - 1 })
            {
                char[] buf = new char[cap];
                bool ok = v.TryFormat(buf, out int w, f.AsSpan(), ci);
                string t = ""; for (int i = 0; i < w && i < buf.Length; i++) t += buf[i];
                Console.WriteLine($"{label} [{f}] cap={cap}: ok={ok} w={w} [{t}]");
            }
        }

        internal static void __GateEntry()
        {
            var dt = new DateTime(2026, 6, 23, 14, 5, 9);
            ShowDT("dt", dt, "yyyy-MM-dd HH:mm:ss");
            ShowDT("dt", dt, "s");

            var day = new DateOnly(2026, 6, 23);
            ShowDate("day", day, "yyyy-MM-dd");
            ShowDate("day", day, "MM/dd/yyyy");

            var tm = new TimeOnly(14, 5, 9);
            ShowTime("tm", tm, "HH:mm:ss");
            ShowTime("tm", tm, "HH:mm");

            // TimeOnly.Microsecond (ticks / 10 % 1000).
            Console.WriteLine(new TimeOnly(507092507500L).Microsecond);   // 750
            Console.WriteLine(new TimeOnly(14, 5, 9).Microsecond);        // 0
            Console.WriteLine(new TimeOnly(0L).Microsecond);              // 0
            Console.WriteLine(new TimeOnly(863999999999L).Microsecond);   // 999
        }
    }
}
