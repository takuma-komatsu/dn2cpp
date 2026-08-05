using System;

namespace CompressionCore
{
    // Auto-merged gate driver: runs each consolidated sample's Run() in
    // order. Each section keeps its own namespace so reflected type names
    // and other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main()
        {
            DeflateRoundTripSubset.Program.Run();
            GZipRoundTripSubset.Program.Run();
            CompressionLevelsSubset.Program.Run();
            CompressionLargeBufferSubset.Program.Run();
            GZipConcatenatedMembersSubset.Program.Run();
            CompressionErrorPathsSubset.Program.Run();
            CompressionAsyncRoundTripSubset.Program.Run();
            ZLibRoundTripSubset.Program.Run();
            BrotliRoundTripSubset.Program.Run();
        }
    }
}
