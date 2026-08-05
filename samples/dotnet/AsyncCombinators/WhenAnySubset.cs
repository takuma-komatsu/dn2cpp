#nullable enable
using System;
using System.Threading.Tasks;

namespace WhenAnySubset
{
    // Task.WhenAny — a Task<Task>/Task<Task<T>> completing with the FIRST
    // input task to finish (the cooperative scheduler picks it; WhenAny itself
    // never faults — the winner's own fault surfaces via its result)..NET's
    // WhenAny winner is nondeterministic in general, so every assertion here is
    // order-independent: a pre-completed input deterministically wins, and the
    // post-await full result set / membership checks don't depend on which
    // pending task happens to finish first.
    internal static class Program
    {
        private static async Task<int> Slow(int v) { await Task.Yield(); return v; }

        private static async Task<string> Run()
        {
            // A pre-completed input always wins WhenAny (deterministic value).
            Task<int> quick = Task.FromResult(99);
            Task<int> slow = Slow(1);
            Task<int> w = await Task.WhenAny(quick, slow);
            int won = w.Result;                       // 99
            await slow;                               // drain the loser

            // Generic WhenAny over an array, then await all -> full result set is
            // order-independent (sorted-equivalent via a fixed sum).
            Task<int>[] arr = { Slow(10), Slow(20), Slow(30) };
            Task<int> first = await Task.WhenAny(arr);
            bool firstValid = first.Result is 10 or 20 or 30;
            int total = (await arr[0]) + (await arr[1]) + (await arr[2]);  // 60

            // Non-generic WhenAny(Task, Task): the returned Task is one of the inputs.
            Task ta = Slow(7), tb = Slow(8);
            Task done = await Task.WhenAny(ta, tb);
            bool winnerIsInput = done == ta || done == tb;
            await ta; await tb;

            return won + "," + firstValid + "," + total + "," + winnerIsInput;
        }

        // WhenAny's argument validation is CATCHABLE, not an abort: an empty array is
        // ArgumentException, a null array and a null element are both
        // ArgumentNullException. An empty task list is ordinary bad input, so aborting
        // the process here would crash a caller that merely got an empty collection.
        //
        // Every probe re-enters WhenAny afterwards with a good list: the point is
        // that the combinator still WORKS after a rejection, which is exactly
        // what an abort cannot demonstrate. Type names only; the messages are
        // localized. (No trick is needed to keep the empty-array call alive —
        // the length test is in the C++ runtime helper, not at the call site.)
        private static async Task<string> Rejects()
        {
            string log = "";

            try
            {
                await Task.WhenAny(new Task[0]);
                log += "empty:none;";
            }
            catch (ArgumentNullException) { log += "empty:ANE;"; }
            catch (ArgumentException) { log += "empty:AE;"; }

            try
            {
                await Task.WhenAny((Task[])null);
                log += "null:none;";
            }
            catch (ArgumentNullException) { log += "null:ANE;"; }
            catch (ArgumentException) { log += "null:AE;"; }

            try
            {
                await Task.WhenAny(new Task[] { Task.CompletedTask, null });
                log += "elem:none;";
            }
            catch (ArgumentNullException) { log += "elem:ANE;"; }
            catch (ArgumentException) { log += "elem:AE;"; }

            // Still usable after three rejections.
            Task<int> alive = await Task.WhenAny(new[] { Slow(5) });
            log += "alive:" + alive.Result;
            return log;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result);          // 99,True,60,True
            Console.WriteLine(Rejects().Result);      // empty:AE;null:ANE;elem:ANE;alive:5
        }
    }
}
