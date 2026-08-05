using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DnZlib.Deflate;

/// <summary>
/// A deflate Huffman tree node — the C# mirror of zlib's 4-byte <c>ct_data</c> union (deflate.h).
/// Two overlapping <see cref="ushort"/> slots reused across the two phases of tree construction.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 4)]
internal struct CtData
{
    /// <summary>First slot: frequency count (while counting) or bit code (after gen_codes).</summary>
    public ushort Fc;

    /// <summary>Second slot: father node (while building) or bit length (after gen_bitlen).</summary>
    public ushort Dl;

    /// <summary>Frequency count alias of <see cref="Fc"/>.</summary>
    public ushort Freq
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] readonly get => Fc;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Fc = value;
    }

    /// <summary>Bit-code alias of <see cref="Fc"/>.</summary>
    public ushort Code
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] readonly get => Fc;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Fc = value;
    }

    /// <summary>Father-node alias of <see cref="Dl"/>.</summary>
    public ushort Dad
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] readonly get => Dl;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Dl = value;
    }

    /// <summary>Bit-length alias of <see cref="Dl"/>.</summary>
    public ushort Len
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] readonly get => Dl;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Dl = value;
    }
}
