using System;
using System.Collections.Generic;
using System.Threading;

// The `lock` statement over a plain `object` monitor and what it lowers to
// (Monitor.Enter(ref taken) / finally Exit), plus the explicit TryEnter forms, re-entrant
// nesting, and the throwing body that must still release. Single-threaded.
namespace LockSubset;

internal static class Program
{
    static readonly object _lock = new object();
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

        // Explicit Monitor.Enter(ref taken)/Exit (what `lock` lowers to).
        bool taken = false;
        Monitor.Enter(_lock, ref taken);
        try { _sum += 100; }
        finally { if (taken) Monitor.Exit(_lock); }
        Console.WriteLine(taken);             // True
        Console.WriteLine(_sum);              // 118

        // Monitor.TryEnter (bool-returning) and the ref-bool form.
        if (Monitor.TryEnter(_lock))
        {
            try { _sum += 1; }
            finally { Monitor.Exit(_lock); }
        }
        Console.WriteLine(_sum);              // 119

        bool t2 = false;
        Monitor.TryEnter(_lock, 0, ref t2);
        try { if (t2) _sum += 1; }
        finally { if (t2) Monitor.Exit(_lock); }
        Console.WriteLine(t2 + "/" + _sum);   // True/120

        // Nested locks on the same object (re-entrant; both are no-ops).
        lock (_lock) { lock (_lock) { _sum *= 2; } }
        Console.WriteLine(_sum);              // 240

        // A lock body that throws still releases via the finally (no deadlock).
        try { lock (_lock) { throw new InvalidOperationException("x"); } }
        catch (InvalidOperationException) { Console.WriteLine("caught"); }
        lock (_lock) { _sum += 7; }
        Console.WriteLine(_sum);              // 247
    }
}
