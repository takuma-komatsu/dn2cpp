using System;

namespace GcGenerationSubset;

internal static class Program
{
    private static object s_keep;
    private static bool InRange(int generation) => generation >= 0 && generation <= GC.MaxGeneration;
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
        Console.WriteLine("== GC logical generations ==");
        s_keep = new byte[8];
        Func<object, int> query = GC.GetGeneration;
        delegate* managed<object, int> pointer = &GC.GetGeneration;
        var weak = new WeakReference(s_keep);
        // Boehm has one logical generation; CLR can promote. Bounds and public
        // validation are shared contracts, not equality of raw generation values.
        Console.WriteLine("gc-generation-max-valid:" + (GC.MaxGeneration >= 0));
        Console.WriteLine("gc-generation-direct:" + InRange(GC.GetGeneration(s_keep)));
        Console.WriteLine("gc-generation-delegate:" + InRange(query(s_keep)));
        Console.WriteLine("gc-generation-pointer:" + InRange(pointer(s_keep)));
        Console.WriteLine("gc-generation-weak:" + InRange(GC.GetGeneration(weak)));
        GC.Collect();
        Console.WriteLine("gc-generation-collected:" + InRange(query(s_keep)));
        Console.WriteLine("gc-generation-weak-collected:" + InRange(GC.GetGeneration(weak)));
        Error("gc-generation-null", () => GC.GetGeneration((object)null));
        Error("gc-generation-delegate-null", () => query(null));
        Error("gc-generation-null-weak", () => GC.GetGeneration((WeakReference)null));
        Error("gc-generation-empty-weak", () => GC.GetGeneration(new WeakReference(null)));
        GC.KeepAlive(s_keep);
        Console.WriteLine("GC logical generations end");
    }
}
