using System;

namespace RefStructGeneral
{
    // Gate driver: runs each section's __GateEntry() in order.
    internal static class Program
    {
        private static void Main()
        {
            RefFieldSubset.Program.__GateEntry();
            SpanGeneralSubset.Program.__GateEntry();
            StackallocStructSubset.Program.__GateEntry();
            StackallocVarSubset.Program.__GateEntry();
        }
    }
}
