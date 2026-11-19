#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SettledCombinatorsSubset
{
    // Task.WhenAll / Task.WhenAny over inputs that are ALREADY settled: the join must be
    // complete when the combinator RETURNS, not one turn of the scheduler later. .NET
    // registers each of its continuations ExecuteSynchronously, so a settled input is
    // consumed by the registration itself.
    //
    // Every line here is READ without waiting first, and that is the whole point: a
    // `.Wait()` ahead of the read passes whether the join finished inline or was posted,
    // so it cannot tell the two apart. The mixed rows are the other half — a single
    // pending input must still leave the join PENDING, so an over-eager inline path is
    // caught too. Diffed exactly against real .NET.
    internal static class Program
    {
        private static Task Faulted()
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetException(new InvalidOperationException("x"));
            return tcs.Task;
        }

        private static Task Canceled()
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        private static void WhenAll()
        {
            Task done = Task.CompletedTask;
            Task done2 = Task.FromResult(7);
            Task<int> doneT = Task.FromResult(1);
            Task<int> doneT2 = Task.FromResult(2);
            Task stuck = new TaskCompletionSource<int>().Task;
            Task<int> stuckT = new TaskCompletionSource<int>().Task;

            Console.WriteLine("wa-two-done: " + Task.WhenAll(new Task[] { done, done2 }).IsCompleted);
            Console.WriteLine("wa-one-done: " + Task.WhenAll(new Task[] { done }).IsCompleted);
            Console.WriteLine("wa-empty: " + Task.WhenAll(new Task[0]).IsCompleted);
            Console.WriteLine("wa-mixed: " + Task.WhenAll(new Task[] { done, stuck }).IsCompleted);
            Console.WriteLine("wa-mixed-rev: " + Task.WhenAll(new Task[] { stuck, done }).IsCompleted);

            Task<int[]> tj = Task.WhenAll(new Task<int>[] { doneT, doneT2 });
            Console.WriteLine("wa-T-two-done: " + tj.IsCompleted + "," + tj.Result[0] + "," + tj.Result[1]);
            Console.WriteLine("wa-T-empty: " + Task.WhenAll(new Task<int>[0]).IsCompleted);
            Console.WriteLine("wa-T-mixed: " + Task.WhenAll(new Task<int>[] { doneT, stuckT }).IsCompleted);

            Console.WriteLine("wa-enum: " + Task.WhenAll((IEnumerable<Task>)new List<Task> { done, done2 }).IsCompleted);
            Console.WriteLine("wa-T-enum: " + Task.WhenAll((IEnumerable<Task<int>>)new List<Task<int>> { doneT, doneT2 }).IsCompleted);

            // A settled input that did not succeed settles the join the same way, and the
            // outcome it carries is the input's, not "completed".
            Task f = Task.WhenAll(new Task[] { done, Faulted() });
            Console.WriteLine("wa-faulted: " + f.IsCompleted + "," + f.IsFaulted + ","
                + f.Exception.InnerException.GetType().Name);
            Task c = Task.WhenAll(new Task[] { done, Canceled() });
            Console.WriteLine("wa-canceled: " + c.IsCompleted + "," + c.IsCanceled + "," + c.IsFaulted
                + "," + (c.Exception is null));
            // A fault anywhere outranks a cancellation anywhere, in either order.
            Console.WriteLine("wa-cancel-then-fault: "
                + Task.WhenAll(new Task[] { Canceled(), Faulted() }).IsFaulted);
            Console.WriteLine("wa-fault-then-cancel: "
                + Task.WhenAll(new Task[] { Faulted(), Canceled() }).IsFaulted);
            // The canceled join re-raises like a canceled task, wrapped by the blocking
            // wait and bare through the awaiter.
            try { Task.WhenAll(new Task[] { done, Canceled() }).Wait(); Console.WriteLine("wa-canceled-wait: no-throw"); }
            catch (AggregateException ae) { Console.WriteLine("wa-canceled-wait: AggregateException|" + ae.InnerException.GetType().Name); }
            try { Task.WhenAll(new Task[] { done, Canceled() }).GetAwaiter().GetResult(); Console.WriteLine("wa-canceled-getresult: no-throw"); }
            catch (Exception ex) { Console.WriteLine("wa-canceled-getresult: " + ex.GetType().Name); }

            // An inline join is itself a settled input to the next combinator.
            Console.WriteLine("wa-of-wa: "
                + Task.WhenAll(new Task[] { Task.WhenAll(new Task[] { done, done2 }) }).IsCompleted);
        }

        private static void WhenAny()
        {
            Task done = Task.CompletedTask;
            Task done2 = Task.FromResult(7);
            Task<int> doneT = Task.FromResult(1);
            Task<int> doneT2 = Task.FromResult(2);
            Task stuck = new TaskCompletionSource<int>().Task;
            Task<int> stuckT = new TaskCompletionSource<int>().Task;

            // The winner is the first settled input in ARRAY order, which is only
            // deterministic because nothing but the registration decides it.
            Task<Task> a = Task.WhenAny(new Task[] { done, done2 });
            Console.WriteLine("wy-two-done: " + a.IsCompleted + "," + (a.Result == done));
            Task<Task> b = Task.WhenAny(new Task[] { stuck, done });
            Console.WriteLine("wy-stuck-then-done: " + b.IsCompleted + "," + (b.Result == done));
            Console.WriteLine("wy-done-then-stuck: " + Task.WhenAny(new Task[] { done, stuck }).IsCompleted);
            Console.WriteLine("wy-two-stuck: " + Task.WhenAny(new Task[] { stuck, stuckT }).IsCompleted);

            Task<Task<int>> t = Task.WhenAny(new Task<int>[] { doneT, doneT2 });
            Console.WriteLine("wy-T-two-done: " + t.IsCompleted + "," + t.Result.Result);
            Console.WriteLine("wy-enum: "
                + Task.WhenAny((IEnumerable<Task>)new List<Task> { done, done2 }).IsCompleted);

            // WhenAny never faults: a faulted winner leaves the join SUCCEEDED and the
            // fault observable only through the winner itself.
            Task<Task> g = Task.WhenAny(new Task[] { Faulted() });
            Console.WriteLine("wy-faulted: " + g.IsCompleted + "," + g.IsFaulted + "," + g.Result.IsFaulted);

            Console.WriteLine("wy-of-wa: "
                + Task.WhenAny(new Task[] { Task.WhenAll(new Task[] { done, done2 }) }).IsCompleted);
        }

        internal static void __GateEntry()
        {
            WhenAll();
            WhenAny();
        }

        private enum PendingCode : ushort { Low = 1, High = 65535 }

        private struct PendingPair
        {
            internal int Number;
            internal string Text;
            public override string ToString() => Number + "/" + Text;
        }

        private static void PendingResults<T>(string label, T first, T second)
        {
            var a = new TaskCompletionSource<T>();
            var b = new TaskCompletionSource<T>();
            var all = Task.WhenAll(new Task<T>[] { a.Task, b.Task });
            bool before = all.IsCompleted;
            a.SetResult(first);
            bool partial = all.IsCompleted;
            b.SetResult(second);
            bool final = all.IsCompleted;
            T[] values = all.Result;
            Console.WriteLine("pending " + label + ": " + before + "," + partial + "," + final
                + "|" + values.Length + "|" + values[0] + "," + values[1]);
        }

        private static void PendingNonGeneric()
        {
            var a = new TaskCompletionSource<int>();
            var b = new TaskCompletionSource<int>();
            var all = Task.WhenAll((IEnumerable<Task>)new List<Task>
                { a.Task, Task.CompletedTask, b.Task, a.Task });
            var any = Task.WhenAny((IEnumerable<Task>)new List<Task> { a.Task, b.Task });
            Console.WriteLine("pending nongeneric before: " + all.IsCompleted + "," + any.IsCompleted);
            a.SetResult(1);
            Console.WriteLine("pending nongeneric partial: " + all.IsCompleted + "," + any.IsCompleted);
            Task winner = any.Result;
            b.SetResult(2);
            Console.WriteLine("pending nongeneric final: " + all.IsCompleted + "," + any.IsCompleted
                + "|" + ReferenceEquals(winner, a.Task) + "," + ReferenceEquals(any.Result, winner));
        }

        private static void PendingOutcomes()
        {
            var fault = new TaskCompletionSource<int>();
            var cancel = new TaskCompletionSource<int>();
            var all = Task.WhenAll(fault.Task, cancel.Task);
            var any = Task.WhenAny(fault.Task, cancel.Task);
            fault.SetException(new InvalidOperationException("pending fault"));
            Console.WriteLine("pending fault partial: " + all.IsCompleted + "," + any.IsCompleted
                + "|" + any.IsFaulted + "," + ReferenceEquals(any.Result, fault.Task));
            cancel.SetCanceled();
            Console.WriteLine("pending fault final: " + all.IsCompleted + "," + all.IsFaulted
                + "," + all.IsCanceled + "|" + all.Exception.InnerExceptions.Count
                + "," + all.Exception.InnerException.Message);

            var success = new TaskCompletionSource<int>();
            var canceled = new TaskCompletionSource<int>();
            var canceledAll = Task.WhenAll(success.Task, canceled.Task);
            var canceledAny = Task.WhenAny(canceled.Task, success.Task);
            canceled.SetCanceled();
            Console.WriteLine("pending cancel partial: " + canceledAll.IsCompleted + "," + canceledAny.IsCompleted
                + "|" + canceledAny.IsCanceled + "," + canceledAny.Result.IsCanceled);
            success.SetResult(3);
            Console.WriteLine("pending cancel final: " + canceledAll.IsCompleted + "," + canceledAll.IsCanceled
                + "," + canceledAll.IsFaulted + "|" + (canceledAll.Exception is null));
        }

        private static void PendingNested()
        {
            var source = new TaskCompletionSource<int>();
            Task chain = source.Task;
            for (int i = 0; i < 1024; i++)
                chain = Task.WhenAll(new Task[] { chain, Task.CompletedTask });
            var any = Task.WhenAny(new Task[] { chain, new TaskCompletionSource<int>().Task });
            var outer = Task.WhenAll(new Task[] { chain, any });
            source.SetResult(17);
            Console.WriteLine("pending nested: " + chain.IsCompleted + "," + any.IsCompleted
                + "," + outer.IsCompleted + "|" + ReferenceEquals(any.Result, chain));
        }

        private static void PendingWorker()
        {
            var source = new TaskCompletionSource<int>();
            var all = Task.WhenAll(source.Task, Task.FromResult(5));
            var any = Task.WhenAny(source.Task, new TaskCompletionSource<int>().Task);
            bool allAtReturn = false;
            bool anyAtReturn = false;
            var worker = new Thread(() =>
            {
                source.SetResult(4);
                allAtReturn = all.IsCompleted;
                anyAtReturn = any.IsCompleted;
            });
            worker.Start();
            worker.Join();
            Console.WriteLine("pending worker: " + allAtReturn + "," + anyAtReturn
                + "|" + all.Result[0] + "," + all.Result[1] + "," + ReferenceEquals(any.Result, source.Task));
        }

        private static void PendingRaces()
        {
            int allCompleted = 0;
            int anyCompleted = 0;
            int validResults = 0;
            for (int i = 0; i < 32; i++)
            {
                var a = new TaskCompletionSource<int>();
                var b = new TaskCompletionSource<int>();
                using var start = new ManualResetEventSlim(false);
                var first = new Thread(() => { start.Wait(); a.SetResult(7); });
                var second = new Thread(() => { start.Wait(); b.SetResult(9); });
                first.Start();
                second.Start();
                if ((i & 1) != 0)
                    start.Set();
                // Alternate concurrent pending callbacks with registration/completion races.
                var all = Task.WhenAll(a.Task, b.Task);
                var any = Task.WhenAny(a.Task, b.Task);
                if ((i & 1) == 0)
                    start.Set();
                first.Join();
                second.Join();
                if (all.IsCompleted)
                    allCompleted++;
                if (any.IsCompleted)
                    anyCompleted++;
                int[] values = all.Result;
                Task<int> winner = any.Result;
                if (values[0] == 7 && values[1] == 9
                    && (ReferenceEquals(winner, a.Task) || ReferenceEquals(winner, b.Task))
                    && ReferenceEquals(any.Result, winner))
                    validResults++;
            }
            Console.WriteLine("pending races: " + allCompleted + "," + anyCompleted + "," + validResults);
        }

        private static void OrdinaryQueuedAwait()
        {
            var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callback = new TaskCompletionSource<bool>();
            int producer = Thread.CurrentThread.ManagedThreadId;
            bool setting = false;
            source.Task.GetAwaiter().OnCompleted(() => callback.SetResult(
                Thread.CurrentThread.ManagedThreadId == producer && setting));
            setting = true;
            source.SetResult(1);
            setting = false;
            Console.WriteLine("ordinary await inline: " + callback.Task.Result);
        }

        internal static void RunPromises()
        {
            Console.WriteLine("== derived task promises ==");
            Type nodeType = Type.GetType("System.Threading.SemaphoreSlim+TaskNode, System.Private.CoreLib", throwOnError: true);
            var constructor = nodeType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            var node = (Task<bool>)constructor.Invoke(null);
            var sibling = (Task<bool>)constructor.Invoke(null);
            var previous = nodeType.GetField("Prev", BindingFlags.Instance | BindingFlags.NonPublic);
            var next = nodeType.GetField("Next", BindingFlags.Instance | BindingFlags.NonPublic);
            previous.SetValue(node, sibling);
            next.SetValue(node, node);
            Console.WriteLine("promise pending: " + node.Status + "," + node.IsCompleted + "," + node.IsCompletedSuccessfully);
            Console.WriteLine("promise identity: " + (node.GetType() == nodeType) + "," + (node is Task)
                + "," + (node.Id > 0 && sibling.Id > 0 && node.Id != sibling.Id));
            Console.WriteLine("promise state: " + (node.AsyncState is null));
            var complete = typeof(Task<bool>).GetMethod("TrySetResult", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var callback = new TaskCompletionSource<bool>();
            int producer = Thread.CurrentThread.ManagedThreadId;
            bool setting = false;
            node.GetAwaiter().OnCompleted(() => callback.SetResult(
                Thread.CurrentThread.ManagedThreadId == producer && setting));
            setting = true;
            bool first = (bool)complete.Invoke(node, new object[] { true });
            setting = false;
            bool second = (bool)complete.Invoke(node, new object[] { false });
            Console.WriteLine("promise completion: " + first + "," + second + "," + node.GetAwaiter().GetResult()
                + "," + node.Status + "," + node.IsCompletedSuccessfully);
            Console.WriteLine("promise fields: " + ReferenceEquals(previous.GetValue(node), sibling)
                + "," + ReferenceEquals(next.GetValue(node), node));
            Console.WriteLine("promise await inline: " + callback.Task.Result);
            Console.WriteLine("derived task promises end");
        }

        internal static void RunProtectedStart()
        {
            Console.WriteLine("== protected task start ==");
            var method = typeof(Task).GetMethod("MarkStarted",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            bool Mark(Task task) => (bool)method.Invoke(task, null);

            int calls = 0;
            var cold = new Task(() => calls++);
            bool first = Mark(cold);
            cold.Start(); // Claims a scheduler, not a second start.
            Console.WriteLine("cold claim: " + first + "/" + Mark(cold) + "/" + cold.Status + "/" + calls);

            var source = new TaskCompletionSource<int>();
            var completed = Task.FromResult(987);
            var faulted = Task.FromException(new InvalidOperationException());
            var canceled = Task.FromCanceled(new CancellationToken(true));
            Console.WriteLine("promise claim: " + Mark(source.Task) + "/" + source.Task.Status);
            source.SetResult(9);
            Console.WriteLine("settled claims: " + Mark(source.Task) + "/" + Mark(completed) + "/"
                + Mark(completed) + "/" + Mark(faulted) + "/" + Mark(canceled));
            _ = faulted.Exception;

            Task sync = null;
            sync = new Task(() => Console.WriteLine("inline delegate: " + sync.Status + "/" + Mark(sync)));
            sync.RunSynchronously();
            var antecedent = new TaskCompletionSource<int>();
            Task<int> continuation = null;
            continuation = antecedent.Task.ContinueWith(_ => (int)continuation.Status);
            antecedent.SetResult(1);
            Console.WriteLine("continuation claim: " + Mark(continuation) + "/" + (TaskStatus)continuation.Result);
            using var tokenSource = new CancellationTokenSource();
            var filteredAntecedent = new TaskCompletionSource<int>();
            Task Filtered(CancellationToken token) => filteredAntecedent.Task.ContinueWith(
                _ => calls++, token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            var filteredNone = Filtered(CancellationToken.None);
            var filteredToken = Filtered(tokenSource.Token);
            bool markedNone = Mark(filteredNone);
            bool markedToken = Mark(filteredToken);
            filteredAntecedent.SetResult(1);
            Console.WriteLine("filtered claims: " + markedNone + "/" + markedToken + "/"
                + filteredNone.Status + "/" + filteredToken.Status + "/" + calls);
            Console.WriteLine("protected task start end");
        }

        internal static void RunSchedulerInline()
        {
            Console.WriteLine("== scheduler inline ==");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var inline = typeof(TaskScheduler).GetMethod("TryRunInline", flags);
            var mark = typeof(Task).GetMethod("MarkStarted", flags);
            var scheduler = TaskScheduler.Default;
            bool Inline(Task task, bool queued) => (bool)inline.Invoke(scheduler, new object[] { task, queued });

            int calls = 0;
            var cold = new Task(() => calls++);
            bool marked = (bool)mark.Invoke(cold, null);
            bool noOwner = Inline(cold, false);
            // Start assigns the scheduler but cannot queue a task whose start flag was already set.
            cold.Start(scheduler);
            bool missingQueue = Inline(cold, true);
            bool ran = Inline(cold, false);
            bool repeated = Inline(cold, false);
            Console.WriteLine("scheduler inline: " + marked + "/" + noOwner + "/" + missingQueue + "/"
                + ran + "/" + repeated + "/" + cold.Status + "/" + calls);

            var promise = new TaskCompletionSource<int>();
            var canceled = Task.FromCanceled(new CancellationToken(true));
            var pending = promise.Task.ContinueWith(_ => { calls++; }, scheduler);
            var completed = Task.FromResult(1999);
            bool markedPending = (bool)mark.Invoke(pending, null);
            bool markedCompleted = (bool)mark.Invoke(completed, null);
            Console.WriteLine("scheduler inline ineligible: " + Inline(promise.Task, false)
                + "/" + Inline(canceled, true) + "|" + markedPending + "/" + Inline(pending, false)
                + "/" + Inline(pending, true) + "|" + markedCompleted + "/" + Inline(completed, false)
                + "/" + Inline(completed, true));

            string CancellationResult(bool matching)
            {
                using var associated = new CancellationTokenSource();
                using var unrelated = new CancellationTokenSource();
                var canceledBy = matching ? associated : unrelated;
                var task = new Task(() =>
                {
                    associated.Cancel();
                    if (!matching)
                        unrelated.Cancel();
                    canceledBy.Token.ThrowIfCancellationRequested();
                }, associated.Token);
                mark.Invoke(task, null);
                task.Start(scheduler);
                return Inline(task, false) + "/" + task.Status;
            }

            Console.WriteLine("scheduler inline cancellation: " + CancellationResult(true)
                + "|" + CancellationResult(false));
        }

        internal static void RunSchedulerQueue()
        {
            var queue = typeof(TaskScheduler).GetMethod("InternalQueueTask",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var mark = typeof(Task).GetMethod("MarkStarted",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            int caller = Environment.CurrentManagedThreadId;
            int calls = 0;
            bool offThread = false;
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var cold = new Task<int>(() =>
            {
                Interlocked.Increment(ref calls);
                offThread = Environment.CurrentManagedThreadId != caller;
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(8)))
                    throw new TimeoutException("queued task was not released");
                return 42;
            });
            queue.Invoke(TaskScheduler.Default, new object[] { cold });
            bool acknowledged = entered.Wait(TimeSpan.FromSeconds(8));
            bool first = acknowledged && (bool)mark.Invoke(cold, null);
            bool second = acknowledged && (bool)mark.Invoke(cold, null);
            TaskStatus running = cold.Status;
            release.Set();
            if (!acknowledged || !cold.Wait(TimeSpan.FromSeconds(8)))
                throw new TimeoutException("queued task did not complete");
            Console.WriteLine("scheduler queue: " + offThread + "/" + first + "/" + second
                + "/" + running + "/" + cold.Result + "/" + calls);
        }

        internal static void RunSemaphoreWaiters()
        {
            Console.WriteLine("== semaphore waiter helpers ==");
            var add = typeof(SemaphoreSlim).GetMethod("CreateAndAddAsyncWaiter", BindingFlags.Instance | BindingFlags.NonPublic);
            var remove = typeof(SemaphoreSlim).GetMethod("RemoveAsyncWaiter", BindingFlags.Instance | BindingFlags.NonPublic);
            Type nodeType = Type.GetType("System.Threading.SemaphoreSlim+TaskNode, System.Private.CoreLib", true);
            var previous = nodeType.GetField("Prev", BindingFlags.Instance | BindingFlags.NonPublic);
            var next = nodeType.GetField("Next", BindingFlags.Instance | BindingFlags.NonPublic);
            var semaphore = new SemaphoreSlim(0);
            // Private helpers require exclusion from every operation on this instance.
            // All helper calls and releases below share this test lock.
            object gate = new object();
            lock (gate)
            {
                var head = (Task<bool>)add.Invoke(semaphore, null);
                var middle = (Task<bool>)add.Invoke(semaphore, null);
                var survivor = (Task<bool>)add.Invoke(semaphore, null);
                var tail = (Task<bool>)add.Invoke(semaphore, null);
                bool linked = ReferenceEquals(next.GetValue(head), middle)
                    && ReferenceEquals(previous.GetValue(tail), survivor);
                bool removed = (bool)remove.Invoke(semaphore, new object[] { middle })
                    && (bool)remove.Invoke(semaphore, new object[] { tail })
                    && (bool)remove.Invoke(semaphore, new object[] { head });
                bool repeated = (bool)remove.Invoke(semaphore, new object[] { head });
                Console.WriteLine("queue remove: " + linked + "/" + removed + "/" + repeated);
                Console.WriteLine("queue links cleared: " + (previous.GetValue(survivor) is null)
                    + "/" + (next.GetValue(survivor) is null) + "/" + (previous.GetValue(middle) is null)
                    + "/" + (next.GetValue(middle) is null));
                int before = semaphore.Release();
                Console.WriteLine("queue release: " + before + "/" + survivor.Result + "/" + semaphore.CurrentCount
                    + "/" + !head.IsCompleted + "/" + !middle.IsCompleted + "/" + !tail.IsCompleted);
                Console.WriteLine("released removal: " + remove.Invoke(semaphore, new object[] { survivor }));
                var first = (Task<bool>)add.Invoke(semaphore, null);
                var second = (Task<bool>)add.Invoke(semaphore, null);
                semaphore.Release();
                bool firstOnly = first.IsCompletedSuccessfully && first.Result
                    && !second.IsCompleted && semaphore.CurrentCount == 0;
                semaphore.Release();
                Console.WriteLine("queue FIFO: " + firstOnly + "/"
                    + (second.IsCompletedSuccessfully && second.Result && semaphore.CurrentCount == 0));
                try { remove.Invoke(semaphore, new object[] { null }); }
                catch (TargetInvocationException e) { Console.WriteLine("null waiter: " + e.InnerException.GetType().Name); }
            }
            Task<bool> expired;
            lock (gate) expired = (Task<bool>)add.Invoke(semaphore, null);
            try { expired.WaitAsync(TimeSpan.FromMilliseconds(1)).GetAwaiter().GetResult(); }
            catch (TimeoutException) { }
            lock (gate)
                Console.WriteLine("timeout helper removal: " + remove.Invoke(semaphore, new object[] { expired }));
            Task<bool> canceled;
            lock (gate) canceled = (Task<bool>)add.Invoke(semaphore, null);
            var source = new CancellationTokenSource();
            Task bounded = canceled.WaitAsync(Timeout.InfiniteTimeSpan, source.Token);
            source.Cancel();
            lock (gate)
                Console.WriteLine("cancel helper removal: " + bounded.IsCanceled + "/"
                    + remove.Invoke(semaphore, new object[] { canceled }));

            bool races = true;
            for (int i = 0; i < 8; i++)
            {
                Task<bool> waiter;
                lock (gate) waiter = (Task<bool>)add.Invoke(semaphore, null);
                bool removed = false;
                var remover = new Thread(() => { lock (gate) removed = (bool)remove.Invoke(semaphore, new object[] { waiter }); });
                var releaser = new Thread(() => { lock (gate) semaphore.Release(); });
                remover.Start();
                releaser.Start();
                remover.Join();
                releaser.Join();
                races &= removed ? !waiter.IsCompleted && semaphore.CurrentCount == 1
                    : waiter.IsCompleted && waiter.Result && semaphore.CurrentCount == 0;
                if (removed) semaphore.Wait(0);
            }
            Console.WriteLine("remove/release linearization: " + races);
            WeakReference queued = null;
            var abandoned = new WeakReference[4];
            var producer = new Thread(() =>
            {
                lock (gate) queued = new WeakReference(add.Invoke(semaphore, null));
                for (int i = 0; i < abandoned.Length; i++)
                {
                    var temporary = new SemaphoreSlim(0);
                    abandoned[i] = new WeakReference(add.Invoke(temporary, null));
                }
            });
            producer.Start();
            producer.Join();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var rooted = (Task<bool>)queued.Target;
            lock (gate) semaphore.Release();
            Console.WriteLine("native queue root: " + (rooted is not null && rooted.Result));
            bool collected = false;
            for (int i = 0; i < abandoned.Length; i++)
                collected |= !abandoned[i].IsAlive;
            Console.WriteLine("abandoned queue collected: " + collected);
            Console.WriteLine("semaphore waiter helpers end");
        }

        internal static void RunPending()
        {
            Console.WriteLine("== pending task joins ==");
            PendingResults("int", 7, 9);
            PendingResults("long", 4294967296L, -4294967297L);
            PendingResults("string", "left", "right");
            PendingResults<object>("object", 11, "value");
            PendingResults("enum", PendingCode.Low, PendingCode.High);
            PendingResults("struct", new PendingPair { Number = 11, Text = "left" },
                new PendingPair { Number = 12, Text = "right" });
            PendingNonGeneric();
            PendingOutcomes();
            PendingNested();
            PendingWorker();
            PendingRaces();
            OrdinaryQueuedAwait();
            Console.WriteLine("pending task joins end");
        }
    }
}
