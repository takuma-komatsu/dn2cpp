using System;
using System.Threading;

namespace CancelAfterTimer
{
    // CancelAfter — and the timed ctors, which are the same timer armed at construction.
    // Unlike Task.Delay, this runs on a real OS timer, so nothing here asserts a duration:
    // a cancel that must happen is waited for with a budget far longer than its delay, and
    // a cancel that must NOT happen is armed at 100 ms, rescheduled to a minute, and only
    // then observed. Both sides of the diff (native and real .NET) print the same booleans
    // for any machine that is not two orders of magnitude off.
    internal static class Program
    {
        private const int Budget = 20000; // wait-for-cancel ceiling; a fire takes ~100 ms
        private const int Minute = 60000; // "long enough that it cannot fire during a gate"

        private static bool WaitCancelled(CancellationToken tok)
        {
            for (int waited = 0; waited < Budget; waited += 10)
            {
                if (tok.IsCancellationRequested)
                    return true;
                Thread.Sleep(10);
            }
            return tok.IsCancellationRequested;
        }

        internal static void __GateEntry()
        {
            // It fires at all.
            var fires = new CancellationTokenSource();
            fires.CancelAfter(100);
            Console.WriteLine("cancelafter fires: " + WaitCancelled(fires.Token));

            // A second CancelAfter RESCHEDULES the one timer — it does not arm a second
            // one. Lengthening must retire the first delay entirely.
            var longer = new CancellationTokenSource();
            longer.CancelAfter(100);
            longer.CancelAfter(Minute);
            Thread.Sleep(1500);
            Console.WriteLine("reschedule longer canceled: " + longer.IsCancellationRequested);

            // Shortening must take effect.
            var shorter = new CancellationTokenSource();
            shorter.CancelAfter(Minute);
            shorter.CancelAfter(100);
            Console.WriteLine("reschedule shorter canceled: " + WaitCancelled(shorter.Token));

            // Cancel() ahead of the timer: the callback runs exactly once, and the timer
            // firing later must not run it again.
            int cancelFirst = 0;
            var raced = new CancellationTokenSource();
            raced.Token.Register(() => Interlocked.Increment(ref cancelFirst));
            raced.CancelAfter(300);
            raced.Cancel();
            int atCancel = Volatile.Read(ref cancelFirst);
            Thread.Sleep(1200);
            Console.WriteLine("cancel before timer: " + atCancel + "," + Volatile.Read(ref cancelFirst)
                              + "," + raced.IsCancellationRequested);

            // Dispose() stops a pending timer: the callback never runs and the source never
            // reports cancellation.
            int afterDispose = 0;
            var disposed = new CancellationTokenSource();
            disposed.Token.Register(() => Interlocked.Increment(ref afterDispose));
            disposed.CancelAfter(300);
            disposed.Dispose();
            Thread.Sleep(1200);
            Console.WriteLine("dispose stops timer: " + Volatile.Read(ref afterDispose)
                              + "," + disposed.IsCancellationRequested);

            // The same through `using`, which lowers to callvirt IDisposable::Dispose and
            // takes a different path through the transpiler than the direct call above.
            int afterUsing = 0;
            CancellationTokenSource scoped = new CancellationTokenSource();
            scoped.Token.Register(() => Interlocked.Increment(ref afterUsing));
            using (scoped)
                scoped.CancelAfter(300);
            Thread.Sleep(1200);
            Console.WriteLine("using stops timer: " + Volatile.Read(ref afterUsing)
                              + "," + scoped.IsCancellationRequested);

            // The timed constructors arm the same timer.
            var ctorMs = new CancellationTokenSource(100);
            Console.WriteLine("ctor(int) fires: " + WaitCancelled(ctorMs.Token));
            var ctorTs = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            Console.WriteLine("ctor(TimeSpan) fires: " + WaitCancelled(ctorTs.Token));

            // …and a later CancelAfter reschedules the ctor's timer rather than adding one.
            var ctorThen = new CancellationTokenSource(100);
            ctorThen.CancelAfter(Minute);
            Thread.Sleep(1500);
            Console.WriteLine("ctor then reschedule canceled: " + ctorThen.IsCancellationRequested);

            // Timeout.Infinite disarms; it does not cancel now.
            var infinite = new CancellationTokenSource();
            infinite.CancelAfter(Timeout.Infinite);
            var infiniteTs = new CancellationTokenSource();
            infiniteTs.CancelAfter(TimeSpan.FromMilliseconds(-1));
            Thread.Sleep(800);
            Console.WriteLine("infinite canceled: " + infinite.IsCancellationRequested
                              + "," + infiniteTs.IsCancellationRequested);

            // Any other negative delay is out of range, from either entry point, and so is
            // one past Timer.MaxSupportedTimeout (4294967294 ms) — which only the TimeSpan
            // overload can reach, `int` being too narrow.
            var negative = new CancellationTokenSource();
            try
            {
                negative.CancelAfter(-5);
                Console.WriteLine("negative delay: no throw");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("negative delay: ArgumentOutOfRangeException");
            }
            var huge = new CancellationTokenSource();
            try
            {
                huge.CancelAfter(TimeSpan.FromDays(1000));
                Console.WriteLine("past max timeout: no throw");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("past max timeout: ArgumentOutOfRangeException");
            }
            try
            {
                var bad = new CancellationTokenSource(-5);
                Console.WriteLine("negative ctor: no throw " + bad.IsCancellationRequested);
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("negative ctor: ArgumentOutOfRangeException");
            }

            // Registering on a source the timer already canceled runs the callback inline.
            int late = 0;
            var fired = new CancellationTokenSource();
            fired.CancelAfter(100);
            WaitCancelled(fired.Token);
            fired.Token.Register(() => Interlocked.Increment(ref late));
            Console.WriteLine("register after fire: " + Volatile.Read(ref late));
        }
    }
}
