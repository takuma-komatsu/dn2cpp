#nullable disable
using System;
using System.Text;

namespace BitConverterSubset
{
    // System.BitConverter surface: GetBytes for every width, the To*(byte[],
    // startIndex) readers (including nonzero offsets and the thrown exception
    // types for bad offsets), the bit-reinterpretation pairs (Double/Int64,
    // Single/Int32, Half/Int16 plus their unsigned variants), the hex
    // ToString overloads, IsLittleEndian, and the span-based TryWriteBytes
    // overloads (success and false-on-short-span). Byte output is dumped as
    // hex so the diff vs real .NET is exact and endian-explicit.
    internal static class Program
    {
        private static string Hex(byte[] b)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < b.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(b[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private static void Show<T>(string tag, Func<T> f)
        {
            try { Console.WriteLine(tag + " = " + f()); }
            catch (Exception ex) { Console.WriteLine(tag + " ! " + ex.GetType().Name); }
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("-- GetBytes --");
            Console.WriteLine("bool true    " + Hex(BitConverter.GetBytes(true)));
            Console.WriteLine("bool false   " + Hex(BitConverter.GetBytes(false)));
            Console.WriteLine("char 'A'     " + Hex(BitConverter.GetBytes('A')));
            Console.WriteLine("char u1234   " + Hex(BitConverter.GetBytes('ሴ')));
            Console.WriteLine("short -2     " + Hex(BitConverter.GetBytes((short)-2)));
            Console.WriteLine("ushort BEEF  " + Hex(BitConverter.GetBytes((ushort)0xBEEF)));
            Console.WriteLine("int -305419897 " + Hex(BitConverter.GetBytes(-305419897)));
            Console.WriteLine("uint 0xDEADBEEF " + Hex(BitConverter.GetBytes(0xDEADBEEFu)));
            Console.WriteLine("long min+2   " + Hex(BitConverter.GetBytes(long.MinValue + 2)));
            Console.WriteLine("ulong 0x1122334455667788 " + Hex(BitConverter.GetBytes(0x1122334455667788UL)));
            Console.WriteLine("float 1.5f   " + Hex(BitConverter.GetBytes(1.5f)));
            Console.WriteLine("float -0f    " + Hex(BitConverter.GetBytes(-0.0f)));
            Console.WriteLine("float NaN    " + Hex(BitConverter.GetBytes(float.NaN)));
            Console.WriteLine("double -2.25 " + Hex(BitConverter.GetBytes(-2.25)));
            Console.WriteLine("double NaN   " + Hex(BitConverter.GetBytes(double.NaN)));
            Console.WriteLine("half 1.5     " + Hex(BitConverter.GetBytes((Half)1.5f)));
            Console.WriteLine("half NaN     " + Hex(BitConverter.GetBytes(Half.NaN)));
            Console.WriteLine("half -inf    " + Hex(BitConverter.GetBytes(Half.NegativeInfinity)));

            Console.WriteLine("-- To* readers --");
            byte[] buf = new byte[]
            {
                0x01, 0x00, 0xFE, 0xFF, 0x78, 0x56, 0x34, 0x12,
                0x00, 0x00, 0xC0, 0x3F, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x02, 0xC0,
            };
            Show("ToBoolean 0", () => BitConverter.ToBoolean(buf, 0));
            Show("ToBoolean 1", () => BitConverter.ToBoolean(buf, 1));
            Show("ToBoolean 2", () => BitConverter.ToBoolean(buf, 2));
            Show("ToChar 0", () => (int)BitConverter.ToChar(buf, 0));
            Show("ToChar 4", () => (int)BitConverter.ToChar(buf, 4));
            Show("ToInt16 2", () => BitConverter.ToInt16(buf, 2));
            Show("ToInt16 3", () => BitConverter.ToInt16(buf, 3));
            Show("ToUInt16 2", () => BitConverter.ToUInt16(buf, 2));
            Show("ToInt32 4", () => BitConverter.ToInt32(buf, 4));
            Show("ToInt32 1", () => BitConverter.ToInt32(buf, 1));
            Show("ToUInt32 2", () => BitConverter.ToUInt32(buf, 2));
            Show("ToInt64 0", () => BitConverter.ToInt64(buf, 0));
            Show("ToInt64 5", () => BitConverter.ToInt64(buf, 5));
            Show("ToUInt64 4", () => BitConverter.ToUInt64(buf, 4));
            Show("ToSingle 8", () => BitConverter.ToSingle(buf, 8));
            Show("ToDouble 12", () => BitConverter.ToDouble(buf, 12));
            Show("ToHalf 8", () => BitConverter.ToHalf(buf, 8));
            Console.WriteLine("-- To* bad offsets --");
            Show("ToInt32 -1", () => BitConverter.ToInt32(buf, -1));
            Show("ToInt32 17", () => BitConverter.ToInt32(buf, 17));
            Show("ToInt32 20", () => BitConverter.ToInt32(buf, 20));
            Show("ToInt64 13", () => BitConverter.ToInt64(buf, 13));
            Show("ToInt16 19", () => BitConverter.ToInt16(buf, 19));

            Console.WriteLine("-- bit reinterpretation --");
            Show("DoubleToInt64Bits -2.5", () => BitConverter.DoubleToInt64Bits(-2.5).ToString("X16"));
            Show("DoubleToUInt64Bits -2.5", () => BitConverter.DoubleToUInt64Bits(-2.5).ToString("X16"));
            Show("Int64BitsToDouble", () => BitConverter.Int64BitsToDouble(0x4004000000000000L));
            Show("UInt64BitsToDouble", () => BitConverter.UInt64BitsToDouble(0xC000000000000000UL));
            Show("SingleToInt32Bits -1.5f", () => BitConverter.SingleToInt32Bits(-1.5f).ToString("X8"));
            Show("SingleToUInt32Bits 2f", () => BitConverter.SingleToUInt32Bits(2.0f).ToString("X8"));
            Show("Int32BitsToSingle", () => BitConverter.Int32BitsToSingle(0x3F800000));
            Show("UInt32BitsToSingle", () => BitConverter.UInt32BitsToSingle(0xBF000000u));
            Show("HalfToInt16Bits -1.5", () => BitConverter.HalfToInt16Bits((Half)(-1.5f)).ToString("X4"));
            Show("HalfToUInt16Bits 1.5", () => BitConverter.HalfToUInt16Bits((Half)1.5f).ToString("X4"));
            Show("Int16BitsToHalf", () => BitConverter.Int16BitsToHalf(0x3C00));
            Show("UInt16BitsToHalf", () => BitConverter.UInt16BitsToHalf(0xC200));

            Console.WriteLine("-- ToString --");
            byte[] hexed = new byte[] { 0x00, 0x1A, 0xFF, 0x7B, 0x80 };
            Show("ToString(b)", () => BitConverter.ToString(hexed));
            Show("ToString(b,2)", () => BitConverter.ToString(hexed, 2));
            Show("ToString(b,1,3)", () => BitConverter.ToString(hexed, 1, 3));
            Show("ToString(b,5)", () => BitConverter.ToString(hexed, 5));
            Show("ToString(b,0,0)", () => BitConverter.ToString(hexed, 0, 0));
            Show("ToString(empty)", () => BitConverter.ToString(Array.Empty<byte>()));
            Show("ToString(b,1,5)", () => BitConverter.ToString(hexed, 1, 5));
            Show("ToString(b,-1)", () => BitConverter.ToString(hexed, -1));

            Console.WriteLine("-- IsLittleEndian --");
            Console.WriteLine("IsLittleEndian = " + BitConverter.IsLittleEndian);

            Console.WriteLine("-- TryWriteBytes --");
            byte[] dst = new byte[8];
            Span<byte> s8 = dst;
            Console.WriteLine("bool   " + BitConverter.TryWriteBytes(s8, true) + " " + Hex(dst));
            Console.WriteLine("char   " + BitConverter.TryWriteBytes(s8, 'A') + " " + Hex(dst));
            Console.WriteLine("short  " + BitConverter.TryWriteBytes(s8, (short)-2) + " " + Hex(dst));
            Console.WriteLine("ushort " + BitConverter.TryWriteBytes(s8, (ushort)0xBEEF) + " " + Hex(dst));
            Console.WriteLine("int    " + BitConverter.TryWriteBytes(s8, -305419897) + " " + Hex(dst));
            Console.WriteLine("uint   " + BitConverter.TryWriteBytes(s8, 0xDEADBEEFu) + " " + Hex(dst));
            Console.WriteLine("long   " + BitConverter.TryWriteBytes(s8, long.MinValue + 2) + " " + Hex(dst));
            Console.WriteLine("ulong  " + BitConverter.TryWriteBytes(s8, 0x1122334455667788UL) + " " + Hex(dst));
            Console.WriteLine("float  " + BitConverter.TryWriteBytes(s8, 1.5f) + " " + Hex(dst));
            Console.WriteLine("double " + BitConverter.TryWriteBytes(s8, -2.25) + " " + Hex(dst));
            Console.WriteLine("half   " + BitConverter.TryWriteBytes(s8, (Half)(-1.5f)) + " " + Hex(dst));
            byte[] tiny = new byte[3];
            Console.WriteLine("int->3   " + BitConverter.TryWriteBytes(tiny, 7) + " " + Hex(tiny));
            Console.WriteLine("short->3 " + BitConverter.TryWriteBytes(tiny, (short)7) + " " + Hex(tiny));
            Console.WriteLine("long->3  " + BitConverter.TryWriteBytes(tiny, 7L) + " " + Hex(tiny));
            Console.WriteLine("double->3 " + BitConverter.TryWriteBytes(tiny, 7.0) + " " + Hex(tiny));
            Console.WriteLine("half->3  " + BitConverter.TryWriteBytes(tiny, (Half)7.0f) + " " + Hex(tiny));
            Console.WriteLine("bool->0  " + BitConverter.TryWriteBytes(Span<byte>.Empty, true));
        }
    }
}
