using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlockingWaitArgsSubset
{
    // The argument contracts of the BLOCKING waits — Task.WaitAny / Task.WaitAll — and
    // of Task.WhenAll. These entry points are what keeps the C++ runtime's remaining
    // aborts unreachable: each rejects bad input with a catchable throw, so nothing
    // walks past them into a helper whose failure is an abort.
    //
    // WaitAny's contract is NOT WhenAny's, though they sit one function apart in the
    // runtime and read alike:
    //   * an EMPTY array is -1 for WaitAny and true/ArgumentException for
    //     WaitAll/WhenAny respectively;
    //   * a NULL ELEMENT is ArgumentException for WaitAny/WaitAll/WhenAll but
    //     ArgumentNullException for a two-task WhenAny;
    //   * the null scan covers the WHOLE array before an index may be returned, so a
    //     settled task ahead of a null one does not hide the rejection — and WaitAll
    //     validates before it waits, so a PENDING task ahead of a null one does not
    //     either (that case is the one that would otherwise block instead of throwing).
    // Every line is diffed against real .NET. The gate entry prints type names only; the
    // later sections print .NET's messages too, which the driver's invariant culture pin
    // keeps stable.
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
            // READ, never waited on: over already-settled inputs WhenAll completes before it
            // returns, and a wait ahead of the read would pass either way.
            Task.WaitAll(new Task[] { done, done2 });
            Task all = Task.WhenAll(new Task[] { done, done2 });
            Console.WriteLine("alive: " + Task.WaitAny(new Task[] { done2, done })
                + "," + all.IsCompleted);
        }

        // The IEnumerable<Task> overloads of WhenAll and WhenAny: a null sequence throws
        // ArgumentNullException naming `tasks`, and the sequence's enumerator is disposed
        // once it has been read.
        internal static void RunSequences()
        {
            Null("whenall-null-seq", () => Task.WhenAll((IEnumerable<Task>)null));
            Null("whenall-T-null-seq", () => Task.WhenAll((IEnumerable<Task<int>>)null));
            Null("whenany-null-seq", () => Task.WhenAny((IEnumerable<Task>)null));
            Null("whenany-T-null-seq", () => Task.WhenAny((IEnumerable<Task<int>>)null));
            Console.WriteLine("whenall-seq-disposed: "
                + Task.WhenAll(new OneTask<Task>(Task.CompletedTask)).IsCompleted + "," + OneTask<Task>.Disposals);
            Console.WriteLine("whenall-T-seq-disposed: "
                + Task.WhenAll(new OneTask<Task<int>>(Task.FromResult(4))).Result[0] + "," + OneTask<Task<int>>.Disposals);
            Console.WriteLine("whenany-seq-disposed: "
                + Task.WhenAny(new OneTask<Task>(Task.CompletedTask)).IsCompleted + "," + OneTask<Task>.Disposals);
        }

        private static void Null(string tag, Func<object> f)
        {
            try { Console.WriteLine(tag + ": " + f()); }
            catch (ArgumentNullException ex) { Console.WriteLine(tag + ": " + ex.GetType().Name + ": " + ex.Message); }
        }

        // The Task[] overloads' rejections carry .NET's messages, which name the parameter:
        // `tasks` for a null or empty array and a null element — except a two-task WhenAny,
        // which .NET forwards to WhenAny(task1, task2), so its null element is an
        // ArgumentNullException naming that parameter, from an array, a sequence or loose
        // tasks alike.
        internal static void RunArrayMessages()
        {
            Task done = Task.CompletedTask;
            Task<int> doneT = Task.FromResult(1);
            Arg("whenall-null-array", () => Task.WhenAll((Task[])null));
            Arg("whenall-T-null-array", () => Task.WhenAll((Task<int>[])null));
            Arg("whenany-null-array", () => Task.WhenAny((Task[])null));
            Arg("whenany-T-null-array", () => Task.WhenAny((Task<int>[])null));
            Arg("waitall-null-array", () => { Task.WaitAll((Task[])null); return "ok"; });
            Arg("waitany-null-array", () => Task.WaitAny((Task[])null));
            Arg("whenall-null-element", () => Task.WhenAll(new Task[] { done, null }));
            Arg("whenall-T-null-element", () => Task.WhenAll(new Task<int>[] { doneT, null }));
            Arg("whenall-seq-null-element", () => Task.WhenAll((IEnumerable<Task>)new List<Task> { done, null }));
            Arg("whenall-loose-null", () => Task.WhenAll(done, null));
            Arg("waitall-null-element", () => { Task.WaitAll(new Task[] { done, null }); return "ok"; });
            Arg("waitany-null-element", () => Task.WaitAny(new Task[] { done, null }));
            Arg("whenany-empty", () => Task.WhenAny(new Task[0]));
            Arg("whenany-T-empty", () => Task.WhenAny(new Task<int>[0]));
            Arg("whenany-seq-empty", () => Task.WhenAny((IEnumerable<Task>)new List<Task>()));
            Arg("whenany-one-null", () => Task.WhenAny(new Task[] { null }));
            Arg("whenany-two-first-null", () => Task.WhenAny(new Task[] { null, done }));
            Arg("whenany-two-second-null", () => Task.WhenAny(new Task[] { done, null }));
            Arg("whenany-T-two-second-null", () => Task.WhenAny(new Task<int>[] { doneT, null }));
            Arg("whenany-loose-two-null", () => Task.WhenAny(done, null));
            Arg("whenany-seq-two-null", () => Task.WhenAny((IEnumerable<Task>)new List<Task> { null, done }));
            Arg("whenany-three-null", () => Task.WhenAny(new Task[] { done, null, done }));
            Arg("whenany-T-three-null", () => Task.WhenAny(new Task<int>[] { doneT, doneT, null }));
            Arg("whenany-loose-three-null", () => Task.WhenAny(done, done, null));
            Arg("whenany-seq-three-null", () => Task.WhenAny((IEnumerable<Task>)new List<Task> { done, null, done }));
        }

        private static void Arg(string tag, Func<object> f)
        {
            try { Console.WriteLine(tag + ": " + f()); }
            catch (ArgumentException ex) { Console.WriteLine(tag + ": " + ex.GetType().Name + ": " + ex.Message); }
        }

        // A null element of an IEnumerable<Task> source. .NET's WhenAny forwards to
        // WhenAny(task1, task2) only for an exact List<TTask> or a TTask[] (covariant
        // arrays included) holding two tasks; any other sequence is an ArgumentException
        // naming `tasks`, raised at the first null so a lazy source is read no further.
        // WhenAll<TResult> also stops at the first null; the non-generic WhenAll reads
        // the whole sequence before it rejects the null.
        internal static void RunSequenceNulls()
        {
            Console.WriteLine("== sequence nulls ==");
            Task done = Task.CompletedTask;
            Task<int> doneT = Task.FromResult(1);
            Arg("whenany-hashset-null", () => Task.WhenAny(new HashSet<Task> { done, null }).IsCompleted);
            Arg("whenany-hashset-only-null", () => Task.WhenAny(new HashSet<Task> { null }).IsCompleted);
            Arg("whenany-queue-null", () => Task.WhenAny(new Queue<Task>(new[] { done, null })).IsCompleted);
            Arg("whenany-lazy-array-null", () => Task.WhenAny(Pass(new[] { done, null })).IsCompleted);
            Arg("whenany-sublist-null", () => Task.WhenAny(new TaskList { done, null }).IsCompleted);
            Arg("whenany-covariant-list-null",
                () => Task.WhenAny((IEnumerable<Task>)new List<Task<int>> { doneT, null }).IsCompleted);
            Arg("whenany-array-seq-null", () => Task.WhenAny((IEnumerable<Task>)new[] { done, null }).IsCompleted);
            Arg("whenany-covariant-array-null",
                () => Task.WhenAny((IEnumerable<Task>)new[] { doneT, null }).IsCompleted);
            Arg("whenany-T-hashset-null", () => Task.WhenAny(new HashSet<Task<int>> { doneT, null }).IsCompleted);
            Arg("whenany-T-list-null",
                () => Task.WhenAny((IEnumerable<Task<int>>)new List<Task<int>> { doneT, null }).IsCompleted);
            Arg("whenany-T-array-seq-null",
                () => Task.WhenAny((IEnumerable<Task<int>>)new[] { null, doneT }).IsCompleted);
            Arg("whenany-lazy-empty", () => Task.WhenAny(Pass(new Task[0])).IsCompleted);
            var log = new List<string>();
            Arg("whenany-lazy-null", () => Task.WhenAny(Yield(log, done)).IsCompleted);
            Console.WriteLine("  read: " + string.Join(",", log));
            log.Clear();
            Arg("whenany-T-lazy-null", () => Task.WhenAny(Yield(log, doneT)).IsCompleted);
            Console.WriteLine("  read: " + string.Join(",", log));
            log.Clear();
            Arg("whenall-lazy-null", () => Task.WhenAll(Yield(log, done)).IsCompleted);
            Console.WriteLine("  read: " + string.Join(",", log));
            log.Clear();
            Arg("whenall-T-lazy-null", () => Task.WhenAll(Yield(log, doneT)).IsCompleted);
            Console.WriteLine("  read: " + string.Join(",", log));
            Console.WriteLine("sequence nulls end");
        }

        // The same elements as a lazy sequence that is neither a list nor an array.
        private static IEnumerable<T> Pass<T>(T[] items)
        {
            foreach (T item in items)
                yield return item;
        }

        // Yields `first`, a null, then `first` again, logging each step it reaches.
        private static IEnumerable<T> Yield<T>(List<string> log, T first) where T : Task
        {
            try
            {
                log.Add("first");
                yield return first;
                log.Add("null");
                yield return null;
                log.Add("after");
                yield return first;
            }
            finally
            {
                log.Add("disposed");
            }
        }
    }

    // A List<Task> subclass: not an exact List<Task>, so .NET takes its general path.
    internal sealed class TaskList : List<Task>
    {
    }

    // Yields one task; counts enumerator disposals.
    internal sealed class OneTask<T> : IEnumerable<T> where T : Task
    {
        internal static int Disposals;
        private readonly T _task;

        internal OneTask(T task) => _task = task;

        public IEnumerator<T> GetEnumerator() => new Cursor(_task);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Cursor : IEnumerator<T>
        {
            private readonly T _task;
            private bool _read;

            internal Cursor(T task) => _task = task;

            public T Current => _task;

            object IEnumerator.Current => _task;

            public bool MoveNext() => !_read && (_read = true);

            public void Reset() => _read = false;

            public void Dispose() => Disposals++;
        }
    }
}
