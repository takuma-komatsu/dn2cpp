#nullable disable
using System;
using System.Threading.Tasks;

namespace WhenAllSubset
{
    // Task.WhenAll<TResult>(Task<TResult>[]) -> Task<TResult[]>. Each input
    // task suspends (Task.Yield) and resumes on the cooperative scheduler; WhenAll
    // joins them by registering a continuation on each, and on the last completion
    // builds the result array. The element kind selects the array representation
    // (int -> int[], reference -> string/object[], long/double -> 8-byte element[]).
    internal static class Program
    {
        private static async Task<int> Square(int x)
        {
            await Task.Yield();
            return x * x;
        }

        private static async Task<string> Label(int x)
        {
            await Task.Yield();
            return "v" + x;
        }

        private static async Task<long> Big(int x)
        {
            await Task.Yield();
            return (long)x * 1000000000L;
        }

        private static async Task<int> Run()
        {
            int[] squares = await Task.WhenAll(new Task<int>[] { Square(2), Square(3), Square(4) });
            int sumSq = squares[0] + squares[1] + squares[2]; // 4 + 9 + 16 = 29

            string[] labels = await Task.WhenAll(new Task<string>[] { Label(1), Label(2) });
            string joined = labels[0] + labels[1]; // "v1v2"

            long[] bigs = await Task.WhenAll(new Task<long>[] { Big(2), Big(3) });
            long bigSum = bigs[0] + bigs[1]; // 5_000_000_000

            Console.WriteLine(sumSq);    // 29
            Console.WriteLine(joined);   // v1v2
            Console.WriteLine(bigSum);   // 5000000000
            return sumSq;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result); // 29
        }
    }
}
