// Port of c/enc/hash_longest_match_inc.h and c/enc/hash_longest_match64_inc.h
// (brotli v1.1.0).
//
// Each C template is expanded exactly once from hash.h (H5 at hash.h:292-294,
// H6 at hash.h:296-298); both expansions name their struct `HashLongestMatch`
// under `#define HashLongestMatch HASHER()`. C# cannot give the struct and the
// static function holder the same name, so the structs are named
// HashLongestMatchH5 / HashLongestMatchH6 and the function holders are the
// static classes H5 / H6 — mirroring how the C names both `HashLongestMatch`
// under the H5/H6 expansion. Unlike the quickly/forgetful-chain templates,
// these two have no compile-time parameters: bucket_bits / block_bits /
// num_last_distances_to_check come from the runtime hasher params.
// The num_ array lives in extra[0] and the buckets_ array in extra[1]
// (see HashMemAllocInBytes + Initialize), mirroring the v1.1.0 layout.

using System.Runtime.CompilerServices;

using static DnBrotli.Enc.Hash;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

/// <summary><c>typedef struct HashLongestMatch</c> (H5 expansion of
/// <c>hash_longest_match_inc.h</c>): a (forgetful) hash table to the data seen
/// by the compressor, to help create backward references to previous data.
/// This is a hash map of fixed size (bucket_size_) to a ring buffer of fixed
/// size (block_size_). The ring buffer contains the last block_size_ index
/// positions of the given hash key in the compressed data.</summary>
internal unsafe struct HashLongestMatchH5
{
    /* Number of hash buckets. */
    public nuint bucket_size_;
    /* Only block_size_ newest backward references are kept,
       and the older are forgotten. */
    public nuint block_size_;
    /* Left-shift for computing hash bucket index from hash value. */
    public int hash_shift_;
    /* Mask for accessing entries in a block (in a ring-buffer manner). */
    public uint block_mask_;

    public int block_bits_;
    public int num_last_distances_to_check_;

    /* Shortcuts. */
    public HasherCommon* common_;

    /* --- Dynamic size members --- */

    /* Number of entries in a particular bucket. */
    public ushort* num_;  /* uint16_t[bucket_size]; */

    /* Buckets containing block_size_ of backward references. */
    public uint* buckets_;  /* uint32_t[bucket_size * block_size]; */
}

internal static unsafe class H5
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint HashTypeLength()
    {
        return 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint StoreLookahead()
    {
        return 4;
    }

    /* HashBytes is the function that chooses the bucket to place the address in. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint HashBytes(byte* data, int shift)
    {
        uint h = BROTLI_UNALIGNED_LOAD32LE(data) * kHashMul32;
        /* The higher bits contain more mixture from the multiplication,
           so we take our results from there. */
        return h >> shift;
    }

    internal static void Initialize(
        HasherCommon* common, HashLongestMatchH5* self,
        BrotliEncoderParams* @params)
    {
        self->common_ = common;

        /* BROTLI_UNUSED(params); */
        self->hash_shift_ = 32 - common->@params.bucket_bits;
        self->bucket_size_ = (nuint)1 << common->@params.bucket_bits;
        self->block_size_ = (nuint)1 << common->@params.block_bits;
        self->block_mask_ = (uint)(self->block_size_ - 1);
        self->num_ = (ushort*)common->extra0;
        self->buckets_ = (uint*)common->extra1;
        self->block_bits_ = common->@params.block_bits;
        self->num_last_distances_to_check_ =
            common->@params.num_last_distances_to_check;
    }

    internal static void Prepare(
        HashLongestMatchH5* self, int one_shot,
        nuint input_size, byte* data)
    {
        ushort* num = self->num_;
        /* Partial preparation is 100 times slower (per socket). */
        nuint partial_prepare_threshold = self->bucket_size_ >> 6;
        if (one_shot != 0 && input_size <= partial_prepare_threshold)
        {
            nuint i;
            for (i = 0; i < input_size; ++i)
            {
                uint key = HashBytes(&data[i], self->hash_shift_);
                num[key] = 0;
            }
        }
        else
        {
            new Span<ushort>(num, (int)self->bucket_size_).Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HashMemAllocInBytes(
        BrotliEncoderParams* @params, int one_shot,
        nuint input_size, nuint* alloc_size)
    {
        nuint bucket_size = (nuint)1 << @params->hasher.bucket_bits;
        nuint block_size = (nuint)1 << @params->hasher.block_bits;
        /* BROTLI_UNUSED(one_shot/input_size); */
        alloc_size[0] = sizeof(ushort) * bucket_size;
        alloc_size[1] = sizeof(uint) * bucket_size * block_size;
    }

    /* Look at 4 bytes at &data[ix & mask].
       Compute a hash from these, and store the value of ix at that position. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Store(
        HashLongestMatchH5* self, byte* data,
        nuint mask, nuint ix)
    {
        uint key = HashBytes(&data[ix & mask], self->hash_shift_);
        nuint minor_ix = self->num_[key] & self->block_mask_;
        nuint offset = minor_ix + ((nuint)key << self->block_bits_);
        self->buckets_[offset] = (uint)ix;
        ++self->num_[key];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StoreRange(HashLongestMatchH5* self,
        byte* data, nuint mask,
        nuint ix_start, nuint ix_end)
    {
        nuint i;
        for (i = ix_start; i < ix_end; ++i)
        {
            Store(self, data, mask, i);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StitchToPreviousBlock(
        HashLongestMatchH5* self,
        nuint num_bytes, nuint position, byte* ringbuffer,
        nuint ringbuffer_mask)
    {
        if (num_bytes >= HashTypeLength() - 1 && position >= 3)
        {
            /* Prepare the hashes for three last bytes of the last write.
               These could not be calculated before, since they require knowledge
               of both the previous and the current block. */
            Store(self, ringbuffer, ringbuffer_mask, position - 3);
            Store(self, ringbuffer, ringbuffer_mask, position - 2);
            Store(self, ringbuffer, ringbuffer_mask, position - 1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void PrepareDistanceCache(
        HashLongestMatchH5* self,
        int* distance_cache)
    {
        Hash.PrepareDistanceCache(distance_cache,
                                  self->num_last_distances_to_check_);
    }

    /* Find a longest backward match of &data[cur_ix] up to the length of
       max_length and stores the position cur_ix in the hash table.

       REQUIRES: PrepareDistanceCache must be invoked for current distance cache
                 values; if this method is invoked repeatedly with the same distance
                 cache values, it is enough to invoke PrepareDistanceCache once.

       Does not look for matches longer than max_length.
       Does not look for matches further away than max_backward.
       Writes the best match into |out|.
       |out|->score is updated only if a better match is found. */
    internal static void FindLongestMatch(
        HashLongestMatchH5* self,
        BrotliEncoderDictionary* dictionary,
        byte* data, nuint ring_buffer_mask,
        int* distance_cache, nuint cur_ix,
        nuint max_length, nuint max_backward,
        nuint dictionary_distance, nuint max_distance,
        HasherSearchResult* @out)
    {
        ushort* num = self->num_;
        uint* buckets = self->buckets_;
        nuint cur_ix_masked = cur_ix & ring_buffer_mask;
        /* Don't accept a short copy from far away. */
        nuint min_score = @out->score;
        nuint best_score = @out->score;
        nuint best_len = @out->len;
        nuint i;
        @out->len = 0;
        @out->len_code_delta = 0;
        /* Try last distance first. */
        for (i = 0; i < (nuint)self->num_last_distances_to_check_; ++i)
        {
            nuint backward = (nuint)distance_cache[i];
            nuint prev_ix = cur_ix - backward;
            if (prev_ix >= cur_ix)
            {
                continue;
            }
            if (backward > max_backward)
            {
                continue;
            }
            prev_ix &= ring_buffer_mask;

            if (cur_ix_masked + best_len > ring_buffer_mask ||
                prev_ix + best_len > ring_buffer_mask ||
                data[cur_ix_masked + best_len] != data[prev_ix + best_len])
            {
                continue;
            }
            {
                nuint len = FindMatchLength.FindMatchLengthWithLimit(&data[prev_ix],
                                                                     &data[cur_ix_masked],
                                                                     max_length);
                if (len >= 3 || (len == 2 && i < 2))
                {
                    /* Comparing for >= 2 does not change the semantics, but just saves for
                       a few unnecessary binary logarithms in backward reference score,
                       since we are not interested in such short matches. */
                    nuint score = BackwardReferenceScoreUsingLastDistance(len);
                    if (best_score < score)
                    {
                        if (i != 0) score -= BackwardReferencePenaltyUsingLastDistance(i);
                        if (best_score < score)
                        {
                            best_score = score;
                            best_len = len;
                            @out->len = best_len;
                            @out->distance = backward;
                            @out->score = best_score;
                        }
                    }
                }
            }
        }
        {
            uint key = HashBytes(&data[cur_ix_masked], self->hash_shift_);
            uint* bucket = &buckets[(nuint)key << self->block_bits_];
            nuint down =
                (num[key] > self->block_size_) ? (num[key] - self->block_size_) : 0u;
            for (i = num[key]; i > down;)
            {
                nuint prev_ix = bucket[--i & self->block_mask_];
                nuint backward = cur_ix - prev_ix;
                if (backward > max_backward)
                {
                    break;
                }
                prev_ix &= ring_buffer_mask;
                if (cur_ix_masked + best_len > ring_buffer_mask ||
                    prev_ix + best_len > ring_buffer_mask ||
                    data[cur_ix_masked + best_len] != data[prev_ix + best_len])
                {
                    continue;
                }
                {
                    nuint len = FindMatchLength.FindMatchLengthWithLimit(&data[prev_ix],
                                                                         &data[cur_ix_masked],
                                                                         max_length);
                    if (len >= 4)
                    {
                        /* Comparing for >= 3 does not change the semantics, but just saves
                           for a few unnecessary binary logarithms in backward reference
                           score, since we are not interested in such short matches. */
                        nuint score = BackwardReferenceScore(len, backward);
                        if (best_score < score)
                        {
                            best_score = score;
                            best_len = len;
                            @out->len = best_len;
                            @out->distance = backward;
                            @out->score = best_score;
                        }
                    }
                }
            }
            bucket[num[key] & self->block_mask_] = (uint)cur_ix;
            ++num[key];
        }
        if (min_score == @out->score)
        {
            SearchInStaticDictionary(dictionary,
                self->common_, &data[cur_ix_masked], max_length, dictionary_distance,
                max_distance, @out, false);
        }
    }
}

/// <summary><c>typedef struct HashLongestMatch</c> (H6 expansion of
/// <c>hash_longest_match64_inc.h</c>); same shape as H5 but with a 64-bit hash
/// multiplier instead of a shift.</summary>
internal unsafe struct HashLongestMatchH6
{
    /* Number of hash buckets. */
    public nuint bucket_size_;
    /* Only block_size_ newest backward references are kept,
       and the older are forgotten. */
    public nuint block_size_;
    /* Hash multiplier tuned to match length. */
    public ulong hash_mul_;
    /* Mask for accessing entries in a block (in a ring-buffer manner). */
    public uint block_mask_;

    public int block_bits_;
    public int num_last_distances_to_check_;

    /* Shortcuts. */
    public HasherCommon* common_;

    /* --- Dynamic size members --- */

    /* Number of entries in a particular bucket. */
    public ushort* num_;  /* uint16_t[bucket_size]; */

    /* Buckets containing block_size_ of backward references. */
    public uint* buckets_;  /* uint32_t[bucket_size * block_size]; */
}

internal static unsafe class H6
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint HashTypeLength()
    {
        return 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint StoreLookahead()
    {
        return 8;
    }

    /* HashBytes is the function that chooses the bucket to place the address in. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint HashBytes(byte* data, ulong hash_mul)
    {
        ulong h = BROTLI_UNALIGNED_LOAD64LE(data) * hash_mul;
        /* The higher bits contain more mixture from the multiplication,
           so we take our results from there. */
        return (nuint)(h >> (64 - 15));
    }

    internal static void Initialize(
        HasherCommon* common, HashLongestMatchH6* self,
        BrotliEncoderParams* @params)
    {
        self->common_ = common;

        /* BROTLI_UNUSED(params); */
        self->hash_mul_ = kHashMul64 << (64 - 5 * 8);
        /* BROTLI_DCHECK(common->params.bucket_bits == 15); */
        self->bucket_size_ = (nuint)1 << common->@params.bucket_bits;
        self->block_bits_ = common->@params.block_bits;
        self->block_size_ = (nuint)1 << common->@params.block_bits;
        self->block_mask_ = (uint)(self->block_size_ - 1);
        self->num_last_distances_to_check_ =
            common->@params.num_last_distances_to_check;
        self->num_ = (ushort*)common->extra0;
        self->buckets_ = (uint*)common->extra1;
    }

    internal static void Prepare(
        HashLongestMatchH6* self, int one_shot,
        nuint input_size, byte* data)
    {
        ushort* num = self->num_;
        /* Partial preparation is 100 times slower (per socket). */
        nuint partial_prepare_threshold = self->bucket_size_ >> 6;
        if (one_shot != 0 && input_size <= partial_prepare_threshold)
        {
            nuint i;
            for (i = 0; i < input_size; ++i)
            {
                nuint key = HashBytes(&data[i], self->hash_mul_);
                num[key] = 0;
            }
        }
        else
        {
            new Span<ushort>(num, (int)self->bucket_size_).Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HashMemAllocInBytes(
        BrotliEncoderParams* @params, int one_shot,
        nuint input_size, nuint* alloc_size)
    {
        nuint bucket_size = (nuint)1 << @params->hasher.bucket_bits;
        nuint block_size = (nuint)1 << @params->hasher.block_bits;
        /* BROTLI_UNUSED(one_shot/input_size); */
        alloc_size[0] = sizeof(ushort) * bucket_size;
        alloc_size[1] = sizeof(uint) * bucket_size * block_size;
    }

    /* Look at 4 bytes at &data[ix & mask].
       Compute a hash from these, and store the value of ix at that position. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Store(
        HashLongestMatchH6* self, byte* data,
        nuint mask, nuint ix)
    {
        ushort* num = self->num_;
        uint* buckets = self->buckets_;
        nuint key = HashBytes(&data[ix & mask], self->hash_mul_);
        nuint minor_ix = num[key] & self->block_mask_;
        nuint offset = minor_ix + (key << self->block_bits_);
        ++num[key];
        buckets[offset] = (uint)ix;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StoreRange(HashLongestMatchH6* self,
        byte* data, nuint mask,
        nuint ix_start, nuint ix_end)
    {
        nuint i;
        for (i = ix_start; i < ix_end; ++i)
        {
            Store(self, data, mask, i);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StitchToPreviousBlock(
        HashLongestMatchH6* self,
        nuint num_bytes, nuint position, byte* ringbuffer,
        nuint ringbuffer_mask)
    {
        if (num_bytes >= HashTypeLength() - 1 && position >= 3)
        {
            /* Prepare the hashes for three last bytes of the last write.
               These could not be calculated before, since they require knowledge
               of both the previous and the current block. */
            Store(self, ringbuffer, ringbuffer_mask, position - 3);
            Store(self, ringbuffer, ringbuffer_mask, position - 2);
            Store(self, ringbuffer, ringbuffer_mask, position - 1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void PrepareDistanceCache(
        HashLongestMatchH6* self,
        int* distance_cache)
    {
        Hash.PrepareDistanceCache(distance_cache,
                                  self->num_last_distances_to_check_);
    }

    /* Find a longest backward match of &data[cur_ix] up to the length of
       max_length and stores the position cur_ix in the hash table.

       REQUIRES: PrepareDistanceCache must be invoked for current distance cache
                 values; if this method is invoked repeatedly with the same distance
                 cache values, it is enough to invoke PrepareDistanceCache once.

       Does not look for matches longer than max_length.
       Does not look for matches further away than max_backward.
       Writes the best match into |out|.
       |out|->score is updated only if a better match is found. */
    internal static void FindLongestMatch(
        HashLongestMatchH6* self,
        BrotliEncoderDictionary* dictionary,
        byte* data, nuint ring_buffer_mask,
        int* distance_cache, nuint cur_ix,
        nuint max_length, nuint max_backward,
        nuint dictionary_distance, nuint max_distance,
        HasherSearchResult* @out)
    {
        ushort* num = self->num_;
        uint* buckets = self->buckets_;
        nuint cur_ix_masked = cur_ix & ring_buffer_mask;
        /* Don't accept a short copy from far away. */
        nuint min_score = @out->score;
        nuint best_score = @out->score;
        nuint best_len = @out->len;
        nuint i;
        @out->len = 0;
        @out->len_code_delta = 0;
        /* Try last distance first. */
        for (i = 0; i < (nuint)self->num_last_distances_to_check_; ++i)
        {
            nuint backward = (nuint)distance_cache[i];
            nuint prev_ix = cur_ix - backward;
            if (prev_ix >= cur_ix)
            {
                continue;
            }
            if (backward > max_backward)
            {
                continue;
            }
            prev_ix &= ring_buffer_mask;

            if (cur_ix_masked + best_len > ring_buffer_mask ||
                prev_ix + best_len > ring_buffer_mask ||
                data[cur_ix_masked + best_len] != data[prev_ix + best_len])
            {
                continue;
            }
            {
                nuint len = FindMatchLength.FindMatchLengthWithLimit(&data[prev_ix],
                                                                     &data[cur_ix_masked],
                                                                     max_length);
                if (len >= 3 || (len == 2 && i < 2))
                {
                    /* Comparing for >= 2 does not change the semantics, but just saves for
                       a few unnecessary binary logarithms in backward reference score,
                       since we are not interested in such short matches. */
                    nuint score = BackwardReferenceScoreUsingLastDistance(len);
                    if (best_score < score)
                    {
                        if (i != 0) score -= BackwardReferencePenaltyUsingLastDistance(i);
                        if (best_score < score)
                        {
                            best_score = score;
                            best_len = len;
                            @out->len = best_len;
                            @out->distance = backward;
                            @out->score = best_score;
                        }
                    }
                }
            }
        }
        {
            nuint key = HashBytes(&data[cur_ix_masked], self->hash_mul_);
            uint* bucket = &buckets[key << self->block_bits_];
            nuint down =
                (num[key] > self->block_size_) ?
                (num[key] - self->block_size_) : 0u;
            uint first4 = BROTLI_UNALIGNED_LOAD32LE(data + cur_ix_masked);
            nuint max_length_m4 = max_length - 4;
            i = num[key];
            for (; i > down;)
            {
                nuint prev_ix = bucket[--i & self->block_mask_];
                uint current4;
                nuint backward = cur_ix - prev_ix;
                if (backward > max_backward)
                {
                    break;
                }
                prev_ix &= ring_buffer_mask;
                if (cur_ix_masked + best_len > ring_buffer_mask ||
                    prev_ix + best_len > ring_buffer_mask ||
                    data[cur_ix_masked + best_len] != data[prev_ix + best_len])
                {
                    continue;
                }
                current4 = BROTLI_UNALIGNED_LOAD32LE(data + prev_ix);
                if (first4 != current4) continue;
                {
                    nuint len = FindMatchLength.FindMatchLengthWithLimit(&data[prev_ix + 4],
                                                                         &data[cur_ix_masked + 4],
                                                                         max_length_m4) + 4;
                    nuint score = BackwardReferenceScore(len, backward);
                    if (best_score < score)
                    {
                        best_score = score;
                        best_len = len;
                        @out->len = best_len;
                        @out->distance = backward;
                        @out->score = best_score;
                    }
                }
            }
            bucket[num[key] & self->block_mask_] = (uint)cur_ix;
            ++num[key];
        }
        if (min_score == @out->score)
        {
            SearchInStaticDictionary(dictionary,
                self->common_, &data[cur_ix_masked], max_length, dictionary_distance,
                max_distance, @out, false);
        }
    }
}
