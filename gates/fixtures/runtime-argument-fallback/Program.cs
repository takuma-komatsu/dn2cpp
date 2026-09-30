using System;
using System.Globalization;

namespace RuntimeArgumentFallback;

// Reading only Exception.Message leaves the argument-specific layout and override unused.
internal static class Program
{
    private static string Show(string text) => text.Replace("\0", "<nul>")
        .Replace("\ud800", "<sur>").Replace("\r", "").Replace("\n", "|");

    private static void Probe(string value, string name)
    {
        try
        {
            Console.WriteLine(System.ThrowHelper.IfNullOrWhitespace(value, name));
        }
        catch (Exception exception)
        {
            Console.WriteLine(Show(exception.Message));
            Console.WriteLine(exception.Message.Length);
        }
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("-- argument Message fallback --");
        string[] names = { "a\0b", "a\ud800b", "", null, "日本語" };
        foreach (string name in names)
        {
            Probe(null, name);
            Probe(" ", name);
        }
    }
}
