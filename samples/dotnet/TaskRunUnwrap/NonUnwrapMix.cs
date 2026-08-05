using System;
using System.Threading.Tasks;

namespace NonUnwrapMix
{
    // The non-unwrap Task.Run overloads (Func<int>, Action) beside the unwrap sections:
    // the unwrap routing must not disturb the value/void worker-pool paths.
    internal static class Program
    {
        private static int s_actionSink;

        private static async Task<int> Mix()
        {
            int a = await Task.Run(() => 1000);            // Func<int> -> Task<int>
            await Task.Run(() => { s_actionSink = 5; });   // Action -> Task
            return a + s_actionSink;                        // 1005
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Mix().Result);   // 1005
        }
    }
}
