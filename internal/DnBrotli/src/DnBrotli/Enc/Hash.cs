// Port of c/enc/hash.h (brotli v1.1.0).
//
// Ported: HasherCommon, the score model, the static-dictionary probes
// (Hash14 / TestStaticDictionaryItem / SearchInStaticDictionary),
// BackwardMatch, the Hasher shell with its `privat` union, and the
// HasherSize / HasherSetup / HasherReset / InitOrStitchToPreviousBlock
// lifecycle dispatching on params->hasher.type.
//
// Dispatch arms: the four "quickly" hashers {2, 3, 4, 54}
// (hash_longest_match_quickly_inc.h -> HashLongestMatchQuickly.cs), the two
// chained hashers {5, 6} (hash_longest_match_inc.h /
// hash_longest_match64_inc.h -> HashLongestMatch.cs), the three forgetful-
// chain hashers {40, 41, 42} (hash_forgetful_chain_inc.h ->
// HashForgetfulChain.cs) and the binary-tree hasher {10}
// (hash_to_binary_tree_inc.h -> HashToBinaryTree.cs) are real. The remaining
// types {35, 55, 65} keep NotImplementedException arms (deferred large-window
// composite hashers). The compound-dictionary match helpers
// (FindCompoundDictionaryMatch & friends) are likewise deferred with the
// compound dictionary itself.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static DnBrotli.Enc.MemoryManager;
using static DnBrotli.Enc.Quality;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

/// <summary><c>HasherCommon</c>.</summary>
internal unsafe struct HasherCommon
{
    /* Dynamically allocated areas; regular hasher uses one or two allocations;
       "composite" hasher uses up to 4 allocations. */
    public void* extra0;  /* C: void* extra[4] (void* elements cannot form a fixed buffer) */
    public void* extra1;
    public void* extra2;
    public void* extra3;

    /* False before the first invocation of HasherSetup (where "extra" memory)
       is allocated. */
    public int is_setup_;  /* BROTLI_BOOL */

    public nuint dict_num_lookups;
    public nuint dict_num_matches;

    public BrotliHasherParams @params;

    /* False if hasher needs to be "prepared" before use. */
    public int is_prepared_;  /* BROTLI_BOOL */

    /* Typed accessors over the extra[4] slots above. */
    public void* extra(nuint i)
    {
        switch (i)
        {
            case 0: return extra0;
            case 1: return extra1;
            case 2: return extra2;
            default: return extra3;
        }
    }

    public void set_extra(nuint i, void* value)
    {
        switch (i)
        {
            case 0: extra0 = value; break;
            case 1: extra1 = value; break;
            case 2: extra2 = value; break;
            default: extra3 = value; break;
        }
    }
}

/// <summary><c>struct HasherSearchResult</c>. <c>score_t</c> is <c>size_t</c> -&gt; nuint.</summary>
internal struct HasherSearchResult
{
    public nuint len;
    public nuint distance;
    public nuint score;         /* score_t */
    public int len_code_delta;  /* == len_code - len */
}

/// <summary><c>struct BackwardMatch</c>.</summary>
internal unsafe struct BackwardMatch
{
    public uint distance;
    public uint length_and_code;
}

/// <summary>The <c>union { H10 _H10; H2 _H2; ... } privat</c> of
/// <c>Hasher</c>. The HashLongestMatchQuickly rooms (H2/H3/H4/H54 share one
/// struct layout), the H5/H6 rooms, the HashForgetfulChain rooms (H40/H41/H42
/// share one struct layout) and the H10 room all overlay at offset 0.</summary>
[StructLayout(LayoutKind.Explicit)]
internal unsafe struct HasherPrivat
{
    [FieldOffset(0)] public HashToBinaryTree _H10;
    [FieldOffset(0)] public HashLongestMatchQuickly _H2;
    [FieldOffset(0)] public HashLongestMatchQuickly _H3;
    [FieldOffset(0)] public HashLongestMatchQuickly _H4;
    [FieldOffset(0)] public HashLongestMatchH5 _H5;
    [FieldOffset(0)] public HashLongestMatchH6 _H6;
    [FieldOffset(0)] public HashForgetfulChain _H40;
    [FieldOffset(0)] public HashForgetfulChain _H41;
    [FieldOffset(0)] public HashForgetfulChain _H42;
    [FieldOffset(0)] public HashLongestMatchQuickly _H54;
}

/// <summary><c>Hasher</c>: common part + the per-type union of hash rooms.</summary>
internal unsafe struct Hasher
{
    public HasherCommon common;
    public HasherPrivat privat;
}

internal static unsafe class Hash
{
    internal const uint kCutoffTransformsCount = 10;
    /*   0,  12,   27,    23,    42,    63,    56,    48,    59,    64 */
    /* 0+0, 4+8, 8+19, 12+11, 16+26, 20+43, 24+32, 28+20, 32+27, 36+28 */
    internal const ulong kCutoffTransforms = 0x071B520ADA2D3200UL;  /* BROTLI_MAKE_UINT64_T(0x071B520A, 0xDA2D3200) */

    /* kHashMul32 multiplier has these properties:
       * The multiplier must be odd. Otherwise we may lose the highest bit.
       * No long streaks of ones or zeros.
       * There is no effort to ensure that it is a prime, the oddity is enough
         for this use.
       * The number has been tuned heuristically against compression benchmarks. */
    internal const uint kHashMul32 = 0x1E35A7BD;
    internal const ulong kHashMul64 = 0x1FE35A7BD3579BD3UL;  /* BROTLI_MAKE_UINT64_T(0x1FE35A7Bu, 0xD3579BD3u) */

    /* MAX_NUM_MATCHES == 64 + MAX_TREE_SEARCH_DEPTH (hash.h:255-256). */
    internal const int MAX_NUM_MATCHES_H10 = 128;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Hash14(byte* data)
    {
        uint h = BROTLI_UNALIGNED_LOAD32LE(data) * kHashMul32;
        /* The higher bits contain more mixture from the multiplication,
           so we take our results from there. */
        return h >> (32 - 14);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void PrepareDistanceCache(
        int* distance_cache, int num_distances)
    {
        if (num_distances > 4)
        {
            int last_distance = distance_cache[0];
            distance_cache[4] = last_distance - 1;
            distance_cache[5] = last_distance + 1;
            distance_cache[6] = last_distance - 2;
            distance_cache[7] = last_distance + 2;
            distance_cache[8] = last_distance - 3;
            distance_cache[9] = last_distance + 3;
            if (num_distances > 10)
            {
                int next_last_distance = distance_cache[1];
                distance_cache[10] = next_last_distance - 1;
                distance_cache[11] = next_last_distance + 1;
                distance_cache[12] = next_last_distance - 2;
                distance_cache[13] = next_last_distance + 2;
                distance_cache[14] = next_last_distance - 3;
                distance_cache[15] = next_last_distance + 3;
            }
        }
    }

    internal const nuint BROTLI_LITERAL_BYTE_SCORE = 135;
    internal const nuint BROTLI_DISTANCE_BIT_PENALTY = 30;
    /* Score must be positive after applying maximal penalty. */
    internal const nuint BROTLI_SCORE_BASE = BROTLI_DISTANCE_BIT_PENALTY * 8 * 8;  /* ... * sizeof(size_t) */

    /* Usually, we always choose the longest backward reference. This function
       allows for the exception of that rule.

       If we choose a backward reference that is further away, it will
       usually be coded with more bits. We approximate this by assuming
       log2(distance). If the distance can be expressed in terms of the
       last four distances, we use some heuristic constants to estimate
       the bits cost. For the first up to four literals we use the bit
       cost of the literals from the literal cost model, after that we
       use the average bit cost of the cost model.

       This function is used to sometimes discard a longer backward reference
       when it is not much longer and the bit cost for encoding it is more
       than the saved literals.

       backward_reference_offset MUST be positive. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BackwardReferenceScore(
        nuint copy_length, nuint backward_reference_offset)
    {
        return BROTLI_SCORE_BASE + BROTLI_LITERAL_BYTE_SCORE * copy_length -
            BROTLI_DISTANCE_BIT_PENALTY * Log2FloorNonZero(backward_reference_offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BackwardReferenceScoreUsingLastDistance(nuint copy_length)
    {
        return BROTLI_LITERAL_BYTE_SCORE * copy_length +
            BROTLI_SCORE_BASE + 15;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BackwardReferencePenaltyUsingLastDistance(
        nuint distance_short_code)
    {
        return (nuint)39 + ((0x1CA10u >> (int)(distance_short_code & 0xE)) & 0xE);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TestStaticDictionaryItem(
        BrotliEncoderDictionary* dictionary, nuint len, nuint word_idx,
        byte* data, nuint max_length, nuint max_backward,
        nuint max_distance, HasherSearchResult* @out)
    {
        nuint offset;
        nuint matchlen;
        nuint backward;
        nuint score;
        offset = dictionary->words->offsets_by_length[len] + len * word_idx;
        if (len > max_length)
        {
            return false;
        }

        matchlen =
            FindMatchLength.FindMatchLengthWithLimit(data, &dictionary->words->data[offset], len);
        if (matchlen + dictionary->cutoffTransformsCount <= len || matchlen == 0)
        {
            return false;
        }
        {
            nuint cut = len - matchlen;
            nuint transform_id = (cut << 2) +
                (nuint)((dictionary->cutoffTransforms >> (int)(cut * 6)) & 0x3F);
            backward = max_backward + 1 + word_idx +
                (transform_id << dictionary->words->size_bits_by_length[len]);
        }
        if (backward > max_distance)
        {
            return false;
        }
        score = BackwardReferenceScore(matchlen, backward);
        if (score < @out->score)
        {
            return false;
        }
        @out->len = matchlen;
        @out->len_code_delta = (int)len - (int)matchlen;
        @out->distance = backward;
        @out->score = score;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SearchInStaticDictionary(
        BrotliEncoderDictionary* dictionary,
        HasherCommon* common, byte* data, nuint max_length,
        nuint max_backward, nuint max_distance,
        HasherSearchResult* @out, bool shallow)
    {
        nuint key;
        nuint i;
        if (common->dict_num_matches < (common->dict_num_lookups >> 7))
        {
            return;
        }
        key = (nuint)Hash14(data) << 1;
        for (i = 0; i < (shallow ? 1u : 2u); ++i, ++key)
        {
            common->dict_num_lookups++;
            if (dictionary->hash_table_lengths[key] != 0)
            {
                bool item_matches = TestStaticDictionaryItem(
                    dictionary, dictionary->hash_table_lengths[key],
                    dictionary->hash_table_words[key], data,
                    max_length, max_backward, max_distance, @out);
                if (item_matches)
                {
                    common->dict_num_matches++;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void InitBackwardMatch(BackwardMatch* self, nuint dist, nuint len)
    {
        self->distance = (uint)dist;
        self->length_and_code = (uint)(len << 5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void InitDictionaryBackwardMatch(BackwardMatch* self,
        nuint dist, nuint len, nuint len_code)
    {
        self->distance = (uint)dist;
        self->length_and_code =
            (uint)((len << 5) | (len == len_code ? 0 : len_code));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BackwardMatchLength(BackwardMatch* self)
    {
        return self->length_and_code >> 5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BackwardMatchLengthCode(BackwardMatch* self)
    {
        nuint code = self->length_and_code & 31;
        return code != 0 ? code : BackwardMatchLength(self);
    }

    /* MUST be invoked before any other method. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HasherInit(Hasher* hasher)
    {
        hasher->common.is_setup_ = 0;
        hasher->common.extra0 = null;
        hasher->common.extra1 = null;
        hasher->common.extra2 = null;
        hasher->common.extra3 = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DestroyHasher(MemoryManager* m, Hasher* hasher)
    {
        if (hasher->common.extra0 != null) { BrotliFree(m, hasher->common.extra0); hasher->common.extra0 = null; }
        if (hasher->common.extra1 != null) { BrotliFree(m, hasher->common.extra1); hasher->common.extra1 = null; }
        if (hasher->common.extra2 != null) { BrotliFree(m, hasher->common.extra2); hasher->common.extra2 = null; }
        if (hasher->common.extra3 != null) { BrotliFree(m, hasher->common.extra3); hasher->common.extra3 = null; }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HasherReset(Hasher* hasher)
    {
        hasher->common.is_prepared_ = 0;
    }

    /// <summary>Shared thrower for the not-yet-ported hasher-type arms of the
    /// dispatch switches below.</summary>
    private static Exception UnportedHasherType(int type, string entry_point)
    {
        /* 35/55/65: large-window composite hashers */
        return new NotImplementedException(
            $"DnBrotli DB-deferred: large-window hasher type {type} (c/enc/hash.h {entry_point})");
    }

    /// <summary><c>HasherSize</c>.</summary>
    internal static void HasherSize(BrotliEncoderParams* @params,
        int one_shot, nuint input_size, nuint* alloc_size)
    {
        switch (@params->hasher.type)
        {
            case 2:
                HashLongestMatchQuickly<H2Policy>.HashMemAllocInBytes(@params, one_shot, input_size, alloc_size);
                break;
            case 3:
                HashLongestMatchQuickly<H3Policy>.HashMemAllocInBytes(@params, one_shot, input_size, alloc_size);
                break;
            case 4:
                HashLongestMatchQuickly<H4Policy>.HashMemAllocInBytes(@params, one_shot, input_size, alloc_size);
                break;
            case 5:
                H5.HashMemAllocInBytes(@params, one_shot, input_size, alloc_size);
                break;
            case 6:
                H6.HashMemAllocInBytes(@params, one_shot, input_size, alloc_size);
                break;
            case 40:
                HashForgetfulChain<H40Policy>.HashMemAllocInBytes(@params, one_shot, input_size, alloc_size);
                break;
            case 41:
                HashForgetfulChain<H41Policy>.HashMemAllocInBytes(@params, one_shot, input_size, alloc_size);
                break;
            case 42:
                HashForgetfulChain<H42Policy>.HashMemAllocInBytes(@params, one_shot, input_size, alloc_size);
                break;
            case 54:
                HashLongestMatchQuickly<H54Policy>.HashMemAllocInBytes(@params, one_shot, input_size, alloc_size);
                break;
            case 10:
                H10.HashMemAllocInBytes(@params, one_shot, input_size, alloc_size);
                break;
            case 35: case 55: case 65:
                throw UnportedHasherType(@params->hasher.type, "HasherSize");
            default:
                break;
        }
    }

    /// <summary><c>HasherSetup</c>.</summary>
    internal static void HasherSetup(MemoryManager* m, Hasher* hasher,
        BrotliEncoderParams* @params, byte* data, nuint position,
        nuint input_size, int is_last)
    {
        int one_shot = (position == 0 && is_last != 0) ? 1 : 0;
        if (hasher->common.is_setup_ == 0)
        {
            nuint* alloc_size = stackalloc nuint[4] { 0, 0, 0, 0 };
            nuint i;
            ChooseHasher(@params, &@params->hasher);
            hasher->common.@params = @params->hasher;
            hasher->common.dict_num_lookups = 0;
            hasher->common.dict_num_matches = 0;
            HasherSize(@params, one_shot, input_size, alloc_size);
            for (i = 0; i < 4; ++i)
            {
                if (alloc_size[i] == 0) continue;
                hasher->common.set_extra(i, BROTLI_ALLOC<byte>(m, alloc_size[i]));
                if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(hasher->common.extra(i))) return;
            }
            switch (hasher->common.@params.type)
            {
                case 2:
                    HashLongestMatchQuickly<H2Policy>.Initialize(&hasher->common, &hasher->privat._H2, @params);
                    break;
                case 3:
                    HashLongestMatchQuickly<H3Policy>.Initialize(&hasher->common, &hasher->privat._H3, @params);
                    break;
                case 4:
                    HashLongestMatchQuickly<H4Policy>.Initialize(&hasher->common, &hasher->privat._H4, @params);
                    break;
                case 5:
                    H5.Initialize(&hasher->common, &hasher->privat._H5, @params);
                    break;
                case 6:
                    H6.Initialize(&hasher->common, &hasher->privat._H6, @params);
                    break;
                case 40:
                    HashForgetfulChain<H40Policy>.Initialize(&hasher->common, &hasher->privat._H40, @params);
                    break;
                case 41:
                    HashForgetfulChain<H41Policy>.Initialize(&hasher->common, &hasher->privat._H41, @params);
                    break;
                case 42:
                    HashForgetfulChain<H42Policy>.Initialize(&hasher->common, &hasher->privat._H42, @params);
                    break;
                case 54:
                    HashLongestMatchQuickly<H54Policy>.Initialize(&hasher->common, &hasher->privat._H54, @params);
                    break;
                case 10:
                    H10.Initialize(&hasher->common, &hasher->privat._H10, @params);
                    break;
                case 35: case 55: case 65:
                    throw UnportedHasherType(hasher->common.@params.type, "HasherSetup/Initialize");
                default:
                    break;
            }
            HasherReset(hasher);
            hasher->common.is_setup_ = 1;
        }

        if (hasher->common.is_prepared_ == 0)
        {
            switch (hasher->common.@params.type)
            {
                case 2:
                    HashLongestMatchQuickly<H2Policy>.Prepare(&hasher->privat._H2, one_shot, input_size, data);
                    break;
                case 3:
                    HashLongestMatchQuickly<H3Policy>.Prepare(&hasher->privat._H3, one_shot, input_size, data);
                    break;
                case 4:
                    HashLongestMatchQuickly<H4Policy>.Prepare(&hasher->privat._H4, one_shot, input_size, data);
                    break;
                case 5:
                    H5.Prepare(&hasher->privat._H5, one_shot, input_size, data);
                    break;
                case 6:
                    H6.Prepare(&hasher->privat._H6, one_shot, input_size, data);
                    break;
                case 40:
                    HashForgetfulChain<H40Policy>.Prepare(&hasher->privat._H40, one_shot, input_size, data);
                    break;
                case 41:
                    HashForgetfulChain<H41Policy>.Prepare(&hasher->privat._H41, one_shot, input_size, data);
                    break;
                case 42:
                    HashForgetfulChain<H42Policy>.Prepare(&hasher->privat._H42, one_shot, input_size, data);
                    break;
                case 54:
                    HashLongestMatchQuickly<H54Policy>.Prepare(&hasher->privat._H54, one_shot, input_size, data);
                    break;
                case 10:
                    H10.Prepare(&hasher->privat._H10, one_shot, input_size, data);
                    break;
                case 35: case 55: case 65:
                    throw UnportedHasherType(hasher->common.@params.type, "HasherSetup/Prepare");
                default:
                    break;
            }
            hasher->common.is_prepared_ = 1;
        }
    }

    /// <summary><c>InitOrStitchToPreviousBlock</c>.</summary>
    internal static void InitOrStitchToPreviousBlock(
        MemoryManager* m, Hasher* hasher, byte* data, nuint mask,
        BrotliEncoderParams* @params, nuint position, nuint input_size,
        int is_last)
    {
        HasherSetup(m, hasher, @params, data, position, input_size, is_last);
        if (BROTLI_IS_OOM(m)) return;
        switch (hasher->common.@params.type)
        {
            case 2:
                HashLongestMatchQuickly<H2Policy>.StitchToPreviousBlock(&hasher->privat._H2, input_size, position, data, mask);
                break;
            case 3:
                HashLongestMatchQuickly<H3Policy>.StitchToPreviousBlock(&hasher->privat._H3, input_size, position, data, mask);
                break;
            case 4:
                HashLongestMatchQuickly<H4Policy>.StitchToPreviousBlock(&hasher->privat._H4, input_size, position, data, mask);
                break;
            case 5:
                H5.StitchToPreviousBlock(&hasher->privat._H5, input_size, position, data, mask);
                break;
            case 6:
                H6.StitchToPreviousBlock(&hasher->privat._H6, input_size, position, data, mask);
                break;
            case 40:
                HashForgetfulChain<H40Policy>.StitchToPreviousBlock(&hasher->privat._H40, input_size, position, data, mask);
                break;
            case 41:
                HashForgetfulChain<H41Policy>.StitchToPreviousBlock(&hasher->privat._H41, input_size, position, data, mask);
                break;
            case 42:
                HashForgetfulChain<H42Policy>.StitchToPreviousBlock(&hasher->privat._H42, input_size, position, data, mask);
                break;
            case 54:
                HashLongestMatchQuickly<H54Policy>.StitchToPreviousBlock(&hasher->privat._H54, input_size, position, data, mask);
                break;
            case 10:
                H10.StitchToPreviousBlock(&hasher->privat._H10, input_size, position, data, mask);
                break;
            case 35: case 55: case 65:
                throw UnportedHasherType(hasher->common.@params.type, "InitOrStitchToPreviousBlock");
            default:
                break;
        }
    }
}
