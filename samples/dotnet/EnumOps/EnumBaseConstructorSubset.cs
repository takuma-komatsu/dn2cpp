using System;
using System.Reflection;

namespace EnumBaseConstructorSubset;

internal static class Program
{
    private enum Wide : long { Value = 1234567890123 }
    private enum Tiny : sbyte { Value = -128 }
    private struct Pair { public int X; public long Y; }

    internal static void Run()
    {
        Console.WriteLine("== enum base constructors ==");
        ConstructorInfo enumCtor = typeof(Enum).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null);
        object boxedEnum = Wide.Value;
        object boxedStruct = new Pair { X = -7, Y = 1234567890123 };
        Invoke(enumCtor, boxedEnum, "enum-enum");
        Console.WriteLine("enum-enum-payload:" + (long)(Wide)boxedEnum);
        object boxedTiny = Tiny.Value;
        Invoke(enumCtor, boxedTiny, "enum-tiny");
        Console.WriteLine("enum-tiny-payload:" + (sbyte)(Tiny)boxedTiny);
        Invoke(enumCtor, null, "enum-null");
        Invoke(enumCtor, new object(), "enum-object");
        Invoke(enumCtor, boxedStruct, "enum-struct");
        Console.WriteLine("enum-struct-payload:" + ((Pair)boxedStruct).X + ":" + ((Pair)boxedStruct).Y);
        Console.WriteLine("enum base constructors end");
    }

    private static void Invoke(ConstructorInfo ctor, object target, string label)
    {
        try
        {
            object result = ctor.Invoke(target, Array.Empty<object>());
            Console.WriteLine("ctor:" + label + ":" + (result == null));
        }
        catch (Exception ex)
        {
            Console.WriteLine("ctor:" + label + ":" + ex.GetType().Name);
        }
    }
}
