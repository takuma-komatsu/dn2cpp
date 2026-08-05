#nullable enable
using System;
using System.Collections.Generic;

// A directly-constructed AggregateException carries the same runtime layout as one from a
// Parallel fan-out: the Exception[] ctor, the params ctor, InnerException, and catching
// one as a base Exception. Construction preserves order, so sorting is only for symmetry
// with the parallel sections.
namespace ManualCtor;

static class Program
{
    internal static void __GateEntry()
    {
        Exception[] arr =
        {
            new InvalidOperationException("man-beta"),
            new ArgumentException("man-alpha"),
            new Exception("man-gamma"),
        };
        AggregateException ae = new AggregateException(arr);
        ParallelAggregate.AggDump.Dump("Manual", ae);
        Console.WriteLine("Manual inner0=" + (ae.InnerException is null ? "<null>" : ae.InnerException.Message));

        AggregateException ae2 = new AggregateException(
            new NotSupportedException("man-p1"),
            new ArgumentException("man-p2"));
        IReadOnlyList<Exception> inner2 = ae2.InnerExceptions;
        Console.WriteLine("ManualParams count=" + inner2.Count);

        try
        {
            throw new AggregateException(arr);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Manual thrown isAgg=" + (ex is AggregateException));
        }
    }
}
