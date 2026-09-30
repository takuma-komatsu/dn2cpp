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

    private static void Bound(string label, Action body)
    {
        try
        {
            body();
        }
        catch (Exception exception)
        {
            GC.Collect();
            Console.WriteLine(label + ": " + Show(exception.Message));
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
        Console.WriteLine("-- bound Message fallback --");
        Bound("insert unsigned", () => "abc".Insert(-1, "x"));
        Bound("insert minimum", () => "abc".Insert(int.MinValue, "x"));
        Bound("array unsigned", () => "abc".ToCharArray(-1, -1));
        Bound("array signed room", () => "abc".ToCharArray(1, 4));
        Bound("array wrapped room", () => "abc".ToCharArray(0, int.MinValue));
        Bound("array negative length", () => "abc".ToCharArray(0, -1));
        Bound("copy source room", () => "abcd".CopyTo(1, new char[4], 0, 4));
        Bound("copy destination room", () => "abcd".CopyTo(0, Array.Empty<char>(), -1, 4));
        Console.WriteLine("bound Message fallback end");
    }
}
