using System;
using System.Threading;

namespace CrossThreadCancel
{
    // A callback registered on the main thread must run when a *different* thread
    // calls Cancel(). The flag is read only after joining the canceller thread, so
    // join establishes happens-before and the result is deterministic.
    internal static class Program
    {
        private static bool s_ran;

        internal static void __GateEntry()
        {
            var cts = new CancellationTokenSource();
            CancellationToken tok = cts.Token;
            s_ran = false;

            tok.Register(() => { s_ran = true; });

            var canceller = new Thread(() => cts.Cancel());
            canceller.Start();
            canceller.Join();

            Console.WriteLine("cross-thread callback ran: " + s_ran);
            Console.WriteLine("token now canceled: " + tok.IsCancellationRequested);
        }
    }
}
