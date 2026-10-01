using System;
using System.Threading;

internal static class WaitHandleValidationSubset
{
    public static void __GateEntry()
    {
        Console.WriteLine("== WaitHandle array fields ==");
        O("wait null", () => WaitHandle.WaitAny(null!).ToString());
        O("wait empty", () => WaitHandle.WaitAny(Array.Empty<WaitHandle>()).ToString());
        O("wait element", () => WaitHandle.WaitAny(new WaitHandle[] { new ManualResetEvent(true), null! }).ToString());
        Exception? saved = null;
        try { WaitHandle.WaitAny(Array.Empty<WaitHandle>()); } catch (Exception ex) { saved = ex; }
        GC.Collect(); GC.WaitForPendingFinalizers();
        Fault("wait fields GC", saved!);
        Console.WriteLine("WaitHandle array fields end");
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
