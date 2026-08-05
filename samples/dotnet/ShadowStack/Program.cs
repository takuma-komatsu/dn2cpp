using System;
using System.Globalization;

namespace ShadowStack
{
    // Gate driver for the opt-in shadow stack (--shadow-stack). Every compiled
    // body's prologue plants a Dn2CppShadowFrame RAII guard whose side effects
    // the optimizer must preserve, so traces are EXACT — deterministic enough to
    // freeze whole trace texts, with no NoInlining attribute anywhere here.
    //
    // argv picks the run mode, so ONE transpiled binary serves every gate arm:
    //   (none)/"full" — full-trace prints; deterministic only under --shadow-stack.
    //   "smoke"       — the same throw paths printing only trace-INDEPENDENT
    //     lines, so the flag-off arm can live-diff it against real .NET.
    //   "unhandled"   — the deep chain escapes Main; the stderr report must
    //     carry the shadow frames.
    //   "bench"       — a call-heavy loop for flag-on/off timing. No gate asserts it.
    //
    // Intentional divergences from real .NET under "full" (details per section):
    //   - Frames render "   at Ns.Type.Method()": no parameter list, no file:line.
    //   - The window is EVERY live frame down to Main, not throw-to-handler.
    //   - A shared canonical body names its group's REPRESENTATIVE instantiation
    //     plus a " [shared generic]" mark (SharedGenericFrame.cs).
    //   - `throw;` preserves the stamped trace exactly (RethrowSemantics.cs).
    //   - Capacity is 1024 frames; the rest collapse to one marker (CapacityNote.cs).
    //   - The current-stack APIs diverge further (CurrentStackSubset.cs).
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            string mode = args.Length > 0 ? args[0] : "full";
            if (mode == "full")
            {
                DeepChainSubset.Program.__GateEntry();
                SharedGenericFrame.Program.__GateEntry();
                RethrowSemantics.Program.__GateEntry();
                CurrentStackSubset.Program.__GateEntry();
                CapacityNote.Program.__GateEntry();
            }
            else if (mode == "smoke")
            {
                DeepChainSubset.Program.__GateSmoke();
                SharedGenericFrame.Program.__GateSmoke();
                RethrowSemantics.Program.__GateSmoke();
                CurrentStackSubset.Program.__GateSmoke();
                CapacityNote.Program.__GateSmoke();
            }
            else if (mode == "unhandled")
            {
                DeepChainSubset.Program.ThrowUncaught();
            }
            else if (mode == "bench")
            {
                Bench.Program.__GateEntry();
            }
            else
            {
                Console.WriteLine("unknown mode: " + mode);
                return 2;
            }
            return 0;
        }
    }
}
