using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ILDietLookupOnly;

internal static class StateMachineMethods
{
    public static void Run()
    {
        Console.WriteLine("== signature-only state machines ==");
        Console.WriteLine("async metadata=" + (typeof(StateMachineMethods).GetMethod(nameof(UncalledAsync)) is not null));
        Console.WriteLine("iterator metadata=" + (typeof(StateMachineMethods).GetMethod(nameof(UncalledIterator)) is not null));
        Console.WriteLine("async iterator metadata=" + (typeof(StateMachineMethods).GetMethod(nameof(UncalledAsyncIterator)) is not null));
        Console.WriteLine("async called=" + CalledAsync().GetAwaiter().GetResult());
        foreach (int value in CalledIterator())
            Console.WriteLine("iterator called=" + value);
        Console.WriteLine("promoted machine=" + CalledRegistration());
        Console.WriteLine("signature-only state machines end");
    }

    public static async Task<int> UncalledAsync()
    {
        await Task.CompletedTask;
        return AsyncBodyOnlyDependency.Read();
    }

    public static IEnumerable<int> UncalledIterator()
    {
        yield return IteratorBodyOnlyDependency.Read();
    }

    public static async IAsyncEnumerable<int> UncalledAsyncIterator()
    {
        await Task.CompletedTask;
        yield return AsyncIteratorBodyOnlyDependency.Read();
    }

    public static async Task<int> CalledAsync()
    {
        await Task.CompletedTask;
        return CalledAsyncDependency.Read();
    }

    public static IEnumerable<int> CalledIterator()
    {
        yield return CalledIteratorDependency.Read();
    }

    // A later executable attribute root must upgrade the signature-only machine.
    [AsyncStateMachine(typeof(PromotedStateMachine))]
    public static void LateRegistration() { }

    [AsyncStateMachine(typeof(PromotedStateMachine))]
    public static int CalledRegistration() => 9;

    [ScalarType(typeof(ScalarAttributePayload))]
    public static void UncalledAttributed() { }
}

internal sealed class PromotedStateMachine : IAsyncStateMachine
{
    public int Value;

    public void MoveNext() => Value = PromotedMachineDependency.Read();

    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
}

internal sealed class ScalarTypeAttribute : Attribute
{
    public ScalarTypeAttribute(Type selected) { }
}

internal sealed class ScalarAttributePayload
{
    public override string ToString() => ScalarAttributeDependency.Read();
}

internal static class AsyncBodyOnlyDependency { public static int Read() => 71; }
internal static class IteratorBodyOnlyDependency { public static int Read() => 81; }
internal static class AsyncIteratorBodyOnlyDependency { public static int Read() => 91; }
internal static class CalledAsyncDependency { public static int Read() => 7; }
internal static class CalledIteratorDependency { public static int Read() => 8; }
internal static class PromotedMachineDependency { public static int Read() => 9; }
internal static class ScalarAttributeDependency { public static string Read() => "scalar attribute body"; }
