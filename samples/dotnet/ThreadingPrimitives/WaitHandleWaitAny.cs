using System;
using System.Threading;

// WaitHandle.WaitAny returns the index of the first signaled handle. Every call here
// pre-signals a handle so it returns immediately, keeping the section single-threaded.
namespace WaitHandleWaitAny;

internal static class Program
{
    internal static void __GateEntry()
    {
        var mre0 = new ManualResetEvent(false);
        var mre1 = new ManualResetEvent(false);
        var mre2 = new ManualResetEvent(false);
        WaitHandle[] handles = { mre0, mre1, mre2 };

        mre1.Set();
        Console.WriteLine(WaitHandle.WaitAny(handles)); // 1 — only mre1 is signaled

        mre0.Set();
        Console.WriteLine(WaitHandle.WaitAny(handles)); // 0 — lowest signaled index wins
                                                        // (manual-reset handles stay set)

        // AutoResetEvent: WaitAny consumes exactly the handle it returns.
        var are = new AutoResetEvent(true);
        WaitHandle[] autos = { are };
        Console.WriteLine(WaitHandle.WaitAny(autos)); // 0
        Console.WriteLine(are.WaitOne(0));            // False — the signal was consumed
    }
}
