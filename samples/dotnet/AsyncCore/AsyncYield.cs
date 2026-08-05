#nullable enable
using System;
using System.Threading.Tasks;

namespace AsyncYield
{
    // Task.Yield(). Unlike Task.Delay (a pending task the scheduler
    // completes on its next turn), Yield's awaiter never reports completed, so
    // awaiting it always suspends and posts MoveNext straight onto the run queue.
    // The method resumes on the scheduler's next pass — yielding its turn to any
    // other ready continuations. A blocking .Result at the top drains the queue.
    internal static class Program
    {
        // A single yield: prints before, suspends, resumes, prints after.
        private static async Task<int> YieldOnce(int n)
        {
            int before = n;
            await Task.Yield();
            return before + 1;
        }

        // Several yields in one method: each resumes the already-boxed heap state
        // machine (exercises the "already boxed" flag across yields).
        private static async Task<int> YieldMany(int n)
        {
            int acc = 0;
            for (int i = 0; i < n; i++)
            {
                acc += i;
                await Task.Yield();
            }
            return acc;
        }

        // Interleaving: two methods each yield between two prints. Because Yield
        // reschedules onto the FIFO run queue, their middle steps interleave
        // rather than running one method to completion before the other.
        private static async Task Ping(string tag)
        {
            Console.WriteLine(tag + "1");
            await Task.Yield();
            Console.WriteLine(tag + "2");
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(YieldOnce(10).Result);   // 11
            Console.WriteLine(YieldMany(5).Result);    // 0+1+2+3+4 = 10

            // Start both before blocking so their continuations share the queue.
            Task a = Ping("A");
            Task b = Ping("B");
            a.Wait();
            b.Wait();
            // Expected order: A1, B1 (both ran to their first yield synchronously
            // up to the await), then A2, B2 (resumptions drained FIFO).
        }
    }
}
