using System;
using System.Globalization;

namespace AsyncCore
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

            AsyncSubset.Program.__GateEntry();
            AsyncSuspend.Program.__GateEntry();
            AsyncYield.Program.__GateEntry();
            AsyncCatchSubset.Program.__GateEntry();
            AsyncStreamsSubset.Program.__GateEntry();
            ValueTaskSubset.Program.__GateEntry();
            StructTaskSubset.Program.__GateEntry();
            TaskStateSubset.Program.__GateEntry();
            ColdTaskSubset.Program.__GateEntry();
            ColdTaskDeadlockSubset.Program.__GateEntry();
            TcsSubset.Program.__GateEntry();
            ContinueWithSubset.Program.__GateEntry();
            AsyncPendingRoot.Program.__GateEntry();
            ThreadAwaitTeardown.Program.__GateEntry();
            TaskInspectSubset.Program.__GateEntry();
            TaskSchedulerSyncContextSubset.Program.__GateEntry();
            ValueTaskSourceStructSubset.Program.__GateEntry();
            StructTaskReferenceBarrierSubset.Program.__GateEntry();
            // APPENDED LAST (the prove-it-ran prefix rule, AGENTS.md).
            AsyncIntrinsicToStringSubset.Program.__GateEntry();
        }
    }
}
