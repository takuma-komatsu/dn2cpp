using System;
using System.Globalization;

namespace StringBuild
{
    // Gate driver. Each section keeps its own namespace so reflected type names
    // stay identical to the standalone samples.
    internal static class Program
    {
        private static void Main(string[] args)
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
            if (args.Length > 0 && args[0] == "before-null-receivers")
                return;
            StringBuilderNullReceiverSubset.Program.Run();
            if (args.Length > 0 && args[0] == "before-builder-ranges")
                return;
            StringBuilderRangeSubset.Program.Run();
            if (args.Length > 0 && args[0] == "before-builder-copy-faults")
                return;
            StringBuilderCopyToSubset.Program.RunFaults();
            if (args.Length > 0 && args[0] == "before-builder-edit-faults")
                return;
            StringBuilderEditSubset.Program.RunFaults();
            if (args.Length > 0 && args[0] == "before-builder-state-faults")
                return;
            StringBuilderMoreSubset.Program.RunFaults();
            if (args.Length > 0 && args[0] == "before-builder-insert-faults")
                return;
            StringBuilderMoreSubset.Program.RunInsertFaults();
            if (args.Length > 0 && args[0] == "before-builder-collection-faults")
                return;
            StringBuilderMoreSubset.Program.RunCollectionFaults();
            if (args.Length > 0 && args[0] == "before-format-faults")
                return;
            StringFormatSubset.Program.RunFaults();
        }
    }
}
