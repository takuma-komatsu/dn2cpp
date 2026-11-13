using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace EnumPrivateContractsSubset;

internal static class Program
{
    // The fixture replaces these bodies with the exact CoreLib declarations.
    private static string Format<T, TStorage>(Type type, char format, ref byte data)
        where T : struct, INumber<T>, IBitwiseOperators<T, T, T>, IMinMaxValue<T>
        where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>, IMinMaxValue<TStorage> => throw new NotSupportedException();
    private static string Default<T, TStorage>(Type type, ref byte data)
        where T : struct, INumber<T>, IBitwiseOperators<T, T, T>, IMinMaxValue<T>
        where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>, IMinMaxValue<TStorage> => throw new NotSupportedException();
    private static string FormatAddress<T, TStorage>(Type type, char format, ref byte data)
        where T : struct, INumber<T>, IBitwiseOperators<T, T, T>, IMinMaxValue<T>
        where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>, IMinMaxValue<TStorage> => throw new NotSupportedException();
    private static string Name(Type type, ulong value) => throw new NotSupportedException();
    private static string NameAddress(Type type, ulong value) => throw new NotSupportedException();
    private static int Element(Type type) => throw new NotSupportedException();
    private static Type Underlying(Type type) => throw new NotSupportedException();
    private static string RuntimeName(Type type, object value) => throw new NotSupportedException();
    private static Func<object, string> BindName(Type type) => throw new NotSupportedException();
    private static bool RuntimeDefined(Type type, object value) => throw new NotSupportedException();
    private static Func<object, bool> BindDefined(Type type) => throw new NotSupportedException();

    private static void Cases<T, TStorage>(Type type, T value, string label)
        where T : struct, INumber<T>, IBitwiseOperators<T, T, T>, IMinMaxValue<T>
        where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>, IMinMaxValue<TStorage>
    {
        ref byte data = ref Unsafe.As<T, byte>(ref value);
        Console.WriteLine(label + ":default=" + Default<T, TStorage>(type, ref data));
        foreach (char format in "GgFfDdXx")
        {
            string result = Format<T, TStorage>(type, format, ref data);
            string address = FormatAddress<T, TStorage>(type, format, ref data);
            if (result != address) throw new Exception("private enum formatter address mismatch");
            Console.WriteLine(label + ":" + format + "=" + result);
        }
        Console.WriteLine(label + ":element=" + Element(type) + ":underlying=" + Underlying(type).Name);
    }

    internal static void Run()
    {
        Console.WriteLine("== enum private runtime contracts ==");
        Cases<sbyte, byte>(typeof(EnumStaticIntegerFormatSubset.I1), -1, "i1");
        Cases<byte, byte>(typeof(EnumStaticIntegerFormatSubset.U1), 200, "u1");
        Cases<short, ushort>(typeof(EnumStaticIntegerFormatSubset.I2), short.MinValue, "i2");
        Cases<ushort, ushort>(typeof(EnumStaticIntegerFormatSubset.U2), ushort.MaxValue, "u2");
        Cases<int, uint>(typeof(EnumStaticIntegerFormatSubset.I4), int.MinValue, "i4");
        Cases<uint, uint>(typeof(EnumStaticIntegerFormatSubset.U4), uint.MaxValue, "u4");
        Cases<long, ulong>(typeof(EnumStaticIntegerFormatSubset.I8), long.MinValue, "i8");
        Cases<ulong, ulong>(typeof(EnumStaticIntegerFormatSubset.U8), ulong.MaxValue, "u8");
        Cases<int, uint>(typeof(EnumStaticIntegerFormatSubset.Plain), 1, "alias");
        Cases<int, uint>(typeof(EnumStaticIntegerFormatSubset.Plain), 3, "flags");
        Type[] types = { typeof(EnumStaticIntegerFormatSubset.I1), typeof(EnumStaticIntegerFormatSubset.U8) };
        foreach (Type type in types)
        {
            Console.WriteLine("scalar:" + type.Name + ":" + Name(type, 1) + ":" + NameAddress(type, 1)
                + ":" + (Name(type, ulong.MaxValue) ?? "null"));
            object value = Enum.ToObject(type, 1);
            Console.WriteLine("runtime:" + type.Name + ":" + RuntimeName(type, value) + ":" + BindName(type)(value)
                + ":" + RuntimeDefined(type, value) + ":" + BindDefined(type)(value));
        }
        byte raw = 255;
        Console.WriteLine("numeric-no-type:" + Format<sbyte, byte>(null, 'D', ref raw)
            + ":" + Format<byte, byte>(null, 'X', ref raw));
        try { Format<byte, byte>(null, 'Q', ref raw); }
        catch (FormatException) { Console.WriteLine("private-invalid-format=FormatException"); }
        Console.WriteLine("enum private runtime contracts end");
    }
}
