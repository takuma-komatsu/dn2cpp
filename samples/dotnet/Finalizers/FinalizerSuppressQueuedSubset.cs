using System;
using System.Runtime.CompilerServices;

namespace FinalizerSuppressQueuedSubset
{
    // GC.SuppressFinalize must cancel the finalizer even when the collector has
    // ALREADY moved the instance onto the finalization queue -- the narrow window
    // between "queued" and "Finalize() ran", where a long WeakReference is the only
    // way managed code can still name the object. Nothing here prints from a
    // finalizer: every line is written by the driver thread, so the section's
    // output cannot depend on when the queue happens to drain.
    internal sealed class Victim
    {
        internal static int Finalized;
        ~Victim()
        {
            Finalized++;
        }
    }

    // The experiment runs inside this finalizer body on purpose: the finalizer
    // thread is the queue's only consumer, so while it sits here nothing can drain
    // a freshly queued entry. That turns the window from a race into a fact. Both
    // victims are queued inside this ONE body: a second window's victim lands in
    // the block the first one vacated, which the first body's dead frame still
    // names, and a conservative collector then never queues it at all.
    internal sealed class Window
    {
        ~Window()
        {
            Program.RunInsideWindow();
        }
    }

    internal static class Program
    {
        private static WeakReference<Victim> s_long;
        private static WeakReference<Victim> s_short;
        internal static bool WindowRan;

        // NoInlining is load-bearing under the conservative (Boehm) GC on every
        // helper here: an inlined body's strong slots join the retry loop's live
        // frame and are scanned as roots every round.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateVictim()
        {
            var v = new Victim();
            s_long = new WeakReference<Victim>(v, trackResurrection: true);
            s_short = new WeakReference<Victim>(v, trackResurrection: false);
        }

        // TryGetTarget hands the referent to a local, so each probe has to sit in
        // its own frame: probing from the retry loop's frame instead leaves that
        // strong slot live across every later round, and a conservative collector
        // reads it as a root -- the referent is then never queued and the loop can
        // only time out.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ShortAlive() => s_short.TryGetTarget(out _);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool LongAlive() => s_long.TryGetTarget(out _);

        // A short weak reference is cleared when the referent becomes
        // finalizer-reachable, a long one only once its Finalize() has run: short
        // dead + long alive is exactly "queued, body not yet run".
        // Whether the window opened is deliberately NOT printed: the wasm build
        // never collects an object first named from inside a finalizer body, so it
        // reaches this section with the victim merely live, and a printed answer
        // would differ per host rather than per behaviour. What every host can
        // assert is what the suppress is handed -- an instance the long reference
        // still names and no finalizer has run yet.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool WaitUntilQueued()
        {
            for (int rounds = 0; rounds < 64; rounds++)
            {
                GC.Collect();
                if (!ShortAlive() && LongAlive())
                    return true;
            }
            return false;
        }

        internal static void RunInsideWindow()
        {
            CreateVictim();
            WaitUntilQueued();
            bool got = s_long.TryGetTarget(out Victim suppressed);
            Console.WriteLine("suppressing a victim: named=" + got + " unfinalized=" + (Victim.Finalized == 0));
            GC.SuppressFinalize(suppressed);

            // The same window, opting the queued instance straight back in: the
            // suppress cancels the queued body and the re-registration arms a
            // fresh one, which must net out to exactly one finalization.
            CreateVictim();
            WaitUntilQueued();
            got = s_long.TryGetTarget(out Victim revived);
            Console.WriteLine("re-registering a victim: named=" + got + " unfinalized=" + (Victim.Finalized == 0));
            GC.SuppressFinalize(revived);
            GC.ReRegisterForFinalize(revived);
            GC.KeepAlive(suppressed);
            WindowRan = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void MakeWindow()
        {
            _ = new Window();
        }

        // Control: the same shape without the suppress, proving a queued victim
        // really would have been finalized.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void MakeControl()
        {
            _ = new Victim();
        }

        // A repeated suppress on a LIVE instance is legal and reaches the same
        // "no registration left to remove" state a post-enqueue suppress does. The
        // finalizer must still run, so whatever records the suppression must stop
        // describing this instance once it is opted back in.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DoubleSuppressThenReRegister()
        {
            var v = new Victim();
            GC.SuppressFinalize(v);
            GC.SuppressFinalize(v);
            GC.ReRegisterForFinalize(v);
        }

        // Collect until `want` finalizations have been observed (or the budget is
        // spent): one round is not guaranteed to reclaim an instance under a
        // conservative collector, and the extra rounds cannot manufacture a
        // finalization that was suppressed. Real .NET always exits on the first.
        private static void DrainUntil(int want)
        {
            for (int rounds = 0; Victim.Finalized < want && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        internal static void __GateEntry()
        {
            MakeWindow();
            for (int rounds = 0; !WindowRan && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            // The window body left both victims queued, so the count is only
            // meaningful once the queue has drained past them.
            DrainUntil(1);
            Console.WriteLine("window ran=" + WindowRan);
            Console.WriteLine("finalizations after post-enqueue suppress=" + Victim.Finalized);
            MakeControl();
            DrainUntil(2);
            Console.WriteLine("control finalizations=" + Victim.Finalized);
            DoubleSuppressThenReRegister();
            DrainUntil(3);
            Console.WriteLine("finalizations after double suppress + re-register=" + Victim.Finalized);
            Console.WriteLine("done-suppress-queued");
        }
    }
}
