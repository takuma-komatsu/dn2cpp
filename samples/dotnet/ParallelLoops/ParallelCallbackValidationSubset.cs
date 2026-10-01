using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal static class ParallelCallbackValidationSubset
{
    static ParallelLoopState? s_state = null;
    static ParallelOptions? s_options = null;
    static int[]? s_nullInts = null;
    static string[]? s_nullStrings = null;
    static List<int>? s_nullList = null;
    static int[] s_ints = { 1, 2, 3 };
    static long s_sum;

    static bool SharedNullArray<T>() where T : class =>
        Parallel.ForEach((T[])null!, _ => { }).IsCompleted;

    static void Fault(string label, Func<object> run)
    {
        string text;
        try
        {
            text = run().ToString()!;
        }
        catch (Exception ex)
        {
            string param = ex is ArgumentException arg ? " [" + (arg.ParamName ?? "null") + "]" : "";
            text = ex.GetType().Name + ": " + ex.Message + param;
        }
        Console.WriteLine(label + " -> " + text);
    }

    static void Run(string label, Action run) => Fault(label, () =>
    {
        run();
        return "returned";
    });

    // A null ParallelLoopState or ParallelOptions receiver faults ahead of the setter's
    // range checks. Parallel.For, ForEach and Invoke refuse a null source, options,
    // body or actions array in .NET's order, before any iteration, even over an empty
    // range, and a null action before any action runs. A non-capturing body, whose
    // cached delegate branches around the source operand, still runs over an array.
    internal static void RunChecks()
    {
        Console.WriteLine("== argument checks ==");
        Run("ParallelLoopState.Break()", () => s_state!.Break());
        Run("ParallelLoopState.Stop()", () => s_state!.Stop());
        Fault("ParallelLoopState.ShouldExitCurrentIteration", () => s_state!.ShouldExitCurrentIteration);
        Fault("ParallelLoopState.IsStopped", () => s_state!.IsStopped);
        Fault("ParallelLoopState.IsExceptional", () => s_state!.IsExceptional);
        Fault("ParallelLoopState.LowestBreakIteration", () => s_state!.LowestBreakIteration ?? -1L);
        Fault("ParallelOptions.MaxDegreeOfParallelism", () => s_options!.MaxDegreeOfParallelism);
        Run("ParallelOptions.MaxDegreeOfParallelism=0", () => s_options!.MaxDegreeOfParallelism = 0);
        Run("ParallelOptions.MaxDegreeOfParallelism=-2", () => s_options!.MaxDegreeOfParallelism = -2);
        Run("ParallelOptions.MaxDegreeOfParallelism=1", () => s_options!.MaxDegreeOfParallelism = 1);

        var opts = new ParallelOptions();
        int[] arr = { 1, 2 };
        var list = new List<int> { 1, 2 };
        int ran = 0;
        Fault("For(null body)", () => Parallel.For(0, 10, (Action<int>)null!).IsCompleted);
        Fault("For(empty, null body)", () => Parallel.For(0, 0, (Action<int>)null!).IsCompleted);
        Fault("For(long, null body)", () => Parallel.For(0L, 10L, (Action<long>)null!).IsCompleted);
        Fault("For(null state body)", () => Parallel.For(0, 10, (Action<int, ParallelLoopState>)null!).IsCompleted);
        Fault("For(null options)", () => Parallel.For(0, 10, s_options!, i => { }).IsCompleted);
        Fault("For(null options, null body)", () => Parallel.For(0, 10, s_options!, (Action<int>)null!).IsCompleted);
        Fault("For(long, null options)", () => Parallel.For(0L, 10L, s_options!, i => { }).IsCompleted);
        Fault("For(options, null body)", () => Parallel.For(0, 10, opts, (Action<int>)null!).IsCompleted);
        Fault("ForEach(null array)", () => Parallel.ForEach(s_nullInts!, n => { }).IsCompleted);
        Fault("ForEach(null list)", () => Parallel.ForEach(s_nullList!, n => { }).IsCompleted);
        Fault("ForEach(null string array)", () => Parallel.ForEach(s_nullStrings!, w => { }).IsCompleted);
        Fault("ForEach(literal null array)", () => Parallel.ForEach((int[])null!, n => { }).IsCompleted);
        Fault("ForEach(literal null list)", () => Parallel.ForEach((List<int>)null!, n => { }).IsCompleted);
        Fault("ForEach(literal null enumerable)",
            () => Parallel.ForEach((IEnumerable<int>)null!, n => { }).IsCompleted);
        Fault("ForEach(literal null, null options, null body)",
            () => Parallel.ForEach((int[])null!, s_options!, (Action<int>)null!).IsCompleted);
        Fault("ForEach(shared null string)", () => SharedNullArray<string>());
        Fault("ForEach(shared null object)", () => SharedNullArray<object>());
        Fault("ForEach(array, null body)", () => Parallel.ForEach(arr, (Action<int>)null!).IsCompleted);
        Fault("ForEach(list, null body)", () => Parallel.ForEach(list, (Action<int>)null!).IsCompleted);
        Fault("ForEach(array, null state body)",
            () => Parallel.ForEach(arr, (Action<int, ParallelLoopState>)null!).IsCompleted);
        Fault("ForEach(null, null)", () => Parallel.ForEach(s_nullInts!, (Action<int>)null!).IsCompleted);
        Fault("ForEach(array, null options)", () => Parallel.ForEach(arr, s_options!, n => { }).IsCompleted);
        Fault("ForEach(array, null options, null body)",
            () => Parallel.ForEach(arr, s_options!, (Action<int>)null!).IsCompleted);
        Fault("ForEach(null, null options, body)",
            () => Parallel.ForEach(s_nullInts!, s_options!, n => { }).IsCompleted);
        Fault("ForEach(list, null options)", () => Parallel.ForEach(list, s_options!, n => { }).IsCompleted);
        Run("Invoke(null)", () => Parallel.Invoke((Action[])null!));
        Run("Invoke(null element)", () => Parallel.Invoke(
            () => Interlocked.Increment(ref ran), null!, () => Interlocked.Increment(ref ran)));
        Run("Invoke(empty)", () => Parallel.Invoke());
        Run("Invoke(null options)", () => Parallel.Invoke(s_options!, () => Interlocked.Increment(ref ran)));
        Run("Invoke(null options, null)", () => Parallel.Invoke(s_options!, (Action[])null!));
        Run("Invoke(null options, null element)", () => Parallel.Invoke(s_options!, new Action[] { null! }));
        Run("Invoke(options, null element)", () => Parallel.Invoke(opts, new Action[] { null! }));
        Console.WriteLine("actions run: " + ran);
        Fault("ForEach(array, cached body)", () =>
            Parallel.ForEach(s_ints, n => Interlocked.Add(ref s_sum, n)).IsCompleted + " " + s_sum);
        Console.WriteLine("argument checks end");
    }
}
