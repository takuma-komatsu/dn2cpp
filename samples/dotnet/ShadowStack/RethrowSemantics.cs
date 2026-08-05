using System;

namespace RethrowSemantics
{
    // Rethrow semantics under the shadow stack, with the full trace texts frozen:
    //   - `throw;` (IL rethrow) PRESERVES the trace stamped at the original throw,
    //     exactly. Declared divergence: real .NET appends the rethrow-to-handler
    //     frames, so its re-read string grows and the equality prints False.
    //   - `throw ex;` RE-CAPTURES at the rethrow site, matching real .NET.
    internal static class Program
    {
        private static string s_savedTrace;
        private static Exception s_savedObject;

        private static void RethrowLeaf()
        {
            throw new InvalidOperationException("thrown in RethrowLeaf");
        }

        private static void RethrowMid()
        {
            try
            {
                RethrowLeaf();
            }
            catch (InvalidOperationException ex)
            {
                s_savedTrace = ex.StackTrace;
                s_savedObject = ex;
                throw;
            }
        }

        private static void RecaptureLeaf()
        {
            throw new InvalidOperationException("thrown in RecaptureLeaf");
        }

        private static void RecaptureSite(Exception ex)
        {
            throw ex;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("== RethrowSemantics ==");
            try
            {
                RethrowMid();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("rethrow-preserved trace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("rethrow preserves trace: "
                    + (s_savedTrace != null && ex.StackTrace == s_savedTrace));
            }

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
                Console.WriteLine("recaptured trace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("recaptured trace contains RecaptureSite: "
                    + (ex.StackTrace != null && ex.StackTrace.Contains("RecaptureSite")));
            }
        }

        internal static void __GateSmoke()
        {
            // Trace-independent identity only, so the verdicts hold on real .NET
            // and on dn2cpp whatever the flag state.
            try
            {
                RethrowMid();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("rethrow same object: " + ReferenceEquals(ex, s_savedObject));
            }
            Exception recaptured = null;
            try
            {
                RecaptureLeaf();
            }
            catch (InvalidOperationException ex)
            {
                recaptured = ex;
            }
            try
            {
                RecaptureSite(recaptured);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("recapture same object: " + ReferenceEquals(ex, recaptured));
            }
        }
    }
}
