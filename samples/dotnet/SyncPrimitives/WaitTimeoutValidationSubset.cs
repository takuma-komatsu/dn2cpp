using System;
using System.Threading;

namespace WaitTimeouts
{
    // The timeout range checks of the blocking waits: Thread.Join and Thread.Sleep,
    // SemaphoreSlim.Wait, ManualResetEventSlim.Wait, WaitHandle.WaitOne,
    // CountdownEvent.Wait, Barrier.SignalAndWait and ReaderWriterLockSlim.TryEnter*.
    // Each API words its own rejection of a timeout below -1 (and of a TimeSpan past
    // Int32.MaxValue milliseconds), and the check precedes any wait, recursion verdict
    // or signal. SemaphoreSlim.Wait(int) alone checks nothing: -2 takes a free token
    // and otherwise fails at once instead of waiting forever. Every wait here either
    // throws or succeeds without blocking, so the section is single-threaded.
    internal static class WaitTimeoutValidationSubset
    {
        static readonly Exception[] s_saved = new Exception[4];

        private static void Fault(string label, Func<object> run)
        {
            string text;
            try
            {
                text = run().ToString()!;
            }
            catch (Exception ex)
            {
                string actual = ex is ArgumentOutOfRangeException range
                    ? " [" + (range.ActualValue?.GetType().Name ?? "null") + "]"
                    : "";
                text = ex.GetType().Name + ": " + ex.Message.Replace("\r", "").Replace("\n", "|") + actual
                    + " param=" + (ex is ArgumentException argument ? argument.ParamName : null)
                    + " value=" + (ex is ArgumentOutOfRangeException rangeValue ? rangeValue.ActualValue : null);
                int slot = label == "Join(-2ms)" ? 0 : label == "Semaphore Wait(-2ms)" ? 1
                    : label == "Barrier SignalAndWait(-2)" ? 2 : label == "MRES Wait(-2)" ? 3 : -1;
                if (slot >= 0)
                    s_saved[slot] = ex;
            }
            Console.WriteLine(label + " -> " + text);
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("== wait timeouts ==");
            TimeSpan m2 = TimeSpan.FromMilliseconds(-2);
            TimeSpan m15 = TimeSpan.FromMilliseconds(-1.5);
            TimeSpan big = TimeSpan.FromMilliseconds(int.MaxValue + 1.0);
            CancellationToken none = CancellationToken.None;

            var done = new Thread(() => { });
            done.Start();
            done.Join();
            Fault("Join(-2)", () => done.Join(-2));
            Fault("Join(-2ms)", () => done.Join(m2));
            Fault("Join(past Int32)", () => done.Join(big));
            Fault("Join(-1)", () => done.Join(-1));
            Fault("Join(-1.5ms)", () => done.Join(m15));
            Fault("Sleep(-2)", () => { Thread.Sleep(-2); return "slept"; });
            Fault("Sleep(-2ms)", () => { Thread.Sleep(m2); return "slept"; });

            var free = new SemaphoreSlim(1);
            var empty = new SemaphoreSlim(0);
            Fault("Semaphore free Wait(-2)", () => free.Wait(-2) + " count=" + free.CurrentCount);
            Fault("Semaphore empty Wait(-2)", () => empty.Wait(-2) + " count=" + empty.CurrentCount);
            Fault("Semaphore Wait(-2, token)", () => empty.Wait(-2, none));
            Fault("Semaphore Wait(-2ms)", () => empty.Wait(m2));
            Fault("Semaphore Wait(-2ms, token)", () => empty.Wait(m2, none));
            free.Release();
            Fault("Semaphore free Wait(unbounded TimeSpan)", () => free.Wait(big) + " count=" + free.CurrentCount);
            free.Release();
            Fault("Semaphore free Wait(-1.5ms)", () => free.Wait(m15) + " count=" + free.CurrentCount);

            var set = new ManualResetEventSlim(true);
            Fault("MRES Wait(-2)", () => set.Wait(-2));
            Fault("MRES Wait(-2, token)", () => set.Wait(-2, none));
            Fault("MRES Wait(-2ms)", () => set.Wait(m2));
            Fault("MRES Wait(-2ms, token)", () => set.Wait(m2, none));
            Fault("MRES Wait(past Int32)", () => set.Wait(big));
            Fault("MRES Wait(-1.5ms)", () => set.Wait(m15));

            var auto = new AutoResetEvent(true);
            Fault("WaitOne(-2)", () => auto.WaitOne(-2));
            Fault("WaitOne(-2, exitContext)", () => auto.WaitOne(-2, false));
            Fault("WaitOne(-2ms)", () => auto.WaitOne(m2));
            Fault("WaitOne(-2ms, exitContext)", () => auto.WaitOne(m2, false));
            Fault("WaitOne(past Int32)", () => auto.WaitOne(big));
            Fault("WaitOne(0) still set", () => auto.WaitOne(0));

            var counted = new CountdownEvent(0);
            Fault("Countdown Wait(-2)", () => counted.Wait(-2));
            Fault("Countdown Wait(-2, token)", () => counted.Wait(-2, none));
            Fault("Countdown Wait(-2ms)", () => counted.Wait(m2));
            Fault("Countdown Wait(-2ms, token)", () => counted.Wait(m2, none));
            Fault("Countdown Wait(past Int32)", () => counted.Wait(big));

            var barrier = new Barrier(1);
            Fault("Barrier SignalAndWait(-2)", () => barrier.SignalAndWait(-2));
            Fault("Barrier SignalAndWait(-2, token)", () => barrier.SignalAndWait(-2, none));
            Fault("Barrier SignalAndWait(-2ms)", () => barrier.SignalAndWait(m2));
            Fault("Barrier SignalAndWait(-2ms, token)", () => barrier.SignalAndWait(m2, none));
            Fault("Barrier SignalAndWait(past Int32)", () => barrier.SignalAndWait(big));
            Console.WriteLine("Barrier phase=" + barrier.CurrentPhaseNumber
                + " remaining=" + barrier.ParticipantsRemaining);

            var rw = new ReaderWriterLockSlim();
            Fault("TryEnterReadLock(-2)", () => rw.TryEnterReadLock(-2));
            Fault("TryEnterReadLock(-2ms)", () => rw.TryEnterReadLock(m2));
            Fault("TryEnterReadLock(past Int32)", () => rw.TryEnterReadLock(big));
            Fault("TryEnterWriteLock(-2)", () => rw.TryEnterWriteLock(-2));
            Fault("TryEnterWriteLock(-2ms)", () => rw.TryEnterWriteLock(m2));
            Fault("TryEnterUpgradeableReadLock(-2)", () => rw.TryEnterUpgradeableReadLock(-2));
            Fault("TryEnterUpgradeableReadLock(-2ms)", () => rw.TryEnterUpgradeableReadLock(m2));
            Console.WriteLine("held: " + rw.IsReadLockHeld + " " + rw.IsWriteLockHeld
                + " " + rw.IsUpgradeableReadLockHeld);
            rw.EnterReadLock();
            // The range check precedes the recursion verdict a held read lock draws.
            Fault("read->TryEnterReadLock(-2)", () => rw.TryEnterReadLock(-2));
            rw.ExitReadLock();
            Console.WriteLine("wait timeouts end");
            ReceiverAndFieldChecks();
        }

        static void ReceiverAndFieldChecks()
        {
            TimeSpan bad = TimeSpan.FromMilliseconds(-2);
            Thread? thread = null;
            SemaphoreSlim? semaphore = null;
            ManualResetEventSlim? reset = null;
            WaitHandle? handle = null;
            CountdownEvent? countdown = null;
            Barrier? barrier = null;
            ReaderWriterLockSlim? rw = null;
            Fault("null Join", () => thread!.Join(bad));
            Fault("null Semaphore", () => semaphore!.Wait(bad));
            Fault("null MRES", () => reset!.Wait(bad));
            Fault("null WaitOne", () => handle!.WaitOne(bad));
            Fault("null Countdown", () => countdown!.Wait(bad));
            Fault("null Barrier", () => barrier!.SignalAndWait(bad));
            Fault("null RWLock", () => rw!.TryEnterReadLock(bad));

            var done = new Thread(() => { });
            done.Start();
            done.Join();
            long[] ticks = { long.MinValue, -20000, -19999, -10000, -9999, 0,
                21474836479999L, 21474836480000L, 9007199254740999L, long.MaxValue };
            var available = new SemaphoreSlim(1);
            var single = new Barrier(1);
            foreach (long value in ticks)
            {
                TimeSpan span = new TimeSpan(value);
                Fault("Join ticks=" + value, () => done.Join(span));
                Fault("Semaphore ticks=" + value, () => available.Wait(span));
                if (available.CurrentCount == 0)
                    available.Release();
                Fault("Barrier ticks=" + value, () => single.SignalAndWait(span));
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int i = 0; i < s_saved.Length; i++)
            {
                Exception ex = s_saved[i];
                object? actual = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
                Console.WriteLine("timeout GC " + i + " type=" + ex.GetType().Name
                    + " param=" + (ex is ArgumentException argument ? argument.ParamName : null)
                    + " actual=" + (actual is null ? "null" : actual.GetType().Name + ":" + actual)
                    + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
            }
            Console.WriteLine("blocking timeout fields end");
        }
    }
}
