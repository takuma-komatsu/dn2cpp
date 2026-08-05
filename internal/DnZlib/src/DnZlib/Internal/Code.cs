using System.Runtime.InteropServices;

namespace DnZlib.Internal;

/// <summary>
/// One inflate Huffman decode-table entry — the C# mirror of zlib's 4-byte <c>code</c> struct
/// (inftrees.h). Kept exactly 4 bytes so a <c>Code*</c> table indexes like the C original and the
/// fast loop performs one aligned load per entry.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 4)]
internal struct Code
{
    /// <summary>Operation flags: 0 = literal, 16 = length/dist base + extra bits, 32 = end-of-block, 64 = invalid.</summary>
    public byte Op;

    /// <summary>Number of bits this code consumes.</summary>
    public byte Bits;

    /// <summary>Literal byte, or length/distance base value, or offset into the sub-table.</summary>
    public ushort Val;
}
