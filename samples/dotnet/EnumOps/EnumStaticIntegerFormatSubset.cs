using System;
using System.Globalization;
using System.Runtime.CompilerServices;

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
    // Only the fixture replaces these four generic mouths; the CLR oracle stays public.
    internal static string Direct<T,U>(Type type, ref byte data) where T:unmanaged where U:unmanaged => Public<T>(type, 'G', ref data);
    internal static string Pointer<T,U>(Type type, ref byte data) where T:unmanaged where U:unmanaged => Public<T>(type, 'G', ref data);
    internal static string Format<T,U>(Type type, char format, ref byte data) where T:unmanaged where U:unmanaged => Public<T>(type, format, ref data);
    internal static string FormatPointer<T,U>(Type type, char format, ref byte data) where T:unmanaged where U:unmanaged => Public<T>(type, format, ref data);

    private static Type StorageEnum<T>() where T:unmanaged
    {
        if (typeof(T)==typeof(sbyte)) return typeof(I1);
        if (typeof(T)==typeof(byte)) return typeof(U1);
        if (typeof(T)==typeof(short)) return typeof(I2);
        if (typeof(T)==typeof(ushort)) return typeof(U2);
        if (typeof(T)==typeof(int)) return typeof(I4);
        if (typeof(T)==typeof(uint)) return typeof(U4);
        if (typeof(T)==typeof(long)) return typeof(I8);
        if (typeof(T)==typeof(ulong)) return typeof(U8);
        throw new InvalidOperationException();
    }

    private static string Public<T>(Type type, char format, ref byte data) where T:unmanaged
    {
        T value=Unsafe.ReadUnaligned<T>(ref data);
        // D/X's reference oracle uses a genuine enum of the fixed primitive width, not the unused Type input.
        if (format is 'D' or 'd' or 'X' or 'x') type=StorageEnum<T>();
        return Enum.Format(type, Enum.ToObject(type, value), format.ToString());
    }

    private static void Cases<T,U>(Type type, T value, string tag) where T:unmanaged where U:unmanaged
    {
        int width=Unsafe.SizeOf<T>();
        byte[] data=new byte[width+2]; data[0]=90; data[width+1]=165;
        ref byte raw=ref data[1]; Unsafe.WriteUnaligned(ref raw,value);
        Console.WriteLine(tag+":default="+Direct<T,U>(type,ref raw));
        Console.WriteLine(tag+":pointer="+Pointer<T,U>(type,ref raw));
        foreach(char format in "GgFfDdXx")
            Console.WriteLine(tag+":"+format+"="+Format<T,U>(type,format,ref raw)+"/"+FormatPointer<T,U>(type,format,ref raw));
        Console.WriteLine(tag+":type-free="+Format<T,U>(null,'D',ref raw)+"/"+FormatPointer<T,U>(null,'x',ref raw));
        bool unchanged=data[0]==90 && data[width+1]==165;
        byte[] expected=new byte[width]; Unsafe.WriteUnaligned(ref expected[0],value);
        for(int i=0;i<width;i++) unchanged &= data[i+1]==expected[i];
        Console.WriteLine(tag+":storage="+unchanged);
    }

    private static void Failure(string name, Action action)
    {
        try { action(); Console.WriteLine(name+"=returned"); }
        catch(Exception e) { Console.WriteLine(name+"="+e.GetType().Name); }
    }

    internal static void RunProbe(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        Console.WriteLine("== enum static integer formatter ==");
        Cases<sbyte,byte>(typeof(I1),0,"i1-zero"); Cases<sbyte,byte>(typeof(I1),3,"i1-alias"); Cases<sbyte,byte>(typeof(I1),-1,"i1-negative"); Cases<sbyte,byte>(typeof(I1),sbyte.MinValue,"i1-min");
        Cases<byte,byte>(typeof(U1),3,"u1-alias"); Cases<byte,byte>(typeof(U1),200,"u1-undefined"); Cases<byte,byte>(typeof(U1),byte.MaxValue,"u1-max");
        Cases<short,ushort>(typeof(I2),3,"i2-alias"); Cases<short,ushort>(typeof(I2),short.MinValue,"i2-min"); Cases<short,ushort>(typeof(I2),-1,"i2-negative");
        Cases<ushort,ushort>(typeof(U2),3,"u2-alias"); Cases<ushort,ushort>(typeof(U2),ushort.MaxValue,"u2-max");
        Cases<int,uint>(typeof(I4),3,"i4-alias"); Cases<int,uint>(typeof(I4),int.MinValue,"i4-min"); Cases<int,uint>(typeof(Plain),1,"plain-alias"); Cases<int,uint>(typeof(Plain),3,"plain-flags");
        Cases<uint,uint>(typeof(U4),3,"u4-alias"); Cases<uint,uint>(typeof(U4),uint.MaxValue,"u4-max");
        Cases<long,ulong>(typeof(I8),3,"i8-alias"); Cases<long,ulong>(typeof(I8),long.MinValue,"i8-min"); Cases<long,ulong>(typeof(I8),-1,"i8-negative");
        Cases<ulong,ulong>(typeof(U8),3,"u8-alias"); Cases<ulong,ulong>(typeof(U8),1UL<<63,"u8-high"); Cases<ulong,ulong>(typeof(U8),ulong.MaxValue,"u8-max");
        byte b=3;
        Failure("invalid-format",()=>Format<byte,byte>(typeof(U1),'Q',ref b));
        Console.WriteLine("enum static integer public parity end");
        if(args.Length==0) return;
        // Invalid private inputs are native-only safety controls, not CLR parity claims.
        Failure("raw-null-before-format",()=>Format<byte,byte>(null,'Q',ref Unsafe.NullRef<byte>()));
        Failure("raw-null-before-decimal",()=>Format<byte,byte>(null,'D',ref Unsafe.NullRef<byte>()));
        Failure("invalid-format-before-type",()=>Format<byte,byte>(null,'Q',ref b));
        Failure("metadata-null-type",()=>Direct<byte,byte>(null,ref b));
        Failure("metadata-non-enum",()=>Direct<byte,byte>(typeof(byte),ref b));
        Failure("metadata-wrong-underlying",()=>Format<byte,byte>(typeof(U2),'F',ref b));
        Console.WriteLine("native enum static safety controls end");
    }
}
