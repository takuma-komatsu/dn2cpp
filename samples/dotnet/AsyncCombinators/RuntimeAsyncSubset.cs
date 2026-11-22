// Runtime-async methods (MethodImplAttributes.Async): Roslyn emits the body as
// synchronous IL around System.Runtime.CompilerServices.AsyncHelpers calls instead of a
// state machine. Each method here opts in by attribute, so the rest of the bucket keeps
// its compiler state machines. Covers the helpers these methods reach: Await over
// Task, Task<T>, ValueTask and ValueTask<T>; UnsafeAwaitAwaiter over Task.Yield; a
// value-type instance method, which works on a copy of its receiver; a generic
// method; and awaits inside a try with catch and finally handlers, one of them with
// a value on the evaluation stack. A configured Task or ValueTask await that declines
// the captured context resumes while its caller blocks with a timeout under a derived
// synchronization context.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    // Roslyn matches this attribute by name; the reference assemblies do not carry it.
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class RuntimeAsyncMethodGenerationAttribute : Attribute
    {
        public RuntimeAsyncMethodGenerationAttribute(bool runtimeAsync) { }
    }
}

namespace RuntimeAsyncSubset
{
    internal struct Counter
    {
        public int N;

        [RuntimeAsyncMethodGeneration(true)]
        public async Task<int> AddAsync(int d)
        {
            N += d;
            await Task.Yield();
            N += d;
            return N;
        }
    }

    internal static class Program
    {
        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitTasks(Task gate, Task<int> value)
        {
            await gate;
            return await value + 1;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async ValueTask<int> AwaitValueTasks(ValueTask gate, ValueTask<int> value)
        {
            await gate;
            return await value * 2;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<T> Echo<T>(T value)
        {
            await Task.Yield();
            return value;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitInTry(Task<int> value, Task fault)
        {
            int total = 1;
            try
            {
                total += await value;
                await fault;
            }
            catch (InvalidOperationException)
            {
                total *= 10;
            }
            finally
            {
                total += 5;
            }
            return total;
        }

        internal static void Run()
        {
            Console.WriteLine("== runtime async ==");
            var gate = new TaskCompletionSource();
            var value = new TaskCompletionSource<int>();
            Task<int> tasks = AwaitTasks(gate.Task, value.Task);
            ValueTask<int> valueTasks = AwaitValueTasks(new ValueTask(gate.Task), new ValueTask<int>(value.Task));
            Console.WriteLine("pending: " + tasks.IsCompleted + "/" + valueTasks.IsCompleted);
            gate.SetResult();
            value.SetResult(20);
            Task<int> settled = AwaitTasks(Task.CompletedTask, Task.FromResult(1));
            Console.WriteLine("tasks: " + tasks.Result + "/" + valueTasks.AsTask().Result
                + " settled: " + settled.IsCompleted + "/" + settled.Result);

            var counter = new Counter { N = 1 };
            Console.WriteLine("struct: " + counter.AddAsync(2).Result + "/" + counter.N);
            Console.WriteLine("generic: " + Echo(7).Result + "/" + Echo("text").Result + "/" + Echo(long.MaxValue).Result);
            var tryValue = new TaskCompletionSource<int>();
            var tryFault = new TaskCompletionSource();
            Task<int> guarded = AwaitInTry(tryValue.Task, tryFault.Task);
            bool guardedBefore = guarded.IsCompleted;
            tryValue.SetResult(3);
            tryFault.SetException(new InvalidOperationException("fault"));
            Console.WriteLine("try: " + guardedBefore + "/" + guarded.Result);
            Console.WriteLine("runtime async end");
        }

        // Drops posts: only a resumption that declines the captured context can run.
        private sealed class DroppingContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback callback, object state) { }
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<bool> ResumeTaskWithoutContext(Task input)
        {
            await input.ConfigureAwait(false);
            return SynchronizationContext.Current is null;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<bool> ResumeValueTaskWithoutContext(ValueTask input)
        {
            await input.ConfigureAwait(false);
            return SynchronizationContext.Current is null;
        }

        internal static void RunConfiguredContexts()
        {
            var timeout = TimeSpan.FromSeconds(8);
            SynchronizationContext previous = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DroppingContext());
                var source = new TaskCompletionSource();
                Task<bool> task = ResumeTaskWithoutContext(source.Task);
                Task<bool> valueTask = ResumeValueTaskWithoutContext(new ValueTask(source.Task));
                source.SetResult();
                Console.WriteLine("runtime async configured contexts: task=" + (task.Wait(timeout) && task.Result)
                    + " value task=" + (valueTask.Wait(timeout) && valueTask.Result));
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }
    }
}
