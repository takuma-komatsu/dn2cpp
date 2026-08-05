#nullable enable
using System;
using System.Threading.Tasks;

namespace ContinueWithSubset
{
    // Task.ContinueWith: the continuation task settles with the delegate's
    // result once the antecedent completes; the delegate receives the settled
    // antecedent (reading .Result/.IsFaulted inside it never blocks). Covers
    // the Action<Task> form, the Action<Task, object?> state form, the generic
    // Func<Task<T>, TNew> form on both a pre-completed and a pool-completed
    // antecedent, chaining, and a fault thrown INSIDE a continuation settling
    // the continuation task (not the antecedent). Deterministic: every
    // continuation task is waited before the next line prints.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            // Action<Task> on a pre-completed antecedent.
            Task done = Task.CompletedTask;
            Task cont = done.ContinueWith(t => Console.WriteLine("ran after: " + t.IsCompletedSuccessfully));
            cont.Wait();
            Console.WriteLine("cont: " + cont.IsCompletedSuccessfully);

            // Generic Func<Task<int>, int> on a pre-completed Task<int>.
            Task<int> doubled = Task.FromResult(21).ContinueWith(t => t.Result * 2);
            Console.WriteLine("doubled: " + doubled.Result);

            // A pool-completed antecedent: the continuation observes its result.
            Task<int> pool = Task.Run(() => 5);
            Task<string> chained = pool.ContinueWith(t => "got " + t.Result)
                                       .ContinueWith(t => t.Result + "!");
            Console.WriteLine("chained: " + chained.Result);

            // The delegate sees a FAULTED antecedent without throwing.
            Task<int> faulted = Task.FromException<int>(new InvalidOperationException("ante boom"));
            Task<string> observed = faulted.ContinueWith(t => "faulted=" + t.IsFaulted);
            Console.WriteLine("observed: " + observed.Result);

            // A throw INSIDE the continuation faults the continuation task only; the
            // blocking Wait() wraps it in an AggregateException, as real .NET does.
            Task boom = Task.CompletedTask.ContinueWith(t => throw new InvalidOperationException("cont boom"));
            try { boom.Wait(); }
            catch (AggregateException ae) when (ae.InnerException is InvalidOperationException ex)
            {
                Console.WriteLine("caught: " + ex.Message);
            }
            Console.WriteLine("boomFaulted: " + boom.IsFaulted);

            // The Action<Task, object?> state form.
            Task stateful = Task.CompletedTask.ContinueWith(
                (t, s) => Console.WriteLine("state: " + (string)s! + "," + t.IsCompleted), "carried");
            stateful.Wait();

            // A cold task's continuation: registered before Start, runs after it.
            var cold = new Task(() => { });
            Task<long> after = cold.ContinueWith(t => 7L);
            cold.Start();
            Console.WriteLine("afterCold: " + after.Result);
        }
    }
}
