using System;
using System.Threading;

namespace Contention
{
    // Many threads Register() concurrently while one thread Cancel()s. The runtime
    // serializes the canceled decision, so every registered callback runs exactly
    // once — either by the Cancel() sweep or immediately because the token was already
    // canceled when it registered. The total count is therefore deterministic
    // (T * K) regardless of how the register/cancel interleave, and proves the path
    // is data-race-free (a racy implementation would lose or double increments).
    internal static class Program
    {
        private static int s_count;
        private const int T = 4;
        private const int K = 50;

        internal static void __GateEntry()
        {
            var cts = new CancellationTokenSource();
            s_count = 0;

            var threads = new Thread[T + 1];
            for (int i = 0; i < T; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < K; j++)
                        cts.Token.Register(() => Interlocked.Increment(ref s_count));
                });
            }
            threads[T] = new Thread(() => cts.Cancel());

            for (int i = 0; i <= T; i++)
                threads[i].Start();
            for (int i = 0; i <= T; i++)
                threads[i].Join();

            Console.WriteLine("contention callbacks run: " + s_count);
        }
    }
}
