using System;
using System.Threading.Tasks;

namespace TaskDelegateContractSubset
{
    // The delegate contract shared by Task.Run and Task.Factory.StartNew, on the
    // RESULT-returning overloads (stateless Func<TResult> and the stateful
    // Func<object, TResult> + state form). Two halves, both invisible to a caller that
    // only ever passes a fresh single-cast lambda:
    //   * a null delegate is rejected SYNCHRONOUSLY, at the entry, before any Task
    //     exists — the throw must reach this frame, not a pool worker;
    //   * a COMBINED delegate runs every handler front-to-back and the task's result is
    //     the last handler's.
    // The handlers append to a field instead of printing, so nothing is written from a
    // pool thread and the output stays deterministic.
    internal static class Program
    {
        private static string s_log = "";

        private static int A(object state) { s_log += "A"; return 1; }
        private static int B(object state) { s_log += "B"; return 2; }
        private static int A0() { s_log += "A"; return 1; }
        private static int B0() { s_log += "B"; return 2; }

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
