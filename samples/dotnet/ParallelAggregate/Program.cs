using System;

namespace ParallelAggregate
{
    // Gate driver: runs each section's __GateEntry() in order. Each section keeps
    // its own namespace, so namespace-sensitive output is stable, and each is
    // deterministic — the bucket is diffed exactly against real .NET.
    internal static class Program
    {
        private static void Main()
        {
            InvokeAggregate.Program.__GateEntry();
            ForWrap.Program.__GateEntry();
            ForEachWrap.Program.__GateEntry();
            ManualCtor.Program.__GateEntry();
            InvokePressure.Program.__GateEntry();
            InnerExceptionsDeclaredType.Program.__GateEntry();
        }
    }
}
