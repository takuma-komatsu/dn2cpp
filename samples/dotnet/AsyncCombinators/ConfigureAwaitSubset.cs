#nullable enable
using System;
using System.Threading.Tasks;

namespace ConfigureAwaitSubset
{
    // custom awaitable — task.ConfigureAwait(bool). It returns a
    // ConfiguredTaskAwaitable wrapping the same task; on the single-threaded
    // cooperative model the flag is a no-op, so the awaitable and its nested
    // ConfiguredTaskAwaiter lower to the same Dn2CppTaskAwaiter as a plain Task await
    // (same suspend/resume path). Covers the generic and non-generic forms, both
    // flag values, a pending (suspending) and a pre-completed awaited task, and the
    // fault path (a faulted task re-raises through ConfiguredTaskAwaiter.GetResult).
    internal static class Program
    {
        private static async Task<int> Slow(int v) { await Task.Delay(v); return v; }

        private static async Task<string> Run()
        {
            int a = await Slow(10).ConfigureAwait(false);          // suspends, resumes -> 10
            int b = await Slow(20).ConfigureAwait(true);           // -> 20
            await Task.Delay(5).ConfigureAwait(false);             // non-generic awaitable
            int c = await Task.FromResult(7).ConfigureAwait(false); // pre-completed -> 7

            bool caught = false;
            try
            {
                await Task.FromException<int>(new InvalidOperationException("x")).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                caught = true;
            }

            return a + "," + b + "," + c + "," + caught;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result);                       // 10,20,7,True
        }
    }
}
