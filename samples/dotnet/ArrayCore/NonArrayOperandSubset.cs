using System;

namespace NonArrayOperandSubset
{
    // A System.Array-typed operand that is not an array. Every non-generic Array member
    // reached through a receiver whose static C++ type degraded to the plain object rep
    // funnels into the runtime's `_dyn` helpers, and those ABORT when the runtime
    // type-info's array flag is clear — deliberately, because the only way to get there
    // is an emitter that routed a call it should not have.
    //
    // This section is the evidence for that "only way": the castclass ahead of every such
    // call raises a CATCHABLE InvalidCastException first, for a string, a bare object and
    // a boxed struct alike, so user code cannot reach the abort. Remove the check and the
    // aborts stop being unreachable — which is why the assertion lives in a gate rather
    // than in a comment. The positive controls that follow keep the same members honest
    // on a real array reached the same way.
    internal static class Program
    {
        private static void E(string tag, Func<object> f)
        {
            try { Console.WriteLine(tag + ": " + f()); }
            catch (Exception ex) { Console.WriteLine(tag + ": " + ex.GetType().Name); }
        }

        internal static void Run()
        {
            Console.WriteLine("== non-array operand ==");
            object str = "not an array";
            object bare = new object();
            object boxed = DateTime.MinValue;

            E("clone-string", () => ((Array)str).Clone());
            E("clone-object", () => ((Array)bare).Clone());
            E("clone-boxed-struct", () => ((Array)boxed).Clone());
            E("copy-string", () => { Array.Copy((Array)str, (Array)str, 1); return "ok"; });
            E("clear-string", () => { Array.Clear((Array)str); return "ok"; });
            // `as` yields null rather than throwing, so the fault is the null receiver's.
            E("as-then-clone", () => (str as Array).Clone());

            Console.WriteLine("== same members, real arrays, same route ==");
            object realInts = new int[] { 1, 2, 3 };
            object realStrs = new string[] { "a", "b" };
            object realBytes = new byte[] { 9, 8, 7 };
            E("clone-int", () => ((int[])((Array)realInts).Clone())[2]);
            E("clone-string-arr", () => ((string[])((Array)realStrs).Clone())[1]);
            E("clone-byte", () => ((byte[])((Array)realBytes).Clone())[0]);
            E("copy-int", () =>
            {
                Array dst = new int[3];
                Array.Copy((Array)realInts, dst, 3);
                return ((int[])dst)[1];
            });
            E("clear-int", () =>
            {
                Array a = new int[] { 5, 6, 7 };
                Array.Clear(a);
                return ((int[])a)[0];
            });
        }
    }
}
