using System;
using System.Globalization;

namespace ConsoleIo
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

            ConsoleWriteSubset.Program.__GateEntry();
            ConsoleFormatSubset.Program.__GateEntry();
            PathSubset.Program.__GateEntry();
            PathSpanSubset.Program.__GateEntry();
            CultureSubset.Program.__GateEntry();
            PathThrowSubset.Program.__GateEntry();
        }
    }
}
