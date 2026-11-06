using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NarrowEnumArgumentSubset;

internal static class EnumArguments451
{
    private enum U8 : byte { }
    private enum I8 : sbyte { }
    private enum U16 : ushort { }
    private enum I16 : short { }
    private enum I32 : int { }
    private enum U32 : uint { }
    private enum I64 : long { }
    private enum U8Other : byte { }
    private enum I8Other : sbyte { }
    private enum U16Other : ushort { }
    private enum I16Other : short { }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Identity<T>(T value) => value;
    private static long GenericU8(int value) => throw new InvalidOperationException("fixture missing");
    private static long GenericOtherU8(int value) => throw new InvalidOperationException("fixture missing");
    private static long GenericPointerU8(int value) => throw new InvalidOperationException("fixture missing");
    private static long GenericI8(int value) => throw new InvalidOperationException("fixture missing");
    private static long GenericOtherI8(int value) => throw new InvalidOperationException("fixture missing");
    private static long GenericPointerI8(int value) => throw new InvalidOperationException("fixture missing");
    private static long GenericU16(int value) => throw new InvalidOperationException("fixture missing");
    private static long GenericOtherU16(int value) => throw new InvalidOperationException("fixture missing");
    private static long GenericPointerU16(int value) => throw new InvalidOperationException("fixture missing");
    private static long GenericI16(int value) => throw new InvalidOperationException("fixture missing");
    private static long GenericOtherI16(int value) => throw new InvalidOperationException("fixture missing");
    private static long GenericPointerI16(int value) => throw new InvalidOperationException("fixture missing");
    private sealed class U8Owner
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal long Echo(U8 value) => (long)value;
    }
    private static long DirectInstance(U8Owner owner, int value) => throw new InvalidOperationException("fixture missing");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoU8(U8 value) => (long)value;
    private static long DirectU8(int value) => throw new InvalidOperationException("fixture missing");
    private static long PointerU8(int value) => throw new InvalidOperationException("fixture missing");
    private static long StoreU8(U8 original, int value) => throw new InvalidOperationException("fixture missing");
    private static long LocalU8(int value) => throw new InvalidOperationException("fixture missing");
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddressU8(U8 value) => ReadU8(ref value);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadU8(ref U8 value) => (long)value;
    private static long DirectAddressU8(int value) => throw new InvalidOperationException("fixture missing");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoI8(I8 value) => (long)value;
    private static long DirectI8(int value) => throw new InvalidOperationException("fixture missing");
    private static long PointerI8(int value) => throw new InvalidOperationException("fixture missing");
    private static long StoreI8(I8 original, int value) => throw new InvalidOperationException("fixture missing");
    private static long LocalI8(int value) => throw new InvalidOperationException("fixture missing");
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddressI8(I8 value) => ReadI8(ref value);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadI8(ref I8 value) => (long)value;
    private static long DirectAddressI8(int value) => throw new InvalidOperationException("fixture missing");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoU16(U16 value) => (long)value;
    private static long DirectU16(int value) => throw new InvalidOperationException("fixture missing");
    private static long PointerU16(int value) => throw new InvalidOperationException("fixture missing");
    private static long StoreU16(U16 original, int value) => throw new InvalidOperationException("fixture missing");
    private static long LocalU16(int value) => throw new InvalidOperationException("fixture missing");
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddressU16(U16 value) => ReadU16(ref value);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadU16(ref U16 value) => (long)value;
    private static long DirectAddressU16(int value) => throw new InvalidOperationException("fixture missing");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoI16(I16 value) => (long)value;
    private static long DirectI16(int value) => throw new InvalidOperationException("fixture missing");
    private static long PointerI16(int value) => throw new InvalidOperationException("fixture missing");
    private static long StoreI16(I16 original, int value) => throw new InvalidOperationException("fixture missing");
    private static long LocalI16(int value) => throw new InvalidOperationException("fixture missing");
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddressI16(I16 value) => ReadI16(ref value);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadI16(ref I16 value) => (long)value;
    private static long DirectAddressI16(int value) => throw new InvalidOperationException("fixture missing");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoI32(I32 value) => (long)value;
    private static long DirectI32(int value) => throw new InvalidOperationException("fixture missing");
    private static long PointerI32(int value) => throw new InvalidOperationException("fixture missing");
    private static long StoreI32(I32 original, int value) => throw new InvalidOperationException("fixture missing");
    private static long LocalI32(int value) => throw new InvalidOperationException("fixture missing");
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddressI32(I32 value) => ReadI32(ref value);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadI32(ref I32 value) => (long)value;
    private static long DirectAddressI32(int value) => throw new InvalidOperationException("fixture missing");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoU32(U32 value) => (long)value;
    private static long DirectU32(int value) => throw new InvalidOperationException("fixture missing");
    private static long PointerU32(int value) => throw new InvalidOperationException("fixture missing");
    private static long StoreU32(U32 original, int value) => throw new InvalidOperationException("fixture missing");
    private static long LocalU32(int value) => throw new InvalidOperationException("fixture missing");
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddressU32(U32 value) => ReadU32(ref value);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadU32(ref U32 value) => (long)value;
    private static long DirectAddressU32(int value) => throw new InvalidOperationException("fixture missing");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long EchoI64(I64 value) => (long)value;
    private static long DirectI64(long value) => throw new InvalidOperationException("fixture missing");
    private static long PointerI64(long value) => throw new InvalidOperationException("fixture missing");

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        int[] values = { int.MinValue, -65537, -32769, -32768, -257, -129, -128, -1,
            0, 127, 128, 255, 256, 32767, 32768, 65535, 65536, int.MaxValue };
        MethodInfo methodU8 = typeof(EnumArguments451).GetMethod("EchoU8", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (int value in values)
        {
            long expected = unchecked((byte)value);
            bool same = DirectU8(value) == expected && PointerU8(value) == expected
                && StoreU8(0, value) == expected && LocalU8(value) == expected
                && DirectAddressU8(value) == expected
                && (long)methodU8.Invoke(null, new object[] { unchecked((U8)value) }) == expected;
            if (!same) throw new InvalidOperationException("narrow-enum-U8-" + value);
        }
        Console.WriteLine("narrow-enum-U8:True");
        MethodInfo methodI8 = typeof(EnumArguments451).GetMethod("EchoI8", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (int value in values)
        {
            long expected = unchecked((sbyte)value);
            bool same = DirectI8(value) == expected && PointerI8(value) == expected
                && StoreI8(0, value) == expected && LocalI8(value) == expected
                && DirectAddressI8(value) == expected
                && (long)methodI8.Invoke(null, new object[] { unchecked((I8)value) }) == expected;
            if (!same) throw new InvalidOperationException("narrow-enum-I8-" + value);
        }
        Console.WriteLine("narrow-enum-I8:True");
        MethodInfo methodU16 = typeof(EnumArguments451).GetMethod("EchoU16", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (int value in values)
        {
            long expected = unchecked((ushort)value);
            bool same = DirectU16(value) == expected && PointerU16(value) == expected
                && StoreU16(0, value) == expected && LocalU16(value) == expected
                && DirectAddressU16(value) == expected
                && (long)methodU16.Invoke(null, new object[] { unchecked((U16)value) }) == expected;
            if (!same) throw new InvalidOperationException("narrow-enum-U16-" + value);
        }
        Console.WriteLine("narrow-enum-U16:True");
        MethodInfo methodI16 = typeof(EnumArguments451).GetMethod("EchoI16", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (int value in values)
        {
            long expected = unchecked((short)value);
            bool same = DirectI16(value) == expected && PointerI16(value) == expected
                && StoreI16(0, value) == expected && LocalI16(value) == expected
                && DirectAddressI16(value) == expected
                && (long)methodI16.Invoke(null, new object[] { unchecked((I16)value) }) == expected;
            if (!same) throw new InvalidOperationException("narrow-enum-I16-" + value);
        }
        Console.WriteLine("narrow-enum-I16:True");
        MethodInfo methodI32 = typeof(EnumArguments451).GetMethod("EchoI32", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (int value in values)
        {
            long expected = unchecked((int)value);
            bool same = DirectI32(value) == expected && PointerI32(value) == expected
                && StoreI32(0, value) == expected && LocalI32(value) == expected
                && DirectAddressI32(value) == expected
                && (long)methodI32.Invoke(null, new object[] { unchecked((I32)value) }) == expected;
            if (!same) throw new InvalidOperationException("narrow-enum-I32-" + value);
        }
        Console.WriteLine("narrow-enum-I32:True");
        MethodInfo methodU32 = typeof(EnumArguments451).GetMethod("EchoU32", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (int value in values)
        {
            long expected = unchecked((uint)value);
            bool same = DirectU32(value) == expected && PointerU32(value) == expected
                && StoreU32(0, value) == expected && LocalU32(value) == expected
                && DirectAddressU32(value) == expected
                && (long)methodU32.Invoke(null, new object[] { unchecked((U32)value) }) == expected;
            if (!same) throw new InvalidOperationException("narrow-enum-U32-" + value);
        }
        Console.WriteLine("narrow-enum-U32:True");
        foreach (int value in values)
        {
            long expected = unchecked((byte)value);
            if (GenericU8(value) != expected || GenericOtherU8(value) != expected
                || GenericPointerU8(value) != expected)
                throw new InvalidOperationException("narrow-enum-generic-U8-" + value);
        }
        Console.WriteLine("narrow-enum-generic-U8:True");
        foreach (int value in values)
        {
            long expected = unchecked((sbyte)value);
            if (GenericI8(value) != expected || GenericOtherI8(value) != expected
                || GenericPointerI8(value) != expected)
                throw new InvalidOperationException("narrow-enum-generic-I8-" + value);
        }
        Console.WriteLine("narrow-enum-generic-I8:True");
        foreach (int value in values)
        {
            long expected = unchecked((ushort)value);
            if (GenericU16(value) != expected || GenericOtherU16(value) != expected
                || GenericPointerU16(value) != expected)
                throw new InvalidOperationException("narrow-enum-generic-U16-" + value);
        }
        Console.WriteLine("narrow-enum-generic-U16:True");
        foreach (int value in values)
        {
            long expected = unchecked((short)value);
            if (GenericI16(value) != expected || GenericOtherI16(value) != expected
                || GenericPointerI16(value) != expected)
                throw new InvalidOperationException("narrow-enum-generic-I16-" + value);
        }
        Console.WriteLine("narrow-enum-generic-I16:True");
        var receiver = new U8Owner();
        foreach (int value in values)
            if (DirectInstance(receiver, value) != unchecked((byte)value))
                throw new InvalidOperationException("narrow-enum-instance");
        Console.WriteLine("narrow-enum-instance:True");
        foreach (long value in new[] { long.MinValue, -(1L << 40), -1, 0, 1L << 40, long.MaxValue })
            if (DirectI64(value) != value || PointerI64(value) != value)
                throw new InvalidOperationException("narrow-enum-I64");
        Console.WriteLine("narrow-enum-I64:True");
        Console.WriteLine("Narrow enum argument routes end");
    }
}
