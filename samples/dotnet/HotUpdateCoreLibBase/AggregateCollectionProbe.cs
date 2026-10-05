using System;

namespace HotUpdateCoreLibBase;

public delegate string AggregateCollectionReader();

public class CollectionAggregate : AggregateException
{
    public int Code;
    public string Tag;
    public CollectionAggregate(string message, Exception[] inner) : base(message, inner)
    {
        Code = 23;
        Tag = "AOT";
    }
}

public class OrdinaryExceptionProbe : Exception
{
    public OrdinaryExceptionProbe(string message) : base(message) { }
}

public static class AggregateCollectionProbe
{
    public static AggregateCollectionReader? PatchRead;
    public static AggregateCollectionReader? PatchMessageRead;
    public static AggregateCollectionReader? PatchOrdinaryMessageRead;

    public static string Inspect(AggregateException aggregate, Exception? first, Exception? second)
    {
        try
        {
            var inner = aggregate.InnerExceptions;
            int count = second is not null ? 2 : first is not null ? 1 : 0;
            return "count=" + inner.Count + "/cached=" + ReferenceEquals(inner, aggregate.InnerExceptions)
                + "/first=" + ReferenceEquals(aggregate.InnerException, first)
                + "/order=" + (inner.Count == count && (count == 0 || ReferenceEquals(inner[0], first))
                    && (count < 2 || ReferenceEquals(inner[1], second)))
                + "/message=" + aggregate.Message;
        }
        catch (Exception error)
        {
            return "collection threw=" + error.GetType().Name;
        }
    }

    public static string Fields(int code, string tag) => code + "/" + tag;

    public static void EmitSurface()
    {
        var seed = new Exception("seed");
        var values = new[] { seed };
        Inspect(new AggregateException("seed", values), seed, null);
        Exception aggregate = new CollectionAggregate("seed", values);
        _ = aggregate.Message;
        Exception ordinary = new OrdinaryExceptionProbe("seed");
        _ = ordinary.Message;
    }
}
