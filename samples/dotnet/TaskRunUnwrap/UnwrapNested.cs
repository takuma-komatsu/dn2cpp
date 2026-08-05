using System;
using System.Threading.Tasks;

namespace UnwrapNested
{
    // Nesting: the unwrap worker drains its own scheduler while the async lambda is
    // suspended on an inner Task.Run, whose cross-thread completion resumes it there.
    internal static class Program
    {
        private static async Task<int> Nested()
        {
            return await Task.Run(async () =>
            {
                int x = await Task.Run(() => 21);   // non-unwrap inner Task.Run<int>
                await Task.Delay(1);
                return x * 2;                         // 42
            });
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Nested().Result);   // 42
        }
    }
}
