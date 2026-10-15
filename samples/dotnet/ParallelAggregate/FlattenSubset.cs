using System;

namespace ParallelAggregate;

internal static class FlattenSubset
{
    private sealed class DerivedAggregate : AggregateException
    {
        internal DerivedAggregate(Exception inner) : base("derived", inner) { }
        public override string Message => "override";
    }

    private static Exception[] Copy(AggregateException value)
    {
        var result = new Exception[value.InnerExceptions.Count];
        value.InnerExceptions.CopyTo(result, 0);
        return result;
    }

    internal static void Run()
    {
        Console.WriteLine("== Aggregate flatten ==");
        var a = new Exception("a");
        var b = new Exception("b");
        var c = new Exception("c");
        var d = new Exception("d");
        var nested = new AggregateException("branch", b, new AggregateException(c));
        var root = new AggregateException("root", nested, a, new AggregateException(), d, a);
        var flat = root.Flatten();
        Console.WriteLine("order=" + string.Join(",", Array.ConvertAll(Copy(flat), e => e.Message)));
        Console.WriteLine("identity=" + ReferenceEquals(flat.InnerExceptions[0], a) + ":" + ReferenceEquals(flat.InnerExceptions[2], a));
        Console.WriteLine("message=" + flat.Message);
        Console.WriteLine("fresh=" + !ReferenceEquals(flat, root) + ":" + !ReferenceEquals(flat, flat.Flatten()));
        Console.WriteLine("empty=" + new AggregateException("empty").Flatten().Message);
        Console.WriteLine("derived=" + new DerivedAggregate(b).Flatten().Message);
        AggregateException deep = new AggregateException(a);
        for (int i = 0; i < 20000; i++)
            deep = new AggregateException(deep);
        Console.WriteLine("deep=" + deep.Flatten().InnerExceptions.Count);
        Console.WriteLine("Aggregate flatten end");
    }
}
