#nullable enable
using System;
using System.Globalization;

// DateTime.ToString(format) / TimeSpan.ToString(format) with the standard and
// custom format specifiers (InvariantCulture only). The real corelib formatting path
// reaches Number/DateTimeFormat/culture machinery we cannot transpile, so the format
// string is lowered to the runtime dn2cpp_datetime_format / dn2cpp_timespan_format
// helpers (a self-contained invariant-culture formatter).
// Every line is diffed exactly against real .NET (InvariantCulture). The CultureInfo
// overloads pass InvariantCulture explicitly so a default current culture cannot drift.
namespace DateTimeFormat;


static class Program
{
    static readonly CultureInfo ci = CultureInfo.InvariantCulture;internal static void __GateEntry()
    {
        var d = new DateTime(2026, 6, 23, 14, 5, 9, 123); // Tuesday, with 123 ms
        var d2 = new DateTime(2024, 1, 2, 3, 4, 5);       // single-digit fields, no ms
        var dUtc = new DateTime(2026, 6, 23, 14, 5, 9, DateTimeKind.Utc);
        var midnight = new DateTime(2026, 12, 31);        // 00:00:00, AM, Thursday

        Console.WriteLine("-- DateTime standard specifiers --");
        Console.WriteLine(d.ToString("d", ci));   // 06/23/2026
        Console.WriteLine(d.ToString("D", ci));   // Tuesday, 23 June 2026
        Console.WriteLine(d.ToString("f", ci));   // Tuesday, 23 June 2026 14:05
        Console.WriteLine(d.ToString("F", ci));   // Tuesday, 23 June 2026 14:05:09
        Console.WriteLine(d.ToString("g", ci));   // 06/23/2026 14:05
        Console.WriteLine(d.ToString("G", ci));   // 06/23/2026 14:05:09
        Console.WriteLine(d.ToString("M", ci));   // June 23
        Console.WriteLine(d.ToString("m", ci));   // June 23
        Console.WriteLine(d.ToString("o", ci));   // 2026-06-23T14:05:09.1230000
        Console.WriteLine(d.ToString("O", ci));   // 2026-06-23T14:05:09.1230000
        Console.WriteLine(dUtc.ToString("o", ci));// 2026-06-23T14:05:09.0000000Z
        Console.WriteLine(d.ToString("R", ci));   // Tue, 23 Jun 2026 14:05:09 GMT
        Console.WriteLine(d.ToString("s", ci));   // 2026-06-23T14:05:09
        Console.WriteLine(d.ToString("t", ci));   // 14:05
        Console.WriteLine(d.ToString("T", ci));   // 14:05:09
        Console.WriteLine(d.ToString("u", ci));   // 2026-06-23 14:05:09Z
        Console.WriteLine(d.ToString("Y", ci));   // 2026 June
        Console.WriteLine(d.ToString("y", ci));   // 2026 June

        Console.WriteLine("-- DateTime custom specifiers --");
        Console.WriteLine(d.ToString("yyyy-MM-dd", ci));            // 2026-06-23
        Console.WriteLine(d.ToString("yyyy/MM/dd HH:mm:ss", ci));   // 2026/06/23 14:05:09
        Console.WriteLine(d.ToString("MM/dd/yyyy", ci));           // 06/23/2026
        Console.WriteLine(d.ToString("dddd, dd MMMM yyyy", ci));    // Tuesday, 23 June 2026
        Console.WriteLine(d.ToString("ddd d MMM yy", ci));         // Tue 23 Jun 26
        Console.WriteLine(d.ToString("h:mm:ss tt", ci));          // 2:05:09 PM
        Console.WriteLine(midnight.ToString("h:mm:ss tt", ci));   // 12:00:00 AM
        Console.WriteLine(d2.ToString("h:mm:ss tt", ci));         // 3:04:05 AM
        Console.WriteLine(d.ToString("HH:mm:ss.fff", ci));         // 14:05:09.123
        Console.WriteLine(d.ToString("HH:mm:ss.fffffff", ci));     // 14:05:09.1230000
        Console.WriteLine(d.ToString("HH:mm:ss.FFF", ci));         // 14:05:09.123
        Console.WriteLine(d2.ToString("HH:mm:ss.FFF", ci));       // 03:04:05 (F trims to nothing)
        Console.WriteLine(d.ToString("M/d/yyyy", ci));            // 6/23/2026
        Console.WriteLine(d2.ToString("M/d/yyyy", ci));          // 1/2/2024
        Console.WriteLine(d.ToString("yyyyMMddHHmmss", ci));      // 20260623140509
        Console.WriteLine(d.ToString("yy", ci));                 // 26 (custom needs 2+ chars)
        Console.WriteLine(d.ToString("'Year:' yyyy", ci));        // Year: 2026
        Console.WriteLine(d.ToString("HH\\hmm\\mss\\s", ci));     // 14h05m09s (escaped literals)
        Console.WriteLine(d.ToString("%d", ci));                 // 23 (force single custom specifier)
        Console.WriteLine(d.ToString("ddd", ci));                // Tue
        Console.WriteLine(d.ToString("dddd", ci));               // Tuesday
        Console.WriteLine(d.ToString("MMM", ci));                // Jun
        Console.WriteLine(d.ToString("MMMM", ci));               // June

        Console.WriteLine("-- DateTime ToString(format) no provider (invariant build) --");
        Console.WriteLine(d.ToString("yyyy-MM-dd"));              // 2026-06-23
        Console.WriteLine(d.ToString((string?)null));            // 06/23/2026 14:05:09 (null == default)

        Console.WriteLine("-- DateTime in string.Format / interpolation --");
        Console.WriteLine(string.Format(ci, "{0:yyyy-MM-dd}", d));  // 2026-06-23
        Console.WriteLine(string.Format(ci, "[{0:HH:mm}]", d));     // [14:05]
        Console.WriteLine($"{d:yyyy/MM/dd}");                       // 2026/06/23
        Console.WriteLine($"{d:dddd}");                             // Tuesday

        Console.WriteLine("-- TimeSpan standard specifiers --");
        var ts = new TimeSpan(1, 2, 3, 4, 5);     // 1.02:03:04.0050000
        var ts2 = new TimeSpan(0, 2, 3, 4);       // 02:03:04
        var tsNeg = new TimeSpan(-1, -2, -3, -4); // negative
        Console.WriteLine(ts.ToString("c"));      // 1.02:03:04.0050000
        Console.WriteLine(ts.ToString("g", ci));  // 1:2:03:04.005
        Console.WriteLine(ts.ToString("G", ci));  // 1:02:03:04.0050000
        Console.WriteLine(ts2.ToString("c"));     // 02:03:04
        Console.WriteLine(ts2.ToString("g", ci)); // 2:03:04
        Console.WriteLine(ts2.ToString("G", ci)); // 0:02:03:04.0000000
        Console.WriteLine(tsNeg.ToString("c"));   // -1.02:03:04
        Console.WriteLine(tsNeg.ToString("g", ci));// -1:2:03:04
        Console.WriteLine(tsNeg.ToString("G", ci));// -1:02:03:04.0000000

        Console.WriteLine("-- TimeSpan custom specifiers --");
        Console.WriteLine(ts.ToString(@"d\.hh\:mm\:ss", ci));   // 1.02:03:04
        Console.WriteLine(ts.ToString(@"hh\:mm", ci));         // 02:03
        Console.WriteLine(ts2.ToString(@"h\:mm\:ss", ci));     // 2:03:04
        Console.WriteLine(ts.ToString(@"dd\.hh\:mm\:ss\.fff", ci)); // 01.02:03:04.005
        Console.WriteLine(ts.ToString(@"mm\:ss", ci));         // 03:04

        Console.WriteLine("-- TimeSpan in string.Format / interpolation --");
        Console.WriteLine(string.Format(ci, "{0:g}", ts));     // 1:2:03:04.005
        Console.WriteLine($"{ts2:c}");                          // 02:03:04
        Console.WriteLine($"{ts:hh\\:mm}");                     // 02:03
    }
}
