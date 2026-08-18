// A program that EXCEEDS the adoption contract: it awaits CustomTask.Yield(), a static
// member on the task type with no BCL counterpart (UniTask.Yield's shape). That
// cross-assembly MemberRef makes the adoption pre-scan decline CustomAsyncTaskLib
// automatically — no flag — so the library's real machinery (builder, Runner, TaskPool,
// scheduler) transpiles through the general pipeline and this program must still run
// byte-identically to real .NET.
//
// Every await is SEQUENTIAL: completion order is a property of the program, never of the
// clock (same rule as the in-contract bucket's suspend section).

using System;
using System.Threading.Tasks;
using CustomAsyncTaskLib;

namespace CustomTaskYieldSubset;

internal static class Program
{
    private static async CustomTask YieldOnce()
    {
        Console.WriteLine("exceed: before yield");
        await CustomTask.Yield();
        Console.WriteLine("exceed: after yield");
    }

    private static async CustomTask<int> YieldSum(int a, int b)
    {
        await CustomTask.Yield();
        return a + b;
    }

    private static async Task Run()
    {
        await YieldOnce();                        // the out-of-contract awaitable itself

        int sum = await YieldSum(20, 22);         // real generic builder + Runner<TSM,T>
        Console.WriteLine($"exceed: sum {sum}");

        await CustomTaskApi.Seq(1, 2);            // the library's own cores, un-adopted

        int nested = await CustomTaskApi.SumOfSums(1, 2, 3, 4);
        Console.WriteLine($"exceed: nested {nested}");

        Console.WriteLine("exceed: done");
    }

    internal static void __GateEntry()
    {
        Run().GetAwaiter().GetResult();
    }
}
