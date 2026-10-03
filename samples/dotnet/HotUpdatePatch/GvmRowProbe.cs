using System;
using HotUpdateBase;

namespace HotGvmRow;

// Baked twice. GvmRowHitPatch binds Crate.Tag<int>, which the base image
// reaches; GvmRowMissPatch binds Crate.Tag<long>, which it never reaches, so
// the loader refuses that image whole.
public static class RowProbe
{
    public static string Run()
    {
#if MISS
        return new Crate().Tag<long>(5L);
#else
        return new Crate().Tag<int>(5);
#endif
    }
}

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine(RowProbe.Run());
    }
}
