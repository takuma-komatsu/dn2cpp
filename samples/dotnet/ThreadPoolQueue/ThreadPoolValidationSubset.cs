using System;
using System.Threading;

internal static class ThreadPoolValidationSubset
{
    public static void __GateEntry()
    {
        Console.WriteLine("== ThreadPool callback fields ==");
        O("pool plain", () => ThreadPool.QueueUserWorkItem(null!).ToString());
        O("pool state", () => ThreadPool.QueueUserWorkItem(null!, new object()).ToString());
        O("pool unsafe", () => ThreadPool.UnsafeQueueUserWorkItem((WaitCallback)null!, new object()).ToString());
        O("pool generic", () => ThreadPool.QueueUserWorkItem<object>(null!, new object(), true).ToString());
        O("pool unsafe generic", () => ThreadPool.UnsafeQueueUserWorkItem<object>(null!, new object(), false).ToString());
        O("pool item", () => ThreadPool.UnsafeQueueUserWorkItem((IThreadPoolWorkItem)null!, false).ToString());
        Exception? saved = null;
        try { ThreadPool.QueueUserWorkItem(null!); } catch (Exception ex) { saved = ex; }
        GC.Collect(); GC.WaitForPendingFinalizers();
        Fault("pool fields GC", saved!);
        Console.WriteLine("ThreadPool callback fields end");
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
