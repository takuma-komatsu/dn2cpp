using System;

namespace ZipCore
{
    // Gate driver: runs each section's __GateEntry() in order.
    internal static class Program
    {
        private static void Main()
        {
            ZipCreateRoundTripSubset.Program.__GateEntry();
            ZipFixedBlobReadSubset.Program.__GateEntry();
            ZipUpdateSubset.Program.__GateEntry();
            ZipCommentSubset.Program.__GateEntry();
            ZipErrorPathsSubset.Program.__GateEntry();
            ZipAsyncSubset.Program.__GateEntry();
        }
    }
}
