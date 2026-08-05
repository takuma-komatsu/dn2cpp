// Port of c/enc/compress_fragment_two_pass.{h,c} (brotli v1.1.0): fast
// two-pass encoding of an input fragment (quality 1) — first pass collects
// backward matches and literals into buffers, second pass emits them with
// prefix codes built from the actual histograms.
//
// C quirk preserved verbatim (do NOT "fix"): in CreateCommands' first
// table-update block the min_match==4 branch hashes offsets {0, 1, 0} for
// positions -3/-2/-1 (the second block uses {0, 1, 2}); upstream ships it
// this way and the output depends on it.

using System.Runtime.CompilerServices;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.BitCost;
using static DnBrotli.Enc.BrotliBitStream;
using static DnBrotli.Enc.EntropyEncode;
using static DnBrotli.Enc.FindMatchLength;
using static DnBrotli.Enc.WriteBits;
using static DnBrotli.Internal.Bits;

namespace DnBrotli.Enc;

/// <summary><c>struct BrotliTwoPassArena</c>. Lives in unmanaged memory only.</summary>
internal unsafe struct BrotliTwoPassArena
{
    public fixed uint lit_histo[256];
    public fixed byte lit_depth[256];
    public fixed ushort lit_bits[256];

    public fixed uint cmd_histo[128];
    public fixed byte cmd_depth[128];
    public fixed ushort cmd_bits[128];

    /* BuildAndStoreCommandPrefixCode: HuffmanTree tmp_tree[513], flattened.
       12 bytes of capacity per entry, not 8 — see CompressFragment's tree_
       (dn2cpp widens the small HuffmanTree fields to int32). */
    public fixed ulong tmp_tree_[(3 * (2 * BROTLI_NUM_LITERAL_SYMBOLS + 1) + 1) / 2];
    public fixed byte tmp_depth[BROTLI_NUM_COMMAND_SYMBOLS];
    public fixed ushort tmp_bits[64];

    /// <summary><c>HuffmanTree tmp_tree[2 * BROTLI_NUM_LITERAL_SYMBOLS + 1]</c>.</summary>
    public HuffmanTree* tmp_tree
    {
        get
        {
            fixed (ulong* p = tmp_tree_)
            {
                return (HuffmanTree*)p;
            }
        }
    }
}

internal static unsafe class CompressFragmentTwoPass
{
    /* TODO(eustas): turn to macro. (C keeps it a static const.) */
    internal const nuint kCompressFragmentTwoPassBlockSize = 1 << 17;

    private const long MAX_DISTANCE = (1L << 18) - WindowGap;  /* BROTLI_MAX_BACKWARD_LIMIT(18) */

    private const uint kHashMul32 = 0x1E35A7BD;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Hash(byte* p, nuint shift, nuint length)
    {
        ulong h =
            (BROTLI_UNALIGNED_LOAD64LE(p) << (int)((8 - length) * 8)) * kHashMul32;
        return (uint)(h >> (int)shift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashBytesAtOffset(ulong v, nuint offset,
        nuint shift, nuint length)
    {
        ulong h = ((v >> (int)(8 * offset)) << (int)((8 - length) * 8)) * kHashMul32;
        return (uint)(h >> (int)shift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMatch(byte* p1, byte* p2, nuint length)
    {
        if (BROTLI_UNALIGNED_LOAD32LE(p1) == BROTLI_UNALIGNED_LOAD32LE(p2))
        {
            if (length == 4) return true;
            return p1[4] == p2[4] && p1[5] == p2[5];
        }
        return false;
    }

    /* Builds a command and distance prefix code (each 64 symbols) into "depth" and
       "bits" based on "histogram" and stores it into the bit stream. */
    private static void BuildAndStoreCommandPrefixCode(BrotliTwoPassArena* s,
                                                       nuint* storage_ix,
                                                       byte* storage)
    {
        /* Tree size for building a tree over 64 symbols is 2 * 64 + 1. */
        new Span<byte>(s->tmp_depth, BROTLI_NUM_COMMAND_SYMBOLS).Clear();
        BrotliCreateHuffmanTree(s->cmd_histo, 64, 15, s->tmp_tree, s->cmd_depth);
        BrotliCreateHuffmanTree(&s->cmd_histo[64], 64, 14, s->tmp_tree,
                                &s->cmd_depth[64]);
        /* We have to jump through a few hoops here in order to compute
           the command bits because the symbols are in a different order than in
           the full alphabet. This looks complicated, but having the symbols
           in this order in the command bits saves a few branches in the Emit*
           functions. */
        Buffer.MemoryCopy(s->cmd_depth + 24, s->tmp_depth, 24, 24);
        Buffer.MemoryCopy(s->cmd_depth, s->tmp_depth + 24, 8, 8);
        Buffer.MemoryCopy(s->cmd_depth + 48, s->tmp_depth + 32, 8, 8);
        Buffer.MemoryCopy(s->cmd_depth + 8, s->tmp_depth + 40, 8, 8);
        Buffer.MemoryCopy(s->cmd_depth + 56, s->tmp_depth + 48, 8, 8);
        Buffer.MemoryCopy(s->cmd_depth + 16, s->tmp_depth + 56, 8, 8);
        BrotliConvertBitDepthsToSymbols(s->tmp_depth, 64, s->tmp_bits);
        Buffer.MemoryCopy(s->tmp_bits + 24, s->cmd_bits, 16, 16);
        Buffer.MemoryCopy(s->tmp_bits + 40, s->cmd_bits + 8, 16, 16);
        Buffer.MemoryCopy(s->tmp_bits + 56, s->cmd_bits + 16, 16, 16);
        Buffer.MemoryCopy(s->tmp_bits, s->cmd_bits + 24, 48, 48);
        Buffer.MemoryCopy(s->tmp_bits + 32, s->cmd_bits + 48, 16, 16);
        Buffer.MemoryCopy(s->tmp_bits + 48, s->cmd_bits + 56, 16, 16);
        BrotliConvertBitDepthsToSymbols(&s->cmd_depth[64], 64, &s->cmd_bits[64]);
        {
            /* Create the bit length array for the full command alphabet. */
            nuint i;
            new Span<byte>(s->tmp_depth, 64).Clear();  /* only 64 first values were used */
            Buffer.MemoryCopy(s->cmd_depth + 24, s->tmp_depth, 8, 8);
            Buffer.MemoryCopy(s->cmd_depth + 32, s->tmp_depth + 64, 8, 8);
            Buffer.MemoryCopy(s->cmd_depth + 40, s->tmp_depth + 128, 8, 8);
            Buffer.MemoryCopy(s->cmd_depth + 48, s->tmp_depth + 192, 8, 8);
            Buffer.MemoryCopy(s->cmd_depth + 56, s->tmp_depth + 384, 8, 8);
            for (i = 0; i < 8; ++i)
            {
                s->tmp_depth[128 + 8 * i] = s->cmd_depth[i];
                s->tmp_depth[256 + 8 * i] = s->cmd_depth[8 + i];
                s->tmp_depth[448 + 8 * i] = s->cmd_depth[16 + i];
            }
            BrotliStoreHuffmanTree(s->tmp_depth, BROTLI_NUM_COMMAND_SYMBOLS,
                                   s->tmp_tree, storage_ix, storage);
        }
        BrotliStoreHuffmanTree(&s->cmd_depth[64], 64, s->tmp_tree, storage_ix,
                               storage);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitInsertLen(uint insertlen, uint** commands)
    {
        if (insertlen < 6)
        {
            **commands = insertlen;
        }
        else if (insertlen < 130)
        {
            uint tail = insertlen - 2;
            uint nbits = Log2FloorNonZero(tail) - 1u;
            uint prefix = tail >> (int)nbits;
            uint inscode = (nbits << 1) + prefix + 2;
            uint extra = tail - (prefix << (int)nbits);
            **commands = inscode | (extra << 8);
        }
        else if (insertlen < 2114)
        {
            uint tail = insertlen - 66;
            uint nbits = Log2FloorNonZero(tail);
            uint code = nbits + 10;
            uint extra = tail - (1u << (int)nbits);
            **commands = code | (extra << 8);
        }
        else if (insertlen < 6210)
        {
            uint extra = insertlen - 2114;
            **commands = 21 | (extra << 8);
        }
        else if (insertlen < 22594)
        {
            uint extra = insertlen - 6210;
            **commands = 22 | (extra << 8);
        }
        else
        {
            uint extra = insertlen - 22594;
            **commands = 23 | (extra << 8);
        }
        ++(*commands);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitCopyLen(nuint copylen, uint** commands)
    {
        if (copylen < 10)
        {
            **commands = (uint)(copylen + 38);
        }
        else if (copylen < 134)
        {
            nuint tail = copylen - 6;
            nuint nbits = Log2FloorNonZero(tail) - 1;
            nuint prefix = tail >> (int)nbits;
            nuint code = (nbits << 1) + prefix + 44;
            nuint extra = tail - (prefix << (int)nbits);
            **commands = (uint)(code | (extra << 8));
        }
        else if (copylen < 2118)
        {
            nuint tail = copylen - 70;
            nuint nbits = Log2FloorNonZero(tail);
            nuint code = nbits + 52;
            nuint extra = tail - ((nuint)1 << (int)nbits);
            **commands = (uint)(code | (extra << 8));
        }
        else
        {
            nuint extra = copylen - 2118;
            **commands = (uint)(63 | (extra << 8));
        }
        ++(*commands);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitCopyLenLastDistance(nuint copylen, uint** commands)
    {
        if (copylen < 12)
        {
            **commands = (uint)(copylen + 20);
            ++(*commands);
        }
        else if (copylen < 72)
        {
            nuint tail = copylen - 8;
            nuint nbits = Log2FloorNonZero(tail) - 1;
            nuint prefix = tail >> (int)nbits;
            nuint code = (nbits << 1) + prefix + 28;
            nuint extra = tail - (prefix << (int)nbits);
            **commands = (uint)(code | (extra << 8));
            ++(*commands);
        }
        else if (copylen < 136)
        {
            nuint tail = copylen - 8;
            nuint code = (tail >> 5) + 54;
            nuint extra = tail & 31;
            **commands = (uint)(code | (extra << 8));
            ++(*commands);
            **commands = 64;
            ++(*commands);
        }
        else if (copylen < 2120)
        {
            nuint tail = copylen - 72;
            nuint nbits = Log2FloorNonZero(tail);
            nuint code = nbits + 52;
            nuint extra = tail - ((nuint)1 << (int)nbits);
            **commands = (uint)(code | (extra << 8));
            ++(*commands);
            **commands = 64;
            ++(*commands);
        }
        else
        {
            nuint extra = copylen - 2120;
            **commands = (uint)(63 | (extra << 8));
            ++(*commands);
            **commands = 64;
            ++(*commands);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitDistance(uint distance, uint** commands)
    {
        uint d = distance + 3;
        uint nbits = Log2FloorNonZero(d) - 1;
        uint prefix = (d >> (int)nbits) & 1;
        uint offset = (2 + prefix) << (int)nbits;
        uint distcode = 2 * (nbits - 1) + prefix + 80;
        uint extra = d - offset;
        **commands = distcode | (extra << 8);
        ++(*commands);
    }

    /* REQUIRES: len <= 1 << 24. */
    private static void BrotliStoreMetaBlockHeader(
        nuint len, int is_uncompressed, nuint* storage_ix,
        byte* storage)
    {
        nuint nibbles = 6;
        /* ISLAST */
        BrotliWriteBits(1, 0, storage_ix, storage);
        if (len <= (1U << 16))
        {
            nibbles = 4;
        }
        else if (len <= (1U << 20))
        {
            nibbles = 5;
        }
        BrotliWriteBits(2, nibbles - 4, storage_ix, storage);
        BrotliWriteBits(nibbles * 4, len - 1, storage_ix, storage);
        /* ISUNCOMPRESSED */
        BrotliWriteBits(1, (ulong)is_uncompressed, storage_ix, storage);
    }

    private static void CreateCommands(byte* input,
        nuint block_size, nuint input_size, byte* base_ip, int* table,
        nuint table_bits, nuint min_match,
        byte** literals, uint** commands)
    {
        /* "ip" is the input pointer. */
        byte* ip = input;
        nuint shift = 64u - table_bits;
        byte* ip_end = input + block_size;
        /* "next_emit" is a pointer to the first byte that is not covered by a
           previous copy. Bytes between "next_emit" and the start of the next copy or
           the end of the input will be emitted as literal bytes. */
        byte* next_emit = input;

        int last_distance = -1;
        nuint kInputMarginBytes = WindowGap;

        if (block_size >= kInputMarginBytes)
        {
            /* For the last block, we need to keep a 16 bytes margin so that we can be
               sure that all distances are at most window size - 16.
               For all other blocks, we only need to keep a margin of 5 bytes so that
               we don't go over the block size with a copy. */
            nuint len_limit = BROTLI_MIN(block_size - min_match,
                                       input_size - kInputMarginBytes);
            byte* ip_limit = input + len_limit;

            uint next_hash;
            for (next_hash = Hash(++ip, shift, min_match); ;)
            {
                /* Step 1: Scan forward in the input looking for a 6-byte-long match.
                   If we get close to exhausting the input then goto emit_remainder.

                   Heuristic match skipping: If 32 bytes are scanned with no matches
                   found, start looking only at every other byte. If 32 more bytes are
                   scanned, look at every third byte, etc.. When a match is found,
                   immediately go back to looking at every byte. This is a small loss
                   (~5% performance, ~0.1% density) for compressible data due to more
                   bookkeeping, but for non-compressible data (such as JPEG) it's a huge
                   win since the compressor quickly "realizes" the data is incompressible
                   and doesn't bother looking for matches everywhere.

                   The "skip" variable keeps track of how many bytes there are since the
                   last match; dividing it by 32 (ie. right-shifting by five) gives the
                   number of bytes to move ahead for each iteration. */
                uint skip = 32;

                byte* next_ip = ip;
                byte* candidate;

            trawl:
                do
                {
                    uint hash = next_hash;
                    uint bytes_between_hash_lookups = skip++ >> 5;
                    ip = next_ip;
                    next_ip = ip + bytes_between_hash_lookups;
                    if (next_ip > ip_limit)
                    {
                        goto emit_remainder;
                    }
                    next_hash = Hash(next_ip, shift, min_match);
                    candidate = ip - last_distance;
                    if (IsMatch(ip, candidate, min_match))
                    {
                        if (candidate < ip)
                        {
                            table[hash] = (int)(ip - base_ip);
                            break;
                        }
                    }
                    candidate = base_ip + table[hash];

                    table[hash] = (int)(ip - base_ip);
                } while (!IsMatch(ip, candidate, min_match));

                /* Check copy distance. If candidate is not feasible, continue search.
                   Checking is done outside of hot loop to reduce overhead. */
                if (ip - candidate > MAX_DISTANCE) goto trawl;

                /* Step 2: Emit the found match together with the literal bytes from
                   "next_emit", and then see if we can find a next match immediately
                   afterwards. Repeat until we find no match for the input
                   without emitting some literal bytes. */

                {
                    /* We have a 6-byte match at ip, and we need to emit bytes in
                       [next_emit, ip). */
                    byte* @base = ip;
                    nuint matched = min_match + FindMatchLengthWithLimit(
                        candidate + min_match, ip + min_match,
                        (nuint)(ip_end - ip) - min_match);
                    int distance = (int)(@base - candidate);  /* > 0 */
                    int insert = (int)(@base - next_emit);
                    ip += matched;
                    EmitInsertLen((uint)insert, commands);
                    Buffer.MemoryCopy(next_emit, *literals, (nuint)insert, (nuint)insert);
                    *literals += insert;
                    if (distance == last_distance)
                    {
                        **commands = 64;
                        ++(*commands);
                    }
                    else
                    {
                        EmitDistance((uint)distance, commands);
                        last_distance = distance;
                    }
                    EmitCopyLenLastDistance(matched, commands);

                    next_emit = ip;
                    if (ip >= ip_limit)
                    {
                        goto emit_remainder;
                    }
                    {
                        /* We could immediately start working at ip now, but to improve
                           compression we first update "table" with the hashes of some
                           positions within the last copy. */
                        ulong input_bytes;
                        uint cur_hash;
                        uint prev_hash;
                        if (min_match == 4)
                        {
                            input_bytes = BROTLI_UNALIGNED_LOAD64LE(ip - 3);
                            cur_hash = HashBytesAtOffset(input_bytes, 3, shift, min_match);
                            prev_hash = HashBytesAtOffset(input_bytes, 0, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 3);
                            prev_hash = HashBytesAtOffset(input_bytes, 1, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 2);
                            /* C quirk: offset 0 (not 2) is hashed for position -1 here. */
                            prev_hash = HashBytesAtOffset(input_bytes, 0, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 1);
                        }
                        else
                        {
                            input_bytes = BROTLI_UNALIGNED_LOAD64LE(ip - 5);
                            prev_hash = HashBytesAtOffset(input_bytes, 0, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 5);
                            prev_hash = HashBytesAtOffset(input_bytes, 1, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 4);
                            prev_hash = HashBytesAtOffset(input_bytes, 2, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 3);
                            input_bytes = BROTLI_UNALIGNED_LOAD64LE(ip - 2);
                            cur_hash = HashBytesAtOffset(input_bytes, 2, shift, min_match);
                            prev_hash = HashBytesAtOffset(input_bytes, 0, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 2);
                            prev_hash = HashBytesAtOffset(input_bytes, 1, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 1);
                        }

                        candidate = base_ip + table[cur_hash];
                        table[cur_hash] = (int)(ip - base_ip);
                    }
                }

                while (ip - candidate <= MAX_DISTANCE &&
                    IsMatch(ip, candidate, min_match))
                {
                    /* We have a 6-byte match at ip, and no need to emit any
                       literal bytes prior to ip. */
                    byte* @base = ip;
                    nuint matched = min_match + FindMatchLengthWithLimit(
                        candidate + min_match, ip + min_match,
                        (nuint)(ip_end - ip) - min_match);
                    ip += matched;
                    last_distance = (int)(@base - candidate);  /* > 0 */
                    EmitCopyLen(matched, commands);
                    EmitDistance((uint)last_distance, commands);

                    next_emit = ip;
                    if (ip >= ip_limit)
                    {
                        goto emit_remainder;
                    }
                    {
                        /* We could immediately start working at ip now, but to improve
                           compression we first update "table" with the hashes of some
                           positions within the last copy. */
                        ulong input_bytes;
                        uint cur_hash;
                        uint prev_hash;
                        if (min_match == 4)
                        {
                            input_bytes = BROTLI_UNALIGNED_LOAD64LE(ip - 3);
                            cur_hash = HashBytesAtOffset(input_bytes, 3, shift, min_match);
                            prev_hash = HashBytesAtOffset(input_bytes, 0, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 3);
                            prev_hash = HashBytesAtOffset(input_bytes, 1, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 2);
                            prev_hash = HashBytesAtOffset(input_bytes, 2, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 1);
                        }
                        else
                        {
                            input_bytes = BROTLI_UNALIGNED_LOAD64LE(ip - 5);
                            prev_hash = HashBytesAtOffset(input_bytes, 0, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 5);
                            prev_hash = HashBytesAtOffset(input_bytes, 1, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 4);
                            prev_hash = HashBytesAtOffset(input_bytes, 2, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 3);
                            input_bytes = BROTLI_UNALIGNED_LOAD64LE(ip - 2);
                            cur_hash = HashBytesAtOffset(input_bytes, 2, shift, min_match);
                            prev_hash = HashBytesAtOffset(input_bytes, 0, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 2);
                            prev_hash = HashBytesAtOffset(input_bytes, 1, shift, min_match);
                            table[prev_hash] = (int)(ip - base_ip - 1);
                        }

                        candidate = base_ip + table[cur_hash];
                        table[cur_hash] = (int)(ip - base_ip);
                    }
                }

                next_hash = Hash(++ip, shift, min_match);
            }
        }

    emit_remainder:
        /* Emit the remaining bytes as literals. */
        if (next_emit < ip_end)
        {
            uint insert = (uint)(ip_end - next_emit);
            EmitInsertLen(insert, commands);
            Buffer.MemoryCopy(next_emit, *literals, insert, insert);
            *literals += insert;
        }
    }

    private static readonly uint[] kNumExtraBits =  /* [128] */
    {
        0,  0,  0,  0,  0,  0,  1,  1,  2,  2,  3,  3,  4,  4,  5,  5,
        6,  7,  8,  9,  10, 12, 14, 24, 0,  0,  0,  0,  0,  0,  0,  0,
        1,  1,  2,  2,  3,  3,  4,  4,  0,  0,  0,  0,  0,  0,  0,  0,
        1,  1,  2,  2,  3,  3,  4,  4,  5,  5,  6,  7,  8,  9,  10, 24,
        0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
        1,  1,  2,  2,  3,  3,  4,  4,  5,  5,  6,  6,  7,  7,  8,  8,
        9,  9,  10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16,
        17, 17, 18, 18, 19, 19, 20, 20, 21, 21, 22, 22, 23, 23, 24, 24,
    };
    private static readonly uint[] kInsertOffset =  /* [24] */
    {
        0,  1,  2,  3,  4,   5,   6,   8,   10,   14,   18,   26,
        34, 50, 66, 98, 130, 194, 322, 578, 1090, 2114, 6210, 22594,
    };

    private static void StoreCommands(BrotliTwoPassArena* s,
                                      byte* literals, nuint num_literals,
                                      uint* commands, nuint num_commands,
                                      nuint* storage_ix, byte* storage)
    {
        nuint i;
        new Span<uint>(s->lit_histo, 256).Clear();
        /* TODO(eustas): is that necessary? */
        new Span<byte>(s->cmd_depth, 128).Clear();
        /* TODO(eustas): is that necessary? */
        new Span<ushort>(s->cmd_bits, 128).Clear();
        new Span<uint>(s->cmd_histo, 128).Clear();
        for (i = 0; i < num_literals; ++i)
        {
            ++s->lit_histo[literals[i]];
        }
        BrotliBuildAndStoreHuffmanTreeFast(s->tmp_tree, s->lit_histo, num_literals,
                                           /* max_bits = */ 8, s->lit_depth,
                                           s->lit_bits, storage_ix, storage);

        for (i = 0; i < num_commands; ++i)
        {
            uint code = commands[i] & 0xFF;
            ++s->cmd_histo[code];
        }
        s->cmd_histo[1] += 1;
        s->cmd_histo[2] += 1;
        s->cmd_histo[64] += 1;
        s->cmd_histo[84] += 1;
        BuildAndStoreCommandPrefixCode(s, storage_ix, storage);

        for (i = 0; i < num_commands; ++i)
        {
            uint cmd = commands[i];
            uint code = cmd & 0xFF;
            uint extra = cmd >> 8;
            BrotliWriteBits(s->cmd_depth[code], s->cmd_bits[code], storage_ix, storage);
            BrotliWriteBits(kNumExtraBits[code], extra, storage_ix, storage);
            if (code < 24)
            {
                uint insert = kInsertOffset[code] + extra;
                uint j;
                for (j = 0; j < insert; ++j)
                {
                    byte lit = *literals;
                    BrotliWriteBits(s->lit_depth[lit], s->lit_bits[lit], storage_ix,
                                    storage);
                    ++literals;
                }
            }
        }
    }

    /* Acceptable loss for uncompressible speedup is 2% */
    private const double MIN_RATIO = 0.98;
    private const nuint SAMPLE_RATE = 43;

    private static bool ShouldCompress(BrotliTwoPassArena* s,
        byte* input, nuint input_size, nuint num_literals)
    {
        double corpus_size = (double)input_size;
        if ((double)num_literals < MIN_RATIO * corpus_size)
        {
            return true;
        }
        else
        {
            double max_total_bit_cost = corpus_size * 8 * MIN_RATIO / SAMPLE_RATE;
            nuint i;
            new Span<uint>(s->lit_histo, 256).Clear();
            for (i = 0; i < input_size; i += SAMPLE_RATE)
            {
                ++s->lit_histo[input[i]];
            }
            return BitsEntropy(s->lit_histo, 256) < max_total_bit_cost;
        }
    }

    private static void RewindBitPosition(nuint new_storage_ix,
                                          nuint* storage_ix, byte* storage)
    {
        nuint bitpos = new_storage_ix & 7;
        nuint mask = (1u << (int)bitpos) - 1;
        storage[new_storage_ix >> 3] &= (byte)mask;
        *storage_ix = new_storage_ix;
    }

    private static void EmitUncompressedMetaBlock(byte* input, nuint input_size,
                                                  nuint* storage_ix, byte* storage)
    {
        BrotliStoreMetaBlockHeader(input_size, 1, storage_ix, storage);
        *storage_ix = (*storage_ix + 7u) & ~(nuint)7u;
        Buffer.MemoryCopy(input, &storage[*storage_ix >> 3], input_size, input_size);
        *storage_ix += input_size << 3;
        storage[*storage_ix >> 3] = 0;
    }

    private static void BrotliCompressFragmentTwoPassImpl(
        BrotliTwoPassArena* s, byte* input, nuint input_size,
        int is_last, uint* command_buf, byte* literal_buf,
        int* table, nuint table_bits, nuint min_match,
        nuint* storage_ix, byte* storage)
    {
        /* Save the start of the first block for position and distance computations.
        */
        byte* base_ip = input;
        /* BROTLI_UNUSED(is_last); */

        while (input_size > 0)
        {
            nuint block_size =
                BROTLI_MIN(input_size, kCompressFragmentTwoPassBlockSize);
            uint* commands = command_buf;
            byte* literals = literal_buf;
            nuint num_literals;
            CreateCommands(input, block_size, input_size, base_ip, table,
                           table_bits, min_match, &literals, &commands);
            num_literals = (nuint)(literals - literal_buf);
            if (ShouldCompress(s, input, block_size, num_literals))
            {
                nuint num_commands = (nuint)(commands - command_buf);
                BrotliStoreMetaBlockHeader(block_size, 0, storage_ix, storage);
                /* No block splits, no contexts. */
                BrotliWriteBits(13, 0, storage_ix, storage);
                StoreCommands(s, literal_buf, num_literals, command_buf, num_commands,
                              storage_ix, storage);
            }
            else
            {
                /* Since we did not find many backward references and the entropy of
                   the data is close to 8 bits, we can simply emit an uncompressed block.
                   This makes compression speed of uncompressible data about 3x faster. */
                EmitUncompressedMetaBlock(input, block_size, storage_ix, storage);
            }
            input += block_size;
            input_size -= block_size;
        }
    }

    /* FOR_TABLE_BITS_(X): X(8) X(9) X(10) X(11) X(12) X(13) X(14) X(15) X(16) X(17)
       min_match = (B <= 15) ? 4 : 6 */
    internal static void BrotliCompressFragmentTwoPass(
        BrotliTwoPassArena* s, byte* input, nuint input_size,
        int is_last, uint* command_buf, byte* literal_buf,
        int* table, nuint table_size, nuint* storage_ix, byte* storage)
    {
        nuint initial_storage_ix = *storage_ix;
        nuint table_bits = Log2FloorNonZero(table_size);
        switch (table_bits)
        {
            case 8:
            case 9:
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
                BrotliCompressFragmentTwoPassImpl(s, input, input_size, is_last,
                    command_buf, literal_buf, table, table_bits, /* min_match */ 4,
                    storage_ix, storage);
                break;
            case 16:
            case 17:
                BrotliCompressFragmentTwoPassImpl(s, input, input_size, is_last,
                    command_buf, literal_buf, table, table_bits, /* min_match */ 6,
                    storage_ix, storage);
                break;
            default: break;  /* BROTLI_DCHECK(0) */
        }

        /* If output is larger than single uncompressed block, rewrite it. */
        if (*storage_ix - initial_storage_ix > 31 + (input_size << 3))
        {
            RewindBitPosition(initial_storage_ix, storage_ix, storage);
            EmitUncompressedMetaBlock(input, input_size, storage_ix, storage);
        }

        if (is_last != 0)
        {
            BrotliWriteBits(1, 1, storage_ix, storage);  /* islast */
            BrotliWriteBits(1, 1, storage_ix, storage);  /* isempty */
            *storage_ix = (*storage_ix + 7u) & ~(nuint)7u;
        }
    }
}
