using System;
using System.Globalization;

namespace TaskRunUnwrap
{
    // Gate driver: each section keeps its own namespace so namespace-sensitive output
    // matches the standalone form. Every Task is awaited, so the output is deterministic.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            UnwrapResult.Program.__GateEntry();
            UnwrapVoid.Program.__GateEntry();
            UnwrapThrow.Program.__GateEntry();
            UnwrapNested.Program.__GateEntry();
            NonUnwrapMix.Program.__GateEntry();
        }
    }
}
