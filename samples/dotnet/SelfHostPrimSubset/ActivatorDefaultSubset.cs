using System;
using System.Globalization;
using System.Reflection;

namespace ActivatorDefaultSubset;

internal static class ActivatorChecks
{
    private struct Plain { public int Value; }
    private static void Check(string label, bool success)
    {
        Console.WriteLine("activator-default-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }
    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Activator default construction ==");
        Check("public-one-arg-nullable", Activator.CreateInstance(typeof(int?)) is null);
        Check("public-two-arg-nullable", Activator.CreateInstance(typeof(Plain?), true) is null);
        Console.WriteLine("Activator default construction end");
    }
}
