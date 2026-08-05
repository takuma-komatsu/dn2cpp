#nullable enable
using System;

// DateTime.ToLocalTime / ToUniversalTime against the host's local time zone
// (DST included). The real corelib path (TimeZoneInfo / ICU / TZif) is untranspilable,
// so these lower to runtime helpers dn2cpp_datetime_to_local / _to_universal, which reuse
// the same libc clock primitives as the wall-clock helpers (localtime_r / mktime).
// The applied offset is host-dependent and thus non-deterministic, so this gate asserts
// only invariants — Kind transitions, the no-op cases, round trips, a whole-minute bounded
// offset, and Now/UtcNow consistency — never an exact converted instant. Carve-out: the
// host's local zone only (no named / historical zones), no TimeZoneInfo.
namespace DateTimeTz;


static class Program
{internal static void __GateEntry()
    {
        // Fixed wall-clock instants at noon, well clear of any host's ~02:00 DST boundary,
        // so the round trips are unambiguous on every time zone.
        DateTime local = new DateTime(2021, 7, 15, 12, 30, 45, DateTimeKind.Local);
        DateTime utc = new DateTime(2021, 7, 15, 12, 30, 45, DateTimeKind.Utc);
        DateTime unspec = new DateTime(2021, 1, 9, 12, 5, 0); // Kind=Unspecified

        Console.WriteLine("-- Kind transitions --");
        Console.WriteLine("Local.ToUniversalTime().Kind=Utc: " + (local.ToUniversalTime().Kind == DateTimeKind.Utc));
        Console.WriteLine("Utc.ToLocalTime().Kind=Local: " + (utc.ToLocalTime().Kind == DateTimeKind.Local));
        Console.WriteLine("Unspecified.ToLocalTime().Kind=Local: " + (unspec.ToLocalTime().Kind == DateTimeKind.Local));
        Console.WriteLine("Unspecified.ToUniversalTime().Kind=Utc: " + (unspec.ToUniversalTime().Kind == DateTimeKind.Utc));

        Console.WriteLine("-- no-op when Kind already matches --");
        Console.WriteLine("Local.ToLocalTime() unchanged: " + (local.ToLocalTime() == local && local.ToLocalTime().Kind == DateTimeKind.Local));
        Console.WriteLine("Utc.ToUniversalTime() unchanged: " + (utc.ToUniversalTime() == utc && utc.ToUniversalTime().Kind == DateTimeKind.Utc));

        Console.WriteLine("-- round trips --");
        Console.WriteLine("Local -> Utc -> Local: " + (local.ToUniversalTime().ToLocalTime() == local));
        Console.WriteLine("Utc -> Local -> Utc: " + (utc.ToLocalTime().ToUniversalTime() == utc));

        Console.WriteLine("-- offset is whole minutes, within 14h --");
        TimeSpan off = local - local.ToUniversalTime(); // = the host UTC offset for that instant
        Console.WriteLine("offset whole minutes: " + (off.Ticks % 600000000L == 0));
        Console.WriteLine("offset within 14h: " + (off.TotalHours >= -14.0 && off.TotalHours <= 14.0));

        Console.WriteLine("-- Now / UtcNow consistency --");
        Console.WriteLine("Now.ToUniversalTime() ~ UtcNow: " + (Math.Abs((DateTime.Now.ToUniversalTime() - DateTime.UtcNow).TotalSeconds) < 5.0));
        Console.WriteLine("UtcNow.ToLocalTime() ~ Now: " + (Math.Abs((DateTime.UtcNow.ToLocalTime() - DateTime.Now).TotalSeconds) < 5.0));
    }
}
