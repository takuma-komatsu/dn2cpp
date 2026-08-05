using System;
using System.Globalization;

namespace ArrayDispatch
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

            ArrayCovarianceSubset.Program.Run();
            ArrayCovariantDispatchSubset.Program.Run();
            ArrayInterfaceDispatchSubset.Program.Run();
            ArrayIntrinsicElementDispatchSubset.Program.Run();
            ArrayInterfaceSetSubset.Program.Run();
            ArrayRuntimeCollectionDispatchSubset.Program.Run();
            ObjectReachedDispatchSubset.Program.Run();
            ReflectionArrayFallbackSubset.Program.Run();
            JaggedElementInterfaceSubset.Program.Run();
            GenericVarianceDispatchSubset.Program.Run();
            MdArrayLiteralSubset.Program.Run();
            MultiDimArraySubset.Program.Run();
            ObjectReachedValueDispatchSubset.Program.Run();
            MdInterfaceDispatchSubset.Program.Run();
        }
    }
}
