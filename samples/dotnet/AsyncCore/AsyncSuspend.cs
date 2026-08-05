#nullable enable
using System;
using System.Threading.Tasks;

namespace AsyncSuspend
{
    // Real suspension/resumption: these methods await *pending* tasks (not
    // already-complete ones, where MoveNext would run straight through). MoveNext
    // saves its state and returns; the awaited task's completion re-queues MoveNext
    // on a cooperative scheduler; a blocking .Result/.Wait at the top drains that
    // scheduler. The struct state machine is boxed to the heap on first suspension
    // so it survives the returning frame.
    internal static class Program
    {
        // Suspends on a pending Task.Delay, then returns. The scheduler completes
        // the delay on its next turn (duration is not modeled).
        private static async Task<int> DelayedAdd(int a, int b)
        {
            await Task.Delay(1);
            return a + b;
        }

        // Two suspensions in one method: the second await resumes the already-
        // boxed heap state machine (exercises the "already boxed" flag). Also
        // awaits a *pending Task<int>* returned by another suspended async method.
        private static async Task<int> TwoAwaits(int n)
        {
            int x = await DelayedAdd(n, n);   // 2n
            int y = await DelayedAdd(x, 1);   // 2n + 1
            return x + y;                      // 4n + 1
        }

        // A double result carried through the reinterpreted result slot.
        private static async Task<double> DelayedHalf(int n)
        {
            await Task.Delay(1);
            return n / 2.0;
        }

        // Throws after suspending: the exception is captured by the builder
        // (SetException -> faulted task) and re-raised at the blocking .Result —
        // wrapped in an AggregateException, the blocking wait's contract.
        private static async Task<int> Faulting()
        {
            await Task.Delay(1);
            throw new InvalidOperationException();
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(DelayedAdd(2, 3).Result);   // 5
            Console.WriteLine(TwoAwaits(5).Result);       // 21
            Console.WriteLine(DelayedHalf(7).Result);     // 3.5

            int caught = 0;
            try
            {
                Console.WriteLine(Faulting().Result);     // throws before printing
            }
            catch (AggregateException ae) when (ae.InnerException is InvalidOperationException)
            {
                caught = 1;
            }
            Console.WriteLine(caught);                     // 1
        }
    }
}
