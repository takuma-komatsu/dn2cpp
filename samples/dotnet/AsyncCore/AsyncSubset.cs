#nullable enable
using System;
using System.Threading.Tasks;

namespace AsyncSubset
{
    // C# `async`/`await` lowers to a compiler-generated state machine driven by
    // AsyncTaskMethodBuilder. Increment 1 models only *synchronously completed*
    // awaits (await Task.CompletedTask / Task.FromResult / another sync-completing
    // async method): the machine runs straight through MoveNext with no
    // suspension, so no scheduler is needed yet. Task/Task<T>, the builder and the
    // awaiter are modeled by hand-written runtime structs.
    internal static class Program
    {
        // Awaits an already-completed (void) task, then returns a value.
        private static async Task<int> AddAsync(int a, int b)
        {
            await Task.CompletedTask;
            return a + b;
        }

        // Awaits a completed Task<T> and uses its result.
        private static async Task<int> FromResultAsync(int x)
        {
            int v = await Task.FromResult(x * 2);
            return v + 1;
        }

        // Awaits other async methods (each completes synchronously). Exercises
        // Task<int>.GetAwaiter / TaskAwaiter<int>.GetResult (the generic awaiter).
        private static async Task<int> ChainAsync(int n)
        {
            int a = await AddAsync(n, n);      // 2n
            int b = await FromResultAsync(n);  // 2n + 1
            return a + b;                       // 4n + 1
        }

        // A non-generic `async Task` (no result): uses the non-generic builder.
        private static async Task RunAsync(int[] log)
        {
            log[0] = await AddAsync(3, 4);     // 7
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(AddAsync(2, 3).Result);       // 5
            Console.WriteLine(FromResultAsync(10).Result);  // 21
            Console.WriteLine(ChainAsync(5).Result);        // 21

            int[] log = new int[1];
            Task t = RunAsync(log);
            t.Wait();                                       // already complete
            Console.WriteLine(log[0]);                      // 7
        }
    }
}
