using System;
using System.Globalization;

namespace DictCollections
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
            EqualityComparerDefaultSubset.Program.__GateEntry();
            RuntimeTypeHandleKeySubset.Program.__GateEntry();
            if (args.Length > 0 && args[0] == "before-default-comparison")
                return;
            Console.WriteLine("== default comparison validation ==");
            DefaultEqualityMouthsSubset.Program.__GateEntry();
            Console.WriteLine("default comparison validation end");
            if (args.Length > 0 && args[0] == "before-nullable-equality")
                return;
            NullableEqualitySubset.Program.__GateEntry();
        }
    }
}
