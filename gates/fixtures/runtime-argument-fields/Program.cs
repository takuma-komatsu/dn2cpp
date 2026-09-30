using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RuntimeArgumentFields;

// No ArgumentException-family constructor may supply the layout these traps need.
internal static class Program
{
    private static void Probe(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine(label + ": success");
        }
        catch (ArgumentException exception)
        {
            Console.WriteLine(label + ": " + exception.GetType().Name + ":" + (exception.ParamName ?? "<null>"));
            if (exception is ArgumentOutOfRangeException range)
                Console.WriteLine("actual=" + (range.ActualValue ?? "<null>") + ":" + (range.ActualValue?.GetType().Name ?? "<null>"));
            Console.WriteLine(exception.Message.Replace("\r", "").Replace("\n", "|"));
            Console.WriteLine(((Exception)exception).Message == exception.Message);
        }
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("-- argument fields without constructors --");
        Probe("substring negative start", () => "abc".Substring(-1));
        Probe("substring start", () => "abc".Substring(5));
        Probe("substring start/length", () => "abc".Substring(5, -1));
        Probe("substring negative length", () => "abc".Substring(1, -1));
        Probe("string count", () => new string('a', -1));
        Probe("builder count", () => new StringBuilder().Append('a', -1));
        Probe("builder start", () => new StringBuilder().Append("a", -1, 0));
        Probe("builder range", () => new StringBuilder().Append("a", 1, 1));
        Probe("builder null", () => new StringBuilder().Append((string)null, 0, 1));
        Probe("join null", () => string.Join(",", (string[])null));
        Probe("dictionary null", () => new Dictionary<string, int>().Add(null, 1));
        Probe("list index", () => new List<int>().RemoveAt(3));
        Probe("ilist null", () => ((IList)new List<int>()).Add(null));
    }
}
