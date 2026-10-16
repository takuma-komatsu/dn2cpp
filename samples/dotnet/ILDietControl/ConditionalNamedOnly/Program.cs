using System;
using System.Globalization;
using System.Reflection;

namespace ILDietConditionalNamedOnly;

// Only a constant type name reaches it; the lookup observes Bar without running it.
internal sealed class NamedApplication
{
    public void Bar()
    {
    }
}

// A conditional descriptor on each constant-named type keeps its members, because the
// name uses the type. No site runs, binds or constructs anything, and the library
// method is looked up by a name no constant spells.
internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== conditional constant-named owners ==");
        Type application = Type.GetType("ILDietConditionalNamedOnly.NamedApplication")!;
        Console.WriteLine("application=" + application.GetMethod("Bar")!.Name);
        Type library = Type.GetType("ILDietControlLib.ConditionalNamedTarget, ILDietControlLib")!;
        string name = new string(new[] { 'R', 'u', 'n' });
        MethodInfo? run = library.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
        Console.WriteLine("library=" + (run is null ? "null" : run.Name));
        Console.WriteLine("conditional constant-named owners end");
    }
}
