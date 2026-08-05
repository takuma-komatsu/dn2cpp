using System;
using System.Diagnostics;

namespace CurrentStackSubset
{
    // The CURRENT-stack half: `new StackTrace()` and its overloads materialize the
    // live shadow stack, Environment.StackTrace flows through the same intrinsics,
    // and `new StackTrace(ex)` reuses the trace stamped at throw. Every name is a
    // transpile-baked guard string, so no inlining heuristic can shift a count —
    // which is what lets the counts below be frozen as exact numbers.
    //
    // Divergences beyond the two declared in ShadowStack/Program.cs:
    //   - ToString() '\n'-terminates EVERY line: the TraceFormat argument is
    //     dropped, so Normal and TrailingNewLine coincide. (Environment.StackTrace
    //     uses Normal on real .NET, so its trailing '\n' here is a divergence;
    //     the getter's own frame at the head is parity.)
    //   - StackFrame.ToString() is the baked "Ns.Type.Method()" text plus '\n'.
    //   - GetFrame(i).GetMethod() is null BY DESIGN even on a materialized frame:
    //     a baked rendered string cannot honestly mint a MethodBase — rationale
    //     at the intrinsic site in MethodCompiler.EmitIntrinsic.Reflection.cs.
    //   - `new StackTrace(-1)` clamps to 0 where real .NET throws
    //     ArgumentOutOfRangeException; this family never throws. skipFrames
    //     consumes the INNERMOST end, matching real .NET's window.
    //   - `new StackTrace(ex).ToString()` equals ex.StackTrace + "\n" — the one
    //     trailing newline is the only difference. (Requires a symbol-free build,
    //     i.e. DebugType=none: a PDB grows real .NET's ex.StackTrace with
    //     "in file:line" text the fNeedFileInfo=false capture lacks.)
    //   - A zero-frame trace's ToString names itself, "   at <stack trace
    //     unavailable in AOT>"; real .NET renders an empty string.
    internal static class Program
    {
        // A frame's ToString() carries a trailing '\n' (pinned once below);
        // single-line prints trim it.
        private static string FrameText(StackFrame f)
        {
            return f.ToString().TrimEnd('\n');
        }

        // Two captures one skip apart in the SAME method, so the shift is
        // observable. The chain __GateEntry -> SkipOuter -> SkipProbe gives
        // skip(2) something real to consume.
        private static void SkipProbe()
        {
            StackTrace st0 = new StackTrace(0);
            StackTrace st2 = new StackTrace(2);
            Console.WriteLine("skip0 FrameCount: " + st0.FrameCount);
            Console.WriteLine("skip2 FrameCount: " + st2.FrameCount);
            Console.WriteLine("skip counts differ by 2: "
                + (st0.FrameCount - st2.FrameCount == 2));
            Console.WriteLine("skip0 frame0: " + FrameText(st0.GetFrame(0)));
            Console.WriteLine("skip0 frame2: " + FrameText(st0.GetFrame(2)));
            Console.WriteLine("skip2 frame0: " + FrameText(st2.GetFrame(0)));
            Console.WriteLine("skip2 frame0 equals skip0 frame2: "
                + (FrameText(st2.GetFrame(0)) == FrameText(st0.GetFrame(2))));
            // Declared divergence: negative skip clamps to 0 (real .NET throws).
            Console.WriteLine("negative skip count equals skip0 count: "
                + (new StackTrace(-1).FrameCount == st0.FrameCount));
        }

        private static void SkipOuter()
        {
            SkipProbe();
        }

        private static void ReuseLeaf()
        {
            throw new InvalidOperationException("thrown in ReuseLeaf");
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("== CurrentStackSubset ==");

            // The getter's own guard frame heads the text, and the dropped
            // TraceFormat leaves a trailing '\n' — so Console.Write, not WriteLine.
            string env = Environment.StackTrace;
            Console.WriteLine("Environment.StackTrace ends with newline: "
                + (env[env.Length - 1] == '\n'));
            Console.WriteLine("Environment.StackTrace:");
            Console.Write(env);

            // The innermost frame is the body that executed the newobj, so the
            // count here is exactly the live __GateEntry and Main.
            StackTrace st = new StackTrace();
            Console.WriteLine("current FrameCount: " + st.FrameCount);
            Console.WriteLine("current GetFrames().Length equals FrameCount: "
                + (st.GetFrames().Length == st.FrameCount));
            Console.WriteLine("current GetFrame(0) is null: " + (st.GetFrame(0) is null));
            Console.WriteLine("current frame0 ToString ends with newline: "
                + (st.GetFrame(0).ToString()[st.GetFrame(0).ToString().Length - 1] == '\n'));
            Console.WriteLine("current frame0: " + FrameText(st.GetFrame(0)));
            // The declared degrade: null even on a materialized frame.
            Console.WriteLine("current GetFrame(0).GetMethod() is null: "
                + (st.GetFrame(0).GetMethod() is null));
            Console.WriteLine("current ToString:");
            Console.Write(st.ToString());

            SkipOuter();

            // Reuses the trace stamped at throw: the SAME frames ex.StackTrace
            // renders, plus ToString's trailing '\n'.
            Exception caught = null;
            try
            {
                ReuseLeaf();
            }
            catch (InvalidOperationException ex)
            {
                caught = ex;
            }
            StackTrace stEx = new StackTrace(caught);
            Console.WriteLine("reuse FrameCount > 0: " + (stEx.FrameCount > 0));
            Console.WriteLine("reuse ToString equals ex.StackTrace + newline: "
                + (stEx.ToString() == caught.StackTrace + "\n"));
            Console.WriteLine("reuse ToString equals ex.StackTrace exactly: "
                + (stEx.ToString() == caught.StackTrace));
            Console.WriteLine("reuse trace:");
            Console.Write(stEx.ToString());

            // An UNTHROWN exception has no stamped trace: zero frames, and the
            // zero-frame ToString still says WHY it is empty.
            StackTrace stUnthrown = new StackTrace(new InvalidOperationException("never thrown"));
            Console.WriteLine("unthrown reuse FrameCount is 0: " + (stUnthrown.FrameCount == 0));
            Console.WriteLine("unthrown reuse says unavailable: "
                + stUnthrown.ToString().Contains("stack trace unavailable in AOT"));
        }

        internal static void __GateSmoke()
        {
            // Only verdicts that hold on real .NET AND on dn2cpp under EITHER flag
            // state. Every current-stack count/text line above is full-mode only:
            // flag-off they are the zero-frame degrade, and GetMethod() is null
            // here but non-null on real .NET.
            StackTrace stUnthrown = new StackTrace(new InvalidOperationException("never thrown"));
            Console.WriteLine("unthrown reuse FrameCount is 0: " + (stUnthrown.FrameCount == 0));
            Console.WriteLine("unthrown reuse GetFrame(0) is null: "
                + (stUnthrown.GetFrame(0) is null));
        }
    }
}
