using System;
using System.Diagnostics;

namespace StackTraceSubset
{
    // System.Diagnostics.StackTrace / StackFrame: DEGRADED intrinsics — the
    // diagnostic-degrades side of the rule in docs/ARCHITECTURE.md §4-B (the
    // canonical statement of the doctrine; DynamicCodegenSubset in this same
    // bucket asserts the load-bearing-fails-loud side). `new StackTrace()`
    // returns a real object with zero frames whose ToString() names its own
    // degradation ("   at <stack trace unavailable in AOT>", never "").
    //
    // This section INTENTIONALLY DIVERGES from real .NET, which reports a populated
    // trace here, and that is why it lives in a freeze bucket and not a diff gate.
    internal static class Program
    {
        internal static void Run()
        {
            // A captured trace: zero frames, and it says so.
            var st = new StackTrace();
            Console.WriteLine("st.FrameCount=" + st.FrameCount);
            Console.WriteLine("st.GetFrames().Length=" + st.GetFrames().Length);
            Console.WriteLine("st.GetFrame(0) is null: " + (st.GetFrame(0) is null));
            Console.WriteLine("st.GetFrame(-1) is null: " + (st.GetFrame(-1) is null));
            Console.Write("st.ToString()=");
            Console.Write(st.ToString());          // already ends in a newline
            // The same text through Object.ToString — the type-info's tostring slot, so
            // a StackTrace held as `object` (logged, interpolated) is honest too.
            Console.Write("as object: ");
            Console.Write(((object)st).ToString());
            Console.WriteLine("st.GetType().FullName=" + st.GetType().FullName);
            Console.WriteLine("st is StackTrace: " + (st is StackTrace));

            // EVERY capturing ctor overload — the sibling-overload rule. All zero.
            Console.WriteLine("capturing ctors: "
                + new StackTrace(true).FrameCount
                + " " + new StackTrace(4).FrameCount
                + " " + new StackTrace(4, true).FrameCount
                + " " + new StackTrace(new Exception("e")).FrameCount
                + " " + new StackTrace(new Exception("e"), true).FrameCount
                + " " + new StackTrace(new Exception("e"), 2).FrameCount
                + " " + new StackTrace(new Exception("e"), 2, true).FrameCount);

            // A captured StackFrame: nothing in it, and honest about that.
            var f = new StackFrame();
            Console.WriteLine("frame.GetMethod() is null: " + (f.GetMethod() is null));
            Console.WriteLine("frame.GetFileName() is null: " + (f.GetFileName() is null));
            Console.WriteLine("frame.GetFileLineNumber()=" + f.GetFileLineNumber());
            Console.WriteLine("frame.GetFileColumnNumber()=" + f.GetFileColumnNumber());
            Console.WriteLine("frame.GetILOffset()=" + f.GetILOffset());
            Console.Write("frame.ToString()=");
            Console.Write(f.ToString());
            Console.WriteLine("frame ctors: "
                + new StackFrame(true).GetFileLineNumber()
                + " " + new StackFrame(2).GetFileLineNumber()
                + " " + new StackFrame(2, true).GetFileLineNumber());

            // A frame the CALLER built is NOT a capture and is NOT degraded: it carries
            // exactly what it was given, and a trace over it reports it. Only a walk of
            // the real stack comes back empty. (These two lines match real .NET's values
            // — only the rendered method name differs, since there is none to recover.)
            var synth = new StackFrame("Widget.cs", 42, 7);
            Console.WriteLine("synthetic: file=" + synth.GetFileName()
                + " line=" + synth.GetFileLineNumber()
                + " col=" + synth.GetFileColumnNumber());
            var st2 = new StackTrace(synth);
            Console.WriteLine("StackTrace(frame).FrameCount=" + st2.FrameCount);
            Console.WriteLine("StackTrace(frame).GetFrame(0) is the frame: "
                + ReferenceEquals(st2.GetFrame(0), synth));
            Console.Write("StackTrace(frame).ToString()=");
            Console.Write(st2.ToString());

            // foreach over an empty GetFrames() — the shape a logger actually writes.
            int n = 0;
            foreach (StackFrame fr in st.GetFrames())
            {
                n++;
                GC.KeepAlive(fr);
            }
            Console.WriteLine("foreach over captured frames: " + n);

            // Environment.StackTrace needs NO intrinsic of its own: its real getter is
            // `new StackTrace(true).ToString(TraceFormat.Normal)`, which transpiles
            // straight through the two intrinsics above.
            Console.Write("Environment.StackTrace=");
            Console.Write(Environment.StackTrace);
            Console.WriteLine();

            // THE INVARIANT the degraded zero-frame intrinsics MUST NOT BREAK: an
            // UNTHROWN exception's StackTrace is null, and real .NET agrees
            // (SelfHostPrimSubset's diff gate depends on it). Capturing a real trace
            // at throw is the throw-time capture, not this.
            var ex = new InvalidOperationException("never thrown");
            Console.WriteLine("unthrown ex.StackTrace is null: " + (ex.StackTrace is null));
        }
    }
}
