#nullable disable
using System;
using System.Buffers.Binary;
using System.Text;

namespace BinaryPrimitivesSubset
{
    // System.Buffers.Binary.BinaryPrimitives surface: the span Read*/Write*
    // pairs in both endiannesses for every integer width plus Single/Double,
    // the TryRead*/TryWrite* forms (success and false on a short buffer, which
    // must leave the destination untouched), and ReverseEndianness for every
    // integer width including the sbyte/byte identity and sign behavior on
    // negative short/int/long values. Buffers are dumped as hex so byte order
    // is asserted exactly against real .NET.
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

        internal static void __GateEntry()
        {
            byte[] src = new byte[]
            {
                0x01, 0x02, 0x80, 0xFF, 0x12, 0x34, 0x56, 0x78,
                0x3F, 0xC0, 0x00, 0x00, 0x40, 0x04, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
            };
            ReadOnlySpan<byte> rs = src;

            Console.WriteLine("-- Read little-endian --");
            Console.WriteLine("i16 " + BinaryPrimitives.ReadInt16LittleEndian(rs.Slice(2)));
            Console.WriteLine("u16 " + BinaryPrimitives.ReadUInt16LittleEndian(rs.Slice(2)));
            Console.WriteLine("i32 " + BinaryPrimitives.ReadInt32LittleEndian(rs));
            Console.WriteLine("u32 " + BinaryPrimitives.ReadUInt32LittleEndian(rs.Slice(1)));
            Console.WriteLine("i64 " + BinaryPrimitives.ReadInt64LittleEndian(rs));
            Console.WriteLine("u64 " + BinaryPrimitives.ReadUInt64LittleEndian(rs.Slice(2)));
            Console.WriteLine("f32 " + BinaryPrimitives.ReadSingleLittleEndian(rs.Slice(4)));
            Console.WriteLine("f64 " + BinaryPrimitives.ReadDoubleLittleEndian(rs.Slice(8)));

            Console.WriteLine("-- Read big-endian --");
            Console.WriteLine("i16 " + BinaryPrimitives.ReadInt16BigEndian(rs.Slice(2)));
            Console.WriteLine("u16 " + BinaryPrimitives.ReadUInt16BigEndian(rs.Slice(2)));
            Console.WriteLine("i32 " + BinaryPrimitives.ReadInt32BigEndian(rs));
            Console.WriteLine("u32 " + BinaryPrimitives.ReadUInt32BigEndian(rs.Slice(1)));
            Console.WriteLine("i64 " + BinaryPrimitives.ReadInt64BigEndian(rs));
            Console.WriteLine("u64 " + BinaryPrimitives.ReadUInt64BigEndian(rs.Slice(2)));
            Console.WriteLine("f32 " + BinaryPrimitives.ReadSingleBigEndian(rs.Slice(8)));
            Console.WriteLine("f64 " + BinaryPrimitives.ReadDoubleBigEndian(rs.Slice(12)));

            Console.WriteLine("-- Write both endiannesses --");
            byte[] w = new byte[8];
            BinaryPrimitives.WriteInt16LittleEndian(w, (short)-2);
            BinaryPrimitives.WriteInt16BigEndian(w.AsSpan(2), (short)-2);
            BinaryPrimitives.WriteUInt16LittleEndian(w.AsSpan(4), (ushort)0xBEEF);
            BinaryPrimitives.WriteUInt16BigEndian(w.AsSpan(6), (ushort)0xBEEF);
            Console.WriteLine("16s " + Hex(w));
            BinaryPrimitives.WriteInt32LittleEndian(w, -305419897);
            BinaryPrimitives.WriteInt32BigEndian(w.AsSpan(4), -305419897);
            Console.WriteLine("i32 " + Hex(w));
            BinaryPrimitives.WriteUInt32LittleEndian(w, 0xDEADBEEFu);
            BinaryPrimitives.WriteUInt32BigEndian(w.AsSpan(4), 0xDEADBEEFu);
            Console.WriteLine("u32 " + Hex(w));
            BinaryPrimitives.WriteInt64LittleEndian(w, long.MinValue + 2);
            Console.WriteLine("i64le " + Hex(w));
            BinaryPrimitives.WriteInt64BigEndian(w, long.MinValue + 2);
            Console.WriteLine("i64be " + Hex(w));
            BinaryPrimitives.WriteUInt64LittleEndian(w, 0x1122334455667788UL);
            Console.WriteLine("u64le " + Hex(w));
            BinaryPrimitives.WriteUInt64BigEndian(w, 0x1122334455667788UL);
            Console.WriteLine("u64be " + Hex(w));
            BinaryPrimitives.WriteSingleLittleEndian(w, -1.5f);
            BinaryPrimitives.WriteSingleBigEndian(w.AsSpan(4), -1.5f);
            Console.WriteLine("f32 " + Hex(w));
            BinaryPrimitives.WriteDoubleLittleEndian(w, -2.25);
            Console.WriteLine("f64le " + Hex(w));
            BinaryPrimitives.WriteDoubleBigEndian(w, -2.25);
            Console.WriteLine("f64be " + Hex(w));

            Console.WriteLine("-- TryRead/TryWrite --");
            Console.WriteLine("tryread i32 ok " + BinaryPrimitives.TryReadInt32LittleEndian(rs, out int ri32) + " " + ri32);
            Console.WriteLine("tryread i32 short " + BinaryPrimitives.TryReadInt32LittleEndian(rs.Slice(17), out ri32) + " " + ri32);
            Console.WriteLine("tryread i16 ok " + BinaryPrimitives.TryReadInt16BigEndian(rs.Slice(2), out short ri16) + " " + ri16);
            Console.WriteLine("tryread i16 short " + BinaryPrimitives.TryReadInt16BigEndian(rs.Slice(19), out ri16) + " " + ri16);
            Console.WriteLine("tryread u64 ok " + BinaryPrimitives.TryReadUInt64BigEndian(rs, out ulong ru64) + " " + ru64);
            Console.WriteLine("tryread u64 short " + BinaryPrimitives.TryReadUInt64BigEndian(rs.Slice(13), out ru64) + " " + ru64);
            Console.WriteLine("tryread f64 ok " + BinaryPrimitives.TryReadDoubleLittleEndian(rs.Slice(8), out double rf64) + " " + rf64);
            Console.WriteLine("tryread f64 short " + BinaryPrimitives.TryReadDoubleLittleEndian(rs.Slice(13), out rf64) + " " + rf64);
            byte[] t4 = new byte[4];
            Console.WriteLine("trywrite i32 ok " + BinaryPrimitives.TryWriteInt32BigEndian(t4, -305419897) + " " + Hex(t4));
            Console.WriteLine("trywrite i32 short " + BinaryPrimitives.TryWriteInt32BigEndian(t4.AsSpan(1), 7) + " " + Hex(t4));
            Console.WriteLine("trywrite u16 ok " + BinaryPrimitives.TryWriteUInt16LittleEndian(t4.AsSpan(2), (ushort)0xBEEF) + " " + Hex(t4));
            Console.WriteLine("trywrite u16 short " + BinaryPrimitives.TryWriteUInt16LittleEndian(t4.AsSpan(3), (ushort)1) + " " + Hex(t4));
            Console.WriteLine("trywrite i64 short " + BinaryPrimitives.TryWriteInt64LittleEndian(t4, 7L) + " " + Hex(t4));
            Console.WriteLine("trywrite f32 ok " + BinaryPrimitives.TryWriteSingleLittleEndian(t4, 1.5f) + " " + Hex(t4));
            Console.WriteLine("trywrite f64 short " + BinaryPrimitives.TryWriteDoubleBigEndian(t4, 1.5) + " " + Hex(t4));

            Console.WriteLine("-- ReverseEndianness --");
            Console.WriteLine("byte 0x80 " + BinaryPrimitives.ReverseEndianness((byte)0x80));
            Console.WriteLine("sbyte -128 " + BinaryPrimitives.ReverseEndianness((sbyte)-128));
            Console.WriteLine("short 0x0102 " + BinaryPrimitives.ReverseEndianness((short)0x0102));
            Console.WriteLine("short -2 " + BinaryPrimitives.ReverseEndianness((short)-2));
            Console.WriteLine("ushort 0xBEEF " + BinaryPrimitives.ReverseEndianness((ushort)0xBEEF));
            Console.WriteLine("int 0x01020304 " + BinaryPrimitives.ReverseEndianness(0x01020304));
            Console.WriteLine("int -2 " + BinaryPrimitives.ReverseEndianness(-2));
            Console.WriteLine("uint 0xDEADBEEF " + BinaryPrimitives.ReverseEndianness(0xDEADBEEFu));
            Console.WriteLine("long 0x0102030405060708 " + BinaryPrimitives.ReverseEndianness(0x0102030405060708L));
            Console.WriteLine("long -2 " + BinaryPrimitives.ReverseEndianness(-2L));
            Console.WriteLine("ulong 0x1122334455667788 " + BinaryPrimitives.ReverseEndianness(0x1122334455667788UL));
            Console.WriteLine("roundtrip int " + BinaryPrimitives.ReverseEndianness(BinaryPrimitives.ReverseEndianness(-305419897)));
        }
    }
}
