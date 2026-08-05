using System;
using System.Threading.Tasks;

namespace UnwrapVoid
{
    // Task.Run(Func<Task>): the outer Task completes only after the inner async lambda
    // finishes, so the lambda's side effect is visible once the outer Task is awaited.
    internal static class Program
    {
        private static int s_sink;

        private static async Task VoidSideEffect()
        {
            s_sink = 0;
            await Task.Run(async () =>
            {
                await Task.Delay(1);
                s_sink = 7;
            });
        }

        internal static void __GateEntry()
        {
            VoidSideEffect().Wait();
            Console.WriteLine(s_sink);   // 7
        }
    }
}
