using System.Globalization;

namespace ThreadingPrimitives
{
    // Gate driver: each section keeps its own namespace so namespace-sensitive output
    // matches a standalone build. All sections are single-threaded and deterministic.
    internal static class Program
    {
        private static void Main()
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
        }
    }
}
