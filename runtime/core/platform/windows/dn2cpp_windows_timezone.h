#pragma once

// Windows zone rules cover DateTime's full range, unlike the CRT time functions.
struct Dn2CppWindowsZoneTransition
{
    int month, week, dayOfWeek, day, hour, minute, second, millisecond;
};

struct Dn2CppWindowsZoneRule
{
    int standardMinutes, daylightMinutes;
    Dn2CppWindowsZoneTransition daylightStart, standardStart;
};

bool dn2cpp_windows_zone_rule(int year, Dn2CppWindowsZoneRule* out);
