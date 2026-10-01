using System;
using System.Globalization;

namespace DateTimeArgumentFieldsSubset;

internal static class Program
{
    private static void ProbeFields(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine(label + ": no exception");
        }
        catch (Exception ex)
        {
            GC.Collect();
            string parameter = ex is ArgumentException arg ? arg.ParamName : "";
            object actual = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
            Console.WriteLine(label + ": " + ex.GetType().FullName + " param=" + parameter
                + " actual=" + (actual is null ? "<null>" : actual.ToString())
                + " actual-type=" + (actual is null ? "<null>" : actual.GetType().Name)
                + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
        }
    }

    // The argument a DateTime constructor refuses first when several are bad: the
    // ticks, then the kind; or the kind, the date, the clock, then the millisecond.
    internal static void RunConstructorOrder()
    {
        Console.WriteLine("== datetime argument fields ==");
        ProbeFields("datetime kind before date", () => new DateTime(2020, 13, 1, 24, 0, 0, (DateTimeKind)5));
        ProbeFields("datetime date before clock with kind",
            () => new DateTime(2020, 13, 1, 24, 0, 0, DateTimeKind.Utc));
        ProbeFields("datetime kind before date with millisecond",
            () => new DateTime(2020, 13, 1, 0, 0, 0, 1000, (DateTimeKind)3));
        ProbeFields("datetime date before clock with millisecond", () => new DateTime(2020, 13, 1, 24, 0, 0, 1000));
        ProbeFields("datetimeoffset date before clock", () => new DateTimeOffset(2020, 13, 1, 24, 0, 0, TimeSpan.Zero));
        ProbeFields("datetime ticks before kind", () => new DateTime(-1, (DateTimeKind)5));
        ProbeFields("datetime ticks kind", () => new DateTime(0, (DateTimeKind)3));
        ProbeFields("datetime specifykind", () => DateTime.SpecifyKind(DateTime.MinValue, (DateTimeKind)(-1)));
        ProbeFields("dateonly todatetime kind",
            () => new DateOnly(2020, 1, 1).ToDateTime(new TimeOnly(1, 0), (DateTimeKind)5));
        ProbeFields("datetime date and time kind",
            () => new DateTime(new DateOnly(2020, 1, 1), new TimeOnly(1, 0), (DateTimeKind)5));
        DateTime dateAndTime = new DateTime(new DateOnly(2020, 1, 2), new TimeOnly(3, 4, 5));
        Console.WriteLine($"datetime date and time: ticks={dateAndTime.Ticks} kind={dateAndTime.Kind}");
        DateTime dateAndTimeUtc = new DateTime(new DateOnly(2020, 1, 2), new TimeOnly(3, 4, 5), DateTimeKind.Utc);
        Console.WriteLine($"datetime date and time utc: ticks={dateAndTimeUtc.Ticks} kind={dateAndTimeUtc.Kind}");
        DateTime local = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Local);
        Console.WriteLine($"datetime local: ticks={local.Ticks} kind={local.Kind}");
        ProbeFields("datetime.daysInMonth", () => DateTime.DaysInMonth(2020, 13));
        ProbeFields("datetime.millisecond", () => _ = new DateTime(2020, 1, 1, 0, 0, 0, 1000));
        ProbeFields("datetime.fromFileTimeUtc", () => DateTime.FromFileTimeUtc(-1));
        ProbeFields("timespan.parseExactFormat", () => TimeSpan.ParseExact("0", (string)null, null));
        ProbeFields("datetimeoffset.parseNull", () => DateTimeOffset.Parse(null));
        ProbeFields("datetime millisecond", () => new DateTime(2020, 1, 1, 0, 0, 0, 1000));
        ProbeFields("datetime negative millisecond", () => new DateTime(2020, 1, 1, 0, 0, 0, -1));
        ProbeFields("datetime millisecond kind", () => new DateTime(2020, 1, 1, 0, 0, 0, 1000, DateTimeKind.Utc));
        ProbeFields("datetimeoffset millisecond", () => new DateTimeOffset(2020, 1, 1, 0, 0, 0, 1000, TimeSpan.Zero));
        ProbeFields("timeonly millisecond", () => new TimeOnly(0, 0, 0, 1000));
        ProbeFields("datetime month", () => new DateTime(2020, 13, 1));
        ProbeFields("datetime day", () => new DateTime(2020, 2, 30));
        ProbeFields("datetime year zero", () => new DateTime(0, 1, 1));
        ProbeFields("datetime year past 9999", () => new DateTime(10000, 1, 1));
        ProbeFields("datetime date before clock", () => new DateTime(2020, 13, 1, 24, 0, 0));
        ProbeFields("dateonly month", () => new DateOnly(2020, 13, 1));
        ProbeFields("datetime hour", () => new DateTime(2020, 1, 1, 24, 0, 0));
        ProbeFields("datetime minute", () => new DateTime(2020, 1, 1, 0, 60, 0));
        ProbeFields("datetime clock before millisecond", () => new DateTime(2020, 1, 1, 0, 0, 60, 1000));
        ProbeFields("timeonly hour", () => new TimeOnly(24, 0));
        ProbeFields("datetime toFileTimeUtc before 1601", () => DateTime.MinValue.ToFileTimeUtc());
        ProbeFields("datetime daysInMonth year", () => DateTime.DaysInMonth(0, 1));
        ProbeFields("datetime daysInMonth month before year", () => DateTime.DaysInMonth(0, 13));
        ProbeFields("datetime isLeapYear year", () => DateTime.IsLeapYear(10000));
        ProbeFields("dateonly addDays past max", () => DateOnly.MaxValue.AddDays(1));
        ProbeFields("dateonly addDays wrapping", () => DateOnly.MaxValue.AddDays(int.MaxValue));
        ProbeFields("dateonly addMonths past max", () => DateOnly.MaxValue.AddMonths(1));
        ProbeFields("dateonly addMonths before min", () => DateOnly.MinValue.AddMonths(-1));
        ProbeFields("dateonly addMonths count", () => DateOnly.MinValue.AddMonths(int.MaxValue));
        ProbeFields("dateonly addYears past max", () => DateOnly.MaxValue.AddYears(1));
        ProbeFields("dateonly addYears count", () => DateOnly.MinValue.AddYears(int.MaxValue));
        ProbeFields("dateonly addYears leap day", () => new DateOnly(2020, 2, 29).AddYears(1));
        Console.WriteLine("datetime constructor order end");
    }
    internal static void Run()
    {
        RunConstructorOrder();
        ProbeFields("datetime method add", () => DateTime.MaxValue.Add(TimeSpan.FromTicks(1)));
        ProbeFields("datetime operator add", () => _ = DateTime.MaxValue + TimeSpan.FromTicks(1));
        ProbeFields("datetime method subtract", () => DateTime.MinValue.Subtract(TimeSpan.FromTicks(1)));
        ProbeFields("datetime operator subtract", () => _ = DateTime.MinValue - TimeSpan.FromTicks(1));
        ProbeFields("datetime subtract minimum", () => DateTime.MinValue.Subtract(TimeSpan.MinValue));
        ProbeFields("datetime operator minimum", () => _ = DateTime.MaxValue - TimeSpan.MinValue);
        ProbeFields("dto subtract minimum", () => DateTimeOffset.MinValue.Subtract(TimeSpan.MinValue));
        ProbeFields("dto operator minimum", () => _ = DateTimeOffset.MaxValue - TimeSpan.MinValue);
        ProbeFields("datetime add months count", () => DateTime.MinValue.AddMonths(int.MaxValue));
        ProbeFields("datetime add months boundary", () => DateTime.MaxValue.AddMonths(1));
        ProbeFields("datetime add years count", () => DateTime.MinValue.AddYears(int.MaxValue));
        ProbeFields("datetime add years boundary", () => DateTime.MinValue.AddYears(-1));
        ProbeFields("datetime add days infinity", () => DateTime.MinValue.AddDays(double.PositiveInfinity));
        var date = new DateTime(2024, 2, 29, 12, 34, 56);
        Console.WriteLine("add fractional ticks=" + (date.AddSeconds(1.0000001).Ticks - date.Ticks));
        Console.WriteLine("add negative fractional ticks=" + (date.AddSeconds(-1.0000001).Ticks - date.Ticks));
        Console.WriteLine("add NaN unchanged=" + (date.AddDays(double.NaN).Ticks == date.Ticks));
        Console.WriteLine("subtract regular ticks=" + date.Subtract(TimeSpan.FromTicks(1)).Ticks);
        Console.WriteLine("datetime argument fields end");
    }
}
