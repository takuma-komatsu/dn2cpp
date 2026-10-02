using System;
using System.Collections.Concurrent;
using System.Globalization;

internal static class Program
{
    private static Exception? s_kept;

    private static void Print(string label, Exception error)
    {
        Console.WriteLine(label + "|" + error.GetType().Name + "|"
            + error.Message.Replace(Environment.NewLine, "|"));
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var collection = new BlockingCollection<int>();
        collection.Dispose();
        try
        {
            Console.WriteLine(collection.Count);
        }
        catch (Exception error)
        {
            s_kept = error;
            Print("disposed", error);
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Print("retained", s_kept!);
        Console.WriteLine("blocking disposal message end");
    }
}
