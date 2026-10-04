using System;

namespace DateTimeDaylightSubset;

internal static class Program
{
    internal static void Run()
    {
        Console.WriteLine("== daylight saving time ==");
        foreach (DateTimeKind kind in new[] { DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local })
        {
            Show(new DateTime(2024, 1, 1, 12, 0, 0, kind));
            Show(new DateTime(2024, 7, 1, 12, 0, 0, kind));
            Show(new DateTime(2024, 3, 10, 2, 30, 0, kind));
            Show(new DateTime(2024, 11, 3, 1, 30, 0, kind));
            Show(new DateTime(2024, 4, 7, 2, 30, 0, kind));
            Show(new DateTime(2024, 10, 6, 2, 30, 0, kind));
            Show(new DateTime(1800, 1, 1, 12, 0, 0, kind));
            Show(DateTime.SpecifyKind(DateTime.MinValue, kind));
            Show(DateTime.SpecifyKind(DateTime.MaxValue, kind));
        }
        // The two UTC instants share a local clock in a fall-back overlap.
        foreach (DateTime utc in new[]
        {
            new DateTime(2024, 11, 3, 5, 30, 0, DateTimeKind.Utc),
            new DateTime(2024, 11, 3, 6, 30, 0, DateTimeKind.Utc),
            new DateTime(2024, 4, 6, 15, 30, 0, DateTimeKind.Utc),
            new DateTime(2024, 4, 6, 16, 30, 0, DateTimeKind.Utc),
        })
        {
            DateTime local = utc.ToLocalTime();
            Show(local);
            Show(DateTime.FromBinary(local.ToBinary()));
            Show(local.AddTicks(0));
            Show(local.AddMonths(0));
            Show(local.Date.AddTicks(local.TimeOfDay.Ticks));
            Console.WriteLine("utc round trip: " + (local.ToUniversalTime() == utc));
        }
        DateTime daylight = new DateTime(2024, 7, 1, 12, 0, 0, DateTimeKind.Local);
        Func<bool> predicate = daylight.IsDaylightSavingTime;
        Console.WriteLine("delegate daylight: " + predicate());
        Console.WriteLine("daylight saving time end");
    }

    private static void Show(DateTime value)
    {
        Console.WriteLine(value.Ticks + ": " + value.Kind + ": " + value.IsDaylightSavingTime());
        Console.WriteLine("conversions: " + value.ToLocalTime().Ticks + ":" + value.ToUniversalTime().Ticks);
        long binary = value.ToBinary();
        Console.WriteLine("binary: " + binary + ":" + DateTime.FromBinary(binary).Ticks);
    }
}
