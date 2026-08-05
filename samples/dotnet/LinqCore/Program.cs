using System;
using System.Globalization;

namespace LinqCore
{
    // Auto-merged gate driver: runs each consolidated sample's Run() in
    // order. Each section keeps its own namespace so reflected type names
    // and other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            LinqSubset.Program.Run();
            LinqMoreSubset.Program.Run();
            LinqMore2Subset.Program.Run();
            LinqSetSubset.Program.Run();
            LinqMinMaxSubset.Program.Run();
            LinqOfTypeSubset.Program.Run();
            LinqNullableSubset.Program.Run();
            LinqByKeySubset.Program.Run();
            LinqGroupSubset.Program.Run();
            LinqIndexSubset.Program.Run();
            ArrayLinqSubset.Program.Run();
            ValueLinqWhereSubset.Program.Run();
            ValueArrayFromCallLinqSubset.Program.Run();
        }
    }
}
