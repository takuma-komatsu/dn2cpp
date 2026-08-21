using System;
using System.Threading;
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
    }
}
