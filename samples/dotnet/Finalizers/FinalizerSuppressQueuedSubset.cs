using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FinalizerSuppressQueuedSubset
{
    // GC.SuppressFinalize must cancel the finalizer even when the collector has
    // ALREADY moved the instance onto the finalization queue -- the narrow window
    // between "queued" and "Finalize() ran", where a long WeakReference is the only
    // way managed code can still name the object. Nothing here prints from a
    // finalizer: every line is written by the driver thread, so the section's
    // output cannot depend on when the queue happens to drain.
    //
    // One type -- and so one counter -- per victim role: with a shared counter a
    // leftover registration from one role can raise the count another role's
    // drain exits on, and the assertion's subject swaps silently while the diff
    // stays exact.
    internal sealed class SuppressedVictim
    {
        internal static int Finalized;
        ~SuppressedVictim()
        {
            Finalized++;
        }
    }

    internal sealed class ReVictim
    {
        internal static int Finalized;
        ~ReVictim()
        {
            Finalized++;
        }
    }

    internal sealed class ReThenSuppressVictim
    {
        internal static int Finalized;
        ~ReThenSuppressVictim()
        {
            Finalized++;
        }
    }

    internal sealed class ControlVictim
    {
        internal static int Finalized;
        ~ControlVictim()
        {
            Finalized++;
        }
    }

    internal sealed class DoubleVictim
    {
        internal static int Finalized;
        ~DoubleVictim()
        {
            Finalized++;
        }
    }

    // The experiment runs inside this finalizer body on purpose: the finalizer
    // thread is the queue's only consumer, so while it sits here nothing can drain
    // a freshly queued entry. That turns the window from a race into a fact. Every
    // victim is queued inside this ONE body: a later window's victim lands in a
    // block an earlier one vacated, which the earlier body's dead frame still
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
        private static WeakReference<object> s_long;
        private static WeakReference<object> s_short;
        private static bool s_requireFinalizerWindows;
        // A strong root over the re-registered victim. While it is set the
        // instance cannot become unreachable again, so a finalization observed
        // under it can only have come from the entry already queued.
        private static object s_root;
        private static bool s_suppressWindowOpened;
        private static bool s_windowOpened;
        // The mirror-image ordering's root and window flag: re-register FIRST,
        // then suppress, both inside the window.
        private static object s_reThenSuppressRoot;
        private static bool s_reThenSuppressWindowOpened;
        internal static bool WindowRan;

        // NoInlining is load-bearing under the conservative (Boehm) GC on every
        // helper here: an inlined body's strong slots join the retry loop's live
        // frame and are scanned as roots every round.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateSuppressedVictim()
        {
            var v = new SuppressedVictim();
            s_long = new WeakReference<object>(v, trackResurrection: true);
            s_short = new WeakReference<object>(v, trackResurrection: false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateReVictim()
        {
            var v = new ReVictim();
            s_long = new WeakReference<object>(v, trackResurrection: true);
            s_short = new WeakReference<object>(v, trackResurrection: false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateReThenSuppressVictim()
        {
            var v = new ReThenSuppressVictim();
            s_long = new WeakReference<object>(v, trackResurrection: true);
            s_short = new WeakReference<object>(v, trackResurrection: false);
        }

        private static void RunOnDeadThread(ThreadStart action)
        {
            if (!s_requireFinalizerWindows)
            {
                // The threadless arm retains the queued-window partial.
                action();
                return;
            }
            var creator = new Thread(action);
            creator.Start();
            creator.Join();
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
        // The threadless arm folds this window into the queued-window partial.
        // The native gate requires every raw window.
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
            RunOnDeadThread(CreateSuppressedVictim);
            s_suppressWindowOpened = WaitUntilQueued();
            bool got = s_long.TryGetTarget(out object suppressed);
            Console.WriteLine("suppressing a victim: named=" + got + " unfinalized=" + (SuppressedVictim.Finalized == 0));
            GC.SuppressFinalize(suppressed);

            // The same window, opting the queued instance straight back in: the
            // suppress cancels the queued body and the re-registration arms a
            // fresh one, which must net out to exactly one finalization.
            RunOnDeadThread(CreateReVictim);
            s_windowOpened = WaitUntilQueued();
            got = s_long.TryGetTarget(out object revived);
            Console.WriteLine("re-registering a victim: named=" + got + " unfinalized=" + (ReVictim.Finalized == 0));
            GC.SuppressFinalize(revived);
            GC.ReRegisterForFinalize(revived);
            // Rooted before this frame dies, so the finalization below can only
            // come from the entry already queued: an implementation that drops
            // that entry and waits for a second unreachability never gets one.
            s_root = revived;

            // The mirror image: re-register FIRST, then suppress. The suppress is
            // the LAST word on the queued entry — it must be dropped — but the
            // re-registration must stay armed for the next unreachability, so the
            // net is zero finalizations while rooted and exactly one after.
            RunOnDeadThread(CreateReThenSuppressVictim);
            s_reThenSuppressWindowOpened = WaitUntilQueued();
            got = s_long.TryGetTarget(out object reThenSuppressed);
            Console.WriteLine("re-then-suppress victim: named=" + got + " unfinalized=" + (ReThenSuppressVictim.Finalized == 0));
            GC.ReRegisterForFinalize(reThenSuppressed);
            GC.SuppressFinalize(reThenSuppressed);
            s_reThenSuppressRoot = reThenSuppressed;
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
            _ = new ControlVictim();
        }

        // A repeated suppress on a LIVE instance is legal and reaches the same
        // "no registration left to remove" state a post-enqueue suppress does. The
        // finalizer must still run, so whatever records the suppression must stop
        // describing this instance once it is opted back in.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DoubleSuppressThenReRegister()
        {
            var v = new DoubleVictim();
            GC.SuppressFinalize(v);
            GC.SuppressFinalize(v);
            GC.ReRegisterForFinalize(v);
        }

        // The drains below collect until their role's OWN counter reaches 1 (or
        // the budget is spent): one round is not guaranteed to reclaim an
        // instance under a conservative collector, the extra rounds cannot
        // manufacture a finalization that was suppressed, and no other object's
        // finalizer moves the exit condition. Real .NET always exits on the
        // first round.
        private static void DrainRe()
        {
            for (int rounds = 0; ReVictim.Finalized < 1 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static void DrainReThenSuppress()
        {
            for (int rounds = 0; ReThenSuppressVictim.Finalized < 1 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static void DrainControl()
        {
            for (int rounds = 0; ControlVictim.Finalized < 1 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static void DrainDouble()
        {
            for (int rounds = 0; DoubleVictim.Finalized < 1 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        // No exit threshold on purpose: a drain keyed on a count already at its
        // target returns without collecting, and the assertion after this helper
        // is precisely that collecting buys nothing further.
        private static void CollectAndWait()
        {
            for (int rounds = 0; rounds < 8; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        internal static void __GateEntry(bool requireFinalizerWindows)
        {
            s_requireFinalizerWindows = requireFinalizerWindows;
            RunOnDeadThread(MakeWindow);
            for (int rounds = 0; !WindowRan && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            // The window body left the re-registered victim queued, so its count
            // is only meaningful once the queue has drained past that entry.
            if (s_windowOpened)
                DrainRe();
            Console.WriteLine("window ran=" + WindowRan);
            // One line per window, not a folded boolean: a red run must name
            // which window failed to open.
            if (s_requireFinalizerWindows)
            {
                Console.WriteLine("suppress window opened=" + s_suppressWindowOpened);
                Console.WriteLine("re-register window opened=" + s_windowOpened);
                Console.WriteLine("re-then-suppress window opened=" + s_reThenSuppressWindowOpened);
            }
            // The assertion, and the one line that pins the re-registration: the
            // dedicated count is read while the root still forbids a second
            // collection, so a 1 here can only be the entry already queued. A
            // build that never opened the window has nothing queued and must sit
            // at 0, so the window is folded in rather than printed.
            Console.WriteLine("queued entry ran while rooted=" + (!s_windowOpened || ReVictim.Finalized == 1));
            // Un-rooting must not buy a second finalization: the queued entry
            // consumed the re-registration. Collected unconditionally -- see
            // CollectAndWait -- so the count is re-read against real collections.
            s_root = null;
            CollectAndWait();
            Console.WriteLine("exactly one finalization after un-rooting=" + (!s_windowOpened || ReVictim.Finalized == 1));
            // The mirror ordering. Threshold-free on purpose: its queued entry was
            // dropped, so a count-keyed drain would return without collecting and
            // the assertion would hold vacuously.
            CollectAndWait();
            Console.WriteLine("re-then-suppress stayed suppressed while rooted=" + (!s_reThenSuppressWindowOpened || ReThenSuppressVictim.Finalized == 0));
            // Un-rooting is what hands the re-registration its unreachability: an
            // implementation whose suppress also tore down the re-registration
            // stays at 0 here forever.
            s_reThenSuppressRoot = null;
            DrainReThenSuppress();
            Console.WriteLine("re-then-suppress finalized after un-rooting=" + (!s_reThenSuppressWindowOpened || ReThenSuppressVictim.Finalized == 1));
            CollectAndWait();
            Console.WriteLine("re-then-suppress finalized exactly once=" + (!s_reThenSuppressWindowOpened || ReThenSuppressVictim.Finalized == 1));
            // Raw and unfolded: a post-enqueue suppress with no re-registration
            // must hold on every host, queued or merely live.
            Console.WriteLine("post-enqueue suppressed finalizations=" + SuppressedVictim.Finalized);
            RunOnDeadThread(MakeControl);
            DrainControl();
            Console.WriteLine("control finalizations=" + ControlVictim.Finalized);
            RunOnDeadThread(DoubleSuppressThenReRegister);
            DrainDouble();
            Console.WriteLine("finalizations after double suppress + re-register=" + DoubleVictim.Finalized);
            Console.WriteLine("done-suppress-queued");
        }
    }
}
