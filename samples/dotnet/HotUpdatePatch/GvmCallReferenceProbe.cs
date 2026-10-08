using System;
using System.Globalization;
using Dn2Cpp.Runtime;
using HotUpdateBase;

namespace HotGvmCall;

#if REFERENCE_BASE
public static class CultureControl
{
    public static void PinInvariantCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
}

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureControl.PinInvariantCulture();

        Console.WriteLine("reference base start");
        Console.WriteLine(new GlassBin().Name());
        if (args.Length > 0 && args[0] == "--prefix")
            return;
        HotUpdate.Run(args[0]);
    }
}
#else
internal static class NonVirtual
{
    internal static string Cold(IOverriddenBin bin)
    {
        return bin.Cold();
    }
}

internal static class Program
{
    private static void Main()
    {
        CultureControl.PinInvariantCulture();

        Console.WriteLine("== referenced default interface body ==");
        IOverriddenBin overridden = new GlassBin();
        Console.WriteLine("library call=" + NonVirtual.Cold(overridden));
        Console.WriteLine("library null call=" + NonVirtual.Cold(null!));
        Console.WriteLine("library callvirt=" + overridden.Cold());
        Console.WriteLine("referenced default interface body end");
    }
}
#endif
