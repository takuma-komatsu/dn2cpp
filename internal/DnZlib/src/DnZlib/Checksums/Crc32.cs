using System.Runtime.CompilerServices;
using DnZlib.Checksums;

namespace DnZlib;

/// <summary>
/// CRC-32 (reflected polynomial <c>0xEDB88320</c>), the checksum used by the gzip container.
/// Slice-by-16 implementation.
/// </summary>
public static class Crc32
{
    /// <summary>Compute the CRC-32 of <paramref name="data"/> (initial value 0).</summary>
    public static uint Compute(ReadOnlySpan<byte> data) => Update(0, data);

    /// <summary>Continue a running CRC-32 with more <paramref name="data"/>.</summary>
    public static uint Update(uint crc, ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return crc;
        unsafe
        {
            fixed (byte* p = data)
                return Update(crc, p, (nuint)data.Length);
        }
    }

    /// <summary>
    /// Combine two CRC-32 values as if their data streams were concatenated, where
    /// <paramref name="len2"/> is the byte length of the second stream.
    /// </summary>
    public static uint Combine(uint crc1, uint crc2, long len2)
        => Crc32Tables.MultModP(Crc32Tables.X2nModP(len2, 3), crc1) ^ crc2;

    /// <summary>Core update over a raw buffer.</summary>
    internal static unsafe uint Update(uint crc, byte* buf, nuint len) => UpdateScalar(crc, buf, len);

    /// <summary>Portable slice-by-16 implementation (little-endian word loads).</summary>
    internal static unsafe uint UpdateScalar(uint crc, byte* buf, nuint len)
    {
        uint c = crc ^ 0xFFFFFFFFu;

        fixed (uint* t = Crc32Tables.Slice)
        {
            while (len >= 16)
            {
                uint one = Unsafe.ReadUnaligned<uint>(buf) ^ c;
                uint two = Unsafe.ReadUnaligned<uint>(buf + 4);
                uint three = Unsafe.ReadUnaligned<uint>(buf + 8);
                uint four = Unsafe.ReadUnaligned<uint>(buf + 12);

                c = t[0 * 256 + ((four >> 24) & 0xFF)] ^ t[1 * 256 + ((four >> 16) & 0xFF)]
                  ^ t[2 * 256 + ((four >> 8) & 0xFF)] ^ t[3 * 256 + (four & 0xFF)]
                  ^ t[4 * 256 + ((three >> 24) & 0xFF)] ^ t[5 * 256 + ((three >> 16) & 0xFF)]
                  ^ t[6 * 256 + ((three >> 8) & 0xFF)] ^ t[7 * 256 + (three & 0xFF)]
                  ^ t[8 * 256 + ((two >> 24) & 0xFF)] ^ t[9 * 256 + ((two >> 16) & 0xFF)]
                  ^ t[10 * 256 + ((two >> 8) & 0xFF)] ^ t[11 * 256 + (two & 0xFF)]
                  ^ t[12 * 256 + ((one >> 24) & 0xFF)] ^ t[13 * 256 + ((one >> 16) & 0xFF)]
                  ^ t[14 * 256 + ((one >> 8) & 0xFF)] ^ t[15 * 256 + (one & 0xFF)];

                buf += 16;
                len -= 16;
            }

            while (len != 0)
            {
                c = t[(c ^ *buf++) & 0xFF] ^ (c >> 8);
                len--;
            }
        }

        return c ^ 0xFFFFFFFFu;
    }
}
