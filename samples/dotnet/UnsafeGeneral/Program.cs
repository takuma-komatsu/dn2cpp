using System;
using System.Globalization;

namespace UnsafeGeneral
{
    // Gate driver: runs each section's __GateEntry() in order.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            UnsafeAsSubset.Program.__GateEntry();
            UnsafeArithSubset.Program.__GateEntry();
            UnsafeRwSubset.Program.__GateEntry();
            UnsafeBlockSubset.Program.__GateEntry();
            MarshalCastSubset.Program.__GateEntry();
            MarshalShapeSubset.Program.__GateEntry();
            MarshalRwSubset.Program.__GateEntry();
            BackingsSubset.Program.__GateEntry();
            ExplicitLayoutSubset.Program.__GateEntry();
            UnsafeUnboxSubset.Program.__GateEntry();
            SafeHandleSubset.Program.__GateEntry();
            UnreachedStructSubset.Program.__GateEntry();
        }
    }
}
