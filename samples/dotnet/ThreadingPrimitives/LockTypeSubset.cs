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

    private static T FromObject<T>(object? value) => (T)value!;

    private static object? ToObject<T>(T value) => value;

    private static Type GenericType<T>() => typeof(T);

#pragma warning disable CS9216 // These identity round trips deliberately pass Lock through Object.
    private static string RoundTrip(object? value, bool generic)
    {
        try
        {
            Lock? restored = generic ? FromObject<Lock?>(value) : (Lock?)value;
            return ReferenceEquals(value, restored).ToString();
        }
        catch (InvalidCastException)
        {
            return "InvalidCastException";
        }
    }

    internal static void RunIdentity()
    {
        Console.WriteLine("== Lock runtime identity ==");
        var value = new Lock();
        object boxed = value;
        object genericBox = ToObject(value)!;
        object wrong = new object();
        Type actual = value.GetType();
        Console.WriteLine("Lock type: " + actual.Name + " " + boxed.GetType().FullName
            + " " + genericBox.GetType().FullName);
        Console.WriteLine("Lock reflection: " + (actual == typeof(Lock)) + " "
            + (GenericType<Lock>() == typeof(Lock)) + " " + typeof(Lock).IsAssignableFrom(actual)
            + " " + actual.IsSealed + " " + (actual.BaseType is null ? "null" : actual.BaseType.FullName));
        Console.WriteLine("Lock object tests: " + (boxed is Lock) + " " + (genericBox is Lock)
            + " " + (wrong is Lock));
        Console.WriteLine("Lock typed round trip: " + RoundTrip(boxed, false) + " "
            + RoundTrip(null, false) + " " + RoundTrip(wrong, false));
        Console.WriteLine("Lock shared round trip: " + RoundTrip(genericBox, true) + " "
            + RoundTrip(null, true) + " " + RoundTrip(wrong, true));
        FromObject<object>(wrong);
        Console.WriteLine("Lock runtime identity end");
    }
#pragma warning restore CS9216
}
