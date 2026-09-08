#nullable enable
// Statics and static-initializer effects stay per REAL instantiation under
// sharing: three independent counters and tags over two enums and int, since
// static-touching bodies fall back per instantiation by the statics taint rule.
// Real System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Runtime.CompilerServices;
using System.Threading;
namespace GenericStaticsSubset;

enum Mode { Off = 0, On = 1 }
enum Gear { Low = 1, High = 2 }

class Counter<T>
{
    public static int Created;
    private static readonly string Tag = "tag:" + typeof(T).Name;
    public T Value;

    public Counter(T v)
    {
        Value = v;
        Created++;
    }

    public static string Describe() => Tag + "#" + Created;
}

class SynchronizedOwner<T>
{
    // The IL has no type-dependent operation; only the static monitor prologue
    // makes this body ineligible for canonical sharing.
    [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.NoInlining)]
    public static bool StaticProbe(object monitor) => MonitorProbe.CanEnter(monitor);

    [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.NoInlining)]
    public bool InstanceProbe(object monitor) => MonitorProbe.CanEnter(monitor);
}

static class MonitorProbe
{
    public static bool CanEnter(object monitor)
    {
        bool entered = false;
        var thread = new Thread(() =>
        {
            entered = Monitor.TryEnter(monitor, 0);
            if (entered)
                Monitor.Exit(monitor);
        });
        thread.Start();
        thread.Join();
        return entered;
    }
}

class Program
{
    internal static void SynchronizedPrologues()
    {
        Console.WriteLine("sync static string own=" + SynchronizedOwner<string>.StaticProbe(typeof(SynchronizedOwner<string>)));
        Console.WriteLine("sync static object own=" + SynchronizedOwner<object>.StaticProbe(typeof(SynchronizedOwner<object>)));
        Console.WriteLine("sync static other=" + SynchronizedOwner<string>.StaticProbe(typeof(SynchronizedOwner<object>)));
        var first = new SynchronizedOwner<string>();
        var second = new SynchronizedOwner<object>();
        Console.WriteLine("sync instance string own=" + first.InstanceProbe(first));
        Console.WriteLine("sync instance object own=" + second.InstanceProbe(second));
        Console.WriteLine("sync instance other=" + first.InstanceProbe(second));
    }

    internal static void __GateEntry()
    {
        var a = new Counter<Mode>(Mode.On);
        var b = new Counter<Mode>(Mode.Off);
        var c = new Counter<Gear>(Gear.High);
        var d = new Counter<int>(42);
        Console.WriteLine(Counter<Mode>.Describe());
        Console.WriteLine(Counter<Gear>.Describe());
        Console.WriteLine(Counter<int>.Describe());
        Console.WriteLine("values=" + a.Value + "," + b.Value + "," + c.Value + "," + d.Value);
        var e = new Counter<Gear>(Gear.Low);
        Console.WriteLine("after=" + Counter<Gear>.Describe() + " mode=" + Counter<Mode>.Created);
        Console.WriteLine("e=" + e.Value);
    }
}
