using System.Globalization;

namespace ArrayCoreFieldAliasProvenance;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        FieldAliasProvenanceSubset.Program.Run();
    }
}
