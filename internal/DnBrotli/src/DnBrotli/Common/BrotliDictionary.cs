// Port of c/common/dictionary.{h,c} (brotli v1.1.0).
//
// The 122,784-byte word blob lives in DictionaryData.g.cs (RVA static data);
// BrotliGetDictionary() returns a pointer-stable BrotliDictionary instance
// whose |data| points at a one-time native-memory copy of that blob, built
// thread-safely in the static constructor (and, like the C static, never
// freed). BrotliSetDictionaryData is a no-op in the C when the dictionary
// data is compiled in (no BROTLI_EXTERNAL_DICTIONARY_DATA), so it is not
// ported.

using System.Runtime.InteropServices;

namespace DnBrotli.Common;

/// <summary><c>struct BrotliDictionary</c>, field-for-field.</summary>
internal unsafe struct BrotliDictionary
{
    /* Number of bits to encode index of dictionary word in a bucket.
       Words in a dictionary are bucketed by length; 0 means that there are no
       words of a given length. Dictionary consists of words with length of
       [4..24] bytes; values at [0..3] and [25..31] indices should not be
       addressed. */
    public fixed byte size_bits_by_length[32];

    /* assert(offset[i + 1] == offset[i] + (bits[i] ? (i << bits[i]) : 0)) */
    public fixed uint offsets_by_length[32];

    /* assert(data_size == offsets_by_length[31]) */
    public nuint data_size;

    public byte* data;
}

internal static unsafe class Dictionary
{
    internal const int BROTLI_MIN_DICTIONARY_WORD_LENGTH = 4;
    internal const int BROTLI_MAX_DICTIONARY_WORD_LENGTH = 24;

    /// <summary><c>static const BrotliDictionary kBrotliDictionary</c>.</summary>
    private static readonly BrotliDictionary* kBrotliDictionary;

    static Dictionary()
    {
        ReadOnlySpan<byte> blob = DictionaryData.Data;
        byte* data = (byte*)NativeMemory.Alloc((nuint)blob.Length);
        blob.CopyTo(new Span<byte>(data, blob.Length));

        BrotliDictionary* d = (BrotliDictionary*)NativeMemory.AllocZeroed((nuint)sizeof(BrotliDictionary));
        DictionaryData.SizeBitsByLength.CopyTo(new Span<byte>(d->size_bits_by_length, 32));
        for (int i = 0; i < 32; ++i)
        {
            d->offsets_by_length[i] = DictionaryData.OffsetsByLength[i];
        }
        d->data_size = (nuint)blob.Length;  /* == sizeof(kBrotliDictionaryData) == 122784 */
        d->data = data;
        kBrotliDictionary = d;
    }

    internal static BrotliDictionary* BrotliGetDictionary()
    {
        return kBrotliDictionary;
    }
}
