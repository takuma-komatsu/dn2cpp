using System;
using System.Globalization;

namespace CoreClrActivationInfoSubset;

public static class ActivationChecks
{
    public sealed class PublicValue { public int Value = 42; }
    enum Choice { One = 1, Two = 2 }
    static void Check(string label, bool value)
    {
        Console.WriteLine("activation-info-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }
    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== CoreCLR activation boundary public controls ==");
        Check("public-construction", Activator.CreateInstance<PublicValue>().Value == 42);
        Check("public-enum", Enum.GetName(Choice.Two) == "Two" && Enum.GetValues<Choice>().Length == 2);
        Console.WriteLine("CoreCLR activation boundary public controls end");
    }
}
