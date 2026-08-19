using System.Globalization;

namespace PalSurface
{
    // Gate driver: each section keeps its own namespace so namespace-sensitive output
    // matches a standalone build.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            NativeHeapPalSubset.Program.__GateEntry();
            StderrWritePalSubset.Program.__GateEntry();
        }
    }
}
