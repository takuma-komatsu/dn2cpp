using System.Runtime.InteropServices;
using DnZlib.Internal;

namespace DnZlib.Inflate;

/// <summary>Type of canonical Huffman code being built (mirrors zlib's <c>codetype</c>).</summary>
internal enum CodeType
{
    Codes = 0,
    Lens = 1,
    Dists = 2,
}

/// <summary>
/// Builds the two-level Huffman decode tables used by inflate — a faithful port of zlib's
/// <c>inflate_table</c> (inftrees.c). Also produces and caches the fixed-code tables.
/// </summary>
internal static unsafe class InfTrees
{
    private const int MaxBits = 15;
    private const int EnoughLens = 852;
    private const int EnoughDists = 592;

    private static readonly ushort[] LBase =
    [
        3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
        35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258, 0, 0,
    ];

    private static readonly ushort[] LExt =
    [
        16, 16, 16, 16, 16, 16, 16, 16, 17, 17, 17, 17, 18, 18, 18, 18,
        19, 19, 19, 19, 20, 20, 20, 20, 21, 21, 21, 21, 16, 199, 75,
    ];

    private static readonly ushort[] DBase =
    [
        1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
        257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
        8193, 12289, 16385, 24577, 0, 0,
    ];

    private static readonly ushort[] DExt =
    [
        16, 16, 16, 16, 17, 17, 18, 18, 19, 19, 20, 20, 21, 21, 22, 22,
        23, 23, 24, 24, 25, 25, 26, 26, 27, 27,
        28, 28, 29, 29, 64, 64,
    ];

    private static readonly Lock FixedLock = new();
    private static Code* _lenFix;
    private static Code* _distFix;
    private static bool _fixedBuilt;

    /// <summary>
    /// Build a decode table for the canonical Huffman code described by <paramref name="lens"/>.
    /// Returns 0 on success, -1 for an invalid (over-subscribed/incomplete) code, and +1 if the
    /// table would exceed ENOUGH. <paramref name="table"/> is advanced past the entries used and
    /// <paramref name="bits"/> receives the actual root-table bit count.
    /// </summary>
    internal static int BuildTable(CodeType type, ushort* lens, uint codes,
                                   ref Code* table, ref uint bits, ushort* work)
    {
        uint len, sym, min, max, root, curr, drop, used, huff, incr, fill, low, mask;
        int left;

        ushort* count = stackalloc ushort[MaxBits + 1];
        ushort* offs = stackalloc ushort[MaxBits + 1];

        // Count codes of each length.
        for (len = 0; len <= MaxBits; len++)
            count[len] = 0;
        for (sym = 0; sym < codes; sym++)
            count[lens[sym]]++;

        // Bound code lengths and force root within range.
        root = bits;
        for (max = MaxBits; max >= 1; max--)
            if (count[max] != 0)
                break;
        if (root > max)
            root = max;
        if (max == 0)
        {
            // No symbols: emit an invalid-code table so decoding reports the error.
            Code invalid = new() { Op = 64, Bits = 1, Val = 0 };
            table[0] = invalid;
            table[1] = invalid;
            table += 2;
            bits = 1;
            return 0;
        }
        for (min = 1; min < max; min++)
            if (count[min] != 0)
                break;
        if (root < min)
            root = min;

        // Check for over-subscribed or incomplete set.
        left = 1;
        for (len = 1; len <= MaxBits; len++)
        {
            left <<= 1;
            left -= count[len];
            if (left < 0)
                return -1;
        }
        if (left > 0 && (type == CodeType.Codes || max != 1))
            return -1;

        // Sort symbols by length, then by symbol order within each length.
        offs[1] = 0;
        for (len = 1; len < MaxBits; len++)
            offs[len + 1] = (ushort)(offs[len] + count[len]);
        for (sym = 0; sym < codes; sym++)
            if (lens[sym] != 0)
                work[offs[lens[sym]]++] = (ushort)sym;

        // Set up base/extra tables and the symbol threshold for this code type.
        ReadOnlySpan<ushort> baseTab = default;
        ReadOnlySpan<ushort> extraTab = default;
        uint match;
        switch (type)
        {
            case CodeType.Lens:
                baseTab = LBase;
                extraTab = LExt;
                match = 257;
                break;
            case CodeType.Dists:
                baseTab = DBase;
                extraTab = DExt;
                match = 0;
                break;
            default: // CODES
                match = 20;
                break;
        }

        // Initialize state for the fill loop.
        huff = 0;
        sym = 0;
        len = min;
        Code* next = table;
        Code* rootBase = table;
        curr = root;
        drop = 0;
        low = uint.MaxValue;
        used = 1u << (int)root;
        mask = used - 1;

        if ((type == CodeType.Lens && used > EnoughLens) ||
            (type == CodeType.Dists && used > EnoughDists))
            return 1;

        for (;;)
        {
            // Create the table entry for this symbol.
            uint w = work[sym];
            byte op;
            ushort val;
            if (w + 1u < match)
            {
                op = 0;
                val = (ushort)w;
            }
            else if (w >= match)
            {
                op = (byte)extraTab[(int)(w - match)];
                val = baseTab[(int)(w - match)];
            }
            else
            {
                op = 32 + 64; // end of block
                val = 0;
            }
            Code here = new() { Op = op, Bits = (byte)(len - drop), Val = val };

            // Replicate the entry for all indices with matching low bits.
            incr = 1u << (int)(len - drop);
            fill = 1u << (int)curr;
            min = fill; // save offset to next table
            do
            {
                fill -= incr;
                next[(huff >> (int)drop) + fill] = here;
            }
            while (fill != 0);

            // Backwards-increment the len-bit code huff.
            incr = 1u << (int)(len - 1);
            while ((huff & incr) != 0)
                incr >>= 1;
            if (incr != 0)
            {
                huff &= incr - 1;
                huff += incr;
            }
            else
            {
                huff = 0;
            }

            // Advance symbol; update count and current length.
            sym++;
            if (--count[len] == 0)
            {
                if (len == max)
                    break;
                len = lens[work[sym]];
            }

            // Create a new sub-table when needed.
            if (len > root && (huff & mask) != low)
            {
                if (drop == 0)
                    drop = root;

                next += min; // min is 1 << curr here

                curr = len - drop;
                left = (int)(1u << (int)curr);
                while (curr + drop < max)
                {
                    left -= count[curr + drop];
                    if (left <= 0)
                        break;
                    curr++;
                    left <<= 1;
                }

                used += 1u << (int)curr;
                if ((type == CodeType.Lens && used > EnoughLens) ||
                    (type == CodeType.Dists && used > EnoughDists))
                    return 1;

                low = huff & mask;
                rootBase[low].Op = (byte)curr;
                rootBase[low].Bits = (byte)root;
                rootBase[low].Val = (ushort)(next - rootBase);
            }
        }

        // Fill the remaining single entry for an incomplete code.
        if (huff != 0)
        {
            next[huff] = new Code { Op = 64, Bits = (byte)(len - drop), Val = 0 };
        }

        table = rootBase + used;
        bits = root;
        return 0;
    }

    /// <summary>Return the fixed-code decode tables (built once, process-lifetime native memory).</summary>
    internal static void GetFixedTables(out Code* lenCode, out uint lenBits, out Code* distCode, out uint distBits)
    {
        if (!Volatile.Read(ref _fixedBuilt))
            BuildFixed();
        lenCode = _lenFix;
        lenBits = 9;
        distCode = _distFix;
        distBits = 5;
    }

    private static void BuildFixed()
    {
        lock (FixedLock)
        {
            if (_fixedBuilt)
                return;

            // 512 (lit/len root) + 32 (dist root) = 544 entries, never freed.
            Code* mem = (Code*)NativeMemory.Alloc(544, (nuint)sizeof(Code));
            ushort* lens = stackalloc ushort[288];
            ushort* work = stackalloc ushort[288];

            uint sym = 0;
            while (sym < 144) lens[sym++] = 8;
            while (sym < 256) lens[sym++] = 9;
            while (sym < 280) lens[sym++] = 7;
            while (sym < 288) lens[sym++] = 8;

            Code* next = mem;
            Code* lenfix = next;
            uint bits = 9;
            BuildTable(CodeType.Lens, lens, 288, ref next, ref bits, work);

            sym = 0;
            while (sym < 32) lens[sym++] = 5;
            Code* distfix = next;
            bits = 5;
            BuildTable(CodeType.Dists, lens, 32, ref next, ref bits, work);

            _lenFix = lenfix;
            _distFix = distfix;
            Volatile.Write(ref _fixedBuilt, true);
        }
    }
}
