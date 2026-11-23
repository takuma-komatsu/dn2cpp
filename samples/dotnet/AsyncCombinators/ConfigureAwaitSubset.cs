#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigureAwaitSubset
{
    // Configured awaits preserve the result and fault of a pending or completed
    // task. An await with context capture disabled can resume while the caller
    // blocks with a custom synchronization context installed.
    internal static class Program
    {
        private static async Task<int> Slow(int v) { await Task.Delay(v); return v; }

        private static async Task<string> Run()
        {
            int a = await Slow(10).ConfigureAwait(false);          // suspends, resumes -> 10
            int b = await Slow(20).ConfigureAwait(true);           // -> 20
            await Task.Delay(5).ConfigureAwait(false);             // non-generic awaitable
            int c = await Task.FromResult(7).ConfigureAwait(false); // pre-completed -> 7

            bool caught = false;
            try
            {
                await Task.FromException<int>(new InvalidOperationException("x")).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                caught = true;
            }

            return a + "," + b + "," + c + "," + caught;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result);                       // 10,20,7,True
        }

        private sealed class RecordingContext : SynchronizationContext
        {
            private readonly Queue<(SendOrPostCallback Callback, object? State)> _pending = new();
            private int _posts;
            public int Posts => Volatile.Read(ref _posts);
            public bool NestedJoinCompleted { get; private set; }
            public Task? SourceJoin { private get; set; }
            public bool SourceJoinCompleted { get; private set; }

            public override void Post(SendOrPostCallback callback, object? state)
            {
                // Post may synchronously finish an independent join before returning.
                var nested = new TaskCompletionSource<int>();
                Task<int[]> joined = Task.WhenAll<int>(nested.Task);
                nested.SetResult(1);
                NestedJoinCompleted = joined.IsCompletedSuccessfully;
                SourceJoinCompleted = SourceJoin?.IsCompletedSuccessfully == true;

                lock (_pending)
                    _pending.Enqueue((callback, state));
                Interlocked.Increment(ref _posts);
            }

            public bool PumpOne()
            {
                (SendOrPostCallback Callback, object? State) work;
                lock (_pending)
                    if (!_pending.TryDequeue(out work))
                        return false;
                work.Callback(work.State);
                return true;
            }
        }

        private static async Task<bool> ResumeWithoutContext(Task input)
        {
            await input.ConfigureAwait(false);
            return SynchronizationContext.Current is null;
        }

        private static async Task<bool> YieldUnderBaseContext()
        {
            await Task.Yield();
            return SynchronizationContext.Current is null;
        }

        private static async Task<bool> YieldUnderContext(SynchronizationContext expected)
        {
            await Task.Yield();
            return ReferenceEquals(SynchronizationContext.Current, expected);
        }

        private static async Task<bool> AwaitUnderContext(Task<int> input, SynchronizationContext expected)
        {
            int result = await input;
            return result == 42 && ReferenceEquals(SynchronizationContext.Current, expected);
        }

        internal static void RunPumpedContext()
        {
            var recording = new RecordingContext();
            SynchronizationContext? previous = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(recording);
                Task<bool> yielded = YieldUnderContext(recording);
                bool yieldPending = !yielded.IsCompleted;
                bool yieldPumped = recording.PumpOne();
                Console.WriteLine($"context yield: {yieldPending}/{yieldPumped}/{yielded.IsCompletedSuccessfully && yielded.Result}");

                var source = new TaskCompletionSource<int>();
                Task<int[]> outerJoin = Task.WhenAll<int>(source.Task);
                recording.SourceJoin = outerJoin;
                Task<bool> awaited = AwaitUnderContext(source.Task, recording);
                bool awaitPending = !awaited.IsCompleted;
                SynchronizationContext.SetSynchronizationContext(null);
                source.SetResult(42);
                bool onlyPumped = !awaited.IsCompleted;
                SynchronizationContext.SetSynchronizationContext(recording);
                bool taskPumped = recording.PumpOne();
                Console.WriteLine($"context task: {awaitPending}/{onlyPumped}/{taskPumped}/{awaited.IsCompletedSuccessfully && awaited.Result}");
                bool outerSettled = outerJoin.IsCompletedSuccessfully && outerJoin.Result[0] == 42;
                Console.WriteLine($"context post nested join: {recording.NestedJoinCompleted}/{outerSettled}");
                Console.WriteLine($"context post source joined: {recording.SourceJoinCompleted}");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        internal static void RunTimedWaitContexts()
        {
            var timeout = TimeSpan.FromSeconds(8);
            var recording = new RecordingContext();
            SynchronizationContext? previous = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(recording);
                var source = new TaskCompletionSource<bool>();
                Task<bool> configured = ResumeWithoutContext(source.Task);
                source.SetResult(true);
                bool contextFree = configured.Wait(timeout) && configured.Result;
                Task<int> continuation = Task.CompletedTask.ContinueWith(_ => 7);
                bool continued = continuation.Wait(timeout) && continuation.Result == 7;

                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                Task<bool> yielded = YieldUnderBaseContext();
                bool resumed = yielded.Wait(timeout) && yielded.Result;
                Console.WriteLine("timed wait contexts: configured=" + contextFree + " continued=" + continued
                    + " base yield=" + resumed + " posts=" + recording.Posts);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }
    }
}
