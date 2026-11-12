#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

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

    private static DateOnly producerDate;
    private static int producerYear, producerMonth, producerDay, trace;
    private delegate void DeconstructDate(out int year, out int month, out int day);

    private static ref DateOnly ReceiverProducer()
    {
        trace = trace * 10 + 1;
        return ref producerDate;
    }

    private static ref int YearProducer() { trace = trace * 10 + 2; return ref producerYear; }
    private static ref int MonthProducer() { trace = trace * 10 + 3; return ref producerMonth; }

    private static ref int DayProducer(bool throws)
    {
        trace = trace * 10 + 4;
        if (throws) throw new FormatException("day output producer");
        return ref producerDay;
    }

    private static string Outputs(int year, int month, int day) => year + "," + month + "," + day;

    // January/February correction reloads already-written outputs, so aliases
    // must remain observable rather than becoming independent property reads.
    internal static void RunDeconstruct()
    {
        Console.WriteLine("== DateOnly deconstruct ==");
        MethodInfo metadata = typeof(DateOnly).GetMethod(nameof(DateOnly.Deconstruct))!;
        int[] dates = { 1, 1, 1, 2024, 2, 29, 2001, 12, 31, 1, 2, 28, 1, 3, 1, 1900, 2, 28, 2000, 2, 29, 2000, 3, 1, 9999, 12, 31 };
        for (int k = 0; k < dates.Length; k += 3)
        {
            var date = new DateOnly(dates[k], dates[k + 1], dates[k + 2]);
            date.Deconstruct(out int year, out int month, out int day);
            string line = date.ToString("yyyy-MM-dd") + " separate=" + Outputs(year, month, day);
            int a, b;
            date.Deconstruct(out a, out a, out a);
            line += " all=" + a;
            date.Deconstruct(out a, out a, out b);
            line += " ym=" + a + "," + b;
            date.Deconstruct(out a, out b, out a);
            line += " yd=" + a + "," + b;
            date.Deconstruct(out b, out a, out a);
            line += " md=" + b + "," + a;
            for (int slot = 0; slot < 3; slot++)
            {
                var target = date;
                int y = -1, m = -2, d = -3;
                ref int storage = ref Unsafe.As<DateOnly, int>(ref target);
                if (slot == 0) target.Deconstruct(out storage, out m, out d);
                if (slot == 1) target.Deconstruct(out y, out storage, out d);
                if (slot == 2) target.Deconstruct(out y, out m, out storage);
                line += " receiver" + slot + "=" + storage + ":" + Outputs(y, m, d);
            }
            object[] values = { -1, -2, -3 };
            object? result = metadata.Invoke(date, values);
            DeconstructDate deconstruct = date.Deconstruct;
            deconstruct(out int delegateYear, out int delegateMonth, out int delegateDay);
            Console.WriteLine(line + " reflect=" + (result is null) + ":" + Outputs((int)values[0], (int)values[1], (int)values[2])
                + " delegate=" + Outputs(delegateYear, delegateMonth, delegateDay));
        }
        for (int mode = 0; mode < 2; mode++)
        {
            producerDate = new DateOnly(2024, 2, 29);
            producerYear = -1; producerMonth = -2; producerDay = -3; trace = 0;
            string error = "none";
            try { ReceiverProducer().Deconstruct(out YearProducer(), out MonthProducer(), out DayProducer(mode == 1)); }
            catch (Exception caught) { error = caught.GetType().Name + ":" + caught.Message; }
            Console.WriteLine("producer" + mode + "=" + error + " trace=" + trace + " " + Outputs(producerYear, producerMonth, producerDay));
        }
        string parameters = "";
        foreach (var parameter in metadata.GetParameters())
            parameters += parameter.ParameterType.Name + " ";
        string nullTarget = "none";
        try { metadata.Invoke(null, new object[] { -1, -2, -3 }); }
        catch (TargetException) { nullTarget = "TargetException"; }
        Console.WriteLine("reflect signature=" + metadata.ReturnType.Name + "(" + parameters.TrimEnd() + ") null target=" + nullTarget);
        Console.WriteLine("DateOnly deconstruct end");
    }
}
