using System;
using System.Globalization;

namespace ExternalRefBarrier
{
    // Gate driver: runs each section's __GateEntry() in order.
    internal static class Program
    {
        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            ExternalWriteBarrierSubset.Program.__GateEntry();
        }
    }
}
