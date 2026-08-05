using System;
using System.Text;
using System.Threading;

namespace LifoOrder
{
    // Multiple callbacks on one token run in last-in-first-out order on Cancel().
    // All three run sequentially on the (single) canceller thread, so appending to
    // the shared builder needs no lock; join makes the result visible to main.
    internal static class Program
    {
        private static readonly StringBuilder s_order = new StringBuilder();

        internal static void __GateEntry()
        {
            s_order.Clear();
            var cts = new CancellationTokenSource();
            CancellationToken tok = cts.Token;

            tok.Register(() => s_order.Append('A'));
            tok.Register(() => s_order.Append('B'));
            tok.Register(() => s_order.Append('C'));

            var canceller = new Thread(() => cts.Cancel());
            canceller.Start();
            canceller.Join();

            Console.WriteLine("LIFO order: " + s_order.ToString());
        }
    }
}
