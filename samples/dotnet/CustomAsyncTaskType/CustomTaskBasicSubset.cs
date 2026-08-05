// An `async CustomTask` method that only awaits an already-completed custom task: nothing
// suspends, so the whole thing runs straight through MoveNext.
//
// CustomTask.CompletedTask is a static FIELD, not a property. An adopted type has no static
// storage, so this exercises the ldsfld fold; a dangling sf_ symbol here is a C++ LINK
// failure, which is why the field spelling matters.

using System;
using System.Threading.Tasks;
using CustomAsyncTaskLib;

namespace CustomTaskBasicSubset;

internal static class Program
{
    private static async CustomTask NopLocal()
    {
        await CustomTask.CompletedTask;
        Console.WriteLine("basic: awaited a completed custom task");
    }

    private static async Task Run()
    {
        await NopLocal();                       // an app-declared async CustomTask
        await CustomTaskApi.Nop();              // a library-declared one (cross-assembly)
        Console.WriteLine("basic: done");
    }

    internal static void __GateEntry()
    {
        Run().GetAwaiter().GetResult();
    }
}
