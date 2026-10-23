using System.Globalization;

namespace ArrayCoreFiniteSearch;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        ArrayFiniteSearchSubset.Program.Run();
    }
}
