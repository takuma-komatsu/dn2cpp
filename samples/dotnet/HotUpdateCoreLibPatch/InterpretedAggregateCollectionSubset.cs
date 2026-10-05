using System;
using HotUpdateCoreLibBase;

namespace HotUpdateCoreLibPatch;

public static class InterpretedAggregateCollectionSubset
{
    public static string Run()
    {
        Console.WriteLine("== interpreted aggregate collections ==");
        var first = new Exception("first");
        var second = new Exception("second");
        var values = new[] { first, second };
        var direct = new DirectCollectionAggregate("direct", values);
        var belowAot = new PatchCollectionAggregate("below AOT", values);
        values[0] = new Exception("replacement");
        Console.WriteLine("collection direct=" + AggregateCollectionProbe.Inspect(direct, first, second)
            + "/" + AggregateCollectionProbe.Fields(direct.Code, direct.Tag));
        direct.Code = 43;
        direct.Tag = "after";
        Console.WriteLine("collection rewritten=" + AggregateCollectionProbe.Inspect(direct, first, second)
            + "/" + AggregateCollectionProbe.Fields(direct.Code, direct.Tag));
        Console.WriteLine("collection below AOT=" + AggregateCollectionProbe.Inspect(belowAot, first, second)
            + "/" + AggregateCollectionProbe.Fields(belowAot.Code, belowAot.Tag) + "/" + belowAot.Origin);
        var empty = new DirectCollectionAggregate("empty", new Exception[0]);
        Console.WriteLine("collection empty=" + AggregateCollectionProbe.Inspect(empty, null, null)
            + "/" + AggregateCollectionProbe.Fields(empty.Code, empty.Tag));
        return "interpreted aggregate collections end";
    }

    public static string RunMessageDispatch()
    {
        Console.WriteLine("== interpreted aggregate Message dispatch ==");
        var value = new MessageCollectionAggregate("base", new[] { new Exception("inner") });
        Console.WriteLine("patch Message base=" + value.BaseMessage());
        Console.WriteLine("patch Message base reads=" + AggregateCollectionProbe.Fields(value.Reads, "reads"));
        AggregateException aggregate = value;
        Console.WriteLine("patch Message virtual=" + aggregate.Message);
        Console.WriteLine("patch Message virtual reads=" + AggregateCollectionProbe.Fields(value.Reads, "reads"));
        Console.WriteLine("patch Message base again=" + value.BaseMessage());
        Console.WriteLine("patch Message final reads=" + AggregateCollectionProbe.Fields(value.Reads, "reads"));
        return "interpreted aggregate Message dispatch end";
    }


    public static string RunOrdinaryMessageDispatch()
    {
        Console.WriteLine("== interpreted ordinary exception Message dispatch ==");
        var value = new MessageOrdinaryException("ordinary base");
        Console.WriteLine("ordinary Message base=" + value.BaseMessage());
        Console.WriteLine("ordinary Message base reads=" + AggregateCollectionProbe.Fields(value.Reads, "reads"));
        Exception exception = value;
        Console.WriteLine("ordinary Message virtual=" + exception.Message);
        Console.WriteLine("ordinary Message virtual reads=" + AggregateCollectionProbe.Fields(value.Reads, "reads"));
        Console.WriteLine("ordinary Message base again=" + value.BaseMessage());
        Console.WriteLine("ordinary Message final reads=" + AggregateCollectionProbe.Fields(value.Reads, "reads"));
        return "interpreted ordinary exception Message dispatch end";
    }

}

internal sealed class DirectCollectionAggregate : AggregateException
{
    internal int Code;
    internal string Tag;
    internal DirectCollectionAggregate(string message, Exception[] inner) : base(message, inner)
    {
        Code = 31;
        Tag = "direct";
    }
}

internal sealed class PatchCollectionAggregate : CollectionAggregate
{
    internal string Origin;
    internal PatchCollectionAggregate(string message, Exception[] inner) : base(message, inner)
    {
        Code = 37;
        Tag = "derived";
        Origin = "patch";
    }
}

internal sealed class MessageCollectionAggregate : CollectionAggregate
{
    internal int Reads;
    internal MessageCollectionAggregate(string message, Exception[] inner) : base(message, inner) { }
    public override string Message
    {
        get
        {
            Reads = Reads + 1;
            return "patch own";
        }
    }
    internal string BaseMessage() => base.Message;
}

internal sealed class MessageOrdinaryException : OrdinaryExceptionProbe
{
    internal int Reads;
    internal MessageOrdinaryException(string message) : base(message) { }
    public override string Message
    {
        get
        {
            Reads = Reads + 1;
            return "ordinary own";
        }
    }
    internal string BaseMessage() => base.Message;
}
