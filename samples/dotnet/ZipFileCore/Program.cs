using System;

namespace ZipFileCore
{
    // Gate driver: runs each section's __GateEntry() in order.
    internal static class Program
    {
        private static void Main()
        {
            ZipFileRoundTripSubset.Program.__GateEntry();
            ZipFileOpenSubset.Program.__GateEntry();
            ZipFileErrorPathsSubset.Program.__GateEntry();
            ZipFileAsyncSubset.Program.__GateEntry();
        }
    }
}
