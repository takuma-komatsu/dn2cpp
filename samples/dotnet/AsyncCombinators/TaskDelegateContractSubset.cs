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
    //     included, and the same for a ContinueWith continuation, whose result kinds
    //     answer through their own thunks, and for a Task-returning delegate, where the
    //     last handler's task is the one that gets unwrapped or handed back — and a null
    //     task unwraps into a cancellation rather than a fault.
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

        private static int CA(Task t) { s_log += "A"; return 1; }
        private static int CB(Task t) { s_log += "B"; return 2; }
        private static double CDA(Task t) { s_log += "A"; return 0.5; }
        private static double CDB(Task t) { s_log += "B"; return 2.5; }
        private static string CSA(Task t) { s_log += "A"; return "a"; }
        private static string CSB(Task t) { s_log += "B"; return "b"; }
        private static Pair CPA(Task t) { s_log += "A"; return new Pair { X = 1, Y = 10 }; }
        private static Pair CPB(Task t) { s_log += "B"; return new Pair { X = 2, Y = 20 }; }
        private static long CLA(Task t) { s_log += "A"; return 1L; }
        private static long CLB(Task t) { s_log += "B"; return 2L; }
        private static float CFA(Task t) { s_log += "A"; return 0.5f; }
        private static float CFB(Task t) { s_log += "B"; return 2.5f; }

        private static async Task UA() { s_log += "A"; await Task.Yield(); }
        private static async Task UB() { s_log += "B"; await Task.Yield(); }
        private static async Task<int> UIA() { s_log += "A"; await Task.Yield(); return 1; }
        private static async Task<int> UIB() { s_log += "B"; await Task.Yield(); return 2; }
        private static async Task<int> FIA() { s_log += "A"; await Task.Yield(); throw new InvalidOperationException("late fault"); }
        private static Task<int> TIA() { s_log += "A"; throw new InvalidOperationException("early throw"); }

        private static Task NullTask() { s_log += "A"; return null; }
        private static TaskCompletionSource<int> s_chainRelease;
        private static Task WaitForLaterHandler() { s_log += "A"; return s_chainRelease.Task; }
        private static Task ReleaseEarlierHandler()
        {
            s_log += "B";
            s_chainRelease.SetResult(1);
            return Task.CompletedTask;
        }

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

            // ContinueWith's continuation is a delegate too, and each of its result kinds
            // answers through its own thunk — so the chain walk has to be in every one.
            Task antecedent = Task.CompletedTask;

            s_log = "";
            Func<Task, int> ci = CA;
            ci += CB;
            int ir = antecedent.ContinueWith(ci).Result;
            Console.WriteLine("ContinueWith int multicast: " + s_log + ", " + ir);

            s_log = "";
            Func<Task, double> cd = CDA;
            cd += CDB;
            double dr = antecedent.ContinueWith(cd).Result;
            Console.WriteLine("ContinueWith double multicast: " + s_log + ", " + dr);

            s_log = "";
            Func<Task, string> cs = CSA;
            cs += CSB;
            string sr = antecedent.ContinueWith(cs).Result;
            Console.WriteLine("ContinueWith ref multicast: " + s_log + ", " + sr);

            s_log = "";
            Func<Task, Pair> cp = CPA;
            cp += CPB;
            Pair pr = antecedent.ContinueWith(cp).Result;
            Console.WriteLine("ContinueWith struct multicast: " + s_log + ", " + pr.X + "/" + pr.Y);

            s_log = "";
            Func<Task, long> cl = CLA;
            cl += CLB;
            long lr = antecedent.ContinueWith(cl).Result;
            Console.WriteLine("ContinueWith long multicast: " + s_log + ", " + lr);

            s_log = "";
            Func<Task, float> cf = CFA;
            cf += CFB;
            float fr = antecedent.ContinueWith(cf).Result;
            Console.WriteLine("ContinueWith float multicast: " + s_log + ", " + fr);

            // A Task-returning delegate is a chain too. Real .NET runs every handler and
            // takes the LAST one's task — Run unwraps it, StartNew hands it back as the
            // outer task's result. Each handler appends before its first await, so the log
            // is the invocation order and not a race between the resumed tails.
            s_log = "";
            Func<Task> unwrapVoid = UA;
            unwrapVoid += UB;
            Task.Run(unwrapVoid).Wait();
            Console.WriteLine("Run unwrap multicast: " + s_log);

            s_log = "";
            Func<Task<int>> unwrapInt = UIA;
            unwrapInt += UIB;
            int ur = Task.Run(unwrapInt).Result;
            Console.WriteLine("Run unwrap-int multicast: " + s_log + ", " + ur);

            s_log = "";
            int nr = Task.Factory.StartNew(unwrapInt).Result.Result;
            Console.WriteLine("StartNew nested multicast: " + s_log + ", " + nr);

            // Every handler must be invoked before an earlier returned task is observed:
            // the second handler is the only code that can settle the first one's task.
            s_log = "";
            s_chainRelease = new TaskCompletionSource<int>();
            Func<Task> dependent = WaitForLaterHandler;
            dependent += ReleaseEarlierHandler;
            Task dependentRun = Task.Run(dependent);
            bool runCompleted = dependentRun.Wait(TimeSpan.FromSeconds(1));
            if (!runCompleted)
            {
                s_chainRelease.TrySetResult(1);
                dependentRun.Wait();
            }
            Console.WriteLine("Run dependent multicast: " + s_log + ", " + runCompleted);

            s_log = "";
            s_chainRelease = new TaskCompletionSource<int>();
            dependent = WaitForLaterHandler;
            dependent += ReleaseEarlierHandler;
            Task<Task> dependentStart = Task.Factory.StartNew(dependent);
            bool startCompleted = dependentStart.Wait(TimeSpan.FromSeconds(1));
            if (!startCompleted)
            {
                s_chainRelease.TrySetResult(1);
                dependentStart.Wait();
            }
            dependentStart.Result.Wait();
            Console.WriteLine("StartNew dependent multicast: " + s_log + ", " + startCompleted);

            // The fault side of the same contract: an EARLIER handler's async fault
            // (thrown after its first await) stays in that handler's own task and the
            // outer still takes the last handler's, while a synchronous throw stops the
            // chain and faults the outer, so the last handler never runs.
            s_log = "";
            Func<Task<int>> faultedEarlier = FIA;
            faultedEarlier += UIB;
            int fe = Task.Run(faultedEarlier).Result;
            Console.WriteLine("Run faulted-earlier multicast: " + s_log + ", " + fe);

            s_log = "";
            Func<Task<int>> throwsEarlier = TIA;
            throwsEarlier += UIB;
            try
            {
                Task.Factory.StartNew(throwsEarlier).Wait();
                Console.WriteLine("StartNew throwing-earlier multicast: no-throw " + s_log);
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("StartNew throwing-earlier multicast: " + s_log + ", "
                    + ae.InnerException.GetType().Name + ": " + ae.InnerException.Message);

            // Unwrapping a null task: .NET's Run(Func<Task>) cancels the outer rather than
            // faulting or crashing, so a body that returns null is a contract, not a bug.
            s_log = "";
            string nullOutcome;
            try
            {
                Task.Run((Func<Task>)NullTask).Wait();
                nullOutcome = "no-throw";
            }
            catch (AggregateException ex)
            {
                nullOutcome = ex.InnerException.GetType().Name;
            }
            Console.WriteLine("Run unwrap-null: " + s_log + ", " + nullOutcome);
            }
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
