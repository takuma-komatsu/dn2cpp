// Port of c/enc/hash_to_binary_tree_inc.h (brotli v1.1.0).
//
// The C template is expanded exactly once from hash.h (H10 at hash.h:246-250,
// with BUCKET_BITS 17, MAX_TREE_SEARCH_DEPTH 64, MAX_TREE_COMP_LENGTH 128)
// and names its struct `HashToBinaryTree` under
// `#define HashToBinaryTree HASHER()`. Like H5/H6 (HashLongestMatch.cs), the
// struct keeps the C name HashToBinaryTree and the function holder is the
// static class H10. The buckets_ array lives in extra[0] and the forest_
// array in extra[1] (see HashMemAllocInBytes + Initialize), mirroring the
// v1.1.0 layout.

using System.Runtime.CompilerServices;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.Hash;
using static DnBrotli.Enc.Quality;
using static DnBrotli.Enc.StaticDict;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

/// <summary><c>typedef struct HashToBinaryTree</c> (H10 expansion of
/// <c>hash_to_binary_tree_inc.h</c>): a (forgetful) hash table where each hash
/// bucket contains a binary tree of sequences whose first 4 bytes share the
/// same hash code. Each sequence is MAX_TREE_COMP_LENGTH long and is
/// identified by its starting position in the input data. The binary tree is
/// sorted by the lexicographic order of the sequences, and it is also a
/// max-heap with respect to the starting positions.</summary>
internal unsafe struct HashToBinaryTree
{
    /* The window size minus 1 */
    public nuint window_mask_;

    /* Hash table that maps the 4-byte hashes of the sequence to the last
       position where this hash was found, which is the root of the binary
       tree of sequences that share this hash bucket. */
    public uint* buckets_;  /* uint32_t[BUCKET_SIZE]; */

    /* A position used to mark a non-existent sequence, i.e. a tree is empty if
       its root is at invalid_pos_ and a node is a leaf if both its children
       are at invalid_pos_. */
    public uint invalid_pos_;

    /* --- Dynamic size members --- */

    /* The union of the binary trees of each hash bucket. The root of the tree
       corresponding to a hash is a sequence starting at buckets_[hash] and
       the left and right children of a sequence starting at pos are
       forest_[2 * pos] and forest_[2 * pos + 1]. */
    public uint* forest_;  /* uint32_t[2 * num_nodes] */
}

internal static unsafe class H10
{
    /* Template parameters (hash.h:247-249). */
    internal const int BUCKET_BITS = 17;
    internal const int MAX_TREE_SEARCH_DEPTH = 64;
    internal const int MAX_TREE_COMP_LENGTH = 128;

    private const int BUCKET_SIZE = 1 << BUCKET_BITS;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint HashTypeLength()
    {
        return 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint StoreLookahead()
    {
        return MAX_TREE_COMP_LENGTH;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint HashBytes(byte* data)
    {
        uint h = BROTLI_UNALIGNED_LOAD32LE(data) * kHashMul32;
        /* The higher bits contain more mixture from the multiplication,
           so we take our results from there. */
        return h >> (32 - BUCKET_BITS);
    }

    internal static void Initialize(
        HasherCommon* common, HashToBinaryTree* self,
        BrotliEncoderParams* @params)
    {
        self->buckets_ = (uint*)common->extra0;
        self->forest_ = (uint*)common->extra1;

        self->window_mask_ = (1u << @params->lgwin) - 1u;
        self->invalid_pos_ = (uint)(0 - self->window_mask_);
    }

    internal static void Prepare(
        HashToBinaryTree* self, int one_shot,
        nuint input_size, byte* data)
    {
        uint invalid_pos = self->invalid_pos_;
        uint i;
        uint* buckets = self->buckets_;
        /* BROTLI_UNUSED(data/one_shot/input_size); */
        for (i = 0; i < BUCKET_SIZE; i++)
        {
            buckets[i] = invalid_pos;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void HashMemAllocInBytes(
        BrotliEncoderParams* @params, int one_shot,
        nuint input_size, nuint* alloc_size)
    {
        nuint num_nodes = (nuint)1 << @params->lgwin;
        if (one_shot != 0 && input_size < num_nodes)
        {
            num_nodes = input_size;
        }
        alloc_size[0] = sizeof(uint) * (nuint)BUCKET_SIZE;
        alloc_size[1] = 2 * sizeof(uint) * num_nodes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint LeftChildIndex(
        HashToBinaryTree* self,
        nuint pos)
    {
        return 2 * (pos & self->window_mask_);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint RightChildIndex(
        HashToBinaryTree* self,
        nuint pos)
    {
        return 2 * (pos & self->window_mask_) + 1;
    }

    /* Stores the hash of the next 4 bytes and in a single tree-traversal, the
       hash bucket's binary tree is searched for matches and is re-rooted at the
       current position.

       If less than MAX_TREE_COMP_LENGTH data is available, the hash bucket of the
       current position is searched for matches, but the state of the hash table
       is not changed, since we can not know the final sorting order of the
       current (incomplete) sequence.

       This function must be called with increasing cur_ix positions. */
    private static BackwardMatch* StoreAndFindMatches(
        HashToBinaryTree* self, byte* data,
        nuint cur_ix, nuint ring_buffer_mask, nuint max_length,
        nuint max_backward, nuint* best_len,
        BackwardMatch* matches)
    {
        nuint cur_ix_masked = cur_ix & ring_buffer_mask;
        nuint max_comp_len =
            BROTLI_MIN(max_length, MAX_TREE_COMP_LENGTH);
        bool should_reroot_tree = max_length >= MAX_TREE_COMP_LENGTH;
        uint key = HashBytes(&data[cur_ix_masked]);
        uint* buckets = self->buckets_;
        uint* forest = self->forest_;
        nuint prev_ix = buckets[key];
        /* The forest index of the rightmost node of the left subtree of the new
           root, updated as we traverse and re-root the tree of the hash bucket. */
        nuint node_left = LeftChildIndex(self, cur_ix);
        /* The forest index of the leftmost node of the right subtree of the new
           root, updated as we traverse and re-root the tree of the hash bucket. */
        nuint node_right = RightChildIndex(self, cur_ix);
        /* The match length of the rightmost node of the left subtree of the new
           root, updated as we traverse and re-root the tree of the hash bucket. */
        nuint best_len_left = 0;
        /* The match length of the leftmost node of the right subtree of the new
           root, updated as we traverse and re-root the tree of the hash bucket. */
        nuint best_len_right = 0;
        nuint depth_remaining;
        if (should_reroot_tree)
        {
            buckets[key] = (uint)cur_ix;
        }
        for (depth_remaining = MAX_TREE_SEARCH_DEPTH; ; --depth_remaining)
        {
            nuint backward = cur_ix - prev_ix;
            nuint prev_ix_masked = prev_ix & ring_buffer_mask;
            if (backward == 0 || backward > max_backward || depth_remaining == 0)
            {
                if (should_reroot_tree)
                {
                    forest[node_left] = self->invalid_pos_;
                    forest[node_right] = self->invalid_pos_;
                }
                break;
            }
            {
                nuint cur_len = BROTLI_MIN(best_len_left, best_len_right);
                nuint len;
                len = cur_len +
                    FindMatchLength.FindMatchLengthWithLimit(&data[cur_ix_masked + cur_len],
                                                             &data[prev_ix_masked + cur_len],
                                                             max_length - cur_len);
                if (matches != null && len > *best_len)
                {
                    *best_len = len;
                    InitBackwardMatch(matches++, backward, len);
                }
                if (len >= max_comp_len)
                {
                    if (should_reroot_tree)
                    {
                        forest[node_left] = forest[LeftChildIndex(self, prev_ix)];
                        forest[node_right] = forest[RightChildIndex(self, prev_ix)];
                    }
                    break;
                }
                if (data[cur_ix_masked + len] > data[prev_ix_masked + len])
                {
                    best_len_left = len;
                    if (should_reroot_tree)
                    {
                        forest[node_left] = (uint)prev_ix;
                    }
                    node_left = RightChildIndex(self, prev_ix);
                    prev_ix = forest[node_left];
                }
                else
                {
                    best_len_right = len;
                    if (should_reroot_tree)
                    {
                        forest[node_right] = (uint)prev_ix;
                    }
                    node_right = LeftChildIndex(self, prev_ix);
                    prev_ix = forest[node_right];
                }
            }
        }
        return matches;
    }

    /* Finds all backward matches of &data[cur_ix & ring_buffer_mask] up to the
       length of max_length and stores the position cur_ix in the hash table.

       Sets *num_matches to the number of matches found, and stores the found
       matches in matches[0] to matches[*num_matches - 1]. The matches will be
       sorted by strictly increasing length and (non-strictly) increasing
       distance. */
    internal static nuint FindAllMatches(
        HashToBinaryTree* self,
        BrotliEncoderDictionary* dictionary,
        byte* data,
        nuint ring_buffer_mask, nuint cur_ix,
        nuint max_length, nuint max_backward,
        nuint dictionary_distance, BrotliEncoderParams* @params,
        BackwardMatch* matches)
    {
        BackwardMatch* orig_matches = matches;
        nuint cur_ix_masked = cur_ix & ring_buffer_mask;
        nuint best_len = 1;
        nuint short_match_max_backward =
            @params->quality != HQ_ZOPFLIFICATION_QUALITY ? 16 : (nuint)64;
        nuint stop = cur_ix - short_match_max_backward;
        uint* dict_matches = stackalloc uint[BROTLI_MAX_STATIC_DICTIONARY_MATCH_LEN + 1];
        nuint i;
        if (cur_ix < short_match_max_backward) { stop = 0; }
        for (i = cur_ix - 1; i > stop && best_len <= 2; --i)
        {
            nuint prev_ix = i;
            nuint backward = cur_ix - prev_ix;
            if (backward > max_backward)
            {
                break;
            }
            prev_ix &= ring_buffer_mask;
            if (data[cur_ix_masked] != data[prev_ix] ||
                data[cur_ix_masked + 1] != data[prev_ix + 1])
            {
                continue;
            }
            {
                nuint len =
                    FindMatchLength.FindMatchLengthWithLimit(&data[prev_ix], &data[cur_ix_masked],
                                                             max_length);
                if (len > best_len)
                {
                    best_len = len;
                    InitBackwardMatch(matches++, backward, len);
                }
            }
        }
        if (best_len < max_length)
        {
            matches = StoreAndFindMatches(self, data, cur_ix,
                ring_buffer_mask, max_length, max_backward, &best_len, matches);
        }
        for (i = 0; i <= BROTLI_MAX_STATIC_DICTIONARY_MATCH_LEN; ++i)
        {
            dict_matches[i] = kInvalidMatch;
        }
        {
            nuint minlen = BROTLI_MAX(4, best_len + 1);
            if (BrotliFindAllStaticDictionaryMatches(dictionary,
                &data[cur_ix_masked], minlen, max_length, &dict_matches[0]))
            {
                nuint maxlen = BROTLI_MIN(
                    BROTLI_MAX_STATIC_DICTIONARY_MATCH_LEN, max_length);
                nuint l;
                for (l = minlen; l <= maxlen; ++l)
                {
                    uint dict_id = dict_matches[l];
                    if (dict_id < kInvalidMatch)
                    {
                        nuint distance = dictionary_distance + (dict_id >> 5) + 1;
                        if (distance <= @params->dist.max_distance)
                        {
                            InitDictionaryBackwardMatch(matches++, distance, l, dict_id & 31);
                        }
                    }
                }
            }
        }
        return (nuint)(matches - orig_matches);
    }

    /* Stores the hash of the next 4 bytes and re-roots the binary tree at the
       current sequence, without returning any matches.
       REQUIRES: ix + MAX_TREE_COMP_LENGTH <= end-of-current-block */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Store(HashToBinaryTree* self,
        byte* data,
        nuint mask, nuint ix)
    {
        /* Maximum distance is window size - 16, see section 9.1. of the spec. */
        nuint max_backward = self->window_mask_ - WindowGap + 1;
        StoreAndFindMatches(self, data, ix, mask, MAX_TREE_COMP_LENGTH,
            max_backward, null, null);
    }

    internal static void StoreRange(HashToBinaryTree* self,
        byte* data, nuint mask,
        nuint ix_start, nuint ix_end)
    {
        nuint i = ix_start;
        nuint j = ix_start;
        if (ix_start + 63 <= ix_end)
        {
            i = ix_end - 63;
        }
        if (ix_start + 512 <= i)
        {
            for (; j < i; j += 8)
            {
                Store(self, data, mask, j);
            }
        }
        for (; i < ix_end; ++i)
        {
            Store(self, data, mask, i);
        }
    }

    internal static void StitchToPreviousBlock(
        HashToBinaryTree* self,
        nuint num_bytes, nuint position, byte* ringbuffer,
        nuint ringbuffer_mask)
    {
        if (num_bytes >= HashTypeLength() - 1 &&
            position >= MAX_TREE_COMP_LENGTH)
        {
            /* Store the last `MAX_TREE_COMP_LENGTH - 1` positions in the hasher.
               These could not be calculated before, since they require knowledge
               of both the previous and the current block. */
            nuint i_start = position - MAX_TREE_COMP_LENGTH + 1;
            nuint i_end = BROTLI_MIN(position, i_start + num_bytes);
            nuint i;
            for (i = i_start; i < i_end; ++i)
            {
                /* Maximum distance is window size - 16, see section 9.1. of the spec.
                   Furthermore, we have to make sure that we don't look further back
                   from the start of the next block than the window size, otherwise we
                   could access already overwritten areas of the ring-buffer. */
                nuint max_backward =
                    self->window_mask_ - BROTLI_MAX((nuint)(WindowGap - 1),
                                                  position - i);
                /* We know that i + MAX_TREE_COMP_LENGTH <= position + num_bytes, i.e. the
                   end of the current block and that we have at least
                   MAX_TREE_COMP_LENGTH tail in the ring-buffer. */
                StoreAndFindMatches(self, ringbuffer, i, ringbuffer_mask,
                    MAX_TREE_COMP_LENGTH, max_backward, null, null);
            }
        }
    }
}
