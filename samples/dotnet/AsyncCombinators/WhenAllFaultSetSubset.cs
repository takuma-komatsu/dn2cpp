#nullable disable
using System;
using System.Collections.Generic;
using System.Threading;
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
        private sealed class MessageFault : Exception
        {
            internal int Reads;
            internal readonly string Label;
            internal readonly bool Throws;
            internal MessageFault(string label, bool throws = true)
            {
                Label = label;
                Throws = throws;
            }
            public override string Message
            {
                get
                {
                    Reads++;
                    if (Throws)
                        throw new InvalidOperationException("message getter");
                    return Label + ":" + Reads;
                }
            }
        }

        private static MessageFault MessageError(string label, bool throws = true)
        {
            var error = new MessageFault(label, throws);
            GC.KeepAlive(new Func<string>(() => error.Message));
            return error;
        }

        private static void ObserveMessage(string label, Func<Exception> action,
            MessageFault first, MessageFault second = null)
        {
            Exception error;
            try { error = action(); }
            catch (Exception caught) { error = caught; }
            Console.WriteLine(label + " result=" + error.GetType().Name
                + " reads=" + first.Reads + "/" + (second is null ? 0 : second.Reads));
            if (error is not AggregateException aggregate)
            {
                Console.WriteLine(label + " direct identity=" + ReferenceEquals(error, first));
                return;
            }
            Console.WriteLine(label + " first=" + ReferenceEquals(aggregate.InnerException, first)
                + " count=" + aggregate.InnerExceptions.Count);
            for (int i = 0; i < 2; i++)
            {
                try { Console.WriteLine(label + " Message=" + aggregate.Message); }
                catch (Exception getter) { Console.WriteLine(label + " getter=" + getter.GetType().Name + ":" + getter.Message); }
                Console.WriteLine(label + " reads=" + first.Reads + "/" + (second is null ? 0 : second.Reads));
            }
        }

        internal static void RunAggregateMessages()
        {
            Console.WriteLine("== lazy task and cancellation Message ==");
            foreach (string mode in new[] { "Exception", "Wait", "Result", "awaiter" })
            {
                var fault = MessageError(mode);
                var task = Task.FromException<int>(fault);
                ObserveMessage("task " + mode, () =>
                {
                    if (mode == "Exception") return task.Exception;
                    if (mode == "Wait") task.Wait();
                    else if (mode == "Result") _ = task.Result;
                    else task.GetAwaiter().GetResult();
                    return null;
                }, fault);
            }
            foreach (string mode in new[] { "default", "false", "true" })
            {
                using var source = new CancellationTokenSource();
                var first = MessageError("first");
                var last = MessageError("last");
                string calls = "";
                source.Token.Register(() => { calls += "first/"; throw first; });
                source.Token.Register(() => { calls += "last/"; throw last; });
                ObserveMessage("cancel " + mode, () =>
                {
                    if (mode == "default") source.Cancel();
                    else source.Cancel(mode == "true");
                    return null;
                }, last, first);
                Console.WriteLine("cancel " + mode + " calls=" + calls + " canceled=" + source.IsCancellationRequested);
            }
            foreach (bool firstOnly in new[] { false, true })
            {
                using var parent = new CancellationTokenSource();
                using var child = CancellationTokenSource.CreateLinkedTokenSource(parent.Token);
                var fault = MessageError("linked", false);
                child.Token.Register(() => throw fault);
                AggregateException aggregate;
                try { parent.Cancel(firstOnly); throw new Exception("not canceled"); }
                catch (AggregateException caught) { aggregate = caught; }
                var childError = firstOnly ? aggregate : (AggregateException)aggregate.InnerException;
                Console.WriteLine("linked " + firstOnly + " constructed reads=" + fault.Reads
                    + " identity=" + ReferenceEquals(childError.InnerException, fault));
                Console.WriteLine("linked " + firstOnly + " Message=" + aggregate.Message + " reads=" + fault.Reads);
                Console.WriteLine("linked " + firstOnly + " Message=" + aggregate.Message + " reads=" + fault.Reads);
            }
            Console.WriteLine("lazy task and cancellation Message end");
        }

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
