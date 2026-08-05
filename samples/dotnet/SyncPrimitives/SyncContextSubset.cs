using System;
using System.Threading;

// SynchronizationContext.Current is a real per-thread slot, not a constant null:
// installed contexts read back by identity, Post/Send dispatch through them, a fresh
// thread sees null, and a worker's install never leaks to the main thread.
namespace SyncContextSubset;

internal static class Program
{
    private sealed class CountingContext : SynchronizationContext
    {
        public int Posted;
        public int Sent;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Posted++;
            d(state);
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Sent++;
            d(state);
        }
    }

    internal static void Run()
    {
        Console.WriteLine(SynchronizationContext.Current is null);               // True

        var ctx = new CountingContext();
        SynchronizationContext.SetSynchronizationContext(ctx);
        Console.WriteLine(ReferenceEquals(SynchronizationContext.Current, ctx)); // True

        int ran = 0;
        ctx.Post(_ => ran++, null);                    // direct call on the override
        SynchronizationContext.Current!.Send(_ => ran++, null); // virtual through Current
        Console.WriteLine(ran);                        // 2
        Console.WriteLine(ctx.Posted);                 // 1
        Console.WriteLine(ctx.Sent);                   // 1

        // A fresh thread starts with null, reads back what it installs, and does not
        // disturb the main thread's slot.
        bool workerSawNull = false;
        bool workerReadBack = false;
        var t = new Thread(() =>
        {
            workerSawNull = SynchronizationContext.Current is null;
            var mine = new CountingContext();
            SynchronizationContext.SetSynchronizationContext(mine);
            workerReadBack = ReferenceEquals(SynchronizationContext.Current, mine);
        });
        t.Start();
        t.Join();
        Console.WriteLine(workerSawNull);                                        // True
        Console.WriteLine(workerReadBack);                                       // True
        Console.WriteLine(ReferenceEquals(SynchronizationContext.Current, ctx)); // True

        // Uninstall: the slot goes back to null.
        SynchronizationContext.SetSynchronizationContext(null);
        Console.WriteLine(SynchronizationContext.Current is null);               // True
    }
}
