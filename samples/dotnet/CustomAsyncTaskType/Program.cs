namespace CustomAsyncTaskType
{
    // Auto-merged gate driver: runs each consolidated sample's __GateEntry() in
    // order. Each section keeps its own namespace so reflected type names and
    // other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main()
        {
            CustomTaskBasicSubset.Program.__GateEntry();
            CustomTaskSuspendSubset.Program.__GateEntry();
            CustomTaskFaultSubset.Program.__GateEntry();
            CustomTaskGenericSubset.Program.__GateEntry();
            CustomTaskLocalSubset.Program.__GateEntry();
            CustomTaskValueTaskSubset.Program.__GateEntry();
        }
    }
}
