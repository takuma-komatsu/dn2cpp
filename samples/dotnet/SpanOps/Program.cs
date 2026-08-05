using System;
using System.Globalization;

namespace SpanOps
{
    // Gate driver. Each section keeps its own namespace so reflected type names
    // stay identical to a standalone program.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            SpanBulkSubset.Program.__GateEntry();
            SpanScanSubset.Program.__GateEntry();
            SpanComparerSubset.Program.__GateEntry();
            SpanStructElementSubset.Program.__GateEntry();
            SpanSortSubset.Program.__GateEntry();
            SpanInstanceSubset.Program.__GateEntry();
            SpanIndexOfAnySubset.Program.__GateEntry();
            ReadOnlySpanByteSubset.Program.__GateEntry();
            CreateSpanSubset.Program.__GateEntry();
            MemorySpanSubset.Program.__GateEntry();
            MemoryMarshalMemorySubset.Program.__GateEntry();
            MemoryMarshalPinnedSubset.Program.__GateEntry();
            MemoryManagerSubset.Program.__GateEntry();
            MemoryMarshalSubset.Program.__GateEntry();
        }
    }
}
