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

    internal static void RunMultiple()
    {
        Console.WriteLine("== WaitHandle multiple waits ==");
        using var auto = new AutoResetEvent(true);
        using var manual = new ManualResetEvent(false);
        WaitHandle[] pair = { auto, manual };
        Console.WriteLine("all partial=" + WaitHandle.WaitAll(pair, 2) + "/" + auto.WaitOne(0));
        auto.Set();
        manual.Set();
        Console.WriteLine("all infinite=" + WaitHandle.WaitAll(pair) + "/" + auto.WaitOne(0) + "/" + manual.WaitOne(0));
        Console.WriteLine("all span=" + WaitHandle.WaitAll(pair, TimeSpan.Zero));
        Console.WriteLine("any milliseconds=" + WaitHandle.WaitAny(pair, 5));
        auto.Set();
        Console.WriteLine("any span=" + WaitHandle.WaitAny(pair, TimeSpan.FromMilliseconds(5)));
        auto.Set();
        Console.WriteLine("all context=" + WaitHandle.WaitAll(pair, 0, true)
            + "/" + WaitHandle.WaitAll(pair, TimeSpan.Zero, false));
        manual.Reset();
        Console.WriteLine("any context=" + WaitHandle.WaitAny(pair, 2, false)
            + "/" + WaitHandle.WaitAny(pair, TimeSpan.Zero, true));

        // Distinct WaitHandle objects may refer to the same underlying event.
        using var alias = new AttachedHandle(manual.SafeWaitHandle);
        manual.Set();
        WaitHandle[] aliases = { alias, manual };
        Console.WriteLine("any alias=" + WaitHandle.WaitAny(aliases, 0));
        try
        {
            Console.WriteLine("all alias=" + WaitHandle.WaitAll(aliases, 0));
        }
        catch (DuplicateWaitObjectException error)
        {
            Console.WriteLine("all alias=duplicate/" + error.ParamName);
        }

#if DN2CPP_HOST_WINDOWS
        using var native = NewNativeAutoEvent(true);
        using var unset = NewNativeAutoEvent(false);
        auto.Set();
        Console.WriteLine("mixed priority=" + WaitHandle.WaitAny(new WaitHandle[] { auto, native }, 0));
        auto.Set();
        Console.WriteLine("mixed partial=" + WaitHandle.WaitAll(new WaitHandle[] { native, auto, unset }, 0));
        Console.WriteLine("mixed success=" + WaitHandle.WaitAll(new WaitHandle[] { native, auto }, 0)
            + "/" + native.WaitOne(0) + "/" + auto.WaitOne(0));
#endif
        Console.WriteLine("WaitHandle multiple waits end");
    }

    private sealed class AttachedHandle : WaitHandle
    {
        internal AttachedHandle(Microsoft.Win32.SafeHandles.SafeWaitHandle handle) => SafeWaitHandle = handle;
    }

#if DN2CPP_HOST_WINDOWS
    [System.Runtime.InteropServices.DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern nint CreateEventW(nint attributes, int manualReset, int initialState, nint name);

    private static WaitHandle NewNativeAutoEvent(bool ready)
    {
        nint handle = CreateEventW(0, 0, ready ? 1 : 0, 0);
        if (handle == 0)
            throw new InvalidOperationException("CreateEventW failed");
        return new AttachedHandle(new Microsoft.Win32.SafeHandles.SafeWaitHandle(handle, ownsHandle: true));
    }
#endif
}
