// Port of c/enc/backward_references_hq.{h,c} (brotli v1.1.0): the Zopfli
// (q10) and HqZopfli (q11) shortest-path command search over the binary-tree
// hasher H10 (HashToBinaryTree.cs).
//
// The cost model uses float exactly as the C does — expression shapes must
// match the C source so FP results are stable (PORTING.md). The compound-
// dictionary lanes (LookupAllCompoundDictionaryMatches + MergeMatches and the
// compound copy-source arm of UpdateNodes) are deferred with the compound
// dictionary itself; they are unreachable while nothing can attach a compound
// dictionary (num_chunks == 0, total_size == 0).

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Context;
using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.Command;
using static DnBrotli.Enc.FastLog;
using static DnBrotli.Enc.Hash;
using static DnBrotli.Enc.MemoryManager;
using static DnBrotli.Enc.Quality;

namespace DnBrotli.Enc;

/// <summary>The <c>union u</c> of <see cref="ZopfliNode"/>. This union holds
/// information used by dynamic-programming. During forward pass |cost| it
/// used to store the goal function. When node is processed its |cost| is
/// invalidated in favor of |shortcut|. On path back-tracing pass |next| is
/// assigned the offset to next node on the path.</summary>
[StructLayout(LayoutKind.Explicit)]
internal struct ZopfliNodeU
{
    /* Smallest cost to get to this byte from the beginning, as found so far. */
    [FieldOffset(0)] public float cost;
    /* Offset to the next node on the path. Equals to command_length() of the
       next node on the path. For last node equals to BROTLI_UINT32_MAX */
    [FieldOffset(0)] public uint next;
    /* Node position that provides next distance for distance cache. */
    [FieldOffset(0)] public uint shortcut;
}

/// <summary><c>struct ZopfliNode</c>.</summary>
internal struct ZopfliNode
{
    /* Best length to get up to this byte (not including this byte itself)
       highest 7 bit is used to reconstruct the length code. */
    public uint length;
    /* Distance associated with the length. */
    public uint distance;
    /* Number of literal inserts before this copy; highest 5 bits contain
       distance short code + 1 (or zero if no short code). */
    public uint dcode_insert_length;

    public ZopfliNodeU u;
}

/// <summary><c>struct ZopfliCostModelArena</c>: temporary data for
/// ZopfliCostModelSetFromCommands.</summary>
internal unsafe struct ZopfliCostModelArena
{
    public fixed uint histogram_literal[BROTLI_NUM_LITERAL_SYMBOLS];
    public fixed uint histogram_cmd[BROTLI_NUM_COMMAND_SYMBOLS];
    public fixed uint histogram_dist[BackwardReferencesHq.BROTLI_MAX_EFFECTIVE_DISTANCE_ALPHABET_SIZE];
    public fixed float cost_literal[BROTLI_NUM_LITERAL_SYMBOLS];
}

/// <summary>The anonymous <c>union { size_t literal_histograms[3 * 256];
/// ZopfliCostModelArena arena; }</c> at the tail of
/// <see cref="ZopfliCostModel"/> (named <c>u</c> here — C# has no anonymous
/// unions).</summary>
[StructLayout(LayoutKind.Explicit)]
internal unsafe struct ZopfliCostModelUnion
{
    /* size_t[3 * 256]; size_t is 64-bit on every dn2cpp target and fixed
       buffers cannot be nuint-typed, so the room is ulong and consumers cast
       to nuint*. */
    [FieldOffset(0)] public fixed ulong literal_histograms[3 * 256];
    [FieldOffset(0)] public ZopfliCostModelArena arena;
}

/// <summary><c>struct ZopfliCostModel</c>: histogram based cost model for
/// zopflification.</summary>
internal unsafe struct ZopfliCostModel
{
    /* The insert and copy length symbols. */
    public fixed float cost_cmd_[BROTLI_NUM_COMMAND_SYMBOLS];
    public float* cost_dist_;
    public uint distance_histogram_size;
    /* Cumulative costs of literals per position in the stream. */
    public float* literal_costs_;
    public float min_cost_cmd_;
    public nuint num_bytes_;

    /* Temporary data. */
    public ZopfliCostModelUnion u;
}

/// <summary><c>struct PosData</c>.</summary>
internal unsafe struct PosData
{
    public nuint pos;
    public fixed int distance_cache[4];
    public float costdiff;
    public float cost;
}

/// <summary><c>struct StartPosQueue</c>: maintains the smallest 8 cost
/// difference together with their positions.</summary>
internal unsafe struct StartPosQueue
{
    /* PosData q_[8] (embedded array of small structs -> eight sequential
       rooms; StartPosQueueQ recovers the C array indexing). */
    public PosData q_0, q_1, q_2, q_3, q_4, q_5, q_6, q_7;
    public nuint idx_;
}

internal static unsafe class BackwardReferencesHq
{
    private const int BROTLI_TRUE = 1;
    private const int BROTLI_FALSE = 0;

    private const uint BROTLI_UINT32_MAX = uint.MaxValue;

    /* BrotliCalculateDistanceCodeLimit(BROTLI_MAX_ALLOWED_DISTANCE, 3, 120). */
    internal const int BROTLI_MAX_EFFECTIVE_DISTANCE_ALPHABET_SIZE = 544;

    private const float kInfinity = 1.7e38f;  /* ~= 2 ^ 127 */

    private static readonly uint[] kDistanceCacheIndex =
    {
        0, 1, 2, 3, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1,
    };
    private static readonly int[] kDistanceCacheOffset =
    {
        0, 0, 0, 0, -1, 1, -2, 2, -3, 3, -1, 1, -2, 2, -3, 3,
    };

    internal static void BrotliInitZopfliNodes(ZopfliNode* array, nuint length)
    {
        ZopfliNode stub = default;  /* C: uninitialized; u.cost alone leaves the
                                       union partially assigned for C# flow analysis. */
        nuint i;
        stub.length = 1;
        stub.distance = 0;
        stub.dcode_insert_length = 0;
        stub.u.cost = kInfinity;
        for (i = 0; i < length; ++i) array[i] = stub;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZopfliNodeCopyLength(ZopfliNode* self)
    {
        return self->length & 0x1FFFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZopfliNodeLengthCode(ZopfliNode* self)
    {
        uint modifier = self->length >> 25;
        return ZopfliNodeCopyLength(self) + 9u - modifier;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZopfliNodeCopyDistance(ZopfliNode* self)
    {
        return self->distance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZopfliNodeDistanceCode(ZopfliNode* self)
    {
        uint short_code = self->dcode_insert_length >> 27;
        return short_code == 0 ?
            ZopfliNodeCopyDistance(self) + BROTLI_NUM_DISTANCE_SHORT_CODES - 1 :
            short_code - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZopfliNodeCommandLength(ZopfliNode* self)
    {
        return ZopfliNodeCopyLength(self) + (self->dcode_insert_length & 0x7FFFFFF);
    }

    private static void InitZopfliCostModel(
        MemoryManager* m, ZopfliCostModel* self, BrotliDistanceParams* dist,
        nuint num_bytes)
    {
        self->num_bytes_ = num_bytes;
        self->literal_costs_ = BROTLI_ALLOC<float>(m, num_bytes + 2);
        self->cost_dist_ = BROTLI_ALLOC<float>(m, dist->alphabet_size_limit);
        self->distance_histogram_size = dist->alphabet_size_limit;
        if (BROTLI_IS_OOM(m)) return;
    }

    private static void CleanupZopfliCostModel(MemoryManager* m, ZopfliCostModel* self)
    {
        BROTLI_FREE(m, ref self->literal_costs_);
        BROTLI_FREE(m, ref self->cost_dist_);
    }

    private static void SetCost(uint* histogram, nuint histogram_size,
                                int literal_histogram, float* cost)
    {
        nuint sum = 0;
        nuint missing_symbol_sum;
        float log2sum;
        float missing_symbol_cost;
        nuint i;
        for (i = 0; i < histogram_size; i++)
        {
            sum += histogram[i];
        }
        log2sum = (float)FastLog2(sum);
        missing_symbol_sum = sum;
        if (literal_histogram == 0)
        {
            for (i = 0; i < histogram_size; i++)
            {
                if (histogram[i] == 0) missing_symbol_sum++;
            }
        }
        missing_symbol_cost = (float)FastLog2(missing_symbol_sum) + 2;
        for (i = 0; i < histogram_size; i++)
        {
            if (histogram[i] == 0)
            {
                cost[i] = missing_symbol_cost;
                continue;
            }

            /* Shannon bits for this symbol. */
            cost[i] = log2sum - (float)FastLog2(histogram[i]);

            /* Cannot be coded with less than 1 bit */
            if (cost[i] < 1) cost[i] = 1;
        }
    }

    private static void ZopfliCostModelSetFromCommands(ZopfliCostModel* self,
                                                       nuint position,
                                                       byte* ringbuffer,
                                                       nuint ringbuffer_mask,
                                                       Command* commands,
                                                       nuint num_commands,
                                                       nuint last_insert_len)
    {
        ZopfliCostModelArena* arena = &self->u.arena;
        nuint pos = position - last_insert_len;
        float min_cost_cmd = kInfinity;
        nuint i;
        float* cost_cmd = self->cost_cmd_;

        new Span<uint>(arena->histogram_literal, BROTLI_NUM_LITERAL_SYMBOLS).Clear();
        new Span<uint>(arena->histogram_cmd, BROTLI_NUM_COMMAND_SYMBOLS).Clear();
        new Span<uint>(arena->histogram_dist, BROTLI_MAX_EFFECTIVE_DISTANCE_ALPHABET_SIZE).Clear();

        for (i = 0; i < num_commands; i++)
        {
            nuint inslength = commands[i].insert_len_;
            nuint copylength = CommandCopyLen(&commands[i]);
            nuint distcode = commands[i].dist_prefix_ & 0x3FFu;
            nuint cmdcode = commands[i].cmd_prefix_;
            nuint j;

            arena->histogram_cmd[cmdcode]++;
            if (cmdcode >= 128) arena->histogram_dist[distcode]++;

            for (j = 0; j < inslength; j++)
            {
                arena->histogram_literal[ringbuffer[(pos + j) & ringbuffer_mask]]++;
            }

            pos += inslength + copylength;
        }

        SetCost(arena->histogram_literal, BROTLI_NUM_LITERAL_SYMBOLS, BROTLI_TRUE,
                arena->cost_literal);
        SetCost(arena->histogram_cmd, BROTLI_NUM_COMMAND_SYMBOLS, BROTLI_FALSE,
                cost_cmd);
        SetCost(arena->histogram_dist, self->distance_histogram_size, BROTLI_FALSE,
                self->cost_dist_);

        for (i = 0; i < BROTLI_NUM_COMMAND_SYMBOLS; ++i)
        {
            min_cost_cmd = BROTLI_MIN(min_cost_cmd, cost_cmd[i]);
        }
        self->min_cost_cmd_ = min_cost_cmd;

        {
            float* literal_costs = self->literal_costs_;
            float literal_carry = 0.0f;
            nuint num_bytes = self->num_bytes_;
            literal_costs[0] = 0.0f;
            for (i = 0; i < num_bytes; ++i)
            {
                literal_carry +=
                    arena->cost_literal[ringbuffer[(position + i) & ringbuffer_mask]];
                literal_costs[i + 1] = literal_costs[i] + literal_carry;
                literal_carry -= literal_costs[i + 1] - literal_costs[i];
            }
        }
    }

    private static void ZopfliCostModelSetFromLiteralCosts(ZopfliCostModel* self,
                                                           nuint position,
                                                           byte* ringbuffer,
                                                           nuint ringbuffer_mask)
    {
        float* literal_costs = self->literal_costs_;
        float literal_carry = 0.0f;
        float* cost_dist = self->cost_dist_;
        float* cost_cmd = self->cost_cmd_;
        nuint num_bytes = self->num_bytes_;
        nuint i;
        LiteralCost.BrotliEstimateBitCostsForLiterals(position, num_bytes, ringbuffer_mask,
                                                      ringbuffer, (nuint*)self->u.literal_histograms,
                                                      &literal_costs[1]);
        literal_costs[0] = 0.0f;
        for (i = 0; i < num_bytes; ++i)
        {
            literal_carry += literal_costs[i + 1];
            literal_costs[i + 1] = literal_costs[i] + literal_carry;
            literal_carry -= literal_costs[i + 1] - literal_costs[i];
        }
        for (i = 0; i < BROTLI_NUM_COMMAND_SYMBOLS; ++i)
        {
            cost_cmd[i] = (float)FastLog2(11 + (uint)i);
        }
        for (i = 0; i < self->distance_histogram_size; ++i)
        {
            cost_dist[i] = (float)FastLog2(20 + (uint)i);
        }
        self->min_cost_cmd_ = (float)FastLog2(11);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ZopfliCostModelGetCommandCost(
        ZopfliCostModel* self, ushort cmdcode)
    {
        return self->cost_cmd_[cmdcode];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ZopfliCostModelGetDistanceCost(
        ZopfliCostModel* self, nuint distcode)
    {
        return self->cost_dist_[distcode];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ZopfliCostModelGetLiteralCosts(
        ZopfliCostModel* self, nuint from, nuint to)
    {
        return self->literal_costs_[to] - self->literal_costs_[from];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ZopfliCostModelGetMinCostCmd(
        ZopfliCostModel* self)
    {
        return self->min_cost_cmd_;
    }

    /* REQUIRES: len >= 2, start_pos <= pos */
    /* REQUIRES: cost < kInfinity, nodes[start_pos].cost < kInfinity */
    /* Maintains the "ZopfliNode array invariant". */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdateZopfliNode(ZopfliNode* nodes, nuint pos,
        nuint start_pos, nuint len, nuint len_code, nuint dist,
        nuint short_code, float cost)
    {
        ZopfliNode* next = &nodes[pos + len];
        next->length = (uint)(len | ((len + 9u - len_code) << 25));
        next->distance = (uint)dist;
        next->dcode_insert_length = (uint)(
            (short_code << 27) | (pos - start_pos));
        next->u.cost = cost;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PosData* StartPosQueueQ(StartPosQueue* self)
    {
        /* C: self->q_ (PosData q_[8]). */
        return &self->q_0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InitStartPosQueue(StartPosQueue* self)
    {
        self->idx_ = 0;
    }

    private static nuint StartPosQueueSize(StartPosQueue* self)
    {
        return BROTLI_MIN(self->idx_, 8);
    }

    private static void StartPosQueuePush(StartPosQueue* self, PosData* posdata)
    {
        nuint offset = ~(self->idx_++) & 7;
        nuint len = StartPosQueueSize(self);
        nuint i;
        PosData* q = StartPosQueueQ(self);
        q[offset] = *posdata;
        /* Restore the sorted order. In the list of |len| items at most |len - 1|
           adjacent element comparisons / swaps are required. */
        for (i = 1; i < len; ++i)
        {
            if (q[offset & 7].costdiff > q[(offset + 1) & 7].costdiff)
            {
                /* BROTLI_SWAP(PosData, q, offset & 7, (offset + 1) & 7); */
                PosData __tmp = q[offset & 7];
                q[offset & 7] = q[(offset + 1) & 7];
                q[(offset + 1) & 7] = __tmp;
            }
            ++offset;
        }
    }

    private static PosData* StartPosQueueAt(StartPosQueue* self, nuint k)
    {
        return &StartPosQueueQ(self)[(k - self->idx_) & 7];
    }

    /* Returns the minimum possible copy length that can improve the cost of any */
    /* future position. */
    private static nuint ComputeMinimumCopyLength(float start_cost,
                                                  ZopfliNode* nodes,
                                                  nuint num_bytes,
                                                  nuint pos)
    {
        /* Compute the minimum possible cost of reaching any future position. */
        float min_cost = start_cost;
        nuint len = 2;
        nuint next_len_bucket = 4;
        nuint next_len_offset = 10;
        while (pos + len <= num_bytes && nodes[pos + len].u.cost <= min_cost)
        {
            /* We already reached (pos + len) with no more cost than the minimum
               possible cost of reaching anything from this pos, so there is no point in
               looking for lengths <= len. */
            ++len;
            if (len == next_len_offset)
            {
                /* We reached the next copy length code bucket, so we add one more
                   extra bit to the minimum cost. */
                min_cost += 1.0f;
                next_len_offset += next_len_bucket;
                next_len_bucket *= 2;
            }
        }
        return len;
    }

    /* REQUIRES: nodes[pos].cost < kInfinity
       REQUIRES: nodes[0..pos] satisfies that "ZopfliNode array invariant". */
    private static uint ComputeDistanceShortcut(nuint block_start,
                                                nuint pos,
                                                nuint max_backward_limit,
                                                nuint gap,
                                                ZopfliNode* nodes)
    {
        nuint clen = ZopfliNodeCopyLength(&nodes[pos]);
        nuint ilen = nodes[pos].dcode_insert_length & 0x7FFFFFF;
        nuint dist = ZopfliNodeCopyDistance(&nodes[pos]);
        /* Since |block_start + pos| is the end position of the command, the copy part
           starts from |block_start + pos - clen|. Distances that are greater than
           this or greater than |max_backward_limit| + |gap| are static dictionary
           references, and do not update the last distances.
           Also distance code 0 (last distance) does not update the last distances. */
        if (pos == 0)
        {
            return 0;
        }
        else if (dist + clen <= block_start + pos + gap &&
                 dist <= max_backward_limit + gap &&
                 ZopfliNodeDistanceCode(&nodes[pos]) > 0)
        {
            return (uint)pos;
        }
        else
        {
            return nodes[pos - clen - ilen].u.shortcut;
        }
    }

    /* Fills in dist_cache[0..3] with the last four distances (as defined by
       Section 4. of the Spec) that would be used at (block_start + pos) if we
       used the shortest path of commands from block_start, computed from
       nodes[0..pos]. The last four distances at block_start are in
       starting_dist_cache[0..3].
       REQUIRES: nodes[pos].cost < kInfinity
       REQUIRES: nodes[0..pos] satisfies that "ZopfliNode array invariant". */
    private static void ComputeDistanceCache(nuint pos,
                                             int* starting_dist_cache,
                                             ZopfliNode* nodes,
                                             int* dist_cache)
    {
        int idx = 0;
        nuint p = nodes[pos].u.shortcut;
        while (idx < 4 && p > 0)
        {
            nuint ilen = nodes[p].dcode_insert_length & 0x7FFFFFF;
            nuint clen = ZopfliNodeCopyLength(&nodes[p]);
            nuint dist = ZopfliNodeCopyDistance(&nodes[p]);
            dist_cache[idx++] = (int)dist;
            /* Because of prerequisite, p >= clen + ilen >= 2. */
            p = nodes[p - clen - ilen].u.shortcut;
        }
        for (; idx < 4; ++idx)
        {
            dist_cache[idx] = *starting_dist_cache++;
        }
    }

    /* Maintains "ZopfliNode array invariant" and pushes node to the queue, if it
       is eligible. */
    private static void EvaluateNode(
        nuint block_start, nuint pos, nuint max_backward_limit,
        nuint gap, int* starting_dist_cache,
        ZopfliCostModel* model, StartPosQueue* queue, ZopfliNode* nodes)
    {
        /* Save cost, because ComputeDistanceCache invalidates it. */
        float node_cost = nodes[pos].u.cost;
        nodes[pos].u.shortcut = ComputeDistanceShortcut(
            block_start, pos, max_backward_limit, gap, nodes);
        if (node_cost <= ZopfliCostModelGetLiteralCosts(model, 0, pos))
        {
            PosData posdata;
            posdata.pos = pos;
            posdata.cost = node_cost;
            posdata.costdiff = node_cost -
                ZopfliCostModelGetLiteralCosts(model, 0, pos);
            ComputeDistanceCache(
                pos, starting_dist_cache, nodes, posdata.distance_cache);
            StartPosQueuePush(queue, &posdata);
        }
    }

    /* Returns longest copy length. */
    private static nuint UpdateNodes(
        nuint num_bytes, nuint block_start, nuint pos,
        byte* ringbuffer, nuint ringbuffer_mask,
        BrotliEncoderParams* @params, nuint max_backward_limit,
        int* starting_dist_cache, nuint num_matches,
        BackwardMatch* matches, ZopfliCostModel* model,
        StartPosQueue* queue, ZopfliNode* nodes)
    {
        nuint stream_offset = @params->stream_offset;
        nuint cur_ix = block_start + pos;
        nuint cur_ix_masked = cur_ix & ringbuffer_mask;
        nuint max_distance = BROTLI_MIN(cur_ix, max_backward_limit);
        nuint dictionary_start = BROTLI_MIN(
            cur_ix + stream_offset, max_backward_limit);
        nuint max_len = num_bytes - pos;
        nuint max_zopfli_len = MaxZopfliLen(@params);
        nuint max_iters = MaxZopfliCandidates(@params);
        nuint min_len;
        nuint result = 0;
        nuint k;
        CompoundDictionary* addon = &@params->dictionary.compound;
        nuint gap = addon->total_size;

        EvaluateNode(block_start + stream_offset, pos, max_backward_limit, gap,
            starting_dist_cache, model, queue, nodes);

        {
            PosData* posdata = StartPosQueueAt(queue, 0);
            float min_cost = (posdata->cost + ZopfliCostModelGetMinCostCmd(model) +
                ZopfliCostModelGetLiteralCosts(model, posdata->pos, pos));
            min_len = ComputeMinimumCopyLength(min_cost, nodes, num_bytes, pos);
        }

        /* Go over the command starting positions in order of increasing cost
           difference. */
        for (k = 0; k < max_iters && k < StartPosQueueSize(queue); ++k)
        {
            PosData* posdata = StartPosQueueAt(queue, k);
            nuint start = posdata->pos;
            ushort inscode = GetInsertLengthCode(pos - start);
            float start_costdiff = posdata->costdiff;
            float base_cost = start_costdiff + (float)GetInsertExtra(inscode) +
                ZopfliCostModelGetLiteralCosts(model, 0, pos);

            /* Look for last distance matches using the distance cache from this
               starting position. */
            nuint best_len = min_len - 1;
            nuint j = 0;
            for (; j < BROTLI_NUM_DISTANCE_SHORT_CODES && best_len < max_len; ++j)
            {
                nuint idx = kDistanceCacheIndex[j];
                nuint backward =
                    (nuint)(posdata->distance_cache[idx] + kDistanceCacheOffset[j]);
                nuint prev_ix = cur_ix - backward;
                nuint len = 0;
                byte continuation = ringbuffer[cur_ix_masked + best_len];
                if (cur_ix_masked + best_len > ringbuffer_mask)
                {
                    break;
                }
                if (backward > dictionary_start + gap)
                {
                    /* Word dictionary -> ignore. */
                    continue;
                }
                if (backward <= max_distance)
                {
                    /* Regular backward reference. */
                    if (prev_ix >= cur_ix)
                    {
                        continue;
                    }

                    prev_ix &= ringbuffer_mask;
                    if (prev_ix + best_len > ringbuffer_mask ||
                        continuation != ringbuffer[prev_ix + best_len])
                    {
                        continue;
                    }
                    len = FindMatchLength.FindMatchLengthWithLimit(&ringbuffer[prev_ix],
                                                                   &ringbuffer[cur_ix_masked],
                                                                   max_len);
                }
                else if (backward > dictionary_start)
                {
                    /* Deferred boundary: compound-dictionary copy source
                       (compound_dictionary.h chunk walk). Unreachable while the
                       compound part stays inert: gap == 0 makes
                       "backward > dictionary_start + gap" continue above. */
                    throw new NotImplementedException(
                        "DnBrotli DB-deferred: compound dictionary " +
                        "(c/enc/backward_references_hq.c UpdateNodes)");
                }
                else
                {
                    /* "Gray" area. It is addressable by decoder, but this encoder
                       instance does not have that data -> should not touch it. */
                    continue;
                }
                {
                    float dist_cost = base_cost +
                        ZopfliCostModelGetDistanceCost(model, j);
                    nuint l;
                    for (l = best_len + 1; l <= len; ++l)
                    {
                        ushort copycode = GetCopyLengthCode(l);
                        ushort cmdcode =
                            CombineLengthCodes(inscode, copycode, j == 0 ? 1 : 0);
                        float cost = (cmdcode < 128 ? base_cost : dist_cost) +
                            (float)GetCopyExtra(copycode) +
                            ZopfliCostModelGetCommandCost(model, cmdcode);
                        if (cost < nodes[pos + l].u.cost)
                        {
                            UpdateZopfliNode(nodes, pos, start, l, l, backward, j + 1, cost);
                            result = BROTLI_MAX(result, l);
                        }
                        best_len = l;
                    }
                }
            }

            /* At higher iterations look only for new last distance matches, since
               looking only for new command start positions with the same distances
               does not help much. */
            if (k >= 2) continue;

            {
                /* Loop through all possible copy lengths at this position. */
                nuint len = min_len;
                for (j = 0; j < num_matches; ++j)
                {
                    BackwardMatch match = matches[j];
                    nuint dist = match.distance;
                    bool is_dictionary_match = dist > dictionary_start + gap;
                    /* We already tried all possible last distance matches, so we can use
                       normal distance code here. */
                    nuint dist_code = dist + BROTLI_NUM_DISTANCE_SHORT_CODES - 1;
                    ushort dist_symbol;
                    uint distextra;
                    uint distnumextra;
                    float dist_cost;
                    nuint max_match_len;
                    Prefix.PrefixEncodeCopyDistance(
                        dist_code, @params->dist.num_direct_distance_codes,
                        @params->dist.distance_postfix_bits, &dist_symbol, &distextra);
                    distnumextra = (uint)dist_symbol >> 10;
                    dist_cost = base_cost + (float)distnumextra +
                        ZopfliCostModelGetDistanceCost(model, dist_symbol & 0x3FFu);

                    /* Try all copy lengths up until the maximum copy length corresponding
                       to this distance. If the distance refers to the static dictionary, or
                       the maximum length is long enough, try only one maximum length. */
                    max_match_len = BackwardMatchLength(&match);
                    if (len < max_match_len &&
                        (is_dictionary_match || max_match_len > max_zopfli_len))
                    {
                        len = max_match_len;
                    }
                    for (; len <= max_match_len; ++len)
                    {
                        nuint len_code =
                            is_dictionary_match ? BackwardMatchLengthCode(&match) : len;
                        ushort copycode = GetCopyLengthCode(len_code);
                        ushort cmdcode = CombineLengthCodes(inscode, copycode, 0);
                        float cost = dist_cost + (float)GetCopyExtra(copycode) +
                            ZopfliCostModelGetCommandCost(model, cmdcode);
                        if (cost < nodes[pos + len].u.cost)
                        {
                            UpdateZopfliNode(nodes, pos, start, len, len_code, dist, 0, cost);
                            result = BROTLI_MAX(result, len);
                        }
                    }
                }
            }
        }
        return result;
    }

    private static nuint ComputeShortestPathFromNodes(nuint num_bytes,
        ZopfliNode* nodes)
    {
        nuint index = num_bytes;
        nuint num_commands = 0;
        while ((nodes[index].dcode_insert_length & 0x7FFFFFF) == 0 &&
            nodes[index].length == 1) --index;
        nodes[index].u.next = BROTLI_UINT32_MAX;
        while (index != 0)
        {
            nuint len = ZopfliNodeCommandLength(&nodes[index]);
            index -= len;
            nodes[index].u.next = (uint)len;
            num_commands++;
        }
        return num_commands;
    }

    /* REQUIRES: nodes != NULL and len(nodes) >= num_bytes + 1 */
    internal static void BrotliZopfliCreateCommands(nuint num_bytes,
        nuint block_start, ZopfliNode* nodes, int* dist_cache,
        nuint* last_insert_len, BrotliEncoderParams* @params,
        Command* commands, nuint* num_literals)
    {
        nuint stream_offset = @params->stream_offset;
        nuint max_backward_limit = MaxBackwardLimit(@params->lgwin);
        nuint pos = 0;
        uint offset = nodes[0].u.next;
        nuint i;
        nuint gap = @params->dictionary.compound.total_size;
        for (i = 0; offset != BROTLI_UINT32_MAX; i++)
        {
            ZopfliNode* next = &nodes[pos + offset];
            nuint copy_length = ZopfliNodeCopyLength(next);
            nuint insert_length = next->dcode_insert_length & 0x7FFFFFF;
            pos += insert_length;
            offset = next->u.next;
            if (i == 0)
            {
                insert_length += *last_insert_len;
                *last_insert_len = 0;
            }
            {
                nuint distance = ZopfliNodeCopyDistance(next);
                nuint len_code = ZopfliNodeLengthCode(next);
                nuint dictionary_start = BROTLI_MIN(
                    block_start + pos + stream_offset, max_backward_limit);
                bool is_dictionary = distance > dictionary_start + gap;
                nuint dist_code = ZopfliNodeDistanceCode(next);
                InitCommand(&commands[i], &@params->dist, insert_length,
                    copy_length, (int)len_code - (int)copy_length, dist_code);

                if (!is_dictionary && dist_code > 0)
                {
                    dist_cache[3] = dist_cache[2];
                    dist_cache[2] = dist_cache[1];
                    dist_cache[1] = dist_cache[0];
                    dist_cache[0] = (int)distance;
                }
            }

            *num_literals += insert_length;
            pos += copy_length;
        }
        *last_insert_len += num_bytes - pos;
    }

    private static nuint ZopfliIterate(nuint num_bytes, nuint position,
        byte* ringbuffer, nuint ringbuffer_mask,
        BrotliEncoderParams* @params, nuint gap, int* dist_cache,
        ZopfliCostModel* model, uint* num_matches,
        BackwardMatch* matches, ZopfliNode* nodes)
    {
        nuint stream_offset = @params->stream_offset;
        nuint max_backward_limit = MaxBackwardLimit(@params->lgwin);
        nuint max_zopfli_len = MaxZopfliLen(@params);
        StartPosQueue queue;
        nuint cur_match_pos = 0;
        nuint i;
        nodes[0].length = 0;
        nodes[0].u.cost = 0;
        InitStartPosQueue(&queue);
        for (i = 0; i + 3 < num_bytes; i++)
        {
            nuint skip = UpdateNodes(num_bytes, position, i, ringbuffer,
                ringbuffer_mask, @params, max_backward_limit, dist_cache,
                num_matches[i], &matches[cur_match_pos], model, &queue, nodes);
            if (skip < BROTLI_LONG_COPY_QUICK_STEP) skip = 0;
            cur_match_pos += num_matches[i];
            if (num_matches[i] == 1 &&
                BackwardMatchLength(&matches[cur_match_pos - 1]) > max_zopfli_len)
            {
                skip = BROTLI_MAX(
                    BackwardMatchLength(&matches[cur_match_pos - 1]), skip);
            }
            if (skip > 1)
            {
                skip--;
                while (skip != 0)
                {
                    i++;
                    if (i + 3 >= num_bytes) break;
                    EvaluateNode(position + stream_offset, i, max_backward_limit, gap,
                        dist_cache, model, &queue, nodes);
                    cur_match_pos += num_matches[i];
                    skip--;
                }
            }
        }
        return ComputeShortestPathFromNodes(num_bytes, nodes);
    }

    /* MergeMatches (compound-dictionary match merging) is deferred with the
       compound dictionary itself; its only callers are the num_chunks != 0
       lanes below. */

    /* REQUIRES: nodes != NULL and len(nodes) >= num_bytes + 1 */
    internal static nuint BrotliZopfliComputeShortestPath(MemoryManager* m, nuint num_bytes,
        nuint position, byte* ringbuffer, nuint ringbuffer_mask,
        byte* literal_context_lut, BrotliEncoderParams* @params,
        int* dist_cache, Hasher* hasher, ZopfliNode* nodes)
    {
        nuint stream_offset = @params->stream_offset;
        nuint max_backward_limit = MaxBackwardLimit(@params->lgwin);
        nuint max_zopfli_len = MaxZopfliLen(@params);
        StartPosQueue queue;
        BackwardMatch* matches =
            BROTLI_ALLOC<BackwardMatch>(m, 2 * (MAX_NUM_MATCHES_H10 + 64));
        nuint store_end = num_bytes >= H10.StoreLookahead() ?
            position + num_bytes - H10.StoreLookahead() + 1 : position;
        nuint i;
        CompoundDictionary* addon = &@params->dictionary.compound;
        nuint gap = addon->total_size;
        nuint lz_matches_offset =
            (addon->num_chunks != 0) ? (nuint)(MAX_NUM_MATCHES_H10 + 128) : 0;
        ZopfliCostModel* model = BROTLI_ALLOC<ZopfliCostModel>(m, 1);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(model) || BROTLI_IS_NULL(matches))
        {
            return 0;
        }
        nodes[0].length = 0;
        nodes[0].u.cost = 0;
        InitZopfliCostModel(m, model, &@params->dist, num_bytes);
        if (BROTLI_IS_OOM(m)) return 0;
        ZopfliCostModelSetFromLiteralCosts(
            model, position, ringbuffer, ringbuffer_mask);
        InitStartPosQueue(&queue);
        for (i = 0; i + H10.HashTypeLength() - 1 < num_bytes; i++)
        {
            nuint pos = position + i;
            nuint max_distance = BROTLI_MIN(pos, max_backward_limit);
            nuint dictionary_start = BROTLI_MIN(
                pos + stream_offset, max_backward_limit);
            nuint skip;
            nuint num_matches;
            nuint dict_id = 0;
            if (@params->dictionary.contextual.context_based != 0)
            {
                byte p1 = pos >= 1 ?
                    ringbuffer[(pos - 1) & ringbuffer_mask] : (byte)0;
                byte p2 = pos >= 2 ?
                    ringbuffer[(pos - 2) & ringbuffer_mask] : (byte)0;
                dict_id = @params->dictionary.contextual.context_map[
                    BROTLI_CONTEXT(p1, p2, literal_context_lut)];
            }
            num_matches = H10.FindAllMatches(&hasher->privat._H10,
                @params->dictionary.contextual.dict(dict_id),
                ringbuffer, ringbuffer_mask, pos, num_bytes - i, max_distance,
                dictionary_start + gap, @params, &matches[lz_matches_offset]);
            if (addon->num_chunks != 0)
            {
                /* Deferred boundary: LookupAllCompoundDictionaryMatches +
                   MergeMatches. Unreachable while nothing can attach a compound
                   dictionary (num_chunks == 0). */
                throw new NotImplementedException(
                    "DnBrotli DB-deferred: compound dictionary " +
                    "(c/enc/backward_references_hq.c BrotliZopfliComputeShortestPath)");
            }
            if (num_matches > 0 &&
                BackwardMatchLength(&matches[num_matches - 1]) > max_zopfli_len)
            {
                matches[0] = matches[num_matches - 1];
                num_matches = 1;
            }
            skip = UpdateNodes(num_bytes, position, i, ringbuffer, ringbuffer_mask,
                @params, max_backward_limit, dist_cache, num_matches, matches, model,
                &queue, nodes);
            if (skip < BROTLI_LONG_COPY_QUICK_STEP) skip = 0;
            if (num_matches == 1 && BackwardMatchLength(&matches[0]) > max_zopfli_len)
            {
                skip = BROTLI_MAX(BackwardMatchLength(&matches[0]), skip);
            }
            if (skip > 1)
            {
                /* Add the tail of the copy to the hasher. */
                H10.StoreRange(&hasher->privat._H10,
                    ringbuffer, ringbuffer_mask, pos + 1, BROTLI_MIN(
                    pos + skip, store_end));
                skip--;
                while (skip != 0)
                {
                    i++;
                    if (i + H10.HashTypeLength() - 1 >= num_bytes) break;
                    EvaluateNode(position + stream_offset, i, max_backward_limit, gap,
                        dist_cache, model, &queue, nodes);
                    skip--;
                }
            }
        }
        CleanupZopfliCostModel(m, model);
        BROTLI_FREE(m, ref model);
        BROTLI_FREE(m, ref matches);
        return ComputeShortestPathFromNodes(num_bytes, nodes);
    }

    /// <summary><c>BrotliCreateZopfliBackwardReferences</c> (q10).</summary>
    internal static void BrotliCreateZopfliBackwardReferences(MemoryManager* m, nuint num_bytes,
        nuint position, byte* ringbuffer, nuint ringbuffer_mask,
        byte* literal_context_lut, BrotliEncoderParams* @params,
        Hasher* hasher, int* dist_cache, nuint* last_insert_len,
        Command* commands, nuint* num_commands, nuint* num_literals)
    {
        ZopfliNode* nodes = BROTLI_ALLOC<ZopfliNode>(m, num_bytes + 1);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(nodes)) return;
        BrotliInitZopfliNodes(nodes, num_bytes + 1);
        *num_commands += BrotliZopfliComputeShortestPath(m, num_bytes,
            position, ringbuffer, ringbuffer_mask, literal_context_lut, @params,
            dist_cache, hasher, nodes);
        if (BROTLI_IS_OOM(m)) return;
        BrotliZopfliCreateCommands(num_bytes, position, nodes, dist_cache,
            last_insert_len, @params, commands, num_literals);
        BROTLI_FREE(m, ref nodes);
    }

    /// <summary><c>BrotliCreateHqZopfliBackwardReferences</c> (q11).</summary>
    internal static void BrotliCreateHqZopfliBackwardReferences(MemoryManager* m, nuint num_bytes,
        nuint position, byte* ringbuffer, nuint ringbuffer_mask,
        byte* literal_context_lut, BrotliEncoderParams* @params,
        Hasher* hasher, int* dist_cache, nuint* last_insert_len,
        Command* commands, nuint* num_commands, nuint* num_literals)
    {
        nuint stream_offset = @params->stream_offset;
        nuint max_backward_limit = MaxBackwardLimit(@params->lgwin);
        uint* num_matches = BROTLI_ALLOC<uint>(m, num_bytes);
        nuint matches_size = 4 * num_bytes;
        nuint store_end = num_bytes >= H10.StoreLookahead() ?
            position + num_bytes - H10.StoreLookahead() + 1 : position;
        nuint cur_match_pos = 0;
        nuint i;
        nuint orig_num_literals;
        nuint orig_last_insert_len;
        int* orig_dist_cache = stackalloc int[4];
        nuint orig_num_commands;
        ZopfliCostModel* model = BROTLI_ALLOC<ZopfliCostModel>(m, 1);
        ZopfliNode* nodes;
        BackwardMatch* matches = BROTLI_ALLOC<BackwardMatch>(m, matches_size);
        CompoundDictionary* addon = &@params->dictionary.compound;
        nuint gap = addon->total_size;
        nuint shadow_matches =
            (addon->num_chunks != 0) ? (nuint)(MAX_NUM_MATCHES_H10 + 128) : 0;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(model) ||
            BROTLI_IS_NULL(num_matches) || BROTLI_IS_NULL(matches))
        {
            return;
        }
        for (i = 0; i + H10.HashTypeLength() - 1 < num_bytes; ++i)
        {
            nuint pos = position + i;
            nuint max_distance = BROTLI_MIN(pos, max_backward_limit);
            nuint dictionary_start = BROTLI_MIN(
                pos + stream_offset, max_backward_limit);
            nuint max_length = num_bytes - i;
            nuint num_found_matches;
            nuint cur_match_end;
            nuint dict_id = 0;
            if (@params->dictionary.contextual.context_based != 0)
            {
                byte p1 = pos >= 1 ?
                    ringbuffer[(pos - 1) & ringbuffer_mask] : (byte)0;
                byte p2 = pos >= 2 ?
                    ringbuffer[(pos - 2) & ringbuffer_mask] : (byte)0;
                dict_id = @params->dictionary.contextual.context_map[
                    BROTLI_CONTEXT(p1, p2, literal_context_lut)];
            }
            /* Ensure that we have enough free slots. */
            BROTLI_ENSURE_CAPACITY(m, ref matches, ref matches_size,
                cur_match_pos + MAX_NUM_MATCHES_H10 + shadow_matches);
            if (BROTLI_IS_OOM(m)) return;
            num_found_matches = H10.FindAllMatches(&hasher->privat._H10,
                @params->dictionary.contextual.dict(dict_id),
                ringbuffer, ringbuffer_mask, pos, max_length,
                max_distance, dictionary_start + gap, @params,
                &matches[cur_match_pos + shadow_matches]);
            if (addon->num_chunks != 0)
            {
                /* Deferred boundary: LookupAllCompoundDictionaryMatches +
                   MergeMatches. Unreachable while nothing can attach a compound
                   dictionary (num_chunks == 0). */
                throw new NotImplementedException(
                    "DnBrotli DB-deferred: compound dictionary " +
                    "(c/enc/backward_references_hq.c BrotliCreateHqZopfliBackwardReferences)");
            }
            cur_match_end = cur_match_pos + num_found_matches;
            /* C: BROTLI_DCHECK over matches[cur_match_pos..cur_match_end) being
               sorted by non-decreasing length. */
            num_matches[i] = (uint)num_found_matches;
            if (num_found_matches > 0)
            {
                nuint match_len = BackwardMatchLength(&matches[cur_match_end - 1]);
                if (match_len > MAX_ZOPFLI_LEN_QUALITY_11)
                {
                    nuint skip = match_len - 1;
                    matches[cur_match_pos++] = matches[cur_match_end - 1];
                    num_matches[i] = 1;
                    /* Add the tail of the copy to the hasher. */
                    H10.StoreRange(&hasher->privat._H10,
                                   ringbuffer, ringbuffer_mask, pos + 1,
                                   BROTLI_MIN(pos + match_len, store_end));
                    new Span<uint>(&num_matches[i + 1], (int)skip).Clear();
                    i += skip;
                }
                else
                {
                    cur_match_pos = cur_match_end;
                }
            }
        }
        orig_num_literals = *num_literals;
        orig_last_insert_len = *last_insert_len;
        Buffer.MemoryCopy(dist_cache, orig_dist_cache, 4 * sizeof(int), 4 * sizeof(int));
        orig_num_commands = *num_commands;
        nodes = BROTLI_ALLOC<ZopfliNode>(m, num_bytes + 1);
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(nodes)) return;
        InitZopfliCostModel(m, model, &@params->dist, num_bytes);
        if (BROTLI_IS_OOM(m)) return;
        for (i = 0; i < 2; i++)
        {
            BrotliInitZopfliNodes(nodes, num_bytes + 1);
            if (i == 0)
            {
                ZopfliCostModelSetFromLiteralCosts(
                    model, position, ringbuffer, ringbuffer_mask);
            }
            else
            {
                ZopfliCostModelSetFromCommands(model, position, ringbuffer,
                    ringbuffer_mask, commands, *num_commands - orig_num_commands,
                    orig_last_insert_len);
            }
            *num_commands = orig_num_commands;
            *num_literals = orig_num_literals;
            *last_insert_len = orig_last_insert_len;
            Buffer.MemoryCopy(orig_dist_cache, dist_cache, 4 * sizeof(int), 4 * sizeof(int));
            *num_commands += ZopfliIterate(num_bytes, position, ringbuffer,
                ringbuffer_mask, @params, gap, dist_cache, model, num_matches, matches,
                nodes);
            BrotliZopfliCreateCommands(num_bytes, position, nodes, dist_cache,
                last_insert_len, @params, commands, num_literals);
        }
        CleanupZopfliCostModel(m, model);
        BROTLI_FREE(m, ref model);
        BROTLI_FREE(m, ref nodes);
        BROTLI_FREE(m, ref matches);
        BROTLI_FREE(m, ref num_matches);
    }
}
