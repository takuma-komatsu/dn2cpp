using System.Globalization;

namespace ThreadingPrimitives
{
    // Gate driver: each section keeps its own namespace so namespace-sensitive output
    // matches a standalone build. Each section prints deterministic observations.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            ThreadingPrimitivesCore.Program.__GateEntry();
            InterlockedSubWord.Program.__GateEntry();
            InterlockedFloatDouble.Program.__GateEntry();
            InterlockedAndOrRead.Program.__GateEntry();
            InterlockedGeneric.Program.__GateEntry();
            Barriers.Program.__GateEntry();
            WaitHandleWaitAny.Program.__GateEntry();
            LockSubset.Program.__GateEntry();
            LockTypeSubset.Program.__GateEntry();
            EventTypeIdentity.Program.__GateEntry();
            LegacyThreadVolatile.Program.__GateEntry();
            WaitHandleRegistryLifetime.Program.__GateEntry();
            if (args.Length != 0 && args[0] == "before-argument-fields")
                return;
            WaitHandleValidationSubset.__GateEntry();
            if (args.Length != 0 && args[0] == "before-ownership")
                return;
            MonitorLockValidationSubset.__GateEntry();
            if (args.Length != 0 && args[0] == "before-lock-identity")
                return;
            LockTypeSubset.Program.RunIdentity();
            if (args.Length != 0 && args[0] == "before-cancellation-thread-faults")
                return;
            ThreadingPrimitivesCore.Program.RunCancellationFaults();
        }
    }
}
