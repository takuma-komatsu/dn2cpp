using System;
using System.Globalization;

namespace DefaultPrincipalSubset;

internal static class Program
{
    // The native-only fixture points this stub at CoreLib's default-principal accessor.
    private static object Direct(object marker) => throw new InvalidOperationException("native fixture required");

    internal static void RunArtifactChecks(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("default-principal:" + Direct(null).GetType().FullName);
    }
}
