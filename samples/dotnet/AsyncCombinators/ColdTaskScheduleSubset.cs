using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ColdTaskScheduleSubset;

internal static class Program
{
    private static void Fault(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine(label + " -> returned");
        }
        catch (Exception ex)
        {
            string param = ex is ArgumentException arg ? " [" + arg.ParamName + "]" : "";
            Console.WriteLine(label + " -> " + ex.GetType().Name + ": " + ex.Message + param);
        }
    }

    private sealed class ProbeScheduler : TaskScheduler
    {
        internal Task Pending;
        internal Exception Refusal;
        internal Action BeforeQueue;
        internal CancellationTokenSource CancelBeforeRefusal;
        internal bool ExecuteOnQueue;
        internal bool ExecuteQueuedInline;
        internal bool PreviouslyQueued;
        internal int Queued;
        internal int InlineAttempts;
        private int _calls;
        private bool _current;
        internal bool BaseDequeue(Task task) => base.TryDequeue(task);
        internal bool Dequeue(Task task) => TryDequeue(task);
        internal bool Execute(Task task) => TryExecuteTask(task);
        protected override bool TryDequeue(Task task)
        {
            if (Pending is null || Pending != task) return false;
            Pending = null;
            return true;
        }
        protected override IEnumerable<Task> GetScheduledTasks() => Array.Empty<Task>();
        protected override void QueueTask(Task task)
        {
            Interlocked.Increment(ref Queued);
            BeforeQueue?.Invoke();
            if (CancelBeforeRefusal is not null)
            {
                Pending = task;
                CancelBeforeRefusal.Cancel();
            }
            if (Refusal is not null) throw Refusal;
            if (ExecuteOnQueue)
                TryExecuteTask(task);
            else Pending = task;
        }
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            Interlocked.Increment(ref InlineAttempts);
            PreviouslyQueued = taskWasPreviouslyQueued;
            return ExecuteQueuedInline && taskWasPreviouslyQueued && TryDequeue(task) && TryExecuteTask(task);
        }

        internal int Observe()
        {
            Interlocked.Increment(ref _calls);
            _current = TaskScheduler.Current == this;
            return 17;
        }

        internal void Report(string name, Task task)
        {
            bool done = task.Wait(TimeSpan.FromSeconds(3));
            string result = done && task is Task<int> value ? " result=" + value.Result : "";
            Console.WriteLine(name + ": complete=" + done + " queue=" + Queued + " inline=" + InlineAttempts
                + " calls=" + _calls + " current=" + _current + result);
        }
    }

    private sealed class CompletionContext : SynchronizationContext
    {
        internal bool Cleared;
        internal int Posts;
        public override void Post(SendOrPostCallback callback, object state)
        {
            Interlocked.Increment(ref Posts);
            Cleared = Task.CurrentId is null && TaskScheduler.Current == TaskScheduler.Default;
            callback(state);
        }
    }

    internal static void RunSchedulerDequeue()
    {
        var task = new Task(() => { });
        var scheduler = new ProbeScheduler { Pending = task };
        var method = typeof(TaskScheduler).GetMethod("TryDequeue", BindingFlags.Instance | BindingFlags.NonPublic);
        var closedNull = (Func<Task, bool>)method.CreateDelegate(typeof(Func<Task, bool>), null);
        Console.WriteLine("scheduler dequeue: " + scheduler.BaseDequeue(task) + "/"
            + scheduler.Dequeue(task) + "/" + scheduler.Dequeue(task) + " closed-null=" + closedNull(task));
    }

    internal static void RunCustomStart()
    {
        using var source = new CancellationTokenSource();
        int calls = 0;
        var task = new Task(() => calls++, source.Token);
        var scheduler = new ProbeScheduler();
        task.Start(scheduler);
        bool handedOff = scheduler.Pending == task && task.Status == TaskStatus.WaitingToRun;
        source.Cancel();
        bool canceled = scheduler.Pending is null && task.IsCanceled;

        scheduler.Refusal = new FormatException("queue refused");
        var refused = new Task(() => calls++);
        bool faulted = false;
        try { refused.Start(scheduler); }
        catch (TaskSchedulerException e)
        {
            faulted = ReferenceEquals(e.InnerException, scheduler.Refusal)
                && ReferenceEquals(refused.Exception.InnerException, e);
        }
        Console.WriteLine("custom Start: handoff=" + handedOff + " canceled=" + canceled + " faulted=" + faulted + " calls=" + calls);

        using var duringQueue = new CancellationTokenSource();
        scheduler.CancelBeforeRefusal = duringQueue;
        var removed = new Task(() => calls++, duringQueue.Token);
        bool storedAfterCancellation = false;
        try { removed.Start(scheduler); }
        catch (Exception e)
        {
            storedAfterCancellation = e is InvalidCastException && removed.IsFaulted
                && ReferenceEquals(removed.Exception?.InnerException?.InnerException, scheduler.Refusal);
        }
        Console.WriteLine("custom Start: rejected after cancellation=" + storedAfterCancellation);

        var owner = new ProbeScheduler();
        Task<int> outer = null;
        Task inner = null;
        bool hidden = false;
        inner = new Task(() => hidden = TaskScheduler.Current == TaskScheduler.Default && Task.CurrentId == inner.Id,
            TaskCreationOptions.HideScheduler);
        outer = new Task<int>(() =>
        {
            inner.Start();
            bool nested = owner.Pending == inner && owner.Execute(inner);
            return nested && hidden && TaskScheduler.Current == owner && Task.CurrentId == outer.Id ? 42 : 0;
        });
        outer.Start(owner);
        var context = new CompletionContext();
        var previous = SynchronizationContext.Current;
        Task observed;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            observed = ObserveCompletion(outer);
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        bool ran = owner.Execute(outer);
        observed.Wait();
        Console.WriteLine("custom execute: first=" + ran + " repeat=" + owner.Execute(outer)
            + " result=" + outer.Result + " restored=" + (TaskScheduler.Current == TaskScheduler.Default && Task.CurrentId is null)
            + " canceled before rejection=" + scheduler.Execute(removed));
        Fault("foreign execute", () => scheduler.Execute(outer));
        Console.WriteLine("custom execute: completion post=" + context.Cleared + " racing refusal=" + RejectRunningTask());
    }

    private static async Task ObserveCompletion(Task task) => await task;

    internal static void RunManagedDispatch()
    {
        Console.WriteLine("== managed scheduler dispatch ==");
        var factoryAction = new ProbeScheduler { ExecuteOnQueue = true };
        factoryAction.Report("StartNew(Action)", Task.Factory.StartNew(() => { factoryAction.Observe(); },
            CancellationToken.None, TaskCreationOptions.None, factoryAction));
        var factoryFunc = new ProbeScheduler { ExecuteOnQueue = true };
        factoryFunc.Report("StartNew(Func)", Task.Factory.StartNew(factoryFunc.Observe,
            CancellationToken.None, TaskCreationOptions.None, factoryFunc));

        var source = new TaskCompletionSource<int>();
        var actionScheduler = new ProbeScheduler { ExecuteOnQueue = true };
        var funcScheduler = new ProbeScheduler { ExecuteOnQueue = true };
        Task action = source.Task.ContinueWith(_ => { actionScheduler.Observe(); }, actionScheduler);
        Task<int> func = source.Task.ContinueWith(t => t.Result + funcScheduler.Observe(), funcScheduler);
        Console.WriteLine("continuations pending=" + (!action.IsCompleted && !func.IsCompleted
            && actionScheduler.Queued == 0 && funcScheduler.Queued == 0));
        source.SetResult(25);
        actionScheduler.Report("ContinueWith(Action)", action);
        funcScheduler.Report("ContinueWith(Func)", func);

        var sync = new ProbeScheduler { ExecuteOnQueue = true };
        bool inlineFirst = false;
        sync.BeforeQueue = () => inlineFirst = sync.InlineAttempts == 1;
        var cold = new Task(() => sync.Observe());
        cold.RunSynchronously(sync);
        sync.Report("RunSynchronously inline first=" + inlineFirst, cold);
    }

    internal static void RunQueuedWait()
    {
        var scheduler = new ProbeScheduler { ExecuteQueuedInline = true };
        int bodyThread = 0;
        var held = new Task(() =>
        {
            bodyThread = Environment.CurrentManagedThreadId;
            scheduler.Observe();
        });
        held.Start(scheduler);
        if (scheduler.Pending != held || held.IsCompleted)
            throw new InvalidOperationException("The scheduler did not hold the task");

        using var entering = new ManualResetEventSlim();
        Exception waitError = null;
        var waiter = new Thread(() =>
        {
            entering.Set();
            try { held.Wait(); }
            catch (Exception e) { waitError = e; }
        }) { IsBackground = true };
        waiter.Start();
        var limit = TimeSpan.FromSeconds(3);
        bool unaided = entering.Wait(limit) && waiter.Join(limit);
        if (!unaided)
        {
            try { scheduler.Execute(held); }
            finally
            {
                if (!waiter.Join(limit))
                    throw new TimeoutException("Wait did not return after releasing the task");
            }
        }
        if (waitError is not null) throw new InvalidOperationException("Queued task wait failed", waitError);
        scheduler.Report("queued Wait unaided=" + unaided + " previously queued=" + scheduler.PreviouslyQueued
            + " on waiter=" + (bodyThread == waiter.ManagedThreadId), held);
    }

    private static async Task<bool> ResumeOn(Task pending, TaskScheduler expected, bool yield = false, bool capture = true)
    {
        if (yield) await Task.Yield();
        else if (capture) await pending;
        else await pending.ConfigureAwait(false);
        return TaskScheduler.Current == expected;
    }

    internal static void RunAwaitScheduler()
    {
        var scheduler = new ProbeScheduler { ExecuteOnQueue = true };
        var posted = new CompletionContext();
        var bypassed = new CompletionContext();
        var source = new TaskCompletionSource<int>();
        Task<bool> pending = null, yielded = null, preferred = null, uncaptured = null;
        bool entered = false;
        var entry = new Task(() =>
        {
            var previous = SynchronizationContext.Current;
            entered = previous is null && TaskScheduler.Current == scheduler;
            pending = ResumeOn(source.Task, scheduler);
            yielded = ResumeOn(source.Task, scheduler, yield: true);
            try
            {
                SynchronizationContext.SetSynchronizationContext(posted);
                preferred = ResumeOn(source.Task, TaskScheduler.Default);
                SynchronizationContext.SetSynchronizationContext(bypassed);
                uncaptured = ResumeOn(source.Task, TaskScheduler.Default, capture: false);
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        });
        entry.Start(scheduler);
        var limit = TimeSpan.FromSeconds(3);
        if (!entry.Wait(limit) || pending.IsCompleted || preferred.IsCompleted || uncaptured.IsCompleted)
            throw new InvalidOperationException("The awaits did not suspend on the pending task");
        source.SetResult(1);
        if (!Task.WhenAll(new Task[] { pending, yielded, preferred, uncaptured }).Wait(limit))
            throw new TimeoutException("The await continuations did not finish");
        Console.WriteLine("await scheduler: entered=" + entered + " pending=" + pending.Result + " yield=" + yielded.Result
            + " queues=" + scheduler.Queued);
        Console.WriteLine("await context preference: posts=" + posted.Posts + "/" + bypassed.Posts
            + " default scheduler=" + preferred.Result + "/" + uncaptured.Result);
    }

    private static bool RejectRunningTask()
    {
        using var entered = new ManualResetEventSlim();
        using var finish = new ManualResetEventSlim();
        var owner = new ProbeScheduler { Refusal = new ApplicationException("queue") };
        var failure = new FormatException("body");
        var task = new Task(() => { entered.Set(); finish.Wait(); throw failure; });
        bool rejectedFinish = false;
        var worker = new Thread(() =>
        {
            try { owner.Execute(task); }
            catch (InvalidCastException) { rejectedFinish = true; }
        });
        owner.BeforeQueue = () => { worker.Start(); entered.Wait(); };
        try { task.Start(owner); }
        catch (TaskSchedulerException) { }
        finally { finish.Set(); }
        worker.Join();
        var errors = task.Exception.InnerExceptions;
        return rejectedFinish && errors.Count == 2 && ReferenceEquals(errors[0].InnerException, owner.Refusal)
            && ReferenceEquals(errors[1], failure);
    }

    internal static void RunSchedulerIds()
    {
        Console.WriteLine("== scheduler ids ==");
        var defaultScheduler = TaskScheduler.Default;
        int defaultId = defaultScheduler.Id;
        var property = typeof(TaskScheduler).GetProperty(nameof(TaskScheduler.Id));
        Console.WriteLine("default/current/reflected: " + (defaultId > 0) + "/" +
            (defaultScheduler.Id == defaultId) + "/" + (TaskScheduler.Current.Id == defaultId) + "/" +
            ((int)property.GetValue(defaultScheduler) == defaultId));

        TaskScheduler first = new ProbeScheduler();
        TaskScheduler second = new ProbeScheduler();
        int secondId = second.Id;
        int taskId = new Task(() => { }).Id;
        int firstId = first.Id;
        Console.WriteLine("read order and independent task counter: " + (secondId > defaultId) + "/" +
            (firstId == secondId + 1) + "/" + (taskId > 0) + "/" + (first.Id == firstId));

        var shared = (TaskScheduler)RuntimeHelpers.GetUninitializedObject(typeof(ProbeScheduler));
        using var start = new ManualResetEventSlim();
        int left = 0;
        int right = 0;
        var reader1 = new Thread(() => { start.Wait(); left = shared.Id; });
        var reader2 = new Thread(() => { start.Wait(); right = shared.Id; });
        reader1.Start();
        reader2.Start();
        start.Set();
        reader1.Join();
        reader2.Join();
        Console.WriteLine("shared first read: " + (left > firstId) + "/" + (left == right) + "/" +
            (left == shared.Id));
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== cold task scheduler ==");
        Console.WriteLine("scheduler: " + (TaskScheduler.Default is not null) + "," +
            (TaskScheduler.Current is not null) + "," +
            ReferenceEquals(TaskScheduler.Default, TaskScheduler.Current));

        var cold = new Task(() => { });
        Fault("cold Start(null)", () => cold.Start(null!));
        Fault("cold RunSynchronously(null)", () => cold.RunSynchronously(null!));
        cold.RunSynchronously();
        Fault("completed Start(null)", () => cold.Start(null!));
        Fault("completed RunSynchronously(null)", () => cold.RunSynchronously(null!));

        var promise = new TaskCompletionSource();
        Fault("promise Start(null)", () => promise.Task.Start(null!));
        Fault("promise Start(default)", () => promise.Task.Start(TaskScheduler.Default));
        Fault("promise RunSynchronously(default)", () => promise.Task.RunSynchronously(TaskScheduler.Default));

        var continuation = promise.Task.ContinueWith(_ => { });
        Fault("continuation Start(default)", () => continuation.Start(TaskScheduler.Default));
        Fault("continuation RunSynchronously(default)", () => continuation.RunSynchronously(TaskScheduler.Default));
        promise.SetResult();
        continuation.Wait();

        var pending = new TaskCompletionSource();
        Task started = Task.Run(() => pending.Task.Wait());
        Fault("started Start(default)", () => started.Start(TaskScheduler.Default));
        Fault("started RunSynchronously(default)", () => started.RunSynchronously(TaskScheduler.Default));
        pending.SetResult();
        started.Wait();

        var inner = new TaskCompletionSource();
        Task unwrap = Task.Run((Func<Task>)(() => inner.Task));
        Fault("unwrap Start(default)", () => unwrap.Start(TaskScheduler.Default));
        Fault("unwrap RunSynchronously(default)", () => unwrap.RunSynchronously(TaskScheduler.Default));
        inner.SetResult();
        unwrap.Wait();

        Fault("StartNew(action, null)", () => Task.Factory.StartNew(
            () => { }, CancellationToken.None, TaskCreationOptions.None, null!));
        Fault("StartNew(null action, null)", () => Task.Factory.StartNew(
            (Action)null!, CancellationToken.None, TaskCreationOptions.None, null!));
        Fault("StartNew<int>(null)", () => Task.Factory.StartNew(
            () => 1, CancellationToken.None, TaskCreationOptions.None, null!));
        Fault("StartNew<int>(null function, null scheduler)", () => Task.Factory.StartNew<int>(
            (Func<int>)null!, CancellationToken.None, TaskCreationOptions.None, null!));
        Fault("StartNew<int>(null function, state, null scheduler)", () => Task.Factory.StartNew<int>(
            (Func<object?, int>)null!, null, CancellationToken.None, TaskCreationOptions.None, null!));
        Fault("ContinueWith(action, null)", () => Task.CompletedTask.ContinueWith(
            _ => { }, CancellationToken.None, TaskContinuationOptions.None, null!));
        Fault("ContinueWith(null action, null)", () => Task.CompletedTask.ContinueWith(
            (Action<Task>)null!, CancellationToken.None, TaskContinuationOptions.None, null!));
        Fault("ContinueWith<int>(function, null)", () => Task.CompletedTask.ContinueWith(
            _ => 1, CancellationToken.None, TaskContinuationOptions.None, null!));
        Console.WriteLine("cold task scheduler end");
    }
}
