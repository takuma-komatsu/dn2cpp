using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

// An exception escaping Main terminates the process ABNORMALLY in real .NET —
// report on stderr, then abort (SIGABRT, 134 on Unix), not a clean nonzero
// exit. The generated main's catch funnel must match, and stdout written before
// the throw must survive on both sides (stack traces differ, so stderr is off
// the diff).
internal static class Program
{
    private sealed class GenericFailure<T> : Exception
    {
        public GenericFailure(string message) : base(message) { }
    }

    private static void ThrowPool(object state) => Escape("pool");
    private static void ThrowThread() => Escape("thread");
    private static void ThrowTimer(object state) => Escape("timer");
    private static void ThrowLocal() => Escape("local continuation");
    private static void ThrowNested() => Escape("nested drain");
    private static void ThrowGeneric(object state)
    {
        Console.WriteLine("throwing generic exception");
        throw new GenericFailure<int>("escaped generic exception");
    }

    private static void Escape(string mode)
    {
        Console.WriteLine("throwing " + mode);
        throw new InvalidOperationException("escaped " + mode);
    }

    private static async void ThrowAsyncVoid()
    {
        await Task.Yield();
        Escape("async void");
    }

    private static void QueueLocal(object state)
        => Task.Yield().GetAwaiter().OnCompleted(ThrowLocal);

    private static Task<int> QueueNested()
    {
        Task.Yield().GetAwaiter().OnCompleted(ThrowNested);
        return new TaskCompletionSource<int>().Task;
    }

    static void Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine("before throw");
        if (args.Length != 0)
        {
            string mode = args[0];
            Console.WriteLine("-- escaping worker: " + mode + " --");
            switch (mode)
            {
                case "pool": ThreadPool.QueueUserWorkItem(ThrowPool); break;
                case "thread": new Thread(ThrowThread).Start(); break;
                case "timer": _ = new Timer(ThrowTimer, null, 1, Timeout.Infinite); break;
                case "cts timer":
                    var source = new CancellationTokenSource();
                    source.Token.Register(() => Escape("cts timer"));
                    source.CancelAfter(1);
                    break;
                case "async void": ThreadPool.QueueUserWorkItem(_ => ThrowAsyncVoid()); break;
                case "local continuation": ThreadPool.QueueUserWorkItem(QueueLocal); break;
                case "nested drain": _ = Task.Factory.StartNew(QueueNested); break;
                case "generic exception": ThreadPool.QueueUserWorkItem(ThrowGeneric); break;
                default: throw new ArgumentException("Unknown worker mode");
            }
            Thread.Sleep(Timeout.Infinite);
            return;
        }
        throw new InvalidOperationException("escaped Main");
    }
}
