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
            if (args.Length == 1 && args[0] == "configured-options-only")
            {
                ConfigureAwaitSubset.Program.RunOptions();
                return;
            }
            if (args.Length == 1 && (args[0] == "fresh-cancellation-filter"
                || args[0] == "fresh-cancellation-filter-generic"))
            {
                TaskCancellationTokenSubset.Program.RunFreshFilter(args[0] == "fresh-cancellation-filter-generic");
                return;
            }
            if (args.Length == 1 && args[0] == "native-delay-deadlines")
            {
                DelayOrderSubset.Program.RunNativeDeadlineProbe();
                BlockingWaitWrapSubset.Program.PollDelayedTasks(waitForCallbacks: false);
                BlockingWaitWrapSubset.Program.PollDelayedWhenAll(waitForCallbacks: false);
                return;
            }
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
            if (args.Length == 1 && args[0] == "before-cancellation-receivers")
                return;
            CancellationReceiverSubset.Program.Run();
            if (args.Length == 1 && args[0] == "before-value-task-source-handoff")
                return;
            ValueTaskSourceHandoffSubset.Program.__GateEntry();
            if (args.Length == 1 && args[0] == "before-value-task-source-rejection")
                return;
            ValueTaskSourceHandoffSubset.Program.RunRejectedRegistration();
            if (args.Length == 1 && args[0] == "before-registration-rejection")
                return;
            RegistrationRejectionSubset.Program.__GateEntry();
            if (args.Length == 1 && args[0] == "before-enum-whenall")
                return;
            WhenAllSubset.Program.RunEnumResults();
            if (args.Length == 1 && args[0] == "before-pending-task-joins")
                return;
            SettledCombinatorsSubset.Program.RunPending();
            if (args.Length == 1 && args[0] == "before-cancellation-callback-faults")
                return;
            CancellationSubset.Program.RunFaults();
            if (args.Length == 1 && args[0] == "before-lazy-aggregate-message")
                return;
            WhenAllFaultSetSubset.Program.RunAggregateMessages();
            if (args.Length == 1 && args[0] == "before-delay-synchronization")
                return;
            DelayOrderSubset.Program.RunSynchronizationChecks();
            if (args.Length == 1 && args[0] == "before-derived-task-promises")
                return;
            SettledCombinatorsSubset.Program.RunPromises();
            if (args.Length == 1 && args[0] == "before-timed-waitasync")
                return;
            WaitAsyncContinueWithSubset.Program.RunTimeouts();
            if (args.Length == 1 && args[0] == "before-semaphore-waiters")
                return;
            SettledCombinatorsSubset.Program.RunSemaphoreWaiters();
            if (args.Length == 1 && args[0] == "before-task-creation-options")
                return;
            TaskCreationOptionsSubset.Program.Run();
            if (args.Length == 1 && args[0] == "before-task-cancellation-tokens")
                return;
            TaskCancellationTokenSubset.Program.Run();
            if (args.Length == 1 && args[0] == "before-protected-task-start")
                return;
            SettledCombinatorsSubset.Program.RunProtectedStart();
            if (args.Length == 1 && args[0] == "before-scheduler-inline")
                return;
            SettledCombinatorsSubset.Program.RunSchedulerInline();
            if (args.Length == 1 && args[0] == "before-scheduler-queue")
                return;
            SettledCombinatorsSubset.Program.RunSchedulerQueue();
            if (args.Length == 1 && args[0] == "before-task-add-exception")
                return;
            WhenAllFaultSetSubset.Program.RunAddedExceptions();
            if (args.Length == 1 && args[0] == "before-task-finish")
                return;
            WhenAllFaultSetSubset.Program.RunFinish();
            if (args.Length == 1 && args[0] == "before-scheduler-ids")
                return;
            ColdTaskScheduleSubset.Program.RunSchedulerIds();
            BlockingWaitWrapSubset.Program.RunTimedWaits();
            ConfigureAwaitSubset.Program.RunTimedWaitContexts();
            BlockingWaitWrapSubset.Program.RunDelayDeadlines();
            if (args.Length == 1 && args[0] == "before-synchronization-context")
                return;
            ConfigureAwaitSubset.Program.RunPumpedContext();
            if (args.Length == 1 && args[0] == "before-configured-options")
                return;
            ConfigureAwaitSubset.Program.RunOptions();
            // Ends the process: a builder suspension's rejected registration is re-raised
            // as an unhandled ThreadPool exception.
            if (args.Length == 2 && args[0] == "suspension-rejection")
                RegistrationRejectionSubset.Program.Suspend(args[1]);
        }
    }
}
