using System;
using System.Globalization;

namespace WeakReferences
{
    // Gate driver: runs each section's __GateEntry() in order.
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length > 0 && args[0] == "local-call")
            {
                LocalCallArgumentSubset.Program.__GateEntry();
                return;
            }

            WeakReferenceBasicSubset.Program.__GateEntry();
            WeakReferenceLongSubset.Program.__GateEntry();
            WeakReferenceMemorySubset.Program.__GateEntry();
            WeakReferenceSweepSubset.Program.__GateEntry();
            WeakReferenceHeapHeldLongSubset.Program.__GateEntry();
            IncrementalWriteBarrierSubset.Program.__GateEntry();
            InternBarrierSubset.Program.__GateEntry();
            PendingCallArgumentSubset.Program.__GateEntry();
            IncrementalWriteBarrierSubset.Program.__ResizeGateEntry();
            ConditionalWeakTableSubset.Program.__GateEntry();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_IMMORTAL_WEAK") == "1")
                return;
            WeakReferenceImmortalSubset.Program.__GateEntry();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_LOCAL_CALL") == "1")
                return;
            LocalCallArgumentSubset.Program.__GateEntry();
        }
    }
}
