using System;
using System.Threading.Tasks;

namespace UnwrapResult
{
    // Task.Run(Func<Task<T>>): the outer Task<T> completes with the inner async lambda's
    // result. Covers each kind carried through the 8-byte result slot, plus a reference.
    internal static class Program
    {
        private static async Task<int> IntResult()
        {
            return await Task.Run(async () =>
            {
                await Task.Delay(1);
                return 42;
            });
        }

        private static async Task<long> LongResult()
        {
            return await Task.Run(async () =>
            {
                await Task.Delay(1);
                return 9_000_000_123L;
            });
        }

        private static async Task<double> DoubleResult()
        {
            return await Task.Run(async () =>
            {
                await Task.Delay(1);
                return 2.5;
            });
        }

        private static async Task<float> FloatResult()
        {
            return await Task.Run(async () =>
            {
                await Task.Delay(1);
                return 1.25f;
            });
        }

        private static async Task<string> RefResult()
        {
            return await Task.Run(async () =>
            {
                await Task.Delay(1);
                return "inner/done";
            });
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(IntResult().Result);     // 42
            Console.WriteLine(LongResult().Result);    // 9000000123
            Console.WriteLine(DoubleResult().Result);  // 2.5
            Console.WriteLine(FloatResult().Result);   // 1.25
            Console.WriteLine(RefResult().Result);     // inner/done
        }
    }
}
