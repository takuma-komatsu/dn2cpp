using System;
using System.Globalization;

internal static class DateTimePublicSurfaceSubset
{
    internal static void Run()
    {
        var culture = CultureInfo.InvariantCulture;
        var date = new DateTime(2026, 3, 4, 11, 12, 13, DateTimeKind.Utc).AddTicks(1234);
        date.Deconstruct(out DateOnly day, out TimeOnly clock);
        date.Deconstruct(out int year, out int month, out int dayOfMonth);
        Console.WriteLine($"DateTime fraction/deconstruct={date.AddMicroseconds(-1.25).Ticks - date.Ticks}:{date.Microsecond}:{date.Nanosecond}:{day.DayNumber}:{clock.Ticks}:{year}:{month}:{dayOfMonth}");
        var unspecified = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
        Console.WriteLine($"DateTime equality/type={DateTime.Equals(date, unspecified)}:{date.Equals((object)unspecified)}:{date.Equals((object)123)}:{date.CompareTo((object)null)}:{(int)date.GetTypeCode()}");
        try { date.CompareTo((object)123); }
        catch (ArgumentException e) { Console.WriteLine("DateTime compare type=" + e.Message); }

        Console.WriteLine("DateTime OLE=" + DateTime.FromOADate(-1.25).Ticks + ":" +
            DateTime.FromOADate(-1.25).ToOADate() + ":" + DateTime.FromOADate(-0.25).ToOADate() + ":" +
            new DateTime(TimeSpan.TicksPerHour * 6).ToOADate());
        try { DateTime.FromOADate(double.NaN); }
        catch (ArgumentException e) { Console.WriteLine("DateTime OLE invalid=" + e.Message); }
        try { new DateTime(99, 12, 31).ToOADate(); }
        catch (OverflowException e) { Console.WriteLine("DateTime OLE overflow=" + e.Message); }

        Console.WriteLine($"DateTime named formats={date.ToShortDateString()}|{date.ToLongDateString()}|{date.ToShortTimeString()}|{date.ToLongTimeString()}");
        Console.WriteLine("DateTime all formats=" + string.Join("|", date.GetDateTimeFormats()));
        Console.WriteLine("DateTime formats provider=" + string.Join("|", date.GetDateTimeFormats(culture)));
        Console.WriteLine("DateTime formats char=" + string.Join("|", date.GetDateTimeFormats('t')));
        Console.WriteLine("DateTime formats char/provider=" + string.Join("|", date.GetDateTimeFormats('g', culture)));
        try { date.GetDateTimeFormats('Q'); }
        catch (FormatException e) { Console.WriteLine("DateTime invalid format=" + e.Message); }

        var formats = new[] { "yyyyMMdd", "yyyy-MM-dd" };
        const string text = "2026-03-04";
        var span = (ReadOnlySpan<char>)text;
        Console.WriteLine("DateTime exact span array=" + DateTime.ParseExact(span, formats, culture, DateTimeStyles.None).Ticks);
        bool spanOk = DateTime.TryParseExact(span, formats, culture, DateTimeStyles.None, out var spanDate);
        bool stringOk = DateTime.TryParseExact(text, formats, culture, DateTimeStyles.None, out var stringDate);
        bool noMatch = DateTime.TryParseExact(text, new[] { "yyyyMMdd", null, "yyyy-MM-dd" }, culture, DateTimeStyles.None, out var miss);
        bool nullInput = DateTime.TryParseExact((string)null, formats, culture, DateTimeStyles.None, out var nullResult);
        Console.WriteLine($"DateTime try exact arrays={spanOk}:{spanDate.Ticks}:{stringOk}:{stringDate.Ticks}:{noMatch}:{miss.Ticks}:{nullInput}:{nullResult.Ticks}");

        const string offsetText = "2026-03-04+02:00";
        string[] offsetFormats = { "yyyy-MM-ddzzz" };
        const DateTimeStyles adjust = DateTimeStyles.AdjustToUniversal;
        var offsetSpan = offsetText.AsSpan();
        var adjusted = DateTimeOffset.ParseExact(offsetSpan, offsetFormats, culture, adjust);
        bool fromString = DateTimeOffset.TryParseExact(offsetText, offsetFormats, culture, adjust, out var stringOffset);
        bool fromSpan = DateTimeOffset.TryParseExact(offsetSpan, offsetFormats, culture, adjust, out var spanOffset);
        Console.WriteLine($"DateTimeOffset exact arrays={adjusted.ToString("O")}|{fromString}:{stringOffset.ToString("O")}|{fromSpan}:{spanOffset.ToString("O")}");

        Calendar calendar = new ShiftCalendar();
        WriteCalendar(new DateTime(2026, 3, 4, calendar));
        WriteCalendar(new DateTime(2026, 3, 4, 11, 12, 13, calendar));
        WriteCalendar(new DateTime(2026, 3, 4, 11, 12, 13, 14, calendar));
        WriteCalendar(new DateTime(2026, 3, 4, 11, 12, 13, 14, calendar, DateTimeKind.Utc));
        WriteCalendar(new DateTime(2026, 3, 4, 11, 12, 13, 14, 15, calendar));
        WriteCalendar(new DateTime(2026, 3, 4, 11, 12, 13, 14, 15, calendar, DateTimeKind.Local));
        try { WriteCalendar(new DateTime(2026, 3, 4, 11, 12, 13, 1000, calendar, (DateTimeKind)9)); }
        catch (ArgumentException e) { Console.WriteLine("DateTime calendar argument order=" + e.ParamName); }
        Console.WriteLine($"DateTime fields={DateTime.MinValue.Ticks}:{DateTime.MaxValue.Ticks}:{DateTime.UnixEpoch.Ticks}:{DateTime.UnixEpoch.Kind}");
        Console.WriteLine("DateTime public surface end");
    }

    private static void WriteCalendar(DateTime date) => Console.WriteLine($"DateTime calendar={date.Ticks}:{date.Kind}");

    // Return a different day and Kind to check virtual dispatch and Kind replacement.
    private sealed class ShiftCalendar : GregorianCalendar
    {
        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) =>
            DateTime.SpecifyKind(base.ToDateTime(year, month, day, hour, minute, second, millisecond, era).AddDays(1), DateTimeKind.Utc);
    }
}
