// Port of c/enc/static_dict.{h,c} (brotli v1.1.0): matching data against the
// static dictionary with the built-in transform set. Ported in full; the
// BROTLI_EXPERIMENTAL has_words_heavy trie walk is compiled out in the
// vendored build and stays out here (has_words_heavy is never set while the
// custom-dictionary machinery is deferred).

using System.Runtime.CompilerServices;

using DnBrotli.Common;

using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.FindMatchLength;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

internal static unsafe class StaticDict
{
    internal const int BROTLI_MAX_STATIC_DICTIONARY_MATCH_LEN = 37;
    internal const uint kInvalidMatch = 0xFFFFFFF;

    /* kDictNumBits / kDictHashMul32 from c/enc/static_dict_lut.h. */
    internal const int kDictNumBits = 15;
    internal const uint kDictHashMul32 = 0x1E35A7BD;

    private const byte BROTLI_TRANSFORM_UPPERCASE_FIRST =
        (byte)BrotliWordTransformType.BROTLI_TRANSFORM_UPPERCASE_FIRST;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Hash(byte* data)
    {
        uint h = BROTLI_UNALIGNED_LOAD32LE(data) * kDictHashMul32;
        /* The higher bits contain more mixture from the multiplication,
           so we take our results from there. */
        return h >> (32 - kDictNumBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddMatch(nuint distance, nuint len, nuint len_code,
                                 uint* matches)
    {
        uint match = (uint)((distance << 5) + len_code);
        matches[len] = BROTLI_MIN(matches[len], match);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint DictMatchLength(BrotliDictionary* dictionary,
                                         byte* data,
                                         nuint id,
                                         nuint len,
                                         nuint maxlen)
    {
        nuint offset = dictionary->offsets_by_length[len] + len * id;
        return FindMatchLengthWithLimit(&dictionary->data[offset], data,
                                        BROTLI_MIN(len, maxlen));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMatch(BrotliDictionary* dictionary,
        DictWord w, byte* data, nuint max_length)
    {
        if (w.len > max_length)
        {
            return false;
        }
        else
        {
            nuint offset = dictionary->offsets_by_length[w.len] +
                (nuint)w.len * (nuint)w.idx;
            byte* dict = &dictionary->data[offset];
            if (w.transform == 0)
            {
                /* Match against base dictionary word. */
                return FindMatchLengthWithLimit(dict, data, w.len) == w.len;
            }
            else if (w.transform == 10)
            {
                /* Match against uppercase first transform.
                   Note that there are only ASCII uppercase words in the lookup table. */
                return dict[0] >= 'a' && dict[0] <= 'z' &&
                       (dict[0] ^ 32) == data[0] &&
                       FindMatchLengthWithLimit(&dict[1], &data[1], w.len - 1u) ==
                       w.len - 1u;
            }
            else
            {
                /* Match against uppercase all transform.
                   Note that there are only ASCII uppercase words in the lookup table. */
                nuint i;
                for (i = 0; i < w.len; ++i)
                {
                    if (dict[i] >= 'a' && dict[i] <= 'z')
                    {
                        if ((dict[i] ^ 32) != data[i]) return false;
                    }
                    else
                    {
                        if (dict[i] != data[i]) return false;
                    }
                }
                return true;
            }
        }
    }

    /* Finds matches for a single static dictionary */
    private static bool BrotliFindAllStaticDictionaryMatchesFor(
        BrotliEncoderDictionary* dictionary, byte* data,
        nuint min_length, nuint max_length, uint* matches)
    {
        bool has_found_match = false;
        {
            nuint offset = dictionary->buckets[Hash(data)];
            bool end = offset == 0;
            while (!end)
            {
                DictWord w = dictionary->dict_words[offset++];
                nuint l = (nuint)(w.len & 0x1F);
                nuint n = (nuint)1 << dictionary->words->size_bits_by_length[l];
                nuint id = w.idx;
                end = (w.len & 0x80) != 0;
                w.len = (byte)l;
                if (w.transform == 0)
                {
                    nuint matchlen =
                        DictMatchLength(dictionary->words, data, id, l, max_length);
                    byte* s;
                    nuint minlen;
                    nuint maxlen;
                    nuint len;
                    /* Transform "" + BROTLI_TRANSFORM_IDENTITY + "" */
                    if (matchlen == l)
                    {
                        AddMatch(id, l, l, matches);
                        has_found_match = true;
                    }
                    /* Transforms "" + BROTLI_TRANSFORM_OMIT_LAST_1 + "" and
                                  "" + BROTLI_TRANSFORM_OMIT_LAST_1 + "ing " */
                    if (matchlen >= l - 1)
                    {
                        AddMatch(id + 12 * n, l - 1, l, matches);
                        if (l + 2 < max_length &&
                            data[l - 1] == 'i' && data[l] == 'n' && data[l + 1] == 'g' &&
                            data[l + 2] == ' ')
                        {
                            AddMatch(id + 49 * n, l + 3, l, matches);
                        }
                        has_found_match = true;
                    }
                    /* Transform "" + BROTLI_TRANSFORM_OMIT_LAST_# + "" (# = 2 .. 9) */
                    minlen = min_length;
                    if (l > 9) minlen = BROTLI_MAX(minlen, l - 9);
                    maxlen = BROTLI_MIN(matchlen, l - 2);
                    for (len = minlen; len <= maxlen; ++len)
                    {
                        nuint cut = l - len;
                        nuint transform_id = (cut << 2) +
                            (nuint)((dictionary->cutoffTransforms >> (int)(cut * 6)) & 0x3F);
                        AddMatch(id + transform_id * n, len, l, matches);
                        has_found_match = true;
                    }
                    if (matchlen < l || l + 6 >= max_length)
                    {
                        continue;
                    }
                    s = &data[l];
                    /* Transforms "" + BROTLI_TRANSFORM_IDENTITY + <suffix> */
                    if (s[0] == ' ')
                    {
                        AddMatch(id + n, l + 1, l, matches);
                        if (s[1] == 'a')
                        {
                            if (s[2] == ' ')
                            {
                                AddMatch(id + 28 * n, l + 3, l, matches);
                            }
                            else if (s[2] == 's')
                            {
                                if (s[3] == ' ') AddMatch(id + 46 * n, l + 4, l, matches);
                            }
                            else if (s[2] == 't')
                            {
                                if (s[3] == ' ') AddMatch(id + 60 * n, l + 4, l, matches);
                            }
                            else if (s[2] == 'n')
                            {
                                if (s[3] == 'd' && s[4] == ' ')
                                {
                                    AddMatch(id + 10 * n, l + 5, l, matches);
                                }
                            }
                        }
                        else if (s[1] == 'b')
                        {
                            if (s[2] == 'y' && s[3] == ' ')
                            {
                                AddMatch(id + 38 * n, l + 4, l, matches);
                            }
                        }
                        else if (s[1] == 'i')
                        {
                            if (s[2] == 'n')
                            {
                                if (s[3] == ' ') AddMatch(id + 16 * n, l + 4, l, matches);
                            }
                            else if (s[2] == 's')
                            {
                                if (s[3] == ' ') AddMatch(id + 47 * n, l + 4, l, matches);
                            }
                        }
                        else if (s[1] == 'f')
                        {
                            if (s[2] == 'o')
                            {
                                if (s[3] == 'r' && s[4] == ' ')
                                {
                                    AddMatch(id + 25 * n, l + 5, l, matches);
                                }
                            }
                            else if (s[2] == 'r')
                            {
                                if (s[3] == 'o' && s[4] == 'm' && s[5] == ' ')
                                {
                                    AddMatch(id + 37 * n, l + 6, l, matches);
                                }
                            }
                        }
                        else if (s[1] == 'o')
                        {
                            if (s[2] == 'f')
                            {
                                if (s[3] == ' ') AddMatch(id + 8 * n, l + 4, l, matches);
                            }
                            else if (s[2] == 'n')
                            {
                                if (s[3] == ' ') AddMatch(id + 45 * n, l + 4, l, matches);
                            }
                        }
                        else if (s[1] == 'n')
                        {
                            if (s[2] == 'o' && s[3] == 't' && s[4] == ' ')
                            {
                                AddMatch(id + 80 * n, l + 5, l, matches);
                            }
                        }
                        else if (s[1] == 't')
                        {
                            if (s[2] == 'h')
                            {
                                if (s[3] == 'e')
                                {
                                    if (s[4] == ' ') AddMatch(id + 5 * n, l + 5, l, matches);
                                }
                                else if (s[3] == 'a')
                                {
                                    if (s[4] == 't' && s[5] == ' ')
                                    {
                                        AddMatch(id + 29 * n, l + 6, l, matches);
                                    }
                                }
                            }
                            else if (s[2] == 'o')
                            {
                                if (s[3] == ' ') AddMatch(id + 17 * n, l + 4, l, matches);
                            }
                        }
                        else if (s[1] == 'w')
                        {
                            if (s[2] == 'i' && s[3] == 't' && s[4] == 'h' && s[5] == ' ')
                            {
                                AddMatch(id + 35 * n, l + 6, l, matches);
                            }
                        }
                    }
                    else if (s[0] == '"')
                    {
                        AddMatch(id + 19 * n, l + 1, l, matches);
                        if (s[1] == '>')
                        {
                            AddMatch(id + 21 * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == '.')
                    {
                        AddMatch(id + 20 * n, l + 1, l, matches);
                        if (s[1] == ' ')
                        {
                            AddMatch(id + 31 * n, l + 2, l, matches);
                            if (s[2] == 'T' && s[3] == 'h')
                            {
                                if (s[4] == 'e')
                                {
                                    if (s[5] == ' ') AddMatch(id + 43 * n, l + 6, l, matches);
                                }
                                else if (s[4] == 'i')
                                {
                                    if (s[5] == 's' && s[6] == ' ')
                                    {
                                        AddMatch(id + 75 * n, l + 7, l, matches);
                                    }
                                }
                            }
                        }
                    }
                    else if (s[0] == ',')
                    {
                        AddMatch(id + 76 * n, l + 1, l, matches);
                        if (s[1] == ' ')
                        {
                            AddMatch(id + 14 * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == '\n')
                    {
                        AddMatch(id + 22 * n, l + 1, l, matches);
                        if (s[1] == '\t')
                        {
                            AddMatch(id + 50 * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == ']')
                    {
                        AddMatch(id + 24 * n, l + 1, l, matches);
                    }
                    else if (s[0] == '\'')
                    {
                        AddMatch(id + 36 * n, l + 1, l, matches);
                    }
                    else if (s[0] == ':')
                    {
                        AddMatch(id + 51 * n, l + 1, l, matches);
                    }
                    else if (s[0] == '(')
                    {
                        AddMatch(id + 57 * n, l + 1, l, matches);
                    }
                    else if (s[0] == '=')
                    {
                        if (s[1] == '"')
                        {
                            AddMatch(id + 70 * n, l + 2, l, matches);
                        }
                        else if (s[1] == '\'')
                        {
                            AddMatch(id + 86 * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == 'a')
                    {
                        if (s[1] == 'l' && s[2] == ' ')
                        {
                            AddMatch(id + 84 * n, l + 3, l, matches);
                        }
                    }
                    else if (s[0] == 'e')
                    {
                        if (s[1] == 'd')
                        {
                            if (s[2] == ' ') AddMatch(id + 53 * n, l + 3, l, matches);
                        }
                        else if (s[1] == 'r')
                        {
                            if (s[2] == ' ') AddMatch(id + 82 * n, l + 3, l, matches);
                        }
                        else if (s[1] == 's')
                        {
                            if (s[2] == 't' && s[3] == ' ')
                            {
                                AddMatch(id + 95 * n, l + 4, l, matches);
                            }
                        }
                    }
                    else if (s[0] == 'f')
                    {
                        if (s[1] == 'u' && s[2] == 'l' && s[3] == ' ')
                        {
                            AddMatch(id + 90 * n, l + 4, l, matches);
                        }
                    }
                    else if (s[0] == 'i')
                    {
                        if (s[1] == 'v')
                        {
                            if (s[2] == 'e' && s[3] == ' ')
                            {
                                AddMatch(id + 92 * n, l + 4, l, matches);
                            }
                        }
                        else if (s[1] == 'z')
                        {
                            if (s[2] == 'e' && s[3] == ' ')
                            {
                                AddMatch(id + 100 * n, l + 4, l, matches);
                            }
                        }
                    }
                    else if (s[0] == 'l')
                    {
                        if (s[1] == 'e')
                        {
                            if (s[2] == 's' && s[3] == 's' && s[4] == ' ')
                            {
                                AddMatch(id + 93 * n, l + 5, l, matches);
                            }
                        }
                        else if (s[1] == 'y')
                        {
                            if (s[2] == ' ') AddMatch(id + 61 * n, l + 3, l, matches);
                        }
                    }
                    else if (s[0] == 'o')
                    {
                        if (s[1] == 'u' && s[2] == 's' && s[3] == ' ')
                        {
                            AddMatch(id + 106 * n, l + 4, l, matches);
                        }
                    }
                }
                else
                {
                    /* Set is_all_caps=0 for BROTLI_TRANSFORM_UPPERCASE_FIRST and
                           is_all_caps=1 otherwise (BROTLI_TRANSFORM_UPPERCASE_ALL)
                       transform. */
                    bool is_all_caps = w.transform != BROTLI_TRANSFORM_UPPERCASE_FIRST;
                    byte* s;
                    if (!IsMatch(dictionary->words, w, data, max_length))
                    {
                        continue;
                    }
                    /* Transform "" + kUppercase{First,All} + "" */
                    AddMatch(id + (is_all_caps ? 44u : 9u) * n, l, l, matches);
                    has_found_match = true;
                    if (l + 1 >= max_length)
                    {
                        continue;
                    }
                    /* Transforms "" + kUppercase{First,All} + <suffix> */
                    s = &data[l];
                    if (s[0] == ' ')
                    {
                        AddMatch(id + (is_all_caps ? 68u : 4u) * n, l + 1, l, matches);
                    }
                    else if (s[0] == '"')
                    {
                        AddMatch(id + (is_all_caps ? 87u : 66u) * n, l + 1, l, matches);
                        if (s[1] == '>')
                        {
                            AddMatch(id + (is_all_caps ? 97u : 69u) * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == '.')
                    {
                        AddMatch(id + (is_all_caps ? 101u : 79u) * n, l + 1, l, matches);
                        if (s[1] == ' ')
                        {
                            AddMatch(id + (is_all_caps ? 114u : 88u) * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == ',')
                    {
                        AddMatch(id + (is_all_caps ? 112u : 99u) * n, l + 1, l, matches);
                        if (s[1] == ' ')
                        {
                            AddMatch(id + (is_all_caps ? 107u : 58u) * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == '\'')
                    {
                        AddMatch(id + (is_all_caps ? 94u : 74u) * n, l + 1, l, matches);
                    }
                    else if (s[0] == '(')
                    {
                        AddMatch(id + (is_all_caps ? 113u : 78u) * n, l + 1, l, matches);
                    }
                    else if (s[0] == '=')
                    {
                        if (s[1] == '"')
                        {
                            AddMatch(id + (is_all_caps ? 105u : 104u) * n, l + 2, l, matches);
                        }
                        else if (s[1] == '\'')
                        {
                            AddMatch(id + (is_all_caps ? 116u : 108u) * n, l + 2, l, matches);
                        }
                    }
                }
            }
        }
        /* Transforms with prefixes " " and "." */
        if (max_length >= 5 && (data[0] == ' ' || data[0] == '.'))
        {
            bool is_space = data[0] == ' ';
            nuint offset = dictionary->buckets[Hash(&data[1])];
            bool end = offset == 0;
            while (!end)
            {
                DictWord w = dictionary->dict_words[offset++];
                nuint l = (nuint)(w.len & 0x1F);
                nuint n = (nuint)1 << dictionary->words->size_bits_by_length[l];
                nuint id = w.idx;
                end = (w.len & 0x80) != 0;
                w.len = (byte)l;
                if (w.transform == 0)
                {
                    byte* s;
                    if (!IsMatch(dictionary->words, w, &data[1], max_length - 1))
                    {
                        continue;
                    }
                    /* Transforms " " + BROTLI_TRANSFORM_IDENTITY + "" and
                                  "." + BROTLI_TRANSFORM_IDENTITY + "" */
                    AddMatch(id + (is_space ? 6u : 32u) * n, l + 1, l, matches);
                    has_found_match = true;
                    if (l + 2 >= max_length)
                    {
                        continue;
                    }
                    /* Transforms " " + BROTLI_TRANSFORM_IDENTITY + <suffix> and
                                  "." + BROTLI_TRANSFORM_IDENTITY + <suffix>
                    */
                    s = &data[l + 1];
                    if (s[0] == ' ')
                    {
                        AddMatch(id + (is_space ? 2u : 77u) * n, l + 2, l, matches);
                    }
                    else if (s[0] == '(')
                    {
                        AddMatch(id + (is_space ? 89u : 67u) * n, l + 2, l, matches);
                    }
                    else if (is_space)
                    {
                        if (s[0] == ',')
                        {
                            AddMatch(id + 103 * n, l + 2, l, matches);
                            if (s[1] == ' ')
                            {
                                AddMatch(id + 33 * n, l + 3, l, matches);
                            }
                        }
                        else if (s[0] == '.')
                        {
                            AddMatch(id + 71 * n, l + 2, l, matches);
                            if (s[1] == ' ')
                            {
                                AddMatch(id + 52 * n, l + 3, l, matches);
                            }
                        }
                        else if (s[0] == '=')
                        {
                            if (s[1] == '"')
                            {
                                AddMatch(id + 81 * n, l + 3, l, matches);
                            }
                            else if (s[1] == '\'')
                            {
                                AddMatch(id + 98 * n, l + 3, l, matches);
                            }
                        }
                    }
                }
                else if (is_space)
                {
                    /* Set is_all_caps=0 for BROTLI_TRANSFORM_UPPERCASE_FIRST and
                           is_all_caps=1 otherwise (BROTLI_TRANSFORM_UPPERCASE_ALL)
                       transform. */
                    bool is_all_caps = w.transform != BROTLI_TRANSFORM_UPPERCASE_FIRST;
                    byte* s;
                    if (!IsMatch(dictionary->words, w, &data[1], max_length - 1))
                    {
                        continue;
                    }
                    /* Transforms " " + kUppercase{First,All} + "" */
                    AddMatch(id + (is_all_caps ? 85u : 30u) * n, l + 1, l, matches);
                    has_found_match = true;
                    if (l + 2 >= max_length)
                    {
                        continue;
                    }
                    /* Transforms " " + kUppercase{First,All} + <suffix> */
                    s = &data[l + 1];
                    if (s[0] == ' ')
                    {
                        AddMatch(id + (is_all_caps ? 83u : 15u) * n, l + 2, l, matches);
                    }
                    else if (s[0] == ',')
                    {
                        if (!is_all_caps)
                        {
                            AddMatch(id + 109 * n, l + 2, l, matches);
                        }
                        if (s[1] == ' ')
                        {
                            AddMatch(id + (is_all_caps ? 111u : 65u) * n, l + 3, l, matches);
                        }
                    }
                    else if (s[0] == '.')
                    {
                        AddMatch(id + (is_all_caps ? 115u : 96u) * n, l + 2, l, matches);
                        if (s[1] == ' ')
                        {
                            AddMatch(id + (is_all_caps ? 117u : 91u) * n, l + 3, l, matches);
                        }
                    }
                    else if (s[0] == '=')
                    {
                        if (s[1] == '"')
                        {
                            AddMatch(id + (is_all_caps ? 110u : 118u) * n, l + 3, l, matches);
                        }
                        else if (s[1] == '\'')
                        {
                            AddMatch(id + (is_all_caps ? 119u : 120u) * n, l + 3, l, matches);
                        }
                    }
                }
            }
        }
        if (max_length >= 6)
        {
            /* Transforms with prefixes "e ", "s ", ", " and "\xC2\xA0" */
            if ((data[1] == ' ' &&
                 (data[0] == 'e' || data[0] == 's' || data[0] == ',')) ||
                (data[0] == 0xC2 && data[1] == 0xA0))
            {
                nuint offset = dictionary->buckets[Hash(&data[2])];
                bool end = offset == 0;
                while (!end)
                {
                    DictWord w = dictionary->dict_words[offset++];
                    nuint l = (nuint)(w.len & 0x1F);
                    nuint n = (nuint)1 << dictionary->words->size_bits_by_length[l];
                    nuint id = w.idx;
                    end = (w.len & 0x80) != 0;
                    w.len = (byte)l;
                    if (w.transform == 0 &&
                        IsMatch(dictionary->words, w, &data[2], max_length - 2))
                    {
                        if (data[0] == 0xC2)
                        {
                            AddMatch(id + 102 * n, l + 2, l, matches);
                            has_found_match = true;
                        }
                        else if (l + 2 < max_length && data[l + 2] == ' ')
                        {
                            nuint t = data[0] == 'e' ? 18u : (data[0] == 's' ? 7u : 13u);
                            AddMatch(id + t * n, l + 3, l, matches);
                            has_found_match = true;
                        }
                    }
                }
            }
        }
        if (max_length >= 9)
        {
            /* Transforms with prefixes " the " and ".com/" */
            if ((data[0] == ' ' && data[1] == 't' && data[2] == 'h' &&
                 data[3] == 'e' && data[4] == ' ') ||
                (data[0] == '.' && data[1] == 'c' && data[2] == 'o' &&
                 data[3] == 'm' && data[4] == '/'))
            {
                nuint offset = dictionary->buckets[Hash(&data[5])];
                bool end = offset == 0;
                while (!end)
                {
                    DictWord w = dictionary->dict_words[offset++];
                    nuint l = (nuint)(w.len & 0x1F);
                    nuint n = (nuint)1 << dictionary->words->size_bits_by_length[l];
                    nuint id = w.idx;
                    end = (w.len & 0x80) != 0;
                    w.len = (byte)l;
                    if (w.transform == 0 &&
                        IsMatch(dictionary->words, w, &data[5], max_length - 5))
                    {
                        AddMatch(id + (data[0] == ' ' ? 41u : 72u) * n, l + 5, l, matches);
                        has_found_match = true;
                        if (l + 5 < max_length)
                        {
                            byte* s = &data[l + 5];
                            if (data[0] == ' ')
                            {
                                if (l + 8 < max_length &&
                                    s[0] == ' ' && s[1] == 'o' && s[2] == 'f' && s[3] == ' ')
                                {
                                    AddMatch(id + 62 * n, l + 9, l, matches);
                                    if (l + 12 < max_length &&
                                        s[4] == 't' && s[5] == 'h' && s[6] == 'e' && s[7] == ' ')
                                    {
                                        AddMatch(id + 73 * n, l + 13, l, matches);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return has_found_match;
    }

    /* Finds matches for one or more dictionaries, if multiple are present
       in the contextual dictionary */
    internal static bool BrotliFindAllStaticDictionaryMatches(
        BrotliEncoderDictionary* dictionary, byte* data,
        nuint min_length, nuint max_length, uint* matches)
    {
        bool has_found_match =
            BrotliFindAllStaticDictionaryMatchesFor(
                dictionary, data, min_length, max_length, matches);

        if (dictionary->parent != null && dictionary->parent->num_dictionaries > 1)
        {
            uint* matches2 = stackalloc uint[BROTLI_MAX_STATIC_DICTIONARY_MATCH_LEN + 1];
            int l;
            BrotliEncoderDictionary* dictionary2 = dictionary->parent->dict(0);
            if (dictionary2 == dictionary)
            {
                dictionary2 = dictionary->parent->dict(1);
            }

            for (l = 0; l < BROTLI_MAX_STATIC_DICTIONARY_MATCH_LEN + 1; l++)
            {
                matches2[l] = kInvalidMatch;
            }

            has_found_match |= BrotliFindAllStaticDictionaryMatchesFor(
                dictionary2, data, min_length, max_length, matches2);

            for (l = 0; l < BROTLI_MAX_STATIC_DICTIONARY_MATCH_LEN + 1; l++)
            {
                if (matches2[l] != kInvalidMatch)
                {
                    uint dist = matches2[l] >> 5;
                    uint len_code = matches2[l] & 31;
                    uint skipdist = (uint)((1u << dictionary->words->
                        size_bits_by_length[len_code]) & ~1u) *
                        dictionary->num_transforms;
                    /* TODO(lode): check for dist overflow */
                    dist += skipdist;
                    AddMatch(dist, (nuint)l, len_code, matches);
                }
            }
        }
        return has_found_match;
    }
}
