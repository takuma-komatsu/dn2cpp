using System;

namespace HotUpdateBase;

public class AggregateProbe : AggregateException
{
    public int Code;
    public string Tag;

    public AggregateProbe() { Code = 17; Tag = "empty"; }
    public AggregateProbe(string? message) : base(message) { Code = 19; Tag = "message"; }
    public AggregateProbe(string? message, Exception[] inner) : base(message, inner) { Code = 23; Tag = "array"; }
    public AggregateProbe(string? message, Exception inner) : base(message, inner) { Code = 29; Tag = "single"; }
    public string BaseMessage() => base.Message;
}

public static class DerivedAggregateSubset
{
    public static string Describe(AggregateException aggregate, Exception? first)
    {
        return aggregate.Message + "/first=" + ReferenceEquals(aggregate.InnerException, first);
    }

    public static string Fields(int code, string tag) => code + "/" + tag;

    public static void Run()
    {
        GC.KeepAlive(typeof(UnavailableAggregateProbe));
        GC.KeepAlive(typeof(UnavailableOrdinaryProbe));
        Console.WriteLine("== AOT derived aggregates ==");
        var first = new Exception("first");
        var second = new Exception("second");
        var empty = new AggregateProbe();
        Console.WriteLine("AOT empty=" + Describe(empty, null) + "/" + Fields(empty.Code, empty.Tag));
        var message = new AggregateProbe("custom");
        Console.WriteLine("AOT message=" + Describe(message, null) + "/" + Fields(message.Code, message.Tag));
        var single = new AggregateProbe("single", first);
        Console.WriteLine("AOT single=" + Describe(single, first) + "/" + Fields(single.Code, single.Tag));
        var array = new[] { first, second };
        var pair = new AggregateProbe("pair", array);
        array[0] = new Exception("replacement");
        pair.Code = 41;
        pair.Tag = "after";
        Console.WriteLine("AOT snapshot=" + Describe(pair, first) + "/" + Fields(pair.Code, pair.Tag));
        Console.WriteLine("AOT base=" + pair.BaseMessage());
        Console.WriteLine("AOT derived aggregates end");
    }
}

// The unavailable-body fixture clears these constructor row pointers in its own image.
public sealed class UnavailableAggregateProbe : AggregateException
{
    public int Code;
    public UnavailableAggregateProbe(string message, Exception[] inner) : base(message, inner)
    {
        Code = 91;
    }
}

public sealed class UnavailableOrdinaryProbe : Exception
{
    public int Code;
    public UnavailableOrdinaryProbe(string message) : base("changed:" + message)
    {
        Code = 91;
    }
}
