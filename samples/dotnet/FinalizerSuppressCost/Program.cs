using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FinalizerSuppressCost
{
    // A batch of victims suppressed AFTER the collector queued them, then drained.
    // Every one leaves a record the drain has to find, so this is the shape whose
    // cost tracks the suppressed-finalizer set's size rather than the drain's
    // length. The measurement is the runtime's own counters on stderr
    // (DN2CPP_GC_SUPPRESS_STATS); stdout carries only facts a precise and a
    // conservative collector both hold, so it can be diffed against real .NET.
    //
    // The process must reach a normal exit: the counters are reported from the
    // exit funnel, and an abort loses them.
    internal sealed class Victim
    {
        internal static int Finalized;

        ~Victim()
        {
            Interlocked.Increment(ref Finalized);
        }
    }

    // The batch is built inside this finalizer body on purpose: the finalizer
    // thread is the queue's only consumer, so while it sits here nothing can drain
    // a freshly queued entry. That turns the window between "queued" and
    // "Finalize() ran" from a race into a fact.
    internal sealed class Window
    {
        ~Window()
        {
            Program.RunInsideWindow();
        }
    }

    internal static class Program
    {
        // Set size and drain length at once — every victim is suppressed
        // post-enqueue and then dequeued. A walk of the whole set per dequeue
        // therefore costs the square of this, orders of magnitude above the linear
        // answer, while the run stays a few seconds.
        private const int Count = 4096;
        // Instances a conservative stack scan may pin, leaving them merely live at
        // the suppress; a precise collector queues every one.
        private const int PinAllowance = 64;

        private static WeakReference<object>[] s_long;
        private static WeakReference<object>[] s_short;
        private static int s_queued;
        private static int s_suppressed;
        internal static bool WindowRan;

        // NoInlining is load-bearing under a conservative collector on every helper
        // that names a victim: an inlined body's strong slots join the caller's
        // long-lived frame and are scanned as roots on every round.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateVictim(int i)
        {
            var v = new Victim();
            s_long[i] = new WeakReference<object>(v, trackResurrection: true);
            s_short[i] = new WeakReference<object>(v, trackResurrection: false);
        }

        // A short weak reference is cleared when the referent becomes
        // finalizer-reachable, a long one only once its Finalize() has run: short
        // dead + long alive is exactly "queued, body not yet run".
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool Queued(int i)
        {
            return !s_short[i].TryGetTarget(out _) && s_long[i].TryGetTarget(out _);
        }

        private static int CountQueued()
        {
            int n = 0;
            for (int i = 0; i < Count; i++)
            {
                if (Queued(i))
                    n++;
            }

            return n;
        }

        // Suppressing a victim the collector left merely live is legal and simply
        // does not reach the set — it costs a record, never correctness.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool SuppressOne(int i)
        {
            if (!s_long[i].TryGetTarget(out object v))
                return false;
            GC.SuppressFinalize(v);
            return true;
        }

        internal static void RunInsideWindow()
        {
            for (int i = 0; i < Count; i++)
                CreateVictim(i);
            for (int rounds = 0; rounds < 64; rounds++)
            {
                GC.Collect();
                s_queued = CountQueued();
                if (s_queued >= Count - PinAllowance)
                    break;
            }

            for (int i = 0; i < Count; i++)
            {
                if (SuppressOne(i))
                    s_suppressed++;
            }

            WindowRan = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void MakeWindow()
        {
            _ = new Window();
        }

        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            s_long = new WeakReference<object>[Count];
            s_short = new WeakReference<object>[Count];
            MakeWindow();
            for (int rounds = 0; !WindowRan && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // The window left the whole batch queued and suppressed; draining it is
            // what pays the set's scan cost, and the counters below it.
            for (int rounds = 0; rounds < 8; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Console.WriteLine("suppress cost window ran=" + WindowRan);
            Console.WriteLine("suppress cost queued enough=" + (s_queued >= Count - PinAllowance));
            Console.WriteLine("suppress cost suppressed all=" + (s_suppressed == Count));
            // A suppress must cancel the body whether it landed on a queued victim
            // or a live one, so this holds on every collector.
            Console.WriteLine("suppress cost no victim finalized=" + (Victim.Finalized == 0));
            Console.WriteLine("suppress cost done");
        }
    }
}
