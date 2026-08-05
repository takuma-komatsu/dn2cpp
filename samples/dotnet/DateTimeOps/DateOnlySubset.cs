#nullable enable
using System;
using System.Collections.Generic;

// System.DateOnly as an intrinsic value type (Dn2CppDateOnly), modeled like
// System.DateTime / DateTimeOffset. The real corelib type reaches Number
// formatting / calendar / culture, so it is lowered to runtime helpers (InvariantCulture
// only). Covers construction, day-number conversions, the date properties, arithmetic
// (AddDays/AddMonths/AddYears with month-end clamping), comparison / equality / hashing,
// default + format ToString, and Parse / TryParse / ParseExact. ToDateTime(TimeOnly) is
// not covered (requires the TimeOnly intrinsic). DayOfWeek is printed as an int — boxing a
// corelib enum to its *name* is a pre-existing gap (same convention as the DateTime /
// DateTimeOffset sections). CoreLib only.
namespace DateOnlySubset;

static class Program
{
    static void Show(object v) => Console.WriteLine(v + " :: " + v.GetType().Name);

    internal static void __GateEntry()
    {
        var d = new DateOnly(2026, 6, 27);

        Console.WriteLine("-- DateOnly ctor / DayNumber / factories --");
        Console.WriteLine(d.ToString());
        Console.WriteLine("DayNumber=" + d.DayNumber);
        Console.WriteLine(DateOnly.FromDayNumber(739428).ToString());
        Console.WriteLine("Min=" + DateOnly.MinValue + " (" + DateOnly.MinValue.DayNumber + ")");
        Console.WriteLine("Max=" + DateOnly.MaxValue + " (" + DateOnly.MaxValue.DayNumber + ")");
        Console.WriteLine(DateOnly.FromDateTime(new DateTime(2026, 6, 27, 14, 5, 9)).ToString());
        Show(d); // boxed ToString + runtime type name

        Console.WriteLine("-- properties --");
        Console.WriteLine(d.Year + "-" + d.Month + "-" + d.Day + " dow=" + (int)d.DayOfWeek + " doy=" + d.DayOfYear);
        var leap = new DateOnly(2024, 2, 29);
        Console.WriteLine(leap.Year + "-" + leap.Month + "-" + leap.Day + " dow=" + (int)leap.DayOfWeek + " doy=" + leap.DayOfYear);

        Console.WriteLine("-- arithmetic (month-end clamp like DateTime) --");
        Console.WriteLine(d.AddDays(10));
        Console.WriteLine(d.AddDays(-40));
        Console.WriteLine(new DateOnly(2024, 1, 31).AddMonths(1));
        Console.WriteLine(d.AddMonths(8));
        Console.WriteLine(d.AddYears(-2));
        Console.WriteLine(new DateOnly(2024, 2, 29).AddYears(1));

        Console.WriteLine("-- comparison / equality --");
        var e = new DateOnly(2026, 6, 28);
        Console.WriteLine("d<e:" + (d < e) + " d<=d2:" + (d <= new DateOnly(2026, 6, 27)) + " d>e:" + (d > e));
        Console.WriteLine("d==d2:" + (d == new DateOnly(2026, 6, 27)) + " d!=e:" + (d != e));
        Console.WriteLine("CompareTo: " + d.CompareTo(e) + " / " + e.CompareTo(d) + " / " + d.CompareTo(new DateOnly(2026, 6, 27)));
        Console.WriteLine("Equals: " + d.Equals(new DateOnly(2026, 6, 27)));

        Console.WriteLine("-- ToString(format) --");
        foreach (var f in new[] { "d", "D", "M", "m", "O", "o", "R", "r", "Y", "y", "yyyy-MM-dd", "ddd MMM d, yyyy", "dd/MM/yyyy" })
            Console.WriteLine(f + " = " + d.ToString(f));

        Console.WriteLine("-- Parse / TryParse / ParseExact --");
        Console.WriteLine(DateOnly.Parse("06/27/2026"));
        Console.WriteLine(DateOnly.Parse("2026-06-27"));
        Console.WriteLine(DateOnly.ParseExact("2026-06-27", "yyyy-MM-dd"));
        Console.WriteLine(DateOnly.ParseExact("27 Jun 2026", "dd MMM yyyy"));
        Console.WriteLine("TryParse ok: " + DateOnly.TryParse("06/27/2026", out DateOnly dp) + " -> " + dp);
        Console.WriteLine("TryParse bad: " + DateOnly.TryParse("garbage", out DateOnly _));

        Console.WriteLine("-- box (object) --");
        object bo = d;
        Console.WriteLine(bo.ToString());
        Console.WriteLine("box Equals: " + bo.Equals(new DateOnly(2026, 6, 27)));
        Console.WriteLine("hash eq: " + (((object)d).GetHashCode() == ((object)new DateOnly(2026, 6, 27)).GetHashCode()));

        Console.WriteLine("-- DateOnly as a Dictionary key --");
        var map = new Dictionary<DateOnly, string>();
        map[d] = "first";
        map[new DateOnly(2026, 6, 27)] = "overwritten"; // same value -> same key
        Console.WriteLine("count=" + map.Count + " value=" + map[d]);
    }
}
