using System;
using System.Collections.Concurrent;

internal static class BlockingCollectionDisposalSubset
{
    static void DisposedFault(string label, Action run)
    {
        try
        {
            run();
            Console.WriteLine(label + " -> returned");
        }
        catch (Exception ex)
        {
            string name = ex is ObjectDisposedException disposed
                ? " [" + disposed.ObjectName + "]"
                : ex is ArgumentException arg ? " [" + arg.ParamName + "]" : "";
            Console.WriteLine(label + " -> " + ex.GetType().Name + ": " + ex.Message + name);
        }
    }

    internal static void RunChecks()
    {
        Console.WriteLine("== dispose checks ==");
        var q = new BlockingCollection<int>(2);
        q.Add(1);
        q.Dispose();
        DisposedFault("Dispose twice", () => q.Dispose());
        DisposedFault("Count", () => { _ = q.Count; });
        DisposedFault("BoundedCapacity", () => { _ = q.BoundedCapacity; });
        DisposedFault("IsAddingCompleted", () => { _ = q.IsAddingCompleted; });
        DisposedFault("IsCompleted", () => { _ = q.IsCompleted; });
        DisposedFault("Add", () => q.Add(2));
        DisposedFault("TryAdd", () => { _ = q.TryAdd(2); });
        DisposedFault("TryAdd(-2)", () => { _ = q.TryAdd(2, -2); });
        DisposedFault("Take", () => { _ = q.Take(); });
        DisposedFault("TryTake", () => { _ = q.TryTake(out _); });
        DisposedFault("TryTake(-2)", () => { _ = q.TryTake(out _, -2); });
        DisposedFault("CompleteAdding", () => q.CompleteAdding());
        IDisposable viaInterface = new BlockingCollection<int>();
        viaInterface.Dispose();
        DisposedFault("interface Dispose then Count", () =>
        {
            _ = ((BlockingCollection<int>)viaInterface).Count;
        });
        var intArrays = new BlockingCollection<int[]>();
        intArrays.Dispose();
        DisposedFault("int[] Count", () => { _ = intArrays.Count; });
        var stringArrays = new BlockingCollection<string[]>();
        stringArrays.Dispose();
        DisposedFault("string[] Count", () => { _ = stringArrays.Count; });
        Console.WriteLine("dispose checks end");
    }
}
