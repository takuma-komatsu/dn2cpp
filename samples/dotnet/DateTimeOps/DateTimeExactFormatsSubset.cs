using System;
using System.Globalization;

internal static class DateTimeExactFormatsSubset
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    internal static void Run()
    {
        Console.WriteLine("== DateTime exact format arrays ==");
        const string day = "2026-11-27";
        string[] formats = { "yyyyMMdd", "yyyy-MM-dd" };
        Console.WriteLine("exact array date later=" + DateTime.ParseExact(day, formats, Culture, DateTimeStyles.None).Ticks);
        Console.WriteLine("exact array offset later=" +
            DateTimeOffset.ParseExact(day, formats, Culture, DateTimeStyles.AssumeUniversal).UtcTicks);

        // These are the edge whitespace flags and the offset format used by XmlConvert.
        string[] xml = { "yyyyMMdd", "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz", "yyyy-MM-ddZ" };
        const DateTimeStyles whites = DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite;
        const string stamp = "\u00a02026-11-27T08:45:01.5+02:00\t";
        var dt = DateTime.ParseExact(stamp, xml, Culture, whites | DateTimeStyles.AdjustToUniversal);
        var dto = DateTimeOffset.ParseExact(stamp, xml, Culture, whites);
        Console.WriteLine($"exact array xml={dt.Ticks}:{dt.Kind}:{dto.UtcTicks}:{dto.Offset.TotalMinutes}");
        var utc = DateTime.ParseExact(day + "Z", xml, Culture, DateTimeStyles.RoundtripKind);
        Console.WriteLine($"exact array roundtrip={utc.Ticks}:{utc.Kind}");
        string[] monthDay = { "--MM-dd" };
        var partial = DateTimeOffset.ParseExact("--03-04", monthDay, Culture, whites);
        bool shortPrefix = DateTime.TryParseExact("-03-04", monthDay, Culture, whites, out _);
        Console.WriteLine($"exact array XML month/day={partial.Month}:{partial.Day}:{shortPrefix}");
        TestBoth("XML midnight", "2026-03-04T24:00:00+00:00", xml, whites | DateTimeStyles.AdjustToUniversal);

        TestBoth("first wins", "2026-03-04", new[] { "yyyy-dd-MM", "yyyy-MM-dd" }, DateTimeStyles.AssumeUniversal);
        TestBoth("exhausted", day, new[] { "yyyyMMdd", "dd/MM/yyyy" }, DateTimeStyles.None);
        TestBoth("null entry before", day, new[] { null, formats[1] }, DateTimeStyles.None);
        TestBoth("null entry after", day, new[] { formats[1], null }, DateTimeStyles.AssumeUniversal);
        TestBoth("null formats", day, null, DateTimeStyles.None);
        TestBoth("empty formats", day, Array.Empty<string>(), DateTimeStyles.None);
        TestBoth("empty input first", "", Array.Empty<string>(), DateTimeStyles.None);
        TestBoth("style first", null, formats, (DateTimeStyles)0x400);
        TestBoth("RFC1123", "Wed, 04 Mar 2026 11:05:21 GMT", new[] { "r" }, DateTimeStyles.RoundtripKind);
        TestBoth("sortable universal", "2026-03-04 11:05:21Z", new[] { "u" }, DateTimeStyles.RoundtripKind);
        TestBoth("weekday mismatch", "Sun, 04 Mar 2026 11:05:21 GMT", new[] { "r" }, DateTimeStyles.None);
        TestBoth("invalid quote fallback", day + "Z", new[] { "yyyy-MM-dd'Z", "yyyy-MM-ddZ" }, DateTimeStyles.AssumeUniversal);
        TestBoth("strict widths", "26-3-4", new[] { "yyyy-MM-dd", "yy-M-d" }, DateTimeStyles.AssumeUniversal);
        TestBoth("clock candidates", "0000002026-03-04T13:35",
            new[] { "yyyyyyyyyy-MM-ddThh:mm", "yyyyyyyyyy-dd-MMTHHH:mm" }, DateTimeStyles.None);
        TestBoth("quoted whitespace", "2026  New   York 03\t", new[] { "yyyy' New York 'MM' '" },
            DateTimeStyles.AllowInnerWhite | DateTimeStyles.AllowTrailingWhite);
        TestBoth("UTC suffix", "2026-03-04 11:05:21GMT", new[] { "u" }, DateTimeStyles.None);
        TestBoth("roundtrip offset", "2026-03-04T11:05:21.0000000+0100", new[] { "O" }, DateTimeStyles.None);
        var today = DateTime.UtcNow.Date;
        var utcTime = DateTime.ParseExact("04:00", new[] { "HH:mm" }, Culture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        Console.WriteLine("exact array UTC date=" + (utcTime.Date == today || utcTime.Date == DateTime.UtcNow.Date));
        Console.WriteLine("DateTime exact format arrays end");
    }

    private static void TestBoth(string label, string input, string[] formats, DateTimeStyles styles)
    {
        try
        {
            Console.WriteLine("exact array date " + label + "=" + DateTime.ParseExact(input, formats, Culture, styles).Ticks);
        }
        catch (FormatException e) { Console.WriteLine("exact array date " + label + "=FormatException:" + e.Message); }
        catch (ArgumentException e) { Console.WriteLine("exact array date " + label + "=" + e.GetType().Name + ":" + e.ParamName); }

        try
        {
            Console.WriteLine("exact array offset " + label + "=" + DateTimeOffset.ParseExact(input, formats, Culture, styles).Ticks);
        }
        catch (FormatException e) { Console.WriteLine("exact array offset " + label + "=FormatException:" + e.Message); }
        catch (ArgumentException e) { Console.WriteLine("exact array offset " + label + "=" + e.GetType().Name + ":" + e.ParamName); }
    }
}
