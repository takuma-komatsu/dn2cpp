using System;

namespace GcMemoryQuerySubset;

internal static class Program
{
    private static byte[] s_keep;

    internal static unsafe void Run()
    {
        Console.WriteLine("== GC memory query entry points ==");
        Func<bool, long> publicQuery = GC.GetTotalMemory;
        delegate* managed<bool, long> pointerQuery = &GC.GetTotalMemory;
        s_keep = new byte[1 << 20];
        s_keep[0] = 17;
        s_keep[s_keep.Length - 1] = 23;
        // Heap accounting differs between CLR, Boehm and the calloc fallback.
        // Compare the shared query contract, not raw byte counts.
        Console.WriteLine("gc-public-direct-nonnegative:" + (GC.GetTotalMemory(false) >= 0));
        Console.WriteLine("gc-public-delegate-nonnegative:" + (publicQuery(false) >= 0));
        Console.WriteLine("gc-public-pointer-nonnegative:" + (pointerQuery(false) >= 0));
        Console.WriteLine("gc-public-collect-nonnegative:" + (publicQuery(true) >= 0));
        Console.WriteLine("gc-live-payload:" + (s_keep[0] == 17 && s_keep[s_keep.Length - 1] == 23));
        GC.KeepAlive(s_keep);
        Console.WriteLine("GC memory query entry points end");
    }
}
