using System.Globalization;

namespace UniTaskSample
{
    // Gate driver: runs each section's __GateEntry() in order. Its own bucket because
    // adoption is decided per PROGRAM (one out-of-contract reference un-adopts the
    // library for the whole program) and the acquisition path — a PackageReference
    // against nuget.org — is itself under test.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            UniTaskCoreSubset.Program.__GateEntry();
        }
    }
}
