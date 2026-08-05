#nullable enable
using System;
using System.Threading.Tasks;

namespace ColdTaskSubset
{
    // `new Task(...)` / `new Task<T>(...)` cold construction: the ctor allocates a
    // pending task carrying the delegate; Start() submits it to the worker pool,
    // RunSynchronously() runs it inline on the calling thread. A second Start (or
    // Start on a promise-style task like Task.Run's) throws a catchable
    // InvalidOperationException; a fault inside RunSynchronously settles the task
    // FAULTED without throwing at the call site. All assertions are deterministic:
    // every started task is waited before its state is read.
    internal static class Program
    {
        private static async Task<long> AwaitCold()
        {
            var t = new Task<long>(() => 123L);
            t.Start();
            return await t;
        }

        // A cold Task<TStruct> — the value-type result does not fit the 8-byte result slot,
        // so the ctor stores a boxing trampoline; Start()/RunSynchronously() run it and
        // .Result / await read the boxed struct back.
        private struct Counts { public int N; public ulong Bytes; }

        private static async Task<(int, ulong)> AwaitColdTuple()
        {
            var t = new Task<(int, ulong)>(() => (11, 22UL));
            t.Start();
            return await t;
        }

        internal static void __GateEntry()
        {
            // Cold void task: not started -> PENDING; Start -> pool; Wait joins.
            int hit = 0;
            var cold = new Task(() => { hit = 1; });
            Console.WriteLine("cold: " + cold.IsCompleted);
            cold.Start();
            cold.Wait();
            Console.WriteLine("started: " + cold.IsCompletedSuccessfully + "," + hit);

            // Cold Task<int> with a result + a creation-options hint (ignored).
            var calc = new Task<int>(() => 6 * 7, TaskCreationOptions.LongRunning);
            calc.Start();
            Console.WriteLine("result: " + calc.Result);

            // State-carrying form: the object? argument reaches the delegate.
            string got = "";
            var stateful = new Task(s => { got = (string)s!; }, "payload");
            stateful.Start();
            stateful.Wait();
            Console.WriteLine("state: " + got);

            // RunSynchronously runs inline on this thread and settles the task.
            int inline = 0;
            var sync = new Task(() => { inline = 3; });
            sync.RunSynchronously();
            Console.WriteLine("sync: " + sync.IsCompletedSuccessfully + "," + inline);

            // A faulting RunSynchronously settles FAULTED, nothing thrown here.
            var boom = new Task(() => throw new InvalidOperationException("boom"));
            boom.RunSynchronously();
            Console.WriteLine("faulted: " + boom.IsFaulted);

            // Start twice / Start on a promise-style (Task.Run) task both throw.
            try { sync.Start(); }
            catch (InvalidOperationException) { Console.WriteLine("restart: InvalidOperationException"); }
            Task promise = Task.Run(() => { });
            promise.Wait();
            try { promise.Start(); }
            catch (InvalidOperationException) { Console.WriteLine("promise: InvalidOperationException"); }

            // An awaited cold Task<T> resumes like any pool task.
            Console.WriteLine("await: " + AwaitCold().Result);

            // Cold Task<(int, ulong)>: a struct/tuple result rides the boxing trampoline.
            var tuple = new Task<(int, ulong)>(() => (3, 9UL));
            tuple.Start();
            var (tn, tb) = tuple.Result;
            Console.WriteLine("tuple: " + tn + "," + tb);

            // A named struct result, run inline via RunSynchronously.
            var cs = new Task<Counts>(() => new Counts { N = 5, Bytes = 4096UL });
            cs.RunSynchronously();
            Console.WriteLine("struct: " + cs.Result.N + "," + cs.Result.Bytes);

            // Awaited cold tuple task through an async method.
            var at = AwaitColdTuple().Result;
            Console.WriteLine("awaittuple: " + at.Item1 + "," + at.Item2);
        }
    }
}
