using System;
using System.Globalization;

namespace GeneralArgumentFallback;

internal static class Program
{
    private static void Bound(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine(label + ": no exception");
        }
        catch (Exception exception)
        {
            GC.Collect();
            Console.WriteLine(label + ": " + exception.Message.Replace("\r", "").Replace("\n", "|"));
        }
    }

    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("-- general BCL Message fallback --");
        Bound("general clamp signed", () => Math.Clamp(0, int.MaxValue, int.MinValue));
        Bound("general clamp unsigned", () => Math.Clamp(0u, uint.MaxValue, 0u));
        Bound("general clamp float", () => Math.Clamp(0f, 1.25f, -2.5f));
        Bound("general round mode", () => Math.Round(1.25, (MidpointRounding)5));
        Bound("general round digits", () => Math.Round(1.25, 16, (MidpointRounding)5));
        Bound("general decimal digits", () => decimal.Round(1.25m, 29));
        Bound("general parse byte overflow", () => byte.Parse("99999999999999999999999"));
        Bound("general parse short overflow", () => short.Parse("-99999999999999999999999"));
        Bound("general parse style", () => int.Parse("1", (NumberStyles)0x800));
        Bound("general marshal null", () => System.Runtime.InteropServices.Marshal.SizeOf((Type)null));
        Bound("general native resolver null", () => System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(null, null));
        var collection = new System.Collections.Concurrent.BlockingCollection<int>(1);
        Bound("general collection timeout", () => collection.TryAdd(1, -2));
        collection.CompleteAdding();
        Bound("general collection completed", () => collection.Take());
        collection.Dispose();
        Bound("general collection disposed", () => Console.WriteLine(collection.Count));
        Console.WriteLine("general BCL Message fallback end");
        if (args.Length > 0 && args[0] == "before-decimal-fault-text")
            return;
        Console.WriteLine("-- decimal parse Message fallback --");
        foreach (string text in new[] { "", "漢\0\ud800" })
        {
            Bound("decimal string", () => decimal.Parse(text));
            Bound("decimal chars", () => decimal.Parse(text.AsSpan(), CultureInfo.InvariantCulture));
            byte[] utf8 = text.Length == 0 ? Array.Empty<byte>() : new byte[] { 0xe6, 0xbc, 0xa2, 0, 0xef, 0xbf, 0xbd };
            Bound("decimal utf8", () => decimal.Parse(utf8.AsSpan(), CultureInfo.InvariantCulture));
        }
        Console.WriteLine("decimal parse Message fallback end");
    }
}
