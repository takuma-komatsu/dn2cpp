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
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_UNSIZED_ARRAY") == "1") return;
        Console.WriteLine("== unsized rank1 array signatures ==");
        Check("Unsized", ArrayRankOneOwner.UnsizedType());
        Check("Sized", ArrayRankOneOwner.SizedType());
        Check("Vector", ArrayRankOneOwner.VectorType());
        Check("VectorOfUnsized", ArrayRankOneOwner.VectorOfUnsizedType());
        Check("UnsizedOfVector", ArrayRankOneOwner.UnsizedOfVectorType());
        Console.WriteLine("unsized rank1 array signatures end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_NESTED_MD_ARRAY") == "1") return;
        Console.WriteLine("== nested MD array identities ==");
        Type nested = ArrayRankOneOwner.UnsizedOfUnsizedType();
        Check("UnsizedOfUnsized", nested);
        Check("UnsizedOfRectangle", ArrayRankOneOwner.UnsizedOfRectangleType());
        Type triple = ArrayRankOneOwner.UnsizedOfUnsizedOfUnsizedType();
        Check("UnsizedOfUnsizedOfUnsized", triple);
        Console.WriteLine($"nested chain={nested.GetArrayRank()}/{nested.GetElementType()!.GetArrayRank()}/{nested.GetElementType() == literal}/{triple.GetElementType() == nested}");
        Array reflected = Array.CreateInstance(literal, new[] { 1 }, new[] { 5 });
        reflected.SetValue(bounded, 5);
        Console.WriteLine($"nested reflected={reflected.GetType() == nested}/{((Array)reflected.GetValue(5)!).GetValue(5)}");
        Console.WriteLine($"nested is={ArrayRankOneOwner.IsUnsizedOfUnsized(reflected)}/{ArrayRankOneOwner.IsUnsizedOfUnsized(bounded)}/{ArrayRankOneOwner.IsUnsizedOfUnsized(zero)}/{ArrayRankOneOwner.IsUnsizedOfUnsized(null)}/{ArrayRankOneOwner.IsUnsizedOfUnsized(Array.CreateInstance(typeof(int[]), new[] { 1 }, new[] { 5 }))}");
        Array wrongRank = Array.CreateInstance(typeof(int[,]), new[] { 1 }, new[] { 5 });
        Array wrongValue = Array.CreateInstance(typeof(long[]), new[] { 1 }, new[] { 5 });
        Console.WriteLine($"nested covariance rejection={ArrayRankOneOwner.IsUnsizedOfUnsized(wrongRank)}/{ArrayRankOneOwner.IsUnsizedOfUnsized(wrongValue)}");
        Console.WriteLine($"nested cast={ReferenceEquals(reflected, ArrayRankOneOwner.CastUnsizedOfUnsized(reflected))}/{ArrayRankOneOwner.CastUnsizedOfUnsized(null) is null}");
        try { ArrayRankOneOwner.CastUnsizedOfUnsized(bounded); }
        catch (InvalidCastException) { Console.WriteLine("nested cast mismatch=InvalidCastException"); }
        try { ArrayRankOneOwner.CastUnsizedOfUnsized(wrongValue); }
        catch (InvalidCastException) { Console.WriteLine("nested value cast mismatch=InvalidCastException"); }
        Array allocated = new int[1, 2][,];
        Type rectangular = typeof(int[,][,]);
        allocated.SetValue(new int[,] { { 31 } }, 0, 1);
        Console.WriteLine($"nested allocation={allocated.GetType() == rectangular}/{rectangular}/{rectangular.GetArrayRank()}/{rectangular.GetElementType()!.GetArrayRank()}/{rectangular.GetElementType() == typeof(int[,])}/{((int[,])allocated.GetValue(0, 1)!)[0, 0]}");
        Console.WriteLine($"nested rectangular is={IsNestedMd(allocated)}/{IsNestedMd(new int[1, 2][])}/{IsNestedMd(new int[1, 2])}/{IsNestedMd(new int[1, 2, 1][,])}/{IsNestedMd(null)}");
        Console.WriteLine($"nested rectangular cast={ReferenceEquals(allocated, CastNestedMd(allocated))}/{CastNestedMd(null) is null}");
        try { CastNestedMd(new int[1, 2][]); }
        catch (InvalidCastException) { Console.WriteLine("nested rectangular cast mismatch=InvalidCastException"); }
        Console.WriteLine("nested MD array identities end");
    }

    static void Check(string name, Type literal)
    {
        Type field = typeof(ArrayRankOneOwner).GetField(name)!.FieldType;
        var method = typeof(ArrayRankOneOwner).GetMethod("Echo" + name)!;
        Type result = method.ReturnType;
        Type parameter = method.GetParameters()[0].ParameterType;
        Console.WriteLine($"{name}={literal}/{literal.IsSZArray}/{literal.GetElementType()!.IsSZArray}");
        Console.WriteLine($"{name} identity={literal == field}/{literal == result}/{literal == parameter}");
    }

    static bool IsNestedMd(object value) => value is int[,][,];

    static object CastNestedMd(object value) => (int[,][,])value;
}
