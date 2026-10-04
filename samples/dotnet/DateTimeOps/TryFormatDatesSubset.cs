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
        private delegate bool ByteFormat(byte[] destination, out int written);

        internal static void RunUtf8()
        {
            Console.WriteLine("== UTF-8 date destinations ==");
            var date = new DateTime(2025, 1, 1, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567);
            var day = DateOnly.FromDateTime(date);
            var time = TimeOnly.FromDateTime(date);
            var offset = new DateTimeOffset(2025, 1, 1, 12, 34, 56, TimeSpan.FromHours(9));
            var duration = TimeSpan.FromTicks(-123456789);
            foreach (string format in new[] { "", "O" })
            {
                CheckBytes("datetime:" + format, (byte[] buffer, out int written) => date.TryFormat(buffer, out written, format.AsSpan(), ci));
                CheckBytes("dateonly:" + format, (byte[] buffer, out int written) => day.TryFormat(buffer, out written, format.AsSpan(), ci));
                CheckBytes("timeonly:" + format, (byte[] buffer, out int written) => time.TryFormat(buffer, out written, format.AsSpan(), ci));
                CheckBytes("offset:" + format, (byte[] buffer, out int written) => offset.TryFormat(buffer, out written, format.AsSpan(), ci));
                char[] chars = new char[64];
                bool ok = offset.TryFormat(chars, out int count, format.AsSpan(), ci);
                Console.WriteLine("offset chars:" + format + "=" + ok + ":" + count + ":" + new string(chars, 0, count));
            }
            CheckBytes("datetime unicode", (byte[] buffer, out int written) => date.TryFormat(buffer, out written, "yyyy'漢'MM-dd".AsSpan(), ci));
            CheckBytes("dateonly unicode", (byte[] buffer, out int written) => day.TryFormat(buffer, out written, "yyyy'漢'MM-dd".AsSpan(), ci));
            CheckBytes("timeonly unicode", (byte[] buffer, out int written) => time.TryFormat(buffer, out written, "HH'漢'mm:ss".AsSpan(), ci));
            CheckBytes("offset unicode", (byte[] buffer, out int written) => offset.TryFormat(buffer, out written, "yyyy'漢'MM-ddzzz".AsSpan(), ci));
            CheckBytes("timespan", (byte[] buffer, out int written) => duration.TryFormat(buffer, out written, "c".AsSpan(), ci));
            Console.WriteLine("UTF-8 date destinations end");
        }

        private static void CheckBytes(string label, ByteFormat format)
        {
            byte[] large = new byte[128];
            format(large, out int length);
            foreach (int capacity in new[] { 0, length - 1, length, length + 2 })
            {
                byte[] destination = new byte[capacity];
                Array.Fill(destination, (byte)77);
                bool ok = format(destination, out int written);
                Console.WriteLine(label + ":" + capacity + "=" + ok + ":" + written + ":" + (ok ? BitConverter.ToString(destination) : ""));
            }
        }

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
