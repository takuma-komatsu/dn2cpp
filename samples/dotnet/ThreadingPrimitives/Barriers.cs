using System;
using System.Threading;

// The memory fences have no observable value effect on one thread, so the markers around
// them are the assertion: they must execute without throwing or deadlocking.
namespace Barriers;

internal static class Program
{
    internal static void __GateEntry()
    {
        int x = 1;
        Console.WriteLine("barrier:before");
        Interlocked.MemoryBarrier();
        x++;
        Interlocked.MemoryBarrierProcessWide();
        x++;
        Thread.MemoryBarrier();
        Console.WriteLine(x);              // 3
        Console.WriteLine("barrier:after");
    }
}
