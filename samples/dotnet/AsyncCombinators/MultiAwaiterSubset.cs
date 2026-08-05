#nullable enable
using System;
using System.Threading.Tasks;

namespace MultiAwaiterSubset
{
    // multi-awaiter fairness. Several async methods await the SAME task; when it
    // completes, every registered continuation is resumed (the scheduler fires the
    // whole continuation list in await/FIFO order), and each awaiter observes the same
    // result. Awaiting an already-completed task resumes promptly too. Assertions are
    // order-independent (the resume order of concurrent awaiters is unspecified in
    //.NET) — only that all awaiters run and see the result.
    internal static class Program
    {
        private static async Task<int> Shared(int v) { await Task.Delay(15); return v; }
        private static async Task<int> Plus(Task<int> t, int d) { return await t + d; }

        private static async Task<string> Run()
        {
            Task<int> s = Shared(10);

            // Five awaiters registered on the one pending task before it completes.
            int[] r = await Task.WhenAll(Plus(s, 1), Plus(s, 2), Plus(s, 3), Plus(s, 4), Plus(s, 5));
            int sum = 0;
            foreach (var x in r) sum += x;            // 11+12+13+14+15 = 65

            // The task is now complete: awaiting it again still yields the result.
            int a = await s, b = await s, c = await s; // 10,10,10

            return sum + "," + (a + b + c);           // 65,30
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result);          // 65,30
        }
    }
}
