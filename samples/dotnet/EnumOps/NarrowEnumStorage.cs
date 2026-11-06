using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace EnumOps;

internal static class NarrowEnumStorage
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Identity<T>(T value) => value;
    private enum U8 : byte { }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoU8(U8 value) => (long)value;
    private static long DirectU8(int value) => EchoU8((U8)value);
    private static long PointerU8(int value) => EchoU8((U8)value);
    private static long GenericU8(int value) => (long)Identity((U8)value);
    private static long LocalU8(int value)
    {
        U8 stored = (U8)value;
        return (long)stored;
    }
    private static long StoreU8(U8 stored, int value)
    {
        stored = (U8)value;
        return (long)stored;
    }
    private enum I8 : sbyte { }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoI8(I8 value) => (long)value;
    private static long DirectI8(int value) => EchoI8((I8)value);
    private static long PointerI8(int value) => EchoI8((I8)value);
    private static long GenericI8(int value) => (long)Identity((I8)value);
    private static long LocalI8(int value)
    {
        I8 stored = (I8)value;
        return (long)stored;
    }
    private static long StoreI8(I8 stored, int value)
    {
        stored = (I8)value;
        return (long)stored;
    }
    private enum U16 : ushort { }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoU16(U16 value) => (long)value;
    private static long DirectU16(int value) => EchoU16((U16)value);
    private static long PointerU16(int value) => EchoU16((U16)value);
    private static long GenericU16(int value) => (long)Identity((U16)value);
    private static long LocalU16(int value)
    {
        U16 stored = (U16)value;
        return (long)stored;
    }
    private static long StoreU16(U16 stored, int value)
    {
        stored = (U16)value;
        return (long)stored;
    }
    private enum I16 : short { }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoI16(I16 value) => (long)value;
    private static long DirectI16(int value) => EchoI16((I16)value);
    private static long PointerI16(int value) => EchoI16((I16)value);
    private static long GenericI16(int value) => (long)Identity((I16)value);
    private static long LocalI16(int value)
    {
        I16 stored = (I16)value;
        return (long)stored;
    }
    private static long StoreI16(I16 stored, int value)
    {
        stored = (I16)value;
        return (long)stored;
    }
    private enum I32 : int { }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoI32(I32 value) => (long)value;
    private static long DirectI32(int value) => EchoI32((I32)value);
    private static long PointerI32(int value) => EchoI32((I32)value);
    private static long GenericI32(int value) => (long)Identity((I32)value);
    private static long LocalI32(int value)
    {
        I32 stored = (I32)value;
        return (long)stored;
    }
    private static long StoreI32(I32 stored, int value)
    {
        stored = (I32)value;
        return (long)stored;
    }

    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== narrow enum storage ==");
        Console.WriteLine("byte=" + DirectU8(256) + "," + PointerU8(256) + "," + GenericU8(256) + "," + LocalU8(256) + "," + StoreU8(0, 256));
        Console.WriteLine("sbyte=" + DirectI8(255) + "," + PointerI8(255) + "," + GenericI8(255) + "," + LocalI8(255) + "," + StoreI8(0, 255));
        Console.WriteLine("ushort=" + DirectU16(65536) + "," + PointerU16(65536) + "," + GenericU16(65536) + "," + LocalU16(65536) + "," + StoreU16(0, 65536));
        Console.WriteLine("short=" + DirectI16(65535) + "," + PointerI16(65535) + "," + GenericI16(65535) + "," + LocalI16(65535) + "," + StoreI16(0, 65535));
        Console.WriteLine("int=" + DirectI32(305419896) + "," + PointerI32(305419896) + "," + GenericI32(305419896) + "," + LocalI32(305419896) + "," + StoreI32(0, 305419896));
        Console.WriteLine("narrow enum storage end");
    }
}
