using System;
using System.Collections.Concurrent;

namespace CustomAsyncTaskLib;

/// <summary>GDTask's <c>GDTaskScheduler</c> shape — a <c>ConcurrentDictionary</c> keyed by the
/// engine loop a runner belongs to. It is here so the gate's tree-shake assertion covers this
/// shape too. Its counters are never
/// printed: they are live in real .NET and dead natively, so they would diverge by design.</summary>
public static class CustomTaskScheduler
{
    private static readonly ConcurrentDictionary<Type, int> s_rentsByStateMachine = new();

    internal static void NoteRent(Type stateMachine) =>
        s_rentsByStateMachine.AddOrUpdate(stateMachine, 1, static (_, n) => n + 1);

    /// <summary>Total runners rented. Diagnostic only — never asserted on (see above).</summary>
    public static int TotalRents
    {
        get
        {
            int total = 0;
            foreach (var kv in s_rentsByStateMachine)
                total += kv.Value;
            return total;
        }
    }
}
