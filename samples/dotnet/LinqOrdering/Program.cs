using System;

namespace LinqOrdering
{
    // Auto-merged gate driver: runs each consolidated sample's Run() in
    // order. Each section keeps its own namespace so reflected type names
    // and other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main()
        {
            LinqOrderSubset.Program.Run();
            LinqOrderComparerSubset.Program.Run();
            LinqComparerSubset.Program.Run();
            LinqOrderStructKeySubset.Program.Run();
        }
    }
}
