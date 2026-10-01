using System;
using System.Collections.Concurrent;

internal static class BlockingCollectionValidationSubset
{
    static BlockingCollection<int>? s_null = null;

    static void Fault(string label, Func<object> run)
    {
        string text;
        try
        {
            text = run().ToString()!;
        }
        catch (Exception ex)
        {
            string actual = ex is ArgumentOutOfRangeException range
                ? " [" + range.ParamName + ", " + (range.ActualValue?.GetType().Name ?? "null") + "]"
                : "";
            text = ex.GetType().Name + ": " + ex.Message + actual;
        }
        Console.WriteLine(label + " -> " + text);
    }

    static void Run(string label, Action run) => Fault(label, () =>
    {
        run();
        return "returned";
    });

    // A null receiver faults ahead of the timeout check, which precedes the completed
    // and full verdicts; a capacity below 1 is refused; and a completed collection
    // refuses additions and a Take with .NET's texts.
    internal static void RunChecks()
    {
        Console.WriteLine("== argument checks ==");
        Run("null Add", () => s_null!.Add(1));
        Fault("null TryAdd(1)", () => s_null!.TryAdd(1));
        Fault("null TryAdd(1, -2)", () => s_null!.TryAdd(1, -2));
        Fault("null Take", () => s_null!.Take());
        Fault("null TryTake(out)", () => s_null!.TryTake(out _));
        Fault("null TryTake(out, -2)", () => s_null!.TryTake(out _, -2));
        Run("null CompleteAdding", () => s_null!.CompleteAdding());
        Fault("null Count", () => s_null!.Count);
        Fault("null IsAddingCompleted", () => s_null!.IsAddingCompleted);
        Fault("null IsCompleted", () => s_null!.IsCompleted);
        Fault("null BoundedCapacity", () => s_null!.BoundedCapacity);
        Run("null Dispose", () => s_null!.Dispose());

        Fault("new(0)", () => new BlockingCollection<int>(0));
        Fault("new(-1)", () => new BlockingCollection<int>(-1));
        Fault("new(int.MinValue)", () => new BlockingCollection<int>(int.MinValue));

        var q = new BlockingCollection<int>();
        Fault("TryAdd(1, -2)", () => q.TryAdd(1, -2));
        Fault("TryAdd(1, int.MinValue)", () => q.TryAdd(1, int.MinValue));
        Fault("TryAdd(1, -1)", () => q.TryAdd(1, -1));
        Fault("TryTake(out, -2)", () => q.TryTake(out _, -2));
        Fault("TryTake(out, -1)", () => q.TryTake(out int v, -1) + " " + v);
        q.CompleteAdding();
        Run("completed Add", () => q.Add(2));
        Fault("completed TryAdd(2)", () => q.TryAdd(2));
        Fault("completed TryAdd(2, 5)", () => q.TryAdd(2, 5));
        Fault("completed TryAdd(2, -2)", () => q.TryAdd(2, -2));
        Fault("completed Take", () => q.Take());
        Fault("completed TryTake(out)", () => q.TryTake(out _));
        Fault("completed TryTake(out, -2)", () => q.TryTake(out _, -2));

        var full = new BlockingCollection<string>(1);
        full.Add("x");
        Fault("full TryAdd(y, -2)", () => full.TryAdd("y", -2));
        Fault("full TryAdd(y, 0)", () => full.TryAdd("y", 0));
        Console.WriteLine("argument checks end");
    }
}
