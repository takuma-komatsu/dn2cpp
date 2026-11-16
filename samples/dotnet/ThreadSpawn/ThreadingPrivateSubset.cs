using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

internal static class ThreadingPrivateSubset
{
    private static int s_nameEffects;

    private static string NameArgument()
    {
        s_nameEffects++;
        return "diagnostic-name";
    }

    // The gate replaces these bodies with the loaded CoreLib declaration.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Direct(Thread? receiver, string? name) => throw new InvalidOperationException("unpatched thread name call");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Address(Thread? receiver, string? name) => throw new InvalidOperationException("unpatched thread name address");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool LockDirect(System.Threading.Lock? receiver) => throw new InvalidOperationException("unpatched lock waiter call");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool LockAddress(System.Threading.Lock? receiver) => throw new InvalidOperationException("unpatched lock waiter address");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void LockResetDirect(System.Threading.Lock? receiver) => throw new InvalidOperationException("unpatched lock reset call");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void LockResetAddress(System.Threading.Lock? receiver) => throw new InvalidOperationException("unpatched lock reset address");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void LockRecordDirect(System.Threading.Lock? receiver) => throw new InvalidOperationException("unpatched lock record call");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void LockRecordAddress(System.Threading.Lock? receiver) => throw new InvalidOperationException("unpatched lock record address");

    internal static void PublicNames()
    {
        Console.WriteLine("== thread names ==");
        string? seen = null;
        var worker = new Thread(() =>
        {
            seen = Thread.CurrentThread.Name;
            Thread.CurrentThread.Name = "worker-after";
        });
        Console.WriteLine("thread fresh name=" + (worker.Name ?? "null"));
        worker.Name = "worker-before";
        worker.Start();
        worker.Join();
        Console.WriteLine("thread worker names=" + seen + "/" + worker.Name);
        worker.Name = null;
        Console.WriteLine("thread cleared name=" + (worker.Name ?? "null"));
        worker.Name = "";
        Console.WriteLine("thread empty name=" + (worker.Name == ""));
        Console.WriteLine("thread names end");
    }

    internal static void NotificationBoundary()
    {
        Thread thread = Thread.CurrentThread;
        thread.Name = "managed-name";
        void Run(string label, Action action)
        {
            string outcome;
            try
            {
                action();
                outcome = "returned";
            }
            catch (Exception error)
            {
                if (error is TargetInvocationException invocation)
                    error = invocation.InnerException!;
                outcome = error.GetType().Name;
                if (error is PlatformNotSupportedException)
                    outcome += "/" + error.Message.Contains("Thread.ThreadNameChanged");
            }
            Console.WriteLine(label + "=" + outcome + "|unchanged=" + (thread.Name == "managed-name"));
        }
        Run("thread notify direct", () => Direct(thread, NameArgument()));
        Run("thread notify null name", () => Direct(thread, null));
        Run("thread notify address", () => Address(thread, "diagnostic-name"));
        Run("thread notify null direct", () => Direct(null, "diagnostic-name"));
        Run("thread notify null address", () => Address(null, NameArgument()));
        Run("thread notify reflection", () => typeof(Thread).GetMethod("ThreadNameChanged",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(thread, new object?[] { "diagnostic-name" }));
        Console.WriteLine("thread notify name effects=" + s_nameEffects);
        Console.WriteLine("thread notification boundary end");

        var sync = new System.Threading.Lock();
        void RunLock(string label, Func<bool> action, string member = "ShouldStopPreemptingWaiters")
        {
            string outcome;
            try
            {
                outcome = action().ToString();
            }
            catch (Exception error)
            {
                if (error is TargetInvocationException invocation)
                    error = invocation.InnerException!;
                outcome = error.GetType().Name;
                if (error is PlatformNotSupportedException)
                    outcome += "/" + error.Message.Contains("Lock." + member);
            }
            Console.WriteLine(label + "=" + outcome);
        }
        RunLock("lock waiter direct", () => LockDirect(sync));
        RunLock("lock waiter address", () => LockAddress(sync));
        RunLock("lock waiter null direct", () => LockDirect(null));
        RunLock("lock waiter null address", () => LockAddress(null));
        RunLock("lock waiter reflection", () => (bool)typeof(System.Threading.Lock).GetMethod("get_ShouldStopPreemptingWaiters",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(sync, null)!);
        RunLock("lock reset direct", () => { LockResetDirect(sync); return false; }, "ResetWaiterStartTime");
        RunLock("lock reset address", () => { LockResetAddress(sync); return false; }, "ResetWaiterStartTime");
        RunLock("lock record direct", () => { LockRecordDirect(sync); return false; }, "RecordWaiterStartTime");
        RunLock("lock record address", () => { LockRecordAddress(sync); return false; }, "RecordWaiterStartTime");
        RunLock("lock reset null", () => { LockResetDirect(null); return false; }, "ResetWaiterStartTime");
        RunLock("lock record null", () => { LockRecordAddress(null); return false; }, "RecordWaiterStartTime");
        Console.WriteLine("lock waiter boundary end");
    }
}
