#include "dn2cpp_windows_timezone.h"

#include <windows.h>

bool dn2cpp_windows_zone_rule(int year, Dn2CppWindowsZoneRule* out)
{
    DYNAMIC_TIME_ZONE_INFORMATION dynamic{};
    if (::GetDynamicTimeZoneInformation(&dynamic) == TIME_ZONE_ID_INVALID)
        return false;
    TIME_ZONE_INFORMATION rule{};
    if (!::GetTimeZoneInformationForYear(static_cast<USHORT>(year), &dynamic, &rule))
        return false;
    out->standardMinutes = -(rule.Bias + rule.StandardBias);
    out->daylightMinutes = -(rule.Bias + rule.DaylightBias);
    auto transition = [](const SYSTEMTIME& value) {
        return Dn2CppWindowsZoneTransition{value.wMonth, value.wYear == 0 ? value.wDay : 0,
            value.wDayOfWeek, value.wDay, value.wHour, value.wMinute,
            value.wSecond, value.wMilliseconds};
    };
    out->daylightStart = transition(rule.DaylightDate);
    out->standardStart = transition(rule.StandardDate);
    if (dynamic.DynamicDaylightTimeDisabled)
        out->daylightStart.month = out->standardStart.month = 0;
    return true;
}
