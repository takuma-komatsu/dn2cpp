using System;
using System.Threading;

namespace NullReceivers
{
    // A null receiver of the blocking primitives' instance members: Thread,
    // SemaphoreSlim, ManualResetEventSlim, AutoResetEvent, CountdownEvent, Barrier,
    // ReaderWriterLockSlim, Timer and ThreadLocal<T>. Each raises
    // NullReferenceException at the call, ahead of the member's own checks of a
    // timeout, a count or a TimeSpan that would reject the argument.
    internal static class Program
    {
        private static Thread? s_thread;
        private static EventWaitHandle? s_event;
        private static Microsoft.Win32.SafeHandles.SafeWaitHandle? s_safe;
        private static TimeProvider? s_provider;
        private static SemaphoreSlim? s_semaphore;
        private static ManualResetEventSlim? s_slim;
        private static AutoResetEvent? s_auto;
        private static CountdownEvent? s_countdown;
        private static Barrier? s_barrier;
        private static ReaderWriterLockSlim? s_rw;
        private static Timer? s_timer;
        private static ThreadLocal<int>? s_local;
        private static ThreadLocal<string>? s_localRef;

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
                text = ex.GetType().Name + ": " + ex.Message + actual;
            }
            Console.WriteLine(label + " -> " + text);
        }

        private static void Run(string label, Action run) => Fault(label, () =>
        {
            run();
            return "returned";
        });

        internal static void __GateEntry()
        {
            Console.WriteLine("== null receivers ==");
            TimeSpan m2 = TimeSpan.FromMilliseconds(-2);
            CancellationToken none = CancellationToken.None;

            Run("Thread.Start()", () => s_thread!.Start());
            Run("Thread.Start(object)", () => s_thread!.Start(1));
            Run("Thread.Join()", () => s_thread!.Join());
            Fault("Thread.Join(-2)", () => s_thread!.Join(-2));
            Fault("Thread.Join(-2ms)", () => s_thread!.Join(m2));
            Fault("Thread.Name", () => s_thread!.Name ?? "null");
            Run("Thread.Name=", () => s_thread!.Name = "x");
            Fault("Thread.IsAlive", () => s_thread!.IsAlive);
            Fault("Thread.IsBackground", () => s_thread!.IsBackground);
            Run("Thread.IsBackground=", () => s_thread!.IsBackground = true);
            Fault("Thread.ManagedThreadId", () => s_thread!.ManagedThreadId);

            Run("SemaphoreSlim.Wait()", () => s_semaphore!.Wait());
            Run("SemaphoreSlim.Wait(token)", () => s_semaphore!.Wait(none));
            Fault("SemaphoreSlim.Wait(-2)", () => s_semaphore!.Wait(-2));
            Fault("SemaphoreSlim.Wait(-2, token)", () => s_semaphore!.Wait(-2, none));
            Fault("SemaphoreSlim.Wait(-2ms)", () => s_semaphore!.Wait(m2));
            Fault("SemaphoreSlim.Wait(-2ms, token)", () => s_semaphore!.Wait(m2, none));
            Fault("SemaphoreSlim.Release()", () => s_semaphore!.Release());
            Fault("SemaphoreSlim.Release(0)", () => s_semaphore!.Release(0));
            Fault("SemaphoreSlim.CurrentCount", () => s_semaphore!.CurrentCount);
            Fault("SemaphoreSlim.WaitAsync()", () => s_semaphore!.WaitAsync());
            Run("SemaphoreSlim.Dispose()", () => s_semaphore!.Dispose());

            Run("ManualResetEventSlim.Set()", () => s_slim!.Set());
            Run("ManualResetEventSlim.Reset()", () => s_slim!.Reset());
            Run("ManualResetEventSlim.Wait()", () => s_slim!.Wait());
            Fault("ManualResetEventSlim.Wait(-2)", () => s_slim!.Wait(-2));
            Fault("ManualResetEventSlim.Wait(-2, token)", () => s_slim!.Wait(-2, none));
            Fault("ManualResetEventSlim.Wait(-2ms)", () => s_slim!.Wait(m2));
            Fault("ManualResetEventSlim.Wait(-2ms, token)", () => s_slim!.Wait(m2, none));
            Fault("ManualResetEventSlim.IsSet", () => s_slim!.IsSet);
            Run("ManualResetEventSlim.Dispose()", () => s_slim!.Dispose());

            Fault("AutoResetEvent.Set()", () => s_auto!.Set());
            Fault("AutoResetEvent.Reset()", () => s_auto!.Reset());
            Fault("AutoResetEvent.WaitOne()", () => s_auto!.WaitOne());
            Fault("AutoResetEvent.WaitOne(-2)", () => s_auto!.WaitOne(-2));
            Fault("AutoResetEvent.WaitOne(-2, exitContext)", () => s_auto!.WaitOne(-2, false));
            Fault("AutoResetEvent.WaitOne(-2ms)", () => s_auto!.WaitOne(m2));
            Fault("AutoResetEvent.WaitOne(-2ms, exitContext)", () => s_auto!.WaitOne(m2, false));
            Fault("AutoResetEvent.SafeWaitHandle", () => s_auto!.SafeWaitHandle);
            Run("AutoResetEvent.Close()", () => s_auto!.Close());
            Run("AutoResetEvent.Dispose()", () => s_auto!.Dispose());

            Fault("CountdownEvent.Signal()", () => s_countdown!.Signal());
            Fault("CountdownEvent.Signal(0)", () => s_countdown!.Signal(0));
            Run("CountdownEvent.AddCount()", () => s_countdown!.AddCount());
            Run("CountdownEvent.AddCount(-1)", () => s_countdown!.AddCount(-1));
            Fault("CountdownEvent.TryAddCount()", () => s_countdown!.TryAddCount());
            Fault("CountdownEvent.TryAddCount(-1)", () => s_countdown!.TryAddCount(-1));
            Run("CountdownEvent.Reset()", () => s_countdown!.Reset());
            Run("CountdownEvent.Reset(-1)", () => s_countdown!.Reset(-1));
            Run("CountdownEvent.Wait()", () => s_countdown!.Wait());
            Fault("CountdownEvent.Wait(-2)", () => s_countdown!.Wait(-2));
            Fault("CountdownEvent.Wait(-2, token)", () => s_countdown!.Wait(-2, none));
            Fault("CountdownEvent.Wait(-2ms)", () => s_countdown!.Wait(m2));
            Fault("CountdownEvent.Wait(-2ms, token)", () => s_countdown!.Wait(m2, none));
            Fault("CountdownEvent.CurrentCount", () => s_countdown!.CurrentCount);
            Fault("CountdownEvent.InitialCount", () => s_countdown!.InitialCount);
            Fault("CountdownEvent.IsSet", () => s_countdown!.IsSet);
            Run("CountdownEvent.Dispose()", () => s_countdown!.Dispose());

            Run("Barrier.SignalAndWait()", () => s_barrier!.SignalAndWait());
            Fault("Barrier.SignalAndWait(-2)", () => s_barrier!.SignalAndWait(-2));
            Fault("Barrier.SignalAndWait(-2, token)", () => s_barrier!.SignalAndWait(-2, none));
            Fault("Barrier.SignalAndWait(-2ms)", () => s_barrier!.SignalAndWait(m2));
            Fault("Barrier.SignalAndWait(-2ms, token)", () => s_barrier!.SignalAndWait(m2, none));
            Fault("Barrier.AddParticipant()", () => s_barrier!.AddParticipant());
            Fault("Barrier.AddParticipants(-1)", () => s_barrier!.AddParticipants(-1));
            Run("Barrier.RemoveParticipant()", () => s_barrier!.RemoveParticipant());
            Run("Barrier.RemoveParticipants(-1)", () => s_barrier!.RemoveParticipants(-1));
            Fault("Barrier.ParticipantCount", () => s_barrier!.ParticipantCount);
            Fault("Barrier.ParticipantsRemaining", () => s_barrier!.ParticipantsRemaining);
            Fault("Barrier.CurrentPhaseNumber", () => s_barrier!.CurrentPhaseNumber);
            Run("Barrier.Dispose()", () => s_barrier!.Dispose());

            Run("ReaderWriterLockSlim.EnterReadLock()", () => s_rw!.EnterReadLock());
            Run("ReaderWriterLockSlim.ExitReadLock()", () => s_rw!.ExitReadLock());
            Run("ReaderWriterLockSlim.EnterWriteLock()", () => s_rw!.EnterWriteLock());
            Run("ReaderWriterLockSlim.ExitWriteLock()", () => s_rw!.ExitWriteLock());
            Run("ReaderWriterLockSlim.EnterUpgradeableReadLock()", () => s_rw!.EnterUpgradeableReadLock());
            Run("ReaderWriterLockSlim.ExitUpgradeableReadLock()", () => s_rw!.ExitUpgradeableReadLock());
            Fault("ReaderWriterLockSlim.TryEnterReadLock(-2)", () => s_rw!.TryEnterReadLock(-2));
            Fault("ReaderWriterLockSlim.TryEnterReadLock(-2ms)", () => s_rw!.TryEnterReadLock(m2));
            Fault("ReaderWriterLockSlim.TryEnterWriteLock(-2)", () => s_rw!.TryEnterWriteLock(-2));
            Fault("ReaderWriterLockSlim.TryEnterWriteLock(-2ms)", () => s_rw!.TryEnterWriteLock(m2));
            Fault("ReaderWriterLockSlim.TryEnterUpgradeableReadLock(-2)",
                () => s_rw!.TryEnterUpgradeableReadLock(-2));
            Fault("ReaderWriterLockSlim.TryEnterUpgradeableReadLock(-2ms)",
                () => s_rw!.TryEnterUpgradeableReadLock(m2));
            Fault("ReaderWriterLockSlim.CurrentReadCount", () => s_rw!.CurrentReadCount);
            Fault("ReaderWriterLockSlim.WaitingWriteCount", () => s_rw!.WaitingWriteCount);
            Fault("ReaderWriterLockSlim.IsReadLockHeld", () => s_rw!.IsReadLockHeld);
            Fault("ReaderWriterLockSlim.IsWriteLockHeld", () => s_rw!.IsWriteLockHeld);
            Fault("ReaderWriterLockSlim.IsUpgradeableReadLockHeld", () => s_rw!.IsUpgradeableReadLockHeld);
            Fault("ReaderWriterLockSlim.RecursionPolicy", () => s_rw!.RecursionPolicy);
            Run("ReaderWriterLockSlim.Dispose()", () => s_rw!.Dispose());

            Fault("Timer.Change(-2, 0)", () => s_timer!.Change(-2, 0));
            Fault("Timer.Change(-2ms, 0ms)", () => s_timer!.Change(m2, TimeSpan.Zero));
            Run("Timer.Dispose()", () => s_timer!.Dispose());
            Fault("Timer.DisposeAsync()", () => s_timer!.DisposeAsync());

            Fault("ThreadLocal<int>.Value", () => s_local!.Value);
            Run("ThreadLocal<int>.Value=", () => s_local!.Value = 1);
            Fault("ThreadLocal<string>.Value", () => s_localRef!.Value ?? "null");
            Fault("ThreadLocal<int>.IsValueCreated", () => s_local!.IsValueCreated);
            Run("ThreadLocal<int>.Dispose()", () => s_local!.Dispose());
            Fault("EventWaitHandle.Set()", () => s_event!.Set());
            Fault("EventWaitHandle.Reset()", () => s_event!.Reset());
            Fault("SafeWaitHandle.IsInvalid", () => s_safe!.IsInvalid);
            Fault("SafeWaitHandle.DangerousGetHandle", () => s_safe!.DangerousGetHandle());
            Fault("TimeProvider.TimestampFrequency", () => s_provider!.TimestampFrequency);
            Fault("TimeProvider.CreateTimer", () => s_provider!.CreateTimer(null!, null, m2, m2));
            Console.WriteLine("null receivers end");
        }
    }
}
