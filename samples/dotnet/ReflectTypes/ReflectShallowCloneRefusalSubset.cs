using System;
using System.Reflection;
using System.Threading;

// The DIVERGING half of reflective cloning. Object.MemberwiseClone works for the
// intrinsic-represented reference types — the agreeing half is diffed live against real
// .NET by MemberwiseCloneSubset in the reflect-invoke bucket — with SEVEN exceptions,
// which is what this section freezes.
//
// Real .NET clones every one of these (measured on CoreCLR 10.0.9: SemaphoreSlim,
// ManualResetEventSlim, CountdownEvent, Barrier, ReaderWriterLockSlim, Timer and Thread
// all hand back a live clone), so the freeze is a DECLARED dn2cpp divergence and not a
// claim about .NET — the same posture as ReflectMarshalVerdictSubset's
// PlatformNotSupportedException rows.
//
// Why these seven and not the rest: dn2cpp represents them as hand-written C++ structs
// whose state is owned SINGULARLY rather than pointed at. Five of them
// (SemaphoreSlim/EventWaitHandle/CountdownEvent/Barrier/ReaderWriterLockSlim) are
// `new`-allocated on the native heap and embed a std::mutex + std::condition_variable;
// Timer additionally owns a running std::thread and a slot in a process-wide registry;
// Thread's handle IS a std::thread. A bitwise copy of any of them is a second OWNER of
// one resource, which is not what a shallow copy is — so the refusal is loud
// (DN2CPP_TF_NO_SHALLOW_CLONE, dn2cpp_core.h) rather than a memcpy that compiles.
// Closing it needs a per-type clone hook that rebuilds the native half; it may not be
// closed by stamping an extent.
//
// Two spellings of the same handle are asserted deliberately: ManualResetEventSlim is
// intrinsic-mapped onto the one EventWaitHandle representation, so the refusal names
// `System.Threading.EventWaitHandle` and not the type the source wrote. That is the
// message a user will actually see, so it is what the freeze records.
namespace ReflectShallowCloneRefusalSubset
{
    internal static class Program
    {
        private static MethodInfo s_mwc;

        // Prints the verdict for one receiver in a form that carries the whole point of
        // the row: which type the runtime NAMED. The message's first clause is taken
        // rather than the whole thing — the remedy sentence after the colon is prose,
        // and freezing prose turns a reworded diagnostic into a red gate.
        private static void Probe(string label, object receiver)
        {
            try
            {
                object clone = s_mwc.Invoke(receiver, null);
                Console.WriteLine(label + " -> clone same=" + ReferenceEquals(receiver, clone));
            }
            catch (TargetInvocationException tie)
            {
                Exception inner = tie.InnerException;
                string msg = inner.Message;
                int colon = msg.IndexOf(':');
                Console.WriteLine(label + " -> " + inner.GetType().Name + ": "
                    + (colon >= 0 ? msg.Substring(0, colon) : msg));
            }
            catch (PlatformNotSupportedException e)
            {
                // dn2cpp's reflective Invoke propagates the helper's throw directly rather
                // than wrapping it, so both shapes are handled and reduced to one line.
                string msg = e.Message;
                int colon = msg.IndexOf(':');
                Console.WriteLine(label + " -> " + e.GetType().Name + ": "
                    + (colon >= 0 ? msg.Substring(0, colon) : msg));
            }
        }

        internal static void Run()
        {
            Console.WriteLine("-- ReflectShallowCloneRefusalSubset --");
            s_mwc = typeof(object).GetMethod(
                "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

            var sem = new SemaphoreSlim(1, 1);
            Probe("SemaphoreSlim", sem);

            var mre = new ManualResetEventSlim(false);
            Probe("ManualResetEventSlim", mre);

            var cde = new CountdownEvent(2);
            Probe("CountdownEvent", cde);

            var bar = new Barrier(1);
            Probe("Barrier", bar);

            var rw = new ReaderWriterLockSlim();
            Probe("ReaderWriterLockSlim", rw);

            // Never armed (both intervals infinite), so no OS timer thread is started and
            // the section stays deterministic; disposed immediately either way.
            var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
            Probe("Timer", timer);
            timer.Dispose();

            // Never started: a Thread object is a receiver whether or not it is running,
            // and starting one would make the section's output depend on a schedule.
            var thread = new Thread(() => { });
            Probe("Thread", thread);

            // The negative control, and the reason the seven rows above are a STATEMENT
            // rather than the status quo: a hand-written runtime struct whose state is all
            // scalars and GC pointers clones fine, so "intrinsic-represented" is not by
            // itself a refusal. If a future change refuses this one too, the divergence has
            // widened and this line goes red rather than the freeze quietly absorbing it.
            var cts = new CancellationTokenSource();
            Probe("CancellationTokenSource", cts);

            // Two clones that SUCCEED but whose runtime type NAME diverges, so the
            // reflect-invoke live diff cannot carry them and they are frozen here
            // instead. Neither divergence is about cloning: a closed generic intrinsic
            // reports the bare `ThreadLocal`1` handle where real .NET reports the
            // assembly-qualified spelling with its type argument, and dn2cpp's reflection
            // handles are System.Type where CoreCLR's are the internal RuntimeType. The
            // rows are here because the clone is what makes those names OBSERVABLE on a
            // second object — if a future change hands back a truncated clone, the name
            // is the first thing that stops answering.
            var tl = new ThreadLocal<int>(() => 7);
            object tlc = s_mwc.Invoke(tl, null);
            Console.WriteLine("ThreadLocal<int> -> clone type=" + tlc.GetType().FullName
                + " same=" + ReferenceEquals(tl, tlc));
            object tyc = s_mwc.Invoke(typeof(int), null);
            Console.WriteLine("Type -> clone type=" + tyc.GetType().FullName
                + " named=" + ((Type)tyc).FullName
                + " same=" + ReferenceEquals(typeof(int), tyc));
            GC.KeepAlive(tl);

            GC.KeepAlive(sem);
            GC.KeepAlive(mre);
            GC.KeepAlive(cde);
            GC.KeepAlive(bar);
            GC.KeepAlive(rw);
            GC.KeepAlive(thread);
            GC.KeepAlive(cts);
        }
    }
}
