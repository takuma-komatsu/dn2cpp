using System;
using System.Globalization;

namespace StringInterp
{
    // Gate driver. Each section keeps its own namespace so reflected type names
    // stay identical to the standalone samples.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            ConcatSubset.Program.__GateEntry();
            ConcatValueSubset.Program.__GateEntry();
            ConcatJoinListSubset.Program.__GateEntry();
            InterpSubset.Program.__GateEntry();
            InterpHandlerSubset.Program.__GateEntry();
            InterpolationNewobjSubset.Program.__GateEntry();
            AppendFormatSubset.Program.__GateEntry();
            CustomFormatSubset.Program.__GateEntry();
        }
    }
}
