// Port of c/enc/backward_references.{h,c} (brotli v1.1.0): the greedy/lazy
// match emission loop for qualities 2..9.
//
// The C expands backward_references_inc.h once per hasher (the N-prefix
// ENABLE_COMPOUND_DICTIONARY=0 family plus the D-prefix compound family).
// Here the four "quickly" hashers (H2/H3/H4/H54) share one generic body
// CreateBackwardReferences<TPolicy>, H5/H6 get their own expansions
// (CreateBackwardReferencesH5/H6) and the three forgetful-chain hashers
// (H40/H41/H42) share the generic body CreateBackwardReferencesFC<TPolicy>;
// every body is the same template text, only the hasher calls and the privat
// union member differ. BrotliCreateBackwardReferences mirrors the C
// FOR_GENERIC_HASHERS dispatch, with NotImplementedException arms for the
// not-yet-ported hasher types ({35,55,65} = deferred; type 10 is never routed
// here — EncodeData sends q10/q11 to the Zopfli entry points). The D-prefix
// (compound dictionary) family is deferred with the compound dictionary
// itself.

using DnBrotli.Common;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Context;
using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.Command;
using static DnBrotli.Enc.Hash;
using static DnBrotli.Enc.Quality;

namespace DnBrotli.Enc;

internal static unsafe class BackwardReferences
{
    private static nuint ComputeDistanceCode(nuint distance,
                                             nuint max_distance,
                                             int* dist_cache)
    {
        if (distance <= max_distance)
        {
            nuint distance_plus_3 = distance + 3;
            nuint offset0 = distance_plus_3 - (nuint)dist_cache[0];
            nuint offset1 = distance_plus_3 - (nuint)dist_cache[1];
            if (distance == (nuint)dist_cache[0])
            {
                return 0;
            }
            else if (distance == (nuint)dist_cache[1])
            {
                return 1;
            }
            else if (offset0 < 7)
            {
                return (0x9750468u >> (int)(4 * offset0)) & 0xF;
            }
            else if (offset1 < 7)
            {
                return (0xFDB1ACEu >> (int)(4 * offset1)) & 0xF;
            }
            else if (distance == (nuint)dist_cache[2])
            {
                return 2;
            }
            else if (distance == (nuint)dist_cache[3])
            {
                return 3;
            }
        }
        return distance + BROTLI_NUM_DISTANCE_SHORT_CODES - 1;
    }

    /// <summary><c>CreateBackwardReferencesNH{2,3,4,54}</c>
    /// (backward_references_inc.h, ENABLE_COMPOUND_DICTIONARY == 0),
    /// specialized per quickly-hasher policy.</summary>
    private static void CreateBackwardReferences<TPolicy>(
        nuint num_bytes, nuint position,
        byte* ringbuffer, nuint ringbuffer_mask,
        byte* literal_context_lut, BrotliEncoderParams* @params,
        Hasher* hasher, int* dist_cache, nuint* last_insert_len,
        Command* commands, nuint* num_commands, nuint* num_literals)
        where TPolicy : struct, IQuicklyPolicy
    {
        /* All quickly hashers share one state layout; the union members _H2/_H3/
           _H4/_H54 alias the same storage (hash.h: &hasher->privat.FN(_)). */
        HashLongestMatchQuickly* privat = &hasher->privat._H2;
        /* Set maximum distance, see section 9.1. of the spec. */
        nuint max_backward_limit = MaxBackwardLimit(@params->lgwin);
        nuint position_offset = @params->stream_offset;

        Command* orig_commands = commands;
        nuint insert_length = *last_insert_len;
        nuint pos_end = position + num_bytes;
        nuint store_end = num_bytes >= HashLongestMatchQuickly<TPolicy>.StoreLookahead() ?
            position + num_bytes - HashLongestMatchQuickly<TPolicy>.StoreLookahead() + 1 : position;

        /* For speed up heuristics for random data. */
        nuint random_heuristics_window_size =
            LiteralSpreeLengthForSparseSearch(@params);
        nuint apply_random_heuristics = position + random_heuristics_window_size;
        nuint gap = @params->dictionary.compound.total_size;

        /* Minimum score to accept a backward reference. */
        const nuint kMinScore = BROTLI_SCORE_BASE + 100;

        HashLongestMatchQuickly<TPolicy>.PrepareDistanceCache(privat, dist_cache);

        while (position + HashLongestMatchQuickly<TPolicy>.HashTypeLength() < pos_end)
        {
            nuint max_length = pos_end - position;
            nuint max_distance = BROTLI_MIN(position, max_backward_limit);
            nuint dictionary_start = BROTLI_MIN(
                position + position_offset, max_backward_limit);
            HasherSearchResult sr;
            nuint dict_id = 0;
            byte p1 = 0;
            byte p2 = 0;
            if (@params->dictionary.contextual.context_based != 0)
            {
                p1 = position >= 1 ?
                    ringbuffer[(position - 1) & ringbuffer_mask] : (byte)0;
                p2 = position >= 2 ?
                    ringbuffer[(position - 2) & ringbuffer_mask] : (byte)0;
                dict_id = @params->dictionary.contextual.context_map[
                    BROTLI_CONTEXT(p1, p2, literal_context_lut)];
            }
            sr.len = 0;
            sr.len_code_delta = 0;
            sr.distance = 0;
            sr.score = kMinScore;
            HashLongestMatchQuickly<TPolicy>.FindLongestMatch(privat,
                @params->dictionary.contextual.dict(dict_id),
                ringbuffer, ringbuffer_mask, dist_cache, position, max_length,
                max_distance, dictionary_start + gap, @params->dist.max_distance, &sr);
            /* ENABLE_COMPOUND_DICTIONARY == 0: no LookupCompoundDictionaryMatch. */
            if (sr.score > kMinScore)
            {
                /* Found a match. Let's look for something even better ahead. */
                int delayed_backward_references_in_row = 0;
                --max_length;
                for (; ; --max_length)
                {
                    const nuint cost_diff_lazy = 175;
                    HasherSearchResult sr2;
                    sr2.len = @params->quality < MIN_QUALITY_FOR_EXTENSIVE_REFERENCE_SEARCH ?
                        BROTLI_MIN(sr.len - 1, max_length) : 0;
                    sr2.len_code_delta = 0;
                    sr2.distance = 0;
                    sr2.score = kMinScore;
                    max_distance = BROTLI_MIN(position + 1, max_backward_limit);
                    dictionary_start = BROTLI_MIN(
                        position + 1 + position_offset, max_backward_limit);
                    if (@params->dictionary.contextual.context_based != 0)
                    {
                        p2 = p1;
                        p1 = ringbuffer[position & ringbuffer_mask];
                        dict_id = @params->dictionary.contextual.context_map[
                            BROTLI_CONTEXT(p1, p2, literal_context_lut)];
                    }
                    HashLongestMatchQuickly<TPolicy>.FindLongestMatch(privat,
                        @params->dictionary.contextual.dict(dict_id),
                        ringbuffer, ringbuffer_mask, dist_cache, position + 1, max_length,
                        max_distance, dictionary_start + gap, @params->dist.max_distance,
                        &sr2);
                    /* ENABLE_COMPOUND_DICTIONARY == 0: no LookupCompoundDictionaryMatch. */
                    if (sr2.score >= sr.score + cost_diff_lazy)
                    {
                        /* Ok, let's just write one byte for now and start a match from the
                           next byte. */
                        ++position;
                        ++insert_length;
                        sr = sr2;
                        if (++delayed_backward_references_in_row < 4 &&
                            position + HashLongestMatchQuickly<TPolicy>.HashTypeLength() < pos_end)
                        {
                            continue;
                        }
                    }
                    break;
                }
                apply_random_heuristics =
                    position + 2 * sr.len + random_heuristics_window_size;
                dictionary_start = BROTLI_MIN(
                    position + position_offset, max_backward_limit);
                {
                    /* The first 16 codes are special short-codes,
                       and the minimum offset is 1. */
                    nuint distance_code = ComputeDistanceCode(
                        sr.distance, dictionary_start + gap, dist_cache);
                    if ((sr.distance <= (dictionary_start + gap)) && distance_code > 0)
                    {
                        dist_cache[3] = dist_cache[2];
                        dist_cache[2] = dist_cache[1];
                        dist_cache[1] = dist_cache[0];
                        dist_cache[0] = (int)sr.distance;
                        HashLongestMatchQuickly<TPolicy>.PrepareDistanceCache(privat, dist_cache);
                    }
                    InitCommand(commands++, &@params->dist, insert_length,
                        sr.len, sr.len_code_delta, distance_code);
                }
                *num_literals += insert_length;
                insert_length = 0;
                /* Put the hash keys into the table, if there are enough bytes left.
                   Depending on the hasher implementation, it can push all positions
                   in the given range or only a subset of them.
                   Avoid hash poisoning with RLE data. */
                {
                    nuint range_start = position + 2;
                    nuint range_end = BROTLI_MIN(position + sr.len, store_end);
                    if (sr.distance < (sr.len >> 2))
                    {
                        range_start = BROTLI_MIN(range_end, BROTLI_MAX(
                            range_start, position + sr.len - (sr.distance << 2)));
                    }
                    HashLongestMatchQuickly<TPolicy>.StoreRange(privat, ringbuffer,
                        ringbuffer_mask, range_start, range_end);
                }
                position += sr.len;
            }
            else
            {
                ++insert_length;
                ++position;
                /* If we have not seen matches for a long time, we can skip some
                   match lookups. Unsuccessful match lookups are very very expensive
                   and this kind of a heuristic speeds up compression quite
                   a lot. */
                if (position > apply_random_heuristics)
                {
                    /* Going through uncompressible data, jump. */
                    if (position >
                        apply_random_heuristics + 4 * random_heuristics_window_size)
                    {
                        /* It is quite a long time since we saw a copy, so we assume
                           that this data is not compressible, and store hashes less
                           often. Hashes of non compressible data are less likely to
                           turn out to be useful in the future, too, so we store less of
                           them to not to flood out the hash table of good compressible
                           data. */
                        nuint kMargin =
                            BROTLI_MAX(HashLongestMatchQuickly<TPolicy>.StoreLookahead() - 1, 4);
                        nuint pos_jump =
                            BROTLI_MIN(position + 16, pos_end - kMargin);
                        for (; position < pos_jump; position += 4)
                        {
                            HashLongestMatchQuickly<TPolicy>.Store(privat, ringbuffer,
                                ringbuffer_mask, position);
                            insert_length += 4;
                        }
                    }
                    else
                    {
                        nuint kMargin =
                            BROTLI_MAX(HashLongestMatchQuickly<TPolicy>.StoreLookahead() - 1, 2);
                        nuint pos_jump =
                            BROTLI_MIN(position + 8, pos_end - kMargin);
                        for (; position < pos_jump; position += 2)
                        {
                            HashLongestMatchQuickly<TPolicy>.Store(privat, ringbuffer,
                                ringbuffer_mask, position);
                            insert_length += 2;
                        }
                    }
                }
            }
        }
        insert_length += pos_end - position;
        *last_insert_len = insert_length;
        *num_commands += (nuint)(commands - orig_commands);
    }

    /// <summary><c>CreateBackwardReferencesNH5</c>
    /// (backward_references_inc.h, ENABLE_COMPOUND_DICTIONARY == 0). Same
    /// template text as <see cref="CreateBackwardReferences{TPolicy}"/>; only
    /// the hasher calls and the privat union member differ.</summary>
    private static void CreateBackwardReferencesH5(
        nuint num_bytes, nuint position,
        byte* ringbuffer, nuint ringbuffer_mask,
        byte* literal_context_lut, BrotliEncoderParams* @params,
        Hasher* hasher, int* dist_cache, nuint* last_insert_len,
        Command* commands, nuint* num_commands, nuint* num_literals)
    {
        HashLongestMatchH5* privat = &hasher->privat._H5;
        /* Set maximum distance, see section 9.1. of the spec. */
        nuint max_backward_limit = MaxBackwardLimit(@params->lgwin);
        nuint position_offset = @params->stream_offset;

        Command* orig_commands = commands;
        nuint insert_length = *last_insert_len;
        nuint pos_end = position + num_bytes;
        nuint store_end = num_bytes >= H5.StoreLookahead() ?
            position + num_bytes - H5.StoreLookahead() + 1 : position;

        /* For speed up heuristics for random data. */
        nuint random_heuristics_window_size =
            LiteralSpreeLengthForSparseSearch(@params);
        nuint apply_random_heuristics = position + random_heuristics_window_size;
        nuint gap = @params->dictionary.compound.total_size;

        /* Minimum score to accept a backward reference. */
        const nuint kMinScore = BROTLI_SCORE_BASE + 100;

        H5.PrepareDistanceCache(privat, dist_cache);

        while (position + H5.HashTypeLength() < pos_end)
        {
            nuint max_length = pos_end - position;
            nuint max_distance = BROTLI_MIN(position, max_backward_limit);
            nuint dictionary_start = BROTLI_MIN(
                position + position_offset, max_backward_limit);
            HasherSearchResult sr;
            nuint dict_id = 0;
            byte p1 = 0;
            byte p2 = 0;
            if (@params->dictionary.contextual.context_based != 0)
            {
                p1 = position >= 1 ?
                    ringbuffer[(position - 1) & ringbuffer_mask] : (byte)0;
                p2 = position >= 2 ?
                    ringbuffer[(position - 2) & ringbuffer_mask] : (byte)0;
                dict_id = @params->dictionary.contextual.context_map[
                    BROTLI_CONTEXT(p1, p2, literal_context_lut)];
            }
            sr.len = 0;
            sr.len_code_delta = 0;
            sr.distance = 0;
            sr.score = kMinScore;
            H5.FindLongestMatch(privat,
                @params->dictionary.contextual.dict(dict_id),
                ringbuffer, ringbuffer_mask, dist_cache, position, max_length,
                max_distance, dictionary_start + gap, @params->dist.max_distance, &sr);
            /* ENABLE_COMPOUND_DICTIONARY == 0: no LookupCompoundDictionaryMatch. */
            if (sr.score > kMinScore)
            {
                /* Found a match. Let's look for something even better ahead. */
                int delayed_backward_references_in_row = 0;
                --max_length;
                for (; ; --max_length)
                {
                    const nuint cost_diff_lazy = 175;
                    HasherSearchResult sr2;
                    sr2.len = @params->quality < MIN_QUALITY_FOR_EXTENSIVE_REFERENCE_SEARCH ?
                        BROTLI_MIN(sr.len - 1, max_length) : 0;
                    sr2.len_code_delta = 0;
                    sr2.distance = 0;
                    sr2.score = kMinScore;
                    max_distance = BROTLI_MIN(position + 1, max_backward_limit);
                    dictionary_start = BROTLI_MIN(
                        position + 1 + position_offset, max_backward_limit);
                    if (@params->dictionary.contextual.context_based != 0)
                    {
                        p2 = p1;
                        p1 = ringbuffer[position & ringbuffer_mask];
                        dict_id = @params->dictionary.contextual.context_map[
                            BROTLI_CONTEXT(p1, p2, literal_context_lut)];
                    }
                    H5.FindLongestMatch(privat,
                        @params->dictionary.contextual.dict(dict_id),
                        ringbuffer, ringbuffer_mask, dist_cache, position + 1, max_length,
                        max_distance, dictionary_start + gap, @params->dist.max_distance,
                        &sr2);
                    /* ENABLE_COMPOUND_DICTIONARY == 0: no LookupCompoundDictionaryMatch. */
                    if (sr2.score >= sr.score + cost_diff_lazy)
                    {
                        /* Ok, let's just write one byte for now and start a match from the
                           next byte. */
                        ++position;
                        ++insert_length;
                        sr = sr2;
                        if (++delayed_backward_references_in_row < 4 &&
                            position + H5.HashTypeLength() < pos_end)
                        {
                            continue;
                        }
                    }
                    break;
                }
                apply_random_heuristics =
                    position + 2 * sr.len + random_heuristics_window_size;
                dictionary_start = BROTLI_MIN(
                    position + position_offset, max_backward_limit);
                {
                    /* The first 16 codes are special short-codes,
                       and the minimum offset is 1. */
                    nuint distance_code = ComputeDistanceCode(
                        sr.distance, dictionary_start + gap, dist_cache);
                    if ((sr.distance <= (dictionary_start + gap)) && distance_code > 0)
                    {
                        dist_cache[3] = dist_cache[2];
                        dist_cache[2] = dist_cache[1];
                        dist_cache[1] = dist_cache[0];
                        dist_cache[0] = (int)sr.distance;
                        H5.PrepareDistanceCache(privat, dist_cache);
                    }
                    InitCommand(commands++, &@params->dist, insert_length,
                        sr.len, sr.len_code_delta, distance_code);
                }
                *num_literals += insert_length;
                insert_length = 0;
                /* Put the hash keys into the table, if there are enough bytes left.
                   Depending on the hasher implementation, it can push all positions
                   in the given range or only a subset of them.
                   Avoid hash poisoning with RLE data. */
                {
                    nuint range_start = position + 2;
                    nuint range_end = BROTLI_MIN(position + sr.len, store_end);
                    if (sr.distance < (sr.len >> 2))
                    {
                        range_start = BROTLI_MIN(range_end, BROTLI_MAX(
                            range_start, position + sr.len - (sr.distance << 2)));
                    }
                    H5.StoreRange(privat, ringbuffer,
                        ringbuffer_mask, range_start, range_end);
                }
                position += sr.len;
            }
            else
            {
                ++insert_length;
                ++position;
                /* If we have not seen matches for a long time, we can skip some
                   match lookups. Unsuccessful match lookups are very very expensive
                   and this kind of a heuristic speeds up compression quite
                   a lot. */
                if (position > apply_random_heuristics)
                {
                    /* Going through uncompressible data, jump. */
                    if (position >
                        apply_random_heuristics + 4 * random_heuristics_window_size)
                    {
                        /* It is quite a long time since we saw a copy, so we assume
                           that this data is not compressible, and store hashes less
                           often. Hashes of non compressible data are less likely to
                           turn out to be useful in the future, too, so we store less of
                           them to not to flood out the hash table of good compressible
                           data. */
                        nuint kMargin =
                            BROTLI_MAX(H5.StoreLookahead() - 1, 4);
                        nuint pos_jump =
                            BROTLI_MIN(position + 16, pos_end - kMargin);
                        for (; position < pos_jump; position += 4)
                        {
                            H5.Store(privat, ringbuffer,
                                ringbuffer_mask, position);
                            insert_length += 4;
                        }
                    }
                    else
                    {
                        nuint kMargin =
                            BROTLI_MAX(H5.StoreLookahead() - 1, 2);
                        nuint pos_jump =
                            BROTLI_MIN(position + 8, pos_end - kMargin);
                        for (; position < pos_jump; position += 2)
                        {
                            H5.Store(privat, ringbuffer,
                                ringbuffer_mask, position);
                            insert_length += 2;
                        }
                    }
                }
            }
        }
        insert_length += pos_end - position;
        *last_insert_len = insert_length;
        *num_commands += (nuint)(commands - orig_commands);
    }

    /// <summary><c>CreateBackwardReferencesNH6</c>
    /// (backward_references_inc.h, ENABLE_COMPOUND_DICTIONARY == 0). Same
    /// template text as <see cref="CreateBackwardReferences{TPolicy}"/>; only
    /// the hasher calls and the privat union member differ.</summary>
    private static void CreateBackwardReferencesH6(
        nuint num_bytes, nuint position,
        byte* ringbuffer, nuint ringbuffer_mask,
        byte* literal_context_lut, BrotliEncoderParams* @params,
        Hasher* hasher, int* dist_cache, nuint* last_insert_len,
        Command* commands, nuint* num_commands, nuint* num_literals)
    {
        HashLongestMatchH6* privat = &hasher->privat._H6;
        /* Set maximum distance, see section 9.1. of the spec. */
        nuint max_backward_limit = MaxBackwardLimit(@params->lgwin);
        nuint position_offset = @params->stream_offset;

        Command* orig_commands = commands;
        nuint insert_length = *last_insert_len;
        nuint pos_end = position + num_bytes;
        nuint store_end = num_bytes >= H6.StoreLookahead() ?
            position + num_bytes - H6.StoreLookahead() + 1 : position;

        /* For speed up heuristics for random data. */
        nuint random_heuristics_window_size =
            LiteralSpreeLengthForSparseSearch(@params);
        nuint apply_random_heuristics = position + random_heuristics_window_size;
        nuint gap = @params->dictionary.compound.total_size;

        /* Minimum score to accept a backward reference. */
        const nuint kMinScore = BROTLI_SCORE_BASE + 100;

        H6.PrepareDistanceCache(privat, dist_cache);

        while (position + H6.HashTypeLength() < pos_end)
        {
            nuint max_length = pos_end - position;
            nuint max_distance = BROTLI_MIN(position, max_backward_limit);
            nuint dictionary_start = BROTLI_MIN(
                position + position_offset, max_backward_limit);
            HasherSearchResult sr;
            nuint dict_id = 0;
            byte p1 = 0;
            byte p2 = 0;
            if (@params->dictionary.contextual.context_based != 0)
            {
                p1 = position >= 1 ?
                    ringbuffer[(position - 1) & ringbuffer_mask] : (byte)0;
                p2 = position >= 2 ?
                    ringbuffer[(position - 2) & ringbuffer_mask] : (byte)0;
                dict_id = @params->dictionary.contextual.context_map[
                    BROTLI_CONTEXT(p1, p2, literal_context_lut)];
            }
            sr.len = 0;
            sr.len_code_delta = 0;
            sr.distance = 0;
            sr.score = kMinScore;
            H6.FindLongestMatch(privat,
                @params->dictionary.contextual.dict(dict_id),
                ringbuffer, ringbuffer_mask, dist_cache, position, max_length,
                max_distance, dictionary_start + gap, @params->dist.max_distance, &sr);
            /* ENABLE_COMPOUND_DICTIONARY == 0: no LookupCompoundDictionaryMatch. */
            if (sr.score > kMinScore)
            {
                /* Found a match. Let's look for something even better ahead. */
                int delayed_backward_references_in_row = 0;
                --max_length;
                for (; ; --max_length)
                {
                    const nuint cost_diff_lazy = 175;
                    HasherSearchResult sr2;
                    sr2.len = @params->quality < MIN_QUALITY_FOR_EXTENSIVE_REFERENCE_SEARCH ?
                        BROTLI_MIN(sr.len - 1, max_length) : 0;
                    sr2.len_code_delta = 0;
                    sr2.distance = 0;
                    sr2.score = kMinScore;
                    max_distance = BROTLI_MIN(position + 1, max_backward_limit);
                    dictionary_start = BROTLI_MIN(
                        position + 1 + position_offset, max_backward_limit);
                    if (@params->dictionary.contextual.context_based != 0)
                    {
                        p2 = p1;
                        p1 = ringbuffer[position & ringbuffer_mask];
                        dict_id = @params->dictionary.contextual.context_map[
                            BROTLI_CONTEXT(p1, p2, literal_context_lut)];
                    }
                    H6.FindLongestMatch(privat,
                        @params->dictionary.contextual.dict(dict_id),
                        ringbuffer, ringbuffer_mask, dist_cache, position + 1, max_length,
                        max_distance, dictionary_start + gap, @params->dist.max_distance,
                        &sr2);
                    /* ENABLE_COMPOUND_DICTIONARY == 0: no LookupCompoundDictionaryMatch. */
                    if (sr2.score >= sr.score + cost_diff_lazy)
                    {
                        /* Ok, let's just write one byte for now and start a match from the
                           next byte. */
                        ++position;
                        ++insert_length;
                        sr = sr2;
                        if (++delayed_backward_references_in_row < 4 &&
                            position + H6.HashTypeLength() < pos_end)
                        {
                            continue;
                        }
                    }
                    break;
                }
                apply_random_heuristics =
                    position + 2 * sr.len + random_heuristics_window_size;
                dictionary_start = BROTLI_MIN(
                    position + position_offset, max_backward_limit);
                {
                    /* The first 16 codes are special short-codes,
                       and the minimum offset is 1. */
                    nuint distance_code = ComputeDistanceCode(
                        sr.distance, dictionary_start + gap, dist_cache);
                    if ((sr.distance <= (dictionary_start + gap)) && distance_code > 0)
                    {
                        dist_cache[3] = dist_cache[2];
                        dist_cache[2] = dist_cache[1];
                        dist_cache[1] = dist_cache[0];
                        dist_cache[0] = (int)sr.distance;
                        H6.PrepareDistanceCache(privat, dist_cache);
                    }
                    InitCommand(commands++, &@params->dist, insert_length,
                        sr.len, sr.len_code_delta, distance_code);
                }
                *num_literals += insert_length;
                insert_length = 0;
                /* Put the hash keys into the table, if there are enough bytes left.
                   Depending on the hasher implementation, it can push all positions
                   in the given range or only a subset of them.
                   Avoid hash poisoning with RLE data. */
                {
                    nuint range_start = position + 2;
                    nuint range_end = BROTLI_MIN(position + sr.len, store_end);
                    if (sr.distance < (sr.len >> 2))
                    {
                        range_start = BROTLI_MIN(range_end, BROTLI_MAX(
                            range_start, position + sr.len - (sr.distance << 2)));
                    }
                    H6.StoreRange(privat, ringbuffer,
                        ringbuffer_mask, range_start, range_end);
                }
                position += sr.len;
            }
            else
            {
                ++insert_length;
                ++position;
                /* If we have not seen matches for a long time, we can skip some
                   match lookups. Unsuccessful match lookups are very very expensive
                   and this kind of a heuristic speeds up compression quite
                   a lot. */
                if (position > apply_random_heuristics)
                {
                    /* Going through uncompressible data, jump. */
                    if (position >
                        apply_random_heuristics + 4 * random_heuristics_window_size)
                    {
                        /* It is quite a long time since we saw a copy, so we assume
                           that this data is not compressible, and store hashes less
                           often. Hashes of non compressible data are less likely to
                           turn out to be useful in the future, too, so we store less of
                           them to not to flood out the hash table of good compressible
                           data. */
                        nuint kMargin =
                            BROTLI_MAX(H6.StoreLookahead() - 1, 4);
                        nuint pos_jump =
                            BROTLI_MIN(position + 16, pos_end - kMargin);
                        for (; position < pos_jump; position += 4)
                        {
                            H6.Store(privat, ringbuffer,
                                ringbuffer_mask, position);
                            insert_length += 4;
                        }
                    }
                    else
                    {
                        nuint kMargin =
                            BROTLI_MAX(H6.StoreLookahead() - 1, 2);
                        nuint pos_jump =
                            BROTLI_MIN(position + 8, pos_end - kMargin);
                        for (; position < pos_jump; position += 2)
                        {
                            H6.Store(privat, ringbuffer,
                                ringbuffer_mask, position);
                            insert_length += 2;
                        }
                    }
                }
            }
        }
        insert_length += pos_end - position;
        *last_insert_len = insert_length;
        *num_commands += (nuint)(commands - orig_commands);
    }

    /// <summary><c>CreateBackwardReferencesNH{40,41,42}</c>
    /// (backward_references_inc.h, ENABLE_COMPOUND_DICTIONARY == 0),
    /// specialized per forgetful-chain policy. Same template text as
    /// <see cref="CreateBackwardReferences{TPolicy}"/>; only the hasher calls
    /// and the privat union member differ.</summary>
    private static void CreateBackwardReferencesFC<TPolicy>(
        nuint num_bytes, nuint position,
        byte* ringbuffer, nuint ringbuffer_mask,
        byte* literal_context_lut, BrotliEncoderParams* @params,
        Hasher* hasher, int* dist_cache, nuint* last_insert_len,
        Command* commands, nuint* num_commands, nuint* num_literals)
        where TPolicy : struct, IForgetfulChainPolicy
    {
        /* All forgetful-chain hashers share one state layout; the union members
           _H40/_H41/_H42 alias the same storage (hash.h: &hasher->privat.FN(_)). */
        HashForgetfulChain* privat = &hasher->privat._H40;
        /* Set maximum distance, see section 9.1. of the spec. */
        nuint max_backward_limit = MaxBackwardLimit(@params->lgwin);
        nuint position_offset = @params->stream_offset;

        Command* orig_commands = commands;
        nuint insert_length = *last_insert_len;
        nuint pos_end = position + num_bytes;
        nuint store_end = num_bytes >= HashForgetfulChain<TPolicy>.StoreLookahead() ?
            position + num_bytes - HashForgetfulChain<TPolicy>.StoreLookahead() + 1 : position;

        /* For speed up heuristics for random data. */
        nuint random_heuristics_window_size =
            LiteralSpreeLengthForSparseSearch(@params);
        nuint apply_random_heuristics = position + random_heuristics_window_size;
        nuint gap = @params->dictionary.compound.total_size;

        /* Minimum score to accept a backward reference. */
        const nuint kMinScore = BROTLI_SCORE_BASE + 100;

        HashForgetfulChain<TPolicy>.PrepareDistanceCache(privat, dist_cache);

        while (position + HashForgetfulChain<TPolicy>.HashTypeLength() < pos_end)
        {
            nuint max_length = pos_end - position;
            nuint max_distance = BROTLI_MIN(position, max_backward_limit);
            nuint dictionary_start = BROTLI_MIN(
                position + position_offset, max_backward_limit);
            HasherSearchResult sr;
            nuint dict_id = 0;
            byte p1 = 0;
            byte p2 = 0;
            if (@params->dictionary.contextual.context_based != 0)
            {
                p1 = position >= 1 ?
                    ringbuffer[(position - 1) & ringbuffer_mask] : (byte)0;
                p2 = position >= 2 ?
                    ringbuffer[(position - 2) & ringbuffer_mask] : (byte)0;
                dict_id = @params->dictionary.contextual.context_map[
                    BROTLI_CONTEXT(p1, p2, literal_context_lut)];
            }
            sr.len = 0;
            sr.len_code_delta = 0;
            sr.distance = 0;
            sr.score = kMinScore;
            HashForgetfulChain<TPolicy>.FindLongestMatch(privat,
                @params->dictionary.contextual.dict(dict_id),
                ringbuffer, ringbuffer_mask, dist_cache, position, max_length,
                max_distance, dictionary_start + gap, @params->dist.max_distance, &sr);
            /* ENABLE_COMPOUND_DICTIONARY == 0: no LookupCompoundDictionaryMatch. */
            if (sr.score > kMinScore)
            {
                /* Found a match. Let's look for something even better ahead. */
                int delayed_backward_references_in_row = 0;
                --max_length;
                for (; ; --max_length)
                {
                    const nuint cost_diff_lazy = 175;
                    HasherSearchResult sr2;
                    sr2.len = @params->quality < MIN_QUALITY_FOR_EXTENSIVE_REFERENCE_SEARCH ?
                        BROTLI_MIN(sr.len - 1, max_length) : 0;
                    sr2.len_code_delta = 0;
                    sr2.distance = 0;
                    sr2.score = kMinScore;
                    max_distance = BROTLI_MIN(position + 1, max_backward_limit);
                    dictionary_start = BROTLI_MIN(
                        position + 1 + position_offset, max_backward_limit);
                    if (@params->dictionary.contextual.context_based != 0)
                    {
                        p2 = p1;
                        p1 = ringbuffer[position & ringbuffer_mask];
                        dict_id = @params->dictionary.contextual.context_map[
                            BROTLI_CONTEXT(p1, p2, literal_context_lut)];
                    }
                    HashForgetfulChain<TPolicy>.FindLongestMatch(privat,
                        @params->dictionary.contextual.dict(dict_id),
                        ringbuffer, ringbuffer_mask, dist_cache, position + 1, max_length,
                        max_distance, dictionary_start + gap, @params->dist.max_distance,
                        &sr2);
                    /* ENABLE_COMPOUND_DICTIONARY == 0: no LookupCompoundDictionaryMatch. */
                    if (sr2.score >= sr.score + cost_diff_lazy)
                    {
                        /* Ok, let's just write one byte for now and start a match from the
                           next byte. */
                        ++position;
                        ++insert_length;
                        sr = sr2;
                        if (++delayed_backward_references_in_row < 4 &&
                            position + HashForgetfulChain<TPolicy>.HashTypeLength() < pos_end)
                        {
                            continue;
                        }
                    }
                    break;
                }
                apply_random_heuristics =
                    position + 2 * sr.len + random_heuristics_window_size;
                dictionary_start = BROTLI_MIN(
                    position + position_offset, max_backward_limit);
                {
                    /* The first 16 codes are special short-codes,
                       and the minimum offset is 1. */
                    nuint distance_code = ComputeDistanceCode(
                        sr.distance, dictionary_start + gap, dist_cache);
                    if ((sr.distance <= (dictionary_start + gap)) && distance_code > 0)
                    {
                        dist_cache[3] = dist_cache[2];
                        dist_cache[2] = dist_cache[1];
                        dist_cache[1] = dist_cache[0];
                        dist_cache[0] = (int)sr.distance;
                        HashForgetfulChain<TPolicy>.PrepareDistanceCache(privat, dist_cache);
                    }
                    InitCommand(commands++, &@params->dist, insert_length,
                        sr.len, sr.len_code_delta, distance_code);
                }
                *num_literals += insert_length;
                insert_length = 0;
                /* Put the hash keys into the table, if there are enough bytes left.
                   Depending on the hasher implementation, it can push all positions
                   in the given range or only a subset of them.
                   Avoid hash poisoning with RLE data. */
                {
                    nuint range_start = position + 2;
                    nuint range_end = BROTLI_MIN(position + sr.len, store_end);
                    if (sr.distance < (sr.len >> 2))
                    {
                        range_start = BROTLI_MIN(range_end, BROTLI_MAX(
                            range_start, position + sr.len - (sr.distance << 2)));
                    }
                    HashForgetfulChain<TPolicy>.StoreRange(privat, ringbuffer,
                        ringbuffer_mask, range_start, range_end);
                }
                position += sr.len;
            }
            else
            {
                ++insert_length;
                ++position;
                /* If we have not seen matches for a long time, we can skip some
                   match lookups. Unsuccessful match lookups are very very expensive
                   and this kind of a heuristic speeds up compression quite
                   a lot. */
                if (position > apply_random_heuristics)
                {
                    /* Going through uncompressible data, jump. */
                    if (position >
                        apply_random_heuristics + 4 * random_heuristics_window_size)
                    {
                        /* It is quite a long time since we saw a copy, so we assume
                           that this data is not compressible, and store hashes less
                           often. Hashes of non compressible data are less likely to
                           turn out to be useful in the future, too, so we store less of
                           them to not to flood out the hash table of good compressible
                           data. */
                        nuint kMargin =
                            BROTLI_MAX(HashForgetfulChain<TPolicy>.StoreLookahead() - 1, 4);
                        nuint pos_jump =
                            BROTLI_MIN(position + 16, pos_end - kMargin);
                        for (; position < pos_jump; position += 4)
                        {
                            HashForgetfulChain<TPolicy>.Store(privat, ringbuffer,
                                ringbuffer_mask, position);
                            insert_length += 4;
                        }
                    }
                    else
                    {
                        nuint kMargin =
                            BROTLI_MAX(HashForgetfulChain<TPolicy>.StoreLookahead() - 1, 2);
                        nuint pos_jump =
                            BROTLI_MIN(position + 8, pos_end - kMargin);
                        for (; position < pos_jump; position += 2)
                        {
                            HashForgetfulChain<TPolicy>.Store(privat, ringbuffer,
                                ringbuffer_mask, position);
                            insert_length += 2;
                        }
                    }
                }
            }
        }
        insert_length += pos_end - position;
        *last_insert_len = insert_length;
        *num_commands += (nuint)(commands - orig_commands);
    }

    /// <summary><c>BrotliCreateBackwardReferences</c> (backward_references.c).</summary>
    internal static void BrotliCreateBackwardReferences(nuint num_bytes,
        nuint position, byte* ringbuffer, nuint ringbuffer_mask,
        byte* literal_context_lut, BrotliEncoderParams* @params,
        Hasher* hasher, int* dist_cache, nuint* last_insert_len,
        Command* commands, nuint* num_commands, nuint* num_literals)
    {
        if (@params->dictionary.compound.num_chunks != 0)
        {
            /* The D-prefix (ENABLE_COMPOUND_DICTIONARY == 1) template family is
               deferred with the compound-dictionary attach machinery; nothing in
               the ported surface can set num_chunks != 0. */
            throw new NotImplementedException(
                "DnBrotli DB-deferred: compound dictionary " +
                "(c/enc/backward_references.c CreateBackwardReferencesDH*)");
        }

        switch (@params->hasher.type)
        {
            case 2:
                CreateBackwardReferences<H2Policy>(num_bytes,
                    position, ringbuffer, ringbuffer_mask,
                    literal_context_lut, @params, hasher, dist_cache,
                    last_insert_len, commands, num_commands, num_literals);
                return;
            case 3:
                CreateBackwardReferences<H3Policy>(num_bytes,
                    position, ringbuffer, ringbuffer_mask,
                    literal_context_lut, @params, hasher, dist_cache,
                    last_insert_len, commands, num_commands, num_literals);
                return;
            case 4:
                CreateBackwardReferences<H4Policy>(num_bytes,
                    position, ringbuffer, ringbuffer_mask,
                    literal_context_lut, @params, hasher, dist_cache,
                    last_insert_len, commands, num_commands, num_literals);
                return;
            case 5:
                CreateBackwardReferencesH5(num_bytes,
                    position, ringbuffer, ringbuffer_mask,
                    literal_context_lut, @params, hasher, dist_cache,
                    last_insert_len, commands, num_commands, num_literals);
                return;
            case 6:
                CreateBackwardReferencesH6(num_bytes,
                    position, ringbuffer, ringbuffer_mask,
                    literal_context_lut, @params, hasher, dist_cache,
                    last_insert_len, commands, num_commands, num_literals);
                return;
            case 40:
                CreateBackwardReferencesFC<H40Policy>(num_bytes,
                    position, ringbuffer, ringbuffer_mask,
                    literal_context_lut, @params, hasher, dist_cache,
                    last_insert_len, commands, num_commands, num_literals);
                return;
            case 41:
                CreateBackwardReferencesFC<H41Policy>(num_bytes,
                    position, ringbuffer, ringbuffer_mask,
                    literal_context_lut, @params, hasher, dist_cache,
                    last_insert_len, commands, num_commands, num_literals);
                return;
            case 42:
                CreateBackwardReferencesFC<H42Policy>(num_bytes,
                    position, ringbuffer, ringbuffer_mask,
                    literal_context_lut, @params, hasher, dist_cache,
                    last_insert_len, commands, num_commands, num_literals);
                return;
            case 54:
                CreateBackwardReferences<H54Policy>(num_bytes,
                    position, ringbuffer, ringbuffer_mask,
                    literal_context_lut, @params, hasher, dist_cache,
                    last_insert_len, commands, num_commands, num_literals);
                return;
            case 35: case 55: case 65:
                throw new NotImplementedException(
                    $"DnBrotli DB-deferred: large-window hasher type {@params->hasher.type} " +
                    "(c/enc/backward_references.c CreateBackwardReferencesNH*)");
            default:
                /* BROTLI_DCHECK(false); */
                break;
        }
    }
}
