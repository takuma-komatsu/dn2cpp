// Genuine suspension through a custom async task type, three ways: on a real Task
// (Task.Yield), on another custom task (the adopted CustomTask.Awaiter resume path), and
// nested through both.
//
// Every await is SEQUENTIAL. Completion order must be a property of the program, not of the
// clock: the async-combinators gate's live real-.NET oracle already flakes on this machine
// because ITS timers run late under load, and this gate must not be able to.

using System;
using System.Threading.Tasks;
using CustomAsyncTaskLib;

namespace CustomTaskSuspendSubset;

internal static class Program
{
    private static async Task Run()
    {
        await CustomTaskApi.Yield();                        // suspends on Task.Yield
        Console.WriteLine("suspend: yielded");

        await CustomTaskApi.Seq(1, 2);                      // two suspensions, in order

        int sum = await CustomTaskApi.Sum(20, 22);          // suspends, then returns a result
        Console.WriteLine($"suspend: sum {sum}");

        string tag = await CustomTaskApi.Tag("hi");
        Console.WriteLine($"suspend: tag {tag}");

        // A custom task awaiting a custom task: the inner awaiter is the adopted
        // CustomTask<T>.Awaiter, not a Task one, so the resume goes through the adopted path.
        int nested = await CustomTaskApi.SumOfSums(1, 2, 3, 4);
        Console.WriteLine($"suspend: nested {nested}");

        Console.WriteLine("suspend: done");
    }

    internal static void __GateEntry()
    {
        Run().GetAwaiter().GetResult();
    }
}
