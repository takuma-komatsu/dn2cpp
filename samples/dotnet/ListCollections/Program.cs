using System;
using System.Globalization;

namespace ListCollections
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

            ListSubset.Program.__GateEntry();
            ListContainsElementsSubset.Program.__GateEntry();
            ListEnumSubset.Program.__GateEntry();
            ListSortSubset.Program.__GateEntry();
            ListSortComparerSubset.Program.__GateEntry();
            ListSortComparisonSubset.Program.__GateEntry();
            ListSortStringSubset.Program.__GateEntry();
            ComparerDefaultSubset.Program.__GateEntry();
            CustomComparerSubset.Program.__GateEntry();
        }
    }
}
