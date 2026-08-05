using System;
using System.Globalization;

namespace FlatMemEdges
{
    // Auto-merged gate driver: runs each consolidated sample's __GateEntry() in
    // order. Each section keeps its own namespace.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            ReinterpretOffsetSubset.Program.__GateEntry();
            PtrArithEdgesSubset.Program.__GateEntry();
            StructFlatBufferSubset.Program.__GateEntry();
            StructValueFieldSubset.Program.__GateEntry();
            SubWordStructLayoutSubset.Program.__GateEntry();
            SubWordClassStaticLayoutSubset.Program.__GateEntry();
        }
    }
}
