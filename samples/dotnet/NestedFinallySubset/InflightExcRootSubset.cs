#nullable enable
using System;

// An in-flight exception — thrown but not yet consumed by its handler — lives
// only in the C++ exception buffer while finally bodies and filters run. Each
// scenario collects and then storms allocations at such a point, so a missing
// GC root shows up as a corrupted message rather than a silent free.
namespace InflightExcRoot;

static class Program
{
    static object? s_occupy; // keeps the allocation storms reachable

    // Reference fields put it in the same allocation classes as exception
    // objects and message strings, so the storm actually recycles their blocks.
    sealed class Blob
    {
        public object? A;
        public object? B;
    }

    // Overwrites stale stack slots, so conservative scanning cannot keep a
    // collection candidate alive through leftover values.
    static long Stomp(int depth)
    {
        if (depth <= 0)
            return 1;
        long a = depth;
        long b = depth * 2;
        long c = depth * 3;
        long d = depth * 4;
        return a + b + c + d + Stomp(depth - 1);
    }

    static void Churn()
    {
        long scrub = Stomp(128);
        GC.Collect();
        for (int j = 0; j < 4096; j++)
        {
            var b = new Blob();
            b.A = s_occupy;
            b.B = scrub;
            s_occupy = b;
        }
    }

    static void ThrowDeep(string tag)
    {
        throw new InvalidOperationException("inflight-" + tag);
    }

    static string ThroughFinally()
    {
        try
        {
            try
            {
                ThrowDeep("fin");
            }
            finally
            {
                Churn();
            }
            return "unreachable";
        }
        catch (InvalidOperationException e)
        {
            return e.Message;
        }
    }

    // Unbound catch + rethrow: no C# local ever holds the exception, so the
    // buffer is its only anchor across the churn and the second finally.
    static string RethrowThroughFinally()
    {
        try
        {
            try
            {
                try
                {
                    ThrowDeep("re");
                }
                catch (InvalidOperationException)
                {
                    Churn();
                    throw;
                }
            }
            finally
            {
                Churn();
            }
            return "unreachable";
        }
        catch (InvalidOperationException e)
        {
            return e.Message;
        }
    }

    // The exception is in flight for the whole filter body.
    static bool Filter(Exception e)
    {
        Churn();
        return e.Message.Length > 0;
    }

    static string ThroughFilter()
    {
        try
        {
            ThrowDeep("flt");
            return "unreachable";
        }
        catch (InvalidOperationException e) when (Filter(e))
        {
            return e.Message;
        }
    }

    // A second exception is thrown and caught inside the finally while the
    // first is stashed; the first must still arrive at its catch intact.
    static string NestedInflight()
    {
        try
        {
            try
            {
                ThrowDeep("outer");
            }
            finally
            {
                try
                {
                    Churn();
                    ThrowDeep("inner");
                }
                catch (InvalidOperationException e2)
                {
                    if (e2.Message != "inflight-inner")
                        Console.WriteLine("inner mismatch: " + e2.Message);
                    Churn();
                }
            }
            return "unreachable";
        }
        catch (InvalidOperationException e)
        {
            return e.Message;
        }
    }

    internal static void __GateEntry()
    {
        Console.WriteLine(ThroughFinally());        // inflight-fin
        Console.WriteLine(RethrowThroughFinally()); // inflight-re
        Console.WriteLine(ThroughFilter());         // inflight-flt
        Console.WriteLine(NestedInflight());        // inflight-outer
    }
}
