using System;

namespace GuidOps
{
    // Auto-merged gate driver: runs each consolidated sample's __GateEntry()
    // in order. Each section keeps its own namespace so namespace-sensitive
    // output stays identical to the originals.
    internal static class Program
    {
        private static void Main()
        {
            GuidNewGuidSubset.Program.__GateEntry();
            GuidParseSubset.Program.__GateEntry();
            GuidFormatSubset.Program.__GateEntry();
            GuidBytesCtorSubset.Program.__GateEntry();
            GuidCompareSubset.Program.__GateEntry();
        }
    }
}
