using System.Text;
using DnBrotli.Common;

namespace DnBrotli.Tests;

/// <summary>Exercises the c/common/transform.{h,c} port: BrotliTransformDictionaryWord
/// over a real static-dictionary word ("position", length-8 bucket, index 0) for the
/// RFC 7932 transform kinds the decoder hits most.</summary>
public unsafe class TransformsTests
{
    /// <summary>Applies builtin transform <paramref name="transformIdx"/> to the
    /// length-<paramref name="len"/> word at <paramref name="idx"/> of the static
    /// dictionary, exactly like decode.c does.</summary>
    private static string Apply(int len, int idx, int transformIdx)
    {
        BrotliDictionary* dict = Dictionary.BrotliGetDictionary();
        byte* word = dict->data + dict->offsets_by_length[len] + (uint)(idx * len);
        byte* dst = stackalloc byte[
            SharedDictionary.SHARED_BROTLI_MAX_DICTIONARY_WORD_LENGTH + 32];
        int written = Transforms.BrotliTransformDictionaryWord(
            dst, word, len, Transforms.BrotliGetTransforms(), transformIdx);
        return Encoding.ASCII.GetString(dst, written);
    }

    [Fact]
    public void BuiltinTransformsShape()
    {
        BrotliTransforms* t = Transforms.BrotliGetTransforms();
        Assert.True(t == Transforms.BrotliGetTransforms());  // pointer-stable
        Assert.Equal(121u, t->num_transforms);
        Assert.Equal((uint)Transforms.kNumTransforms, t->num_transforms);
        Assert.Equal(217, t->prefix_suffix_size);
        Assert.True(t->@params == null);
        // c/common/transform.c line 170: {0, 12, 27, 23, 42, 63, 56, 48, 59, 64}.
        Assert.Equal(0, t->cutOffTransforms[0]);
        Assert.Equal(12, t->cutOffTransforms[1]);
        Assert.Equal(64, t->cutOffTransforms[9]);
        // The dictionary word the tests below transform.
        Assert.Equal("position", Apply(8, 0, 0));
    }

    [Fact]
    public void IdentityTransform()
    {
        // Transform 0 is {49, IDENTITY, 49}: empty prefix, empty suffix
        // (c/common/transform.c line 40).
        Assert.Equal("position", Apply(8, 0, 0));
        // Transform 1 is {49, IDENTITY, 0}: suffix " " (line 41).
        Assert.Equal("position ", Apply(8, 0, 1));
    }

    [Fact]
    public void OmitFirstTransform()
    {
        // Transform 3 is {49, OMIT_FIRST_1, 49} (c/common/transform.c line 43).
        Assert.Equal("osition", Apply(8, 0, 3));
        // Transform 11 is {49, OMIT_FIRST_2, 49} (line 51).
        Assert.Equal("sition", Apply(8, 0, 11));
    }

    [Fact]
    public void OmitLastTransform()
    {
        // Transform 12 is {49, OMIT_LAST_1, 49} (c/common/transform.c line 52).
        Assert.Equal("positio", Apply(8, 0, 12));
        // Transform 23 is {49, OMIT_LAST_3, 49} (line 63).
        Assert.Equal("posit", Apply(8, 0, 23));
    }

    [Fact]
    public void UppercaseFirstTransform()
    {
        // Transform 9 is {49, UPPERCASE_FIRST, 49} (c/common/transform.c line 49).
        Assert.Equal("Position", Apply(8, 0, 9));
        // Transform 4 is {49, UPPERCASE_FIRST, 0}: suffix " " (line 44).
        Assert.Equal("Position ", Apply(8, 0, 4));
    }

    [Fact]
    public void UppercaseAllTransform()
    {
        // Transform 44 is {49, UPPERCASE_ALL, 49} (c/common/transform.c line 84).
        Assert.Equal("POSITION", Apply(8, 0, 44));
    }

    [Fact]
    public void PrefixAndSuffixTransform()
    {
        // Transform 5 is {49, IDENTITY, 47}: suffix " the " (c/common/transform.c
        // line 45; prefix_suffix_map[47] == 0xCF -> "\5 the ").
        Assert.Equal("position the ", Apply(8, 0, 5));
        // Transform 6 is {0, IDENTITY, 49}: prefix " " (line 46).
        Assert.Equal(" position", Apply(8, 0, 6));
    }

    [Fact]
    public void TransformsOnOtherBuckets()
    {
        // Length-4 bucket, word 0 is "time"; length-10 bucket, word 5 is "conditions".
        Assert.Equal("time", Apply(4, 0, 0));
        Assert.Equal("Time", Apply(4, 0, 9));
        Assert.Equal("CONDITIONS", Apply(10, 5, 44));
    }
}
