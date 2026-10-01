using System;
using System.Threading;

internal static class ParallelOptionsValidationSubset
{
    public static void __GateEntry()
    {
        Console.WriteLine("== ParallelOptions fields ==");
        int[] ints = { int.MinValue, -2, -1, 0, 1, int.MaxValue };
        foreach (int dop in ints) { var options = new System.Threading.Tasks.ParallelOptions(); O("dop:" + dop, () => { options.MaxDegreeOfParallelism = dop; return options.MaxDegreeOfParallelism.ToString(); }); }
        for (int state = 0; state < 2; state++)
        {
            System.Threading.Tasks.ParallelOptions? options = state == 0 ? null : new System.Threading.Tasks.ParallelOptions();
            O("dop getter:" + state, () => options!.MaxDegreeOfParallelism.ToString());
            foreach (int dop in ints)
                O("dop receiver:" + state + ":" + dop, () => { options!.MaxDegreeOfParallelism = dop; return options.MaxDegreeOfParallelism.ToString(); });
        }
        Exception? saved = null;
        try { new System.Threading.Tasks.ParallelOptions().MaxDegreeOfParallelism = 0; } catch (Exception ex) { saved = ex; }
        GC.Collect(); GC.WaitForPendingFinalizers();
        Fault("parallel options fields GC", saved!);
        Console.WriteLine("ParallelOptions fields end");
    }

    private static void Fault(string label, Exception ex)
    {
        Console.WriteLine(label + " type=" + ex.GetType().Name);
        Console.WriteLine(label + " param=" + (ex is ArgumentException a ? a.ParamName : null));
        Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
        object? value = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
        Console.WriteLine(label + " actual=" + (value is null ? "null" : value.GetType().Name + ":" + value));
    }

    private static void O(string label, Func<string> action)
    {
        try
        {
            Console.WriteLine(label + " success=" + action());
        }
        catch (Exception ex)
        {
            Fault(label, ex);
        }
    }
}
