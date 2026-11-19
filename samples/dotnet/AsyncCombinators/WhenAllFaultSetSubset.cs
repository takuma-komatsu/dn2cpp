#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
        internal static void RunAddedExceptions()
        {
            var add = typeof(Task).GetMethod("AddException",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null, new[] { typeof(object) }, null);
            void Add(Task task, object payload) => add.Invoke(task, new[] { payload });

            var first = new ArgumentException("stored first");
            var second = new InvalidOperationException("stored second");
            var third = new Exception("stored third");
            var fourth = new Exception("stored fourth");
            int bodies = 0, continuations = 0;
            var cold = new Task<int>(() => { bodies++; return 42; });
            var continuation = cold.ContinueWith(_ => Interlocked.Increment(ref continuations),
                TaskContinuationOptions.ExecuteSynchronously);
            Add(cold, first);
            Add(cold, ExceptionDispatchInfo.Capture(second));
            Add(cold, new List<Exception> { third });
            Add(cold, new List<ExceptionDispatchInfo> { ExceptionDispatchInfo.Capture(fourth) });
            Console.WriteLine("task add pending: " + cold.Status + "/" + (cold.Exception is null)
                + "/" + cold.IsCompleted + "/" + continuations);
            cold.RunSynchronously();
            if (!continuation.Wait(TimeSpan.FromSeconds(8)))
                throw new TimeoutException("faulted task continuation did not complete");

            bool Matches(AggregateException error) => error?.InnerExceptions.Count == 4
                && ReferenceEquals(error.InnerExceptions[0], first) && ReferenceEquals(error.InnerExceptions[1], second)
                && ReferenceEquals(error.InnerExceptions[2], third) && ReferenceEquals(error.InnerExceptions[3], fourth);
            Exception awaited = null;
            try { cold.GetAwaiter().GetResult(); }
            catch (Exception error) { awaited = error; }
            Console.WriteLine("task add finish: " + cold.Status + "/" + bodies + "/" + continuations
                + "/" + Matches(cold.Exception) + "/" + Matches(Task.WhenAll(cold, Task.CompletedTask).Exception)
                + "/" + ReferenceEquals(awaited, first));

            var promise = new TaskCompletionSource<int>();
            Add(promise.Task, first);
            Console.WriteLine("task add promise: " + promise.Task.Status + "/" + promise.TrySetResult(73)
                + "/" + promise.Task.Status + "/" + promise.Task.Result + "/" + (promise.Task.Exception is null));

            var source = new TaskCompletionSource<int>();
            var joined = Task.WhenAll(source.Task);
            Add(joined, first);
            source.SetException(second);
            Exception joinedAwaited = null;
            try { joined.GetAwaiter().GetResult(); }
            catch (Exception error) { joinedAwaited = error; }
            Console.WriteLine("task add join: " + ReferenceEquals(joinedAwaited, first)
                + "/" + (joined.Exception.InnerExceptions.Count == 2)
                + "/" + ReferenceEquals(joined.Exception.InnerExceptions[0], first)
                + "/" + ReferenceEquals(joined.Exception.InnerExceptions[1], second));

            var proxy = Task.Run<int>(() => cold);
            try
            {
                if (!proxy.Wait(TimeSpan.FromSeconds(8)))
                    throw new TimeoutException("faulted task proxy did not complete");
            }
            catch (AggregateException) { }
            Exception proxyAwaited = null;
            try { proxy.GetAwaiter().GetResult(); }
            catch (Exception error) { proxyAwaited = error; }
            Console.WriteLine("task add proxy: " + Matches(proxy.Exception)
                + "/" + ReferenceEquals(proxyAwaited, first));
        }

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
