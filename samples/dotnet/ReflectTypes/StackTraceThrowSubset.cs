using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StackTraceThrowSubset
{
    // Exception.StackTrace captured AT THROW — the sibling of StackTraceSubset
    // (the degraded zero-frame stack-trace intrinsics) and the half it
    // deliberately left open: `new StackTrace()` stays a zero-frame degrade
    // (walking the CURRENT stack has no anchor the runtime can trust;
    // --shadow-stack materializes it, covered in the ShadowStack bucket), but
    // dn2cpp_throw stamps the thrown exception with the real PC sequence,
    // resolved lazily against the reflection method table when StackTrace/ToString is
    // read. Thrown => a real "   at Type.Method()" trace; unthrown stays null
    // (StackTraceSubset's invariant, asserted there — not repeated here). Part 6
    // pins the real-frame-materialization half that DOES exist flag-off: `new
    // StackTrace(thrownEx)` materializes the same stamped kind-0 PC sequence
    // into real StackFrames, resolved through the same fn index
    // ex.StackTrace reads — so its verdicts are exactly as inlining-dependent as the
    // trace itself, and stay Contains()-shaped like everything else here.
    //
    // Every assertion below prints a Contains()/StartsWith() verdict, never the
    // trace itself:
    // frame COUNT and frame ORDER are best-effort under -O2 (an inlined or
    // tail-called frame simply drops), so the freeze pins only what the feature
    // guarantees — that a [MethodImpl(NoInlining)] frame on the throw path resolves
    // to its method and declaring-type name. NoInlining lowers to DN2CPP_NOINLINE,
    // which is what makes these frames survive the optimizer.
    //
    // INTENTIONAL DIVERGENCES from real .NET (verified against `dotnet run` at
    // capture time; frame format itself is declared at dn2cpp_exc_trace_render):
    //   - "rethrow preserves trace: True": real .NET prints False here, because a
    //     `throw;` APPENDS the frames from the rethrow site to the new handler onto
    //     the preserved trace, so the re-read string grows. dn2cpp re-renders the
    //     ONE PC sequence stamped at the original throw — preservation is exact,
    //     stronger than real .NET's, and the equality holds.
    //   - "StackTrace(thrownEx) GetFrame(0).GetMethod() is null: True": real .NET
    //     mints the MethodBase; dn2cpp answers null BY DESIGN even on a frame
    //     materialized from a capture — a baked rendered string cannot honestly
    //     name a method row (representative-named shared bodies, signatureless
    //     overload ambiguity, --trim-reflection's stripped tables); rationale at
    //     the intrinsic site in MethodCompiler.EmitIntrinsic.Reflection.cs.
    //   Everything else — thrown non-null, NoInlining leaf/mid frames resolving,
    //   the innermost rendered frame naming TraceLeaf (the runtime's throw
    //   machinery below it is dropped, never rendered as a managed method),
    //   `throw ex;` re-capturing at the rethrow site, ToString growing a trace
    //   section only once thrown, await propagation preserving the original
    //   frames, and part 6's StackTrace(thrownEx) reuse (FrameCount > 0, the
    //   TraceLeaf probe, GetFrame(0) non-null, unthrown => zero frames) —
    //   matches real .NET's verdicts line for line.
    internal static class Program
    {
        // 1. A NoInlining call chain: the thrown exception's trace resolves the leaf,
        //    the mid frame, and their declaring type. Method names are unique on
        //    purpose — the Contains() probes must not match anything else.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TraceLeaf()
        {
            throw new InvalidOperationException("thrown in TraceLeaf");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TraceMid()
        {
            TraceLeaf();
            // Unreachable (TraceLeaf always throws), but its presence keeps the call
            // out of tail position — a tail-called TraceLeaf would leave no TraceMid
            // return address on the stack for the capture to see.
            Console.WriteLine("unreachable: TraceLeaf returned");
        }

        // 2. `throw;` (IL rethrow) PRESERVES the stamped trace: the trace read in the
        //    outer handler renders the same string the inner handler saved.
        private static string s_savedRethrowTrace;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RethrowLeaf()
        {
            throw new InvalidOperationException("thrown in RethrowLeaf");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RethrowMid()
        {
            try
            {
                RethrowLeaf();
            }
            catch (InvalidOperationException ex)
            {
                s_savedRethrowTrace = ex.StackTrace;
                throw;
            }
        }

        // 3. `throw ex;` RE-CAPTURES: the trace is re-stamped at the rethrow site, so
        //    RecaptureSite's frame appears only after it.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RecaptureLeaf()
        {
            throw new InvalidOperationException("thrown in RecaptureLeaf");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RecaptureSite(Exception ex)
        {
            throw ex;
        }

        // 5. Await propagation preserves the trace: the fault crosses the async
        //    machinery (builder SetException, awaiter GetResult) and the original
        //    NoInlining throw frame still resolves.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AsyncThrowLeaf()
        {
            throw new InvalidOperationException("thrown in AsyncThrowLeaf");
        }

        private static async Task AsyncThrower()
        {
            AsyncThrowLeaf();
            await Task.CompletedTask;
        }

        private static async Task<bool> AwaitPropagatedTraceHasLeaf()
        {
            try
            {
                await AsyncThrower();
            }
            catch (InvalidOperationException ex)
            {
                return ex.StackTrace != null && ex.StackTrace.Contains("AsyncThrowLeaf");
            }
            return false;
        }

        internal static void Run()
        {
            // 1. Thrown => non-null, and the NoInlining frames resolve.
            Exception thrownEx = null;
            try
            {
                TraceMid();
            }
            catch (InvalidOperationException ex)
            {
                thrownEx = ex;
                string trace = ex.StackTrace;
                Console.WriteLine("thrown StackTrace is null: " + (trace is null));
                Console.WriteLine("trace contains TraceLeaf: "
                    + (trace != null && trace.Contains("TraceLeaf")));
                Console.WriteLine("trace contains TraceMid: "
                    + (trace != null && trace.Contains("TraceMid")));
                Console.WriteLine("trace contains declaring type: "
                    + (trace != null && trace.Contains("StackTraceThrowSubset.Program")));
                // The INNERMOST rendered frame must be the innermost managed
                // body. Below TraceLeaf on the physical stack sit only the
                // runtime's throw machinery (dn2cpp_throw, the trace stamp,
                // the PAL walk) — non-table C++ functions the resolver must
                // DROP, never render as a neighboring managed method: before
                // exact function-entry resolution, the trace head named
                // whatever emitted body the linker placed before dn2cpp_throw
                // (a plausible wrong answer). Real .NET's trace starts at
                // TraceLeaf too, so the verdict matches (its line may append
                // " in file:line"; StartsWith is prefix-proof against that).
                Console.WriteLine("innermost rendered frame is TraceLeaf: "
                    + (trace != null && trace.StartsWith(
                        "   at StackTraceThrowSubset.Program.TraceLeaf()")));
            }

            // 2. `throw;` preserves — see the divergence note in the header: real
            //    .NET appends the rethrow-to-handler frames and answers False.
            try
            {
                RethrowMid();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("rethrow preserves trace: "
                    + (s_savedRethrowTrace != null && ex.StackTrace == s_savedRethrowTrace));
            }

            // 3. `throw ex;` re-captures at the rethrow site.
            Exception recaptured = null;
            try
            {
                RecaptureLeaf();
            }
            catch (InvalidOperationException ex)
            {
                recaptured = ex;
                Console.WriteLine("original trace contains RecaptureSite: "
                    + (ex.StackTrace != null && ex.StackTrace.Contains("RecaptureSite")));
            }
            try
            {
                RecaptureSite(recaptured);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("recaptured trace contains RecaptureSite: "
                    + (ex.StackTrace != null && ex.StackTrace.Contains("RecaptureSite")));
            }

            // 4. ToString grows the "   at " trace section only once thrown.
            Console.WriteLine("thrown ToString contains at: "
                + thrownEx.ToString().Contains("   at "));
            Console.WriteLine("unthrown ToString contains at: "
                + new InvalidOperationException("never thrown").ToString().Contains("   at "));

            // 5. Await propagation preserves the original throw frame.
            Console.WriteLine("await-propagated trace contains AsyncThrowLeaf: "
                + AwaitPropagatedTraceHasLeaf().GetAwaiter().GetResult());

            // 6. new StackTrace(thrownEx) reuses the kind-0 trace stamped at
            //    throw: same PCs, same fn-index resolution, same dropped-if-unresolved
            //    posture as ex.StackTrace — so only Contains()/nullness verdicts, never
            //    counts (frame survival under -O2 is inlining-dependent). The TraceLeaf
            //    probe reuses part 1's NoInlining marker. GetMethod() is the declared
            //    null degrade (see the header); unthrown means no stamp, so zero frames
            //    (real .NET agrees on the zero).
            StackTrace reused = new StackTrace(thrownEx);
            Console.WriteLine("StackTrace(thrownEx) FrameCount > 0: "
                + (reused.FrameCount > 0));
            Console.WriteLine("StackTrace(thrownEx) ToString contains TraceLeaf: "
                + reused.ToString().Contains("TraceLeaf"));
            Console.WriteLine("StackTrace(thrownEx) GetFrame(0) is null: "
                + (reused.GetFrame(0) is null));
            Console.WriteLine("StackTrace(thrownEx) GetFrame(0).GetMethod() is null: "
                + (reused.GetFrame(0).GetMethod() is null));
            Console.WriteLine("StackTrace(unthrown) FrameCount is 0: "
                + (new StackTrace(new InvalidOperationException("never thrown")).FrameCount == 0));
        }
    }
}
