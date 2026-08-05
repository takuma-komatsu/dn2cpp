#nullable enable
using System;
using System.Collections.Generic;

// System.DateTimeOffset as an intrinsic value type (Dn2CppDateTimeOffset),
// modeled like System.DateTime. The real corelib type reaches Number formatting /
// InternalCall (clock, calendar) / culture, so it is lowered to runtime helpers. Covers
// the deterministic surface — construction, the clock/UTC properties, Unix-time
// conversions, instant-based equality / comparison / hashing, DateTime<->DateTimeOffset
// conversion, ToOffset, the default invariant ToString(), ToString(format) (with
// z/zz/zzz/K offset specifiers), Parse / TryParse / ParseExact, and arithmetic
// (Add* / +/- TimeSpan) — all exact-diffable vs real .NET. Now / UtcNow are
// non-deterministic (only invariants are asserted). Named / historical time zones stay
// a carve-out (host local zone only). CoreLib only.
namespace DateTimeOffsetSubset;


static class Program
{
    static void Show(object v) => Console.WriteLine(v + " :: " + v.GetType().Name);internal static void __GateEntry()
    {
        // ---- construction ----
        // (year, month, day, hour, minute, second, offset)
        var a = new DateTimeOffset(2020, 1, 15, 10, 30, 0, TimeSpan.FromHours(5));
        // (year, ..., millisecond, offset)
        var b = new DateTimeOffset(2020, 1, 15, 10, 30, 0, 250, TimeSpan.FromHours(-8));
        // (DateTime[Unspecified], TimeSpan)
        var c = new DateTimeOffset(new DateTime(2021, 7, 4, 9, 0, 0), new TimeSpan(5, 30, 0));
        // (DateTime[Utc]) -> offset 0
        var u = new DateTimeOffset(new DateTime(2022, 3, 1, 12, 0, 0, DateTimeKind.Utc));
        // (long ticks, offset)
        var t = new DateTimeOffset(637134210000000000L, TimeSpan.Zero);

        Console.WriteLine("-- ToString (default invariant) --");
        Console.WriteLine(a.ToString());
        Console.WriteLine(b.ToString());
        Console.WriteLine(c.ToString());
        Console.WriteLine(u.ToString());
        Show(a); // boxed ToString + runtime type name

        Console.WriteLine("-- components (read the clock) --");
        Console.WriteLine(a.Year + "-" + a.Month + "-" + a.Day + " " + a.Hour + ":" + a.Minute + ":" + a.Second + "." + b.Millisecond);
        // DayOfWeek/Kind are printed as ints — boxing a corelib enum to its *name* is a
        // pre-existing gap (the DateTimeSubset gate uses the same (int) convention).
        Console.WriteLine("DayOfWeek=" + (int)a.DayOfWeek + " DayOfYear=" + a.DayOfYear);
        Console.WriteLine("Ticks=" + a.Ticks + " UtcTicks=" + a.UtcTicks);
        Console.WriteLine("Offset=" + a.Offset + " / " + b.Offset);
        Console.WriteLine("DateTime=" + a.DateTime + " (Kind=" + (int)a.DateTime.Kind + ")");
        Console.WriteLine("UtcDateTime=" + a.UtcDateTime + " (Kind=" + (int)a.UtcDateTime.Kind + ")");
        Console.WriteLine("Date=" + a.Date + " TimeOfDay=" + a.TimeOfDay);

        Console.WriteLine("-- LocalDateTime invariants (host-dependent value) --");
        Console.WriteLine("LocalDateTime.Kind=Local: " + (a.LocalDateTime.Kind == DateTimeKind.Local));
        Console.WriteLine("LocalDateTime same instant: " + (a.LocalDateTime.ToUniversalTime() == a.UtcDateTime));

        Console.WriteLine("-- Unix time (deterministic) --");
        var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Console.WriteLine("epoch seconds=" + epoch.ToUnixTimeSeconds() + " millis=" + epoch.ToUnixTimeMilliseconds());
        Console.WriteLine("a seconds=" + a.ToUnixTimeSeconds() + " millis=" + a.ToUnixTimeMilliseconds());
        DateTimeOffset fromS = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000L);
        DateTimeOffset fromMs = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_500L);
        Console.WriteLine("FromUnixTimeSeconds=" + fromS);
        Console.WriteLine("FromUnixTimeMilliseconds=" + fromMs);
        Console.WriteLine("unix round trip: " + (DateTimeOffset.FromUnixTimeSeconds(a.ToUnixTimeSeconds()).ToUnixTimeSeconds() == a.ToUnixTimeSeconds()));

        Console.WriteLine("-- equality / comparison (by UTC instant) --");
        // Same instant, different offsets: 10:30+05:00 == 05:30+00:00.
        var sameInstant = new DateTimeOffset(2020, 1, 15, 5, 30, 0, TimeSpan.Zero);
        Console.WriteLine("a == sameInstant: " + (a == sameInstant));
        Console.WriteLine("a.Equals(sameInstant): " + a.Equals(sameInstant));
        Console.WriteLine("hash agrees: " + (a.GetHashCode() == sameInstant.GetHashCode()));
        Console.WriteLine("a != b: " + (a != b));
        Console.WriteLine("a < b: " + (a < b) + " a > b: " + (a > b));
        Console.WriteLine("CompareTo: " + a.CompareTo(b) + " / " + b.CompareTo(a) + " / " + a.CompareTo(sameInstant));
        Console.WriteLine("a - b = " + (a - b).TotalHours + "h");

        Console.WriteLine("-- ToOffset (same instant, new offset) --");
        DateTimeOffset shifted = a.ToOffset(TimeSpan.FromHours(-3));
        Console.WriteLine(shifted + " same instant: " + (shifted == a));

        Console.WriteLine("-- MinValue / MaxValue / ticks ctor --");
        Console.WriteLine("Min=" + DateTimeOffset.MinValue.Ticks + " Max=" + DateTimeOffset.MaxValue.Ticks);
        Console.WriteLine("ticks ctor: " + t);

        Console.WriteLine("-- implicit DateTime(Utc) -> DateTimeOffset --");
        DateTimeOffset implicitDto = new DateTime(2022, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        Console.WriteLine(implicitDto + " offset0: " + (implicitDto.Offset == TimeSpan.Zero));

        Console.WriteLine("-- DateTimeOffset as a Dictionary key --");
        var map = new Dictionary<DateTimeOffset, string>();
        map[a] = "first";
        map[sameInstant] = "overwritten"; // same instant -> same key
        Console.WriteLine("count=" + map.Count + " value=" + map[a]);

        var neg = new DateTimeOffset(2020, 7, 4, 9, 5, 6, TimeSpan.FromMinutes(-330)); // -05:30

        Console.WriteLine("-- ToString(format) --");
        // Standard specifiers: O/o (round-trip with offset), R/r and u render the UTC instant,
        // s/G/g/D/F/t use the clock with no offset.
        Console.WriteLine("O = " + a.ToString("O"));
        Console.WriteLine("R = " + a.ToString("R"));
        Console.WriteLine("u = " + a.ToString("u"));
        Console.WriteLine("s = " + a.ToString("s"));
        Console.WriteLine("G = " + a.ToString("G"));
        Console.WriteLine("D = " + a.ToString("D"));
        Console.WriteLine("F = " + a.ToString("F"));
        // Custom offset specifiers: z (hours), zz (2-digit hours), zzz / K (HH:mm).
        Console.WriteLine("z = " + a.ToString("yyyy-MM-dd HH:mm:ss z"));
        Console.WriteLine("zzz = " + a.ToString("yyyy-MM-dd HH:mm:ss zzz"));
        Console.WriteLine("K = " + a.ToString("yyyy/MM/dd HH:mm:ss K"));
        Console.WriteLine("neg z = " + neg.ToString("yyyy-MM-dd HH:mm:ss z"));
        Console.WriteLine("neg zz = " + neg.ToString("yyyy-MM-dd HH:mm:ss zz"));
        Console.WriteLine("neg zzz = " + neg.ToString("ddd, dd MMM yyyy HH:mm:ss zzz"));

        Console.WriteLine("-- Parse / TryParse --");
        Console.WriteLine("iso+off = " + DateTimeOffset.Parse("2020-01-15T10:30:00+05:00").ToString("O"));
        Console.WriteLine("spaced  = " + DateTimeOffset.Parse("2020-01-15 10:30:00 +05:00").ToString("O"));
        Console.WriteLine("Z       = " + DateTimeOffset.Parse("2020-01-15T10:30:00Z").ToString("O"));
        Console.WriteLine("us+off  = " + DateTimeOffset.Parse("01/15/2020 10:30:00 +05:00").ToString("O"));
        Console.WriteLine("neg off = " + DateTimeOffset.Parse("2020-07-04T09:05:06-05:30").ToString("O"));
        Console.WriteLine("frac    = " + DateTimeOffset.Parse("2020-01-15T10:30:00.2500000+05:00").ToString("O"));
        Console.WriteLine("TryParse ok: " + DateTimeOffset.TryParse("2020-01-15T10:30:00+05:00", out DateTimeOffset tp) + " -> " + tp.ToString("O"));
        Console.WriteLine("TryParse bad: " + DateTimeOffset.TryParse("not a date", out DateTimeOffset _));

        Console.WriteLine("-- ParseExact --");
        Console.WriteLine("O round trip equal: " + (DateTimeOffset.ParseExact(a.ToString("O"), "O", null) == a));
        Console.WriteLine("custom zzz = " + DateTimeOffset.ParseExact("2020-01-15 10:30:00 +05:00", "yyyy-MM-dd HH:mm:ss zzz", null).ToString("O"));

        Console.WriteLine("-- arithmetic (offset preserved, acts on clock) --");
        Console.WriteLine("AddDays(3) = " + a.AddDays(3).ToString("O"));
        Console.WriteLine("AddHours(20) = " + a.AddHours(20).ToString("O"));
        Console.WriteLine("AddMinutes(45) = " + a.AddMinutes(45).ToString("O"));
        Console.WriteLine("AddSeconds(90) = " + a.AddSeconds(90).ToString("O"));
        Console.WriteLine("AddMilliseconds(1500) = " + a.AddMilliseconds(1500).ToString("O"));
        Console.WriteLine("AddTicks(5) = " + a.AddTicks(5).ToString("O"));
        Console.WriteLine("AddMonths(13) = " + a.AddMonths(13).ToString("O"));
        Console.WriteLine("AddYears(2) = " + a.AddYears(2).ToString("O"));
        Console.WriteLine("Add(90min) = " + a.Add(TimeSpan.FromMinutes(90)).ToString("O"));
        Console.WriteLine("Subtract(90min) = " + a.Subtract(TimeSpan.FromMinutes(90)).ToString("O"));
        Console.WriteLine("a + 1h = " + (a + TimeSpan.FromHours(1)).ToString("O"));
        Console.WriteLine("a - 1h = " + (a - TimeSpan.FromHours(1)).ToString("O"));
        Console.WriteLine("a.Subtract(b) hours = " + a.Subtract(b).TotalHours);
        Console.WriteLine("offset preserved: " + (a.AddDays(3).Offset == a.Offset));

        Console.WriteLine("-- Now / UtcNow (invariants only) --");
        DateTimeOffset now = DateTimeOffset.Now, utcNow = DateTimeOffset.UtcNow;
        Console.WriteLine("UtcNow.Offset == Zero: " + (utcNow.Offset == TimeSpan.Zero));
        Console.WriteLine("Now/UtcNow same instant: " + (Math.Abs((now - utcNow).TotalSeconds) < 5));
        Console.WriteLine("Now.Year sane: " + (now.Year >= 2020 && now.Year < 2100));
    }
}
