using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace DnZlib.Deflate;

/// <summary>
/// Returns the number of equal leading bytes (0..256) of two buffers — the vectorized inner match
/// comparison used by <c>longest_match</c>. Produces the same length as a byte-by-byte compare, so
/// the compressed output is unchanged; it is merely faster.
/// </summary>
internal static unsafe class Compare256Helper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Match(byte* s0, byte* s1)
    {
        if (Vector128.IsHardwareAccelerated)
            return MatchVector(s0, s1);
        return MatchScalar(s0, s1);
    }

    private static uint MatchScalar(byte* s0, byte* s1)
    {
        uint len = 0;
        while (len < 256)
        {
            ulong x = Unsafe.ReadUnaligned<ulong>(s0 + len) ^ Unsafe.ReadUnaligned<ulong>(s1 + len);
            if (x != 0)
                return len + (uint)(BitOperations.TrailingZeroCount(x) >> 3);
            len += 8;
        }
        return 256;
    }

    // Cross-platform Vector128<byte>: the JIT lowers Equals/ExtractMostSignificantBits to NEON on
    // arm64 and SSE2 on x86 from this one implementation — no architecture-specific code.
    private static uint MatchVector(byte* s0, byte* s1)
    {
        uint len = 0;
        while (len < 256)
        {
            Vector128<byte> a = Vector128.Load(s0 + len);
            Vector128<byte> b = Vector128.Load(s1 + len);
            uint mask = Vector128.Equals(a, b).ExtractMostSignificantBits();
            if (mask != 0xFFFF)
                return len + (uint)BitOperations.TrailingZeroCount(~mask);
            len += 16;
        }
        return 256;
    }

    /// <summary>Same as <see cref="Match"/>, but against a single repeated byte value (RLE runs).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint MatchByte(byte* s0, byte value)
    {
        if (Vector128.IsHardwareAccelerated)
            return MatchByteVector(s0, value);
        return MatchByteScalar(s0, value);
    }

    private static uint MatchByteScalar(byte* s0, byte value)
    {
        ulong pattern = 0x0101010101010101UL * value;
        uint len = 0;
        while (len < 256)
        {
            ulong x = Unsafe.ReadUnaligned<ulong>(s0 + len) ^ pattern;
            if (x != 0)
                return len + (uint)(BitOperations.TrailingZeroCount(x) >> 3);
            len += 8;
        }
        return 256;
    }

    private static uint MatchByteVector(byte* s0, byte value)
    {
        Vector128<byte> b = Vector128.Create(value);
        uint len = 0;
        while (len < 256)
        {
            Vector128<byte> a = Vector128.Load(s0 + len);
            uint mask = Vector128.Equals(a, b).ExtractMostSignificantBits();
            if (mask != 0xFFFF)
                return len + (uint)BitOperations.TrailingZeroCount(~mask);
            len += 16;
        }
        return 256;
    }
}
