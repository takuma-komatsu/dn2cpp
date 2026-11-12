using System;

namespace GcCollectionSubset;

internal static class Program
{
    private static byte[] s_keep;
    private static void Error(string label, Action action)
    {
        try { action(); Console.WriteLine(label + ":accepted"); }
        catch (Exception error)
        {
            Console.WriteLine(label + ":" + error.GetType().Name + ":"
                + (error is ArgumentException argument ? argument.ParamName : ""));
        }
    }
    internal static unsafe void Run()
    {
        Console.WriteLine("== GC collection overloads ==");
        s_keep = new byte[32]; s_keep[0] = 37;
        int maximum = GC.MaxGeneration;
        int before = GC.CollectionCount(0);
        Action zero = GC.Collect;
        Action<int> one = GC.Collect;
        Action<int, GCCollectionMode> two = GC.Collect;
        Action<int, GCCollectionMode, bool> three = GC.Collect;
        Action<int, GCCollectionMode, bool, bool> four = GC.Collect;
        delegate* managed<int, GCCollectionMode, bool, bool, void> pointer = &GC.Collect;
        zero(); one(0); two(0, GCCollectionMode.Forced);
        three(0, GCCollectionMode.Optimized, false);
        four(0, GCCollectionMode.Default, true, false);
        pointer(maximum, GCCollectionMode.Aggressive, true, true);
        GC.Collect(maximum, GCCollectionMode.Aggressive);
        Console.WriteLine("gc-collect-routes:True");
        Console.WriteLine("gc-collect-counter-advanced:" + (GC.CollectionCount(0) > before));
        Console.WriteLine("gc-collect-live-payload:" + (s_keep[0] == 37));
        Error("gc-collect-negative", () => one(-1));
        Error("gc-collect-negative-first", () => two(-1, (GCCollectionMode)99));
        Error("gc-collect-mode-low", () => two(0, (GCCollectionMode)(-1)));
        Error("gc-collect-mode-high", () => two(0, (GCCollectionMode)99));
        Error("gc-collect-aggressive-generation", () => four(maximum + 1, GCCollectionMode.Aggressive, true, true));
        Error("gc-collect-aggressive-blocking", () => four(maximum, GCCollectionMode.Aggressive, false, true));
        Error("gc-collect-aggressive-compacting", () => four(maximum, GCCollectionMode.Aggressive, true, false));
        GC.KeepAlive(s_keep);
        Console.WriteLine("GC collection overloads end");
    }
}
