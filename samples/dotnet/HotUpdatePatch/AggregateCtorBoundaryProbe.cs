using System;
using HotUpdateBase;

namespace HotGvmRow;

// The same caller binds an aggregate or an ordinary ctor whose row is made unavailable.
public static class RowProbe
{
    public static void Run()
    {
#if AGGREGATE
        var value = new UnavailableAggregateProbe("seed", new[] { new Exception("inner") });
        Console.WriteLine("aggregate ctor=" + value.Message);
#else
        var value = new UnavailableOrdinaryProbe("seed");
        Console.WriteLine("ordinary ctor=" + value.Message + "/" + DerivedAggregateSubset.Fields(value.Code, "field"));
#endif
        Console.WriteLine("unavailable ctor fixture end");
    }
}

internal static class Program
{
    private static void Main() => RowProbe.Run();
}
