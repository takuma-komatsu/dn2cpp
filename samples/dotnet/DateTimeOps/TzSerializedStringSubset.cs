using System;

// TimeZoneInfo.FromSerializedString over a fixed serialized custom zone with
// adjustment rules. Deserializing a transition time routes through the internal
// TimeOnly.ToDateTime() (StringSerializer.GetNextTransitionTimeValue), the one
// TimeOnly member only this path reaches. Prints the string's own facts plus
// offsets in and out of the daylight window, so it is clock-invariant. CoreLib only.
namespace TzSerializedString;

static class Program
{
    const string Serialized =
        "Dn2CppZone;120;Dn2Cpp Zone;Dn2Cpp Standard;Dn2Cpp Daylight;"
        + "[01:01:0001;12:31:9999;60;[1;03:30:00;3;15;];[0;02:00:00;10;2;0;];];";

    internal static void __GateEntry()
    {
        Console.WriteLine("-- TimeZoneInfo.FromSerializedString (adjustment rules) --");
        var tz = TimeZoneInfo.FromSerializedString(Serialized);
        Console.WriteLine("Id=" + tz.Id);
        Console.WriteLine("BaseUtcOffset=" + tz.BaseUtcOffset);
        Console.WriteLine("SupportsDst=" + tz.SupportsDaylightSavingTime);
        Console.WriteLine("StandardName=" + tz.StandardName);
        Console.WriteLine("offJul=" + tz.GetUtcOffset(new DateTime(2024, 7, 1, 12, 0, 0)));
        Console.WriteLine("offJan=" + tz.GetUtcOffset(new DateTime(2024, 1, 10, 12, 0, 0)));
        Console.WriteLine("dstMar20=" + tz.IsDaylightSavingTime(new DateTime(2024, 3, 20, 12, 0, 0)));
        Console.WriteLine("dstMar10=" + tz.IsDaylightSavingTime(new DateTime(2024, 3, 10, 12, 0, 0)));
        Console.WriteLine("dstNov=" + tz.IsDaylightSavingTime(new DateTime(2024, 11, 20, 12, 0, 0)));
    }
}
