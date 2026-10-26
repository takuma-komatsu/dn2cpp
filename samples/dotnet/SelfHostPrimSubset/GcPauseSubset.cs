using System;

namespace GcPauseSubset;

internal static class Program
{
    internal static unsafe void Run()
    {
        Console.WriteLine("== GC pause duration ==");
        // Keep enough live roots that coarse-clock targets still observe progress.
        var roots = new object[8192];
        for (int i = 0; i < roots.Length; i++) roots[i] = new byte[256];
        TimeSpan before = GC.GetTotalPauseDuration();
        for (int i = 0; i < 8; i++) GC.Collect();
        TimeSpan after = GC.GetTotalPauseDuration();
        Console.WriteLine("pause-nonnegative:" + (before.Ticks >= 0));
        Console.WriteLine("pause-monotonic:" + (after >= before));
        Console.WriteLine("pause-advanced:" + (after > before || GC.CollectionCount(0) == 0));
        Func<TimeSpan> read = GC.GetTotalPauseDuration;
        Console.WriteLine("pause-delegate:" + (read() >= after));
        delegate* managed<TimeSpan> pointer = &GC.GetTotalPauseDuration;
        Console.WriteLine("pause-pointer:" + (pointer() >= after));
        GC.KeepAlive(roots);
        Console.WriteLine("GC pause duration end");
    }
}
