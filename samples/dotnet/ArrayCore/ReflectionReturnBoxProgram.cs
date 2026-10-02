using System.Globalization;

namespace ArrayCoreReflectionReturnBoxes;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        DynamicArrayNullEqualitySubset.Program.RunReflectionReturnBoxesOnly();
    }
}
