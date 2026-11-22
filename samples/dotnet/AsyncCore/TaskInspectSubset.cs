#nullable enable
using System;
using System.Threading.Tasks;

namespace TaskInspectSubset
{
    // A completed task supports timed waits and disposal. Only a faulted task
    // exposes an AggregateException. Task IDs are checked for positivity,
    // uniqueness and stability because each runtime assigns its own values.
    internal static class Program
    {
        private static void Run()
        {
            // Wait(TimeSpan) on a completed task inside `using`: True, and the
            // scope exit's Dispose (callvirt IDisposable::Dispose) is a no-op.
            using (Task<int> t = Task.FromResult(11))
            {
                Console.WriteLine("wait(ts): " + t.Wait(TimeSpan.FromSeconds(5)));
            }

            // Explicit Dispose() on a completed task (legal in real .NET too).
            Task done = Task.FromResult(2);
            done.Dispose();
            Console.WriteLine("dispose: ok");

            // Faulted: Exception is the AggregateException wrapper — non-null,
            // InnerException is the original fault, identity-stable across reads.
            // The WRAPPER's identity is deliberately not asserted: real .NET mints a fresh
            // AggregateException on every read and dn2cpp caches one, an unobserved
            // divergence. The inner exception is the same object on both.
            Task<int> f = Task.FromException<int>(new InvalidOperationException("boom"));
            Console.WriteLine("faulted: hasExc=" + (f.Exception != null)
                + " inner=" + f.Exception!.InnerException!.Message
                + " innerStable=" + ReferenceEquals(f.Exception.InnerException, f.Exception.InnerException));

            // Non-faulted statuses answer null — canceled included (real .NET
            // keeps a canceled task's OCE out of Exception).
            var tcs = new TaskCompletionSource<int>();
            tcs.SetCanceled();
            Console.WriteLine("nonfaulted: fromresult=" + (Task.FromResult(1).Exception == null)
                + " canceled=" + (tcs.Task.Exception == null));

            // Id: positive, distinct per task, stable across reads.
            Task a = Task.FromResult(3);
            Task b = Task.FromResult(4);
            int id1 = a.Id, id2 = a.Id;
            Console.WriteLine("id: aPos=" + (a.Id > 0) + " bPos=" + (b.Id > 0)
                + " distinct=" + (a.Id != b.Id) + " stable=" + (id1 == id2));
        }

        internal static void __GateEntry()
        {
            Run();
        }
    }
}
