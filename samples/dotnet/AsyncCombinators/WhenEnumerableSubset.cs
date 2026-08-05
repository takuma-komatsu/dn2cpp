#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WhenEnumerableSubset
{
    // Task.WhenAll / Task.WhenAny over a non-array IEnumerable<Task<T>> source
    // (a List, a LINQ result, …) — materialized by an inline interface-enumeration
    // loop into a ref array, then handed to the same cooperative join as the array
    // form. Also covers the non-generic Task.WhenAll (void result, no result array)
    // over both an IEnumerable and an array, including first-fault propagation.
    // WhenAny's winner is nondeterministic in general, so a pre-completed input
    // faces losers that CANNOT complete until the race is over; every other
    // assertion is order-independent.
    internal static class Program
    {
        private static async Task<int> Slow(int v) { await Task.Yield(); return v; }
        private static async Task Work() { await Task.Yield(); }

        // A losing task the driver holds shut. `await Task.Yield()` does NOT make a loser:
        // its continuation is queued to the thread pool, so the task may already be
        // complete by the time WhenAny walks the list and then IT wins — a race machine
        // load decides, i.e. green in isolation and red under a loaded batch.
        //
        // Hold the losers open rather than reordering the list. Putting the pre-completed
        // task first is also deterministic today, but only because
        // TaskFactory.CommonCWAnyLogic registers completion actions in list order and
        // stops at the first that fires — an implementation detail of one runtime. A task
        // with no completion path cannot win under any scheduler on either side.
        private static async Task<int> Held(Task<int> gate, int v) { await gate; return v; }

        private static async Task<string> Run()
        {
            // WhenAny over a List<Task<int>> with a pre-completed input -> 99 wins,
            // because 99 is the only member that is able to be complete.
            TaskCompletionSource<int> release = new();
            List<Task<int>> any = new() { Held(release.Task, 5), Task.FromResult(99), Held(release.Task, 7) };
            Task<int> w = await Task.WhenAny(any);
            int won = w.Result;                                       // 99
            release.SetResult(0);                                    // race over; let the losers finish
            int anySum = 0;
            foreach (var t in any) anySum += await t;                // 5+99+7 = 111

            // Generic WhenAll over a List<Task<int>> -> int[] result (sum-checked,
            // order-independent).
            List<Task<int>> all = new() { Slow(2), Slow(3), Slow(4) };
            int[] r = await Task.WhenAll(all);
            int allSum = r[0] + r[1] + r[2];                         // 9

            // Non-generic WhenAll (void) over a List<Task> and over a Task[].
            List<Task> voidList = new() { Work(), Work() };
            await Task.WhenAll(voidList);
            await Task.WhenAll(new Task[] { Work(), Work() });

            // Non-generic WhenAll fault propagation: a faulted input faults the join.
            bool caught = false;
            try
            {
                List<Task> bad = new() { Work(), Task.FromException(new InvalidOperationException("x")) };
                await Task.WhenAll(bad);
            }
            catch (InvalidOperationException)
            {
                caught = true;
            }

            // The .NET 9+ `params ReadOnlySpan<Task>` overload (3+ loose tasks): Roslyn
            // lowers them through an [InlineArray] of N tasks + a span over it. WhenAny's
            // winner is nondeterministic (the cooperative order need not match .NET's
            // thread pool), so only assert the winner is a valid member; WhenAll's
            // result set is order-independent.
            Task<int> sw = await Task.WhenAny(Slow(11), Slow(22), Slow(33));
            bool spanWinnerValid = sw.Result is 11 or 22 or 33;      // any input may win
            int[] sr = await Task.WhenAll(Slow(10), Slow(20), Slow(30));
            int spanSum = sr[0] + sr[1] + sr[2];                     // 60

            return won + "," + anySum + "," + r.Length + "," + allSum + "," + caught
                 + "," + spanWinnerValid + "," + spanSum;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result);                         // 99,111,3,9,True,True,60
        }
    }
}
