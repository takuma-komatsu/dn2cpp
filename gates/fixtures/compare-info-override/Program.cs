using System;
using System.Globalization;
using System.Runtime.CompilerServices;

// An allocated CultureInfo subclass overriding CompareInfo, reached through a
// virtual CultureInfo.CompareInfo call. The transpiler refuses the dispatch.
// The instance skips the base constructor, which has no native mapping.
internal sealed class OverridingCulture : CultureInfo
{
    public OverridingCulture() : base("") { }
    public override CompareInfo CompareInfo => InvariantCulture.CompareInfo;
}

internal static class Program
{
    private static void Main()
    {
        var culture = (CultureInfo)RuntimeHelpers.GetUninitializedObject(typeof(OverridingCulture));
        Console.WriteLine(culture.CompareInfo.Name);
    }
}
