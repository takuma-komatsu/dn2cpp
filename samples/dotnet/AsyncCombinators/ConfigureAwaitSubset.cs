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

        private static string ConfiguredResult(Task input, ConfigureAwaitOptions options)
        {
            try
            {
                input.ConfigureAwait(options).GetAwaiter().GetResult();
                return "ok";
            }
            catch (Exception e)
            {
                return e.GetType().Name;
            }
        }

        private static string RejectedOptions(Action configure)
        {
            try
            {
                configure();
                return "accepted";
            }
            catch (ArgumentOutOfRangeException e)
            {
                return e.ParamName + ": " + e.Message;
            }
        }

        private static async Task<bool> AwaitOptions(Task input, ConfigureAwaitOptions options, SynchronizationContext? expected)
        {
            await input.ConfigureAwait(options);
            return ReferenceEquals(SynchronizationContext.Current, expected);
        }

        private static bool PumpConfiguredCallback(RecordingContext context, bool unsafeRegistration)
        {
            bool called = false;
            var source = new TaskCompletionSource<int>();
            var awaiter = source.Task.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext).GetAwaiter();
            if (unsafeRegistration)
                awaiter.UnsafeOnCompleted(() => called = true);
            else
                awaiter.OnCompleted(() => called = true);
            SynchronizationContext.SetSynchronizationContext(null);
            source.SetResult(17);
            SynchronizationContext.SetSynchronizationContext(context);
            return !called && context.PumpOne() && called;
        }

        private static async Task<bool> AwaitForcedReady(int submitter)
        {
            int value = await Task.FromResult(17).ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            return value == 17 && Environment.CurrentManagedThreadId != submitter;
        }

        private static void RunOptionsAfterThreadExit()
        {
            var direct = new TaskCompletionSource<bool>();
            Task<bool>? awaited = null;
            var worker = new Thread(() =>
            {
                int submitter = Environment.CurrentManagedThreadId;
                Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding).GetAwaiter()
                    .UnsafeOnCompleted(() => direct.SetResult(Environment.CurrentManagedThreadId != submitter));
                awaited = AwaitForcedReady(submitter);
            });
            worker.Start();
            worker.Join();

            // No one pumps the submitting thread's scheduler after it exits.
            var state = awaited!;
            Task.WhenAll((Task)direct.Task, state).Wait(TimeSpan.FromSeconds(8));
            Console.WriteLine("configured options after thread exit: "
                + (direct.Task.IsCompletedSuccessfully && direct.Task.Result) + "/"
                + (state.IsCompletedSuccessfully && state.Result));
        }

        internal static void RunOptions()
        {
            const ConfigureAwaitOptions force = ConfigureAwaitOptions.ForceYielding;
            const ConfigureAwaitOptions suppress = ConfigureAwaitOptions.SuppressThrowing;
            const ConfigureAwaitOptions capture = ConfigureAwaitOptions.ContinueOnCapturedContext;
            Task faulted = Task.FromException<int>(new InvalidOperationException("configured fault"));
            Task canceled = Task.FromCanceled(new CancellationToken(true));
            var normal = Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.None).GetAwaiter();
            var forced = Task.CompletedTask.ConfigureAwait(force).GetAwaiter();
            var typed = Task.FromResult(17).ConfigureAwait(force | capture).GetAwaiter();
            Console.WriteLine($"configured options ready: {normal.IsCompleted}/{forced.IsCompleted}/{typed.IsCompleted}/{typed.GetResult()}");
            Console.WriteLine($"configured options result: {ConfiguredResult(faulted, suppress)}/{ConfiguredResult(canceled, suppress)}"
                + $"/{ConfiguredResult(faulted, ConfigureAwaitOptions.None)}/{ConfiguredResult(canceled, ConfigureAwaitOptions.None)}");
            Console.WriteLine("configured options invalid: " + RejectedOptions(() => Task.CompletedTask.ConfigureAwait((ConfigureAwaitOptions)8)));
#pragma warning disable CA2261 // Prove the generic overload rejects the option.
            Console.WriteLine("configured options generic invalid: " + RejectedOptions(() => Task.FromResult(17).ConfigureAwait(suppress | force)));
#pragma warning restore CA2261

            var context = new RecordingContext();
            var previous = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(context);
                var forcedState = AwaitOptions(faulted, force | suppress | capture, context);
                bool suspended = !forcedState.IsCompleted;
                bool pumped = context.PumpOne();
                bool resumed = forcedState.IsCompletedSuccessfully && forcedState.Result;
                var source = new TaskCompletionSource<bool>();
                var bypassState = AwaitOptions(source.Task, ConfigureAwaitOptions.None, null);
                source.SetResult(true);
                bool bypassed = bypassState.Wait(TimeSpan.FromSeconds(8)) && bypassState.Result;
                Console.WriteLine($"configured options state: {suspended}/{pumped}/{resumed}/{bypassed}");

                var completion = new TaskCompletionSource<bool>();
                Task.CompletedTask.ConfigureAwait(force).GetAwaiter().UnsafeOnCompleted(
                    () => completion.SetResult(SynchronizationContext.Current is null));
                bool directBypass = completion.Task.Wait(TimeSpan.FromSeconds(8)) && completion.Task.Result;
                bool safe = PumpConfiguredCallback(context, unsafeRegistration: false);
                bool unsafeCall = PumpConfiguredCallback(context, unsafeRegistration: true);
                Console.WriteLine($"configured options callbacks: {safe}/{unsafeCall}/{directBypass}");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
            RunOptionsAfterThreadExit();
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
