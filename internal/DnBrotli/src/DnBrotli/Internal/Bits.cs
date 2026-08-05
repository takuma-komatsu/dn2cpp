// Port of the load/store and bit-scan macros from c/common/platform.h
// (brotli v1.1.0).
//
// Choices, per PORTING.md:
//   - brotli_reg_t is nuint; every dn2cpp target is 64-bit, so the
//     BROTLI_64_BITS branches of the C are the only ones ported.
//   - Little-endian is assumed (and asserted once here): the
//     BROTLI_UNALIGNED_LOAD*LE macros map straight to Unsafe.ReadUnaligned.
//   - BROTLI_UNALIGNED_READ_FAST is treated as true (Unsafe.ReadUnaligned is
//     a plain load on every dn2cpp target).

using System.Runtime.CompilerServices;
using System.Numerics;

namespace DnBrotli.Internal;

internal static unsafe class Bits
{
    static Bits()
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new PlatformNotSupportedException("DnBrotli requires a little-endian target.");
        }
    }

    /// <summary><c>BROTLI_UNALIGNED_LOAD16LE</c> (<c>BrotliUnalignedRead16</c>).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort BROTLI_UNALIGNED_LOAD16LE(void* p)
    {
        return Unsafe.ReadUnaligned<ushort>(p);
    }

    /// <summary><c>BROTLI_UNALIGNED_LOAD32LE</c> (<c>BrotliUnalignedRead32</c>).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint BROTLI_UNALIGNED_LOAD32LE(void* p)
    {
        return Unsafe.ReadUnaligned<uint>(p);
    }

    /// <summary><c>BROTLI_UNALIGNED_LOAD64LE</c> (<c>BrotliUnalignedRead64</c>).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong BROTLI_UNALIGNED_LOAD64LE(void* p)
    {
        return Unsafe.ReadUnaligned<ulong>(p);
    }

    /// <summary><c>BROTLI_UNALIGNED_STORE64LE</c> (<c>BrotliUnalignedWrite64</c>).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BROTLI_UNALIGNED_STORE64LE(void* p, ulong v)
    {
        Unsafe.WriteUnaligned(p, v);
    }

    /// <summary><c>BROTLI_BSR32(x)</c>: index of the highest set bit; <paramref name="x"/>
    /// must be non-zero.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint BROTLI_BSR32(uint x)
    {
        return (uint)BitOperations.Log2(x);
    }

    /// <summary><c>Log2FloorNonZero</c> from <c>c/enc/fast_log.h</c> — shared here because
    /// both coder lanes use it. Matches the C <c>BROTLI_BSR32((uint32_t)n)</c> path.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Log2FloorNonZero(nuint n)
    {
        return BROTLI_BSR32((uint)n);
    }

    /// <summary><c>BROTLI_TZCNT64</c>: number of trailing zero bits; <paramref name="x"/>
    /// must be non-zero.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint BROTLI_TZCNT64(ulong x)
    {
        return (uint)BitOperations.TrailingZeroCount(x);
    }
}
