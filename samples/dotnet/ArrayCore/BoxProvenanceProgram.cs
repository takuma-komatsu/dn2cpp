#if BOX_PROVENANCE_ONLY
using System.Globalization;

namespace ArrayCoreBoxProvenance;

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        DynamicArrayNullEqualitySubset.Program.RunFrameworkBoxesOnly();
        if (args.Length != 0 && args[0] == "before-array-search-provenance-additions")
            return;
        DynamicArrayNullEqualitySubset.Program.RunFrameworkBoxAdditions();
    }
}
#endif
