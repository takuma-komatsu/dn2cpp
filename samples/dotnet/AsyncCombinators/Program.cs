using System;
using System.Globalization;

namespace AsyncCombinators
{
    // Auto-merged gate driver: runs each consolidated sample's Run() in
    // order. Each section keeps its own namespace so reflected type names
    // and other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            WhenAllSubset.Program.__GateEntry();
            WhenAnySubset.Program.__GateEntry();
            WhenEnumerableSubset.Program.__GateEntry();
            ConfigureAwaitSubset.Program.__GateEntry();
            DelayOrderSubset.Program.__GateEntry();
            CancellationSubset.Program.__GateEntry();
            CustomAwaitableSubset.Program.__GateEntry();
            MultiAwaiterSubset.Program.__GateEntry();
            TaskFactorySubset.Program.__GateEntry();
            ValueTaskConfigureAwaitSubset.Program.__GateEntry();
            AsyncVoidSubset.Program.__GateEntry();
            BlockingWaitWrapSubset.Program.__GateEntry();
            // APPENDED LAST (the prove-it-ran prefix rule, AGENTS.md): the bucket's
            // previous output stays an unchanged prefix of the new one.
            WaitAsyncContinueWithSubset.Program.__GateEntry();
            BlockingWaitArgsSubset.Program.__GateEntry();
            SettledCombinatorsSubset.Program.__GateEntry();
            WhenAllFaultSetSubset.Program.__GateEntry();
            TaskDelegateContractSubset.Program.__GateEntry();
            if (args.Length == 1 && args[0] == "before-task-validation")
                return;
            TaskDurationValidationSubset.Program.__GateEntry();
            TaskReceiverValidationSubset.Program.__GateEntry();
            if (args.Length == 1 && args[0] == "before-task-lifecycle")
                return;
            BlockingWaitArgsSubset.Program.RunSequences();
            BlockingWaitArgsSubset.Program.RunArrayMessages();
            BlockingWaitArgsSubset.Program.RunSequenceNulls();
            ColdTaskScheduleSubset.Program.__GateEntry();
            TaskOriginSubset.Program.__GateEntry();
        }
    }
}
