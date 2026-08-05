using System;
using System.Threading;

namespace UnregisterDispose
{
    // Disposing the CancellationTokenRegistration detaches its callback, so a later
    // Cancel() runs only the still-registered ones.
    internal static class Program
    {
        private static bool s_ranA;
        private static bool s_ranB;

        internal static void __GateEntry()
        {
            var cts = new CancellationTokenSource();
            CancellationToken tok = cts.Token;
            s_ranA = false;
            s_ranB = false;

            CancellationTokenRegistration regA = tok.Register(() => { s_ranA = true; });
            tok.Register(() => { s_ranB = true; });
            regA.Dispose();

            cts.Cancel();

            Console.WriteLine("disposed A ran: " + s_ranA + ", B ran: " + s_ranB);
        }
    }
}
