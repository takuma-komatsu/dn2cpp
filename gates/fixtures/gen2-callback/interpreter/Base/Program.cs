using System;
using System.Globalization;
using Dn2Cpp.Runtime;

namespace Gen2CallbackInterp;

internal static class Host
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Gen2CallbackSubset.Program.RunReflectionChecks();
        Console.WriteLine("gen2-interp-base-ready");
        if (args.Length == 0) return;
        try { HotUpdate.Run(args[0]); }
        catch (Exception error)
        {
            Console.WriteLine("gen2-interp-host-rejected:" + error.Message);
        }
    }
}

public sealed class OrdinaryImported
{
    public int Value;
    public OrdinaryImported() { Value = 7; }
    ~OrdinaryImported() { }
}

public static class BoundaryAssertions
{
    public static bool IsAllocationMessage(string message) => message == "Allocation of this runtime type is not supported";
}
