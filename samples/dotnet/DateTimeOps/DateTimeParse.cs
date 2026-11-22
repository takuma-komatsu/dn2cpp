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

    internal static void RunMissingDateDefaults()
    {
        Console.WriteLine("== date parsing defaults ==");
        const string time = "12:34:56";
        const string format = "HH:mm:ss";
        var clock = new TimeSpan(12, 34, 56);
        var before = DateTime.Now;
        var exact = DateTime.ParseExact(time, format, ci);
        Console.WriteLine("date exact today=" + IsToday(exact, before, clock));
        bool exactOk = DateTime.TryParseExact(time.AsSpan(), format.AsSpan(), ci, DateTimeStyles.NoCurrentDateDefault, out var minimum);
        Console.WriteLine("date exact minimum=" + (exactOk && minimum.Ticks == clock.Ticks));

        var monthDay = DateTime.ParseExact("06-15", "MM-dd", ci);
        var monthDayWithStyle = DateTime.ParseExact("06-15", "MM-dd", ci, DateTimeStyles.NoCurrentDateDefault);
        var day = DateTime.ParseExact("15", "dd", ci);
        Console.WriteLine("date exact month-day=" + IsThisYear(monthDay, before, 6, 15)
            + "/" + IsThisYear(monthDayWithStyle, before, 6, 15));
        Console.WriteLine("date exact day/year=" + IsThisYear(day, before, 1, 15)
            + "/" + (DateTime.ParseExact("2024", "yyyy", ci) == new DateTime(2024, 1, 1)));

        try { Console.WriteLine("date general today=" + IsToday(DateTime.Parse(time, ci), before, clock)); }
        catch (FormatException) { Console.WriteLine("date general today=FormatException"); }
        bool general = DateTime.TryParse(time, ci, DateTimeStyles.None, out var tried);
        bool generalMinimum = DateTime.TryParse(time, ci, DateTimeStyles.NoCurrentDateDefault, out var triedMinimum);
        Console.WriteLine("date general try=" + (general && IsToday(tried, before, clock))
            + "/" + (generalMinimum && triedMinimum.Ticks == clock.Ticks));

        bool localOk = DateTimeOffset.TryParseExact(time, format, ci, DateTimeStyles.None, out var local);
        Console.WriteLine("offset exact today=" + (localOk && IsToday(local.DateTime, before, clock)));
        Console.WriteLine("offset exact relative date=" + IsOffsetToday(time + " +14:00", TimeSpan.FromHours(14), clock)
            + "/" + IsOffsetToday(time + " -14:00", TimeSpan.FromHours(-14), clock));
        const string incomplete = "06-15 +02:00";
        bool incompleteRejected = !DateTimeOffset.TryParseExact(incomplete, "MM-dd zzz", ci, DateTimeStyles.None, out _);
        try
        {
            _ = DateTimeOffset.ParseExact(incomplete, "MM-dd zzz", ci, DateTimeStyles.None);
            Console.WriteLine("offset exact incomplete=False");
        }
        catch (FormatException e)
        {
            Console.WriteLine("offset exact incomplete=" + (incompleteRejected && e.Message.Contains(incomplete)));
        }

        try
        {
            _ = DateTimeOffset.ParseExact(time, format, ci, DateTimeStyles.NoCurrentDateDefault);
            Console.WriteLine("offset exact style=accepted");
        }
        catch (ArgumentException e) { Console.WriteLine("offset exact style=" + e.ParamName); }
        try
        {
            _ = DateTimeOffset.TryParseExact(time.AsSpan(), new[] { format }, ci, DateTimeStyles.NoCurrentDateDefault, out _);
            Console.WriteLine("offset exact try style=accepted");
        }
        catch (ArgumentException e) { Console.WriteLine("offset exact try style=" + e.ParamName); }
    }

    private static bool IsToday(DateTime parsed, DateTime before, TimeSpan clock) =>
        (parsed.Date == before.Date || parsed.Date == DateTime.Now.Date)
        && parsed.TimeOfDay == clock && parsed.Kind == DateTimeKind.Unspecified;

    private static bool IsThisYear(DateTime parsed, DateTime before, int month, int day) =>
        (parsed.Year == before.Year || parsed.Year == DateTime.Now.Year)
        && parsed.Month == month && parsed.Day == day;

    private static bool IsOffsetToday(string input, TimeSpan offset, TimeSpan clock)
    {
        var before = DateTime.UtcNow.Add(offset).Date;
        if (!DateTimeOffset.TryParseExact(input, "HH:mm:ss zzz", ci, DateTimeStyles.None, out var parsed))
            return false;
        return (parsed.Date == before || parsed.Date == DateTime.UtcNow.Add(offset).Date)
            && parsed.Offset == offset && parsed.TimeOfDay == clock;
    }
}
