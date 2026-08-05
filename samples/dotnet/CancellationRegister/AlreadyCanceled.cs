using System;
using System.Threading;

namespace AlreadyCanceled
{
    // Registering on an already-canceled token runs the callback immediately and
    // synchronously, before Register returns — so the flag is already set on the next
    // line (no thread, no await).
    internal static class Program
    {
        private static bool s_ran;

        internal static void __GateEntry()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            CancellationToken tok = cts.Token;
            s_ran = false;

            tok.Register(() => { s_ran = true; });
            bool ranBeforeReturn = s_ran;

            Console.WriteLine("immediate on already-canceled: " + ranBeforeReturn);
        }
    }
}
