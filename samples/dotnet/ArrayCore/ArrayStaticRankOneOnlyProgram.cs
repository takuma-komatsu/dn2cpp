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
}
