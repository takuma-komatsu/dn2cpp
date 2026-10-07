using System;
using System.Globalization;

static class ArrayStaticRankOneOnlyProgram
{
    static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== static rank1 MD identity ==");
        Type literal = ArrayRankOneOwner.ArrayType();
        Type field = typeof(ArrayRankOneOwner).GetField("Value")!.FieldType;
        Array bounded = Array.CreateInstance(typeof(int), new[] { 1 }, new[] { 5 });
        Array zero = Array.CreateInstance(typeof(int), new[] { 1 }, new[] { 0 });
        bounded.SetValue(23, 5);
        Console.WriteLine($"static={literal}/{literal.Name}/{literal.IsSZArray}/{literal.GetArrayRank()}");
        Console.WriteLine($"identity={literal == field}/{literal == bounded.GetType()}/{literal == zero.GetType()}");
        Console.WriteLine($"static clone={((Array)bounded.Clone()).GetType() == literal}/{bounded.GetValue(5)}");
        Console.WriteLine("static rank1 MD identity end");
    }
}
