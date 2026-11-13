using System;

// Enum.Format over every integral width: flags aliases, sign bits, undefined
// values, a non-flags alias and the invalid-format fault.
namespace EnumStaticIntegerFormatSubset;

[Flags] internal enum I1 : sbyte { Zero=0, One=1, Two=2, Both=3, Alias=3, High=sbyte.MinValue }
[Flags] internal enum U1 : byte { Zero=0, One=1, Two=2, Both=3, Alias=3, High=128 }
[Flags] internal enum I2 : short { Zero=0, One=1, Two=2, Both=3, Alias=3, High=short.MinValue }
[Flags] internal enum U2 : ushort { Zero=0, One=1, Two=2, Both=3, Alias=3, High=32768 }
[Flags] internal enum I4 : int { Zero=0, One=1, Two=2, Both=3, Alias=3, High=int.MinValue }
[Flags] internal enum U4 : uint { Zero=0, One=1, Two=2, Both=3, Alias=3, High=2147483648 }
[Flags] internal enum I8 : long { Zero=0, One=1, Two=2, Both=3, Alias=3, High=long.MinValue }
[Flags] internal enum U8 : ulong { Zero=0, One=1, Two=2, Both=3, Alias=3, High=9223372036854775808 }
internal enum Plain : int { Zero=0, First=1, Alias=1, Two=2 }

internal static class Program
{
    private static void Cases<T>(Type type, T value, string tag)
    {
        object boxed = Enum.ToObject(type, value);
        foreach (char format in "GgFfDdXx")
            Console.WriteLine(tag + ":" + format + "=" + Enum.Format(type, boxed, format.ToString()));
    }

    internal static void Run()
    {
        Console.WriteLine("== enum static integer format ==");
        Cases<sbyte>(typeof(I1),0,"i1-zero"); Cases<sbyte>(typeof(I1),3,"i1-alias"); Cases<sbyte>(typeof(I1),-1,"i1-negative"); Cases<sbyte>(typeof(I1),sbyte.MinValue,"i1-min");
        Cases<byte>(typeof(U1),3,"u1-alias"); Cases<byte>(typeof(U1),200,"u1-undefined"); Cases<byte>(typeof(U1),byte.MaxValue,"u1-max");
        Cases<short>(typeof(I2),3,"i2-alias"); Cases<short>(typeof(I2),short.MinValue,"i2-min"); Cases<short>(typeof(I2),-1,"i2-negative");
        Cases<ushort>(typeof(U2),3,"u2-alias"); Cases<ushort>(typeof(U2),ushort.MaxValue,"u2-max");
        Cases<int>(typeof(I4),3,"i4-alias"); Cases<int>(typeof(I4),int.MinValue,"i4-min"); Cases<int>(typeof(Plain),1,"plain-alias"); Cases<int>(typeof(Plain),3,"plain-flags");
        Cases<uint>(typeof(U4),3,"u4-alias"); Cases<uint>(typeof(U4),uint.MaxValue,"u4-max");
        Cases<long>(typeof(I8),3,"i8-alias"); Cases<long>(typeof(I8),long.MinValue,"i8-min"); Cases<long>(typeof(I8),-1,"i8-negative");
        Cases<ulong>(typeof(U8),3,"u8-alias"); Cases<ulong>(typeof(U8),1UL<<63,"u8-high"); Cases<ulong>(typeof(U8),ulong.MaxValue,"u8-max");
        try { Console.WriteLine("invalid-format=" + Enum.Format(typeof(U1), U1.Both, "Q")); }
        catch (Exception e) { Console.WriteLine("invalid-format=" + e.GetType().Name); }
        Console.WriteLine("enum static integer format end");
    }
}
