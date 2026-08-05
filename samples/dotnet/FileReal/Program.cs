using System;
using System.Globalization;

namespace FileReal
{
    // Auto-merged gate driver: runs each consolidated sample's __GateEntry() in
    // order. Each section keeps its own namespace so reflected type names and
    // other namespace-sensitive output stay identical to the originals.
    //
    // args[0] is a caller-supplied scratch directory: the gate gives the native
    // build and real .NET SEPARATE fresh directories and diffs their stdout
    // exactly, so no section may ever print an absolute path (nor an exception
    // Message, which embeds one — print exception TYPE names).
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            string dir = args[0];
            FileCoreSubset.Program.__GateEntry(dir);
            FileStreamSubset.Program.__GateEntry(dir);
            StreamTextSubset.Program.__GateEntry(dir);
            FileMetaSubset.Program.__GateEntry(dir);
            StreamAsyncSubset.Program.__GateEntry(dir);
            FileAsyncSubset.Program.__GateEntry(dir);
            FileAsyncEnumSubset.Program.__GateEntry(dir);
            // Take no scratch directory: they walk the non-file half of the same
            // PAL (process id, passwd lookup, special-folder paths), so they
            // have nothing to write.
            PalIdentitySubset.Program.__GateEntry();
            FolderPathSubset.Program.__GateEntry();
            // Also takes no scratch directory: it asserts what bounded native imports'
            // substituted call sites DO, touching no file. Must stay LAST — it is the only
            // section that provokes a throw, so anything it destabilises shows up as a
            // truncation rather than as a shifted diff.
            BoundedImportSubset.Program.__GateEntry();
            return 0;
        }
    }
}
