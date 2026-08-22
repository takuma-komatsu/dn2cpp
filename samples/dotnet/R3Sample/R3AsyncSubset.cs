using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace R3AsyncSubset
{
    internal static class Program
    {
        internal static void __GateEntry()
        {
            OneShotTimer();
            BufferedAsyncEnumerable();
            CancelledAsyncEnumerable();
            PendingReadResumedCrossThread();
            PendingReadCancelledCrossThread();
            PendingEarlySyncRead();
        }

        private static void OneShotTimer()
        {
            using var completed = new CountdownEvent(1);
            int nextCount = 0;
            bool success = false;
            var subscription = Observable
                .Timer(TimeSpan.FromMilliseconds(30))
                .Subscribe(
                    _ => Interlocked.Increment(ref nextCount),
                    e => Console.WriteLine("[timer error] " + e.Message),
                    result =>
                    {
                        success = result.IsSuccess;
                        completed.Signal();
                    });

            completed.Wait();
            subscription.Dispose();
            Console.WriteLine("[timer] next=" + nextCount + " success=" + success + " disposed=True");
        }

        private static void BufferedAsyncEnumerable()
        {
            using var subject = new Subject<int>();
            var enumerator = subject.ToAsyncEnumerable().GetAsyncEnumerator();

            subject.OnNext(1);
            subject.OnNext(10);
            subject.OnNext(100);
            subject.OnCompleted();

            int count = 0;
            int sum = 0;
            while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
            {
                count++;
                sum += enumerator.Current;
            }
            enumerator.DisposeAsync().GetAwaiter().GetResult();
            Console.WriteLine("[async buffered] count=" + count + " sum=" + sum + " completed=True");
        }

        private static void CancelledAsyncEnumerable()
        {
            using var subject = new Subject<int>();
            using var cancellation = new CancellationTokenSource();
            int upstreamDisposes = 0;
            var enumerator = subject
                .Do(onDispose: () => upstreamDisposes++)
                .ToAsyncEnumerable(cancellation.Token)
                .GetAsyncEnumerator();

            subject.OnNext(7);
            bool first = enumerator.MoveNextAsync().GetAwaiter().GetResult();
            int value = enumerator.Current;
            cancellation.Cancel();

            bool cancelled = false;
            try
            {
                enumerator.MoveNextAsync().GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            bool disposedByCancellation = upstreamDisposes == 1;
            enumerator.DisposeAsync().GetAwaiter().GetResult();
            Console.WriteLine("[async cancel] first=" + first + ":" + value
                + " cancelled=" + cancelled + " cancel-disposed=" + disposedByCancellation
                + " dispose-stable=" + (upstreamDisposes == 1));
        }

        // Both sections above publish/cancel BEFORE the first MoveNextAsync, so only the
        // already-completed ValueTask fast path ever ran. These read first, then publish
        // from another thread — forcing OnCompleted's registered-continuation path and, in
        // the cancel case, TrySetCanceled on an outstanding read.

        private static void PendingReadResumedCrossThread() =>
            PendingReadResumedCrossThreadAsync().GetAwaiter().GetResult();

        private static async Task PendingReadResumedCrossThreadAsync()
        {
            using var subject = new Subject<int>();
            var enumerator = subject.ToAsyncEnumerable().GetAsyncEnumerator();

            ValueTask<bool> pending = enumerator.MoveNextAsync();
            // Read before queueing: proves the pending arm was taken, not the completed one.
            bool pendingBeforePublish = pending.IsCompleted;
            ThreadPool.QueueUserWorkItem(_ => { Thread.Sleep(5); subject.OnNext(41); });
            bool moved = await pending;
            int value = enumerator.Current;

            ValueTask<bool> tail = enumerator.MoveNextAsync();
            bool pendingBeforeComplete = tail.IsCompleted;
            ThreadPool.QueueUserWorkItem(_ => { Thread.Sleep(5); subject.OnCompleted(); });
            bool ended = await tail; // false: sequence end, not a value

            await enumerator.DisposeAsync();
            Console.WriteLine("[async pending] beforePublish=" + pendingBeforePublish + " moved=" + moved
                + " value=" + value + " beforeComplete=" + pendingBeforeComplete + " ended=" + ended);
        }

        private static void PendingReadCancelledCrossThread() =>
            PendingReadCancelledCrossThreadAsync().GetAwaiter().GetResult();

        private static async Task PendingReadCancelledCrossThreadAsync()
        {
            using var subject = new Subject<int>();
            using var cancellation = new CancellationTokenSource();
            int upstreamDisposes = 0;
            var enumerator = subject
                .Do(onDispose: () => upstreamDisposes++)
                .ToAsyncEnumerable(cancellation.Token)
                .GetAsyncEnumerator();

            ValueTask<bool> outstanding = enumerator.MoveNextAsync();
            bool pendingBeforeCancel = outstanding.IsCompleted;
            var cancelReturned = new TaskCompletionSource<int>();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(5);
                cancellation.Cancel();
                cancelReturned.SetResult(1);
            });

            bool cancelled = false;
            try
            {
                await outstanding;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            // Cancel callbacks run LIFO: the read's own callback can resume `outstanding`
            // before the subscription-dispose callback registered ahead of it has run.
            await cancelReturned.Task;

            await enumerator.DisposeAsync();
            Console.WriteLine("[async pending cancel] beforeCancel=" + pendingBeforeCancel
                + " cancelled=" + cancelled + " dispose-stable=" + (upstreamDisposes == 1));
        }

        // The two sections above AWAIT their pending read. This one reads it
        // SYNCHRONOUSLY while still pending, which a source-backed ValueTask refuses
        // rather than waiting for: the source's own GetResult raises, so the message is
        // whichever that source type mints. The refused operation stays live — the same
        // ValueTask must still complete under a later await — and the two sources answer
        // differently, so both are here: the channel behind R3's ToAsyncEnumerable, and
        // an async iterator's ManualResetValueTaskSourceCore promise.
        private static void PendingEarlySyncRead() =>
            PendingEarlySyncReadAsync().GetAwaiter().GetResult();

        private static async IAsyncEnumerable<int> OneAfterSignal(Task signal)
        {
            await signal;
            yield return 11;
        }

        private static async Task PendingEarlySyncReadAsync()
        {
            using var subject = new Subject<int>();
            var enumerator = subject.ToAsyncEnumerable().GetAsyncEnumerator();

            ValueTask<bool> pending = enumerator.MoveNextAsync();
            bool pendingBeforeRead = pending.IsCompleted;
            string channelRefusal = "none";
            try
            {
                pending.GetAwaiter().GetResult();
            }
            catch (InvalidOperationException e)
            {
                channelRefusal = e.Message;
            }
            ThreadPool.QueueUserWorkItem(_ => { Thread.Sleep(5); subject.OnNext(7); });
            bool moved = await pending;
            int value = enumerator.Current;
            await enumerator.DisposeAsync();
            Console.WriteLine("[early read channel] before=" + pendingBeforeRead
                + " refused=" + channelRefusal + " moved=" + moved + " value=" + value);

            var gate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var iterator = OneAfterSignal(gate.Task).GetAsyncEnumerator();

            ValueTask<bool> pendingIterator = iterator.MoveNextAsync();
            bool iteratorBeforeRead = pendingIterator.IsCompleted;
            string promiseRefusal = "none";
            try
            {
                pendingIterator.GetAwaiter().GetResult();
            }
            catch (InvalidOperationException e)
            {
                promiseRefusal = e.Message;
            }
            ThreadPool.QueueUserWorkItem(_ => { Thread.Sleep(5); gate.SetResult(1); });
            bool movedIterator = await pendingIterator;
            int iteratorValue = iterator.Current;
            await iterator.DisposeAsync();
            Console.WriteLine("[early read promise] before=" + iteratorBeforeRead
                + " refused=" + promiseRefusal + " moved=" + movedIterator
                + " value=" + iteratorValue);
        }
    }
}
