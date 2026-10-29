using System;

namespace Gen2CallbackPatch;

internal static class Program
{
    private static void Create0() => throw new InvalidOperationException("native fixture required");
#if GEN2_OBJECT_ARGUMENT
    private static void Create1() => throw new InvalidOperationException("native fixture required");
#endif

    private static void Main()
    {
        // Culture is pinned by the native host before patch execution.
        bool first = false;
        try { Create0(); }
        catch (PlatformNotSupportedException error)
        {
            first = Gen2CallbackInterp.BoundaryAssertions.IsAllocationMessage(error.Message);
        }
        Console.WriteLine(first ? "gen2-interp-ctor0:True" : "gen2-interp-ctor0:False");
        if (!first) throw new InvalidOperationException("constructor zero did not reach allocation fence");
#if GEN2_OBJECT_ARGUMENT
        bool second = false;
        try { Create1(); }
        catch (PlatformNotSupportedException error)
        {
            second = Gen2CallbackInterp.BoundaryAssertions.IsAllocationMessage(error.Message);
        }
        Console.WriteLine(second ? "gen2-interp-ctor1:True" : "gen2-interp-ctor1:False");
        if (!second) throw new InvalidOperationException("constructor one did not reach allocation fence");
#endif
        var ordinary = new Gen2CallbackInterp.OrdinaryImported();
        Console.WriteLine(ordinary.Value == 7 ? "gen2-interp-ordinary:True" : "gen2-interp-ordinary:False");
        if (ordinary.Value != 7) throw new InvalidOperationException("ordinary imported constructor did not run");
        Console.WriteLine("gen2-interp-end");
    }
}
