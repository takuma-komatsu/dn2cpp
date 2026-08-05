#nullable enable
using System;
using System.Collections.Generic;

namespace ParallelAggregate;

// Shared dump helper for the AggregateException sections. Messages are SORTED, so the
// output is independent of the order in which parallel iterations throw. The upcast to
// IReadOnlyList is style only; the declared-type route is covered by the
// InnerExceptionsDeclaredType section.
static class AggDump
{
    internal static void Dump(string tag, AggregateException ae)
    {
        IReadOnlyList<Exception> inner = ae.InnerExceptions;
        Console.WriteLine(tag + " count=" + inner.Count);
        string[] msgs = new string[inner.Count];
        for (int i = 0; i < inner.Count; i++)
            msgs[i] = inner[i].Message;
        Array.Sort(msgs, StringComparer.Ordinal);
        foreach (string m in msgs)
            Console.WriteLine("  " + m);
    }
}
