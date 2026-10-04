using System;
using System.Globalization;

namespace ConsoleIo
{
    // Auto-merged gate driver: runs each consolidated sample's Run() in
    // order. Each section keeps its own namespace so reflected type names
    // and other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            if (args.Length > 0 && args[0] == "standard-pipe")
            {
                ConsoleStandardStreamSubset.Program.WritePipe();
                return;
            }
            if (args.Length > 0 && args[0] == "standard-input")
            {
                ConsoleStandardStreamSubset.Program.ReadInput();
                return;
            }

            ConsoleWriteSubset.Program.__GateEntry();
            ConsoleFormatSubset.Program.__GateEntry();
            PathSubset.Program.__GateEntry();
            PathSpanSubset.Program.__GateEntry();
            CultureSubset.Program.__GateEntry();
            PathThrowSubset.Program.__GateEntry();
            if (args.Length > 0 && args[0] == "before-standard-streams")
                return;
            ConsoleStandardStreamSubset.Program.Run();
        }
    }
}
