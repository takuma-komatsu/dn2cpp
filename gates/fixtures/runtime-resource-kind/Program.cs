using System;
using System.Globalization;
using System.Resources;

namespace RuntimeResourceKind;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var manager = new ResourceManager("RuntimeResourceKind.Mixed", typeof(Program).Assembly);
        DateTime decoded = (DateTime)manager.GetObject("p-datetime-local");
        GC.Collect();
        Console.WriteLine("resource local kind=" + decoded.Kind);
        Console.WriteLine("resource local UTC ticks=" + decoded.ToUniversalTime().Ticks);
        Console.WriteLine("resource local kind end");
    }
}
