#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

// System.DateTime / System.TimeSpan modeled as intrinsic value types backed
// by the runtime Dn2CppDateTime / Dn2CppTimeSpan (the real corelib types reach
// Number formatting / InternalCall / culture, untranspilable). Exercises ctors,
// properties, arithmetic (incl. DateTime±TimeSpan and DateTime-DateTime), comparison,
// default ToString (DateTime via InvariantCulture "G" = MM/dd/yyyy HH:mm:ss; TimeSpan
// via the culture-independent "c" format), the static helpers, and boxed Equals/
// GetHashCode (object virtuals + value-type collection keys). All output diffed
// exactly against real .NET (InvariantCulture). From*(double) uses double literals so
// overload resolution binds the classic single-arg factories.
namespace DateTimeSubset;


static class Program
{
    static readonly CultureInfo ci = CultureInfo.InvariantCulture;internal static void __GateEntry()
    {
        Console.WriteLine("-- TimeSpan ctors / ToString --");
        Console.WriteLine(new TimeSpan(0, 1, 0, 0).ToString());      // 01:00:00 (d,h,m,s)
        Console.WriteLine(new TimeSpan(1, 2, 3, 4).ToString());      // 1.02:03:04
        Console.WriteLine(new TimeSpan(1, 2, 3, 4, 5).ToString());   // 1.02:03:04.0050000
        Console.WriteLine(new TimeSpan(2, 30, 0).ToString());        // 02:30:00 (h,m,s)
        Console.WriteLine(new TimeSpan(123).ToString());             // 00:00:00.0000123 (ticks)
        Console.WriteLine(TimeSpan.Zero.ToString());                 // 00:00:00
        Console.WriteLine(TimeSpan.MaxValue.ToString());
        Console.WriteLine(TimeSpan.MinValue.ToString());

        Console.WriteLine("-- TimeSpan factories --");
        Console.WriteLine(TimeSpan.FromDays(2.5).ToString());         // 2.12:00:00
        Console.WriteLine(TimeSpan.FromHours(1.5).ToString());        // 01:30:00
        Console.WriteLine(TimeSpan.FromMinutes(90.0).ToString());     // 01:30:00
        Console.WriteLine(TimeSpan.FromSeconds(1.5).ToString());      // 00:00:01.5000000
        Console.WriteLine(TimeSpan.FromMilliseconds(250.0).ToString());// 00:00:00.2500000
        Console.WriteLine(TimeSpan.FromTicks(5000).ToString());       // 00:00:00.0005000

        Console.WriteLine("-- TimeSpan props --");
        var t = new TimeSpan(1, 2, 3, 4, 5);
        Console.WriteLine($"{t.Days} {t.Hours} {t.Minutes} {t.Seconds} {t.Milliseconds} {t.Ticks}");
        Console.WriteLine(t.TotalDays.ToString(ci));
        Console.WriteLine(t.TotalHours.ToString(ci));
        Console.WriteLine(t.TotalMinutes.ToString(ci));
        Console.WriteLine(t.TotalSeconds.ToString(ci));
        Console.WriteLine(t.TotalMilliseconds.ToString(ci));
        var tn = new TimeSpan(-1, -2, -3, -4, -5);
        Console.WriteLine($"{tn.Days} {tn.Hours} {tn.Minutes} {tn.Seconds} {tn.Milliseconds}");

        Console.WriteLine("-- TimeSpan arithmetic / compare --");
        var a = TimeSpan.FromHours(2.0);
        var b = TimeSpan.FromMinutes(30.0);
        Console.WriteLine((a + b).ToString());                       // 02:30:00
        Console.WriteLine((a - b).ToString());                       // 01:30:00
        Console.WriteLine((-a).ToString());                          // -02:00:00
        Console.WriteLine(a.Add(b).ToString());                      // 02:30:00
        Console.WriteLine(a.Subtract(b).ToString());                 // 01:30:00
        Console.WriteLine(a.Negate().ToString());                    // -02:00:00
        Console.WriteLine((-a).Duration().ToString());               // 02:00:00
        Console.WriteLine(a > b);                                    // True
        Console.WriteLine(a == TimeSpan.FromMinutes(120.0));         // True
        Console.WriteLine(a.CompareTo(b));                           // 1
        Console.WriteLine(TimeSpan.Compare(b, a));                   // -1
        Console.WriteLine(a.Equals(TimeSpan.FromMinutes(120.0)));    // True

        Console.WriteLine("-- DateTime ctors / ToString --");
        Console.WriteLine(new DateTime(2026, 6, 23, 14, 5, 9).ToString(ci));     // 06/23/2026 14:05:09
        Console.WriteLine(new DateTime(2026, 6, 23).ToString(ci));               // 06/23/2026 00:00:00
        Console.WriteLine(new DateTime(2026, 6, 23, 14, 5, 9, 123).ToString(ci));
        Console.WriteLine(DateTime.MinValue.ToString(ci));                       // 01/01/0001 00:00:00
        Console.WriteLine(DateTime.MaxValue.ToString(ci));                       // 12/31/9999 23:59:59
        Console.WriteLine(new DateTime(2000, 2, 29).ToString(ci));               // leap day

        Console.WriteLine("-- DateTime props --");
        var d = new DateTime(2026, 6, 23, 14, 5, 9, 123);
        Console.WriteLine($"{d.Year} {d.Month} {d.Day} {d.Hour} {d.Minute} {d.Second} {d.Millisecond}");
        Console.WriteLine($"{(int)d.DayOfWeek} {d.DayOfYear} {d.Ticks} {(int)d.Kind}");
        Console.WriteLine(d.Date.ToString(ci));                                  // 06/23/2026 00:00:00
        Console.WriteLine(d.TimeOfDay.ToString());                              // 14:05:09.1230000

        Console.WriteLine("-- DateTime arithmetic --");
        Console.WriteLine(new DateTime(2026, 1, 31).AddMonths(1).ToString(ci));  // 02/28/2026
        Console.WriteLine(new DateTime(2024, 2, 29).AddYears(1).ToString(ci));   // 02/28/2025
        Console.WriteLine(new DateTime(2026, 6, 23).AddDays(10).ToString(ci));
        Console.WriteLine(new DateTime(2026, 6, 23, 12, 0, 0).AddHours(13).ToString(ci));
        Console.WriteLine(new DateTime(2026, 6, 23).AddMinutes(90.0).ToString(ci));
        Console.WriteLine(new DateTime(2026, 6, 23).AddTicks(5).Ticks);
        Console.WriteLine((new DateTime(2026, 6, 23) + TimeSpan.FromDays(2.0)).ToString(ci));
        Console.WriteLine((new DateTime(2026, 6, 23) - TimeSpan.FromDays(2.0)).ToString(ci));
        Console.WriteLine((new DateTime(2026, 6, 25) - new DateTime(2026, 6, 23)).ToString());        // 2.00:00:00
        Console.WriteLine(new DateTime(2026, 6, 23).Subtract(new DateTime(2026, 6, 20)).ToString());  // 3.00:00:00

        Console.WriteLine("-- DateTime compare / statics --");
        Console.WriteLine(new DateTime(2026, 6, 23) < new DateTime(2026, 6, 24));   // True
        Console.WriteLine(new DateTime(2026, 6, 23) == new DateTime(2026, 6, 23));  // True
        Console.WriteLine(new DateTime(2026, 6, 23).CompareTo(new DateTime(2026, 6, 24))); // -1
        Console.WriteLine(DateTime.Compare(new DateTime(2026, 6, 24), new DateTime(2026, 6, 23))); // 1
        Console.WriteLine(DateTime.IsLeapYear(2024));      // True
        Console.WriteLine(DateTime.IsLeapYear(2026));      // False
        Console.WriteLine(DateTime.DaysInMonth(2026, 2));  // 28
        Console.WriteLine(DateTime.DaysInMonth(2024, 2));  // 29

        Console.WriteLine("-- boxed Equals / GetHashCode --");
        object o1 = new DateTime(2026, 6, 23);
        object o2 = new DateTime(2026, 6, 23);
        Console.WriteLine(o1.Equals(o2));                          // True
        Console.WriteLine(o1.GetHashCode() == o2.GetHashCode());   // True
        var set = new HashSet<DateTime> { new DateTime(2026, 6, 23), new DateTime(2026, 6, 23), new DateTime(2026, 6, 24) };
        Console.WriteLine(set.Count);                              // 2
        object ts1 = TimeSpan.FromHours(2.0);
        object ts2 = TimeSpan.FromMinutes(120.0);
        Console.WriteLine(ts1.Equals(ts2));                        // True
        Console.WriteLine(ts1.GetHashCode() == ts2.GetHashCode()); // True
        var tset = new HashSet<TimeSpan> { TimeSpan.FromHours(1.0), TimeSpan.FromMinutes(60.0), TimeSpan.FromHours(2.0) };
        Console.WriteLine(tset.Count);                             // 2
    }
}
