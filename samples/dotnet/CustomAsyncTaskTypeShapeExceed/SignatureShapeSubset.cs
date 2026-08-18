// A same-name overload is not the await-pattern member. The metadata pre-scan must decline
// this library before intrinsic dispatch can consume the wrong number of IL operands.

using System;
using CustomAsyncTaskLib;

namespace SignatureShapeSubset;

internal static class Program
{
    private static void Run()
    {
        CustomTask.CompletedTask.GetAwaiter(0).GetResult();
        Console.WriteLine("shape: overload");
        Console.WriteLine("shape: done");
    }

    internal static void __GateEntry()
    {
        Run();
    }
}
