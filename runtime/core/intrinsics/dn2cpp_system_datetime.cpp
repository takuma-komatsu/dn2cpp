// dn2cpp_system_datetime.cpp — System.DateTime / TimeSpan / DateTimeOffset /
// DateOnly / TimeOnly intrinsics: tick-based (1 tick = 100ns) construction,
// arithmetic, wall-clock reads, host-local time-zone conversion (DST included),
// and InvariantCulture formatting/parsing, emitted in place of the BCL IL.
#include "dn2cpp_core.h"
#include "platform/dn2cpp_pal.h" // localtime_r / mktime via the PAL seam

#include <cstdint>
#include <cstdio>
#include <cstring>
#include <cstdlib>
#include <ctime>   // std::tm / std::time_t (broken-down time types)
#include <chrono>
#include <type_traits> // make_unsigned, for the width-templated flags formatter

// ===== System.TimeSpan / System.DateTime =====
// Tick scale (1 tick = 100ns). DateTime ticks are 0..MAX (year 0001..9999).
static const int64_t DN2CPP_TPMS = 10000LL;          // ticks per millisecond
static const int64_t DN2CPP_TPS  = 10000000LL;       // ticks per second
static const int64_t DN2CPP_TPM  = 600000000LL;      // ticks per minute
static const int64_t DN2CPP_TPH  = 36000000000LL;    // ticks per hour
static const int64_t DN2CPP_TPD  = 864000000000LL;   // ticks per day

// Cumulative days at the start of each month (index 1..12; [0]=0, [12]=365/366).
static const int s_daysToMonth365[13] = { 0,31,59,90,120,151,181,212,243,273,304,334,365 };
static const int s_daysToMonth366[13] = { 0,31,60,91,121,152,182,213,244,274,305,335,366 };

static bool dn2cpp_dt_isleap(int y) { return (y % 4 == 0 && y % 100 != 0) || (y % 400 == 0); }

// Days in a month, with .NET's ArgumentOutOfRangeException for an out-of-range
// month. The bound also guards the read: the cumulative tables are 13 entries,
// so `days[mo]` at mo=13 would run one past the end and answer a
// plausible-looking number.
static int dn2cpp_dt_days_in_month_checked(int y, int mo)
{
    if (mo < 1 || mo > 12)
        dn2cpp_throw_argument_out_of_range();
    const int* days = dn2cpp_dt_isleap(y) ? s_daysToMonth366 : s_daysToMonth365;
    return days[mo] - days[mo - 1];
}

// Proleptic-Gregorian year/month/day -> ticks at midnight (matches .NET DateToTicks).
// The YEAR is deliberately not checked here — an out-of-range year lands outside
// [0, MaxTicks] and the packing choke point raises the same
// ArgumentOutOfRangeException from there.
static int64_t dn2cpp_dt_date_to_ticks(int y, int mo, int d)
{
    if (d < 1 || d > dn2cpp_dt_days_in_month_checked(y, mo))
        dn2cpp_throw_argument_out_of_range();
    const int* days = dn2cpp_dt_isleap(y) ? s_daysToMonth366 : s_daysToMonth365;
    int yy = y - 1;
    int64_t n = (int64_t)yy * 365 + yy / 4 - yy / 100 + yy / 400 + days[mo - 1] + (d - 1);
    return n * DN2CPP_TPD;
}
// Unchecked, because most callers are NOT a clock reading: TimeSpan's
// (d, h, m, s, ms) ctors take negative components by design, and localtime_r's
// broken-down output is trusted. The clock-of-day callers use the _checked form.
static int64_t dn2cpp_dt_time_to_ticks(int h, int mi, int s, int ms)
{
    return ((int64_t)h * 3600 + (int64_t)mi * 60 + s) * DN2CPP_TPS + (int64_t)ms * DN2CPP_TPMS;
}
// A time OF DAY: the components of `new DateTime(y, mo, d, h, mi, s[, ms])` and
// `new TimeOnly(...)`, which real .NET bounds to a real wall-clock reading.
static int64_t dn2cpp_dt_time_of_day_to_ticks(int h, int mi, int s, int ms)
{
    if (h < 0 || h > 23 || mi < 0 || mi > 59 || s < 0 || s > 59 || ms < 0 || ms > 999)
        dn2cpp_throw_argument_out_of_range();
    return dn2cpp_dt_time_to_ticks(h, mi, s, ms);
}

// ticks -> year/month/day/day-of-year (matches .NET GetDatePart). Any of the out
// pointers may be null. DateTime ticks are non-negative, so `n` fits an int.
static void dn2cpp_dt_datepart(int64_t ticks, int* py, int* pmo, int* pd, int* pdoy)
{
    int n = (int)(ticks / DN2CPP_TPD);
    int y400 = n / 146097; n -= y400 * 146097;
    int y100 = n / 36524; if (y100 == 4) y100 = 3; n -= y100 * 36524;
    int y4 = n / 1461; n -= y4 * 1461;
    int y1 = n / 365; if (y1 == 4) y1 = 3; n -= y1 * 365;
    if (py) *py = y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;
    if (pdoy) *pdoy = n + 1;
    bool leap = (y1 == 3) && (y4 != 24 || y100 == 3);
    const int* days = leap ? s_daysToMonth366 : s_daysToMonth365;
    int m = (n >> 5) + 1;
    while (n >= days[m]) m++;
    if (pmo) *pmo = m;
    if (pd) *pd = n - days[m - 1] + 1;
}

// ---- TimeSpan ----
Dn2CppTimeSpan dn2cpp_timespan_from_ticks(int64_t ticks) { return Dn2CppTimeSpan{ ticks }; }
Dn2CppTimeSpan dn2cpp_timespan_from_hms(int32_t h, int32_t m, int32_t s)
{ return Dn2CppTimeSpan{ dn2cpp_dt_time_to_ticks(h, m, s, 0) }; }
Dn2CppTimeSpan dn2cpp_timespan_from_dhms(int32_t d, int32_t h, int32_t m, int32_t s)
{ return Dn2CppTimeSpan{ (int64_t)d * DN2CPP_TPD + dn2cpp_dt_time_to_ticks(h, m, s, 0) }; }
Dn2CppTimeSpan dn2cpp_timespan_from_dhmsms(int32_t d, int32_t h, int32_t m, int32_t s, int32_t ms)
{ return Dn2CppTimeSpan{ (int64_t)d * DN2CPP_TPD + dn2cpp_dt_time_to_ticks(h, m, s, ms) }; }
Dn2CppTimeSpan dn2cpp_timespan_from_unit(double value, int64_t ticksPerUnit)
{ return Dn2CppTimeSpan{ (int64_t)(value * (double)ticksPerUnit) }; }
Dn2CppTimeSpan dn2cpp_timespan_add(Dn2CppTimeSpan a, Dn2CppTimeSpan b) { return Dn2CppTimeSpan{ a.ticks + b.ticks }; }
Dn2CppTimeSpan dn2cpp_timespan_sub(Dn2CppTimeSpan a, Dn2CppTimeSpan b) { return Dn2CppTimeSpan{ a.ticks - b.ticks }; }
Dn2CppTimeSpan dn2cpp_timespan_neg(Dn2CppTimeSpan a) { return Dn2CppTimeSpan{ -a.ticks }; }
Dn2CppTimeSpan dn2cpp_timespan_duration(Dn2CppTimeSpan a) { return Dn2CppTimeSpan{ a.ticks < 0 ? -a.ticks : a.ticks }; }
int32_t dn2cpp_timespan_cmp(Dn2CppTimeSpan a, Dn2CppTimeSpan b)
{ return a.ticks < b.ticks ? -1 : (a.ticks > b.ticks ? 1 : 0); }
int32_t dn2cpp_timespan_days(Dn2CppTimeSpan a) { return (int32_t)(a.ticks / DN2CPP_TPD); }
int32_t dn2cpp_timespan_hours(Dn2CppTimeSpan a) { return (int32_t)((a.ticks / DN2CPP_TPH) % 24); }
int32_t dn2cpp_timespan_minutes(Dn2CppTimeSpan a) { return (int32_t)((a.ticks / DN2CPP_TPM) % 60); }
int32_t dn2cpp_timespan_seconds(Dn2CppTimeSpan a) { return (int32_t)((a.ticks / DN2CPP_TPS) % 60); }
int32_t dn2cpp_timespan_milliseconds(Dn2CppTimeSpan a) { return (int32_t)((a.ticks / DN2CPP_TPMS) % 1000); }
double dn2cpp_timespan_total(Dn2CppTimeSpan a, int64_t ticksPerUnit) { return (double)a.ticks / (double)ticksPerUnit; }
int32_t dn2cpp_timespan_hash(Dn2CppTimeSpan a) { return (int32_t)a.ticks ^ (int32_t)(a.ticks >> 32); }

Dn2CppString* dn2cpp_timespan_to_string(Dn2CppTimeSpan a)
{
    int64_t ticks = a.ticks;
    char buf[40]; int o = 0;
    if (ticks < 0) buf[o++] = '-';
    // Split on the signed value, then take the magnitude of each component (avoids
    // overflowing on TimeSpan.MinValue, where abs(ticks) would not fit int64).
    int64_t days = ticks / DN2CPP_TPD;
    int64_t rem = ticks % DN2CPP_TPD;
    int hours = (int)(rem / DN2CPP_TPH); rem %= DN2CPP_TPH;
    int mins = (int)(rem / DN2CPP_TPM); rem %= DN2CPP_TPM;
    int secs = (int)(rem / DN2CPP_TPS);
    int64_t frac = rem % DN2CPP_TPS;
    if (days < 0) days = -days;
    if (hours < 0) hours = -hours;
    if (mins < 0) mins = -mins;
    if (secs < 0) secs = -secs;
    if (frac < 0) frac = -frac;
    char tmp[24];
    if (days != 0) { int k = std::snprintf(tmp, sizeof(tmp), "%lld.", (long long)days); for (int i = 0; i < k; i++) buf[o++] = tmp[i]; }
    int k = std::snprintf(tmp, sizeof(tmp), "%02d:%02d:%02d", hours, mins, secs);
    for (int i = 0; i < k; i++) buf[o++] = tmp[i];
    if (frac != 0) { int j = std::snprintf(tmp, sizeof(tmp), ".%07lld", (long long)frac); for (int i = 0; i < j; i++) buf[o++] = tmp[i]; }
    return dn2cpp_string_from_utf8(buf, o);
}

// ---- DateTime ----
Dn2CppDateTime dn2cpp_datetime_from_ticks(int64_t ticks, int32_t kind) { return dn2cpp_datetime_pack(ticks, kind); }
// Windows FILETIME (100ns intervals since 1601-01-01 UTC) <-> DateTime/DateTimeOffset ticks.
// 504911232000000000 = ticks from 0001-01-01 to 1601-01-01 (DateTime.FileTimeOffset in the
// real BCL); MaxTicks (DateTime.MaxValue.Ticks, 3155378975999999999) bounds FromFileTimeUtc's
// valid input range. Pure arithmetic — no OS call, no leap-second table (dn2cpp does not model
// SystemSupportsLeapSeconds, matching every other DateTime intrinsic here).
static const int64_t DN2CPP_FILETIME_OFFSET = 504911232000000000LL;
static const int64_t DN2CPP_MAX_TICKS = 3155378975999999999LL;
// DateTime.FromFileTimeUtc: real .NET throws ArgumentOutOfRangeException when
// (ulong)fileTime > MaxTicks - FileTimeOffset — this also rejects any negative fileTime (its
// ulong reinterpretation is huge). Kind=Utc (1).
Dn2CppDateTime dn2cpp_datetime_from_file_time_utc(int64_t fileTime)
{
    if ((uint64_t)fileTime > (uint64_t)(DN2CPP_MAX_TICKS - DN2CPP_FILETIME_OFFSET))
        dn2cpp_throw_argument_out_of_range();
    return dn2cpp_datetime_pack(fileTime + DN2CPP_FILETIME_OFFSET, 1);
}
// DateTime.ToFileTimeUtc: only Kind=Local (2) is converted to universal first (Unspecified's
// ticks are used as-is, treated as already-UTC — a deliberate real-BCL asymmetry vs.
// ToUniversalTime(), which treats Unspecified as Local; see DateTime.ToFileTime below, which
// composes the two correctly). Throws ArgumentOutOfRangeException for a result before the
// FILETIME epoch (ticks < FileTimeOffset).
int64_t dn2cpp_datetime_to_file_time_utc(Dn2CppDateTime a)
{
    int64_t ticks = (a.kind() == 2) ? dn2cpp_datetime_to_universal(a).ticks() : a.ticks();
    ticks -= DN2CPP_FILETIME_OFFSET;
    if (ticks < 0)
        dn2cpp_throw_argument_out_of_range();
    return ticks;
}
Dn2CppDateTime dn2cpp_datetime_ymd(int32_t y, int32_t mo, int32_t d, int32_t kind)
{ return dn2cpp_datetime_pack(dn2cpp_dt_date_to_ticks(y, mo, d), kind); }
Dn2CppDateTime dn2cpp_datetime_ymdhms(int32_t y, int32_t mo, int32_t d, int32_t h, int32_t mi, int32_t s, int32_t kind)
{ return dn2cpp_datetime_pack(dn2cpp_dt_date_to_ticks(y, mo, d) + dn2cpp_dt_time_of_day_to_ticks(h, mi, s, 0), kind); }
Dn2CppDateTime dn2cpp_datetime_ymdhmsms(int32_t y, int32_t mo, int32_t d, int32_t h, int32_t mi, int32_t s, int32_t ms, int32_t kind)
{ return dn2cpp_datetime_pack(dn2cpp_dt_date_to_ticks(y, mo, d) + dn2cpp_dt_time_of_day_to_ticks(h, mi, s, ms), kind); }
int32_t dn2cpp_datetime_year(Dn2CppDateTime a) { int y; dn2cpp_dt_datepart(a.ticks(), &y, nullptr, nullptr, nullptr); return y; }
int32_t dn2cpp_datetime_month(Dn2CppDateTime a) { int m; dn2cpp_dt_datepart(a.ticks(), nullptr, &m, nullptr, nullptr); return m; }
int32_t dn2cpp_datetime_day(Dn2CppDateTime a) { int d; dn2cpp_dt_datepart(a.ticks(), nullptr, nullptr, &d, nullptr); return d; }
int32_t dn2cpp_datetime_hour(Dn2CppDateTime a) { return (int32_t)((a.ticks() / DN2CPP_TPH) % 24); }
int32_t dn2cpp_datetime_minute(Dn2CppDateTime a) { return (int32_t)((a.ticks() / DN2CPP_TPM) % 60); }
int32_t dn2cpp_datetime_second(Dn2CppDateTime a) { return (int32_t)((a.ticks() / DN2CPP_TPS) % 60); }
int32_t dn2cpp_datetime_millisecond(Dn2CppDateTime a) { return (int32_t)((a.ticks() / DN2CPP_TPMS) % 1000); }
int32_t dn2cpp_datetime_dayofweek(Dn2CppDateTime a) { return (int32_t)((a.ticks() / DN2CPP_TPD + 1) % 7); }
int32_t dn2cpp_datetime_dayofyear(Dn2CppDateTime a) { int doy; dn2cpp_dt_datepart(a.ticks(), nullptr, nullptr, nullptr, &doy); return doy; }
void dn2cpp_datetime_get_date(Dn2CppDateTime a, int32_t* year, int32_t* month, int32_t* day)
{
    int y, m, d;
    dn2cpp_dt_datepart(a.ticks(), &y, &m, &d, nullptr);
    if (year) *year = y;
    if (month) *month = m;
    if (day) *day = d;
}
void dn2cpp_datetime_get_time(Dn2CppDateTime a, int32_t* hour, int32_t* minute, int32_t* second)
{
    int64_t ticks = a.ticks();
    if (hour) *hour = (int32_t)((ticks / DN2CPP_TPH) % 24);
    if (minute) *minute = (int32_t)((ticks / DN2CPP_TPM) % 60);
    if (second) *second = (int32_t)((ticks / DN2CPP_TPS) % 60);
}
void dn2cpp_datetime_get_time_ms(Dn2CppDateTime a, int32_t* hour, int32_t* minute, int32_t* second, int32_t* millisecond)
{
    int64_t ticks = a.ticks();
    if (hour) *hour = (int32_t)((ticks / DN2CPP_TPH) % 24);
    if (minute) *minute = (int32_t)((ticks / DN2CPP_TPM) % 60);
    if (second) *second = (int32_t)((ticks / DN2CPP_TPS) % 60);
    if (millisecond) *millisecond = (int32_t)((ticks / DN2CPP_TPMS) % 1000);
}
void dn2cpp_datetime_get_time_precise(Dn2CppDateTime a, int32_t* hour, int32_t* minute, int32_t* second, int32_t* tick)
{
    int64_t ticks = a.ticks();
    if (hour) *hour = (int32_t)((ticks / DN2CPP_TPH) % 24);
    if (minute) *minute = (int32_t)((ticks / DN2CPP_TPM) % 60);
    if (second) *second = (int32_t)((ticks / DN2CPP_TPS) % 60);
    if (tick) *tick = (int32_t)(ticks % DN2CPP_TPS);
}
Dn2CppDateTime dn2cpp_datetime_add_ticks(Dn2CppDateTime a, int64_t ticks) { return dn2cpp_datetime_pack(a.ticks() + ticks, a.kind()); }
Dn2CppDateTime dn2cpp_datetime_add_unit(Dn2CppDateTime a, double value, int64_t ticksPerUnit)
{ return dn2cpp_datetime_pack(a.ticks() + (int64_t)(value * (double)ticksPerUnit), a.kind()); }
Dn2CppDateTime dn2cpp_datetime_add_months(Dn2CppDateTime a, int32_t months)
{
    int y, mo, d; dn2cpp_dt_datepart(a.ticks(), &y, &mo, &d, nullptr);
    int64_t tod = a.ticks() % DN2CPP_TPD;
    int i = mo - 1 + months;
    if (i >= 0) { mo = i % 12 + 1; y += i / 12; }
    else { mo = 12 + (i + 1) % 12; y += (i - 11) / 12; }
    const int* days = dn2cpp_dt_isleap(y) ? s_daysToMonth366 : s_daysToMonth365;
    int dim = days[mo] - days[mo - 1];
    if (d > dim) d = dim; // clamp (e.g. Jan 31 + 1 month -> Feb 28)
    return dn2cpp_datetime_pack(dn2cpp_dt_date_to_ticks(y, mo, d) + tod, a.kind());
}
Dn2CppDateTime dn2cpp_datetime_add_years(Dn2CppDateTime a, int32_t years) { return dn2cpp_datetime_add_months(a, years * 12); }
int32_t dn2cpp_datetime_cmp(Dn2CppDateTime a, Dn2CppDateTime b)
{ return a.ticks() < b.ticks() ? -1 : (a.ticks() > b.ticks() ? 1 : 0); }
int32_t dn2cpp_datetime_is_leap_year(int32_t y) { return dn2cpp_dt_isleap(y) ? 1 : 0; }
int32_t dn2cpp_datetime_days_in_month(int32_t y, int32_t mo)
{ return dn2cpp_dt_days_in_month_checked(y, mo); }
int32_t dn2cpp_datetime_hash(Dn2CppDateTime a) { return (int32_t)a.ticks() ^ (int32_t)(a.ticks() >> 32); }

Dn2CppString* dn2cpp_datetime_to_string(Dn2CppDateTime a)
{
    int y, mo, d; dn2cpp_dt_datepart(a.ticks(), &y, &mo, &d, nullptr);
    int64_t tod = a.ticks() % DN2CPP_TPD;
    int h = (int)(tod / DN2CPP_TPH);
    int mi = (int)((tod / DN2CPP_TPM) % 60);
    int s = (int)((tod / DN2CPP_TPS) % 60);
    char buf[32];
    int n = std::snprintf(buf, sizeof(buf), "%02d/%02d/%04d %02d:%02d:%02d", mo, d, y, h, mi, s);
    return dn2cpp_string_from_utf8(buf, n);
}

// ---- DateTime wall clock ----
// Ticks at the Unix epoch (1970-01-01) measured from the .NET epoch (0001-01-01).
static const int64_t DN2CPP_UNIX_EPOCH_TICKS = 621355968000000000LL;

// UtcNow: 100ns ticks since 0001-01-01, in UTC (Kind=Utc=1). Full sub-second precision.
Dn2CppDateTime dn2cpp_datetime_utc_now()
{
    using namespace std::chrono;
    using tick_dur = duration<int64_t, std::ratio<1, 10000000>>; // 1 tick = 100ns
    int64_t unix_ticks = duration_cast<tick_dur>(system_clock::now().time_since_epoch()).count();
    return dn2cpp_datetime_pack(DN2CPP_UNIX_EPOCH_TICKS + unix_ticks, 1);
}
// Now: the same instant expressed in local wall-clock components (Kind=Local=2). The
// broken-down local time (incl. DST) comes from localtime_r; ticks are then rebuilt from
// the local Y/M/D/H:M:S so no time-zone-offset arithmetic is needed. The sub-second
// fraction (whole-second remainder of the UTC instant) is added back.
Dn2CppDateTime dn2cpp_datetime_now()
{
    using namespace std::chrono;
    using tick_dur = duration<int64_t, std::ratio<1, 10000000>>;
    int64_t unix_ticks = duration_cast<tick_dur>(system_clock::now().time_since_epoch()).count();
    std::time_t secs = (std::time_t)(unix_ticks / DN2CPP_TPS);
    int64_t frac = unix_ticks % DN2CPP_TPS;
    std::tm lt{};
    dn2cpp_pal_localtime((int64_t)secs, &lt);
    int64_t date_ticks = dn2cpp_dt_date_to_ticks(lt.tm_year + 1900, lt.tm_mon + 1, lt.tm_mday);
    int64_t time_ticks = dn2cpp_dt_time_to_ticks(lt.tm_hour, lt.tm_min, lt.tm_sec, 0);
    return dn2cpp_datetime_pack(date_ticks + time_ticks + frac, 2);
}
// Today: local midnight (Now with the time-of-day stripped), Kind=Local.
Dn2CppDateTime dn2cpp_datetime_today()
{
    Dn2CppDateTime n = dn2cpp_datetime_now();
    return dn2cpp_datetime_pack(n.ticks() - (n.ticks() % DN2CPP_TPD), n.kind());
}

// ---- DateTime time-zone conversion (host local zone, DST included) ----
// The real corelib path (TimeZoneInfo / ICU / TZif) is untranspilable, so these
// lean on the same libc clock primitives as the wall-clock helpers: localtime_r
// (UTC instant -> broken-down local) and mktime (broken-down local -> UTC instant),
// both of which fold in the host zone's offset and DST automatically. Only whole-
// second offsets are applied; the sub-second tick fraction passes through intact.

// ToLocalTime: Utc/Unspecified are treated as UTC and projected onto the local
// wall clock (Kind=Local). An already-Local value is returned unchanged.
//
// These two SATURATE rather than throw, which is why they are the only callers of
// dn2cpp_datetime_pack_clamped: real .NET's ToLocalTime/ToUniversalTime run through
// TimeZoneInfo, which clamps the shifted instant to MinValue/MaxValue instead of
// raising. Routing them through the checked pack would turn a value real .NET
// answers into an exception.
Dn2CppDateTime dn2cpp_datetime_to_local(Dn2CppDateTime a)
{
    if (a.kind() == 2) return a; // already Local
    int64_t frac = a.ticks() % DN2CPP_TPS;                       // sub-second remainder
    int64_t whole = a.ticks() - frac;                            // whole-second UTC instant
    std::time_t secs = (std::time_t)((whole - DN2CPP_UNIX_EPOCH_TICKS) / DN2CPP_TPS);
    std::tm lt{};
    dn2cpp_pal_localtime((int64_t)secs, &lt);
    int64_t date_ticks = dn2cpp_dt_date_to_ticks(lt.tm_year + 1900, lt.tm_mon + 1, lt.tm_mday);
    int64_t time_ticks = dn2cpp_dt_time_to_ticks(lt.tm_hour, lt.tm_min, lt.tm_sec, 0);
    return dn2cpp_datetime_pack_clamped(date_ticks + time_ticks + frac, 2);
}
// ToUniversalTime: Local/Unspecified are treated as local wall clock and folded
// back to the UTC instant (Kind=Utc). An already-Utc value is returned unchanged.
Dn2CppDateTime dn2cpp_datetime_to_universal(Dn2CppDateTime a)
{
    if (a.kind() == 1) return a; // already Utc
    int64_t frac = a.ticks() % DN2CPP_TPS;
    int y, mo, d; dn2cpp_dt_datepart(a.ticks(), &y, &mo, &d, nullptr);
    int64_t tod = a.ticks() % DN2CPP_TPD;
    std::tm lt{};
    lt.tm_year = y - 1900; lt.tm_mon = mo - 1; lt.tm_mday = d;
    lt.tm_hour = (int)(tod / DN2CPP_TPH);
    lt.tm_min  = (int)((tod / DN2CPP_TPM) % 60);
    lt.tm_sec  = (int)((tod / DN2CPP_TPS) % 60);
    lt.tm_isdst = -1; // let mktime resolve DST for this local instant
    std::time_t secs = (std::time_t)dn2cpp_pal_mktime_local(&lt);
    return dn2cpp_datetime_pack_clamped(DN2CPP_UNIX_EPOCH_TICKS + (int64_t)secs * DN2CPP_TPS + frac, 1);
}
// The host's UTC offset (whole minutes) for a local wall-clock instant. mktime maps the
// local components to the UTC instant; the offset is the (clock - UTC) difference. Whole-
// second remainder is dropped from both sides so the result is exact whole minutes (every
// real zone is a whole-minute offset for current dates).
int32_t dn2cpp_local_offset_minutes(Dn2CppDateTime localClock)
{
    int64_t whole = localClock.ticks() - (localClock.ticks() % DN2CPP_TPS);
    Dn2CppDateTime utc = dn2cpp_datetime_to_universal(dn2cpp_datetime_pack(whole, 0));
    return (int32_t)((whole - utc.ticks()) / DN2CPP_TPM);
}

// ---- System.DateTimeOffset ----
// Ticks at the Unix epoch in *seconds* / *milliseconds* (from the .NET epoch), used by the
// Unix-time conversions (matches DateTimeOffset.UnixEpochSeconds / UnixEpochMilliseconds).
static const int64_t DN2CPP_UNIX_EPOCH_SECONDS = DN2CPP_UNIX_EPOCH_TICKS / DN2CPP_TPS; // 62135596800
static const int64_t DN2CPP_UNIX_EPOCH_MILLIS  = DN2CPP_UNIX_EPOCH_TICKS / DN2CPP_TPMS; // 62135596800000

Dn2CppDateTimeOffset dn2cpp_datetimeoffset_make(int64_t clockTicks, int32_t offsetMinutes)
{ return Dn2CppDateTimeOffset{ clockTicks, offsetMinutes }; }
// new DateTimeOffset(DateTime) / implicit operator: the offset comes from the Kind. Utc -> 0;
// Local/Unspecified -> the host offset for that wall-clock instant. The clock ticks are the
// DateTime's ticks as-is.
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_from_datetime(Dn2CppDateTime dt)
{
    int32_t off = dt.kind() == 1 ? 0 : dn2cpp_local_offset_minutes(dt);
    return Dn2CppDateTimeOffset{ dt.ticks(), off };
}
// new DateTimeOffset(DateTime, TimeSpan): clock = the DateTime's ticks, offset = the TimeSpan
// in whole minutes (carve-out: the .NET Kind/offset consistency validation is relaxed).
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_from_dt_offset(Dn2CppDateTime dt, Dn2CppTimeSpan offset)
{ return Dn2CppDateTimeOffset{ dt.ticks(), (int32_t)(offset.ticks / DN2CPP_TPM) }; }
// ToOffset(TimeSpan): same instant, a new offset. The new clock is the UTC instant shifted
// by the new offset.
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_to_offset(Dn2CppDateTimeOffset d, Dn2CppTimeSpan offset)
{
    int32_t newOff = (int32_t)(offset.ticks / DN2CPP_TPM);
    int64_t utc = d.ticks - (int64_t)d.offsetMinutes * DN2CPP_TPM;
    return Dn2CppDateTimeOffset{ utc + (int64_t)newOff * DN2CPP_TPM, newOff };
}
Dn2CppDateTime dn2cpp_datetimeoffset_clock(Dn2CppDateTimeOffset d) { return dn2cpp_datetime_pack(d.ticks, 0); }
Dn2CppDateTime dn2cpp_datetimeoffset_utc(Dn2CppDateTimeOffset d)
{ return dn2cpp_datetime_pack(d.ticks - (int64_t)d.offsetMinutes * DN2CPP_TPM, 1); }
Dn2CppTimeSpan dn2cpp_datetimeoffset_offset(Dn2CppDateTimeOffset d)
{ return Dn2CppTimeSpan{ (int64_t)d.offsetMinutes * DN2CPP_TPM }; }
int64_t dn2cpp_datetimeoffset_utc_ticks(Dn2CppDateTimeOffset d)
{ return d.ticks - (int64_t)d.offsetMinutes * DN2CPP_TPM; }
// Ordering / equality are by the UTC instant, so two values for the same instant at
// different offsets compare equal (matches DateTimeOffset.CompareTo / == / Equals).
int32_t dn2cpp_datetimeoffset_cmp(Dn2CppDateTimeOffset a, Dn2CppDateTimeOffset b)
{
    int64_t ua = dn2cpp_datetimeoffset_utc_ticks(a), ub = dn2cpp_datetimeoffset_utc_ticks(b);
    return ua < ub ? -1 : (ua > ub ? 1 : 0);
}
int32_t dn2cpp_datetimeoffset_equals(Dn2CppDateTimeOffset a, Dn2CppDateTimeOffset b)
{ return dn2cpp_datetimeoffset_utc_ticks(a) == dn2cpp_datetimeoffset_utc_ticks(b) ? 1 : 0; }
// GetHashCode == UtcDateTime.GetHashCode(), so it agrees with the instant-based equality.
int32_t dn2cpp_datetimeoffset_hash(Dn2CppDateTimeOffset d)
{ return dn2cpp_datetime_hash(dn2cpp_datetimeoffset_utc(d)); }
int64_t dn2cpp_datetimeoffset_to_unix_seconds(Dn2CppDateTimeOffset d)
{ return dn2cpp_datetimeoffset_utc_ticks(d) / DN2CPP_TPS - DN2CPP_UNIX_EPOCH_SECONDS; }
int64_t dn2cpp_datetimeoffset_to_unix_millis(Dn2CppDateTimeOffset d)
{ return dn2cpp_datetimeoffset_utc_ticks(d) / DN2CPP_TPMS - DN2CPP_UNIX_EPOCH_MILLIS; }
// FromUnixTime* build a UTC value (offset 0): the clock equals the UTC instant.
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_from_unix_seconds(int64_t s)
{ return Dn2CppDateTimeOffset{ s * DN2CPP_TPS + DN2CPP_UNIX_EPOCH_TICKS, 0 }; }
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_from_unix_millis(int64_t ms)
{ return Dn2CppDateTimeOffset{ ms * DN2CPP_TPMS + DN2CPP_UNIX_EPOCH_TICKS, 0 }; }
// Default ToString(): invariant "MM/dd/yyyy HH:mm:ss zzz" (clock components + signed offset).
Dn2CppString* dn2cpp_datetimeoffset_to_string(Dn2CppDateTimeOffset d)
{
    int y, mo, dd; dn2cpp_dt_datepart(d.ticks, &y, &mo, &dd, nullptr);
    int64_t tod = d.ticks % DN2CPP_TPD;
    int h = (int)(tod / DN2CPP_TPH);
    int mi = (int)((tod / DN2CPP_TPM) % 60);
    int s = (int)((tod / DN2CPP_TPS) % 60);
    int32_t off = d.offsetMinutes;
    char sign = off < 0 ? '-' : '+';
    int32_t ao = off < 0 ? -off : off;
    char buf[48];
    int n = std::snprintf(buf, sizeof(buf), "%02d/%02d/%04d %02d:%02d:%02d %c%02d:%02d",
        mo, dd, y, h, mi, s, sign, ao / 60, ao % 60);
    return dn2cpp_string_from_utf8(buf, n);
}

// The invariant DateTimeFormatInfo's abbreviated month / day names as string[], used to
// pre-populate the synthesized invariant DTFI (CultureInfo.get_DateTimeFormat). dn2cpp does
// not construct a DTFI's CultureData/Calendar, so the lazy name-array init would deref null;
// seeding the fields with the invariant values (which is all the always-invariant culture's
// DateTimeFormat ever holds) means that init is never reached. 13 month slots (the 13th
// empty, matching .NET's calendar convention) and 7 day slots.
// arrType is the caller's precise string[] type-info (ti_arr_String), so the getter's
// castclass to it succeeds — a bare object[] (dn2cpp_newarr_ref) would fail that cast.
Dn2CppArrayRef* dn2cpp_dtfi_invariant_abbrev_month_names(const Dn2CppTypeInfo* arrType)
{
    static const char* const names[13] = {
        "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec",""};
    Dn2CppArrayRef* a = dn2cpp_newarr_ref_t(13, arrType);
    for (int32_t i = 0; i < 13; i++)
        dn2cpp_gc_store_ref(&a->data[i], reinterpret_cast<Dn2CppObject*>(
            dn2cpp_string_from_utf8(names[i], static_cast<int32_t>(std::strlen(names[i])))));
    return a;
}
Dn2CppArrayRef* dn2cpp_dtfi_invariant_abbrev_day_names(const Dn2CppTypeInfo* arrType)
{
    static const char* const names[7] = {"Sun","Mon","Tue","Wed","Thu","Fri","Sat"};
    Dn2CppArrayRef* a = dn2cpp_newarr_ref_t(7, arrType);
    for (int32_t i = 0; i < 7; i++)
        dn2cpp_gc_store_ref(&a->data[i], reinterpret_cast<Dn2CppObject*>(
            dn2cpp_string_from_utf8(names[i], static_cast<int32_t>(std::strlen(names[i])))));
    return a;
}

// ISpanFormattable write of an already-formatted string into a char span: fits ->
// copy + set *written + return 1; too short -> leave the span untouched, *written = 0,
// return 0 (the .NET TryFormat contract). Backs DateTimeOffset.TryFormat's span overload.
int32_t dn2cpp_string_try_copy_to_span(Dn2CppString* s, char16_t* dest, int32_t destLen, int32_t* written)
{
    int32_t n = (s != nullptr) ? s->length : 0;
    if (n > destLen)
    {
        if (written != nullptr) *written = 0;
        return 0;
    }
    for (int32_t i = 0; i < n; i++)
        dest[i] = s->chars[i];
    if (written != nullptr) *written = n;
    return 1;
}

// The IUtf8SpanFormattable sibling of the above: the same TryFormat contract over a
// UTF-8 destination. Transcoding goes through dn2cpp_utf16_to_utf8 — sized first, then
// written — because a per-code-unit encode would emit a surrogate PAIR as two 3-byte
// sequences (CESU-8, not UTF-8) and would also over-estimate the size, refusing spans
// .NET fills. That helper folds a lone surrogate to U+FFFD, as Encoding.UTF8 does.
int32_t dn2cpp_string_try_copy_to_utf8_span(Dn2CppString* s, uint8_t* dest, int32_t destLen, int32_t* written)
{
    if (s == nullptr)
    {
        if (written != nullptr) *written = 0;
        return 1;
    }
    int32_t utf8Len = dn2cpp_utf16_to_utf8(s->chars, s->length, nullptr, 0);
    if (utf8Len > destLen)
    {
        if (written != nullptr) *written = 0;
        return 0;
    }
    dn2cpp_utf16_to_utf8(s->chars, s->length, reinterpret_cast<char*>(dest), utf8Len);
    if (written != nullptr) *written = utf8Len;
    return 1;
}

// ---- DateTime / TimeSpan custom formatting (InvariantCulture only) ----
// Names indexed as the runtime exposes them: month 1-based, day-of-week 0=Sunday
// (matches dn2cpp_datetime_dayofweek).
static const char* const s_dtMonthFull[12] = {
    "January", "February", "March", "April", "May", "June",
    "July", "August", "September", "October", "November", "December" };
static const char* const s_dtMonthAbbr[12] = {
    "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
static const char* const s_dtDayFull[7] = {
    "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
static const char* const s_dtDayAbbr[7] = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

// Append an ASCII C-string into a char16_t output buffer at index o.
static void dn2cpp_fmt_puts(char16_t* out, int32_t& o, const char* s)
{
    while (*s != '\0') out[o++] = static_cast<char16_t>(static_cast<unsigned char>(*s++));
}
// Append `val` (>= 0) as decimal, zero-padded to at least `width` digits.
static void dn2cpp_fmt_putnum(char16_t* out, int32_t& o, int64_t val, int width)
{
    char tmp[24]; int t = 0;
    if (val == 0) tmp[t++] = '0';
    else { while (val > 0) { tmp[t++] = static_cast<char>('0' + (int)(val % 10)); val /= 10; } }
    for (int i = t; i < width; i++) out[o++] = u'0';
    for (int i = t - 1; i >= 0; i--) out[o++] = static_cast<char16_t>(tmp[i]);
}
// Append the first `k` digits (k in 1..7) of the 7-digit second fraction `frac7`.
// `trim` (the 'F' specifier) drops trailing zeros and emits nothing when all zero;
// when it emits nothing it also removes an immediately preceding '.' (a .NET quirk:
// "HH:mm:ss.FFF" on a whole second yields "...:ss", not "...:ss.").
static void dn2cpp_fmt_putfrac(char16_t* out, int32_t& o, int frac7, int k, bool trim)
{
    if (k > 7) k = 7;
    int div = 1; for (int e = 0; e < 7 - k; e++) div *= 10;
    int val = frac7 / div;
    char buf[8];
    for (int j = k - 1; j >= 0; j--) { buf[j] = static_cast<char>('0' + val % 10); val /= 10; }
    int last = k;
    if (trim) while (last > 0 && buf[last - 1] == '0') last--;
    if (trim && last == 0)
    {
        if (o > 0 && out[o - 1] == u'.') o--; // drop the now-orphaned decimal separator
        return;
    }
    for (int j = 0; j < last; j++) out[o++] = static_cast<char16_t>(buf[j]);
}

// Render a signed UTC offset for the z/zz/zzz/K specifiers (DateTimeOffset).
// width: 1='z' (sign + minimal hours), 2='zz' (sign + 2-digit hours), 3='zzz'/'K'
// (sign + HH:mm). Minutes are shown only for width 3 (matches .NET: -05:30 -> "-5"/"-05"/"-05:30").
static void dn2cpp_fmt_put_offset(char16_t* out, int32_t& o, int32_t offMin, int width)
{
    out[o++] = offMin < 0 ? u'-' : u'+';
    int32_t ao = offMin < 0 ? -offMin : offMin;
    if (width == 1) dn2cpp_fmt_putnum(out, o, ao / 60, 1);
    else if (width == 2) dn2cpp_fmt_putnum(out, o, ao / 60, 2);
    else { dn2cpp_fmt_putnum(out, o, ao / 60, 2); out[o++] = u':'; dn2cpp_fmt_putnum(out, o, ao % 60, 2); }
}

// Render a DateTime custom format pattern. Fields are pre-decomposed; `kind` is the
// DateTimeKind int (0=Unspecified, 1=Utc, 2=Local). When `hasOffset` (a DateTimeOffset
// value), z/zz/zzz/K render `offMin`; otherwise z renders the no-tz-model zero and
// K renders the Kind tag (DateTime, no time-zone offset to apply).
static void dn2cpp_dt_format_custom(const char16_t* p, int32_t n,
    int y, int mo, int d, int dow, int h24, int mi, int s, int frac7, int kind,
    int32_t offMin, bool hasOffset,
    char16_t* out, int32_t& o)
{
    int i = 0;
    while (i < n)
    {
        char16_t c = p[i];
        if (c == u'\'' || c == u'"') // quoted literal run
        {
            char16_t q = c; i++;
            while (i < n && p[i] != q) out[o++] = p[i++];
            if (i < n) i++; // closing quote
            continue;
        }
        if (c == u'\\') { i++; if (i < n) out[o++] = p[i++]; continue; } // escape next char
        if (c == u'%') { i++; continue; } // force single custom specifier (no-op here)
        int cnt = 1;
        while (i + cnt < n && p[i + cnt] == c) cnt++;
        bool handled = true;
        switch (c)
        {
            case u'd':
                if (cnt == 1) dn2cpp_fmt_putnum(out, o, d, 1);
                else if (cnt == 2) dn2cpp_fmt_putnum(out, o, d, 2);
                else if (cnt == 3) dn2cpp_fmt_puts(out, o, s_dtDayAbbr[dow]);
                else dn2cpp_fmt_puts(out, o, s_dtDayFull[dow]);
                break;
            case u'M':
                if (cnt == 1) dn2cpp_fmt_putnum(out, o, mo, 1);
                else if (cnt == 2) dn2cpp_fmt_putnum(out, o, mo, 2);
                else if (cnt == 3) dn2cpp_fmt_puts(out, o, s_dtMonthAbbr[mo - 1]);
                else dn2cpp_fmt_puts(out, o, s_dtMonthFull[mo - 1]);
                break;
            case u'y':
                dn2cpp_fmt_putnum(out, o, cnt <= 2 ? y % 100 : y, cnt == 1 ? 1 : cnt);
                break;
            case u'H': dn2cpp_fmt_putnum(out, o, h24, cnt >= 2 ? 2 : 1); break;
            case u'h': { int h12 = h24 % 12; if (h12 == 0) h12 = 12; dn2cpp_fmt_putnum(out, o, h12, cnt >= 2 ? 2 : 1); break; }
            case u'm': dn2cpp_fmt_putnum(out, o, mi, cnt >= 2 ? 2 : 1); break;
            case u's': dn2cpp_fmt_putnum(out, o, s, cnt >= 2 ? 2 : 1); break;
            case u'f': dn2cpp_fmt_putfrac(out, o, frac7, cnt, false); break;
            case u'F': dn2cpp_fmt_putfrac(out, o, frac7, cnt, true); break;
            case u't':
                if (cnt == 1) out[o++] = (h24 < 12) ? u'A' : u'P';
                else dn2cpp_fmt_puts(out, o, (h24 < 12) ? "AM" : "PM");
                break;
            case u'g': dn2cpp_fmt_puts(out, o, "A.D."); break;
            case u'K': // DateTimeOffset: the offset as +HH:mm. DateTime: the Kind tag.
                if (hasOffset) dn2cpp_fmt_put_offset(out, o, offMin, 3);
                else if (kind == 1) out[o++] = u'Z';
                else if (kind == 2) dn2cpp_fmt_puts(out, o, "+00:00");
                break;
            case u'z': // DateTimeOffset: the value's offset. DateTime: zero (no tz model).
                if (hasOffset) dn2cpp_fmt_put_offset(out, o, offMin, cnt == 1 ? 1 : (cnt == 2 ? 2 : 3));
                else if (cnt == 1) dn2cpp_fmt_puts(out, o, "+0");
                else if (cnt == 2) dn2cpp_fmt_puts(out, o, "+00");
                else dn2cpp_fmt_puts(out, o, "+00:00");
                break;
            case u':': out[o++] = u':'; break; // invariant time separator
            case u'/': out[o++] = u'/'; break; // invariant date separator
            default: handled = false; break;
        }
        if (handled) i += cnt;
        else { out[o++] = c; i++; } // literal pass-through
    }
}

// Standard single-char specifier -> its InvariantCulture pattern, or nullptr if the
// char is not a recognized standard specifier (caller throws FormatException).
static const char* dn2cpp_dt_standard_pattern(char16_t c)
{
    switch (c)
    {
        case u'd': return "MM/dd/yyyy";
        case u'D': return "dddd, dd MMMM yyyy";
        case u'f': return "dddd, dd MMMM yyyy HH:mm";
        case u'F': return "dddd, dd MMMM yyyy HH:mm:ss";
        case u'g': return "MM/dd/yyyy HH:mm";
        case u'G': return "MM/dd/yyyy HH:mm:ss";
        case u'M': case u'm': return "MMMM dd";
        case u'O': case u'o': return "yyyy-MM-ddTHH:mm:ss.fffffffK";
        case u'R': case u'r': return "ddd, dd MMM yyyy HH:mm:ss 'GMT'";
        case u's': return "yyyy-MM-ddTHH:mm:ss";
        case u't': return "HH:mm";
        case u'T': return "HH:mm:ss";
        case u'u': return "yyyy-MM-dd HH:mm:ssZ";
        case u'Y': case u'y': return "yyyy MMMM";
        default: return nullptr; // 'U' (UTC conversion) is a carve-out -> FormatException
    }
}

Dn2CppString* dn2cpp_datetime_format(Dn2CppDateTime a, Dn2CppString* fmt)
{
    int y, mo, d; dn2cpp_dt_datepart(a.ticks(), &y, &mo, &d, nullptr);
    int dow = (int)((a.ticks() / DN2CPP_TPD + 1) % 7);
    int64_t tod = a.ticks() % DN2CPP_TPD;
    int h = (int)(tod / DN2CPP_TPH);
    int mi = (int)((tod / DN2CPP_TPM) % 60);
    int s = (int)((tod / DN2CPP_TPS) % 60);
    int frac7 = (int)(a.ticks() % DN2CPP_TPS);

    // null/empty -> the "G" pattern; a single char -> its standard pattern; otherwise
    // the string is a custom format. Standard patterns are short ASCII, copied to a
    // char16_t buffer so the one custom renderer serves every case.
    const char16_t* pat; int32_t plen;
    char16_t patbuf[48];
    if (fmt == nullptr || fmt->length <= 1)
    {
        const char* sp;
        if (fmt == nullptr || fmt->length == 0) sp = "MM/dd/yyyy HH:mm:ss";
        else
        {
            sp = dn2cpp_dt_standard_pattern(fmt->chars[0]);
            if (sp == nullptr) dn2cpp_throw_format();
        }
        int32_t j = 0;
        while (sp[j] != '\0') { patbuf[j] = static_cast<char16_t>(static_cast<unsigned char>(sp[j])); j++; }
        pat = patbuf; plen = j;
    }
    else { pat = fmt->chars; plen = fmt->length; }

    // Each pattern char expands to at most 9 output chars (full month/day names);
    // 12x + a slack constant is a safe upper bound, then we trim length to o.
    int32_t cap = plen * 12 + 64;
    char16_t* out; Dn2CppString* res = dn2cpp_string_alloc(&out, cap);
    int32_t o = 0;
    dn2cpp_dt_format_custom(pat, plen, y, mo, d, dow, h, mi, s, frac7, a.kind(), 0, false, out, o);
    res->length = o; out[o] = u'\0';
    return res;
}

// DateTimeOffset ToString(format). Every standard specifier and custom format
// renders the *clock* components (with the value's offset shown by z/zz/zzz/K); the
// universal specifiers 'R'/'r'/'u' render the *UTC* instant (their patterns carry a
// literal 'GMT'/'Z'). null/empty -> the default invariant ToString.
Dn2CppString* dn2cpp_datetimeoffset_format(Dn2CppDateTimeOffset d, Dn2CppString* fmt)
{
    if (fmt == nullptr || fmt->length == 0)
        return dn2cpp_datetimeoffset_to_string(d);

    Dn2CppDateTime src = dn2cpp_datetimeoffset_clock(d); // clock (Kind=Unspecified) by default
    bool universal = false;
    const char16_t* pat; int32_t plen; char16_t patbuf[48];
    if (fmt->length == 1)
    {
        char16_t fc = fmt->chars[0];
        if (fc == u'R' || fc == u'r' || fc == u'u') { src = dn2cpp_datetimeoffset_utc(d); universal = true; }
        const char* sp = dn2cpp_dt_standard_pattern(fc);
        if (sp == nullptr) dn2cpp_throw_format();
        int32_t j = 0;
        while (sp[j] != '\0') { patbuf[j] = static_cast<char16_t>(static_cast<unsigned char>(sp[j])); j++; }
        pat = patbuf; plen = j;
    }
    else { pat = fmt->chars; plen = fmt->length; }

    int y, mo, dd; dn2cpp_dt_datepart(src.ticks(), &y, &mo, &dd, nullptr);
    int dow = (int)((src.ticks() / DN2CPP_TPD + 1) % 7);
    int64_t tod = src.ticks() % DN2CPP_TPD;
    int h = (int)(tod / DN2CPP_TPH);
    int mi = (int)((tod / DN2CPP_TPM) % 60);
    int s = (int)((tod / DN2CPP_TPS) % 60);
    int frac7 = (int)(src.ticks() % DN2CPP_TPS);
    int32_t cap = plen * 12 + 64;
    char16_t* out; Dn2CppString* res = dn2cpp_string_alloc(&out, cap);
    int32_t o = 0;
    // For the universal specifiers the pattern uses a literal 'Z'/'GMT', so no z/K offset
    // is rendered (hasOffset=false); every other case shows the value's offset.
    dn2cpp_dt_format_custom(pat, plen, y, mo, dd, dow, h, mi, s, frac7, 0, d.offsetMinutes, !universal, out, o);
    res->length = o; out[o] = u'\0';
    return res;
}

// TimeSpan "g"/"G": split the signed value, take each component's magnitude (avoids
// overflow at MinValue), then render. "g" = [-][d:]h:mm:ss[.FFFFFFF] (minimal hours,
// days/fraction omitted when zero); "G" = [-]d:hh:mm:ss.fffffff (always shown).
static Dn2CppString* dn2cpp_ts_format_general(Dn2CppTimeSpan a, bool longForm)
{
    int64_t ticks = a.ticks;
    bool neg = ticks < 0;
    int64_t days = ticks / DN2CPP_TPD; int64_t rem = ticks % DN2CPP_TPD;
    int h = (int)(rem / DN2CPP_TPH); rem %= DN2CPP_TPH;
    int mi = (int)(rem / DN2CPP_TPM); rem %= DN2CPP_TPM;
    int s = (int)(rem / DN2CPP_TPS);
    int frac = (int)(rem % DN2CPP_TPS);
    if (days < 0) days = -days;
    if (h < 0) h = -h; if (mi < 0) mi = -mi; if (s < 0) s = -s; if (frac < 0) frac = -frac;
    char16_t out[48]; int32_t o = 0;
    if (neg) out[o++] = u'-';
    if (longForm)
    {
        dn2cpp_fmt_putnum(out, o, days, 1); out[o++] = u':';
        dn2cpp_fmt_putnum(out, o, h, 2); out[o++] = u':';
        dn2cpp_fmt_putnum(out, o, mi, 2); out[o++] = u':';
        dn2cpp_fmt_putnum(out, o, s, 2);
        out[o++] = u'.'; dn2cpp_fmt_putfrac(out, o, frac, 7, false);
    }
    else
    {
        if (days != 0) { dn2cpp_fmt_putnum(out, o, days, 1); out[o++] = u':'; }
        dn2cpp_fmt_putnum(out, o, h, 1); out[o++] = u':';
        dn2cpp_fmt_putnum(out, o, mi, 2); out[o++] = u':';
        dn2cpp_fmt_putnum(out, o, s, 2);
        if (frac != 0) { out[o++] = u'.'; dn2cpp_fmt_putfrac(out, o, frac, 7, true); }
    }
    return dn2cpp_string_from_chars(out, o);
}

// TimeSpan custom format. Components are magnitudes (no automatic sign, matching
// .NET): d (days, dd.. zero-padded), h/hh, m/mm, s/ss, f/F (fraction), ' " quotes,
// \ escape, % single-specifier, : separator.
static Dn2CppString* dn2cpp_ts_format_custom(Dn2CppTimeSpan a, const char16_t* p, int32_t n)
{
    int64_t ticks = a.ticks;
    int64_t days = ticks / DN2CPP_TPD; int64_t rem = ticks % DN2CPP_TPD;
    int h = (int)(rem / DN2CPP_TPH); rem %= DN2CPP_TPH;
    int mi = (int)(rem / DN2CPP_TPM); rem %= DN2CPP_TPM;
    int s = (int)(rem / DN2CPP_TPS);
    int frac = (int)(rem % DN2CPP_TPS);
    if (days < 0) days = -days;
    if (h < 0) h = -h; if (mi < 0) mi = -mi; if (s < 0) s = -s; if (frac < 0) frac = -frac;
    int32_t cap = n * 12 + 32;
    char16_t* out; Dn2CppString* res = dn2cpp_string_alloc(&out, cap);
    int32_t o = 0, i = 0;
    while (i < n)
    {
        char16_t c = p[i];
        if (c == u'\'' || c == u'"') { char16_t q = c; i++; while (i < n && p[i] != q) out[o++] = p[i++]; if (i < n) i++; continue; }
        if (c == u'\\') { i++; if (i < n) out[o++] = p[i++]; continue; }
        if (c == u'%') { i++; continue; }
        int cnt = 1;
        while (i + cnt < n && p[i + cnt] == c) cnt++;
        bool handled = true;
        switch (c)
        {
            case u'd': dn2cpp_fmt_putnum(out, o, days, cnt > 8 ? 8 : cnt); break;
            case u'h': dn2cpp_fmt_putnum(out, o, h, cnt >= 2 ? 2 : 1); break;
            case u'm': dn2cpp_fmt_putnum(out, o, mi, cnt >= 2 ? 2 : 1); break;
            case u's': dn2cpp_fmt_putnum(out, o, s, cnt >= 2 ? 2 : 1); break;
            case u'f': dn2cpp_fmt_putfrac(out, o, frac, cnt, false); break;
            case u'F': dn2cpp_fmt_putfrac(out, o, frac, cnt, true); break;
            case u':': out[o++] = u':'; break;
            default: handled = false; break;
        }
        if (handled) i += cnt;
        else { out[o++] = c; i++; }
    }
    res->length = o; out[o] = u'\0';
    return res;
}

Dn2CppString* dn2cpp_timespan_format(Dn2CppTimeSpan a, Dn2CppString* fmt)
{
    if (fmt == nullptr || fmt->length == 0)
        return dn2cpp_timespan_to_string(a); // null/empty == "c"
    if (fmt->length == 1)
    {
        char16_t c = fmt->chars[0];
        if (c == u'c' || c == u't' || c == u'T') return dn2cpp_timespan_to_string(a);
        if (c == u'g') return dn2cpp_ts_format_general(a, false);
        if (c == u'G') return dn2cpp_ts_format_general(a, true);
        dn2cpp_throw_format();
    }
    return dn2cpp_ts_format_custom(a, fmt->chars, fmt->length);
}

// ---- DateTime / TimeSpan parsing (InvariantCulture only) ----
// Read up to `maxDigits` consecutive ASCII digits from p[i..n); advance i; return the
// value and the digit count via *count (0 = no digit at i).
static int64_t dn2cpp_parse_digits(const char16_t* p, int n, int& i, int maxDigits, int& count)
{
    int64_t v = 0; count = 0;
    while (i < n && count < maxDigits && p[i] >= u'0' && p[i] <= u'9')
    { v = v * 10 + (p[i] - u'0'); i++; count++; }
    return v;
}
// Case-insensitive (ASCII) match of `name` at in[ii..]; on success advance ii.
static bool dn2cpp_ci_match(const char16_t* in, int inLen, int& ii, const char* name)
{
    int k = ii, j = 0;
    while (name[j] != '\0')
    {
        if (k >= inLen) return false;
        char16_t a = in[k];
        char16_t au = (a >= u'a' && a <= u'z') ? (char16_t)(a - 32) : a;
        char nc = name[j];
        char16_t ncu = (char16_t)(unsigned char)((nc >= 'a' && nc <= 'z') ? nc - 32 : nc);
        if (au != ncu) return false;
        k++; j++;
    }
    ii = k;
    return true;
}
static int dn2cpp_match_month(const char16_t* in, int inLen, int& ii, bool full)
{
    const char* const* tab = full ? s_dtMonthFull : s_dtMonthAbbr;
    for (int m = 0; m < 12; m++) { int k = ii; if (dn2cpp_ci_match(in, inLen, k, tab[m])) { ii = k; return m + 1; } }
    return 0;
}
static int dn2cpp_match_day(const char16_t* in, int inLen, int& ii, bool full)
{
    const char* const* tab = full ? s_dtDayFull : s_dtDayAbbr;
    for (int w = 0; w < 7; w++) { int k = ii; if (dn2cpp_ci_match(in, inLen, k, tab[w])) { ii = k; return w; } }
    return -1;
}
// Parse a numeric UTC offset [+-]HH[[:]mm] at in[ii..) into signed minutes; on
// success advance ii and set *offMin. Does NOT handle 'Z' (the caller checks that first).
// Returns false if there's no leading +/- or the hour digits are missing/out of range.
static bool dn2cpp_parse_offset_token(const char16_t* in, int n, int& ii, int32_t* offMin)
{
    if (ii >= n || (in[ii] != u'+' && in[ii] != u'-')) return false;
    int sign = (in[ii] == u'-') ? -1 : 1; ii++;
    int c; int hh = (int)dn2cpp_parse_digits(in, n, ii, 2, c); if (c == 0) return false;
    int mm = 0;
    if (ii < n && in[ii] == u':') { ii++; mm = (int)dn2cpp_parse_digits(in, n, ii, 2, c); if (c == 0) return false; }
    else if (ii < n && in[ii] >= u'0' && in[ii] <= u'9') { mm = (int)dn2cpp_parse_digits(in, n, ii, 2, c); if (c == 0) return false; }
    if (hh > 14 || mm > 59) return false;
    *offMin = sign * (hh * 60 + mm);
    return true;
}

// TimeSpan general invariant parse: [ws][-]{ d | [d.]hh:mm[:ss[.fffffff]] }[ws].
bool dn2cpp_timespan_try_parse(Dn2CppString* s, Dn2CppTimeSpan* out)
{
    if (s == nullptr) return false;
    const char16_t* p = s->chars;
    int i = 0, e = s->length;
    while (i < e && (p[i] == u' ' || p[i] == u'\t')) i++;
    while (e > i && (p[e - 1] == u' ' || p[e - 1] == u'\t')) e--;
    if (i >= e) return false;
    bool neg = false;
    if (p[i] == u'-') { neg = true; i++; } else if (p[i] == u'+') i++;
    if (i >= e) return false;
    int colon = -1;
    for (int k = i; k < e; k++) if (p[k] == u':') { colon = k; break; }
    int64_t days = 0, hh = 0, mm = 0, ss = 0, frac = 0;
    int c;
    if (colon == -1)
    {
        days = dn2cpp_parse_digits(p, e, i, 8, c);
        if (c == 0 || i != e) return false;
    }
    else
    {
        int dot = -1;
        for (int k = i; k < colon; k++) if (p[k] == u'.') { dot = k; break; }
        if (dot != -1)
        {
            days = dn2cpp_parse_digits(p, dot, i, 8, c);
            if (c == 0 || i != dot) return false;
            i = dot + 1;
        }
        hh = dn2cpp_parse_digits(p, e, i, 2, c); if (c == 0) return false;
        if (i >= e || p[i] != u':') return false; i++;
        mm = dn2cpp_parse_digits(p, e, i, 2, c); if (c == 0) return false;
        if (i < e && p[i] == u':')
        {
            i++;
            ss = dn2cpp_parse_digits(p, e, i, 2, c); if (c == 0) return false;
            if (i < e && p[i] == u'.')
            {
                i++;
                int fc; int64_t fv = dn2cpp_parse_digits(p, e, i, 7, fc); if (fc == 0) return false;
                for (int z = fc; z < 7; z++) fv *= 10;
                frac = fv;
            }
        }
        if (i != e) return false;
        if (hh > 23 || mm > 59 || ss > 59) return false;
    }
    int64_t ticks = days * DN2CPP_TPD + hh * DN2CPP_TPH + mm * DN2CPP_TPM + ss * DN2CPP_TPS + frac;
    out->ticks = neg ? -ticks : ticks;
    return true;
}

// TimeSpan custom-format parse (inverse of dn2cpp_ts_format_custom): d/h/m/s/f/F,
// ' " quotes, \ escape, % single-specifier, literals.
static bool dn2cpp_ts_parse_custom(const char16_t* p, int pn, const char16_t* in, int inLen, Dn2CppTimeSpan* out)
{
    int fi = 0, ii = 0, c;
    int64_t days = 0, hh = 0, mm = 0, ss = 0, frac = 0;
    while (fi < pn)
    {
        char16_t ch = p[fi];
        if (ch == u'\'' || ch == u'"') { char16_t q = ch; fi++; while (fi < pn && p[fi] != q) { if (ii >= inLen || in[ii] != p[fi]) return false; ii++; fi++; } if (fi < pn) fi++; continue; }
        if (ch == u'\\') { fi++; if (fi < pn) { if (ii >= inLen || in[ii] != p[fi]) return false; ii++; fi++; } continue; }
        if (ch == u'%') { fi++; continue; }
        int cnt = 1; while (fi + cnt < pn && p[fi + cnt] == ch) cnt++;
        switch (ch)
        {
            case u'd': days = dn2cpp_parse_digits(in, inLen, ii, 8, c); if (c == 0) return false; break;
            case u'h': hh = dn2cpp_parse_digits(in, inLen, ii, 2, c); if (c == 0) return false; break;
            case u'm': mm = dn2cpp_parse_digits(in, inLen, ii, 2, c); if (c == 0) return false; break;
            case u's': ss = dn2cpp_parse_digits(in, inLen, ii, 2, c); if (c == 0) return false; break;
            case u'f': case u'F':
            {
                int fc; int64_t fv = dn2cpp_parse_digits(in, inLen, ii, cnt > 7 ? 7 : cnt, fc);
                if (ch == u'f' && fc == 0) return false;
                for (int z = fc; z < 7; z++) fv *= 10;
                frac = fv;
                break;
            }
            default:
                if (ii >= inLen || in[ii] != ch) return false; ii++; fi += cnt; continue;
        }
        fi += cnt;
    }
    if (ii != inLen) return false;
    if (hh > 23 || mm > 59 || ss > 59) return false;
    out->ticks = days * DN2CPP_TPD + hh * DN2CPP_TPH + mm * DN2CPP_TPM + ss * DN2CPP_TPS + frac;
    return true;
}

bool dn2cpp_timespan_try_parse_exact(Dn2CppString* s, Dn2CppString* fmt, Dn2CppTimeSpan* out)
{
    if (s == nullptr || fmt == nullptr) return false;
    if (fmt->length == 1)
    {
        char16_t c = fmt->chars[0];
        // c/t/T (constant) and g/G general all read the general invariant form here.
        if (c == u'c' || c == u't' || c == u'T' || c == u'g' || c == u'G')
            return dn2cpp_timespan_try_parse(s, out);
        return false;
    }
    return dn2cpp_ts_parse_custom(fmt->chars, fmt->length, s->chars, s->length, out);
}

// .NET answers a null input and a malformed one with DIFFERENT exception types --
// ArgumentNullException for null, FormatException for a string that does not
// parse -- and try_parse reports only that it failed, so the two are told apart
// here rather than through it.
Dn2CppTimeSpan dn2cpp_timespan_parse(Dn2CppString* s)
{
    if (s == nullptr) dn2cpp_throw_argument_null();
    Dn2CppTimeSpan r;
    if (!dn2cpp_timespan_try_parse(s, &r))
        dn2cpp_throw_sr1(&dn2cpp_format_exception_type, DN2CPP_SR_BAD_TIMESPAN, s);
    return r;
}
Dn2CppTimeSpan dn2cpp_timespan_parse_exact(Dn2CppString* s, Dn2CppString* fmt)
{
    if (s == nullptr || fmt == nullptr) dn2cpp_throw_argument_null();
    Dn2CppTimeSpan r;
    if (!dn2cpp_timespan_try_parse_exact(s, fmt, &r))
        dn2cpp_throw_sr1(&dn2cpp_format_exception_type, DN2CPP_SR_BAD_TIMESPAN, s);
    return r;
}

// DateTime custom-format parse (inverse of dn2cpp_dt_format_custom). A format with no
// date part leaves the default 0001-01-01 (no clock; carve-out). For DateTime
// (offOut == nullptr) a z/K offset is consumed but not applied (only 'Z' sets Kind=Utc);
// for DateTimeOffset (offOut != nullptr) a z/K offset is captured into *offOut and
// *hasOffOut, and a missing/malformed offset there fails the parse.
static bool dn2cpp_dt_parse_custom(const char16_t* p, int pn, const char16_t* in, int inLen,
    Dn2CppDateTime* out, int32_t* offOut, bool* hasOffOut)
{
    int fi = 0, ii = 0, c;
    int y = 1, mo = 1, d = 1, h = 0, mi = 0, ss = 0, frac = 0, kind = 0;
    bool h12 = false, pm = false;
    while (fi < pn)
    {
        char16_t ch = p[fi];
        if (ch == u'\'' || ch == u'"') { char16_t q = ch; fi++; while (fi < pn && p[fi] != q) { if (ii >= inLen || in[ii] != p[fi]) return false; ii++; fi++; } if (fi < pn) fi++; continue; }
        if (ch == u'\\') { fi++; if (fi < pn) { if (ii >= inLen || in[ii] != p[fi]) return false; ii++; fi++; } continue; }
        if (ch == u'%') { fi++; continue; }
        int cnt = 1; while (fi + cnt < pn && p[fi + cnt] == ch) cnt++;
        switch (ch)
        {
            case u'y': { int md = cnt >= 3 ? cnt : (cnt == 2 ? 2 : 4); y = (int)dn2cpp_parse_digits(in, inLen, ii, md, c); if (c == 0) return false; break; }
            case u'M':
                if (cnt >= 3) { int m = dn2cpp_match_month(in, inLen, ii, cnt >= 4); if (m == 0) return false; mo = m; }
                else { mo = (int)dn2cpp_parse_digits(in, inLen, ii, 2, c); if (c == 0) return false; }
                break;
            case u'd':
                if (cnt >= 3) { if (dn2cpp_match_day(in, inLen, ii, cnt >= 4) < 0) return false; } // weekday name: consume, no validation
                else { d = (int)dn2cpp_parse_digits(in, inLen, ii, 2, c); if (c == 0) return false; }
                break;
            case u'H': h = (int)dn2cpp_parse_digits(in, inLen, ii, 2, c); if (c == 0) return false; break;
            case u'h': h = (int)dn2cpp_parse_digits(in, inLen, ii, 2, c); if (c == 0) return false; h12 = true; break;
            case u'm': mi = (int)dn2cpp_parse_digits(in, inLen, ii, 2, c); if (c == 0) return false; break;
            case u's': ss = (int)dn2cpp_parse_digits(in, inLen, ii, 2, c); if (c == 0) return false; break;
            case u'f': case u'F':
            {
                int fc; int64_t fv = dn2cpp_parse_digits(in, inLen, ii, cnt > 7 ? 7 : cnt, fc);
                if (ch == u'f' && fc == 0) return false;
                for (int z = fc; z < 7; z++) fv *= 10;
                frac = (int)fv;
                break;
            }
            case u't':
            {
                if (ii + 2 <= inLen)
                {
                    char16_t a = in[ii], b = in[ii + 1];
                    char16_t au = (a >= u'a' && a <= u'z') ? (char16_t)(a - 32) : a;
                    char16_t bu = (b >= u'a' && b <= u'z') ? (char16_t)(b - 32) : b;
                    if (bu == u'M' && (au == u'A' || au == u'P')) { pm = (au == u'P'); ii += 2; break; }
                }
                if (cnt == 1 && ii < inLen) // single 't' -> one char A/P
                {
                    char16_t a = in[ii]; char16_t au = (a >= u'a' && a <= u'z') ? (char16_t)(a - 32) : a;
                    if (au == u'A' || au == u'P') { pm = (au == u'P'); ii++; break; }
                }
                return false;
            }
            case u'z': case u'K': // tz offset / kind tag
                if (offOut) // DateTimeOffset: capture the offset ('Z' = UTC; else [+-]HH[:]mm)
                {
                    if (ii < inLen && in[ii] == u'Z') { *offOut = 0; *hasOffOut = true; ii++; }
                    else if (dn2cpp_parse_offset_token(in, inLen, ii, offOut)) *hasOffOut = true;
                    else return false;
                }
                else // DateTime: consume, apply only Z (Utc)
                {
                    if (ii < inLen && in[ii] == u'Z') { kind = 1; ii++; }
                    else while (ii < inLen && (in[ii] == u'+' || in[ii] == u'-' || in[ii] == u':' || (in[ii] >= u'0' && in[ii] <= u'9'))) ii++;
                }
                break;
            default:
                if (ii >= inLen || in[ii] != ch) return false; ii++; fi += cnt; continue;
        }
        fi += cnt;
    }
    if (ii != inLen) return false;
    if (h12) { if (pm) { if (h != 12) h += 12; } else if (h == 12) h = 0; }
    if (mo < 1 || mo > 12 || h > 23 || mi > 59 || ss > 59 || y < 1 || y > 9999) return false;
    if (d < 1 || d > dn2cpp_datetime_days_in_month(y, mo)) return false;
    *out = dn2cpp_datetime_pack(
        dn2cpp_dt_date_to_ticks(y, mo, d) + dn2cpp_dt_time_to_ticks(h, mi, ss, 0) + frac, kind);
    return true;
}

bool dn2cpp_datetime_try_parse_exact(Dn2CppString* s, Dn2CppString* fmt, Dn2CppDateTime* out)
{
    if (s == nullptr || fmt == nullptr) return false;
    const char16_t* pat; int plen; char16_t patbuf[48];
    if (fmt->length == 1)
    {
        const char* sp = dn2cpp_dt_standard_pattern(fmt->chars[0]);
        if (sp == nullptr) return false;
        int j = 0; while (sp[j] != '\0') { patbuf[j] = (char16_t)(unsigned char)sp[j]; j++; }
        pat = patbuf; plen = j;
    }
    else { pat = fmt->chars; plen = fmt->length; }
    return dn2cpp_dt_parse_custom(pat, plen, s->chars, s->length, out, nullptr, nullptr);
}

// DateTime general invariant parse over an already-trimmed range p[i..e): ISO
// yyyy-MM-dd[(T| )HH:mm[:ss[.fffffff]]] and the US-invariant M/d/yyyy[ HH:mm[:ss[.fffffff]]].
// Date separator picks the field order ('-' = y-M-d, '/' = M/d/y). A bare time (no date) is
// unsupported (no clock). On success sets *ticksOut. Shared by DateTime.Parse and
// DateTimeOffset.Parse (the latter strips a trailing offset first).
static bool dn2cpp_dt_parse_general_range(const char16_t* p, int i, int e, int64_t* ticksOut)
{
    int c;
    while (i < e && (p[i] == u' ' || p[i] == u'\t')) i++;
    while (e > i && (p[e - 1] == u' ' || p[e - 1] == u'\t')) e--;
    if (i >= e) return false;
    // Split date / time on the first 'T' or ' '.
    int split = -1;
    for (int k = i; k < e; k++) if (p[k] == u'T' || p[k] == u' ') { split = k; break; }
    int dateEnd = (split == -1) ? e : split;
    int timeStart = (split == -1) ? e : split + 1;
    // Date field separator.
    char16_t sep = 0;
    for (int k = i; k < dateEnd; k++) { if (p[k] == u'-') { sep = u'-'; break; } if (p[k] == u'/') { sep = u'/'; break; } }
    if (sep == 0) return false;
    int parts[3] = { 0, 0, 0 }, np = 0, k = i;
    while (k < dateEnd && np < 3)
    {
        int64_t v = dn2cpp_parse_digits(p, dateEnd, k, 4, c);
        if (c == 0) return false;
        parts[np++] = (int)v;
        if (k < dateEnd && p[k] == sep) k++;
        else break;
    }
    if (np != 3 || k != dateEnd) return false;
    int y, mo, d;
    if (sep == u'-') { y = parts[0]; mo = parts[1]; d = parts[2]; }
    else { mo = parts[0]; d = parts[1]; y = parts[2]; }
    int h = 0, mi = 0, ss = 0, frac = 0;
    if (timeStart < e)
    {
        k = timeStart;
        h = (int)dn2cpp_parse_digits(p, e, k, 2, c); if (c == 0) return false;
        if (k >= e || p[k] != u':') return false; k++;
        mi = (int)dn2cpp_parse_digits(p, e, k, 2, c); if (c == 0) return false;
        if (k < e && p[k] == u':')
        {
            k++;
            ss = (int)dn2cpp_parse_digits(p, e, k, 2, c); if (c == 0) return false;
            if (k < e && p[k] == u'.')
            {
                k++;
                int fc; int64_t fv = dn2cpp_parse_digits(p, e, k, 7, fc); if (fc == 0) return false;
                for (int z = fc; z < 7; z++) fv *= 10;
                frac = (int)fv;
            }
        }
        if (k != e) return false;
        if (h > 23 || mi > 59 || ss > 59) return false;
    }
    if (y < 1 || y > 9999 || mo < 1 || mo > 12) return false;
    if (d < 1 || d > dn2cpp_datetime_days_in_month(y, mo)) return false;
    *ticksOut = dn2cpp_dt_date_to_ticks(y, mo, d) + dn2cpp_dt_time_to_ticks(h, mi, ss, 0) + frac;
    return true;
}

bool dn2cpp_datetime_try_parse(Dn2CppString* s, Dn2CppDateTime* out)
{
    if (s == nullptr) return false;
    int64_t ticks;
    if (!dn2cpp_dt_parse_general_range(s->chars, 0, s->length, &ticks)) return false;
    *out = dn2cpp_datetime_pack(ticks, 0);
    return true;
}

Dn2CppDateTime dn2cpp_datetime_parse(Dn2CppString* s)
{
    if (s == nullptr) dn2cpp_throw_argument_null();
    Dn2CppDateTime r;
    if (!dn2cpp_datetime_try_parse(s, &r))
        dn2cpp_throw_sr1(&dn2cpp_format_exception_type, DN2CPP_SR_BAD_DATETIME, s);
    return r;
}
Dn2CppDateTime dn2cpp_datetime_parse_exact(Dn2CppString* s, Dn2CppString* fmt)
{
    if (s == nullptr || fmt == nullptr) dn2cpp_throw_argument_null();
    Dn2CppDateTime r;
    if (!dn2cpp_datetime_try_parse_exact(s, fmt, &r))
        dn2cpp_throw_sr1(&dn2cpp_format_exception_type, DN2CPP_SR_BAD_DATETIME, s);
    return r;
}

// ---- DateTimeOffset parsing / arithmetic / wall clock ----
// General invariant parse: the DateTime general form plus an optional trailing UTC offset
// ('Z' = UTC, or [+-]HH[:]mm). A missing offset falls back to the host local offset (like
// DateTimeOffset.Parse on a no-offset string — non-deterministic, host zone only).
bool dn2cpp_datetimeoffset_try_parse(Dn2CppString* s, Dn2CppDateTimeOffset* out)
{
    if (s == nullptr) return false;
    const char16_t* p = s->chars;
    int i = 0, e = s->length;
    while (i < e && (p[i] == u' ' || p[i] == u'\t')) i++;
    while (e > i && (p[e - 1] == u' ' || p[e - 1] == u'\t')) e--;
    if (i >= e) return false;

    int32_t off = 0; bool hasOff = false;
    if (p[e - 1] == u'Z') { off = 0; hasOff = true; e--; }
    else
    {
        // The offset is a [+-]HH[:]mm tail (<= 6 chars). Find the rightmost +/- within that
        // window and accept it only if it parses as an offset running exactly to the end —
        // this avoids mistaking an ISO date's '-' separators for an offset.
        for (int k = e - 1; k >= i && k >= e - 6; k--)
        {
            if (p[k] == u'+' || p[k] == u'-')
            {
                int tmp = k; int32_t o;
                if (dn2cpp_parse_offset_token(p, e, tmp, &o) && tmp == e) { off = o; hasOff = true; e = k; }
                break;
            }
        }
    }
    while (e > i && (p[e - 1] == u' ' || p[e - 1] == u'\t')) e--;

    int64_t ticks;
    if (!dn2cpp_dt_parse_general_range(p, i, e, &ticks)) return false;
    if (!hasOff) off = dn2cpp_local_offset_minutes(dn2cpp_datetime_pack(ticks, 0));
    out->ticks = ticks; out->offsetMinutes = off;
    return true;
}
bool dn2cpp_datetimeoffset_try_parse_exact(Dn2CppString* s, Dn2CppString* fmt, Dn2CppDateTimeOffset* out)
{
    if (s == nullptr || fmt == nullptr) return false;
    const char16_t* pat; int plen; char16_t patbuf[48];
    if (fmt->length == 1)
    {
        const char* sp = dn2cpp_dt_standard_pattern(fmt->chars[0]);
        if (sp == nullptr) return false;
        int j = 0; while (sp[j] != '\0') { patbuf[j] = (char16_t)(unsigned char)sp[j]; j++; }
        pat = patbuf; plen = j;
    }
    else { pat = fmt->chars; plen = fmt->length; }
    Dn2CppDateTime dt; int32_t off = 0; bool hasOff = false;
    if (!dn2cpp_dt_parse_custom(pat, plen, s->chars, s->length, &dt, &off, &hasOff)) return false;
    if (!hasOff) off = dn2cpp_local_offset_minutes(dt); // no offset in the pattern -> host local
    out->ticks = dt.ticks(); out->offsetMinutes = off;
    return true;
}
// TryParseExact(input, string[] formats, ...): try each format in turn, first match wins —
// the multi-format shape HttpDateParser.TryParse uses (RFC1123 / RFC850 / asctime).
bool dn2cpp_datetimeoffset_try_parse_exact_multi(Dn2CppString* s, Dn2CppArrayRef* formats, Dn2CppDateTimeOffset* out)
{
    if (formats == nullptr) return false;
    for (int32_t i = 0; i < formats->length; i++)
    {
        auto* fmt = reinterpret_cast<Dn2CppString*>(formats->data[i]);
        if (dn2cpp_datetimeoffset_try_parse_exact(s, fmt, out))
            return true;
    }
    return false;
}
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_parse(Dn2CppString* s)
{
    if (s == nullptr) dn2cpp_throw_argument_null();
    Dn2CppDateTimeOffset r;
    if (!dn2cpp_datetimeoffset_try_parse(s, &r))
        dn2cpp_throw_sr1(&dn2cpp_format_exception_type, DN2CPP_SR_BAD_DATETIME, s);
    return r;
}
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_parse_exact(Dn2CppString* s, Dn2CppString* fmt)
{
    if (s == nullptr || fmt == nullptr) dn2cpp_throw_argument_null();
    Dn2CppDateTimeOffset r;
    if (!dn2cpp_datetimeoffset_try_parse_exact(s, fmt, &r))
        dn2cpp_throw_sr1(&dn2cpp_format_exception_type, DN2CPP_SR_BAD_DATETIME, s);
    return r;
}
// Arithmetic keeps the offset and acts on the clock value (= new DateTimeOffset(ClockDateTime
// +/- delta, Offset)). The UTC instant therefore shifts by the same delta.
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_add_ticks(Dn2CppDateTimeOffset d, int64_t ticks)
{ return Dn2CppDateTimeOffset{ d.ticks + ticks, d.offsetMinutes }; }
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_add_unit(Dn2CppDateTimeOffset d, double value, int64_t ticksPerUnit)
{ Dn2CppDateTime c = dn2cpp_datetime_add_unit(dn2cpp_datetime_pack(d.ticks, 0), value, ticksPerUnit); return Dn2CppDateTimeOffset{ c.ticks(), d.offsetMinutes }; }
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_add_months(Dn2CppDateTimeOffset d, int32_t months)
{ Dn2CppDateTime c = dn2cpp_datetime_add_months(dn2cpp_datetime_pack(d.ticks, 0), months); return Dn2CppDateTimeOffset{ c.ticks(), d.offsetMinutes }; }
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_add_years(Dn2CppDateTimeOffset d, int32_t years)
{ Dn2CppDateTime c = dn2cpp_datetime_add_years(dn2cpp_datetime_pack(d.ticks, 0), years); return Dn2CppDateTimeOffset{ c.ticks(), d.offsetMinutes }; }
// Now: local clock + the host local offset. UtcNow: the UTC instant at offset 0. Both are
// non-deterministic (host clock + zone) — gates assert invariants only.
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_now()
{
    Dn2CppDateTime n = dn2cpp_datetime_now(); // local wall clock (Kind=Local)
    return Dn2CppDateTimeOffset{ n.ticks(), dn2cpp_local_offset_minutes(n) };
}
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_utc_now()
{
    Dn2CppDateTime u = dn2cpp_datetime_utc_now();
    return Dn2CppDateTimeOffset{ u.ticks(), 0 };
}

// ===== System.DateOnly =====
// A date with no time, stored as a day number (days since 0001-01-01). The calendar
// helpers above operate on ticks, so a DateOnly maps to a midnight DateTime via
// ticks = dayNumber * DN2CPP_TPD; the day number itself is ticks / DN2CPP_TPD.
Dn2CppDateOnly dn2cpp_dateonly_from_daynum(int32_t n) { return Dn2CppDateOnly{ n }; }
Dn2CppDateOnly dn2cpp_dateonly_ymd(int32_t y, int32_t mo, int32_t d)
{ return Dn2CppDateOnly{ (int32_t)(dn2cpp_dt_date_to_ticks(y, mo, d) / DN2CPP_TPD) }; }
Dn2CppDateOnly dn2cpp_dateonly_from_datetime(Dn2CppDateTime dt)
{ return Dn2CppDateOnly{ (int32_t)(dt.ticks() / DN2CPP_TPD) }; }
int32_t dn2cpp_dateonly_year(Dn2CppDateOnly dd) { int y; dn2cpp_dt_datepart((int64_t)dd.dayNumber * DN2CPP_TPD, &y, nullptr, nullptr, nullptr); return y; }
int32_t dn2cpp_dateonly_month(Dn2CppDateOnly dd) { int m; dn2cpp_dt_datepart((int64_t)dd.dayNumber * DN2CPP_TPD, nullptr, &m, nullptr, nullptr); return m; }
int32_t dn2cpp_dateonly_day(Dn2CppDateOnly dd) { int d; dn2cpp_dt_datepart((int64_t)dd.dayNumber * DN2CPP_TPD, nullptr, nullptr, &d, nullptr); return d; }
// 0=Sunday..6=Saturday. 0001-01-01 (dayNumber 0) is a Monday, matching dn2cpp_datetime_dayofweek.
int32_t dn2cpp_dateonly_dayofweek(Dn2CppDateOnly dd) { return (int32_t)((dd.dayNumber + 1) % 7); }
int32_t dn2cpp_dateonly_dayofyear(Dn2CppDateOnly dd) { int doy; dn2cpp_dt_datepart((int64_t)dd.dayNumber * DN2CPP_TPD, nullptr, nullptr, nullptr, &doy); return doy; }
Dn2CppDateOnly dn2cpp_dateonly_add_days(Dn2CppDateOnly dd, int32_t days) { return Dn2CppDateOnly{ dd.dayNumber + days }; }
Dn2CppDateOnly dn2cpp_dateonly_add_months(Dn2CppDateOnly dd, int32_t months)
{
    int y, mo, d; dn2cpp_dt_datepart((int64_t)dd.dayNumber * DN2CPP_TPD, &y, &mo, &d, nullptr);
    int i = mo - 1 + months;
    if (i >= 0) { mo = i % 12 + 1; y += i / 12; }
    else { mo = 12 + (i + 1) % 12; y += (i - 11) / 12; }
    const int* days = dn2cpp_dt_isleap(y) ? s_daysToMonth366 : s_daysToMonth365;
    int dim = days[mo] - days[mo - 1];
    if (d > dim) d = dim; // clamp (Jan 31 + 1 month -> Feb 28), like DateTime
    return Dn2CppDateOnly{ (int32_t)(dn2cpp_dt_date_to_ticks(y, mo, d) / DN2CPP_TPD) };
}
Dn2CppDateOnly dn2cpp_dateonly_add_years(Dn2CppDateOnly dd, int32_t years) { return dn2cpp_dateonly_add_months(dd, years * 12); }
int32_t dn2cpp_dateonly_cmp(Dn2CppDateOnly a, Dn2CppDateOnly b)
{ return a.dayNumber < b.dayNumber ? -1 : (a.dayNumber > b.dayNumber ? 1 : 0); }
int32_t dn2cpp_dateonly_hash(Dn2CppDateOnly dd) { return dd.dayNumber; }

Dn2CppString* dn2cpp_dateonly_to_string(Dn2CppDateOnly dd)
{
    int y, mo, d; dn2cpp_dt_datepart((int64_t)dd.dayNumber * DN2CPP_TPD, &y, &mo, &d, nullptr);
    char buf[16];
    int n = std::snprintf(buf, sizeof(buf), "%02d/%02d/%04d", mo, d, y);
    return dn2cpp_string_from_utf8(buf, n);
}

// Standard single-char specifier -> its InvariantCulture *date-only* pattern. Unlike
// dn2cpp_dt_standard_pattern, O/o and R/r carry no time component (DateOnly is date-only).
static const char* dn2cpp_dateonly_standard_pattern(char16_t c)
{
    switch (c)
    {
        case u'd': return "MM/dd/yyyy";
        case u'D': return "dddd, dd MMMM yyyy";
        case u'M': case u'm': return "MMMM dd";
        case u'O': case u'o': return "yyyy-MM-dd";
        case u'R': case u'r': return "ddd, dd MMM yyyy";
        case u'Y': case u'y': return "yyyy MMMM";
        default: return nullptr;
    }
}
Dn2CppString* dn2cpp_dateonly_format(Dn2CppDateOnly dd, Dn2CppString* fmt)
{
    int y, mo, d; dn2cpp_dt_datepart((int64_t)dd.dayNumber * DN2CPP_TPD, &y, &mo, &d, nullptr);
    int dow = (int)((dd.dayNumber + 1) % 7);
    const char16_t* pat; int32_t plen; char16_t patbuf[48];
    if (fmt == nullptr || fmt->length <= 1)
    {
        const char* sp;
        if (fmt == nullptr || fmt->length == 0) sp = "MM/dd/yyyy"; // default == "d"
        else
        {
            sp = dn2cpp_dateonly_standard_pattern(fmt->chars[0]);
            if (sp == nullptr) dn2cpp_throw_format();
        }
        int32_t j = 0;
        while (sp[j] != '\0') { patbuf[j] = static_cast<char16_t>(static_cast<unsigned char>(sp[j])); j++; }
        pat = patbuf; plen = j;
    }
    else { pat = fmt->chars; plen = fmt->length; }
    int32_t cap = plen * 12 + 64;
    char16_t* out; Dn2CppString* res = dn2cpp_string_alloc(&out, cap);
    int32_t o = 0;
    // Date-only: zero the time components (a time specifier in a custom format renders 0).
    dn2cpp_dt_format_custom(pat, plen, y, mo, d, dow, 0, 0, 0, 0, 0, 0, false, out, o);
    res->length = o; out[o] = u'\0';
    return res;
}
// Parse reuses the DateTime parser and keeps the date part (a time component is dropped —
// a carve-out vs .NET, which throws FormatException on a time in DateOnly.Parse).
bool dn2cpp_dateonly_try_parse(Dn2CppString* s, Dn2CppDateOnly* out)
{
    Dn2CppDateTime dt;
    if (!dn2cpp_datetime_try_parse(s, &dt)) { out->dayNumber = 0; return false; }
    out->dayNumber = (int32_t)(dt.ticks() / DN2CPP_TPD);
    return true;
}
bool dn2cpp_dateonly_try_parse_exact(Dn2CppString* s, Dn2CppString* fmt, Dn2CppDateOnly* out)
{
    Dn2CppDateTime dt;
    if (!dn2cpp_datetime_try_parse_exact(s, fmt, &dt)) { out->dayNumber = 0; return false; }
    out->dayNumber = (int32_t)(dt.ticks() / DN2CPP_TPD);
    return true;
}
// Not dn2cpp_datetime_parse: DateOnly's refusal names DateOnly, so the failure has to
// be raised here rather than inherited from the DateTime parser it borrows.
Dn2CppDateOnly dn2cpp_dateonly_parse(Dn2CppString* s)
{
    if (s == nullptr) dn2cpp_throw_argument_null();
    Dn2CppDateOnly r;
    if (!dn2cpp_dateonly_try_parse(s, &r))
        dn2cpp_throw_sr1(&dn2cpp_format_exception_type, DN2CPP_SR_BAD_DATEONLY, s);
    return r;
}
Dn2CppDateOnly dn2cpp_dateonly_parse_exact(Dn2CppString* s, Dn2CppString* fmt)
{
    if (s == nullptr || fmt == nullptr) dn2cpp_throw_argument_null();
    Dn2CppDateOnly r;
    if (!dn2cpp_dateonly_try_parse_exact(s, fmt, &r))
        dn2cpp_throw_sr1(&dn2cpp_format_exception_type, DN2CPP_SR_BAD_DATEONLY, s);
    return r;
}

// ===== System.TimeOnly =====
// A time within a day, stored as a tick count since midnight (0..863999999999 = TPD-1).
Dn2CppTimeOnly dn2cpp_timeonly_from_ticks(int64_t ticks) { return Dn2CppTimeOnly{ ticks }; }
Dn2CppTimeOnly dn2cpp_timeonly_hms(int32_t h, int32_t m, int32_t s)
{ return Dn2CppTimeOnly{ dn2cpp_dt_time_of_day_to_ticks(h, m, s, 0) }; }
Dn2CppTimeOnly dn2cpp_timeonly_hmsms(int32_t h, int32_t m, int32_t s, int32_t ms)
{ return Dn2CppTimeOnly{ dn2cpp_dt_time_of_day_to_ticks(h, m, s, ms) }; }
Dn2CppTimeOnly dn2cpp_timeonly_from_timespan(Dn2CppTimeSpan ts) { return Dn2CppTimeOnly{ ts.ticks }; }
Dn2CppTimeOnly dn2cpp_timeonly_from_datetime(Dn2CppDateTime dt) { return Dn2CppTimeOnly{ dt.ticks() % DN2CPP_TPD }; }
int32_t dn2cpp_timeonly_hour(Dn2CppTimeOnly t) { return (int32_t)((t.ticks / DN2CPP_TPH) % 24); }
int32_t dn2cpp_timeonly_minute(Dn2CppTimeOnly t) { return (int32_t)((t.ticks / DN2CPP_TPM) % 60); }
int32_t dn2cpp_timeonly_second(Dn2CppTimeOnly t) { return (int32_t)((t.ticks / DN2CPP_TPS) % 60); }
int32_t dn2cpp_timeonly_millisecond(Dn2CppTimeOnly t) { return (int32_t)((t.ticks / DN2CPP_TPMS) % 1000); }
// Add* wrap within the day (matches TimeOnly.Add / AddHours / AddMinutes).
Dn2CppTimeOnly dn2cpp_timeonly_add(Dn2CppTimeOnly t, int64_t deltaTicks)
{ int64_t r = (t.ticks + deltaTicks) % DN2CPP_TPD; if (r < 0) r += DN2CPP_TPD; return Dn2CppTimeOnly{ r }; }
Dn2CppTimeOnly dn2cpp_timeonly_add_unit(Dn2CppTimeOnly t, double value, int64_t ticksPerUnit)
{ return dn2cpp_timeonly_add(t, (int64_t)(value * (double)ticksPerUnit)); }
// TimeOnly - TimeOnly = the wrapped elapsed TimeSpan: ((a-b)+day)%day, always in [0, 24h).
Dn2CppTimeSpan dn2cpp_timeonly_diff(Dn2CppTimeOnly a, Dn2CppTimeOnly b)
{ return Dn2CppTimeSpan{ (a.ticks - b.ticks + DN2CPP_TPD) % DN2CPP_TPD }; }
int32_t dn2cpp_timeonly_cmp(Dn2CppTimeOnly a, Dn2CppTimeOnly b)
{ return a.ticks < b.ticks ? -1 : (a.ticks > b.ticks ? 1 : 0); }
int32_t dn2cpp_timeonly_hash(Dn2CppTimeOnly t) { return (int32_t)t.ticks ^ (int32_t)(t.ticks >> 32); }

Dn2CppString* dn2cpp_timeonly_to_string(Dn2CppTimeOnly t)
{
    int h = (int)(t.ticks / DN2CPP_TPH), mi = (int)((t.ticks / DN2CPP_TPM) % 60);
    char buf[8];
    int n = std::snprintf(buf, sizeof(buf), "%02d:%02d", h, mi);
    return dn2cpp_string_from_utf8(buf, n);
}

// Standard single-char specifier -> its InvariantCulture time pattern (no date component).
static const char* dn2cpp_timeonly_standard_pattern(char16_t c)
{
    switch (c)
    {
        case u't': return "HH:mm";
        case u'T': return "HH:mm:ss";
        case u'O': case u'o': return "HH:mm:ss.fffffff";
        case u'R': case u'r': return "HH:mm:ss";
        default: return nullptr;
    }
}
Dn2CppString* dn2cpp_timeonly_format(Dn2CppTimeOnly t, Dn2CppString* fmt)
{
    int h = (int)(t.ticks / DN2CPP_TPH);
    int mi = (int)((t.ticks / DN2CPP_TPM) % 60);
    int s = (int)((t.ticks / DN2CPP_TPS) % 60);
    int frac7 = (int)(t.ticks % DN2CPP_TPS);
    const char16_t* pat; int32_t plen; char16_t patbuf[48];
    if (fmt == nullptr || fmt->length <= 1)
    {
        const char* sp;
        if (fmt == nullptr || fmt->length == 0) sp = "HH:mm"; // default == "t"
        else
        {
            sp = dn2cpp_timeonly_standard_pattern(fmt->chars[0]);
            if (sp == nullptr) dn2cpp_throw_format();
        }
        int32_t j = 0;
        while (sp[j] != '\0') { patbuf[j] = static_cast<char16_t>(static_cast<unsigned char>(sp[j])); j++; }
        pat = patbuf; plen = j;
    }
    else { pat = fmt->chars; plen = fmt->length; }
    int32_t cap = plen * 12 + 64;
    char16_t* out; Dn2CppString* res = dn2cpp_string_alloc(&out, cap);
    int32_t o = 0;
    // Time-only: placeholder date (0001-01-01, Monday); a date specifier in a custom format
    // renders that placeholder (a carve-out; .NET TimeOnly throws on date specifiers).
    dn2cpp_dt_format_custom(pat, plen, 1, 1, 1, 1, h, mi, s, frac7, 0, 0, false, out, o);
    res->length = o; out[o] = u'\0';
    return res;
}
// Parse the invariant time forms [H]H:mm[:ss[.fffffff]] with an optional trailing AM/PM. The
// DateTime general parser rejects a bare time (no date), so this is a dedicated parser.
bool dn2cpp_timeonly_try_parse(Dn2CppString* s, Dn2CppTimeOnly* out)
{
    out->ticks = 0;
    if (s == nullptr) return false;
    const char16_t* p = s->chars; int i = 0, e = s->length;
    while (i < e && (p[i] == u' ' || p[i] == u'\t')) i++;
    while (e > i && (p[e - 1] == u' ' || p[e - 1] == u'\t')) e--;
    if (i >= e) return false;
    bool h12 = false, pm = false;
    if (e - i >= 2)
    {
        char16_t a = p[e - 2], b = p[e - 1];
        char16_t au = (a >= u'a' && a <= u'z') ? (char16_t)(a - 32) : a;
        char16_t bu = (b >= u'a' && b <= u'z') ? (char16_t)(b - 32) : b;
        if (bu == u'M' && (au == u'A' || au == u'P'))
        { h12 = true; pm = (au == u'P'); e -= 2; while (e > i && (p[e - 1] == u' ' || p[e - 1] == u'\t')) e--; }
    }
    int c, k = i;
    int h = (int)dn2cpp_parse_digits(p, e, k, 2, c); if (c == 0) return false;
    if (k >= e || p[k] != u':') return false; k++;
    int mi = (int)dn2cpp_parse_digits(p, e, k, 2, c); if (c == 0) return false;
    int ss = 0, frac = 0;
    if (k < e && p[k] == u':')
    {
        k++;
        ss = (int)dn2cpp_parse_digits(p, e, k, 2, c); if (c == 0) return false;
        if (k < e && p[k] == u'.')
        {
            k++;
            int fc; int64_t fv = dn2cpp_parse_digits(p, e, k, 7, fc); if (fc == 0) return false;
            for (int z = fc; z < 7; z++) fv *= 10;
            frac = (int)fv;
        }
    }
    if (k != e) return false;
    if (h12) { if (pm) { if (h != 12) h += 12; } else if (h == 12) h = 0; }
    if (h > 23 || mi > 59 || ss > 59) return false;
    out->ticks = dn2cpp_dt_time_to_ticks(h, mi, ss, 0) + frac;
    return true;
}
// ParseExact reuses the DateTime custom parser by prefixing a fixed date to both the format
// and the input (the custom parser requires a valid date part), then keeps the time-of-day.
bool dn2cpp_timeonly_try_parse_exact(Dn2CppString* s, Dn2CppString* fmt, Dn2CppTimeOnly* out)
{
    out->ticks = 0;
    if (s == nullptr || fmt == nullptr) return false;
    char16_t tpat[48]; int tlen;
    if (fmt->length == 1)
    {
        const char* sp = dn2cpp_timeonly_standard_pattern(fmt->chars[0]);
        if (sp == nullptr) return false;
        tlen = 0; while (sp[tlen] != '\0') { tpat[tlen] = (char16_t)(unsigned char)sp[tlen]; tlen++; }
    }
    else
    {
        if (fmt->length > 40) return false;
        tlen = fmt->length; for (int z = 0; z < tlen; z++) tpat[z] = fmt->chars[z];
    }
    char16_t pat[64]; int pl = 0; const char* dp = "yyyy-MM-dd ";
    for (int z = 0; dp[z]; z++) pat[pl++] = (char16_t)dp[z];
    for (int z = 0; z < tlen; z++) pat[pl++] = tpat[z];
    char16_t inb[80]; int il = 0; const char* ip = "0001-01-01 ";
    for (int z = 0; ip[z]; z++) inb[il++] = (char16_t)ip[z];
    if (s->length > 60) return false;
    for (int z = 0; z < s->length; z++) inb[il++] = s->chars[z];
    Dn2CppDateTime dt;
    if (!dn2cpp_dt_parse_custom(pat, pl, inb, il, &dt, nullptr, nullptr)) return false;
    out->ticks = dt.ticks() % DN2CPP_TPD;
    return true;
}
Dn2CppTimeOnly dn2cpp_timeonly_parse(Dn2CppString* s)
{
    if (s == nullptr) dn2cpp_throw_argument_null();
    Dn2CppTimeOnly r;
    if (!dn2cpp_timeonly_try_parse(s, &r))
        dn2cpp_throw_sr1(&dn2cpp_format_exception_type, DN2CPP_SR_BAD_TIMEONLY, s);
    return r;
}
Dn2CppTimeOnly dn2cpp_timeonly_parse_exact(Dn2CppString* s, Dn2CppString* fmt)
{
    if (s == nullptr || fmt == nullptr) dn2cpp_throw_argument_null();
    Dn2CppTimeOnly r;
    if (!dn2cpp_timeonly_try_parse_exact(s, fmt, &r))
        dn2cpp_throw_sr1(&dn2cpp_format_exception_type, DN2CPP_SR_BAD_TIMEONLY, s);
    return r;
}

bool dn2cpp_fmt_numeric(Dn2CppString* fmt)
{
    if (fmt == nullptr || fmt->length == 0)
        return false;
    char16_t c = fmt->chars[0];
    return c == u'D' || c == u'd' || c == u'X' || c == u'x';
}

// Templated on the member table's width: the int32 instantiation is what every
// sub-64-bit enum emits, the int64 one what a long/ulong-underlying [Flags] enum
// needs — its members do not survive the int32 model.
template <typename T>
static Dn2CppString* dn2cpp_enum_flags_to_string_t(T value, const T* vals,
                                                   Dn2CppString* const* names, int32_t count)
{
    using U = typename std::make_unsigned<T>::type;
    constexpr int32_t kBits = static_cast<int32_t>(sizeof(T) * 8);
    U rem = static_cast<U>(value);
    if (rem == 0)
    {
        // A zero value renders the zero-named member ("None"), else "0".
        for (int32_t i = 0; i < count; i++)
            if (vals[i] == 0)
                return names[i];
        return nullptr;
    }
    // Greedy decomposition: vals are sorted by descending unsigned value, so the
    // largest covered flags are consumed first.
    int32_t idx[kBits];
    int n = 0;
    for (int32_t i = 0; i < count && n < kBits; i++)
    {
        U v = static_cast<U>(vals[i]);
        if (v != 0 && (rem & v) == v)
        {
            idx[n++] = i;
            rem &= ~v;
        }
    }
    if (rem != 0)
        return nullptr; // leftover bits -> caller renders the decimal value
    int32_t total = (n - 1) * 2; // ", " separators
    for (int k = 0; k < n; k++)
        total += names[idx[k]]->length;
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, total);
    int32_t o = 0;
    // idx is in descending-value order; emit in reverse for .NET's ascending list.
    for (int k = n - 1; k >= 0; k--)
    {
        Dn2CppString* nm = names[idx[k]];
        std::memcpy(dst + o, nm->chars, static_cast<size_t>(nm->length) * sizeof(char16_t));
        o += nm->length;
        if (k > 0)
        {
            dst[o++] = u',';
            dst[o++] = u' ';
        }
    }
    return s;
}

Dn2CppString* dn2cpp_enum_flags_to_string(int32_t value, const int32_t* vals,
                                          Dn2CppString* const* names, int32_t count)
{
    return dn2cpp_enum_flags_to_string_t(value, vals, names, count);
}

Dn2CppString* dn2cpp_enum_flags_to_string64(int64_t value, const int64_t* vals,
                                            Dn2CppString* const* names, int32_t count)
{
    return dn2cpp_enum_flags_to_string_t(value, vals, names, count);
}

Dn2CppString* dn2cpp_char_to_string(char16_t c)
{
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, 1);
    dst[0] = c;
    return s;
}

Dn2CppString* dn2cpp_string_from_utf32(int32_t cp)
{
    char16_t* dst;
    if (cp <= 0xFFFF)
    {
        Dn2CppString* s = dn2cpp_string_alloc(&dst, 1);
        dst[0] = (char16_t)cp;
        return s;
    }
    Dn2CppString* s = dn2cpp_string_alloc(&dst, 2);
    dst[0] = (char16_t)(((cp - 0x10000) >> 10) + 0xD800);
    dst[1] = (char16_t)(((cp - 0x10000) & 0x3FF) + 0xDC00);
    return s;
}

// ── Boxed type-infos for this namespace's value types ────────────────────────
// These live here, not with the other primitives in dn2cpp_typeinfo.cpp, and they
// spell their tostring/gethashcode/equals slots rather than leaving them null for
// a dispatcher if-chain to sort out. Both serve one end: a program that never
// mentions a date type must not link this translation unit. Static archives link
// at .o granularity, so one naming reference from the always-linked
// dn2cpp_typeinfo.cpp.o would drag all of it into a hello world.

static Dn2CppString* dn2cpp_timespan_box_tostring(Dn2CppObject* o)
{ return dn2cpp_timespan_to_string(*dn2cpp_boxed<Dn2CppTimeSpan>(o)); }
static int32_t dn2cpp_timespan_box_hash(Dn2CppObject* o)
{ return dn2cpp_timespan_hash(*dn2cpp_boxed<Dn2CppTimeSpan>(o)); }
static int32_t dn2cpp_timespan_box_equals(Dn2CppObject* a, Dn2CppObject* b)
{
    return (b->type == &dn2cpp_timespan_type
            && dn2cpp_boxed<Dn2CppTimeSpan>(a)->ticks == dn2cpp_boxed<Dn2CppTimeSpan>(b)->ticks) ? 1 : 0;
}
// $"{ts:c}" / string.Format("{0:g}", ts). The date formatters are culture-invariant,
// so the NumberFormatInfo the numeric holes thread through is unused here.
static Dn2CppString* dn2cpp_timespan_box_formatspec(Dn2CppObject* o, Dn2CppString* spec,
                                                    const Dn2CppNumberFormatInfo*)
{ return dn2cpp_timespan_format(*dn2cpp_boxed<Dn2CppTimeSpan>(o), spec); }

// DateTime formats as the invariant "G" (current culture is not modeled), and
// Equals compares ticks only — Kind is ignored, matching .NET.
static Dn2CppString* dn2cpp_datetime_box_tostring(Dn2CppObject* o)
{ return dn2cpp_datetime_to_string(*dn2cpp_boxed<Dn2CppDateTime>(o)); }
static int32_t dn2cpp_datetime_box_hash(Dn2CppObject* o)
{ return dn2cpp_datetime_hash(*dn2cpp_boxed<Dn2CppDateTime>(o)); }
static int32_t dn2cpp_datetime_box_equals(Dn2CppObject* a, Dn2CppObject* b)
{
    return (b->type == &dn2cpp_datetime_type
            && dn2cpp_boxed<Dn2CppDateTime>(a)->ticks() == dn2cpp_boxed<Dn2CppDateTime>(b)->ticks()) ? 1 : 0;
}
// $"{dt:HH:mm}" / string.Format("{0:yyyy-MM-dd}", dt).
static Dn2CppString* dn2cpp_datetime_box_formatspec(Dn2CppObject* o, Dn2CppString* spec,
                                                    const Dn2CppNumberFormatInfo*)
{ return dn2cpp_datetime_format(*dn2cpp_boxed<Dn2CppDateTime>(o), spec); }

// DateTimeOffset hashes and compares by its UTC instant, not its offset:
// 12:00+00:00 equals 13:00+01:00.
static Dn2CppString* dn2cpp_datetimeoffset_box_tostring(Dn2CppObject* o)
{ return dn2cpp_datetimeoffset_to_string(*dn2cpp_boxed<Dn2CppDateTimeOffset>(o)); }
static int32_t dn2cpp_datetimeoffset_box_hash(Dn2CppObject* o)
{ return dn2cpp_datetimeoffset_hash(*dn2cpp_boxed<Dn2CppDateTimeOffset>(o)); }
static int32_t dn2cpp_datetimeoffset_box_equals(Dn2CppObject* a, Dn2CppObject* b)
{
    return (b->type == &dn2cpp_datetimeoffset_type
            && dn2cpp_datetimeoffset_equals(*dn2cpp_boxed<Dn2CppDateTimeOffset>(a),
                                            *dn2cpp_boxed<Dn2CppDateTimeOffset>(b))) ? 1 : 0;
}
// $"{dto:O}" / string.Format("{0:O}", dto) / ((IFormattable)boxed).ToString("O", inv).
static Dn2CppString* dn2cpp_datetimeoffset_box_formatspec(Dn2CppObject* o, Dn2CppString* spec,
                                                          const Dn2CppNumberFormatInfo*)
{ return dn2cpp_datetimeoffset_format(*dn2cpp_boxed<Dn2CppDateTimeOffset>(o), spec); }

static Dn2CppString* dn2cpp_dateonly_box_tostring(Dn2CppObject* o)
{ return dn2cpp_dateonly_to_string(*dn2cpp_boxed<Dn2CppDateOnly>(o)); }
static int32_t dn2cpp_dateonly_box_hash(Dn2CppObject* o)
{ return dn2cpp_dateonly_hash(*dn2cpp_boxed<Dn2CppDateOnly>(o)); }
static int32_t dn2cpp_dateonly_box_equals(Dn2CppObject* a, Dn2CppObject* b)
{
    return (b->type == &dn2cpp_dateonly_type
            && dn2cpp_boxed<Dn2CppDateOnly>(a)->dayNumber
                   == dn2cpp_boxed<Dn2CppDateOnly>(b)->dayNumber) ? 1 : 0;
}
static Dn2CppString* dn2cpp_dateonly_box_formatspec(Dn2CppObject* o, Dn2CppString* spec,
                                                    const Dn2CppNumberFormatInfo*)
{ return dn2cpp_dateonly_format(*dn2cpp_boxed<Dn2CppDateOnly>(o), spec); }

static Dn2CppString* dn2cpp_timeonly_box_tostring(Dn2CppObject* o)
{ return dn2cpp_timeonly_to_string(*dn2cpp_boxed<Dn2CppTimeOnly>(o)); }
static int32_t dn2cpp_timeonly_box_hash(Dn2CppObject* o)
{ return dn2cpp_timeonly_hash(*dn2cpp_boxed<Dn2CppTimeOnly>(o)); }
static int32_t dn2cpp_timeonly_box_equals(Dn2CppObject* a, Dn2CppObject* b)
{
    return (b->type == &dn2cpp_timeonly_type
            && dn2cpp_boxed<Dn2CppTimeOnly>(a)->ticks == dn2cpp_boxed<Dn2CppTimeOnly>(b)->ticks) ? 1 : 0;
}
static Dn2CppString* dn2cpp_timeonly_box_formatspec(Dn2CppObject* o, Dn2CppString* spec,
                                                    const Dn2CppNumberFormatInfo*)
{ return dn2cpp_timeonly_format(*dn2cpp_boxed<Dn2CppTimeOnly>(o), spec); }

// Constant-initialized (dn2cpp_ti_with_typeobject is constexpr and a static
// function's address is a constant expression), so the mutual type/type_obj
// reference across this and dn2cpp_typeinfo.cpp needs no dynamic initializer and
// has no order dependency.
//
// DN2CPP_TF_NOT_MARSHALABLE on DateTime and DateTimeOffset, and on neither of the
// other four. These type-infos are hand-written, so the emitter's marshalling
// classifier never sees them and the flag has to be stated here: DateTime and
// DateTimeOffset carry TypeAttributes.AutoLayout, so Marshal.SizeOf raises
// ArgumentException for both, while TimeSpan (8), DateOnly (4), TimeOnly (8) and
// Decimal are sequential and answer at exactly the sizes these structs stamp as
// instanceSize. The flag is about AutoLayout, not width (see CppEmitter's
// s_intrinsicStructExtents).
//
// ---- The date/time handles' reflection field tables ----
//
// Runtime-OWNED handles carry only what is written here, so an absent table answers
// "this type has no fields" — a wrong answer that reads like a true one. These rows
// are real .NET's exact public field surface for the three types that have one;
// DateOnly and TimeOnly have none, so their absent tables are already right. The
// argument for hand-writing rather than binding emitted metadata is at
// dn2cpp_primflds_bool in dn2cpp_typeinfo.cpp.
#define DN2CPP_TSFLD_I8(nm, val) \
    static Dn2CppObject* dn2cpp_ownfld_timespan_##nm(Dn2CppObject*) \
    { int64_t v = (val); return dn2cpp_box(&dn2cpp_int64_type, &v, sizeof(v)); }
#define DN2CPP_TSFLD_I8_ROW(nm) \
    { #nm, &dn2cpp_timespan_type, &dn2cpp_int64_type, \
      DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL, \
      dn2cpp_ownfld_timespan_##nm, nullptr, nullptr, 0, 0x8056, 0 }
#define DN2CPP_TSFLD_TS(nm, ticksVal) \
    static Dn2CppObject* dn2cpp_ownfld_timespan_##nm(Dn2CppObject*) \
    { Dn2CppTimeSpan v = { (ticksVal) }; return dn2cpp_box(&dn2cpp_timespan_type, &v, sizeof(v)); }
#define DN2CPP_TSFLD_TS_ROW(nm) \
    { #nm, &dn2cpp_timespan_type, &dn2cpp_timespan_type, \
      DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY, \
      dn2cpp_ownfld_timespan_##nm, nullptr, nullptr, 0, 0x36, 0 }
DN2CPP_TSFLD_TS(Zero, 0)
DN2CPP_TSFLD_TS(MaxValue, 9223372036854775807LL)
DN2CPP_TSFLD_TS(MinValue, (-9223372036854775807LL - 1))
DN2CPP_TSFLD_I8(NanosecondsPerTick, 100)
DN2CPP_TSFLD_I8(TicksPerMicrosecond, 10)
DN2CPP_TSFLD_I8(TicksPerMillisecond, 10000)
DN2CPP_TSFLD_I8(TicksPerSecond, 10000000)
DN2CPP_TSFLD_I8(TicksPerMinute, 600000000)
DN2CPP_TSFLD_I8(TicksPerHour, 36000000000LL)
DN2CPP_TSFLD_I8(TicksPerDay, 864000000000LL)
DN2CPP_TSFLD_I8(MicrosecondsPerMillisecond, 1000)
DN2CPP_TSFLD_I8(MicrosecondsPerSecond, 1000000)
DN2CPP_TSFLD_I8(MicrosecondsPerMinute, 60000000)
DN2CPP_TSFLD_I8(MicrosecondsPerHour, 3600000000LL)
DN2CPP_TSFLD_I8(MicrosecondsPerDay, 86400000000LL)
DN2CPP_TSFLD_I8(MillisecondsPerSecond, 1000)
DN2CPP_TSFLD_I8(MillisecondsPerMinute, 60000)
DN2CPP_TSFLD_I8(MillisecondsPerHour, 3600000)
DN2CPP_TSFLD_I8(MillisecondsPerDay, 86400000)
DN2CPP_TSFLD_I8(SecondsPerMinute, 60)
DN2CPP_TSFLD_I8(SecondsPerHour, 3600)
DN2CPP_TSFLD_I8(SecondsPerDay, 86400)
DN2CPP_TSFLD_I8(MinutesPerHour, 60)
DN2CPP_TSFLD_I8(MinutesPerDay, 1440)
// The one Int32 among them, and the one row a copy-paste would get wrong.
static Dn2CppObject* dn2cpp_ownfld_timespan_HoursPerDay(Dn2CppObject*)
{ int32_t v = 24; return dn2cpp_box(&dn2cpp_int32_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_ownflds_timespan[] = {
    DN2CPP_TSFLD_TS_ROW(Zero), DN2CPP_TSFLD_TS_ROW(MaxValue), DN2CPP_TSFLD_TS_ROW(MinValue),
    DN2CPP_TSFLD_I8_ROW(NanosecondsPerTick),
    DN2CPP_TSFLD_I8_ROW(TicksPerMicrosecond), DN2CPP_TSFLD_I8_ROW(TicksPerMillisecond),
    DN2CPP_TSFLD_I8_ROW(TicksPerSecond), DN2CPP_TSFLD_I8_ROW(TicksPerMinute),
    DN2CPP_TSFLD_I8_ROW(TicksPerHour), DN2CPP_TSFLD_I8_ROW(TicksPerDay),
    DN2CPP_TSFLD_I8_ROW(MicrosecondsPerMillisecond), DN2CPP_TSFLD_I8_ROW(MicrosecondsPerSecond),
    DN2CPP_TSFLD_I8_ROW(MicrosecondsPerMinute), DN2CPP_TSFLD_I8_ROW(MicrosecondsPerHour),
    DN2CPP_TSFLD_I8_ROW(MicrosecondsPerDay),
    DN2CPP_TSFLD_I8_ROW(MillisecondsPerSecond), DN2CPP_TSFLD_I8_ROW(MillisecondsPerMinute),
    DN2CPP_TSFLD_I8_ROW(MillisecondsPerHour), DN2CPP_TSFLD_I8_ROW(MillisecondsPerDay),
    DN2CPP_TSFLD_I8_ROW(SecondsPerMinute), DN2CPP_TSFLD_I8_ROW(SecondsPerHour),
    DN2CPP_TSFLD_I8_ROW(SecondsPerDay),
    DN2CPP_TSFLD_I8_ROW(MinutesPerHour), DN2CPP_TSFLD_I8_ROW(MinutesPerDay),
    { "HoursPerDay", &dn2cpp_timespan_type, &dn2cpp_int32_type,
      DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_ownfld_timespan_HoursPerDay, nullptr, nullptr, 0, 0x8056, 0 },
};
#undef DN2CPP_TSFLD_I8
#undef DN2CPP_TSFLD_I8_ROW
#undef DN2CPP_TSFLD_TS
#undef DN2CPP_TSFLD_TS_ROW

#define DN2CPP_DTFLD(nm, ticksVal, kindVal) \
    static Dn2CppObject* dn2cpp_ownfld_datetime_##nm(Dn2CppObject*) \
    { Dn2CppDateTime v = dn2cpp_dt_word((ticksVal), (kindVal)); \
      return dn2cpp_box(&dn2cpp_datetime_type, &v, sizeof(v)); }
#define DN2CPP_DTFLD_ROW(nm) \
    { #nm, &dn2cpp_datetime_type, &dn2cpp_datetime_type, \
      DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY, \
      dn2cpp_ownfld_datetime_##nm, nullptr, nullptr, 0, 0x36, 0 }
DN2CPP_DTFLD(MinValue, 0, 0)
DN2CPP_DTFLD(MaxValue, DN2CPP_DT_MAX_TICKS, 0)
// UnixEpoch is the one of the three that is Utc-kinded, which its ToString shows.
DN2CPP_DTFLD(UnixEpoch, 621355968000000000LL, 1)
static const Dn2CppFieldInfo dn2cpp_ownflds_datetime[] = {
    DN2CPP_DTFLD_ROW(MinValue), DN2CPP_DTFLD_ROW(MaxValue), DN2CPP_DTFLD_ROW(UnixEpoch),
};
#undef DN2CPP_DTFLD
#undef DN2CPP_DTFLD_ROW

#define DN2CPP_DTOFLD(nm, ticksVal) \
    static Dn2CppObject* dn2cpp_ownfld_dto_##nm(Dn2CppObject*) \
    { Dn2CppDateTimeOffset v = { (ticksVal), 0 }; \
      return dn2cpp_box(&dn2cpp_datetimeoffset_type, &v, sizeof(v)); }
#define DN2CPP_DTOFLD_ROW(nm) \
    { #nm, &dn2cpp_datetimeoffset_type, &dn2cpp_datetimeoffset_type, \
      DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY, \
      dn2cpp_ownfld_dto_##nm, nullptr, nullptr, 0, 0x36, 0 }
DN2CPP_DTOFLD(MinValue, 0)
DN2CPP_DTOFLD(MaxValue, DN2CPP_DT_MAX_TICKS)
DN2CPP_DTOFLD(UnixEpoch, 621355968000000000LL)
static const Dn2CppFieldInfo dn2cpp_ownflds_dto[] = {
    DN2CPP_DTOFLD_ROW(MinValue), DN2CPP_DTOFLD_ROW(MaxValue), DN2CPP_DTOFLD_ROW(UnixEpoch),
};
#undef DN2CPP_DTOFLD
#undef DN2CPP_DTOFLD_ROW

extern const Dn2CppType dn2cpp_timespan_type_obj;
const Dn2CppTypeInfo dn2cpp_timespan_type = dn2cpp_ti_with_formatspec(
    dn2cpp_ti_with_typeobject({ "System.TimeSpan", nullptr, (int32_t)sizeof(Dn2CppTimeSpan), nullptr, nullptr, 0, &dn2cpp_timespan_box_tostring, &dn2cpp_timespan_box_hash, &dn2cpp_timespan_box_equals, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_SEALED), dn2cpp_ownflds_timespan, 25 }, &dn2cpp_timespan_type_obj),
    &dn2cpp_timespan_box_formatspec);
const Dn2CppType dn2cpp_timespan_type_obj = { { &dn2cpp_type_type }, &dn2cpp_timespan_type };
extern const Dn2CppType dn2cpp_datetime_type_obj;
const Dn2CppTypeInfo dn2cpp_datetime_type = dn2cpp_ti_with_formatspec(
    dn2cpp_ti_with_typeobject({ "System.DateTime", nullptr, (int32_t)sizeof(Dn2CppDateTime), nullptr, nullptr, 0, &dn2cpp_datetime_box_tostring, &dn2cpp_datetime_box_hash, &dn2cpp_datetime_box_equals, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_SEALED | DN2CPP_TF_NOT_MARSHALABLE), dn2cpp_ownflds_datetime, 3 }, &dn2cpp_datetime_type_obj),
    &dn2cpp_datetime_box_formatspec);
const Dn2CppType dn2cpp_datetime_type_obj = { { &dn2cpp_type_type }, &dn2cpp_datetime_type };
extern const Dn2CppType dn2cpp_datetimeoffset_type_obj;
const Dn2CppTypeInfo dn2cpp_datetimeoffset_type = dn2cpp_ti_with_formatspec(
    dn2cpp_ti_with_typeobject({ "System.DateTimeOffset", nullptr, (int32_t)sizeof(Dn2CppDateTimeOffset), nullptr, nullptr, 0, &dn2cpp_datetimeoffset_box_tostring, &dn2cpp_datetimeoffset_box_hash, &dn2cpp_datetimeoffset_box_equals, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_SEALED | DN2CPP_TF_NOT_MARSHALABLE), dn2cpp_ownflds_dto, 3 }, &dn2cpp_datetimeoffset_type_obj),
    &dn2cpp_datetimeoffset_box_formatspec);
const Dn2CppType dn2cpp_datetimeoffset_type_obj = { { &dn2cpp_type_type }, &dn2cpp_datetimeoffset_type };
extern const Dn2CppType dn2cpp_dateonly_type_obj;
const Dn2CppTypeInfo dn2cpp_dateonly_type = dn2cpp_ti_with_formatspec(
    dn2cpp_ti_with_typeobject({ "System.DateOnly", nullptr, (int32_t)sizeof(Dn2CppDateOnly), nullptr, nullptr, 0, &dn2cpp_dateonly_box_tostring, &dn2cpp_dateonly_box_hash, &dn2cpp_dateonly_box_equals, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_SEALED) }, &dn2cpp_dateonly_type_obj),
    &dn2cpp_dateonly_box_formatspec);
const Dn2CppType dn2cpp_dateonly_type_obj = { { &dn2cpp_type_type }, &dn2cpp_dateonly_type };
extern const Dn2CppType dn2cpp_timeonly_type_obj;
const Dn2CppTypeInfo dn2cpp_timeonly_type = dn2cpp_ti_with_formatspec(
    dn2cpp_ti_with_typeobject({ "System.TimeOnly", nullptr, (int32_t)sizeof(Dn2CppTimeOnly), nullptr, nullptr, 0, &dn2cpp_timeonly_box_tostring, &dn2cpp_timeonly_box_hash, &dn2cpp_timeonly_box_equals, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_SEALED) }, &dn2cpp_timeonly_type_obj),
    &dn2cpp_timeonly_box_formatspec);
const Dn2CppType dn2cpp_timeonly_type_obj = { { &dn2cpp_type_type }, &dn2cpp_timeonly_type };
