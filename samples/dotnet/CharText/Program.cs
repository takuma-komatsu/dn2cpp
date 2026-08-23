using System;
using System.Globalization;

namespace CharText
{
    // Auto-merged gate driver: runs each consolidated sample's Run() in
    // order. Each section keeps its own namespace so reflected type names
    // and other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            CharSubset.Program.__GateEntry();
            CharCasingInvariantSubset.Program.__GateEntry();
            CharClassifySubset.Program.__GateEntry();
            CharRestSubset.Program.__GateEntry();
            CharConcatSubset.Program.__GateEntry();
            CharIsSurrogateSubset.Program.__GateEntry();
            SubwordToStringSubset.Program.__GateEntry();
            ToStringSubset.Program.__GateEntry();
            CultureInvariantOverloadsSubset.Program.__GateEntry();
            EncodingGetStringSubset.Program.__GateEntry();
            CharStringClassifySubset.Program.__GateEntry();
        }
    }
}
