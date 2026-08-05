using System;
using System.Globalization;

namespace DictCollections
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

            DictSubset.Program.__GateEntry();
            DictApiSubset.Program.__GateEntry();
            DictEnumSubset.Program.__GateEntry();
            DictMoreSubset.Program.__GateEntry();
            DictStringSubset.Program.__GateEntry();
            HashSetSubset.Program.__GateEntry();
            HashKeySubset.Program.__GateEntry();
            ValueStructKeySubset.Program.__GateEntry();
            CustomComparerSubset.Program.__GateEntry();
            TupleStructuralSubset.Program.__GateEntry();
            SortedCollectionsSubset.Program.__GateEntry();
            NonGenericSortedListSubset.Program.__GateEntry();
            FrozenDictSubset.Program.__GateEntry();
            JoinEnumerableSubset.Program.__GateEntry();
            OrdinalStringSetDedupSubset.Program.__GateEntry();
            StructKeySubset.Program.__GateEntry();
            ValueTupleKeyDictSubset.Program.__GateEntry();
        }
    }
}
