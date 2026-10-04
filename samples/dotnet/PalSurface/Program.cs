using System.Globalization;

namespace PalSurface
{
    // Gate driver: each section keeps its own namespace so namespace-sensitive output
    // matches a standalone build.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            NativeHeapPalSubset.Program.__GateEntry();
            StderrWritePalSubset.Program.__GateEntry();
            NetworkInterfacePalSubset.Program.__GateEntry();
            FileStreamPalSubset.Program.__GateEntry();
            if (args.Length > 0 && args[0] == "before-temp-file")
                return;
            FileStreamPalSubset.Program.RunTempFile();
        }
    }
}
