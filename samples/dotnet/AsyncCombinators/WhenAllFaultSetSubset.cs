#nullable disable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WhenAllFaultSetSubset
{
    // Task.WhenAll aggregates EVERY faulted input, not the first one it finds: .NET's
    // join continuation AddRanges each faulted input's Exception.InnerExceptions, so a
    // nested join FLATTENS (three inners, not two) while a cancellation alongside a
    // fault contributes nothing. The three mouths that mint an AggregateException over
    // a task — Task.Exception, the blocking wait (Wait()/Result) and Task.WaitAll —
    // must agree on that set, and the awaiter mouth raises its first element unwrapped.
    // Every task here is pre-settled, so the order is the array's. Diffed exact vs .NET.
    internal static class Program
    {
        private static Task<int> FaultedT(string msg)
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetException(new InvalidOperationException(msg));
            return tcs.Task;
        }

        private static Task Faulted(string msg)
        {
            return FaultedT(msg);
        }

        private static Task Canceled()
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        private static string Messages(IReadOnlyList<Exception> inner)
        {
            string s = "";
            for (int i = 0; i < inner.Count; i++)
            {
                if (i > 0)
                {
                    s += ",";
                }
                s += inner[i].Message;
            }
            return s;
        }

        internal static void __GateEntry()
        {
            Task j = Task.WhenAll(new Task[] { Faulted("a"), Faulted("b") });
            IReadOnlyList<Exception> inner = j.Exception.InnerExceptions;
            Console.WriteLine("wafs-count: " + inner.Count);
            Console.WriteLine("wafs-messages: " + Messages(inner));
            Console.WriteLine("wafs-first: "
                + ReferenceEquals(j.Exception.InnerException, inner[0]));
            Console.WriteLine("wafs-agg-message: " + j.Exception.Message);

            Task jc = Task.WhenAll(new Task[] { Canceled(), Faulted("a"), Faulted("b") });
            Console.WriteLine("wafs-cancel-excluded: " + jc.Exception.InnerExceptions.Count
                + "," + Messages(jc.Exception.InnerExceptions));

            // The blocking wait mints its OWN aggregate, over the same set.
            try
            {
                j.Wait();
                Console.WriteLine("wafs-wait: no-throw");
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("wafs-wait: " + ae.InnerExceptions.Count
                    + "," + Messages(ae.InnerExceptions));
            }

            try
            {
                int[] r = Task.WhenAll(new Task<int>[] { FaultedT("a"), FaultedT("b") }).Result;
                Console.WriteLine("wafs-result: no-throw " + r.Length);
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("wafs-result: " + ae.InnerExceptions.Count
                    + "," + Messages(ae.InnerExceptions));
            }

            // The awaiter mouth raises the first inner, unwrapped.
            try
            {
                Task.WhenAll(new Task[] { Faulted("a"), Faulted("b") }).GetAwaiter().GetResult();
                Console.WriteLine("wafs-getresult: no-throw");
            }
            catch (Exception e)
            {
                Console.WriteLine("wafs-getresult: " + e.GetType().Name + "," + e.Message);
            }

            Task n = Task.WhenAll(new Task[]
            {
                Task.WhenAll(new Task[] { Faulted("a"), Faulted("b") }),
                Faulted("c"),
            });
            Console.WriteLine("wafs-nested: " + n.Exception.InnerExceptions.Count
                + "," + Messages(n.Exception.InnerExceptions));

            try
            {
                Task.WaitAll(new Task[] { Task.WhenAll(new Task[] { Faulted("a"), Faulted("b") }) });
                Console.WriteLine("wafs-waitall: no-throw");
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("wafs-waitall: " + ae.InnerExceptions.Count
                    + "," + Messages(ae.InnerExceptions));
            }
        }
    }
}
