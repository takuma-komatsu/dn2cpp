using System;
using System.Globalization;

namespace HotPath
{
    // Auto-merged gate driver: runs each consolidated sample's __GateEntry() in
    // order. Each section keeps its own namespace so namespace-sensitive output
    // stays identical to a standalone build.
    internal static class Program
    {
        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            HotPathBasicSubset.Program.__GateEntry();
            HotPathGenericSubset.Program.__GateEntry();
            HotPathMixedSubset.Program.__GateEntry();
            HotPathInlineSubset.Program.__GateEntry();
            HotPathBoundsSubset.Program.__GateEntry();
            HotPathNoAllocSubset.Program.__GateEntry();
            HotPathFastMathSubset.Program.__GateEntry();
            HotPathNoAliasSubset.Program.__GateEntry();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_REFLECTION_NOALLOC") == "1")
                return;
            HotPathNoAllocSubset.Program.__GateReflectionEntry();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_COVARIANT_STORES") == "1")
                return;
            HotPathBoundsSubset.Program.__GateCovariantEntry();
        }
    }
}
