using System.Globalization;

namespace CustomAsyncTaskTypeExceed
{
    // Gate driver: runs each section's __GateEntry() in order. Its own bucket rather
    // than a CustomAsyncTaskType section because adoption is decided per PROGRAM: one
    // out-of-contract reference would un-adopt the in-contract bucket's library too.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            CustomTaskYieldSubset.Program.__GateEntry();
        }
    }
}
