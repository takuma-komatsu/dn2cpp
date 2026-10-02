using System.Globalization;

namespace ArrayCoreSearchLoops;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        ArraySearchLoopSubset.Program.Run();
    }
}
