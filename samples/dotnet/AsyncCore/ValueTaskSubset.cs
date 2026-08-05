#nullable enable
using System;
using System.Threading.Tasks;

namespace ValueTaskSubset
{
    // custom awaitable — ValueTask / ValueTask<T>. A ValueTask is modeled as the
    // same {task} struct as a TaskAwaiter (always backed by a Dn2CppTask) and its
    // async-method builder reuses Dn2CppAsyncBuilder, so the await/suspend/resume +
    // fault path is the Task machinery. Covers an async ValueTask<T> method, an async
    // ValueTask (void) method, the sync-result and from-Task constructors,.AsTask,
    //.Result,.IsCompleted, and fault propagation through the awaiter.
    internal static class Program
    {
        private static int s_side = 0;

        private static async ValueTask<int> Slow(int v) { await Task.Delay(v); return v; }
        private static async ValueTask Bump() { await Task.Yield(); s_side += 10; }
        private static async ValueTask<int> Boom() { await Task.Yield(); throw new InvalidOperationException("x"); }

        private static async Task<string> Run()
        {
            int a = await Slow(5);                                  // async ValueTask<T> -> 5
            int b = await new ValueTask<int>(11);                   // sync-result ctor -> 11
            int c = await new ValueTask<int>(Task.FromResult(20));  // from-Task ctor -> 20

            ValueTask<int> vt = Slow(3);
            int d = await vt.AsTask();                              //.AsTask -> 3

            bool done = new ValueTask<int>(7).IsCompleted;         // sync ValueTask -> True

            await Bump();
            await Bump();                                           // s_side = 20

            bool caught = false;
            try { await Boom(); }
            catch (InvalidOperationException) { caught = true; }    // fault path

            return a + "," + b + "," + c + "," + d + "," + done + "," + s_side + "," + caught;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result);                       // 5,11,20,3,True,20,True
        }
    }
}
