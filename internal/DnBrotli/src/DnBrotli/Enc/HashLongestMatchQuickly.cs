// Port of c/enc/hash_longest_match_quickly_inc.h (brotli v1.1.0).
//
// The C template is expanded four times from hash.h (H2/H3/H4/H54); here the
// template parameters BUCKET_BITS / BUCKET_SWEEP_BITS / HASH_LEN /
// USE_DICTIONARY become a struct-implemented policy interface and the single
// generic body HashLongestMatchQuickly<TPolicy> — the JIT specializes per
// policy struct and folds the constants, exactly like the C preprocessor.
// Instantiations (verified against the #define blocks in c/enc/hash.h):
//   H2:  BUCKET_BITS 16, BUCKET_SWEEP_BITS 0, HASH_LEN 5, USE_DICTIONARY 1
//   H3:  BUCKET_BITS 16, BUCKET_SWEEP_BITS 1, HASH_LEN 5, USE_DICTIONARY 0
//   H4:  BUCKET_BITS 17, BUCKET_SWEEP_BITS 2, HASH_LEN 5, USE_DICTIONARY 1
//   H54: BUCKET_BITS 20, BUCKET_SWEEP_BITS 2, HASH_LEN 7, USE_DICTIONARY 0
// The bucket array lives in the hasher's extra[0] allocation (see
// HashMemAllocInBytes + Initialize), mirroring the v1.1.0 layout.

using System.Runtime.CompilerServices;

using static DnBrotli.Enc.Hash;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

/// <summary>Compile-time template parameters of
/// <c>hash_longest_match_quickly_inc.h</c>.</summary>
internal interface IQuicklyPolicy
{
    int BUCKET_BITS { get; }
    int BUCKET_SWEEP_BITS { get; }
    int HASH_LEN { get; }
    bool USE_DICTIONARY { get; }
}

internal struct H2Policy : IQuicklyPolicy
{
    public int BUCKET_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 16; }
    public int BUCKET_SWEEP_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 0; }
    public int HASH_LEN { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 5; }
    public bool USE_DICTIONARY { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => true; }
}

internal struct H3Policy : IQuicklyPolicy
{
    public int BUCKET_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 16; }
    public int BUCKET_SWEEP_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 1; }
    public int HASH_LEN { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 5; }
    public bool USE_DICTIONARY { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => false; }
}

internal struct H4Policy : IQuicklyPolicy
{
    public int BUCKET_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 17; }
    public int BUCKET_SWEEP_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 2; }
    public int HASH_LEN { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 5; }
    public bool USE_DICTIONARY { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => true; }
}

internal struct H54Policy : IQuicklyPolicy
{
    public int BUCKET_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 20; }
    public int BUCKET_SWEEP_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 2; }
    public int HASH_LEN { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 7; }
    public bool USE_DICTIONARY { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => false; }
}

/// <summary><c>typedef struct HashLongestMatchQuickly</c>: a (forgetful) hash
/// table to the data seen by the compressor, to help create backward
/// references to previous data. This is a hash map of fixed size
/// (BUCKET_SIZE). The layout is identical for all four policies, so a single
/// struct backs every union member of <see cref="HasherPrivat"/>.</summary>
internal unsafe struct HashLongestMatchQuickly
{
    /* Shortcuts. */
    public HasherCommon* common;

    /* --- Dynamic size members --- */

    public uint* buckets_;  /* uint32_t[BUCKET_SIZE]; */
}

/// <summary>Fixed-size scratch buffer for the <c>keys[BUCKET_SWEEP]</c> local
/// in <see cref="HashLongestMatchQuickly{TPolicy}.FindLongestMatch"/> (max
/// sweep is 4). A local of this type is a stack-declared "fixed variable"
/// (C# spec 7.6.9), so its <c>fixed ulong</c> buffer converts to a pointer
/// without a <c>fixed</c> statement and, unlike <c>stackalloc</c>, is not
/// zero-initialized per call.</summary>
internal unsafe struct SweepKeys
{
    public fixed ulong k[4];
}

internal static unsafe class HashLongestMatchQuickly<TPolicy>
    where TPolicy : struct, IQuicklyPolicy
{
    private static int BUCKET_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => default(TPolicy).BUCKET_BITS; }
    private static int BUCKET_SIZE { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 1 << BUCKET_BITS; }
    private static int BUCKET_MASK { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => BUCKET_SIZE - 1; }
    private static int BUCKET_SWEEP { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 1 << default(TPolicy).BUCKET_SWEEP_BITS; }
    private static int BUCKET_SWEEP_MASK { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (BUCKET_SWEEP - 1) << 3; }
    private static int HASH_LEN { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => default(TPolicy).HASH_LEN; }
    private static bool USE_DICTIONARY { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => default(TPolicy).USE_DICTIONARY; }

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

    /* HashBytes is the function that chooses the bucket to place
       the address in. The HashLongestMatch and HashLongestMatchQuickly
       classes have separate, different implementations of hashing. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint HashBytes(byte* data)
    {
        ulong h = (BROTLI_UNALIGNED_LOAD64LE(data) << (64 - 8 * HASH_LEN)) *
                  kHashMul64;
        /* The higher bits contain more mixture from the multiplication,
           so we take our results from there. */
        return (uint)(h >> (64 - BUCKET_BITS));
    }

    internal static void Initialize(
        HasherCommon* common, HashLongestMatchQuickly* self,
        BrotliEncoderParams* @params)
    {
        self->common = common;

        /* BROTLI_UNUSED(params); */
        self->buckets_ = (uint*)common->extra0;
    }

    internal static void Prepare(
        HashLongestMatchQuickly* self, int one_shot,
        nuint input_size, byte* data)
    {
        uint* buckets = self->buckets_;
        /* Partial preparation is 100 times slower (per socket). */
        nuint partial_prepare_threshold = (nuint)BUCKET_SIZE >> 5;
        if (one_shot != 0 && input_size <= partial_prepare_threshold)
        {
            nuint i;
            for (i = 0; i < input_size; ++i)
            {
                uint key = HashBytes(&data[i]);
                if (BUCKET_SWEEP == 1)
                {
                    buckets[key] = 0;
                }
                else
                {
                    uint j;
                    for (j = 0; j < BUCKET_SWEEP; ++j)
                    {
                        buckets[(key + (j << 3)) & BUCKET_MASK] = 0;
                    }
                }
            }
        }
        else
        {
            /* It is not strictly necessary to fill this buffer here, but
               not filling will make the results of the compression stochastic
               (but correct). This is because random data would cause the
               system to find accidentally good backward references here and there. */
            new Span<uint>(buckets, BUCKET_SIZE).Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HashMemAllocInBytes(
        BrotliEncoderParams* @params, int one_shot,
        nuint input_size, nuint* alloc_size)
    {
        /* BROTLI_UNUSED(params/one_shot/input_size); */
        alloc_size[0] = sizeof(uint) * (nuint)BUCKET_SIZE;
    }

    /* Look at 5 bytes at &data[ix & mask].
       Compute a hash from these, and store the value somewhere within
       [ix .. ix+3]. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Store(
        HashLongestMatchQuickly* self,
        byte* data, nuint mask, nuint ix)
    {
        uint key = HashBytes(&data[ix & mask]);
        if (BUCKET_SWEEP == 1)
        {
            self->buckets_[key] = (uint)ix;
        }
        else
        {
            /* Wiggle the value with the bucket sweep range. */
            uint off = (uint)ix & (uint)BUCKET_SWEEP_MASK;
            self->buckets_[(key + off) & BUCKET_MASK] = (uint)ix;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StoreRange(
        HashLongestMatchQuickly* self,
        byte* data, nuint mask,
        nuint ix_start, nuint ix_end)
    {
        nuint i;
        for (i = ix_start; i < ix_end; ++i)
        {
            Store(self, data, mask, i);
        }
    }

    internal static void StitchToPreviousBlock(
        HashLongestMatchQuickly* self,
        nuint num_bytes, nuint position,
        byte* ringbuffer, nuint ringbuffer_mask)
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
        HashLongestMatchQuickly* self,
        int* distance_cache)
    {
        /* BROTLI_UNUSED(self); BROTLI_UNUSED(distance_cache); */
    }

    /* Find a longest backward match of &data[cur_ix & ring_buffer_mask]
       up to the length of max_length and stores the position cur_ix in the
       hash table.

       Does not look for matches longer than max_length.
       Does not look for matches further away than max_backward.
       Writes the best match into |out|.
       |out|->score is updated only if a better match is found. */
    internal static void FindLongestMatch(
        HashLongestMatchQuickly* self,
        BrotliEncoderDictionary* dictionary,
        byte* data,
        nuint ring_buffer_mask, int* distance_cache,
        nuint cur_ix, nuint max_length, nuint max_backward,
        nuint dictionary_distance, nuint max_distance,
        HasherSearchResult* @out)
    {
        uint* buckets = self->buckets_;
        nuint best_len_in = @out->len;
        nuint cur_ix_masked = cur_ix & ring_buffer_mask;
        int compare_char = data[cur_ix_masked + best_len_in];
        nuint key = HashBytes(&data[cur_ix_masked]);
        nuint key_out = 0;
        nuint min_score = @out->score;
        nuint best_score = @out->score;
        nuint best_len = best_len_in;
        nuint cached_backward = (nuint)distance_cache[0];
        nuint prev_ix = cur_ix - cached_backward;
        @out->len_code_delta = 0;
        if (prev_ix < cur_ix)
        {
            prev_ix &= (uint)ring_buffer_mask;
            if (compare_char == data[prev_ix + best_len])
            {
                nuint len = FindMatchLength.FindMatchLengthWithLimit(
                    &data[prev_ix], &data[cur_ix_masked], max_length);
                if (len >= 4)
                {
                    nuint score = BackwardReferenceScoreUsingLastDistance(len);
                    if (best_score < score)
                    {
                        @out->len = len;
                        @out->distance = cached_backward;
                        @out->score = score;
                        if (BUCKET_SWEEP == 1)
                        {
                            buckets[key] = (uint)cur_ix;
                            return;
                        }
                        else
                        {
                            best_len = len;
                            best_score = score;
                            compare_char = data[cur_ix_masked + len];
                        }
                    }
                }
            }
        }
        if (BUCKET_SWEEP == 1)
        {
            nuint backward;
            nuint len;
            /* Only one to look for, don't bother to prepare for a loop. */
            prev_ix = buckets[key];
            buckets[key] = (uint)cur_ix;
            backward = cur_ix - prev_ix;
            prev_ix &= (uint)ring_buffer_mask;
            if (compare_char != data[prev_ix + best_len_in])
            {
                return;
            }
            if (backward == 0 || backward > max_backward)
            {
                return;
            }
            len = FindMatchLength.FindMatchLengthWithLimit(&data[prev_ix],
                                                           &data[cur_ix_masked],
                                                           max_length);
            if (len >= 4)
            {
                nuint score = BackwardReferenceScore(len, backward);
                if (best_score < score)
                {
                    @out->len = len;
                    @out->distance = backward;
                    @out->score = score;
                    return;
                }
            }
        }
        else
        {
            /* size_t keys[BUCKET_SWEEP] (max sweep is 4); stack-local fixed
               buffer instead of stackalloc so this per-call hot path avoids
               an alloca + memset (dn2cpp) / localsinit zeroing (JIT) — every
               slot is written before it is read below, so leaving it
               uninitialized cannot change output. */
            SweepKeys keysBuf;
            nuint* keys = (nuint*)keysBuf.k;
            nuint i;
            for (i = 0; i < (nuint)BUCKET_SWEEP; ++i)
            {
                keys[i] = (key + (i << 3)) & (nuint)BUCKET_MASK;
            }
            key_out = keys[(cur_ix & (nuint)BUCKET_SWEEP_MASK) >> 3];
            for (i = 0; i < (nuint)BUCKET_SWEEP; ++i)
            {
                nuint len;
                nuint backward;
                prev_ix = buckets[keys[i]];
                backward = cur_ix - prev_ix;
                prev_ix &= (uint)ring_buffer_mask;
                if (compare_char != data[prev_ix + best_len])
                {
                    continue;
                }
                if (backward == 0 || backward > max_backward)
                {
                    continue;
                }
                len = FindMatchLength.FindMatchLengthWithLimit(&data[prev_ix],
                                                               &data[cur_ix_masked],
                                                               max_length);
                if (len >= 4)
                {
                    nuint score = BackwardReferenceScore(len, backward);
                    if (best_score < score)
                    {
                        best_len = len;
                        @out->len = len;
                        compare_char = data[cur_ix_masked + len];
                        best_score = score;
                        @out->score = score;
                        @out->distance = backward;
                    }
                }
            }
        }
        if (USE_DICTIONARY && min_score == @out->score)
        {
            SearchInStaticDictionary(dictionary,
                self->common, &data[cur_ix_masked], max_length, dictionary_distance,
                max_distance, @out, true);
        }
        if (BUCKET_SWEEP != 1)
        {
            buckets[key_out] = (uint)cur_ix;
        }
    }
}
