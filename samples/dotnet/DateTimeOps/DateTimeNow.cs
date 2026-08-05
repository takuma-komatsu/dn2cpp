#nullable enable
using System;
using System.Globalization;

// DateTime.Now / UtcNow / Today (wall clock) + Convert.ChangeType with a DateTime
// target (closes ). The real corelib clock path (native interop / time zones) is
// untranspilable, so Now / UtcNow / Today lower to the runtime helpers dn2cpp_datetime_now /
// _utc_now / _today, mirroring the intrinsic-value-type pattern. Wall-clock output is
// non-deterministic, so the gate asserts only invariants (Kind, a sane year range, Today at
// midnight, the local/UTC offset bound) — never an exact instant. Convert.ChangeType to a
// DateTime target IS deterministic (string -> invariant parse) and is diffed exactly vs real
//.NET. Carve-out: only a string source converts to DateTime (a numeric source throws, as in
// real.NET); the bare-time parse defaults to 0001-01-01 (no clock), per.
namespace DateTimeNow;


static class Program
{
    static readonly CultureInfo ci = CultureInfo.InvariantCulture;

    static void Show(object v) => Console.WriteLine(v + " :: " + v.GetType().Name);internal static void __GateEntry()
    {
        Console.WriteLine("-- wall clock invariants --");
        DateTime now = DateTime.Now;
        DateTime utc = DateTime.UtcNow;
        DateTime today = DateTime.Today;

        Console.WriteLine("Now.Kind=Local: " + (now.Kind == DateTimeKind.Local));
        Console.WriteLine("UtcNow.Kind=Utc: " + (utc.Kind == DateTimeKind.Utc));
        Console.WriteLine("Today.Kind=Local: " + (today.Kind == DateTimeKind.Local));

        Console.WriteLine("Now year sane: " + (now.Year >= 2020 && now.Year <= 9999));
        Console.WriteLine("UtcNow year sane: " + (utc.Year >= 2020 && utc.Year <= 9999));

        Console.WriteLine("Today is midnight: " + (today.Hour == 0 && today.Minute == 0 && today.Second == 0));
        Console.WriteLine("Today.TimeOfDay zero: " + (today.TimeOfDay == TimeSpan.Zero));
        Console.WriteLine("Now.Date is midnight: " + (now.Date.TimeOfDay == TimeSpan.Zero));

        // Local vs UTC differ only by the time-zone offset (DST included): a bounded span
        // of at most 14 hours in either direction. (now / utc are read microseconds apart;
        // the epsilon stays well inside the bound.)
        double offsetHours = (now - utc).TotalHours;
        Console.WriteLine("offset within 14h: " + (offsetHours >= -14.0 && offsetHours <= 14.0));

        Console.WriteLine("-- Convert.ChangeType -> DateTime (R2-D2) --");
        Show(Convert.ChangeType("2026-06-23", typeof(DateTime), ci));
        Show(Convert.ChangeType("2026-06-23 14:05:09", typeof(DateTime), ci));
        Show(Convert.ChangeType("2026-06-23T14:05:09", typeof(DateTime), ci));
        Show(Convert.ChangeType("06/23/2026", typeof(DateTime), ci));
        Show(Convert.ChangeType("06/23/2026 14:05", typeof(DateTime), ci));
        // TypeCode form: target named by TypeCode.DateTime instead of a runtime Type.
        Show(Convert.ChangeType("2026-06-23 14:05:09", TypeCode.DateTime));
        // Identity: a DateTime source passes through unchanged.
        object existing = new DateTime(2026, 6, 23, 1, 2, 3);
        Show(Convert.ChangeType(existing, typeof(DateTime), ci));
    }
}
