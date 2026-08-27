using System;
using System.Globalization;

namespace MarshalPinning
{
    // Auto-merged gate driver: runs each consolidated sample's __GateEntry() in
    // order. Each section keeps its own namespace.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            PinnedArraySubset.Program.__GateEntry();
            StructMarshalRoundtripSubset.Program.__GateEntry();
            SizeOfOffsetOfSubset.Program.__GateEntry();
            MarshalCopySubset.Program.__GateEntry();
            ConservativeGcPinSubset.Program.__GateEntry();
            NativeMemPrimitivesSubset.Program.__GateEntry();
            NativeMemoryBulkSubset.Program.__GateEntry();
            MarshalStringNativeSubset.Program.__GateEntry();
            NativeMemorySubset.Program.__GateEntry();
            StructMarshalSubset.Program.__GateEntry();
            StructMarshalOffsetOfSubset.Program.__GateEntry();
            AlignedAllocSubset.Program.__GateEntry();   // was aligned-alloc-subset
            MarshalNullFaultSubset.Program.__GateEntry();
            MarshalHResultSubset.Program.__GateEntry();
            NativeMemoryOverflowSubset.Program.__GateEntry();
            // GCHandlePinSubset stays LAST: its sections 13 and 17 assert
            // aggregate heap facts and storm the allocator, so nothing may
            // inherit their churn. See the header of GCHandlePinSubset.cs.
            GCHandlePinSubset.Program.__GateEntry();    // was gchandle-pin-subset
        }
    }
}
