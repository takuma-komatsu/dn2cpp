using System;
using System.Diagnostics;

namespace MonotonicClockCore
{
    // The PAL's monotonic clock, reached the way game code reaches it: through the real
    // BCL bodies. Stopwatch.GetTimestamp calls Interop.Sys.GetTimestamp, while the user
    // assembly's Environment.TickCount64 MemberReference resolves to the CoreLib getter
    // that calls Interop.Sys.GetLowResolutionTimestamp. Both P/Invokes lower to direct
    // native calls. In-CoreLib MethodDefinition calls to TickCount64 can take the
    // dn2cpp_tickcount64 intrinsic instead.
    //
    // Stopwatch.Frequency is constant in the supported CoreLib, so the wasm PAL
    // defines no SystemNative_GetTimestampResolution entry point.
    //
    // Only predicates are printed: the readings are non-deterministic, and the frequency
    // is the host's, so the raw number would say more about the machine than about the
    // clock. Busy-wait, never Thread.Sleep: this bucket runs on the wasm axis too, where
    // Thread throws.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("-- MonotonicClockCore --");
            // Unix and Web promise nanosecond ticks. Windows uses the QPC rate, which is
            // machine-specific but must be positive on both sides of the diff.
            bool frequencyValid = OperatingSystem.IsWindows()
                ? Stopwatch.Frequency > 0
                : Stopwatch.Frequency == 1_000_000_000;
            Console.WriteLine($"frequencyValid={frequencyValid}");

            long stamp0Before = Stopwatch.GetTimestamp();
            long tick0 = Environment.TickCount64;
            long stamp0After = Stopwatch.GetTimestamp();
            Console.WriteLine($"tickNonNegative={tick0 >= 0}");
            Console.WriteLine($"timestampNonZero={stamp0Before != 0}");

            // Spin until the coarse clock has moved. The iteration bound is what makes a
            // stopped clock end the section with a printed False instead of hanging until
            // the harness kills the run.
            //
            // Bracket each TickCount64 read with Stopwatch reads. A scheduler pause then
            // widens the interval instead of becoming measurement error.
            long tick1 = tick0;
            long stamp1Before = stamp0After, stamp1After = stamp0After;
            for (int i = 0; i < 1_000_000_000 && tick1 - tick0 < WaitMs; i++)
            {
                stamp1Before = Stopwatch.GetTimestamp();
                tick1 = Environment.TickCount64;
                stamp1After = Stopwatch.GetTimestamp();
            }

            Console.WriteLine($"tickAdvanced={tick1 - tick0 >= WaitMs}");
            Console.WriteLine($"timestampAdvanced={stamp1After > stamp0Before}");

            // The coarse-clock interval must lie between the inner and outer Stopwatch
            // brackets. Slack covers TickCount64's quantization, not scheduling delay.
            double lowerMs = (stamp1Before - stamp0After) * 1000.0 / Stopwatch.Frequency;
            double upperMs = (stamp1After - stamp0Before) * 1000.0 / Stopwatch.Frequency;
            double byTick = tick1 - tick0;
            bool clocksAgree = byTick + GranularitySlackMs >= lowerMs
                && byTick <= upperMs + GranularitySlackMs;
            Console.WriteLine($"clocksAgree={clocksAgree}");
        }

        // Long enough that a scale error cannot hide inside the granularity allowance.
        private const int WaitMs = 50;

        // Headroom over Windows' coarse-clock quantum, while remaining below WaitMs.
        private const double GranularitySlackMs = 20.0;
    }
}
