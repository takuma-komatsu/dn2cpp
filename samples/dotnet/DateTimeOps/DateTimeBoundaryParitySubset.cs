using System;
using System.Globalization;

namespace DateTimeBoundaryParitySubset;

internal static class Program
{
    private static void Show(string name, Func<string> action)
    {
        try
        {
            Console.WriteLine(name + "=" + action());
        }
        catch (Exception ex)
        {
            string parameter = ex is ArgumentException arg ? arg.ParamName : "";
            Console.WriteLine(name + "=" + ex.GetType().Name + ":" + parameter + ":"
                + ex.Message.Replace("\r", "").Replace("\n", "|"));
        }
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== datetime offset boundaries ==");
        Show("micro", () => new DateTime(2024, 2, 29, 12, 34, 56, 789, 123).Ticks.ToString());
        Show("micro kind", () => new DateTime(2024, 2, 29, 12, 34, 56, 789, 987, DateTimeKind.Utc).Ticks + ":" + (int)new DateTime(2024, 2, 29, 12, 34, 56, 789, 987, DateTimeKind.Utc).Kind);
        Show("micro negative", () => new DateTime(2024, 2, 29, 12, 34, 56, 789, -1).Ticks.ToString());
        Show("micro high", () => new DateTime(2024, 2, 29, 12, 34, 56, 789, 1000, DateTimeKind.Utc).Ticks.ToString());
        Show("micro after millisecond", () => new DateTime(2024, 2, 29, 12, 34, 56, 1000, -1).Ticks.ToString());
        Show("micro after kind", () => new DateTime(2024, 2, 29, 12, 34, 56, 0, -1, (DateTimeKind)99).Ticks.ToString());

        TimeSpan east = TimeSpan.FromMinutes(330);
        DateTime clock = new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Unspecified);
        Show("dto ticks", () => { var d = new DateTimeOffset(clock.Ticks, east); return d.Ticks + ":" + d.UtcTicks + ":" + d.Offset.Ticks; });
        Show("dto parts", () => { var d = new DateTimeOffset(2024, 2, 29, 12, 34, 56, 789, east); return d.Ticks + ":" + d.UtcTicks + ":" + d.Offset.Ticks; });
        Show("dto datetime", () => { var d = new DateTimeOffset(clock, east); return d.Ticks + ":" + d.UtcTicks + ":" + d.Offset.Ticks; });
        Show("dto tooffset", () => { var d = new DateTimeOffset(clock, east); var shifted = d.ToOffset(TimeSpan.FromHours(-3)); return shifted.Ticks + ":" + shifted.UtcTicks + ":" + shifted.Offset.Ticks; });
        Show("dto local match", () => { var d = new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Local); var inferred = new DateTimeOffset(d); return (new DateTimeOffset(d, inferred.Offset).UtcTicks == inferred.UtcTicks).ToString(); });

        Show("dto precision", () => new DateTimeOffset(clock.Ticks, TimeSpan.FromSeconds(30)).Ticks.ToString());
        Show("dto offset high", () => new DateTimeOffset(clock, TimeSpan.FromHours(15)).Ticks.ToString());
        Show("dto offset low", () => new DateTimeOffset(clock, TimeSpan.FromHours(-15)).Ticks.ToString());
        Show("dto utc low", () => new DateTimeOffset(DateTime.MinValue.Ticks, TimeSpan.FromMinutes(1)).Ticks.ToString());
        Show("dto utc high", () => new DateTimeOffset(DateTime.MaxValue.Ticks, TimeSpan.FromMinutes(-1)).Ticks.ToString());
        Show("dto utc kind", () => new DateTimeOffset(new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Utc), TimeSpan.FromMinutes(1)).Ticks.ToString());
        Show("dto utc kind before precision", () => new DateTimeOffset(new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Utc), TimeSpan.FromSeconds(30)).Ticks.ToString());
        Show("dto local kind", () => new DateTimeOffset(new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Local), TimeSpan.FromDays(1)).Ticks.ToString());
        Show("dto offset before ticks", () => new DateTimeOffset(long.MinValue, TimeSpan.FromSeconds(30)).Ticks.ToString());
        Show("dto ticks invalid", () => new DateTimeOffset(long.MinValue, TimeSpan.Zero).Ticks.ToString());
        Show("dto offset before date", () => new DateTimeOffset(0, 2, 29, 12, 34, 56, TimeSpan.FromSeconds(30)).Ticks.ToString());
        Show("dto utc before millisecond", () => new DateTimeOffset(1, 1, 1, 0, 0, 0, 1000, TimeSpan.FromMinutes(1)).Ticks.ToString());
        Show("dto tooffset precision", () => new DateTimeOffset(clock, east).ToOffset(TimeSpan.FromSeconds(30)).Ticks.ToString());
        Show("dto tooffset range", () => new DateTimeOffset(clock, east).ToOffset(TimeSpan.FromHours(15)).Ticks.ToString());
        Show("dto tooffset clock", () => DateTimeOffset.MaxValue.ToOffset(TimeSpan.FromHours(1)).Ticks.ToString());
        Show("dto add utc", () => new DateTimeOffset(DateTime.MaxValue.AddMinutes(-1), TimeSpan.FromMinutes(-1)).AddTicks(1).Ticks.ToString());
        Show("dto tryparse offset range", () => { bool ok = DateTimeOffset.TryParse("2024-02-29T12:34:56+14:30", out var d); return ok + ":" + d.Ticks + ":" + d.Offset.Ticks; });
        Show("dto tryparse utc range", () => { bool ok = DateTimeOffset.TryParse("0001-01-01T00:00:00+01:00", out var d); return ok + ":" + d.Ticks + ":" + d.Offset.Ticks; });
        Show("dto parse offset range", () => DateTimeOffset.Parse("2024-02-29T12:34:56+14:30").Ticks.ToString());
        Show("dto parse utc range", () => DateTimeOffset.Parse("0001-01-01T00:00:00+01:00").Ticks.ToString());
        Show("dto parse utc before offset", () => DateTimeOffset.Parse("0001-01-01T00:00:00+14:30").Ticks.ToString());
        Show("dto tryexact utc range", () => { bool ok = DateTimeOffset.TryParseExact("0001-01-01T00:00:00.0000000+01:00", "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d); return ok + ":" + d.Ticks + ":" + d.Offset.Ticks; });
        Show("dto parseexact offset range", () => DateTimeOffset.ParseExact("2024-02-29T12:34:56+14:30", "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture).Ticks.ToString());
        Show("dto tryexact later format", () => { bool ok = DateTimeOffset.TryParseExact("2024-02-29T12:34:56+14:30".AsSpan(), new[] { "yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-ddTHH:mm:ss'+14:30'" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d); return ok + ":" + d.Ticks; });
        Show("dto tryexact null formats reset", () => { var d = new DateTimeOffset(123456789, TimeSpan.Zero); bool ok = DateTimeOffset.TryParseExact("2024-02-29T12:34:56+00:00", (string[])null, CultureInfo.InvariantCulture, DateTimeStyles.None, out d); return ok + ":" + d.Ticks + ":" + d.Offset.Ticks; });
        Show("dto tryexact empty formats reset", () => { var d = new DateTimeOffset(123456789, TimeSpan.Zero); bool ok = DateTimeOffset.TryParseExact("2024-02-29T12:34:56+00:00", Array.Empty<string>(), CultureInfo.InvariantCulture, DateTimeStyles.None, out d); return ok + ":" + d.Ticks + ":" + d.Offset.Ticks; });
        string accepted = "yyyy-MM-ddTHH:mm:ss'+14:30'";
        foreach (string invalid in new string[] { null, "" })
        {
            Show("dto tryexact invalid first " + (invalid is null ? "null" : "empty"), () => { var d = new DateTimeOffset(123456789, TimeSpan.Zero); bool ok = DateTimeOffset.TryParseExact("2024-02-29T12:34:56+14:30", new[] { invalid, accepted }, CultureInfo.InvariantCulture, DateTimeStyles.None, out d); return ok + ":" + d.Ticks + ":" + d.Offset.Ticks; });
            Show("dto tryexact invalid later " + (invalid is null ? "null" : "empty"), () => { var d = new DateTimeOffset(123456789, TimeSpan.Zero); bool ok = DateTimeOffset.TryParseExact("2024-02-29T12:34:56+14:30", new[] { accepted, invalid }, CultureInfo.InvariantCulture, DateTimeStyles.None, out d); return ok + ":" + d.Ticks; });
        }
        Show("dto parseexact empty format", () => DateTimeOffset.ParseExact("2024-02-29", "", CultureInfo.InvariantCulture).Ticks.ToString());
        Show("dto parseexact both empty", () => DateTimeOffset.ParseExact("", "", CultureInfo.InvariantCulture).Ticks.ToString());
        Show("dto parseexact empty input", () => DateTimeOffset.ParseExact("", "O", CultureInfo.InvariantCulture).Ticks.ToString());
        Show("dto tryexact both empty reset", () => { var d = new DateTimeOffset(123456789, TimeSpan.Zero); bool ok = DateTimeOffset.TryParseExact("", "", CultureInfo.InvariantCulture, DateTimeStyles.None, out d); return ok + ":" + d.Ticks + ":" + d.Offset.Ticks; });
        Console.WriteLine("datetime offset boundaries end");
    }

    internal static void RunBinary()
    {
        Console.WriteLine("== datetime binary ==");
        foreach (DateTimeKind kind in new[] { DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local })
        {
            foreach (long ticks in new[] { 0L, 638713838451234567L, DateTime.MaxValue.Ticks })
            {
                var value = new DateTime(ticks, kind);
                long binary = value.ToBinary();
                var restored = DateTime.FromBinary(binary);
                Console.WriteLine("binary:" + kind + ":" + ticks + "=" + binary + ":" + restored.Ticks + ":" + restored.Kind);
            }
        }
        foreach (long value in new[] { DateTime.MaxValue.Ticks + 1, 0x4000000000000000L, long.MaxValue, long.MinValue, -1L })
            Show("binary raw:" + value, () => { var date = DateTime.FromBinary(value); return date.Ticks + ":" + date.Kind; });
        foreach (int hour in new[] { 5, 6 })
        {
            long utc = new DateTime(2024, 11, 3, hour, 30, 0, DateTimeKind.Utc).Ticks;
            long payload = utc | long.MinValue;
            var restored = DateTime.FromBinary(payload);
            Console.WriteLine("binary repeated:" + hour + "=" + restored.Ticks + ":" + (restored.ToBinary() == payload));
        }
        Show("binary invalid local", () => new DateTime(2024, 3, 10, 2, 30, 0, DateTimeKind.Local).ToBinary().ToString());
        Console.WriteLine("datetime binary end");
    }

    internal static void RunExactSpans()
    {
        Console.WriteLine("== datetime exact spans ==");
        foreach (string text in new[] { null, "", "invalid", "2025-01-01T12:34:56.1234567Z", "2025-01-01" })
        {
            foreach (string format in new[] { "O", "yyyy-MM-dd", "" })
            {
                string label = (text ?? "<null>") + ":" + format;
                Show("date exact string:" + label, () => { var value = new DateTime(123); bool ok = DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value); return ok + ":" + value.Ticks + ":" + value.Kind; });
                Show("date exact span:" + label, () => { var value = new DateTime(123); bool ok = DateTime.TryParseExact(text.AsSpan(), format.AsSpan(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value); return ok + ":" + value.Ticks + ":" + value.Kind; });
                Show("date parse exact string:" + label, () => { var value = DateTime.ParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind); return value.Ticks + ":" + value.Kind; });
                Show("date parse exact span:" + label, () => { var value = DateTime.ParseExact(text.AsSpan(), format.AsSpan(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind); return value.Ticks + ":" + value.Kind; });
            }
        }
        Console.WriteLine("datetime exact spans end");
    }
}
