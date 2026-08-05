#nullable enable
using System;
using System.Threading.Tasks;

namespace DelayOrderSubset
{
    // real Task.Delay timing/ordering. The cooperative scheduler runs a virtual
    // clock — concurrent delays complete in DURATION order (shortest first), not in
    // start order — deterministically and without real sleeping. Equal-duration ties
    // are unspecified in.NET, so every duration here is distinct. Covers the int-ms
    // and TimeSpan overloads and the cumulative clock across awaited delays.
    //
    // The durations are spaced far wider than the ordering needs. The oracle this is
    // diffed against is real .NET, whose timer callbacks run late under load; on a
    // busy machine a 10 ms spacing lets a 20 ms delay land before a 10 ms one, and
    // the gate fails on the side that is not under test. 100 ms steps were measured
    // NOT to be enough: a parallel suite (and the culture sweep, which runs this
    // oracle five times) skewed a 100 ms timer past a 200 ms one. Hence 400 ms steps
    // — the section pays ~2 s of wall clock to keep the assert off the machine's
    // load average.
    //
    // The appends are serialized under a lock: two timer continuations appending to
    // one string concurrently is a lost-update race on real .NET's thread pool (a
    // letter simply vanishes), which the deterministic single-threaded native side
    // can never reproduce — exactly the wrong side to be the flaky one.
    internal static class Program
    {
        private static readonly object s_gate = new object();
        private static string s_log = "";

        private static void Append(string id)
        {
            lock (s_gate)
            {
                s_log += id;
            }
        }

        private static async Task Work(string id, int ms)
        {
            await Task.Delay(ms);
            Append(id);
        }

        private static async Task WorkTs(string id, int ms)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(ms));
            Append(id);
        }

        private static async Task Run()
        {
            // Four concurrent delays with distinct durations -> completion order is by
            // duration: B(400) D(800) C(1200) A(1600).
            await Task.WhenAll(Work("A", 1600), Work("B", 400), Work("C", 1200), Work("D", 800));
            s_log += "|";

            // TimeSpan overload, distinct durations -> y(200) before x(600).
            await Task.WhenAll(WorkTs("x", 600), WorkTs("y", 200));
            s_log += "|";

            // A plain awaited delay still suspends and resumes (clock advances).
            await Task.Delay(100);
            s_log += "done";
        }

        internal static void __GateEntry()
        {
            Run().Wait();
            Console.WriteLine(s_log);                // BDCA|yx|done
        }
    }
}
