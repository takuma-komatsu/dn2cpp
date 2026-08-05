using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using DnBrotli.Common;
using DnBrotli.Dec;

namespace DnBrotli.Tests;

/// <summary>Pins the ported static data blobs: SHA-256 of every generated table plus spot
/// checks read directly off the vendored C source (file/line cited at each assertion).</summary>
public unsafe class StaticDataTests
{
    private static string Sha256(ReadOnlySpan<byte> data)
    {
        return Convert.ToHexStringLower(SHA256.HashData(data));
    }

    [Fact]
    public void DictionaryDataBlobMatches()
    {
        Assert.Equal(122_784, DictionaryData.Data.Length);
        Assert.Equal("20e42eb1b511c21806d4d227d07e5dd06877d8ce7b3a817f378f313653f35c70",
            Sha256(DictionaryData.Data));
        // c/common/dictionary.c line 16: the blob opens with the word "time".
        Assert.Equal("time"u8.ToArray(), DictionaryData.Data.Slice(0, 4).ToArray());
    }

    [Fact]
    public void SizeBitsByLengthMatches()
    {
        Assert.Equal(32, DictionaryData.SizeBitsByLength.Length);
        Assert.Equal("97e5baafb08edcd2b7f9e5bc1fc0cbdc46ce6fe265f583ad726809bc418cbb1e",
            Sha256(DictionaryData.SizeBitsByLength));
    }

    [Fact]
    public void OffsetsByLengthMatches()
    {
        Assert.Equal(32, DictionaryData.OffsetsByLength.Length);
        byte[] le = new byte[128];
        for (int i = 0; i < 32; ++i)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(le.AsSpan(i * 4), DictionaryData.OffsetsByLength[i]);
        }
        Assert.Equal("9f726ff6c890d095b5490327cc49b2f9ceae08b0e0f977ded9a21a5aec6de84d", Sha256(le));
        // c/common/dictionary.c lines 5883-5885.
        Assert.Equal(122_016u, DictionaryData.OffsetsByLength[24]);
        Assert.Equal(122_784u, DictionaryData.OffsetsByLength[31]);
    }

    [Fact]
    public void BrotliGetDictionaryIsPointerStableAndPopulated()
    {
        BrotliDictionary* d = Dictionary.BrotliGetDictionary();
        Assert.True(d == Dictionary.BrotliGetDictionary());  // pointer-stable
        Assert.Equal((nuint)122_784, d->data_size);
        Assert.Equal(122_784u, d->offsets_by_length[31]);
        Assert.Equal(10, d->size_bits_by_length[4]);   // c/common/dictionary.c line 5873
        Assert.Equal(5, d->size_bits_by_length[24]);   // c/common/dictionary.c line 5876
        Assert.Equal((byte)'t', d->data[0]);           // "time"
        Assert.Equal((byte)'e', d->data[3]);
    }

    [Fact]
    public void ContextLookupTableSpotChecks()
    {
        ReadOnlySpan<byte> t = Context._kBrotliContextLookupTable;
        Assert.Equal(2048, t.Length);
        // Determinism pin (hash of the mechanically extracted C initializer).
        Assert.Equal("0e4abc034ef46244934d117f15876cd477d0fb2ac94a023093338e72e37f7ad9", Sha256(t));
        // CONTEXT_LSB6, last byte: c/common/context.c lines 8-11 (identity 0..63, repeated).
        Assert.Equal(63, t[63]);
        Assert.Equal(0, t[64]);
        // CONTEXT_MSB6, last byte: c/common/context.c line 44 (0,0,0,0, 1,1,1,1, ...).
        Assert.Equal(0, t[512]);
        Assert.Equal(1, t[516]);
        // CONTEXT_UTF8, last byte, 'A' (0x41): c/common/context.c line 85 (row 0x40-0x4F).
        Assert.Equal(48, t[1024 + 'A']);
        // CONTEXT_SIGNED, last byte: c/common/context.c line 122 (0, then 8s).
        Assert.Equal(0, t[1536]);
        Assert.Equal(8, t[1537]);
        // CONTEXT_SIGNED, second last byte, final entry: c/common/context.c line 155.
        Assert.Equal(7, t[2047]);
    }

    [Fact]
    public void ContextLutMacroSelectsModeSlices()
    {
        byte* lsb6 = Context.BROTLI_CONTEXT_LUT((nuint)ContextType.CONTEXT_LSB6);
        byte* utf8 = Context.BROTLI_CONTEXT_LUT((nuint)ContextType.CONTEXT_UTF8);
        Assert.True(utf8 == lsb6 + 1024);
        // BROTLI_CONTEXT('e', 'm', utf8-lut): lut['e'] | (lut+256)['m'] == 56 | 3
        // (c/common/context.c lines 87 and 108).
        Assert.Equal(59, Context.BROTLI_CONTEXT((byte)'e', (byte)'m', utf8));
    }

    [Fact]
    public void CmdLutSpotChecksAgainstCSource()
    {
        CmdLutElement* lut = Prefix.kCmdLut;

        // Determinism pin: hash of the 704 x 8-byte LE records in native memory.
        Assert.Equal(8, sizeof(CmdLutElement));
        Assert.Equal("fdbc5d5f207a43d34b26441e29b7024f4f3d00be66502d8a73233bcbe9f6c1b4",
            Sha256(new ReadOnlySpan<byte>(lut, 704 * 8)));

        // c/dec/prefix.h line 27: { 0x00, 0x00, 0, 0x00, 0x0000, 0x0002 }.
        Assert.Equal(0, lut[0].insert_len_extra_bits);
        Assert.Equal(0, lut[0].copy_len_extra_bits);
        Assert.Equal(0, lut[0].distance_code);
        Assert.Equal(0, lut[0].context);
        Assert.Equal(0, lut[0].insert_len_offset);
        Assert.Equal(2, lut[0].copy_len_offset);

        // c/dec/prefix.h line 91: { 0x00, 0x01, 0, 0x03, 0x0000, 0x000a }.
        Assert.Equal(0, lut[64].insert_len_extra_bits);
        Assert.Equal(1, lut[64].copy_len_extra_bits);
        Assert.Equal(0, lut[64].distance_code);
        Assert.Equal(3, lut[64].context);
        Assert.Equal(0, lut[64].insert_len_offset);
        Assert.Equal(0x000A, lut[64].copy_len_offset);

        // c/dec/prefix.h line 411: { 0x00, 0x05, -1, 0x03, 0x0000, 0x0046 }.
        Assert.Equal(0, lut[384].insert_len_extra_bits);
        Assert.Equal(5, lut[384].copy_len_extra_bits);
        Assert.Equal(-1, lut[384].distance_code);
        Assert.Equal(3, lut[384].context);
        Assert.Equal(0, lut[384].insert_len_offset);
        Assert.Equal(0x0046, lut[384].copy_len_offset);

        // c/dec/prefix.h line 730 (last entry): { 0x18, 0x18, -1, 0x03, 0x5842, 0x0846 }.
        Assert.Equal(0x18, lut[703].insert_len_extra_bits);
        Assert.Equal(0x18, lut[703].copy_len_extra_bits);
        Assert.Equal(-1, lut[703].distance_code);
        Assert.Equal(3, lut[703].context);
        Assert.Equal(0x5842, lut[703].insert_len_offset);
        Assert.Equal(0x0846, lut[703].copy_len_offset);
    }

    [Fact]
    public void PrefixCodeRangesMatchConstantsC()
    {
        // c/common/constants.c lines 10-16.
        Assert.Equal(26, BrotliConstants._kBrotliPrefixCodeRanges.Length);
        Assert.Equal(1, BrotliConstants._kBrotliPrefixCodeRanges[0].offset);
        Assert.Equal(2, BrotliConstants._kBrotliPrefixCodeRanges[0].nbits);
        Assert.Equal(16_625, BrotliConstants._kBrotliPrefixCodeRanges[25].offset);
        Assert.Equal(24, BrotliConstants._kBrotliPrefixCodeRanges[25].nbits);
    }

    [Fact]
    public void PrefixSuffixTableMatchesCSource()
    {
        // c/common/transform.c lines 14-27: 217 bytes including the implicit trailing 0.
        ReadOnlySpan<byte> ps = Transforms.kPrefixSuffix;
        Assert.Equal(217, ps.Length);
        Assert.Equal(1, ps[0]);            // "\1 "
        Assert.Equal((byte)' ', ps[1]);
        Assert.Equal(8, ps[5]);            // "\10 of the " (octal 10 == 8)
        Assert.Equal(" of the ", Encoding.ASCII.GetString(ps.Slice(6, 8)));
        Assert.Equal(0xC2, ps[0xC8]);      // "\2\xc2\xa0"
        Assert.Equal(0xA0, ps[0xC9]);
        Assert.Equal(0, ps[216]);          // implicit trailing zero
        // kTransformsData: 121 triplets; c/common/transform.c lines 40-42.
        Assert.Equal(121 * 3, Transforms.kTransformsData.Length);
        Assert.Equal(49, Transforms.kTransformsData[0]);
        Assert.Equal(0, Transforms.kTransformsData[1]);   // BROTLI_TRANSFORM_IDENTITY
        Assert.Equal(49, Transforms.kTransformsData[2]);
    }

    [Fact]
    public void DictionaryHashBlobsMatch()
    {
        // c/enc/dictionary_hash.c: kStaticDictionaryHashWords (32768 x uint16 LE)
        // and kStaticDictionaryHashLengths (32768 x uint8).
        Assert.Equal(65_536, DnBrotli.Enc.DictionaryHash.HashWordsLE.Length);
        Assert.Equal("5f60018015074e571ec29147973866b82ada416b08325bf75d8e3c94bc83fb37",
            Sha256(DnBrotli.Enc.DictionaryHash.HashWordsLE));
        Assert.Equal(32_768, DnBrotli.Enc.DictionaryHash.HashLengths.Length);
        Assert.Equal("05720624c4ffa3f378a4b9363e32627cbecd7efc761009aa49d5e78d65b0c82b",
            Sha256(DnBrotli.Enc.DictionaryHash.HashLengths));
    }

    [Fact]
    public void StaticDictLutBlobsMatch()
    {
        // c/enc/static_dict_lut.h: kStaticDictionaryBuckets (32768 x uint16 LE) and
        // kStaticDictionaryWords (31705 x DictWord{len,transform,idx LE} = 4 bytes).
        Assert.Equal(65_536, DnBrotli.Enc.StaticDictLut.BucketsLE.Length);
        Assert.Equal("7b6539ba4c302b1ebc592faec82fbf0c1f6530f7d143db3f411c1ee666958077",
            Sha256(DnBrotli.Enc.StaticDictLut.BucketsLE));
        Assert.Equal(31_705 * 4, DnBrotli.Enc.StaticDictLut.WordsLE.Length);
        Assert.Equal("0d1192dba189dcd045ca677d24c828d266a6372c4c1bb73ef4b47427f9181327",
            Sha256(DnBrotli.Enc.StaticDictLut.WordsLE));
    }

    [Fact]
    public void EncoderDictDecodedTablesSpotChecks()
    {
        // Decoded native copies must agree with the blobs and the C source shape.
        // c/enc/static_dict_lut.h line 29: buckets open with 1,0,0,0.
        Assert.Equal(1, DnBrotli.Enc.EncoderDict.kStaticDictionaryBuckets[0]);
        Assert.Equal(0, DnBrotli.Enc.EncoderDict.kStaticDictionaryBuckets[1]);
        // Last two buckets (index 32766, 32767): 31703, 0.
        Assert.Equal(31_703, DnBrotli.Enc.EncoderDict.kStaticDictionaryBuckets[32_766]);
        Assert.Equal(0, DnBrotli.Enc.EncoderDict.kStaticDictionaryBuckets[32_767]);
        // First DictWord is the unused sentinel {0,0,0}; last is {138,11,347}.
        Assert.Equal(0, DnBrotli.Enc.EncoderDict.kStaticDictionaryWords[0].len);
        Assert.Equal(138, DnBrotli.Enc.EncoderDict.kStaticDictionaryWords[31_704].len);
        Assert.Equal(11, DnBrotli.Enc.EncoderDict.kStaticDictionaryWords[31_704].transform);
        Assert.Equal(347, DnBrotli.Enc.EncoderDict.kStaticDictionaryWords[31_704].idx);
        // c/enc/dictionary_hash.c: hash words open 1002,0,0,0; lengths open 8,0,0,0.
        Assert.Equal(1002, DnBrotli.Enc.EncoderDict.kStaticDictionaryHashWords[0]);
        Assert.Equal(8, DnBrotli.Enc.EncoderDict.kStaticDictionaryHashLengths[0]);
        Assert.Equal(0, DnBrotli.Enc.EncoderDict.kStaticDictionaryHashLengths[1]);
    }

    [Fact]
    public void SharedDictionaryDefaultsToBuiltin()
    {
        BrotliSharedDictionaryInternal* dict = SharedDictionary.BrotliSharedDictionaryCreateInstance();
        Assert.True(dict != null);
        Assert.Equal(0u, dict->num_prefix);
        Assert.Equal(0, dict->context_based);
        Assert.Equal(1, dict->num_dictionaries);
        Assert.Equal(0, dict->num_word_lists);
        Assert.Equal(0, dict->num_transform_lists);
        Assert.True(dict->words(0) == Dictionary.BrotliGetDictionary());
        Assert.True(dict->transforms(0) == Transforms.BrotliGetTransforms());
        Assert.True(dict->words_instances == null);
        Assert.True(dict->transforms_instances == null);
        SharedDictionary.BrotliSharedDictionaryDestroyInstance(dict);
    }
}
