#nullable enable
using System;
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
            private int _posts;
            public int Posts => Volatile.Read(ref _posts);
            public override void Post(SendOrPostCallback callback, object? state)
                => Interlocked.Increment(ref _posts);
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
