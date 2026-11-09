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

    private static void Check(string label, bool condition)
    {
        if (!condition) throw new InvalidOperationException("parse." + label);
        _checks++;
        Console.WriteLine("parse." + label + "=ok");
    }

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

    private static void Effect(string label, int index) { _order += label; if (_factoryFailure == index) throw new FactoryFailure(); }
    private static ReadOnlySpan<char> InputChars() { Effect("I", 0); return "1".AsSpan(); }
    private static ReadOnlySpan<byte> InputBytes() { Effect("I", 0); return _bytes; }
    private static NumberStyles Style(NumberStyles value) { Effect("Y", 1); return value; }
    private static IFormatProvider ProviderArg(bool throws) { Effect("P", 2); return new Provider(throws); }
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
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== Primitive span parsing managed bodies ==");
        ProviderOrder();
        Console.WriteLine("parse.checks=" + _checks);
        Console.WriteLine("== Primitive span parsing done ==");
    }
}
