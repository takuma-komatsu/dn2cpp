using System.Globalization;

namespace ArrayCoreDiamondProvenance;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        DynamicArrayNullEqualitySubset.Program.RunDiamondOnly();
    }
}
