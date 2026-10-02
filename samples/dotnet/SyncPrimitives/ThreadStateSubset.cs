using System;
using System.Threading;

namespace ThreadStates
{
    // Thread's lifecycle checks. The constructor refuses a null start delegate and a
    // negative maxStackSize; Join refuses a thread never started, after its timeout's
    // range check; Start refuses a thread already started, the current one included,
    // with ThreadStateException; and Start(object) refuses a ThreadStart body without
    // starting it. Start() hands a ParameterizedThreadStart body null, a thread joining
    // itself times out, and two threads may join one thread at once.
    internal static class Program
    {
        private static void Fault(string label, Func<object> run)
        {
            string text;
            try
            {
                text = run().ToString()!;
            }
            catch (Exception ex)
            {
                string actual = ex is ArgumentOutOfRangeException range
                    ? " [" + (range.ActualValue?.GetType().Name ?? "null") + "]"
                    : "";
                text = ex.GetType().Name + ": " + ex.Message + actual;
            }
            Console.WriteLine(label + " -> " + text);
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("== thread states ==");
            Fault("new Thread(null ThreadStart)", () => new Thread((ThreadStart)null!));
            Fault("new Thread(null ParameterizedThreadStart)",
                () => new Thread((ParameterizedThreadStart)null!));
            Fault("new Thread(body, -1)", () => new Thread(() => { }, -1));
            Fault("new Thread(null, -1)", () => new Thread((ThreadStart)null!, -1));
            Fault("new Thread(param body, -1)", () => new Thread(_ => { }, -1));
            Fault("new Thread(body, 0)", () => new Thread(() => { }, 0).IsAlive);

            var fresh = new Thread(() => { });
            Fault("unstarted Join()", () => { fresh.Join(); return "joined"; });
            Fault("unstarted Join(0)", () => fresh.Join(0));
            Fault("unstarted Join(-2)", () => fresh.Join(-2));
            Fault("unstarted Join(TimeSpan.Zero)", () => fresh.Join(TimeSpan.Zero));
            Fault("unstarted Join(-2ms)", () => fresh.Join(TimeSpan.FromMilliseconds(-2)));
            Fault("ThreadStart Start(object)", () => { fresh.Start("x"); return "started"; });
            Fault("ThreadStart Start(null)", () => { fresh.Start(null); return "started"; });
            fresh.Start();
            fresh.Join();
            Fault("second Start()", () => { fresh.Start(); return "started"; });
            Fault("second Start(object)", () => { fresh.Start("x"); return "started"; });
            Fault("Join() after exit", () => { fresh.Join(); return "joined"; });
            Fault("Join(0) after exit", () => fresh.Join(0));

            object? seen = "unset";
            var param = new Thread(o => seen = o);
            param.Start();
            param.Join();
            Console.WriteLine("ParameterizedThreadStart Start() -> " + (seen ?? "null"));
            Fault("ParameterizedThreadStart second Start(object)",
                () => { param.Start("x"); return "started"; });

            Thread self = Thread.CurrentThread;
            Fault("CurrentThread.Join(0)", () => self.Join(0));
            Fault("CurrentThread.Join(10ms)", () => self.Join(TimeSpan.FromMilliseconds(10)));
            Fault("CurrentThread.Start()", () => { self.Start(); return "started"; });
            Fault("CurrentThread.Start(object)", () => { self.Start("x"); return "started"; });

            bool? ownJoin = null;
            Thread? joiner = null;
            joiner = new Thread(() => ownJoin = joiner!.Join(5));
            joiner.Start();
            joiner.Join();
            Console.WriteLine("a thread's own Join(5) -> " + ownJoin);

            var slow = new Thread(() => Thread.Sleep(50));
            slow.Start();
            var first = new Thread(() => slow.Join());
            var second = new Thread(() => slow.Join());
            first.Start();
            second.Start();
            first.Join();
            second.Join();
            Console.WriteLine("two joiners done, alive: " + slow.IsAlive);

            try
            {
                fresh.Start();
            }
            catch (ThreadStateException ex)
            {
                Console.WriteLine("caught ThreadStateException, SystemException: "
                    + (ex is SystemException));
            }
            Console.WriteLine(new ThreadStateException().Message);
            Console.WriteLine("thread states end");
        }
    }
}
