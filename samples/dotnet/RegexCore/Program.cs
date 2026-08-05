using System;

namespace RegexCore
{
    // Gate driver: runs each section's __GateEntry() in order. Each section
    // keeps its own namespace — some output is namespace-sensitive.
    internal static class Program
    {
        private static void Main()
        {
            RegexMatchBasicsSubset.Program.__GateEntry();
            RegexGroupsSubset.Program.__GateEntry();
            RegexClassesQuantifiersSubset.Program.__GateEntry();
            RegexIgnoreCaseSubset.Program.__GateEntry();
            RegexIgnoreCaseCultureSubset.Program.__GateEntry();
            RegexReplaceSplitSubset.Program.__GateEntry();
            RegexOptionsSubset.Program.__GateEntry();
            RegexEscapeSubset.Program.__GateEntry();
            RegexTimeoutSubset.Program.__GateEntry();
            RegexCountEnumerateSubset.Program.__GateEntry();
            RegexNonBacktrackingSubset.Program.__GateEntry();
            RegexApiSurfaceSubset.Program.__GateEntry();
            RegexGeneratedSubset.Program.__GateEntry();
        }
    }
}
