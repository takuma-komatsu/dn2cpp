#nullable disable
// (practical reflection): Nullable.GetUnderlyingType(Type) + typeof(T?). A serializer
// / data-binding layer detects a nullable property type with Nullable.GetUnderlyingType. The
// carve-out was that typeof(int?) emitted a reference to an un-emitted Nullable<Int32>
// type-info (a closed generic value type otherwise unreached). Now typeof(T?) notes the
// closed Nullable<T> for emission, so its type-info carries genericDef=Nullable`1 +
// genericArgs=[T] (the closed-generic metadata); Nullable.GetUnderlyingType reads that
// arg back, and IsGenericType / GetGenericArguments work too. Nullable<T> *values* (int?
// locals, lifted operators, the special box/unbox) are pinned separately by
// NullableValueSubset. Diffed exact vs real.NET.
using System;

namespace ReflectNullableSubset;

public static class Program
{
    static string U(Type t) => Nullable.GetUnderlyingType(t)?.Name ?? "null";

    internal static void Run()
    {
        Console.WriteLine(U(typeof(int?)));
        Console.WriteLine(U(typeof(double?)));
        Console.WriteLine(U(typeof(bool?)));
        Console.WriteLine(U(typeof(int)));     // null — not nullable
        Console.WriteLine(U(typeof(string)));  // null — reference type

        // IsGenericType / GetGenericArguments on a Nullable type
        Type nt = typeof(long?);
        Console.WriteLine(nt.IsGenericType);
        Console.WriteLine(nt.GetGenericArguments()[0].Name);

        // serializer-style: use the underlying type when nullable, else the type itself
        Type prop = typeof(int?);
        Type effective = Nullable.GetUnderlyingType(prop) ?? prop;
        Console.WriteLine("effective=" + effective.Name);
    }
}
