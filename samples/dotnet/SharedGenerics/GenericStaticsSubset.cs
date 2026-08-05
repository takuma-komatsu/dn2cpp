#nullable enable
// Statics and static-initializer effects stay per REAL instantiation under
// sharing: three independent counters and tags over two enums and int, since
// static-touching bodies fall back per instantiation by the statics taint rule.
// Real System.Private.CoreLib (-r), run vs .NET.
using System;
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

class Program
{
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
