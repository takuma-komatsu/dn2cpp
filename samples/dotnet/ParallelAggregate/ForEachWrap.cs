#nullable enable
using System;
using System.Threading.Tasks;

// Parallel.ForEach wraps body exceptions in an AggregateException, over a
// reference-element and a primitive-element source. Exactly ONE element throws per case,
// because real .NET's inner-exception count is non-deterministic when several do. Each
// body captures `p` so the lambda is an instance delegate; the source must be a concrete
// array, the only Parallel.ForEach source shape modeled.
namespace ForEachWrap;

static class Program
{
    internal static void __GateEntry()
    {
        string p = "fe";

        string[] words = new string[100];
        for (int i = 0; i < 100; i++)
            words[i] = "item" + i;
        try
        {
            Parallel.ForEach(words, s => { if (s == "item42") throw new ArgumentException(p + "-" + s); });
            Console.WriteLine("ForEachRef unreachable");
        }
        catch (AggregateException ae)
        {
            ParallelAggregate.AggDump.Dump("ForEachRef", ae);
        }

        int[] nums = new int[100];
        for (int i = 0; i < 100; i++)
            nums[i] = i;
        try
        {
            Parallel.ForEach(nums, n => { if (n == 73) throw new InvalidOperationException(p + "-n73"); });
            Console.WriteLine("ForEachInt unreachable");
        }
        catch (AggregateException ae)
        {
            ParallelAggregate.AggDump.Dump("ForEachInt", ae);
        }
    }
}
