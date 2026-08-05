// Port of c/common/transform.{h,c} (brotli v1.1.0).
//
// The RFC 7932 transform tables are hand-inlined below, matching the C
// initializer layout. kPrefixSuffix and kTransformsData are RVA static data
// (unmovable, so raw pointers into them are stable); kPrefixSuffixMap is
// copied once into native memory at static init so the BrotliTransforms
// struct can hold a plain ushort*. The C field name "params" is a C# keyword
// and is spelled with the verbatim identifier @params.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DnBrotli.Common;

/// <summary><c>enum BrotliWordTransformType</c>. Values match the C enum exactly.</summary>
internal enum BrotliWordTransformType
{
    BROTLI_TRANSFORM_IDENTITY = 0,
    BROTLI_TRANSFORM_OMIT_LAST_1 = 1,
    BROTLI_TRANSFORM_OMIT_LAST_2 = 2,
    BROTLI_TRANSFORM_OMIT_LAST_3 = 3,
    BROTLI_TRANSFORM_OMIT_LAST_4 = 4,
    BROTLI_TRANSFORM_OMIT_LAST_5 = 5,
    BROTLI_TRANSFORM_OMIT_LAST_6 = 6,
    BROTLI_TRANSFORM_OMIT_LAST_7 = 7,
    BROTLI_TRANSFORM_OMIT_LAST_8 = 8,
    BROTLI_TRANSFORM_OMIT_LAST_9 = 9,
    BROTLI_TRANSFORM_UPPERCASE_FIRST = 10,
    BROTLI_TRANSFORM_UPPERCASE_ALL = 11,
    BROTLI_TRANSFORM_OMIT_FIRST_1 = 12,
    BROTLI_TRANSFORM_OMIT_FIRST_2 = 13,
    BROTLI_TRANSFORM_OMIT_FIRST_3 = 14,
    BROTLI_TRANSFORM_OMIT_FIRST_4 = 15,
    BROTLI_TRANSFORM_OMIT_FIRST_5 = 16,
    BROTLI_TRANSFORM_OMIT_FIRST_6 = 17,
    BROTLI_TRANSFORM_OMIT_FIRST_7 = 18,
    BROTLI_TRANSFORM_OMIT_FIRST_8 = 19,
    BROTLI_TRANSFORM_OMIT_FIRST_9 = 20,
    BROTLI_TRANSFORM_SHIFT_FIRST = 21,
    BROTLI_TRANSFORM_SHIFT_ALL = 22,
    BROTLI_NUM_TRANSFORM_TYPES,  /* Counts transforms, not a transform itself. */
}

/// <summary><c>struct BrotliTransforms</c>, field-for-field.</summary>
internal unsafe struct BrotliTransforms
{
    public ushort prefix_suffix_size;
    /* Last character must be null, so prefix_suffix_size must be at least 1. */
    public byte* prefix_suffix;
    public ushort* prefix_suffix_map;
    public uint num_transforms;
    /* Each entry is a [prefix_id, transform, suffix_id] triplet. */
    public byte* transforms;
    /* Shift for BROTLI_TRANSFORM_SHIFT_FIRST and BROTLI_TRANSFORM_SHIFT_ALL,
       must be NULL if and only if no such transforms are present. */
    public byte* @params;
    /* Indices of transforms like ["", BROTLI_TRANSFORM_OMIT_LAST_#, ""].
       0-th element corresponds to ["", BROTLI_TRANSFORM_IDENTITY, ""].
       -1, if cut-off transform does not exist. */
    public fixed short cutOffTransforms[Transforms.BROTLI_TRANSFORMS_MAX_CUT_OFF + 1];
}

internal static unsafe class Transforms
{
    internal const int BROTLI_TRANSFORMS_MAX_CUT_OFF =
        (int)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_9;

    /// <summary>Number of RFC 7932 builtin transforms — the C computes
    /// <c>sizeof(kTransformsData) / 3</c> == 121.</summary>
    internal const int kNumTransforms = 121;

    /// <summary><c>kPrefixSuffix[217]</c>: RFC 7932 transforms string data, including the
    /// implicit trailing zero of the C char array.</summary>
    internal static ReadOnlySpan<byte> kPrefixSuffix =>
    [
        0x01, 0x20, 0x02, 0x2C, 0x20, 0x08, 0x20, 0x6F, 0x66, 0x20, 0x74, 0x68, 0x65, 0x20, 0x04, 0x20,
        0x6F, 0x66, 0x20, 0x02, 0x73, 0x20, 0x01, 0x2E, 0x05, 0x20, 0x61, 0x6E, 0x64, 0x20, 0x04, 0x20,
        0x69, 0x6E, 0x20, 0x01, 0x22, 0x04, 0x20, 0x74, 0x6F, 0x20, 0x02, 0x22, 0x3E, 0x01, 0x0A, 0x02,
        0x2E, 0x20, 0x01, 0x5D, 0x05, 0x20, 0x66, 0x6F, 0x72, 0x20, 0x03, 0x20, 0x61, 0x20, 0x06, 0x20,
        0x74, 0x68, 0x61, 0x74, 0x20, 0x01, 0x27, 0x06, 0x20, 0x77, 0x69, 0x74, 0x68, 0x20, 0x06, 0x20,
        0x66, 0x72, 0x6F, 0x6D, 0x20, 0x04, 0x20, 0x62, 0x79, 0x20, 0x01, 0x28, 0x06, 0x2E, 0x20, 0x54,
        0x68, 0x65, 0x20, 0x04, 0x20, 0x6F, 0x6E, 0x20, 0x04, 0x20, 0x61, 0x73, 0x20, 0x04, 0x20, 0x69,
        0x73, 0x20, 0x04, 0x69, 0x6E, 0x67, 0x20, 0x02, 0x0A, 0x09, 0x01, 0x3A, 0x03, 0x65, 0x64, 0x20,
        0x02, 0x3D, 0x22, 0x04, 0x20, 0x61, 0x74, 0x20, 0x03, 0x6C, 0x79, 0x20, 0x01, 0x2C, 0x02, 0x3D,
        0x27, 0x05, 0x2E, 0x63, 0x6F, 0x6D, 0x2F, 0x07, 0x2E, 0x20, 0x54, 0x68, 0x69, 0x73, 0x20, 0x05,
        0x20, 0x6E, 0x6F, 0x74, 0x20, 0x03, 0x65, 0x72, 0x20, 0x03, 0x61, 0x6C, 0x20, 0x04, 0x66, 0x75,
        0x6C, 0x20, 0x04, 0x69, 0x76, 0x65, 0x20, 0x05, 0x6C, 0x65, 0x73, 0x73, 0x20, 0x04, 0x65, 0x73,
        0x74, 0x20, 0x04, 0x69, 0x7A, 0x65, 0x20, 0x02, 0xC2, 0xA0, 0x04, 0x6F, 0x75, 0x73, 0x20, 0x05,
        0x20, 0x74, 0x68, 0x65, 0x20, 0x02, 0x65, 0x20, 0x00
    ];

    /// <summary><c>kPrefixSuffixMap[50]</c>.</summary>
    private static ReadOnlySpan<ushort> kPrefixSuffixMap =>
    [
        0x00, 0x02, 0x05, 0x0E, 0x13, 0x16, 0x18, 0x1E, 0x23, 0x25,
        0x2A, 0x2D, 0x2F, 0x32, 0x34, 0x3A, 0x3E, 0x45, 0x47, 0x4E,
        0x55, 0x5A, 0x5C, 0x63, 0x68, 0x6D, 0x72, 0x77, 0x7A, 0x7C,
        0x80, 0x83, 0x88, 0x8C, 0x8E, 0x91, 0x97, 0x9F, 0xA5, 0xA9,
        0xAD, 0xB2, 0xB7, 0xBD, 0xC2, 0xC7, 0xCA, 0xCF, 0xD5, 0xD8,
    ];

    /// <summary><c>kTransformsData[]</c>: RFC 7932 transforms,
    /// [prefix_id, transform, suffix_id] triplets.</summary>
    internal static ReadOnlySpan<byte> kTransformsData =>
    [
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 0,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 0,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_FIRST_1, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 0,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 47,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 49,
        4, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 0,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 3,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 6,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_FIRST_2, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_1, 49,
        1, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 0,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 1,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 0,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 7,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 9,
        48, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 0,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 8,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 5,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 10,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 11,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_3, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 13,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 14,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_FIRST_3, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_2, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 15,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 16,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 12,
        5, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 49,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 1,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_FIRST_4, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 18,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 17,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 19,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 20,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_FIRST_5, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_FIRST_6, 49,
        47, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_4, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 22,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 23,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 24,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 25,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_7, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_1, 26,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 27,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 28,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 12,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 29,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_FIRST_9, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_FIRST_7, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_6, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 21,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 1,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_8, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 31,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 32,
        47, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 3,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_5, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_9, 49,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 1,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 8,
        5, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 21,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 0,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 10,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 30,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 5,
        35, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 49,
        47, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 2,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 17,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 36,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 33,
        5, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 0,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 21,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 5,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 37,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 30,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 38,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 0,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 39,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 49,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 34,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 8,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 12,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 21,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 40,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 12,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 41,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 42,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 17,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 43,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 5,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 10,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 34,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 33,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 44,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 5,
        45, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 49,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 33,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 30,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 30,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_IDENTITY, 46,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 1,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 34,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 33,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 30,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 1,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 33,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 21,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 12,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 5,
        49, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 34,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 12,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 30,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL, 34,
        0, (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST, 34
    ];

    /// <summary><c>static const BrotliTransforms kBrotliTransforms</c> — the builtin
    /// instance, allocated once in native memory (thread-safe via static ctor,
    /// intentionally never freed, exactly like the C static).</summary>
    private static readonly BrotliTransforms* kBrotliTransforms;

    static Transforms()
    {
        ushort* map = (ushort*)NativeMemory.Alloc((nuint)(kPrefixSuffixMap.Length * sizeof(ushort)));
        kPrefixSuffixMap.CopyTo(new Span<ushort>(map, kPrefixSuffixMap.Length));

        BrotliTransforms* t = (BrotliTransforms*)NativeMemory.AllocZeroed((nuint)sizeof(BrotliTransforms));
        t->prefix_suffix_size = (ushort)kPrefixSuffix.Length;
        t->prefix_suffix = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(kPrefixSuffix));
        t->prefix_suffix_map = map;
        t->num_transforms = (uint)(kTransformsData.Length / 3);
        t->transforms = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(kTransformsData));
        t->@params = null;  /* no extra parameters */
        t->cutOffTransforms[0] = 0;
        t->cutOffTransforms[1] = 12;
        t->cutOffTransforms[2] = 27;
        t->cutOffTransforms[3] = 23;
        t->cutOffTransforms[4] = 42;
        t->cutOffTransforms[5] = 63;
        t->cutOffTransforms[6] = 56;
        t->cutOffTransforms[7] = 48;
        t->cutOffTransforms[8] = 59;
        t->cutOffTransforms[9] = 64;
        kBrotliTransforms = t;
    }

    internal static BrotliTransforms* BrotliGetTransforms()
    {
        return kBrotliTransforms;
    }

    /* T is BrotliTransforms*; result is uint8_t. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte BROTLI_TRANSFORM_PREFIX_ID(BrotliTransforms* t, int i)
    {
        return t->transforms[(i * 3) + 0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte BROTLI_TRANSFORM_TYPE(BrotliTransforms* t, int i)
    {
        return t->transforms[(i * 3) + 1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte BROTLI_TRANSFORM_SUFFIX_ID(BrotliTransforms* t, int i)
    {
        return t->transforms[(i * 3) + 2];
    }

    /* T is BrotliTransforms*; result is const uint8_t*. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte* BROTLI_TRANSFORM_PREFIX(BrotliTransforms* t, int i)
    {
        return &t->prefix_suffix[t->prefix_suffix_map[BROTLI_TRANSFORM_PREFIX_ID(t, i)]];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte* BROTLI_TRANSFORM_SUFFIX(BrotliTransforms* t, int i)
    {
        return &t->prefix_suffix[t->prefix_suffix_map[BROTLI_TRANSFORM_SUFFIX_ID(t, i)]];
    }

    private static int ToUpperCase(byte* p)
    {
        if (p[0] < 0xC0)
        {
            if (p[0] >= (byte)'a' && p[0] <= (byte)'z')
            {
                p[0] ^= 32;
            }
            return 1;
        }
        /* An overly simplified uppercasing model for UTF-8. */
        if (p[0] < 0xE0)
        {
            p[1] ^= 32;
            return 2;
        }
        /* An arbitrary transform for three byte characters. */
        p[2] ^= 5;
        return 3;
    }

    private static int Shift(byte* word, int word_len, ushort parameter)
    {
        /* Limited sign extension: scalar < (1 << 24). */
        uint scalar =
            (parameter & 0x7FFFu) + (0x1000000u - (parameter & 0x8000u));
        if (word[0] < 0x80)
        {
            /* 1-byte rune / 0sssssss / 7 bit scalar (ASCII). */
            scalar += word[0];
            word[0] = (byte)(scalar & 0x7Fu);
            return 1;
        }
        else if (word[0] < 0xC0)
        {
            /* Continuation / 10AAAAAA. */
            return 1;
        }
        else if (word[0] < 0xE0)
        {
            /* 2-byte rune / 110sssss AAssssss / 11 bit scalar. */
            if (word_len < 2) return 1;
            scalar += (uint)((word[1] & 0x3Fu) | ((word[0] & 0x1Fu) << 6));
            word[0] = (byte)(0xC0 | ((scalar >> 6) & 0x1F));
            word[1] = (byte)((word[1] & 0xC0u) | (scalar & 0x3F));
            return 2;
        }
        else if (word[0] < 0xF0)
        {
            /* 3-byte rune / 1110ssss AAssssss BBssssss / 16 bit scalar. */
            if (word_len < 3) return word_len;
            scalar += (uint)((word[2] & 0x3Fu) | ((word[1] & 0x3Fu) << 6) |
                ((word[0] & 0x0Fu) << 12));
            word[0] = (byte)(0xE0 | ((scalar >> 12) & 0x0F));
            word[1] = (byte)((word[1] & 0xC0u) | ((scalar >> 6) & 0x3F));
            word[2] = (byte)((word[2] & 0xC0u) | (scalar & 0x3F));
            return 3;
        }
        else if (word[0] < 0xF8)
        {
            /* 4-byte rune / 11110sss AAssssss BBssssss CCssssss / 21 bit scalar. */
            if (word_len < 4) return word_len;
            scalar += (uint)((word[3] & 0x3Fu) | ((word[2] & 0x3Fu) << 6) |
                ((word[1] & 0x3Fu) << 12) | ((word[0] & 0x07u) << 18));
            word[0] = (byte)(0xF0 | ((scalar >> 18) & 0x07));
            word[1] = (byte)((word[1] & 0xC0u) | ((scalar >> 12) & 0x3F));
            word[2] = (byte)((word[2] & 0xC0u) | ((scalar >> 6) & 0x3F));
            word[3] = (byte)((word[3] & 0xC0u) | (scalar & 0x3F));
            return 4;
        }
        return 1;
    }

    internal static int BrotliTransformDictionaryWord(byte* dst, byte* word, int len,
        BrotliTransforms* transforms, int transform_idx)
    {
        int idx = 0;
        byte* prefix = BROTLI_TRANSFORM_PREFIX(transforms, transform_idx);
        byte type = BROTLI_TRANSFORM_TYPE(transforms, transform_idx);
        byte* suffix = BROTLI_TRANSFORM_SUFFIX(transforms, transform_idx);
        {
            int prefix_len = *prefix++;
            while (prefix_len-- != 0) { dst[idx++] = *prefix++; }
        }
        {
            int t = type;
            int i = 0;
            if (t <= (int)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_LAST_9)
            {
                len -= t;
            }
            else if (t >= (int)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_FIRST_1
                && t <= (int)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_FIRST_9)
            {
                int skip = t - ((int)BrotliWordTransformType.BROTLI_TRANSFORM_OMIT_FIRST_1 - 1);
                word += skip;
                len -= skip;
            }
            while (i < len) { dst[idx++] = word[i++]; }
            if (t == (int)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST)
            {
                ToUpperCase(&dst[idx - len]);
            }
            else if (t == (int)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_ALL)
            {
                byte* uppercase = &dst[idx - len];
                while (len > 0)
                {
                    int step = ToUpperCase(uppercase);
                    uppercase += step;
                    len -= step;
                }
            }
            else if (t == (int)BrotliWordTransformType.BROTLI_TRANSFORM_SHIFT_FIRST)
            {
                ushort param = (ushort)(transforms->@params[transform_idx * 2]
                    + (transforms->@params[transform_idx * 2 + 1] << 8));
                Shift(&dst[idx - len], len, param);
            }
            else if (t == (int)BrotliWordTransformType.BROTLI_TRANSFORM_SHIFT_ALL)
            {
                ushort param = (ushort)(transforms->@params[transform_idx * 2]
                    + (transforms->@params[transform_idx * 2 + 1] << 8));
                byte* shift = &dst[idx - len];
                while (len > 0)
                {
                    int step = Shift(shift, len, param);
                    shift += step;
                    len -= step;
                }
            }
        }
        {
            int suffix_len = *suffix++;
            while (suffix_len-- != 0) { dst[idx++] = *suffix++; }
            return idx;
        }
    }
}
