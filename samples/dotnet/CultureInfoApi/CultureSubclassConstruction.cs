using System;
using System.Globalization;

namespace CultureInfoApi;

internal sealed class DerivedCulture : CultureInfo
{
    public DerivedCulture(string name) : base(name) { }
    public DerivedCulture(string name, bool useUserOverride) : base(name, useUserOverride) { }
    public DerivedCulture(int culture) : base(culture) { }
    public DerivedCulture(int culture, bool useUserOverride) : base(culture, useUserOverride) { }
}

internal static class CultureSubclassConstruction
{
    private static void Construct(string label, Func<DerivedCulture> create)
    {
        try
        {
            var culture = create();
            Console.WriteLine("culture subclass " + label + "=" + culture.Name);
        }
        catch (PlatformNotSupportedException error)
        {
            Console.WriteLine("culture subclass " + label + "=" + error.GetType().Name + ":" + error.Message);
        }
    }

    public static void Run()
    {
        Console.WriteLine("== culture subclass construction ==");
        Construct("string", () => new DerivedCulture(""));
        Construct("string/bool", () => new DerivedCulture("", false));
        Construct("int", () => new DerivedCulture(127));
        Construct("int/bool", () => new DerivedCulture(127, false));
        Console.WriteLine("culture subclass construction end");
    }
}
