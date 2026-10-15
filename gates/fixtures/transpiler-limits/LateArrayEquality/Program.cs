using System;
using System.Globalization;

namespace LateArrayEquality;

internal struct SignatureMatch<T>
{
    public override bool Equals(object? other)
    {
        TypedReference reference = __makeref(other);
        return __reftype(reference) == typeof(object);
    }

    public override int GetHashCode() => 0;
}

internal static class Signatures
{
    public static SignatureMatch<int> Value() => default;
}

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Type element = typeof(Signatures).GetMethod(nameof(Signatures.Value))!.ReturnType;
        Array values = Array.CreateInstance(element, 1);
        Console.WriteLine(Array.IndexOf(values, null));
    }
}
