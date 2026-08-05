#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Exceptions already thrown are held only by the loop's collector while the remaining
// actions allocate and force collections, so the collector must keep them GC-visible.
// Fixed-width message suffixes keep the sorted output independent of completion order.
namespace InvokePressure;

static class Program
{
    internal static void __GateEntry()
    {
        const int N = 192;
        string p = "prs";
        var actions = new Action[N];
        for (int i = 0; i < N; i++)
        {
            int k = i;
            if ((k & 1) == 1)
            {
                actions[i] = () => throw new InvalidOperationException(p + "-" + (1000 + k));
            }
            else
            {
                actions[i] = () =>
                {
                    long sink = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        int[] junk = new int[4096];
                        junk[k] = k + j + 1;
                        sink += junk[k];
                    }
                    if (sink <= 0)
                        Console.WriteLine("pressure sink unreachable");
                    if ((k & 31) == 0)
                        GC.Collect();
                };
            }
        }

        try
        {
            Parallel.Invoke(actions);
            Console.WriteLine("pressure unreachable");
        }
        catch (AggregateException ae)
        {
            IReadOnlyList<Exception> inner = ae.InnerExceptions;
            string[] msgs = new string[inner.Count];
            for (int i = 0; i < inner.Count; i++)
                msgs[i] = inner[i].Message;
            Array.Sort(msgs, StringComparer.Ordinal);
            int mismatches = 0;
            int at = 0;
            for (int i = 1; i < N; i += 2)
            {
                string want = p + "-" + (1000 + i);
                if (at >= msgs.Length || msgs[at] != want)
                    mismatches++;
                at++;
            }
            Console.WriteLine("pressure count=" + inner.Count);      // 96
            Console.WriteLine("pressure mismatches=" + mismatches);  // 0
        }
    }
}
