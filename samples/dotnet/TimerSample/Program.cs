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
        PublicTimerInterfaces();
        SystemTimeProviderOneShot();
        SystemTimeProviderChange();
        SystemTimeProviderDisposeAsync();
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

    static void PublicTimerInterfaces()
    {
        int fires = 0;
        var timer = new Timer(_ => Interlocked.Increment(ref fires), null,
            TimeSpan.FromHours(1), Timeout.InfiniteTimeSpan);
        object boxed = timer;
        var disposed = timer.DisposeAsync();
        disposed.GetAwaiter().GetResult();
        Console.WriteLine($"timer type={timer.GetType().FullName} itimer={boxed is ITimer} " +
            $"async={disposed.IsCompletedSuccessfully} stopped={fires == 0}");
    }

    // TimeProvider.System constructs CoreLib's private ITimer adapter. The callback and
    // state use the same runtime timer path as the public Timer constructors.
    static void SystemTimeProviderOneShot()
    {
        var done = new CountdownEvent(1);
        int observed = -1;
        ITimer timer = TimeProvider.System.CreateTimer(s =>
        {
            observed = (int)s!;
            done.Signal();
        }, 11, TimeSpan.FromMilliseconds(30), Timeout.InfiniteTimeSpan);
        var publicTimer = new Timer(_ => { });
        bool distinct = publicTimer.GetType() != timer.GetType();
        publicTimer.Dispose();
        Console.WriteLine($"provider type={timer.GetType().FullName} itimer={((object)timer is ITimer)} distinct={distinct}");
        done.Wait();
        timer.Dispose();
        Console.WriteLine($"provider state={observed}");                // provider state=11
    }

    // Exercise Change through the ITimer interface, not the concrete Timer intrinsic.
    static void SystemTimeProviderChange()
    {
        var done = new CountdownEvent(1);
        int fires = 0;
        ITimer timer = TimeProvider.System.CreateTimer(_ =>
        {
            Interlocked.Increment(ref fires);
            done.Signal();
        }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        bool changed = timer.Change(TimeSpan.FromMilliseconds(30), Timeout.InfiniteTimeSpan);
        done.Wait();
        timer.Dispose();
        Console.WriteLine($"provider change={changed} fires={fires}");  // provider change=True fires=1
    }

    // ITimer inherits IAsyncDisposable. The runtime adapter stops synchronously and
    // returns an already-completed ValueTask through the interface slot.
    static void SystemTimeProviderDisposeAsync()
    {
        ITimer timer = TimeProvider.System.CreateTimer(
            _ => { }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        var disposed = timer.DisposeAsync();
        disposed.GetAwaiter().GetResult();
        Console.WriteLine($"provider async disposed={disposed.IsCompletedSuccessfully}");
    }

}
