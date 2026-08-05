using System;
using System.Diagnostics;

namespace CapacityNote
{
    // The shadow stack's capacity boundary: the per-thread buffer holds 1024
    // names, deeper frames are COUNTED but not stored, and the render leads with
    // "   at <N innermost frames past shadow-stack capacity>" where the lost
    // innermost frames would have been. The 1030-deep recursion here overflows
    // by exactly 9 (1033 live frames at throw), so the marker, the line count
    // and the outermost frame are frozen; the 1025-line text is not printed.
    //
    // The current-stack capture rides the same overflow: FrameCount covers
    // MATERIALIZED frames only, so it is exactly 1024 and its ToString leads
    // with the same marker. Recorded into statics here, printed by __GateEntry.
    //
    // Declared divergence: real .NET has no such capacity — all 1033 frames,
    // no marker line.
    internal static class Program
    {
        private static int s_overflowCaptureCount;
        private static string s_overflowCaptureFirstLine;

        private static void Recurse(int n)
        {
            if (n == 0)
            {
                StackTrace st = new StackTrace();
                s_overflowCaptureCount = st.FrameCount;
                s_overflowCaptureFirstLine = st.ToString().Split('\n')[0];
                throw new InvalidOperationException("bottom of CapacityNote recursion");
            }
            Recurse(n - 1);
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("== CapacityNote ==");
            try
            {
                Recurse(1030);
            }
            catch (InvalidOperationException ex)
            {
                string trace = ex.StackTrace;
                string[] lines = trace.Split('\n');
                Console.WriteLine("trace contains capacity marker: "
                    + trace.Contains("past shadow-stack capacity"));
                Console.WriteLine("capacity trace first line: " + lines[0]);
                Console.WriteLine("capacity trace line count: " + lines.Length);
                Console.WriteLine("capacity trace last line: " + lines[lines.Length - 1]);
                Console.WriteLine("overflow capture FrameCount: " + s_overflowCaptureCount);
                Console.WriteLine("overflow capture first line: " + s_overflowCaptureFirstLine);
            }
        }

        internal static void __GateSmoke()
        {
            try
            {
                Recurse(1030);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("capacity caught: " + ex.Message);
            }
        }
    }
}
