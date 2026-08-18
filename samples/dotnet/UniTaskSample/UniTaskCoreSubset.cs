using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace UniTaskCoreSubset
{
    // The real NuGet UniTask's core surface: AsyncUniTaskMethodBuilder (both arities,
    // implicit in every async UniTask method here), Yield, CompletedTask, FromResult,
    // Run, SwitchToThreadPool, Task interop (AsUniTask/AsTask), WhenAll (both
    // arities), and exception propagation through the library's
    // ExceptionResultSource. UniTask.Delay does not exist in the NuGet .NET TFMs —
    // it is Unity-PlayerLoop-scheduled — so timer waits ride Task.Delay().AsUniTask.
    // Every await is sequential — completion order is a property of the program,
    // never of the clock or the thread pool.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            MainFlow().AsTask().GetAwaiter().GetResult();
        }

        private static async UniTask MainFlow()
        {
            Console.WriteLine("[completed] " + UniTask.CompletedTask.Status);

            var fr = UniTask.FromResult(42);
            Console.WriteLine("[fromresult] " + await fr);

            Console.WriteLine("[yield] before");
            await UniTask.Yield();
            Console.WriteLine("[yield] after");

            await Task.Delay(10).AsUniTask(useCurrentSynchronizationContext: false);
            Console.WriteLine("[delay] done");

            int ran = await UniTask.Run(() => 6 * 7);
            Console.WriteLine("[run] " + ran);

            Console.WriteLine("[chain] " + await AddViaChain(20, 3));

            // WhenAll over UniTask<T>[]: results arrive positionally, printed only
            // after the whole set completed — no per-task race can reorder them.
            var results = await UniTask.WhenAll(Slot(1), Slot(2), Slot(3));
            Console.WriteLine("[whenall<T>] " + results.Item1 + "," + results.Item2 + "," + results.Item3);

            var arr = await UniTask.WhenAll(new[] { Slot(10), Slot(20) });
            Console.WriteLine("[whenall arr] " + arr[0] + "," + arr[1]);

            int side = 0;
            await UniTask.WhenAll(Bump(() => side += 5), Bump(() => side += 7));
            Console.WriteLine("[whenall void] side=" + side);

            try
            {
                await Throwing();
                Console.WriteLine("[throw] not reached");
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("[throw] caught " + e.Message);
            }

            try
            {
                int v = await ThrowingTyped();
                Console.WriteLine("[throw<T>] not reached " + v);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("[throw<T>] caught " + e.Message);
            }

            Console.WriteLine("[end] ok");
        }

        private static async UniTask<int> AddViaChain(int a, int b)
        {
            await UniTask.Yield();
            int sum = a + await Inner(b);
            return sum;
        }

        private static async UniTask<int> Inner(int b)
        {
            await UniTask.SwitchToThreadPool();
            return b;
        }

        private static async UniTask<int> Slot(int n)
        {
            await UniTask.Yield();
            return n * 2;
        }

        private static async UniTask Bump(Action act)
        {
            await UniTask.Yield();
            act();
        }

        private static async UniTask Throwing()
        {
            await UniTask.Yield();
            throw new InvalidOperationException("boom");
        }

        private static async UniTask<int> ThrowingTyped()
        {
            await Task.Delay(1).AsUniTask(useCurrentSynchronizationContext: false);
            throw new ArgumentException("typed boom");
        }
    }
}
