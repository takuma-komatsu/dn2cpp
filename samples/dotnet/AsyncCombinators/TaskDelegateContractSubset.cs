using System;
using System.Threading.Tasks;

namespace TaskDelegateContractSubset
{
    // The delegate contract of the RESULT-returning task producers — Task.Run,
    // Task.Factory.StartNew (stateless Func<TResult> and the stateful
    // Func<object, TResult> + state form) and the cold `new Task(...)` / `new Task<T>(...)`
    // constructors. Two halves, both invisible to a caller that only ever passes a fresh
    // single-cast lambda:
    //   * a null delegate is rejected SYNCHRONOUSLY, at the entry, before any Task
    //     exists — the throw must reach this frame, not a pool worker. Every constructor
    //     names "action", the Func ones included; Run/StartNew name "function".
    //   * a COMBINED delegate runs every handler front-to-back and the task's result is
    //     the last handler's — a struct result, whose trampoline walks the chain itself,
    //     included.
    // The handlers append to a field instead of printing, so nothing is written from a
    // pool thread and the output stays deterministic.
    internal static class Program
    {
        private static string s_log = "";

        private static int A(object state) { s_log += "A"; return 1; }
        private static int B(object state) { s_log += "B"; return 2; }
        private static int A0() { s_log += "A"; return 1; }
        private static int B0() { s_log += "B"; return 2; }

        private struct Pair
        {
            public int X;
            public int Y;
        }

        private static Pair SA() { s_log += "A"; return new Pair { X = 1, Y = 10 }; }
        private static Pair SB() { s_log += "B"; return new Pair { X = 2, Y = 20 }; }

        internal static void __GateEntry()
        {
            Console.WriteLine(Reject("StartNew-state", () =>
                Task.Factory.StartNew((Func<object, int>)null, (object)7)));
            Console.WriteLine(Reject("Run", () => Task.Run((Func<int>)null)));

            s_log = "";
            Func<object, int> stateful = A;
            stateful += B;
            int r1 = Task.Factory.StartNew(stateful, (object)7).Result;
            Console.WriteLine("StartNew-state multicast: " + s_log + ", " + r1);

            s_log = "";
            Func<int> stateless = A0;
            stateless += B0;
            int r2 = Task.Run(stateless).Result;
            Console.WriteLine("Run multicast: " + s_log + ", " + r2);

            Console.WriteLine(Reject("ctor-Action", () => new Task((Action)null)));
            Console.WriteLine(Reject("ctor-Action-state", () =>
                new Task((Action<object>)null, (object)7)));
            Console.WriteLine(Reject("ctor-Func", () => new Task<int>((Func<int>)null)));
            Console.WriteLine(Reject("ctor-Func-struct", () => new Task<Pair>((Func<Pair>)null)));

            Func<Pair> structHandlers = SA;
            structHandlers += SB;

            s_log = "";
            Pair p1 = Task.Run(structHandlers).Result;
            Console.WriteLine("Run struct multicast: " + s_log + ", " + p1.X + "/" + p1.Y);

            s_log = "";
            Pair p2 = Task.Factory.StartNew(structHandlers).Result;
            Console.WriteLine("StartNew struct multicast: " + s_log + ", " + p2.X + "/" + p2.Y);

            s_log = "";
            Task<Pair> cold = new Task<Pair>(structHandlers);
            cold.Start();
            Pair p3 = cold.Result;
            Console.WriteLine("cold struct multicast: " + s_log + ", " + p3.X + "/" + p3.Y);
        }

        // The submit MUST throw here; a returned task would mean the rejection happened
        // (or crashed) on a worker instead.
        private static string Reject(string what, Func<Task> submit)
        {
            try
            {
                submit();
                return what + " null: no-throw";
            }
            catch (ArgumentNullException ex)
            {
                return what + " null: " + ex.GetType().Name + ": " + ex.Message;
            }
        }
    }
}
