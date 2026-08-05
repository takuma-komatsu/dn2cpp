#nullable enable
using System;
using System.Collections.Generic;

// System.TimeOnly as an intrinsic value type (Dn2CppTimeOnly = a tick count
// since midnight, 0..863999999999), modeled like System.TimeSpan / DateTime. The real
// corelib type reaches Number formatting / calendar / culture, so it is lowered to runtime
// helpers (InvariantCulture only). Covers construction, FromTimeSpan/FromDateTime, the time
// properties, wrapping arithmetic (Add/AddHours/AddMinutes), the wrapped TimeOnly - TimeOnly
// difference, ToTimeSpan, comparison / equality / hashing, default + format ToString, and
// Parse / TryParse / ParseExact. Also exercises DateOnly.ToDateTime(TimeOnly).
// CoreLib only.
namespace TimeOnlySubset;

static class Program
{
    static void Show(object v) => Console.WriteLine(v + " :: " + v.GetType().Name);

    internal static void __GateEntry()
    {
        var t = new TimeOnly(14, 5, 9);

        Console.WriteLine("-- TimeOnly ctors / Ticks --");
        Console.WriteLine(t.ToString());
        Console.WriteLine("Ticks=" + t.Ticks);
        Console.WriteLine(new TimeOnly(9, 30).ToString());
        Console.WriteLine(new TimeOnly(14, 5, 9, 250).ToString());
        Console.WriteLine(new TimeOnly(531090000000L).ToString());
        Console.WriteLine("Min=" + TimeOnly.MinValue + " (" + TimeOnly.MinValue.Ticks + ")");
        Console.WriteLine("Max=" + TimeOnly.MaxValue + " (" + TimeOnly.MaxValue.Ticks + ")");
        Console.WriteLine(TimeOnly.FromTimeSpan(new TimeSpan(3, 15, 30)).ToString());
        Console.WriteLine(TimeOnly.FromDateTime(new DateTime(2026, 6, 27, 14, 5, 9)).ToString());
        Show(t); // boxed ToString + runtime type name

        Console.WriteLine("-- properties --");
        Console.WriteLine($"{t.Hour} {t.Minute} {t.Second} {t.Millisecond}");
        var f = new TimeOnly(14, 5, 9, 250);
        Console.WriteLine($"{f.Hour} {f.Minute} {f.Second} {f.Millisecond}");

        Console.WriteLine("-- arithmetic (wraps within 24h) --");
        Console.WriteLine(t.Add(new TimeSpan(2, 0, 0)));
        Console.WriteLine(t.AddHours(12));
        Console.WriteLine(t.AddHours(-20));
        Console.WriteLine(t.AddMinutes(90));
        Console.WriteLine(new TimeOnly(23, 30).AddHours(1));
        Console.WriteLine(t.ToTimeSpan());

        Console.WriteLine("-- subtraction (TimeOnly - TimeOnly = wrapped TimeSpan) --");
        Console.WriteLine(new TimeOnly(15, 0, 0) - new TimeOnly(14, 0, 0));
        Console.WriteLine(new TimeOnly(1, 0, 0) - new TimeOnly(23, 0, 0));

        Console.WriteLine("-- comparison / equality --");
        var e = new TimeOnly(14, 5, 10);
        Console.WriteLine("t<e:" + (t < e) + " t<=t2:" + (t <= new TimeOnly(14, 5, 9)) + " t>e:" + (t > e));
        Console.WriteLine("t==t2:" + (t == new TimeOnly(14, 5, 9)) + " t!=e:" + (t != e));
        Console.WriteLine("CompareTo: " + t.CompareTo(e) + " / " + e.CompareTo(t));
        Console.WriteLine("Equals: " + t.Equals(new TimeOnly(14, 5, 9)));

        Console.WriteLine("-- ToString(format) --");
        foreach (var fmt in new[] { "t", "T", "o", "O", "r", "R", "HH:mm", "HH:mm:ss.fff", "h:mm tt" })
            Console.WriteLine(fmt + " = " + f.ToString(fmt));

        Console.WriteLine("-- Parse / TryParse / ParseExact --");
        Console.WriteLine(TimeOnly.Parse("14:05"));
        Console.WriteLine(TimeOnly.Parse("14:05:09"));
        Console.WriteLine(TimeOnly.Parse("14:05:09.2500000"));
        Console.WriteLine(TimeOnly.ParseExact("14:05:09", "HH:mm:ss"));
        Console.WriteLine(TimeOnly.ParseExact("02:05 PM", "h:mm tt"));
        Console.WriteLine("TryParse ok: " + TimeOnly.TryParse("14:05", out TimeOnly tp) + " -> " + tp);
        Console.WriteLine("TryParse bad: " + TimeOnly.TryParse("garbage", out TimeOnly _));

        Console.WriteLine("-- box / dict --");
        object bo = t;
        Console.WriteLine(bo.ToString());
        Console.WriteLine("box Equals: " + bo.Equals(new TimeOnly(14, 5, 9)));
        Console.WriteLine("hash eq: " + (((object)t).GetHashCode() == ((object)new TimeOnly(14, 5, 9)).GetHashCode()));
        var map = new Dictionary<TimeOnly, string>();
        map[t] = "a";
        map[new TimeOnly(14, 5, 9)] = "b"; // same value -> same key
        Console.WriteLine("count=" + map.Count + " value=" + map[t]);

        Console.WriteLine("-- DateOnly.ToDateTime(TimeOnly) --");
        var d0 = new DateOnly(2026, 6, 27);
        Console.WriteLine(d0.ToDateTime(t).ToString("yyyy-MM-dd HH:mm:ss"));
        Console.WriteLine((int)d0.ToDateTime(t, DateTimeKind.Utc).Kind);
    }
}
