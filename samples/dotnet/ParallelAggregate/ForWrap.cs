#nullable enable
using System;
using System.Threading.Tasks;

// Parallel.For wraps body exceptions in an AggregateException: the int overload, an n == 1
// range, the long overload, and Exception.InnerException. Exactly ONE iteration throws per
// case, because real .NET stops scheduling at an unspecified point and so leaves the
// inner-exception count non-deterministic when several do. Each body captures `p` so the
// lambda is an instance delegate (see InvokeAggregate).
namespace ForWrap;

static class Program
{
    internal static void __GateEntry()
    {
        string p = "for";

        try
        {
            Parallel.For(0, 200, i => { if (i == 137) throw new InvalidOperationException(p + "-137"); });
            Console.WriteLine("For unreachable");
        }
        catch (AggregateException ae)
        {
            ParallelAggregate.AggDump.Dump("For", ae);
        }

        // A single-iteration range is still wrapped.
        try
        {
            Parallel.For(0, 1, i => throw new Exception(p + "-only"));
            Console.WriteLine("ForN1 unreachable");
        }
        catch (AggregateException ae)
        {
            ParallelAggregate.AggDump.Dump("ForN1", ae);
        }

        try
        {
            Parallel.For(0L, 64L, i => { if (i == 50L) throw new ArgumentException(p + "-long50"); });
            Console.WriteLine("ForLong unreachable");
        }
        catch (AggregateException ae)
        {
            ParallelAggregate.AggDump.Dump("ForLong", ae);
        }

        // InnerException on the aggregate is the first — here only — inner exception.
        try
        {
            Parallel.For(0, 50, i => { if (i == 10) throw new InvalidOperationException(p + "-innerProp"); });
            Console.WriteLine("Inner unreachable");
        }
        catch (AggregateException ae)
        {
            Exception? first = ae.InnerException;
            Console.WriteLine("InnerException=" + (first is null ? "<null>" : first.Message));
        }
    }
}
