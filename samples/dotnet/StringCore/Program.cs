using System;
using System.Globalization;

namespace StringCore
{
    // Gate driver. Each section keeps its own namespace so reflected type names
    // stay identical to the standalone samples.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            StringMethodSubset.Program.Run();
            StringCompareSubset.Program.Run();
            StringComparisonFoldSubset.Program.Run();
            StringIndexOfCmpSubset.Program.Run();
            StringTrimIndexSubset.Program.Run();
            StringPadSubset.Program.Run();
            StringSplitSubset.Program.Run();
            StringSplitMoreSubset.Program.Run();
            StringReplaceNormalizeSubset.Program.Run();
            StringJoinSubset.Program.Run();
            StringSpanSubset.Program.Run();
            StringCopyToSubset.Program.Run();
            StringCtorMiscSubset.Program.Run();
            StringSurfaceSubset.Program.Run();
            StringFromCharsSubset.Program.Run();
            StringCreateSubset.Program.Run();
            StringEnumerableSubset.Program.Run();
            StringLinqSubset.Program.Run();
            StringInternSubset.Program.Run();
            StringCultureSpanSubset.Program.Run();
            StringValidationThrowSubset.Program.Run();
            StringNullFaultSubset.Program.Run();
            JoinCallResultSubset.Program.Run();
            OrdinalCultureComparerSubset.Program.Run();
        }
    }
}
