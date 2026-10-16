using System;
using System.Globalization;
using System.Reflection;

namespace ILDietInheritedStaticOnly;

// Only a FlattenHierarchy field read on AppStaticDerived, named by a constant string,
// reaches the base's static field, whose initializer is not beforefieldinit.
internal class AppStaticBase
{
    public static int Seed = Initial();

    static AppStaticBase()
    {
    }

    private static int Initial() => 41;
}

internal sealed class AppStaticDerived : AppStaticBase { }

// No token, allocation, constructor lookup or nameless member selection reaches either
// derived owner, so only the field read can retain its base's initializer.
internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== inherited static fields ==");
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        Type library = Type.GetType("ILDietControlLib.InheritedStaticDerived, ILDietControlLib")!;
        Console.WriteLine("library=" + library.GetField("Seed", flags)!.GetValue(null));
        Type application = Type.GetType("ILDietInheritedStaticOnly.AppStaticDerived")!;
        Console.WriteLine("application=" + application.GetField("Seed", flags)!.GetValue(null));
        Console.WriteLine("inherited static fields end");
    }
}
