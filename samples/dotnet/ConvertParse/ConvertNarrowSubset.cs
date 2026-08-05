#nullable disable
using System;

namespace ConvertNarrowSubset
{
    // The sub-word / unsigned Convert.To* families (ToByte/ToSByte/ToInt16/
    // ToUInt16/ToUInt32/ToUInt64): banker's rounding for floating and decimal
    // sources followed by the range check (254.5 -> 254 but 255.5 rounds to the
    // even 256 and overflows), overflow in both directions from every integer
    // width, string sources (a null string yields 0, unlike Parse), bool/char
    // sources, boxed-object sources, and the thrown exception types.
    internal static class Program
    {
        private static void Show<T>(string tag, Func<T> f)
        {
            try { Console.WriteLine(tag + " = " + f()); }
            catch (Exception ex) { Console.WriteLine(tag + " ! " + ex.GetType().Name); }
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("-- double/float rounding (half to even) --");
            Show("byte 254.5", () => Convert.ToByte(254.5));
            Show("byte 255.5", () => Convert.ToByte(255.5));
            Show("byte 255.49", () => Convert.ToByte(255.49));
            Show("byte -0.5", () => Convert.ToByte(-0.5));
            Show("byte -0.51", () => Convert.ToByte(-0.51));
            Show("byte 0.5", () => Convert.ToByte(0.5));
            Show("byte 1.5", () => Convert.ToByte(1.5));
            Show("byte 254.5f", () => Convert.ToByte(254.5f));
            Show("byte 255.5f", () => Convert.ToByte(255.5f));
            Show("sbyte -128.5", () => Convert.ToSByte(-128.5));
            Show("sbyte -128.51", () => Convert.ToSByte(-128.51));
            Show("sbyte 127.5", () => Convert.ToSByte(127.5));
            Show("short 32767.5", () => Convert.ToInt16(32767.5));
            Show("short -32768.5", () => Convert.ToInt16(-32768.5));
            Show("ushort 65535.5", () => Convert.ToUInt16(65535.5));
            Show("uint 4294967295.0", () => Convert.ToUInt32(4294967295.0));
            Show("uint 4294967296.0", () => Convert.ToUInt32(4294967296.0));
            Show("uint -0.5", () => Convert.ToUInt32(-0.5));
            Show("uint -0.51", () => Convert.ToUInt32(-0.51));
            Show("ulong 18446744073709549568.0", () => Convert.ToUInt64(18446744073709549568.0));
            Show("ulong 1.8446744073709552E19", () => Convert.ToUInt64(1.8446744073709552E19));
            Show("ulong -0.5", () => Convert.ToUInt64(-0.5));
            Show("ulong -0.51", () => Convert.ToUInt64(-0.51));
            Show("ulong 2.5", () => Convert.ToUInt64(2.5));
            Show("ulong 3.5", () => Convert.ToUInt64(3.5));

            Console.WriteLine("-- decimal sources --");
            Show("byte 254.5m", () => Convert.ToByte(254.5m));
            Show("byte 255.5m", () => Convert.ToByte(255.5m));
            Show("byte -0.5m", () => Convert.ToByte(-0.5m));
            Show("byte -0.51m", () => Convert.ToByte(-0.51m));
            Show("sbyte -128.5m", () => Convert.ToSByte(-128.5m));
            Show("short 2.5m", () => Convert.ToInt16(2.5m));
            Show("short 3.5m", () => Convert.ToInt16(3.5m));
            Show("uint 4294967295.4m", () => Convert.ToUInt32(4294967295.4m));
            Show("uint 4294967295.5m", () => Convert.ToUInt32(4294967295.5m));
            Show("ulong max m", () => Convert.ToUInt64(18446744073709551615m));
            Show("ulong max.4 m", () => Convert.ToUInt64(18446744073709551615.4m));
            Show("ulong max.5 m", () => Convert.ToUInt64(18446744073709551615.5m));
            Show("ulong -0.5m", () => Convert.ToUInt64(-0.5m));

            Console.WriteLine("-- integer sources --");
            Show("byte 256", () => Convert.ToByte(256));
            Show("byte -1", () => Convert.ToByte(-1));
            Show("byte byte7", () => Convert.ToByte((byte)7));
            Show("byte ulong.Max", () => Convert.ToByte(ulong.MaxValue));
            Show("sbyte 128", () => Convert.ToSByte(128));
            Show("sbyte -129L", () => Convert.ToSByte(-129L));
            Show("short 32768", () => Convert.ToInt16(32768));
            Show("ushort -1", () => Convert.ToUInt16(-1));
            Show("ushort 65536L", () => Convert.ToUInt16(65536L));
            Show("uint -1", () => Convert.ToUInt32(-1));
            Show("uint 4294967296L", () => Convert.ToUInt32(4294967296L));
            Show("uint uint.Max", () => Convert.ToUInt32(uint.MaxValue));
            Show("ulong -1", () => Convert.ToUInt64(-1));
            Show("ulong -1L", () => Convert.ToUInt64(-1L));
            Show("ulong ulong.Max", () => Convert.ToUInt64(ulong.MaxValue));
            Show("ulong long.Max", () => Convert.ToUInt64(long.MaxValue));

            Console.WriteLine("-- bool/char sources --");
            Show("byte true", () => Convert.ToByte(true));
            Show("byte false", () => Convert.ToByte(false));
            Show("sbyte true", () => Convert.ToSByte(true));
            Show("ushort true", () => Convert.ToUInt16(true));
            Show("uint true", () => Convert.ToUInt32(true));
            Show("ulong true", () => Convert.ToUInt64(true));
            Show("byte 'A'", () => Convert.ToByte('A'));
            Show("byte u0100", () => Convert.ToByte('\u0100'));
            Show("sbyte 'A'", () => Convert.ToSByte('A'));
            Show("sbyte u0080", () => Convert.ToSByte('\u0080'));
            Show("short uFFFF", () => Convert.ToInt16('\uFFFF'));
            Show("ushort uFFFF", () => Convert.ToUInt16('\uFFFF'));
            Show("uint 'A'", () => Convert.ToUInt32('A'));
            Show("ulong 'A'", () => Convert.ToUInt64('A'));

            Console.WriteLine("-- string sources (null -> 0) --");
            Show("byte null", () => Convert.ToByte((string)null));
            Show("byte \"255\"", () => Convert.ToByte("255"));
            Show("byte \"256\"", () => Convert.ToByte("256"));
            Show("byte \"\"", () => Convert.ToByte(""));
            Show("byte \" 12 \"", () => Convert.ToByte(" 12 "));
            Show("byte \"+9\"", () => Convert.ToByte("+9"));
            Show("byte \"12.0\"", () => Convert.ToByte("12.0"));
            Show("byte prov", () => Convert.ToByte("200", System.Globalization.CultureInfo.InvariantCulture));
            Show("sbyte \"-128\"", () => Convert.ToSByte("-128"));
            Show("sbyte null", () => Convert.ToSByte((string)null));
            Show("short \"-32768\"", () => Convert.ToInt16("-32768"));
            Show("ushort \"65535\"", () => Convert.ToUInt16("65535"));
            Show("uint null", () => Convert.ToUInt32((string)null));
            Show("uint \"-1\"", () => Convert.ToUInt32("-1"));
            Show("uint \"4294967295\"", () => Convert.ToUInt32("4294967295"));
            Show("ulong \"18446744073709551615\"", () => Convert.ToUInt64("18446744073709551615"));
            Show("ulong \"18446744073709551616\"", () => Convert.ToUInt64("18446744073709551616"));
            Show("ulong null", () => Convert.ToUInt64((string)null));

            Console.WriteLine("-- boxed object sources --");
            Show("byte (object)null", () => Convert.ToByte((object)null));
            Show("byte (object)300", () => Convert.ToByte((object)300));
            Show("byte (object)254.5", () => Convert.ToByte((object)254.5));
            Show("byte (object)\"77\"", () => Convert.ToByte((object)"77"));
            Show("sbyte (object)true", () => Convert.ToSByte((object)true));
            Show("short (object)'A'", () => Convert.ToInt16((object)'A'));
            Show("ushort (object)-1", () => Convert.ToUInt16((object)(-1)));
            Show("uint (object)-1", () => Convert.ToUInt32((object)(-1)));
            Show("uint (object)7", () => Convert.ToUInt32((object)7));
        }
    }
}
