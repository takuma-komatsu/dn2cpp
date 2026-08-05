// Exceptions across a custom-async-task await: the builder's SetException must fault the
// task, and the awaiter's GetResult must rethrow it at the awaiting frame.
//
// Both the void and the T-returning forms, and both a throw AFTER the first suspension (the
// runner promise faults) and one BEFORE it (the builder has no runner yet — the arm the
// library's FaultedSource covers). Every exception is observed, so nothing is left faulted at
// shutdown.

using System;
using System.Threading.Tasks;
using CustomAsyncTaskLib;

namespace CustomTaskFaultSubset;

internal static class Program
{
    // Throws before its first suspension: the builder never rents a runner.
    private static async CustomTask ThrowEarly(string message)
    {
        throw new InvalidOperationException(message);
#pragma warning disable CS0162 // unreachable — but it is what makes this method async
        await CustomTask.CompletedTask;
#pragma warning restore CS0162
    }

    private static async Task Run()
    {
        try
        {
            await CustomTaskApi.Throw("boom");
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine($"fault: caught {e.Message}");
        }

        try
        {
            int _ = await CustomTaskApi.ThrowResult("boom-result");
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine($"fault: caught {e.Message}");
        }

        try
        {
            await ThrowEarly("boom-early");
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine($"fault: caught {e.Message}");
        }

        Console.WriteLine("fault: done");
    }

    internal static void __GateEntry()
    {
        Run().GetAwaiter().GetResult();
    }
}
