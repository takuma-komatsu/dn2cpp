namespace DnZlib.Checksums;

/// <summary>
/// Precomputed tables for CRC-32 with the reflected polynomial <c>0xEDB88320</c>: a slice-by-16
/// lookup table for the scalar fast path, plus the powers-of-x table used by <c>Combine</c>.
/// Generated once at static init from the polynomial (we do not embed zlib's ~16&#8239;KB blob).
/// </summary>
internal static class Crc32Tables
{
    /// <summary>The CRC-32 polynomial, reflected, with x^32 implied.</summary>
    public const uint Poly = 0xEDB88320u;

    /// <summary>
    /// Flat slice-by-16 table: 16 slices × 256 entries. Entry for slice <c>s</c>, byte <c>b</c> is at
    /// index <c>s * 256 + b</c>. Slice 0 is the classic byte-wise table.
    /// </summary>
    public static readonly uint[] Slice = BuildSlice();

    private static readonly uint[] X2nTable = BuildX2n();

    private static uint[] BuildSlice()
    {
        var t = new uint[16 * 256];

        // Slice 0: CRC of each byte value.
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? (c >> 1) ^ Poly : c >> 1;
            t[n] = c;
        }

        // Slices 1..15: fold one more zero byte through slice 0 each step.
        for (uint n = 0; n < 256; n++)
        {
            uint c = t[n];
            for (int slice = 1; slice < 16; slice++)
            {
                c = t[c & 0xFF] ^ (c >> 8);
                t[slice * 256 + n] = c;
            }
        }

        return t;
    }

    private static uint[] BuildX2n()
    {
        var x = new uint[32];
        uint p = 1u << 30; // x^1
        x[0] = p;
        for (int n = 1; n < 32; n++)
            x[n] = p = MultModP(p, p);
        return x;
    }

    /// <summary>Multiply <c>a(x)·b(x) mod p(x)</c> in reflected form. Requires <paramref name="a"/> != 0.</summary>
    public static uint MultModP(uint a, uint b)
    {
        uint m = 1u << 31;
        uint p = 0;
        for (;;)
        {
            if ((a & m) != 0)
            {
                p ^= b;
                if ((a & (m - 1)) == 0)
                    break;
            }
            m >>= 1;
            b = (b & 1) != 0 ? (b >> 1) ^ Poly : b >> 1;
        }
        return p;
    }

    /// <summary>Return <c>x^(n · 2^k) mod p(x)</c>. <paramref name="n"/> must not be negative.</summary>
    public static uint X2nModP(long n, uint k)
    {
        uint p = 1u << 31; // x^0 == 1
        while (n != 0)
        {
            if ((n & 1) != 0)
                p = MultModP(X2nTable[k & 31], p);
            n >>= 1;
            k++;
        }
        return p;
    }
}
