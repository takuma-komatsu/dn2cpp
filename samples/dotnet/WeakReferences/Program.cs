using System;
using System.Globalization;

namespace WeakReferences
{
    // Gate driver: runs each section's __GateEntry() in order.
    internal static class Program
    {
        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            WeakReferenceBasicSubset.Program.__GateEntry();
            WeakReferenceLongSubset.Program.__GateEntry();
            WeakReferenceMemorySubset.Program.__GateEntry();
            WeakReferenceSweepSubset.Program.__GateEntry();
            WeakReferenceHeapHeldLongSubset.Program.__GateEntry();
            IncrementalWriteBarrierSubset.Program.__GateEntry();
            InternBarrierSubset.Program.__GateEntry();
            PendingCallArgumentSubset.Program.__GateEntry();
            IncrementalWriteBarrierSubset.Program.__ResizeGateEntry();
        }
    }
}
