using System;
using System.Globalization;

namespace StringBuild
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

            StringFormatSubset.Program.Run();
            StringBuilderSubset.Program.Run();
            StringBuilderEditSubset.Program.Run();
            StringBuilderMoreSubset.Program.Run();
            StringBuilderCopyToSubset.Program.Run();
            AppendJoinSubset.Program.Run();
            SpanFormatSubset.Program.Run();
            StringFormatThrowSubset.Program.Run();
            // Append new sections LAST: the previous output must stay an unchanged
            // prefix, or a perturbation of an earlier section reads as intentional.
            CompositeFormatSubset.Program.Run();
        }
    }
}
