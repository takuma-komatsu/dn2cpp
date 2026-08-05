#nullable enable
using System;
using System.Globalization;

// DateTime / TimeSpan Parse / TryParse / ParseExact (InvariantCulture, ISO and
// standard forms). The real corelib parse path reaches DateTimeParse / culture / Number
// machinery we cannot transpile, so parsing is lowered to the runtime helpers
// dn2cpp_datetime_try_parse[_exact] / dn2cpp_timespan_try_parse[_exact], mirroring the
// intrinsic-value-type pattern. Parsed values are echoed via the formatters
// (round-trip "o"/"c" + Ticks + Kind) so a successful parse is verified by
// its exact field values. Diffed exactly against real .NET (InvariantCulture).
namespace DateTimeParse;


static class Program
{
    static readonly CultureInfo ci = CultureInfo.InvariantCulture;

    static void D(DateTime v) => Console.WriteLine(v.ToString("yyyy-MM-dd HH:mm:ss.fffffff", ci) + " K=" + (int)v.Kind);
    static void T(TimeSpan v) => Console.WriteLine(v.ToString("c") + " ticks=" + v.Ticks);internal static void __GateEntry()
    {
        Console.WriteLine("-- TimeSpan.Parse --");
        T(TimeSpan.Parse("1.02:03:04", ci));
        T(TimeSpan.Parse("02:03:04", ci));
        T(TimeSpan.Parse("1.02:03:04.0050000", ci));
        T(TimeSpan.Parse("00:00:01.5", ci));
        T(TimeSpan.Parse("-1.02:03:04", ci));
        T(TimeSpan.Parse("10:30", ci));
        T(TimeSpan.Parse("5", ci));
        T(TimeSpan.Parse("  1:2:3  ", ci));
        T(TimeSpan.Parse("-5", ci));
        T(TimeSpan.Parse("23:59:59.9999999", ci));

        Console.WriteLine("-- TimeSpan.TryParse --");
        Console.WriteLine(TimeSpan.TryParse("1.02:03:04", ci, out var t1) + " " + t1.Ticks);
        Console.WriteLine(TimeSpan.TryParse("not a span", ci, out var t2) + " " + t2.Ticks);
        Console.WriteLine(TimeSpan.TryParse("25:00", ci, out var t3) + " " + t3.Ticks);
        Console.WriteLine(TimeSpan.TryParse("", ci, out var t4) + " " + t4.Ticks);

        Console.WriteLine("-- TimeSpan.ParseExact --");
        T(TimeSpan.ParseExact("1.02:03:04", "c", ci));
        T(TimeSpan.ParseExact("02:03:04", @"hh\:mm\:ss", ci));
        T(TimeSpan.ParseExact("01.02:03:04.005", @"dd\.hh\:mm\:ss\.fff", ci));
        T(TimeSpan.ParseExact("05", @"mm", ci));

        Console.WriteLine("-- DateTime.Parse (ISO) --");
        D(DateTime.Parse("2026-06-23", ci));
        D(DateTime.Parse("2026-06-23T14:05:09", ci));
        D(DateTime.Parse("2026-06-23 14:05:09", ci));
        D(DateTime.Parse("2026-06-23T14:05:09.1230000", ci));

        Console.WriteLine("-- DateTime.Parse (US invariant) --");
        D(DateTime.Parse("06/23/2026", ci));
        D(DateTime.Parse("06/23/2026 14:05:09", ci));
        D(DateTime.Parse("6/3/2026", ci));
        D(DateTime.Parse("06/23/2026 14:05", ci));
        D(DateTime.Parse("12/31/9999 23:59:59", ci));

        Console.WriteLine("-- DateTime.TryParse --");
        Console.WriteLine(DateTime.TryParse("2026-06-23", ci, out var d1) + " " + d1.Ticks);
        Console.WriteLine(DateTime.TryParse("garbage", ci, out var d2) + " " + d2.Ticks);
        Console.WriteLine(DateTime.TryParse("2026-13-01", ci, out var d3) + " " + d3.Ticks);
        Console.WriteLine(DateTime.TryParse("2026-02-30", ci, out var d4) + " " + d4.Ticks);

        Console.WriteLine("-- DateTime.ParseExact --");
        D(DateTime.ParseExact("2026-06-23", "yyyy-MM-dd", ci));
        D(DateTime.ParseExact("2026/06/23 14:05:09", "yyyy/MM/dd HH:mm:ss", ci));
        D(DateTime.ParseExact("23/06/2026", "dd/MM/yyyy", ci));
        D(DateTime.ParseExact("06/23/2026", "MM/dd/yyyy", ci));
        D(DateTime.ParseExact("20260623140509", "yyyyMMddHHmmss", ci));
        D(DateTime.ParseExact("2026-06-23T14:05:09", "s", ci));
        D(DateTime.ParseExact("Jun 23 2026", "MMM dd yyyy", ci));
        D(DateTime.ParseExact("June 23, 2026", "MMMM dd, yyyy", ci));
        D(DateTime.ParseExact("2026-06-23 2:05:09 PM", "yyyy-MM-dd h:mm:ss tt", ci));
        D(DateTime.ParseExact("12:00:00 AM 2026-06-23", "h:mm:ss tt yyyy-MM-dd", ci));

        Console.WriteLine("-- DateTime.TryParseExact --");
        Console.WriteLine(DateTime.TryParseExact("2026-06-23", "yyyy-MM-dd", ci, DateTimeStyles.None, out var e1) + " " + e1.Ticks);
        Console.WriteLine(DateTime.TryParseExact("2026-06-23", "MM/dd/yyyy", ci, DateTimeStyles.None, out var e2) + " " + e2.Ticks);
    }
}
