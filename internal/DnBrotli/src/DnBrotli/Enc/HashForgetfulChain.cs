// Port of c/enc/hash_forgetful_chain_inc.h (brotli v1.1.0).
//
// The C template is expanded three times from hash.h; here the template
// parameters BUCKET_BITS / NUM_BANKS / BANK_BITS / NUM_LAST_DISTANCES_TO_CHECK
// become a struct-implemented policy interface and the single generic body
// HashForgetfulChain<TPolicy> — the JIT specializes per policy struct and
// folds the constants, exactly like the C preprocessor. Instantiations
// (verified against the #define blocks in c/enc/hash.h lines 300-326):
//   H40: BUCKET_BITS 15, NUM_BANKS 1,   BANK_BITS 16, NUM_LAST_DISTANCES_TO_CHECK 4
//   H41: BUCKET_BITS 15, NUM_BANKS 1,   BANK_BITS 16, NUM_LAST_DISTANCES_TO_CHECK 10
//   H42: BUCKET_BITS 15, NUM_BANKS 512, BANK_BITS 9,  NUM_LAST_DISTANCES_TO_CHECK 16
// CAPPED_CHAINS is 0 for all instantiations (a plain #define in the template).
// The addr/head/tiny_hash arrays live in extra[0] and the banks in extra[1]
// (see HashMemAllocInBytes + the Addr/Head/TinyHash/Banks accessors),
// mirroring the v1.1.0 layout.

using System.Runtime.CompilerServices;

using static DnBrotli.Enc.Hash;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

/// <summary>Compile-time template parameters of
/// <c>hash_forgetful_chain_inc.h</c>.</summary>
internal interface IForgetfulChainPolicy
{
    int BUCKET_BITS { get; }
    int NUM_BANKS { get; }
    int BANK_BITS { get; }
    int NUM_LAST_DISTANCES_TO_CHECK { get; }
}

internal struct H40Policy : IForgetfulChainPolicy
{
    public int BUCKET_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 15; }
    public int NUM_BANKS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 1; }
    public int BANK_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 16; }
    public int NUM_LAST_DISTANCES_TO_CHECK { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 4; }
}

internal struct H41Policy : IForgetfulChainPolicy
{
    public int BUCKET_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 15; }
    public int NUM_BANKS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 1; }
    public int BANK_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 16; }
    public int NUM_LAST_DISTANCES_TO_CHECK { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 10; }
}

internal struct H42Policy : IForgetfulChainPolicy
{
    public int BUCKET_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 15; }
    public int NUM_BANKS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 512; }
    public int BANK_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 9; }
    public int NUM_LAST_DISTANCES_TO_CHECK { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 16; }
}

/// <summary><c>typedef struct FN(Slot) { uint16_t delta; uint16_t next; }</c>.
/// Sequential layout, sizeof == 4, matching the C slot exactly; the C
/// <c>FN(Bank)</c> (an array of BANK_SIZE slots) is addressed here as a flat
/// FCSlot* with index <c>bank * BANK_SIZE + slot</c>.</summary>
internal struct FCSlot
{
    public ushort delta;
    public ushort next;
}

/// <summary><c>typedef struct HashForgetfulChain</c>: a (forgetful) hash table
/// to the data seen by the compressor, to help create backward references to
/// previous data. Hashes are stored in chains which are bucketed to groups.
/// Group of chains share a storage "bank". When more than "bank size" chain
/// nodes are added, oldest nodes are replaced; this way several chains may
/// share a tail. All three policies (H40/H41/H42) share this one struct, so a
/// single layout backs every union member of <see cref="HasherPrivat"/>.</summary>
internal unsafe struct HashForgetfulChain
{
    /* C: uint16_t free_slot_idx[NUM_BANKS]. Sized for the largest
       instantiation (H42, NUM_BANKS == 512); H40/H41 only touch [0]. */
    public fixed ushort free_slot_idx[512];  /* Up to 1KiB. Move to dynamic? */
    public nuint max_hops;

    /* Shortcuts. */
    public void* extra0;  /* C: void* extra[2] (void* elements cannot form a fixed buffer) */
    public void* extra1;
    public HasherCommon* common;

    /* --- Dynamic size members --- */

    /* uint32_t addr[BUCKET_SIZE]; */

    /* uint16_t head[BUCKET_SIZE]; */

    /* Truncated hash used for quick rejection of "distance cache" candidates. */
    /* uint8_t tiny_hash[65536];*/

    /* FN(Bank) banks[NUM_BANKS]; */
}

internal static unsafe class HashForgetfulChain<TPolicy>
    where TPolicy : struct, IForgetfulChainPolicy
{
    private static int BUCKET_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => default(TPolicy).BUCKET_BITS; }
    private static int NUM_BANKS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => default(TPolicy).NUM_BANKS; }
    private static int BANK_BITS { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => default(TPolicy).BANK_BITS; }
    private static int NUM_LAST_DISTANCES_TO_CHECK
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default(TPolicy).NUM_LAST_DISTANCES_TO_CHECK;
    }
    private static int BANK_SIZE { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 1 << BANK_BITS; }
    /* Number of hash buckets. */
    private static int BUCKET_SIZE { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 1 << BUCKET_BITS; }
    /* #define CAPPED_CHAINS 0 — the CAPPED_CHAINS branches are folded out
       below (each site keeps the C original as a comment). */

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

    /* HashBytes is the function that chooses the bucket to place the address in.*/
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint HashBytes(byte* data)
    {
        uint h = BROTLI_UNALIGNED_LOAD32LE(data) * kHashMul32;
        /* The higher bits contain more mixture from the multiplication,
           so we take our results from there. */
        return h >> (32 - BUCKET_BITS);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint* Addr(void* extra)
    {
        return (uint*)extra;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort* Head(void* extra)
    {
        return (ushort*)(&Addr(extra)[BUCKET_SIZE]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte* TinyHash(void* extra)
    {
        return (byte*)(&Head(extra)[BUCKET_SIZE]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FCSlot* Banks(void* extra)
    {
        return (FCSlot*)extra;
    }

    internal static void Initialize(
        HasherCommon* common, HashForgetfulChain* self,
        BrotliEncoderParams* @params)
    {
        self->common = common;
        self->extra0 = common->extra0;
        self->extra1 = common->extra1;

        self->max_hops =
            (nuint)(@params->quality > 6 ? 7u : 8u) << (@params->quality - 4);
    }

    internal static void Prepare(
        HashForgetfulChain* self, int one_shot,
        nuint input_size, byte* data)
    {
        uint* addr = Addr(self->extra0);
        ushort* head = Head(self->extra0);
        byte* tiny_hash = TinyHash(self->extra0);
        /* Partial preparation is 100 times slower (per socket). */
        nuint partial_prepare_threshold = (nuint)BUCKET_SIZE >> 6;
        if (one_shot != 0 && input_size <= partial_prepare_threshold)
        {
            nuint i;
            for (i = 0; i < input_size; ++i)
            {
                nuint bucket = HashBytes(&data[i]);
                /* See InitEmpty comment. */
                addr[bucket] = 0xCCCCCCCC;
                head[bucket] = 0xCCCC;
            }
        }
        else
        {
            /* Fill |addr| array with 0xCCCCCCCC value. Because of wrapping, position
               processed by hasher never reaches 3GB + 64M; this makes all new chains
               to be terminated after the first node. */
            new Span<byte>(addr, sizeof(uint) * BUCKET_SIZE).Fill(0xCC);
            new Span<ushort>(head, BUCKET_SIZE).Clear();
        }
        new Span<byte>(tiny_hash, 65536).Clear();
        new Span<ushort>(self->free_slot_idx, NUM_BANKS).Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HashMemAllocInBytes(
        BrotliEncoderParams* @params, int one_shot,
        nuint input_size, nuint* alloc_size)
    {
        /* BROTLI_UNUSED(params/one_shot/input_size); */
        alloc_size[0] = sizeof(uint) * (nuint)BUCKET_SIZE +
                        sizeof(ushort) * (nuint)BUCKET_SIZE + sizeof(byte) * (nuint)65536;
        alloc_size[1] = (nuint)sizeof(FCSlot) * (nuint)BANK_SIZE * (nuint)NUM_BANKS;
    }

    /* Look at 4 bytes at &data[ix & mask]. Compute a hash from these, and prepend
       node to corresponding chain; also update tiny_hash for current position. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Store(HashForgetfulChain* self,
        byte* data, nuint mask, nuint ix)
    {
        uint* addr = Addr(self->extra0);
        ushort* head = Head(self->extra0);
        byte* tiny_hash = TinyHash(self->extra0);
        FCSlot* banks = Banks(self->extra1);
        nuint key = HashBytes(&data[ix & mask]);
        nuint bank = key & (nuint)(NUM_BANKS - 1);
        nuint idx = (nuint)(self->free_slot_idx[bank]++ & (BANK_SIZE - 1));
        nuint delta = ix - addr[key];
        tiny_hash[(ushort)ix] = (byte)key;
        if (delta > 0xFFFF) delta = 0xFFFF;  /* C: CAPPED_CHAINS ? 0 : 0xFFFF (CAPPED_CHAINS == 0) */
        banks[bank * (nuint)BANK_SIZE + idx].delta = (ushort)delta;
        banks[bank * (nuint)BANK_SIZE + idx].next = head[key];
        addr[key] = (uint)ix;
        head[key] = (ushort)idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StoreRange(
        HashForgetfulChain* self,
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
        HashForgetfulChain* self,
        nuint num_bytes, nuint position, byte* ringbuffer,
        nuint ring_buffer_mask)
    {
        if (num_bytes >= HashTypeLength() - 1 && position >= 3)
        {
            /* Prepare the hashes for three last bytes of the last write.
               These could not be calculated before, since they require knowledge
               of both the previous and the current block. */
            Store(self, ringbuffer, ring_buffer_mask, position - 3);
            Store(self, ringbuffer, ring_buffer_mask, position - 2);
            Store(self, ringbuffer, ring_buffer_mask, position - 1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void PrepareDistanceCache(
        HashForgetfulChain* self,
        int* distance_cache)
    {
        /* BROTLI_UNUSED(self); */
        Hash.PrepareDistanceCache(distance_cache, NUM_LAST_DISTANCES_TO_CHECK);
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
        HashForgetfulChain* self,
        BrotliEncoderDictionary* dictionary,
        byte* data, nuint ring_buffer_mask,
        int* distance_cache,
        nuint cur_ix, nuint max_length, nuint max_backward,
        nuint dictionary_distance, nuint max_distance,
        HasherSearchResult* @out)
    {
        uint* addr = Addr(self->extra0);
        ushort* head = Head(self->extra0);
        byte* tiny_hashes = TinyHash(self->extra0);
        FCSlot* banks = Banks(self->extra1);
        nuint cur_ix_masked = cur_ix & ring_buffer_mask;
        /* Don't accept a short copy from far away. */
        nuint min_score = @out->score;
        nuint best_score = @out->score;
        nuint best_len = @out->len;
        nuint i;
        nuint key = HashBytes(&data[cur_ix_masked]);
        byte tiny_hash = (byte)key;
        @out->len = 0;
        @out->len_code_delta = 0;
        /* Try last distance first. */
        for (i = 0; i < (nuint)NUM_LAST_DISTANCES_TO_CHECK; ++i)
        {
            nuint backward = (nuint)distance_cache[i];
            nuint prev_ix = cur_ix - backward;
            /* For distance code 0 we want to consider 2-byte matches. */
            if (i > 0 && tiny_hashes[(ushort)prev_ix] != tiny_hash) continue;
            if (prev_ix >= cur_ix || backward > max_backward)
            {
                continue;
            }
            prev_ix &= ring_buffer_mask;
            {
                nuint len = FindMatchLength.FindMatchLengthWithLimit(&data[prev_ix],
                                                                     &data[cur_ix_masked],
                                                                     max_length);
                if (len >= 2)
                {
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
            nuint bank = key & (nuint)(NUM_BANKS - 1);
            nuint backward = 0;
            nuint hops = self->max_hops;
            nuint delta = cur_ix - addr[key];
            nuint slot = head[key];
            /* C: while (hops--). The test happens before the decrement and hops
               is never read inside the body, so decrementing at loop entry is
               equivalent (the loop runs exactly max_hops times unless broken). */
            while (hops != 0)
            {
                hops--;
                nuint prev_ix;
                nuint last = slot;
                backward += delta;
                if (backward > max_backward) break;  /* C: || (CAPPED_CHAINS && !delta) (CAPPED_CHAINS == 0) */
                prev_ix = (cur_ix - backward) & ring_buffer_mask;
                slot = banks[bank * (nuint)BANK_SIZE + last].next;
                delta = banks[bank * (nuint)BANK_SIZE + last].delta;
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
            Store(self, data, ring_buffer_mask, cur_ix);
        }
        if (@out->score == min_score)
        {
            SearchInStaticDictionary(dictionary,
                self->common, &data[cur_ix_masked], max_length, dictionary_distance,
                max_distance, @out, false);
        }
    }
}
