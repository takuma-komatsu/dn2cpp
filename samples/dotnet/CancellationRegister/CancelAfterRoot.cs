using System;
using System.Threading;

namespace CancelAfterRoot
{
    // A pending CancelAfter must keep its source — and the registration chain hanging off
    // it — alive, exactly as .NET's timer queue roots the source it will call back into.
    // Arm() drops every managed reference to the source before the timer is due, so the
    // only thing that can keep it is the runtime's own timer state. Deep recursion scrubs
    // stale stack slots first (a leftover copy of the pointer would otherwise root it by
    // accident and prove nothing), then repeated collections separated by an allocation
    // storm recycle and overwrite any block a collection freed by mistake. If the source
    // were collected, the fire would either never happen (this prints False) or walk freed
    // registration nodes (this crashes).
    internal static class Program
    {
        private static int s_fired;
        private static object? s_occupy; // keeps the allocation storm reachable

        private sealed class Blob
        {
            public object? A;
            public object? B;
        }

        // Armed here and unreferenced on return: neither the source nor its token nor its
        // registration is reachable from anywhere the collector can see.
        private static void Arm()
        {
            var cts = new CancellationTokenSource();
            cts.Token.Register(() => Interlocked.Exchange(ref s_fired, 1));
            cts.CancelAfter(1200);
        }

        private static long Stomp(int depth)
        {
            if (depth <= 0)
                return 1;
            long a = depth;
            long b = depth * 2;
            long c = depth * 3;
            long d = depth * 4;
            return a + b + c + d + Stomp(depth - 1);
        }

        internal static void __GateEntry()
        {
            Arm();
            long scrub = Stomp(96);
            for (int round = 0; round < 8; round++)
            {
                GC.Collect();
                for (int j = 0; j < 2048; j++)
                {
                    var b = new Blob();
                    b.A = s_occupy;
                    b.B = scrub;
                    s_occupy = b;
                }
                GC.Collect();
                Thread.Sleep(20);
            }
            bool fired = false;
            for (int waited = 0; waited < 20000 && !fired; waited += 10)
            {
                fired = Volatile.Read(ref s_fired) == 1;
                if (!fired)
                    Thread.Sleep(10);
            }
            Console.WriteLine("unreferenced cancelafter still fired: " + fired);
        }
    }
}
