using System;
using System.Collections.Generic;
using System.Threading;

// `System.Threading.Lock`: `lock (Lock)` lowers to EnterScope()/finally Scope.Dispose()
// rather than Monitor.Enter/Exit, a second lowering of the same statement. Covers the
// `using` form, explicit TryEnter/Enter/Exit, nesting, and the throwing body.
namespace LockTypeSubset;

internal static class Program
{
    static readonly Lock _lock = new Lock();
    static readonly Dictionary<string, int> _counts = new();
    static int _sum;

    static void Add(int x)
    {
        lock (_lock) { _sum += x; }
    }

    // A lock around a value-returning body.
    static int Bump(string key)
    {
        lock (_lock)
        {
            _counts.TryGetValue(key, out int c);
            c++;
            _counts[key] = c;
            return c;
        }
    }

    internal static void __GateEntry()
    {
        Add(5);
        Add(3);
        Add(10);
        Console.WriteLine(_sum);              // 18

        Console.WriteLine(Bump("a"));         // 1
        Console.WriteLine(Bump("a"));         // 2
        Console.WriteLine(Bump("b"));         // 1
        Console.WriteLine(_counts["a"]);      // 2

        // Explicit EnterScope()/Dispose() (what `lock (Lock)` lowers to).
        Lock.Scope scope = _lock.EnterScope();
        try { _sum += 100; }
        finally { scope.Dispose(); }
        Console.WriteLine(_sum);              // 118

        // The `using`-statement form (EnterScope() bound to a using var -> Dispose).
        using (_lock.EnterScope())
        {
            _sum += 1;
        }
        Console.WriteLine(_sum);              // 119

        // TryEnter / Enter / Exit explicit forms.
        if (_lock.TryEnter())
        {
            try { _sum += 1; }
            finally { _lock.Exit(); }
        }
        Console.WriteLine(_sum);              // 120

        _lock.Enter();
        try { _sum += 5; }
        finally { _lock.Exit(); }
        Console.WriteLine(_sum);              // 125

        // Nested locks on the same Lock (both no-ops).
        lock (_lock) { lock (_lock) { _sum *= 2; } }
        Console.WriteLine(_sum);              // 250

        // A lock body that throws still releases via the finally (no deadlock).
        try { lock (_lock) { throw new InvalidOperationException("x"); } }
        catch (InvalidOperationException) { Console.WriteLine("caught"); }
        lock (_lock) { _sum += 7; }
        Console.WriteLine(_sum);              // 257
    }
}
