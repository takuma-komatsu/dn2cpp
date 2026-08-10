#nullable enable
using System;
using System.Threading.Tasks;

// FfSettler's opposite: there every settling item SLEEPS so the waiter reaches the drain
// with the settler still counted; here the pool worker settles and leaves the in-flight
// count as fast as possible, so the waiter's "no settler" read races the worker's
// continuation enqueue. The blocked task is one indirection from the pool task — an async
// method awaiting a started cold task, blocked on with .Result — so at the zero read the
// waited task is still pending and only the freshly-enqueued MoveNext can settle it: a
// drain that turns the empty count into a verdict without re-probing its own queue dies
// here with a false deadlock. Looped, because the window is a few instructions wide.
// Identical output on real .NET, which never diagnoses a deadlock.
namespace FastSettler;

static class Program
{
    static async Task<int> AwaitCold(int i)
    {
        // Near-instant delegate on purpose: the worker's settle -> enqueue -> leave
        // epilogue must land inside the waiter's probe window; any sleep closes it.
        var cold = new Task<int>(() => i * 3);
        cold.Start();
        int r = await cold;
        return r + 1;
    }

    public static void __GateEntry()
    {
        int sum = 0;
        for (int i = 0; i < 1000; i++)
            sum += AwaitCold(i).Result;
        Console.WriteLine("fast settler: " + sum);
    }
}
