using System;
using System.Reflection;
using System.Threading;

// Native cloning divergences: owning runtime structs refuse a second resource
// owner, while strings copy their full UTF-16 buffer. Nonempty string receivers
// can corrupt the CLR heap and must run only through the native opt-in route.
// The agreeing shapes remain in MemberwiseCloneSubset's live CLR parity gate.
// ManualResetEventSlim shares the EventWaitHandle representation, so its refusal
// names that runtime type rather than the source spelling.
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

            // ThreadLocal<int> retains the bare generic runtime name rather than CLR's
            // closed spelling. The Type clone is a control: its RuntimeType header and
            // represented type remain readable on a distinct object.
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

        internal static void RunUnwrappedProbe()
        {
            Console.WriteLine("== metadata-answer invoke flags ==");
            var sem = new SemaphoreSlim(1, 1);
            MethodInfo method = typeof(object).GetMethod(
                "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                method.Invoke(sem, BindingFlags.DoNotWrapExceptions, null, null, null);
                Console.WriteLine("SemaphoreSlim unwrapped -> clone returned");
            }
            catch (PlatformNotSupportedException e)
            {
                Console.WriteLine("SemaphoreSlim unwrapped -> " + e.GetType().Name);
            }
        }

        internal static void RunStringCopies()
        {
            Console.WriteLine("== safe string memberwise copies ==");
            MethodInfo method = typeof(object).GetMethod(
                "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            string[] sources = { "hello world", "A\u03A9\uD83D\uDE00\0Z", new string('\u754C', 257) };
            var copies = new string[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                string source = sources[i];
                string copy = (string)method.Invoke(source, null);
                string second = (string)method.Invoke(copy, null);
                copies[i] = copy;
                Console.WriteLine("string copy " + i + "=" + copy.Length + ":" + copy.GetType().FullName
                    + ":" + (source == copy && copy == second) + ":" + ReferenceEquals(source, copy)
                    + ":" + ReferenceEquals(copy, second));
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (int i = 0; i < sources.Length; i++)
            {
                string source = sources[i];
                string copy = copies[i];
                bool charactersMatch = source.Length == copy.Length;
                for (int j = 0; j < source.Length && charactersMatch; j++)
                    charactersMatch &= source[j] == copy[j];
                Console.WriteLine("string copy GC " + i + "=" + (source == copy)
                    + ":" + charactersMatch + ":" + copy.Length);
            }
            Console.WriteLine("safe string memberwise copies end");
        }
    }
}
