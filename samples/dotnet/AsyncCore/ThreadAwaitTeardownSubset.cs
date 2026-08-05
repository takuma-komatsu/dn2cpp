#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

// A spawned thread's async method suspends at its first await and returns, so the OS
// thread ends while the registered continuation still names that thread's scheduler.
// When the pool worker later completes the awaited task it routes the continuation to
// that scheduler from another thread — which must not touch a destroyed lock.
//
// dn2cpp keeps every scheduler alive for the process, so the hand-off is safe; nothing
// pumps a dead thread's queue, so the post-await tail never runs. Real .NET captures no
// synchronization context here and resumes the tail on the pool, so `tail ran` would be
// 1 there (whenever the pool got to it) — this section is frozen, not diffed, because
// of that divergence.
namespace ThreadAwaitTeardown;

static class Program
{
    static readonly ManualResetEventSlim s_release = new ManualResetEventSlim(false);
    static int s_tailRan;

    static async Task Orphan()
    {
        await Task.Run(() => s_release.Wait());
        Volatile.Write(ref s_tailRan, 1);
    }

    // Dropping the returned Task is the point: the thread body returns as soon as
    // Orphan suspends, rather than draining its own scheduler until the task settles.
    static void ThreadBody()
    {
        _ = Orphan();
    }

    internal static void __GateEntry()
    {
        var th = new Thread(ThreadBody);
        th.Start();
        th.Join(); // returns at Orphan's await — the thread is gone, its scheduler is not
        Console.WriteLine("thread joined");

        s_release.Set(); // the pool worker completes the task and fires the continuation
        Thread.Sleep(50);

        Console.WriteLine("tail ran = " + Volatile.Read(ref s_tailRan));
        Console.WriteLine("survived cross-thread completion");
    }
}
