using System;

namespace HotPath
{
    // Auto-merged gate driver: runs each consolidated sample's __GateEntry() in
    // order. Each section keeps its own namespace so namespace-sensitive output
    // stays identical to a standalone build.
    internal static class Program
    {
        private static void Main()
        {
            HotPathBasicSubset.Program.__GateEntry();
            HotPathGenericSubset.Program.__GateEntry();
            HotPathMixedSubset.Program.__GateEntry();
            HotPathInlineSubset.Program.__GateEntry();
            HotPathBoundsSubset.Program.__GateEntry();
            HotPathNoAllocSubset.Program.__GateEntry();
            HotPathFastMathSubset.Program.__GateEntry();
            HotPathNoAliasSubset.Program.__GateEntry();
        }
    }
}
