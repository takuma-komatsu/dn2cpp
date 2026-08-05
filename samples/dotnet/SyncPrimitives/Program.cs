using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

// The blocking synchronization primitives — SemaphoreSlim, ManualResetEventSlim,
// AutoResetEvent, CountdownEvent, Barrier, ReaderWriterLockSlim and Monitor condition
// signaling — coordinating real threads. Every result is read after Join, which is what
// keeps the output deterministic.
static class Program
{
    static int s_consumed;
    static int s_released;
    static int s_turns;
    static int s_woken;
    static int s_signaled;
    static int s_barrierArrivals;
    static int s_phaseActions;
    static int s_rwCount;
    static int s_rwValidReads;

    const int N = 6;

    // Monitor condition-variable state for the broadcast and handoff sections.
    static readonly object s_mon = new object();
    static bool s_ready;
    static readonly Queue<int> s_queue = new Queue<int>();
    static bool s_producerDone;
    static int s_consumedSum;

    static void Main()
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        // SemaphoreSlim: N consumers each block on Wait(); the producer Releases N
        // tokens, freeing exactly N consumers.
        var sem = new SemaphoreSlim(0);
        var consumers = new Thread[N];
        for (int i = 0; i < N; i++)
        {
            consumers[i] = new Thread(() =>
            {
                sem.Wait();
                Interlocked.Increment(ref s_consumed);
            });
            consumers[i].Start();
        }
        sem.Release(N);
        for (int i = 0; i < N; i++) consumers[i].Join();
        Console.WriteLine(s_consumed);            // 6
        Console.WriteLine(sem.CurrentCount);      // 0

        // ManualResetEventSlim: N workers wait at a gate; one Set releases them all.
        var gate = new ManualResetEventSlim(false);
        Console.WriteLine(gate.IsSet);            // False
        var workers = new Thread[N];
        for (int i = 0; i < N; i++)
        {
            workers[i] = new Thread(() =>
            {
                gate.Wait();
                Interlocked.Increment(ref s_released);
            });
            workers[i].Start();
        }
        gate.Set();
        for (int i = 0; i < N; i++) workers[i].Join();
        Console.WriteLine(gate.IsSet);            // True
        Console.WriteLine(s_released);            // 6

        // AutoResetEvent: strict ping-pong between two threads (3 turns each).
        var ping = new AutoResetEvent(true);      // ping starts; pong waits
        var pong = new AutoResetEvent(false);
        var a = new Thread(() =>
        {
            for (int i = 0; i < 3; i++) { ping.WaitOne(); Interlocked.Increment(ref s_turns); pong.Set(); }
        });
        var b = new Thread(() =>
        {
            for (int i = 0; i < 3; i++) { pong.WaitOne(); Interlocked.Increment(ref s_turns); ping.Set(); }
        });
        a.Start();
        b.Start();
        a.Join();
        b.Join();
        Console.WriteLine(s_turns);               // 6

        // Monitor.PulseAll broadcast. The while(!s_ready) guard is what makes this
        // correct whether a worker reaches Wait before or after the PulseAll.
        var waiters = new Thread[N];
        for (int i = 0; i < N; i++)
        {
            waiters[i] = new Thread(() =>
            {
                lock (s_mon)
                {
                    while (!s_ready) Monitor.Wait(s_mon);
                }
                Interlocked.Increment(ref s_woken);
            });
            waiters[i].Start();
        }
        lock (s_mon)
        {
            s_ready = true;
            Monitor.PulseAll(s_mon);
        }
        for (int i = 0; i < N; i++) waiters[i].Join();
        Console.WriteLine(s_woken);               // 6

        // Monitor.Pulse handoff: a producer/consumer over a shared queue. The consumer
        // drains until the producer signals completion, so the sum is fully determined.
        const int items = 10;
        var consumer = new Thread(() =>
        {
            while (true)
            {
                int value;
                lock (s_mon)
                {
                    while (s_queue.Count == 0 && !s_producerDone) Monitor.Wait(s_mon);
                    if (s_queue.Count == 0 && s_producerDone) break;
                    value = s_queue.Dequeue();
                }
                s_consumedSum += value;
            }
        });
        var producer = new Thread(() =>
        {
            for (int i = 1; i <= items; i++)
            {
                lock (s_mon)
                {
                    s_queue.Enqueue(i);
                    Monitor.Pulse(s_mon);
                }
            }
            lock (s_mon)
            {
                s_producerDone = true;
                Monitor.Pulse(s_mon);
            }
        });
        consumer.Start();
        producer.Start();
        producer.Join();
        consumer.Join();
        Console.WriteLine(s_consumedSum);         // 55 (1+2+...+10)

        // CountdownEvent: N workers each Signal() once; main Wait()s to zero, after
        // which IsSet/CurrentCount are deterministic.
        var cde = new CountdownEvent(N);
        var signalers = new Thread[N];
        for (int i = 0; i < N; i++)
        {
            signalers[i] = new Thread(() =>
            {
                cde.Signal();
                Interlocked.Increment(ref s_signaled);
            });
            signalers[i].Start();
        }
        cde.Wait();
        for (int i = 0; i < N; i++) signalers[i].Join();
        Console.WriteLine(cde.IsSet);             // True
        Console.WriteLine(cde.CurrentCount);      // 0
        Console.WriteLine(s_signaled);            // 6

        // CountdownEvent AddCount / TryAddCount / Reset / Signal(n), single-threaded.
        var cde2 = new CountdownEvent(1);
        Console.WriteLine(cde2.InitialCount);     // 1
        cde2.AddCount(2);
        Console.WriteLine(cde2.CurrentCount);     // 3
        Console.WriteLine(cde2.Signal(3));        // True (count -> 0)
        Console.WriteLine(cde2.IsSet);            // True
        cde2.Reset();                             // back to InitialCount
        Console.WriteLine(cde2.CurrentCount);     // 1
        Console.WriteLine(cde2.IsSet);            // False
        cde2.Reset(5);                            // current + initial = 5
        Console.WriteLine(cde2.CurrentCount);     // 5
        Console.WriteLine(cde2.InitialCount);     // 5
        Console.WriteLine(cde2.TryAddCount());    // True (not yet set)
        Console.WriteLine(cde2.CurrentCount);     // 6

        // Barrier: N threads run BP phases of SignalAndWait(). After Join the arrival
        // total (N*BP) and the phase number are fixed.
        const int BP = 4;
        var barrier = new Barrier(N);
        var phasers = new Thread[N];
        for (int i = 0; i < N; i++)
        {
            phasers[i] = new Thread(() =>
            {
                for (int p = 0; p < BP; p++)
                {
                    Interlocked.Increment(ref s_barrierArrivals);
                    barrier.SignalAndWait();
                }
            });
            phasers[i].Start();
        }
        for (int i = 0; i < N; i++) phasers[i].Join();
        Console.WriteLine(s_barrierArrivals);     // 24 (N * BP)
        Console.WriteLine(barrier.CurrentPhaseNumber); // 4
        Console.WriteLine(barrier.ParticipantCount);   // 6

        // Barrier with a post-phase action: it runs once per completed phase, on the
        // last arriver, so after Join it has run exactly BP times.
        var barrier2 = new Barrier(N, _ => Interlocked.Increment(ref s_phaseActions));
        var phasers2 = new Thread[N];
        for (int i = 0; i < N; i++)
        {
            phasers2[i] = new Thread(() =>
            {
                for (int p = 0; p < BP; p++) barrier2.SignalAndWait();
            });
            phasers2[i].Start();
        }
        for (int i = 0; i < N; i++) phasers2[i].Join();
        Console.WriteLine(s_phaseActions);        // 4 (one post-phase action per phase)
        Console.WriteLine(barrier2.CurrentPhaseNumber); // 4

        // Blocking timeout overloads. Two flavors keep the booleans deterministic:
        // zero-wall-clock immediate checks, and real-timeout-false cases with margins
        // wide enough (a 100ms wait vs a 3000ms sleeper, no pulser) that the answer
        // cannot flip between runs.

        // SemaphoreSlim.Wait(int): empty -> false at once; Release then -> true.
        var tsem = new SemaphoreSlim(0);
        Console.WriteLine(tsem.Wait(0));          // False
        tsem.Release(1);
        Console.WriteLine(tsem.Wait(0));          // True

        // ManualResetEventSlim.Wait(int): unset -> false; Set then -> true.
        var tmre = new ManualResetEventSlim(false);
        Console.WriteLine(tmre.Wait(0));          // False
        tmre.Set();
        Console.WriteLine(tmre.Wait(0));          // True

        // AutoResetEvent.WaitOne(int): unset -> false; Set then -> true (consumed).
        var tare = new AutoResetEvent(false);
        Console.WriteLine(tare.WaitOne(0));       // False
        tare.Set();
        Console.WriteLine(tare.WaitOne(0));       // True

        // Monitor.TryEnter(obj, int) on a free lock -> true; recursive re-enter -> true.
        var tlock = new object();
        bool got = Monitor.TryEnter(tlock, 0);
        Console.WriteLine(got);                   // True
        Console.WriteLine(Monitor.TryEnter(tlock, 0)); // True (recursive re-entry)
        Monitor.Exit(tlock);
        Monitor.Exit(tlock);

        // TimeSpan overloads drive the same timed paths (a zero span is an immediate check).
        var tsem2 = new SemaphoreSlim(0);
        Console.WriteLine(tsem2.Wait(TimeSpan.Zero));               // False
        tsem2.Release(1);
        Console.WriteLine(tsem2.Wait(TimeSpan.FromMilliseconds(0))); // True

        // A thread holds tmon and parks in Monitor.Wait(tmon, 100) with no pulser, so
        // the wait returns false after ~100ms.
        object tmon = new object();
        bool monTimedOut = false;
        var monThread = new Thread(() =>
        {
            lock (tmon)
            {
                monTimedOut = !Monitor.Wait(tmon, 100);
            }
        });
        monThread.Start();
        monThread.Join();
        Console.WriteLine(monTimedOut);           // True (Wait timed out -> returned false)

        // Join(100) on a thread sleeping ~3000ms is false; a plain Join() then reaps it.
        var sleeper = new Thread(() => Thread.Sleep(3000));
        sleeper.Start();
        bool joined = sleeper.Join(100);
        sleeper.Join();                           // clean up (waits out the sleep)
        Console.WriteLine(joined);                // False (did not finish within 100ms)

        // ReaderWriterLockSlim: the write lock serializes N writers, so the final count is
        // exactly N*RWWrites regardless of interleaving. Concurrent readers snapshot under
        // the read lock, and every snapshot lies in [0, N*RWWrites], so the valid-read
        // count is fixed too.
        const int RWWrites = 50;
        var rw = new ReaderWriterLockSlim();
        var rwWriters = new Thread[N];
        for (int i = 0; i < N; i++)
        {
            rwWriters[i] = new Thread(() =>
            {
                for (int k = 0; k < RWWrites; k++)
                {
                    rw.EnterWriteLock();
                    s_rwCount++;
                    rw.ExitWriteLock();
                }
            });
            rwWriters[i].Start();
        }
        var rwReaders = new Thread[N];
        for (int i = 0; i < N; i++)
        {
            rwReaders[i] = new Thread(() =>
            {
                for (int k = 0; k < RWWrites; k++)
                {
                    rw.EnterReadLock();
                    int snapshot = s_rwCount; // a consistent (non-torn) read under the shared lock
                    rw.ExitReadLock();
                    if (snapshot >= 0 && snapshot <= N * RWWrites)
                        Interlocked.Increment(ref s_rwValidReads);
                }
            });
            rwReaders[i].Start();
        }
        for (int i = 0; i < N; i++) rwWriters[i].Join();
        for (int i = 0; i < N; i++) rwReaders[i].Join();
        Console.WriteLine(s_rwCount);             // 300 (N * RWWrites)
        Console.WriteLine(s_rwValidReads);        // 300 (every snapshot in range)
        Console.WriteLine(rw.CurrentReadCount);   // 0 (no readers held after Join)

        // TryEnterWriteLock(0) is false while another thread holds the write lock; the
        // event handshake is what fixes the ordering. Once released, TryEnter succeeds
        // for both read and write.
        var rw2 = new ReaderWriterLockSlim();
        var held = new ManualResetEventSlim(false);     // set once the helper holds the write lock
        var release = new ManualResetEventSlim(false);  // set by main to let the helper release
        bool tryWriteWhileHeld = true;
        var holder = new Thread(() =>
        {
            rw2.EnterWriteLock();
            held.Set();          // signal: the write lock is now held
            release.Wait();      // wait until main has attempted its TryEnter
            rw2.ExitWriteLock();
        });
        holder.Start();
        held.Wait();                                  // ensure the write lock is held first
        tryWriteWhileHeld = rw2.TryEnterWriteLock(0); // false: a writer holds it
        release.Set();                                // let the holder release
        holder.Join();
        Console.WriteLine(tryWriteWhileHeld);     // False
        bool tryReadFree = rw2.TryEnterReadLock(0);
        Console.WriteLine(tryReadFree);           // True (lock now free)
        rw2.ExitReadLock();
        bool tryWriteFree = rw2.TryEnterWriteLock(0);
        Console.WriteLine(tryWriteFree);          // True (lock now free)
        rw2.ExitWriteLock();

        // MethodImplAttribute: Synchronized bodies and the inlining hints.
        MethodImplSubset.Program.Run();

        // SynchronizationContext.Current as a real per-thread slot.
        SyncContextSubset.Program.Run();

        // The IDisposable interface mouth on the intrinsic primitives.
        DisposableIntrinsics.Program.__GateEntry();

        // ReaderWriterLockSlim per-thread ownership and the recursion matrix.
        RwLockRecursion.Program.__GateEntry();
    }
}
