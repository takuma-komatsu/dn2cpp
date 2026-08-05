#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncStreamsSubset
{
    // async streams — `async IAsyncEnumerable<T>` + `await foreach` + `await using`.
    // Roslyn lowers an async iterator to a *class* state machine driven by
    // AsyncIteratorMethodBuilder, whose MoveNextAsync hands back a ValueTask<bool>
    // over a ManualResetValueTaskSourceCore<bool> promise; the enumerator's
    // DisposeAsync returns a `new ValueTask(IValueTaskSource, short)`. Both the
    // suspending path (Task.Yield between elements) and the synchronously-completing
    // path (every await already done, so MoveNextAsync returns a finished ValueTask)
    // are exercised, plus the iterator's try/finally running through the consumer's
    // early `break`.
    internal static class Program
    {
        // Suspending producer: every element crosses a real suspension.
        private static async IAsyncEnumerable<int> SquaresAsync(int n)
        {
            for (int i = 1; i <= n; i++)
            {
                await Task.Yield();
                yield return i * i;
            }
        }

        // Synchronously-completing producer: no await ever suspends, so each
        // MoveNextAsync returns an already-completed ValueTask<bool>.
        private static async IAsyncEnumerable<string> NamesAsync()
        {
            await Task.CompletedTask;
            yield return "ann";
            await Task.CompletedTask;
            yield return "bob";
            yield return "cid";
        }

        // The iterator's finally must run when the consumer abandons it early —
        // `await foreach` disposes the enumerator on the way out.
        private static async IAsyncEnumerable<int> CountingAsync()
        {
            try
            {
                int i = 0;
                while (true)
                {
                    await Task.Yield();
                    yield return i++;
                }
            }
            finally
            {
                Console.WriteLine("  iterator finally");
            }
        }

        private static async Task<int> SumAsync(IAsyncEnumerable<int> src)
        {
            int sum = 0;
            await foreach (int v in src)
                sum += v;
            return sum;
        }

        private static async Task Run()
        {
            await using (AsyncRes scope = new AsyncRes("block"))
            {
                await foreach (int v in SquaresAsync(4))
                    Console.WriteLine($"  await foreach: {v}");
            }

            await foreach (string s in NamesAsync())
                Console.WriteLine($"  sync stream: {s}");

            Console.WriteLine($"  sum: {await SumAsync(SquaresAsync(5))}");

            await foreach (int v in CountingAsync())
            {
                Console.WriteLine($"  counting: {v}");
                if (v == 2)
                    break;
            }

            // Manual enumerator drive — the shape `await foreach` desugars to.
            IAsyncEnumerator<string> e = NamesAsync().GetAsyncEnumerator();
            try
            {
                while (await e.MoveNextAsync())
                    Console.WriteLine($"  manual: {e.Current}");
            }
            finally
            {
                await e.DisposeAsync();
            }

            await using AsyncRes trailing = new AsyncRes("declaration");
            Console.WriteLine("  tail");
        }

        // An async ValueTask<T> read back from SYNCHRONOUS code — the sync-over-async
        // idiom, and the same result-propagation path an async iterator's
        // MoveNextAsync(): ValueTask<bool> travels. GetResult() / .Result on a ValueTask
        // whose method actually suspended must BLOCK until the task completes, exactly
        // as the Task forms already do: reading the result slot of a still-pending task
        // silently hands back default(T).
        private static async ValueTask<int> SumSuspending(int a, int b)
        {
            await Task.Yield();          // a real suspension — the ValueTask is pending on return
            return a + b;
        }

        private static async ValueTask<int> SumSynchronous(int a, int b)
        {
            await Task.FromResult(1);    // already complete — never suspends
            return a + b;
        }

        private static void SyncOverAsync()
        {
            Console.WriteLine($"  vt suspended: {SumSuspending(20, 22).GetAwaiter().GetResult()}");
            Console.WriteLine($"  vt sync: {SumSynchronous(20, 22).GetAwaiter().GetResult()}");
            Console.WriteLine($"  vt result: {SumSuspending(1, 2).Result}");
            Console.WriteLine($"  vt configured: {SumSuspending(3, 4).ConfigureAwait(false).GetAwaiter().GetResult()}");
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("== async streams ==");
            Run().GetAwaiter().GetResult();
            SyncOverAsync();
            Console.WriteLine("== async streams done ==");
        }
    }

    // await using / IAsyncDisposable — the disposal is an ordinary interface
    // callvirt returning a ValueTask the consumer awaits.
    internal sealed class AsyncRes : IAsyncDisposable
    {
        private readonly string _name;

        public AsyncRes(string name)
        {
            _name = name;
            Console.WriteLine($"  open {_name}");
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            Console.WriteLine($"  close {_name}");
        }
    }
}
