using System;
using System.Threading;

namespace CancellationRegister;

// Cancel runs each registration through the source's private dispatcher: it passes
// the state and the source's token, runs a multicast callback in order and stops it
// at the first throw, which Cancel surfaces. CancellationToken(true) yields one
// canonical canceled token, including on first use from racing threads.
internal static class CancellationDispatchSubset
{
    private static int _calls;
    private static int _order;
    private static object? _state;
    private static CancellationToken _token;
    private static Exception? _marker;

    private static void Reset()
    {
        _calls = 0;
        _order = 0;
        _state = null;
        _token = default;
    }

    private static string Effects(object? state) =>
        " calls=" + _calls + " order=" + _order + " state=" + ReferenceEquals(_state, state);

    private static string Token(CancellationToken expected) =>
        " token=" + _token.CanBeCanceled + ":" + _token.IsCancellationRequested + ":" + _token.Equals(expected);

    private static void ActionOne(object? state) { _calls++; _order = _order * 10 + 1; _state = state; }

    private static void ActionTwo(object? state) { _calls++; _order = _order * 10 + 2; _state = state; }

    private static void ActionThrow(object? state) { ActionOne(state); throw _marker!; }

    private static void TokenOne(object? state, CancellationToken token) { ActionOne(state); _token = token; }

    private static void TokenThrow(object? state, CancellationToken token) { ActionTwo(state); _token = token; throw _marker!; }

    private static void TokenThree(object? state, CancellationToken token) { _calls++; _order = _order * 10 + 3; _state = state; _token = token; }

    private static string Cancel(CancellationTokenSource source, bool throwOnFirst)
    {
        try
        {
            source.Cancel(throwOnFirst);
            return "returned";
        }
        catch (AggregateException error)
        {
            return "aggregate:" + error.InnerExceptions.Count + ":" + ReferenceEquals(error.InnerExceptions[0], _marker);
        }
        catch (Exception error)
        {
            return ReferenceEquals(error, _marker) ? "marker" : error.GetType().Name;
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== Cancellation dispatch ==");
        var state = new object();
        Reset();
        var source = new CancellationTokenSource();
        source.Token.Register((Action<object?>)Delegate.Combine(new Action<object?>(ActionOne), new Action<object?>(ActionTwo))!, state);
        Console.WriteLine("action multicast=" + Cancel(source, false) + Effects(state));

        Reset();
        _marker = new InvalidOperationException("action marker");
        source = new CancellationTokenSource();
        source.Token.Register(ActionThrow, state);
        Console.WriteLine("action throw first=" + Cancel(source, true) + Effects(state));

        Reset();
        _marker = new InvalidOperationException("multicast marker");
        source = new CancellationTokenSource();
        var callbacks = (Action<object?, CancellationToken>)Delegate.Combine(new Action<object?, CancellationToken>(TokenOne),
            new Action<object?, CancellationToken>(TokenThrow), new Action<object?, CancellationToken>(TokenThree))!;
        source.Token.Register(callbacks, state);
        Console.WriteLine("token multicast throw=" + Cancel(source, false) + Effects(state) + Token(source.Token));

        Reset();
        var canceled = new CancellationTokenSource();
        canceled.Cancel();
        canceled.Token.Register(TokenOne, null);
        Console.WriteLine("token canceled=" + Effects(null) + Token(canceled.Token)
            + " canonical=" + _token.Equals(new CancellationToken(true)));

        var canonical = new CancellationToken(true);
        GC.Collect();
        Console.WriteLine("canonical after GC=" + canonical.Equals(new CancellationToken(true))
            + ":" + canonical.Equals(canceled.Token));
        Console.WriteLine("Cancellation dispatch end");
    }

    private static CancellationToken[] _coldTokens = Array.Empty<CancellationToken>();
    private static int _coldReady;
    private static int _coldStart;

    private static void ColdWorker(object? argument)
    {
        int index = (int)argument!;
        Interlocked.Increment(ref _coldReady);
        while (Volatile.Read(ref _coldStart) == 0) Thread.Yield();
        _coldTokens[index] = new CancellationToken(true);
    }

    // Runs in a fresh process: no canceled token exists before the threads race.
    internal static void RunColdFirstUse()
    {
        _coldTokens = new CancellationToken[4];
        var workers = new Thread[4];
        for (int index = 0; index < workers.Length; index++)
        {
            workers[index] = new Thread(ColdWorker);
            workers[index].Start(index);
        }
        while (Volatile.Read(ref _coldReady) != workers.Length) Thread.Yield();
        Volatile.Write(ref _coldStart, 1);
        foreach (var worker in workers) worker.Join();
        bool same = true;
        foreach (var token in _coldTokens)
            same &= token.CanBeCanceled && token.IsCancellationRequested && token.Equals(_coldTokens[0]);
        Console.WriteLine("cold first use=" + same + ":" + _coldTokens[0].Equals(new CancellationToken(true))
            + ":" + new CancellationToken(false).Equals(default(CancellationToken)));
    }
}
