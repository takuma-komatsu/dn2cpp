using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PrimitiveWideningSubset;

internal static class Program
{
    private static void Check(string label, bool value)
    {
        Console.WriteLine("primitive-widening-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Primitive widening contract ==");
        int[] integers = new int[1];
        ((Array)integers).SetValue((byte)7, 0);
        Check("public-widen", integers[0] == 7);
        ((Array)integers).SetValue(9, 0);
        Check("public-identity", integers[0] == 9);
        bool refused = false;
        try { ((Array)integers).SetValue(3L, 0); }
        catch (ArgumentException) { refused = true; }
        catch (InvalidCastException) { refused = true; }
        Check("public-reject", refused && integers[0] == 9);
        Console.WriteLine("Primitive widening contract end");
    }
}
