#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Parallel.Invoke runs EVERY action, so unlike Parallel.For/ForEach the aggregated
// inner-exception count is deterministic — this is the multi-exception case.
//
// Every action captures `p` so it compiles to an instance (display-class) delegate: a
// params Action[] of cached static delegates confuses the array's element-type tracking.
namespace InvokeAggregate;

static class Program
{
    internal static void __GateEntry()
    {
        string p = "inv";

        try
        {
            Parallel.Invoke(
                () => throw new InvalidOperationException(p + "-delta"),
                () => throw new ArgumentException(p + "-alpha"),
                () => throw new Exception(p + "-charlie"),
                () => throw new InvalidOperationException(p + "-bravo"));
            Console.WriteLine("Invoke unreachable");
        }
        catch (AggregateException ae)
        {
            ParallelAggregate.AggDump.Dump("Invoke", ae);
        }

        // The aggregate is catchable as a base Exception, and `is` discriminates it.
        try
        {
            Parallel.Invoke(
                () => throw new InvalidOperationException(p + "-x"),
                () => throw new ArgumentException(p + "-y"));
            Console.WriteLine("base unreachable");
        }
        catch (Exception ex)
        {
            Console.WriteLine("base-catch isAgg=" + (ex is AggregateException));
            IReadOnlyList<Exception> inner = ((AggregateException)ex).InnerExceptions;
            Console.WriteLine("base-catch count=" + inner.Count);
        }

        // Even a lone throwing action is wrapped.
        try
        {
            Parallel.Invoke(() => throw new NotSupportedException(p + "-solo"));
            Console.WriteLine("solo unreachable");
        }
        catch (AggregateException ae)
        {
            ParallelAggregate.AggDump.Dump("InvokeSolo", ae);
        }
    }
}
