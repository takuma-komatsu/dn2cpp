using System;
using System.Threading.Tasks;

namespace BlockingWaitArgsSubset
{
    // The argument contracts of the BLOCKING waits — Task.WaitAny / Task.WaitAll — and
    // of Task.WhenAll, which had none at all (a null array or a null element reached the
    // registration loop as a raw dereference). These entry points are what keeps the C++
    // runtime's remaining aborts unreachable: each rejects bad input with a catchable
    // throw, so nothing walks past them into a helper whose failure is an abort.
    //
    // WaitAny's contract is NOT WhenAny's, though they sit one function apart in the
    // runtime and read alike:
    //   * an EMPTY array is -1 for WaitAny and true/ArgumentException for
    //     WaitAll/WhenAny respectively;
    //   * a NULL ELEMENT is ArgumentException for WaitAny/WaitAll/WhenAll but
    //     ArgumentNullException for WhenAny;
    //   * the null scan covers the WHOLE array before an index may be returned, so a
    //     settled task ahead of a null one does not hide the rejection — and WaitAll
    //     validates before it waits, so a PENDING task ahead of a null one does not
    //     either (that case is the one that would otherwise block instead of throwing).
    // Every line is diffed against real .NET; type names only, since the messages are
    // localized.
    internal static class Program
    {
        private static void E(string tag, Func<object> f)
        {
            try { Console.WriteLine(tag + ": " + f()); }
            catch (Exception ex) { Console.WriteLine(tag + ": " + ex.GetType().Name); }
        }

        internal static void __GateEntry()
        {
            Task done = Task.CompletedTask;
            Task done2 = Task.FromResult(7);
            Task<int> doneT = Task.FromResult(1);
            // Never completed: WaitAll must reject the null element WITHOUT waiting on
            // this one. A regression that drains first reports the deadlock verdict here
            // instead, which is a different line rather than a hang.
            Task stuck = new TaskCompletionSource<int>().Task;

            E("waitany-empty", () => Task.WaitAny(new Task[0]));
            E("waitany-null-array", () => Task.WaitAny((Task[])null));
            E("waitany-done-then-null", () => Task.WaitAny(new Task[] { done, null }));
            E("waitany-null-then-done", () => Task.WaitAny(new Task[] { null, done }));
            E("waitany-two-done", () => Task.WaitAny(new Task[] { done, done2 }));

            E("waitall-empty", () => { Task.WaitAll(new Task[0]); return "ok"; });
            E("waitall-null-array", () => { Task.WaitAll((Task[])null); return "ok"; });
            E("waitall-done-then-null", () => { Task.WaitAll(new Task[] { done, null }); return "ok"; });
            E("waitall-stuck-then-null", () => { Task.WaitAll(new Task[] { stuck, null }); return "ok"; });
            E("waitall-two-done", () => { Task.WaitAll(new Task[] { done, done2 }); return "ok"; });

            E("whenall-null-array", () => Task.WhenAll((Task[])null).IsCompleted);
            E("whenall-done-then-null", () => Task.WhenAll(new Task[] { done, null }).IsCompleted);
            E("whenall-T-null-array", () => Task.WhenAll((Task<int>[])null).IsCompleted);
            E("whenall-T-done-then-null", () => Task.WhenAll(new Task<int>[] { doneT, null }).IsCompleted);
            E("whenall-empty", () => Task.WhenAll(new Task[0]).IsCompleted);
            E("whenall-T-empty", () => Task.WhenAll(new Task<int>[0]).Result.Length);

            // Still alive and still combining after every rejection above — the property an
            // abort cannot demonstrate, and the reason these are throws at all. The join is
            // WAITED rather than read: a WhenAll over already-settled inputs posts its
            // completion to the scheduler here instead of finishing inline, so its
            // IsCompleted is only stable once something has turned the loop.
            Task.WaitAll(new Task[] { done, done2 });
            Task all = Task.WhenAll(new Task[] { done, done2 });
            all.Wait();
            Console.WriteLine("alive: " + Task.WaitAny(new Task[] { done2, done })
                + "," + all.IsCompleted);
        }
    }
}
