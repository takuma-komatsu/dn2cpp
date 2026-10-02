using System;
using System.Globalization;
using System.Resources;

namespace ResourceReceiverSubset;

internal static class Program
{
    private static ResourceManager s_manager = null!;

    private static void Fault(string label, Func<object?> action)
    {
        string result;
        try
        {
            result = action()?.ToString() ?? "null";
        }
        catch (Exception ex)
        {
            result = ex.GetType().Name;
        }
        Console.WriteLine(label + " -> " + result);
    }

    private static void Run(string label, Action action) => Fault(label, () =>
    {
        action();
        return "returned";
    });

    internal static void Run()
    {
        Console.WriteLine("== resource manager receivers ==");
        CultureInfo invariant = CultureInfo.InvariantCulture;
        Fault("GetString(name)", () => s_manager.GetString("Greeting"));
        Fault("GetString(null)", () => s_manager.GetString(null!));
        Fault("GetString(name, culture)", () => s_manager.GetString("Greeting", invariant));
        Fault("GetObject(name)", () => s_manager.GetObject("Greeting"));
        Fault("GetObject(null, culture)", () => s_manager.GetObject(null!, invariant));
        Fault("BaseName", () => s_manager.BaseName);
        Run("ReleaseAllResources()", () => s_manager.ReleaseAllResources());
        Fault("GetStream(name)", () => s_manager.GetStream("Greeting"));
        Fault("GetStream(null, culture)", () => s_manager.GetStream(null!, invariant));
        Console.WriteLine("resource manager receivers end");
    }
}
