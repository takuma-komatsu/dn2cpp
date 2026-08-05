using System.Runtime.InteropServices;
using static DnZlib.Deflate.DeflateConstants;

namespace DnZlib.Deflate;

/// <summary>
/// Static Huffman tables shared by all deflate streams — generated once at type init (a port of
/// zlib's <c>tr_static_init</c>) into process-lifetime native memory for stable hot-path pointers.
/// </summary>
internal static unsafe class TreesTables
{
    public static readonly int* ExtraLbits;   // [LengthCodes]
    public static readonly int* ExtraDbits;   // [DCodes]
    public static readonly int* ExtraBlbits;  // [BlCodes]
    public static readonly byte* BlOrder;     // [BlCodes]
    public static readonly int* BaseLength;   // [LengthCodes]
    public static readonly int* BaseDist;     // [DCodes]
    public static readonly byte* LengthCode;  // [MaxMatch-MinMatch+1] = [256]
    public static readonly byte* DistCode;    // [DistCodeLen] = [512]
    public static readonly CtData* StaticLtree; // [LCodes+2] = [288]
    public static readonly CtData* StaticDtree; // [DCodes] = [30]

    private static readonly int[] ExtraLbitsSrc =
        [0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0];
    private static readonly int[] ExtraDbitsSrc =
        [0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13];
    private static readonly int[] ExtraBlbitsSrc =
        [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 7];
    private static readonly byte[] BlOrderSrc =
        [16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15];

    static TreesTables()
    {
        ExtraLbits = CopyInt(ExtraLbitsSrc);
        ExtraDbits = CopyInt(ExtraDbitsSrc);
        ExtraBlbits = CopyInt(ExtraBlbitsSrc);
        BlOrder = CopyByte(BlOrderSrc);

        BaseLength = (int*)NativeMemory.AllocZeroed(LengthCodes, sizeof(int));
        BaseDist = (int*)NativeMemory.AllocZeroed(DCodes, sizeof(int));
        LengthCode = (byte*)NativeMemory.AllocZeroed(MaxMatch - MinMatch + 1, sizeof(byte));
        DistCode = (byte*)NativeMemory.AllocZeroed(DistCodeLen, sizeof(byte));
        StaticLtree = (CtData*)NativeMemory.AllocZeroed(LCodes + 2, (nuint)sizeof(CtData));
        StaticDtree = (CtData*)NativeMemory.AllocZeroed(DCodes, (nuint)sizeof(CtData));

        BuildTables();
    }

    private static int* CopyInt(int[] src)
    {
        int* p = (int*)NativeMemory.Alloc((nuint)src.Length, sizeof(int));
        for (int i = 0; i < src.Length; i++)
            p[i] = src[i];
        return p;
    }

    private static byte* CopyByte(byte[] src)
    {
        byte* p = (byte*)NativeMemory.Alloc((nuint)src.Length, sizeof(byte));
        for (int i = 0; i < src.Length; i++)
            p[i] = src[i];
        return p;
    }

    /// <summary>Reverse the low <paramref name="len"/> bits of <paramref name="code"/>.</summary>
    public static uint BiReverse(uint code, int len)
    {
        uint res = 0;
        do
        {
            res |= code & 1;
            code >>= 1;
            res <<= 1;
        }
        while (--len > 0);
        return res >> 1;
    }

    private static void BuildTables()
    {
        // length -> length code, and base_length.
        int length = 0;
        for (int code = 0; code < LengthCodes - 1; code++)
        {
            BaseLength[code] = length;
            for (int n = 0; n < (1 << ExtraLbits[code]); n++)
                LengthCode[length++] = (byte)code;
        }
        // length == 256; overwrite length_code[255] with code 28 (best encoding of length 258).
        LengthCode[length - 1] = LengthCodes - 1;

        // distance -> distance code, and base_dist.
        int dist = 0;
        int c = 0;
        for (; c < 16; c++)
        {
            BaseDist[c] = dist;
            for (int n = 0; n < (1 << ExtraDbits[c]); n++)
                DistCode[dist++] = (byte)c;
        }
        // dist == 256
        dist >>= 7; // all distances now divided by 128
        for (; c < DCodes; c++)
        {
            BaseDist[c] = dist << 7;
            for (int n = 0; n < (1 << (ExtraDbits[c] - 7)); n++)
                DistCode[256 + dist++] = (byte)c;
        }

        // static literal tree bit lengths.
        ushort* blCount = stackalloc ushort[MaxBits + 1];
        for (int bits = 0; bits <= MaxBits; bits++)
            blCount[bits] = 0;
        int nn = 0;
        while (nn <= 143) { StaticLtree[nn++].Len = 8; blCount[8]++; }
        while (nn <= 255) { StaticLtree[nn++].Len = 9; blCount[9]++; }
        while (nn <= 279) { StaticLtree[nn++].Len = 7; blCount[7]++; }
        while (nn <= 287) { StaticLtree[nn++].Len = 8; blCount[8]++; }
        GenCodes(StaticLtree, LCodes + 1, blCount);

        // static distance tree: trivial, all 5-bit reversed codes.
        for (int n = 0; n < DCodes; n++)
        {
            StaticDtree[n].Len = 5;
            StaticDtree[n].Code = (ushort)BiReverse((uint)n, 5);
        }
    }

    internal static void GenCodes(CtData* tree, int maxCode, ushort* blCount)
    {
        ushort* nextCode = stackalloc ushort[MaxBits + 1];
        uint code = 0;
        for (int bits = 1; bits <= MaxBits; bits++)
        {
            code = (code + blCount[bits - 1]) << 1;
            nextCode[bits] = (ushort)code;
        }
        for (int n = 0; n <= maxCode; n++)
        {
            int len = tree[n].Len;
            if (len == 0)
                continue;
            tree[n].Code = (ushort)BiReverse(nextCode[len]++, len);
        }
    }
}
