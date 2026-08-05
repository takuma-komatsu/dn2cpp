using System;

namespace WeakReferences
{
    // Gate driver: runs each section's __GateEntry() in order.
    internal static class Program
    {
        private static void Main()
        {
            WeakReferenceBasicSubset.Program.__GateEntry();
            WeakReferenceLongSubset.Program.__GateEntry();
            WeakReferenceMemorySubset.Program.__GateEntry();
            WeakReferenceSweepSubset.Program.__GateEntry();
            WeakReferenceHeapHeldLongSubset.Program.__GateEntry();
        }
    }
}
