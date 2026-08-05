using System;
using System.Threading;

// System.Threading.Timer. Firing is timing-based, so a latch gates main until the expected
// number of fires has happened and only stable facts are printed — never a timing-dependent
// tick count. Every timer is Disposed so its thread joins and the program exits.
static class Program
{
    static void Main()
    {
        OneShot();
        Periodic();
        StartViaChange();
        OneShotTimeSpan();
        LongOverload();
    }

    // An Infinite period never re-fires, so the count is stable once the latch trips and
    // the Dispose has joined the thread.
    static void OneShot()
    {
        var done = new CountdownEvent(1);
        int fires = 0, observed = -1;
        var timer = new Timer(s =>
        {
            Interlocked.Increment(ref fires);
            observed = (int)s!;
            done.Signal();
        }, 7, 30, Timeout.Infinite);
        done.Wait();
        timer.Dispose();
        Console.WriteLine($"oneshot fires={fires} state={observed}");   // oneshot fires=1 state=7
    }

    // The latch is signalled off the unique Interlocked result, so it counts the first N
    // ticks even if callbacks overlap. Only the >= N predicate is deterministic.
    static void Periodic()
    {
        const int N = 3;
        var reached = new CountdownEvent(N);
        int count = 0;
        var timer = new Timer(_ =>
        {
            int c = Interlocked.Increment(ref count);
            if (c <= N)
                reached.Signal();
        }, null, 10, 10);
        reached.Wait();
        timer.Dispose();
        Console.WriteLine($"periodic count>={N}: {count >= N}");        // periodic count>=3: True
    }

    // The 1-arg ctor starts idle: the timer only fires once Change arms it.
    static void StartViaChange()
    {
        var done = new CountdownEvent(1);
        int fires = 0;
        var timer = new Timer(_ => { Interlocked.Increment(ref fires); done.Signal(); });
        bool ok = timer.Change(30, Timeout.Infinite);
        done.Wait();
        timer.Dispose();
        Console.WriteLine($"change ok={ok} fires={fires}");             // change ok=True fires=1
    }

    // TimeSpan overload: a -1 ms period is infinite.
    static void OneShotTimeSpan()
    {
        var done = new ManualResetEventSlim(false);
        int fires = 0;
        var timer = new Timer(_ => { Interlocked.Increment(ref fires); done.Set(); },
                              null, TimeSpan.FromMilliseconds(30.0), TimeSpan.FromMilliseconds(-1.0));
        done.Wait();
        timer.Dispose();
        Console.WriteLine($"timespan fires={fires}");                   // timespan fires=1
    }

    // long dueTime/period overload.
    static void LongOverload()
    {
        var done = new CountdownEvent(1);
        int fires = 0;
        var timer = new Timer(_ => { Interlocked.Increment(ref fires); done.Signal(); },
                              null, 30L, Timeout.Infinite);
        done.Wait();
        timer.Dispose();
        Console.WriteLine($"long fires={fires}");                       // long fires=1
    }
}
