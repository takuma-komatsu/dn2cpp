#nullable enable
using System;

// System.TimeZoneInfo — transpiled as real BCL IL (unlike the DateTimeTz section, which
// uses the libc runtime helpers). TimeZoneInfo.GetUtcOffset(DateTime) reaches, via the
// internal GetIsDaylightSavings, DateTime.IsAmbiguousDaylightSavingTime() — the same path
// Newtonsoft.Json's DateTimeUtils.TryParseDateTimeOffsetIso walks when it resolves an ISO
// 8601 instant against a time zone. That internal predicate reads DateTime's
// KindLocalAmbiguousDst flag, which dn2cpp's {Unspecified, Utc, Local} kind model never
// carries, so it is intrinsically false (see MethodCompiler.EmitIntrinsic.ValueTypes.cs).
//
// Only the Utc zone is exercised at RUN time: it is a pure in-memory zone (fixed zero
// offset, no DST), so its facts are host-independent. That is a statement about what the
// section observes, NOT about what it drags in: GetUtcOffset holds a static edge to the
// LOCAL zone (CachedData.get_Local -> GetLocalTimeZone), so the host-zone lookup enters the
// tree whatever the program touches at run time. On POSIX that lands on Interop.Sys.ReadLink
// over /etc/localtime — the TZif path the DateTimeTz section names as untranspilable; on
// Windows it lands on the registry, so this section's premise there is that advapi32 is an
// admitted P/Invoke module (Compilation.IsRuntimeProvidedPInvokeModule). The display names
// below are const-folded at the call site (CoreIntrinsics.ConstFoldedStringCall), so they
// stay host-independent on a non-English host instead of being read back out of the
// registry. The gate's purpose is to reach and exercise IsAmbiguousDaylightSavingTime in
// the corpus; the Utc zone's transpiled GetUtcOffset body carries the call to it regardless
// of runtime zone.
namespace DateTimeTzInfo;

static class Program
{
    internal static void __GateEntry()
    {
        var noon = new DateTime(2021, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        var winter = new DateTime(2021, 1, 9, 3, 30, 0, DateTimeKind.Unspecified);

        Console.WriteLine("-- TimeZoneInfo.Utc (fixed, host-independent) --");
        Console.WriteLine("Utc.Id: " + TimeZoneInfo.Utc.Id);
        Console.WriteLine("Utc.StandardName: " + TimeZoneInfo.Utc.StandardName);
        Console.WriteLine("Utc.DisplayName: " + TimeZoneInfo.Utc.DisplayName);
        Console.WriteLine("Utc.BaseUtcOffset zero: " + (TimeZoneInfo.Utc.BaseUtcOffset == TimeSpan.Zero));
        Console.WriteLine("Utc.GetUtcOffset(noon) zero: " + (TimeZoneInfo.Utc.GetUtcOffset(noon) == TimeSpan.Zero));
        Console.WriteLine("Utc.GetUtcOffset(winter) zero: " + (TimeZoneInfo.Utc.GetUtcOffset(winter) == TimeSpan.Zero));
        Console.WriteLine("Utc.IsDaylightSavingTime(noon): " + TimeZoneInfo.Utc.IsDaylightSavingTime(noon));
        Console.WriteLine("Utc.IsDaylightSavingTime(winter): " + TimeZoneInfo.Utc.IsDaylightSavingTime(winter));
        Console.WriteLine("Utc.SupportsDaylightSavingTime: " + TimeZoneInfo.Utc.SupportsDaylightSavingTime);
    }
}
