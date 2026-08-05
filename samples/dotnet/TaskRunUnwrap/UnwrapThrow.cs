using System;
using System.Threading.Tasks;

namespace UnwrapThrow
{
    // Fault propagation through the unwrap: the outer Task settles with the SAME exception
    // the inner lambda threw, and awaiting it re-raises that exception unwrapped (not an
    // AggregateException), so a typed catch matches. Both the Func<Task<T>> and Func<Task>.
    internal static class Program
    {
        private static async Task<string> ThrowFromResult()
        {
            try
            {
                int v = await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    throw new InvalidOperationException("boom-result");
#pragma warning disable CS0162
                    return 0;
#pragma warning restore CS0162
                });
                return "no-throw:" + v;
            }
            catch (InvalidOperationException ex)
            {
                return "caught:" + ex.Message;
            }
        }

        private static async Task<string> ThrowFromVoid()
        {
            try
            {
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    throw new InvalidOperationException("boom-void");
                });
                return "no-throw";
            }
            catch (InvalidOperationException ex)
            {
                return "caught:" + ex.Message;
            }
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(ThrowFromResult().Result);  // caught:boom-result
            Console.WriteLine(ThrowFromVoid().Result);    // caught:boom-void
        }
    }
}
