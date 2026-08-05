#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ValueTaskConfigureAwaitSubset
{
    // ValueTask(<T>).ConfigureAwait(bool) -> ConfiguredValueTaskAwaitable(<T>),
    // intrinsic-mapped to the same {task} awaiter struct as a plain ValueTask
    // await (the flag is a no-op on the cooperative model). Covers a
    // pre-completed ValueTask<int>, a genuinely suspending one (Task.Run on
    // the worker pool), the non-generic form, default(ValueTask) (the BCL's
    // null-task encoding of "completed successfully"), the fault static
    // (ValueTask.FromException<T>), and the canceled-task statics
    // (Task.FromCanceled<T> / ValueTask.FromCanceled). Real .NET raises
    // TaskCanceledException (an OperationCanceledException subclass) from the
    // canceled statics while the transpiled runtime raises the base
    // OperationCanceledException, so the catch clauses print a fixed label
    // instead of the concrete type name.
    internal static class Program
    {
        private static async Task<string> Run()
        {
            // Pre-completed generic ValueTask<int>: completes synchronously.
            int a = await new ValueTask<int>(42).ConfigureAwait(false);

            // Task-backed ValueTask<int> that genuinely suspends (worker pool).
            int b = await new ValueTask<int>(Task.Run(() => 40 + 3)).ConfigureAwait(false);

            // Non-generic ValueTask over a pending Task.
            await new ValueTask(Task.Delay(5)).ConfigureAwait(false);

            // default(ValueTask): a null task normalized to "completed successfully".
            await default(ValueTask);

            // Pre-faulted ValueTask<int>: awaiting re-raises the stored exception
            // through the configured awaiter's GetResult.
            string fault = "none";
            try
            {
                await ValueTask.FromException<int>(new InvalidOperationException("vt-boom")).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                fault = ex.Message;
            }

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Pre-canceled Task<int>: awaiting raises a cancellation exception.
            string canceledTask = "no";
            try { await Task.FromCanceled<int>(cts.Token); }
            catch (OperationCanceledException) { canceledTask = "oce"; }

            // Pre-canceled non-generic ValueTask.
            string canceledValueTask = "no";
            try { await ValueTask.FromCanceled(cts.Token); }
            catch (OperationCanceledException) { canceledValueTask = "oce"; }

            return a + "," + b + "," + fault + "," + canceledTask + "," + canceledValueTask;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result); // 42,43,vt-boom,oce,oce
        }
    }
}
