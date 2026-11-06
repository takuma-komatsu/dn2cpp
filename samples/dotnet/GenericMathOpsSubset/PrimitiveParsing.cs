using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GenericMathParsing;

internal static class PrimitiveParsing
{
    private static int _checks;
    private static string _order = "";
    private static int _factoryFailure = -1;
    private static byte[] _bytes = new byte[] { 0x31 };
    private static char _char = '\uffff';
    private static nint _native = 42;
    private const string Invalid = "Input string was not in a correct format.";
    private const string Single = "String must be exactly one character long.";

    private static void Check(string label, bool condition)
    {
        if (!condition) throw new InvalidOperationException("parse." + label);
        _checks++;
        Console.WriteLine("parse." + label + "=ok");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ParseNumber<T>(ReadOnlySpan<char> text, NumberStyles styles, IFormatProvider provider) where T : INumberBase<T> => T.Parse(text, styles, provider);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TryNumber<T>(ReadOnlySpan<char> text, NumberStyles styles, IFormatProvider provider, out T value) where T : INumberBase<T> => T.TryParse(text, styles, provider, out value);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ParseSpan<T>(ReadOnlySpan<char> text, IFormatProvider provider) where T : ISpanParsable<T> => T.Parse(text, provider);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TrySpan<T>(ReadOnlySpan<char> text, IFormatProvider provider, out T value) where T : ISpanParsable<T> => T.TryParse(text, provider, out value);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ParseBytes<T>(ReadOnlySpan<byte> text, NumberStyles styles, IFormatProvider provider) where T : INumberBase<T> => T.Parse(text, styles, provider);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TryBytes<T>(ReadOnlySpan<byte> text, NumberStyles styles, IFormatProvider provider, out T value) where T : INumberBase<T> => T.TryParse(text, styles, provider, out value);

    private sealed class ProviderFailure : Exception { }
    private sealed class FactoryFailure : Exception { }
    private sealed class Provider : IFormatProvider
    {
        private readonly bool _throws;
        internal Provider(bool throws) { _throws = throws; }
        public object GetFormat(Type type)
        {
            _order += "G";
            if (_throws) throw new ProviderFailure();
            return CultureInfo.InvariantCulture.NumberFormat;
        }
    }

    private static void CharCase(string label, string text, bool success, char expected)
    {
        for (int route = 0; route < 2; route++)
        {
            _order = "";
            char value = '\uffff';
            bool ok = route == 0 ? TryNumber<char>(text.AsSpan(), (NumberStyles)(-1), new Provider(true), out value)
                : TrySpan<char>(text.AsSpan(), new Provider(true), out value);
            Check("char." + label + ".try" + route, ok == success && value == expected && _order == "");
            bool format = false; value = '\uffff';
            try { value = route == 0 ? ParseNumber<char>(text.AsSpan(), (NumberStyles)(-1), new Provider(true)) : ParseSpan<char>(text.AsSpan(), new Provider(true)); }
            catch (FormatException error) { format = error.Message == Single; }
            Check("char." + label + ".read" + route, format == !success && (!success || value == expected) && _order == "");
        }
    }

    private static void CharBytes(string label, byte[] text, bool success, char expected, string failure)
    {
        _order = ""; char value = '\uffff';
        bool ok = TryBytes<char>(text, (NumberStyles)(-1), new Provider(true), out value);
        Check("char.utf8." + label + ".try", ok == success && value == expected && _order == "");
        bool format = false; value = '\uffff';
        try { value = ParseBytes<char>(text, (NumberStyles)(-1), new Provider(true)); }
        catch (FormatException error) { format = error.Message == failure; }
        Check("char.utf8." + label + ".read", format == !success && (!success || value == expected) && _order == "");
    }

    private static void NativeCase<T>(string label, string text, NumberStyles styles, int failure, T expected, bool spanRoute, T sentinel) where T : IBinaryInteger<T>
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        for (int route = 0; route < (spanRoute ? 3 : 2); route++)
        {
            T value = sentinel;
            bool ok = route == 0 ? TryNumber<T>(text.AsSpan(), styles, CultureInfo.InvariantCulture, out value)
                : route == 1 ? TryBytes<T>(bytes, styles, CultureInfo.InvariantCulture, out value)
                : TrySpan<T>(text.AsSpan(), CultureInfo.InvariantCulture, out value);
            Check(label + ".try" + route, ok == (failure == 0) && value.Equals(expected));
            value = sentinel; int actual = 0;
            try { value = route == 0 ? ParseNumber<T>(text.AsSpan(), styles, CultureInfo.InvariantCulture)
                : route == 1 ? ParseBytes<T>(bytes, styles, CultureInfo.InvariantCulture)
                : ParseSpan<T>(text.AsSpan(), CultureInfo.InvariantCulture); }
            catch (FormatException) { actual = 1; }
            catch (OverflowException) { actual = 2; }
            Check(label + ".read" + route, actual == failure && (failure != 0 || value.Equals(expected)));
        }
    }

    private static void Malformed<T>(string label, byte[] bytes, IFormatProvider provider, T sentinel) where T : IBinaryInteger<T>
    {
        T value = sentinel;
        Check(label + ".try", !TryBytes<T>(bytes, NumberStyles.Integer, provider, out value) && value.Equals(default(T)));
        bool format = false;
        try { ParseBytes<T>(bytes, NumberStyles.Integer, provider); }
        catch (FormatException error) { format = error.Message == Invalid; }
        Check(label + ".read", format);
    }

    private static void ReplacementSign<T>(string label, bool signed, T one, T negativeValue, T sentinel) where T : IBinaryInteger<T>
    {
        var nfi = new NumberFormatInfo();
        byte[] malformed = { 0xff, 0x31 }, encoded = { 0xef, 0xbf, 0xbd, 0x31 };
        for (int negative = 0; negative < 2; negative++)
        {
            nfi.PositiveSign = negative == 0 ? "\ufffd" : "+";
            nfi.NegativeSign = negative == 1 ? "\ufffd" : "-";
            Malformed<T>(label + ".bad" + negative, malformed, nfi, sentinel);
            T value = sentinel;
            bool ok = TryBytes<T>(encoded, NumberStyles.Integer, nfi, out value);
            T expected = negative == 0 ? one : negativeValue;
            Check(label + ".encoded.try" + negative, ok == (negative == 0 || signed) && value.Equals(expected));
            bool overflow = false; value = default(T);
            try { value = ParseBytes<T>(encoded, NumberStyles.Integer, nfi); }
            catch (OverflowException) { overflow = true; }
            Check(label + ".encoded.read" + negative, overflow == (negative == 1 && !signed) && (overflow || value.Equals(expected)));
        }
    }

    private static void ValidSign(string label, string sign, byte[] bytes)
    {
        var nfi = new NumberFormatInfo { PositiveSign = sign };
        nint value = 42;
        Check("utf8.valid." + label + ".try", TryBytes<nint>(bytes, NumberStyles.Integer, nfi, out value) && value == 1);
        Check("utf8.valid." + label + ".read", ParseBytes<nint>(bytes, NumberStyles.Integer, nfi) == 1);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CharFields { public char Before; public char Value; public char After; }
    private static void Storage()
    {
        char local = '\uffff';
        Check("storage.local", TryNumber<char>("\ud800".AsSpan(), NumberStyles.Integer, null, out local) && local == '\ud800');
        Check("storage.local-zero", !TryNumber<char>("xx".AsSpan(), NumberStyles.Integer, null, out local) && local == 0);
        char[] array = { '\u1234', '\uffff', '\u5678' };
        Check("storage.array", TrySpan<char>("\uffff".AsSpan(), null, out array[1]) && array[1] == '\uffff' && array[0] == '\u1234' && array[2] == '\u5678');
        Check("storage.array-zero", !TryBytes<char>(new byte[] { 0xff }, NumberStyles.Integer, null, out array[1]) && array[1] == 0 && array[0] == '\u1234' && array[2] == '\u5678');
        CharFields fields = new CharFields { Before = '\u1234', Value = '\uffff', After = '\u5678' };
        Check("storage.field", TryNumber<char>("\udfff".AsSpan(), NumberStyles.Integer, null, out fields.Value) && fields.Value == '\udfff' && fields.Before == '\u1234' && fields.After == '\u5678');
        Check("storage.field-zero", !TrySpan<char>("".AsSpan(), null, out fields.Value) && fields.Value == 0 && fields.Before == '\u1234' && fields.After == '\u5678');
        nuint[] native = { 123, 42, 456 };
        Check("storage.native", TryBytes<nuint>(Encoding.UTF8.GetBytes("18446744073709551615"), NumberStyles.Integer, null, out native[1]) && native[1] == nuint.MaxValue && native[0] == 123 && native[2] == 456);
        Check("storage.native-zero", !TryBytes<nuint>(new byte[] { 0xff }, NumberStyles.Integer, null, out native[1]) && native[1] == 0 && native[0] == 123 && native[2] == 456);
    }

    private static void Effect(string label, int index) { _order += label; if (_factoryFailure == index) throw new FactoryFailure(); }
    private static ReadOnlySpan<char> InputChars() { Effect("I", 0); return "1".AsSpan(); }
    private static ReadOnlySpan<byte> InputBytes() { Effect("I", 0); return _bytes; }
    private static NumberStyles Style(NumberStyles value) { Effect("Y", 1); return value; }
    private static IFormatProvider ProviderArg(bool throws) { Effect("P", 2); return new Provider(throws); }
    private static ref char CharOut() { Effect("O", 3); return ref _char; }
    private static ref nint NativeOut() { Effect("O", 3); return ref _native; }
    private static byte _Byte = 42;
    private static ref byte OutByte() { Effect("O", 3); return ref _Byte; }
    private static sbyte _SByte = 42;
    private static ref sbyte OutSByte() { Effect("O", 3); return ref _SByte; }
    private static short _Int16 = 42;
    private static ref short OutInt16() { Effect("O", 3); return ref _Int16; }
    private static ushort _UInt16 = 42;
    private static ref ushort OutUInt16() { Effect("O", 3); return ref _UInt16; }
    private static int _Int32 = 42;
    private static ref int OutInt32() { Effect("O", 3); return ref _Int32; }
    private static uint _UInt32 = 42;
    private static ref uint OutUInt32() { Effect("O", 3); return ref _UInt32; }
    private static long _Int64 = 42;
    private static ref long OutInt64() { Effect("O", 3); return ref _Int64; }
    private static ulong _UInt64 = 42;
    private static ref ulong OutUInt64() { Effect("O", 3); return ref _UInt64; }

    private static void Reset()
    {
        _order = ""; _factoryFailure = -1;
        _Byte = 42;
        _SByte = 42;
        _Int16 = 42;
        _UInt16 = 42;
        _Int32 = 42;
        _UInt32 = 42;
        _Int64 = 42;
        _UInt64 = 42;
    }
    private static ulong WidthValue(int width) => width switch
    {
        0 => (ulong)_Byte,
        1 => (ulong)_SByte,
        2 => (ulong)_Int16,
        3 => (ulong)_UInt16,
        4 => (ulong)_Int32,
        5 => (ulong)_UInt32,
        6 => (ulong)_Int64,
        7 => (ulong)_UInt64,
        _ => throw new InvalidOperationException()
    };
    private static bool DirectTry(int width, bool bytes, NumberStyles style, bool throws)
    {
        switch (width)
        {
            case 0: return bytes ? byte.TryParse(InputBytes(), Style(style), ProviderArg(throws), out OutByte()) : byte.TryParse(InputChars(), Style(style), ProviderArg(throws), out OutByte());
            case 1: return bytes ? sbyte.TryParse(InputBytes(), Style(style), ProviderArg(throws), out OutSByte()) : sbyte.TryParse(InputChars(), Style(style), ProviderArg(throws), out OutSByte());
            case 2: return bytes ? short.TryParse(InputBytes(), Style(style), ProviderArg(throws), out OutInt16()) : short.TryParse(InputChars(), Style(style), ProviderArg(throws), out OutInt16());
            case 3: return bytes ? ushort.TryParse(InputBytes(), Style(style), ProviderArg(throws), out OutUInt16()) : ushort.TryParse(InputChars(), Style(style), ProviderArg(throws), out OutUInt16());
            case 4: return bytes ? int.TryParse(InputBytes(), Style(style), ProviderArg(throws), out OutInt32()) : int.TryParse(InputChars(), Style(style), ProviderArg(throws), out OutInt32());
            case 5: return bytes ? uint.TryParse(InputBytes(), Style(style), ProviderArg(throws), out OutUInt32()) : uint.TryParse(InputChars(), Style(style), ProviderArg(throws), out OutUInt32());
            case 6: return bytes ? long.TryParse(InputBytes(), Style(style), ProviderArg(throws), out OutInt64()) : long.TryParse(InputChars(), Style(style), ProviderArg(throws), out OutInt64());
            case 7: return bytes ? ulong.TryParse(InputBytes(), Style(style), ProviderArg(throws), out OutUInt64()) : ulong.TryParse(InputChars(), Style(style), ProviderArg(throws), out OutUInt64());
            default: throw new InvalidOperationException();
        }
    }
    private static void DirectParse(int width, bool bytes, NumberStyles style, bool throws)
    {
        switch (width)
        {
            case 0: if (bytes) byte.Parse(InputBytes(), Style(style), ProviderArg(throws)); else byte.Parse(InputChars(), Style(style), ProviderArg(throws)); break;
            case 1: if (bytes) sbyte.Parse(InputBytes(), Style(style), ProviderArg(throws)); else sbyte.Parse(InputChars(), Style(style), ProviderArg(throws)); break;
            case 2: if (bytes) short.Parse(InputBytes(), Style(style), ProviderArg(throws)); else short.Parse(InputChars(), Style(style), ProviderArg(throws)); break;
            case 3: if (bytes) ushort.Parse(InputBytes(), Style(style), ProviderArg(throws)); else ushort.Parse(InputChars(), Style(style), ProviderArg(throws)); break;
            case 4: if (bytes) int.Parse(InputBytes(), Style(style), ProviderArg(throws)); else int.Parse(InputChars(), Style(style), ProviderArg(throws)); break;
            case 5: if (bytes) uint.Parse(InputBytes(), Style(style), ProviderArg(throws)); else uint.Parse(InputChars(), Style(style), ProviderArg(throws)); break;
            case 6: if (bytes) long.Parse(InputBytes(), Style(style), ProviderArg(throws)); else long.Parse(InputChars(), Style(style), ProviderArg(throws)); break;
            case 7: if (bytes) ulong.Parse(InputBytes(), Style(style), ProviderArg(throws)); else ulong.Parse(InputChars(), Style(style), ProviderArg(throws)); break;
            default: throw new InvalidOperationException();
        }
    }
    private static void ProviderOrder()
    {
        for (int width = 0; width < 8; width++)
        for (int bytes = 0; bytes < 2; bytes++)
        {
            string label = "order.width" + width + (bytes == 0 ? ".chars" : ".bytes");
            _bytes = new byte[] { 0xff, 0x31 };
            Reset(); bool argument = false;
            try { DirectTry(width, bytes != 0, (NumberStyles)(-1), true); }
            catch (ArgumentException error) { argument = error.ParamName == "style"; }
            Check(label + ".try-invalid", argument && _order == "IYPO" && WidthValue(width) == 42);
            Reset(); bool provider = false;
            try { DirectTry(width, bytes != 0, NumberStyles.Integer, true); }
            catch (ProviderFailure) { provider = true; }
            Check(label + ".try-provider", provider && _order == "IYPOG" && WidthValue(width) == 42);
            Reset(); argument = false;
            try { DirectParse(width, bytes != 0, (NumberStyles)(-1), true); }
            catch (ArgumentException error) { argument = error.ParamName == "style"; }
            Check(label + ".read-invalid", argument && _order == "IYP");
            Reset(); provider = false;
            try { DirectParse(width, bytes != 0, NumberStyles.Integer, true); }
            catch (ProviderFailure) { provider = true; }
            Check(label + ".read-provider", provider && _order == "IYPG");
            _bytes = new byte[] { 0x31 }; Reset();
            Check(label + ".success", DirectTry(width, bytes != 0, NumberStyles.Integer, false) && WidthValue(width) == 1 && _order == "IYPOG");
        }
        for (int fail = 0; fail < 4; fail++)
        {
            Reset(); _factoryFailure = fail; _native = 42; bool caught = false;
            try { TryNumber<nint>(InputChars(), Style((NumberStyles)(-1)), ProviderArg(true), out NativeOut()); }
            catch (FactoryFailure) { caught = true; }
            Check("order.factory" + fail, caught && _native == 42 && _order == (fail == 0 ? "I" : fail == 1 ? "IY" : fail == 2 ? "IYP" : "IYPO"));
        }
        Reset(); _char = '\uffff';
        Check("order.char-ignored", TryNumber<char>(InputChars(), Style((NumberStyles)(-1)), ProviderArg(true), out CharOut()) && _char == '1' && _order == "IYPO");
        for (int bytes = 0; bytes < 2; bytes++)
        {
            Reset(); _native = 42; _bytes = new byte[] { 0xff }; bool argument = false;
            try { if (bytes == 0) TryNumber<nint>(InputChars(), Style((NumberStyles)(-1)), ProviderArg(true), out NativeOut());
                else TryBytes<nint>(InputBytes(), Style((NumberStyles)(-1)), ProviderArg(true), out NativeOut()); }
            catch (ArgumentException error) { argument = error.ParamName == "style"; }
            Check("order.native-invalid" + bytes, argument && _native == 42 && _order == "IYPO");
        }
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== Primitive span parsing managed bodies ==");
        Check("pointer64", IntPtr.Size == 8 && UIntPtr.Size == 8);
        CharCase("empty", "", false, (char)0x0000);
        CharCase("nul", "\0", true, (char)0x0000);
        CharCase("digit", "1", true, (char)0x0031);
        CharCase("letter", "A", true, (char)0x0041);
        CharCase("space", " ", true, (char)0x0020);
        CharCase("accent", "\u00e9", true, (char)0x00e9);
        CharCase("nonascii", "\u0100", true, (char)0x0100);
        CharCase("high-surrogate", "\ud800", true, (char)0xd800);
        CharCase("low-surrogate", "\udfff", true, (char)0xdfff);
        CharCase("ffff", "\uffff", true, (char)0xffff);
        CharCase("two", "65", false, (char)0x0000);
        CharCase("pair", "\ud83d\ude00", false, (char)0x0000);
        CharBytes("empty", new byte[] {  }, false, (char)0x0000, Single);
        CharBytes("nul", new byte[] { 0x00 }, true, (char)0x0000, Single);
        CharBytes("ascii", new byte[] { 0x31 }, true, (char)0x0031, Single);
        CharBytes("accent", new byte[] { 0xc3, 0xa9 }, true, (char)0x00e9, Single);
        CharBytes("bmp", new byte[] { 0xc4, 0x80 }, true, (char)0x0100, Single);
        CharBytes("replacement", new byte[] { 0xef, 0xbf, 0xbd }, true, (char)0xfffd, Single);
        CharBytes("ffff", new byte[] { 0xef, 0xbf, 0xbf }, true, (char)0xffff, Single);
        CharBytes("pair", new byte[] { 0xf0, 0x9f, 0x98, 0x80 }, false, (char)0x0000, Single);
        CharBytes("malformed", new byte[] { 0xff }, false, (char)0x0000, Invalid);
        CharBytes("truncated", new byte[] { 0xe2, 0x82 }, false, (char)0x0000, Invalid);
        CharBytes("overlong", new byte[] { 0xc0, 0xaf }, false, (char)0x0000, Invalid);
        CharBytes("surrogate", new byte[] { 0xed, 0xa0, 0x80 }, false, (char)0x0000, Invalid);
        CharCase("long", new string('A', 300), false, (char)0);
        CharBytes("long", Encoding.UTF8.GetBytes(new string('A', 300)), false, (char)0, Single);
        NativeCase<nint>("nint.zero", "0", NumberStyles.Integer, 0, 0, true, (nint)42);
        NativeCase<nint>("nint.one", "1", NumberStyles.Integer, 0, 1, true, (nint)42);
        NativeCase<nint>("nint.empty", "", NumberStyles.Integer, 1, 0, true, (nint)42);
        NativeCase<nint>("nint.text", "x", NumberStyles.Integer, 1, 0, true, (nint)42);
        NativeCase<nint>("nint.whitespace", "  +1  ", NumberStyles.Integer, 0, 1, true, (nint)42);
        NativeCase<nint>("nint.min", "-9223372036854775808", NumberStyles.Integer, 0, nint.MinValue, true, (nint)42);
        NativeCase<nint>("nint.max", "9223372036854775807", NumberStyles.Integer, 0, nint.MaxValue, true, (nint)42);
        NativeCase<nint>("nint.below", "-9223372036854775809", NumberStyles.Integer, 2, 0, true, (nint)42);
        NativeCase<nint>("nint.above", "9223372036854775808", NumberStyles.Integer, 2, 0, true, (nint)42);
        NativeCase<nint>("nint.hex-max", "7FFFFFFFFFFFFFFF", NumberStyles.HexNumber, 0, nint.MaxValue, false, (nint)42);
        NativeCase<nint>("nint.hex-negative", "FFFFFFFFFFFFFFFF", NumberStyles.HexNumber, 0, -1, false, (nint)42);
        NativeCase<nint>("nint.binary", "101", NumberStyles.BinaryNumber, 0, 5, false, (nint)42);
        NativeCase<nuint>("nuint.zero", "0", NumberStyles.Integer, 0, 0, true, (nuint)42);
        NativeCase<nuint>("nuint.one", "1", NumberStyles.Integer, 0, 1, true, (nuint)42);
        NativeCase<nuint>("nuint.empty", "", NumberStyles.Integer, 1, 0, true, (nuint)42);
        NativeCase<nuint>("nuint.text", "x", NumberStyles.Integer, 1, 0, true, (nuint)42);
        NativeCase<nuint>("nuint.whitespace", "  +1  ", NumberStyles.Integer, 0, 1, true, (nuint)42);
        NativeCase<nuint>("nuint.high", "9223372036854775808", NumberStyles.Integer, 0, (nuint)(1UL << 63), true, (nuint)42);
        NativeCase<nuint>("nuint.max", "18446744073709551615", NumberStyles.Integer, 0, nuint.MaxValue, true, (nuint)42);
        NativeCase<nuint>("nuint.above", "18446744073709551616", NumberStyles.Integer, 2, 0, true, (nuint)42);
        NativeCase<nuint>("nuint.negative", "-1", NumberStyles.Integer, 2, 0, true, (nuint)42);
        NativeCase<nuint>("nuint.hex-max", "FFFFFFFFFFFFFFFF", NumberStyles.HexNumber, 0, nuint.MaxValue, false, (nuint)42);
        NativeCase<nuint>("nuint.binary", "101", NumberStyles.BinaryNumber, 0, 5, false, (nuint)42);
        ReplacementSign<nint>("replacement.nint", true, (nint)1, (nint)(-1), (nint)42);
        ReplacementSign<nuint>("replacement.nuint", false, (nuint)1, (nuint)0, (nuint)42);
        ReplacementSign<long>("replacement.long", true, (long)1, (long)-1, (long)42);
        ReplacementSign<ulong>("replacement.ulong", false, (ulong)1, (ulong)0, (ulong)42);
        Malformed<nint>("utf8.bad.continuation.nint", new byte[] { 0x80 }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.continuation.nuint", new byte[] { 0x80 }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.c1.nint", new byte[] { 0xc1, 0xbf }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.c1.nuint", new byte[] { 0xc1, 0xbf }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.bad-second.nint", new byte[] { 0xc2, 0x31 }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.bad-second.nuint", new byte[] { 0xc2, 0x31 }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.short2.nint", new byte[] { 0xc2 }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.short2.nuint", new byte[] { 0xc2 }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.e0-overlong.nint", new byte[] { 0xe0, 0x9f, 0xbf }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.e0-overlong.nuint", new byte[] { 0xe0, 0x9f, 0xbf }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.ed-surrogate.nint", new byte[] { 0xed, 0xa0, 0x80 }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.ed-surrogate.nuint", new byte[] { 0xed, 0xa0, 0x80 }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.short3.nint", new byte[] { 0xe1, 0x80 }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.short3.nuint", new byte[] { 0xe1, 0x80 }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.bad-third.nint", new byte[] { 0xe1, 0x80, 0x31 }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.bad-third.nuint", new byte[] { 0xe1, 0x80, 0x31 }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.f0-overlong.nint", new byte[] { 0xf0, 0x8f, 0xbf, 0xbf }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.f0-overlong.nuint", new byte[] { 0xf0, 0x8f, 0xbf, 0xbf }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.f4-too-high.nint", new byte[] { 0xf4, 0x90, 0x80, 0x80 }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.f4-too-high.nuint", new byte[] { 0xf4, 0x90, 0x80, 0x80 }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.f5.nint", new byte[] { 0xf5, 0x80, 0x80, 0x80 }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.f5.nuint", new byte[] { 0xf5, 0x80, 0x80, 0x80 }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.short4.nint", new byte[] { 0xf1, 0x80, 0x80 }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.short4.nuint", new byte[] { 0xf1, 0x80, 0x80 }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.bad-fourth.nint", new byte[] { 0xf1, 0x80, 0x80, 0x31 }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.bad-fourth.nuint", new byte[] { 0xf1, 0x80, 0x80, 0x31 }, CultureInfo.InvariantCulture, (nuint)42);
        Malformed<nint>("utf8.bad.suffix.nint", new byte[] { 0x31, 0xff }, CultureInfo.InvariantCulture, (nint)42);
        Malformed<nuint>("utf8.bad.suffix.nuint", new byte[] { 0x31, 0xff }, CultureInfo.InvariantCulture, (nuint)42);
        ValidSign("two-low", "\u0080", new byte[] { 0xc2, 0x80, 0x31 });
        ValidSign("two-high", "\u07ff", new byte[] { 0xdf, 0xbf, 0x31 });
        ValidSign("three-low", "\u0800", new byte[] { 0xe0, 0xa0, 0x80, 0x31 });
        ValidSign("before-surrogate", "\ud7ff", new byte[] { 0xed, 0x9f, 0xbf, 0x31 });
        ValidSign("after-surrogate", "\ue000", new byte[] { 0xee, 0x80, 0x80, 0x31 });
        ValidSign("three-high", "\uffff", new byte[] { 0xef, 0xbf, 0xbf, 0x31 });
        ValidSign("four-low", "\ud800\udc00", new byte[] { 0xf0, 0x90, 0x80, 0x80, 0x31 });
        ValidSign("four-high", "\udbff\udfff", new byte[] { 0xf4, 0x8f, 0xbf, 0xbf, 0x31 });
        bool quoted = false;
        try { ParseBytes<nint>(new byte[] { 0x78 }, NumberStyles.Integer, null); }
        catch (FormatException error) { quoted = error.Message == "The input string 'x' was not in a correct format."; }
        Check("utf8.valid-nonnumeric-message", quoted);
        Storage();
        ProviderOrder();
        Console.WriteLine("parse.checks=" + _checks);
        Console.WriteLine("== Primitive span parsing done ==");
    }
}
