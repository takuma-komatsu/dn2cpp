using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DnZlib;
using static DnZlib.Deflate.DeflateConstants;

namespace DnZlib.Deflate;

/// <summary>Huffman tree construction and block emission — a faithful port of zlib's trees.c.</summary>
internal static unsafe class Trees
{
    private const int Smallest = 1;

    private static readonly StaticTreeDesc StaticLDesc =
        new(TreesTables.StaticLtree, TreesTables.ExtraLbits, Literals + 1, LCodes, MaxBits);
    private static readonly StaticTreeDesc StaticDDesc =
        new(TreesTables.StaticDtree, TreesTables.ExtraDbits, 0, DCodes, MaxBits);
    private static readonly StaticTreeDesc StaticBlDesc =
        new(null, TreesTables.ExtraBlbits, 0, BlCodes, MaxBlBits);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int DCode(uint dist) =>
        dist < 256 ? TreesTables.DistCode[dist] : TreesTables.DistCode[256 + (dist >> 7)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Smaller(CtData* tree, int n, int m, byte* depth) =>
        tree[n].Freq < tree[m].Freq || (tree[n].Freq == tree[m].Freq && depth[n] <= depth[m]);

    public static void TrInit(DeflateState* s)
    {
        s->LDesc.DynTree = s->DynLtree;
        s->LDesc.Stat = StaticLDesc;
        s->DDesc.DynTree = s->DynDtree;
        s->DDesc.Stat = StaticDDesc;
        s->BlDesc.DynTree = s->BlTree;
        s->BlDesc.Stat = StaticBlDesc;

        s->BiBuf = 0;
        s->BiValid = 0;
        s->BiUsed = 0;
        InitBlock(s);
    }

    private static void InitBlock(DeflateState* s)
    {
        // Bulk-zeros Fc+Dl together (not just Freq/Fc as a byte-at-a-time loop would): BuildTree
        // always rewrites Len (Dl's alias) for every index in [0, elems) on its very next call —
        // via the `tree[n].Len = 0` branch for freq==0 leaves, or via GenBitlen for freq!=0 leaves
        // — before any code reads Len/Dad again, so the extra zeroed bytes are dead until then.
        NativeMemory.Clear(s->DynLtree, (nuint)LCodes * (nuint)sizeof(CtData));
        NativeMemory.Clear(s->DynDtree, (nuint)DCodes * (nuint)sizeof(CtData));
        NativeMemory.Clear(s->BlTree, (nuint)BlCodes * (nuint)sizeof(CtData));
        s->DynLtree[EndBlock].Freq = 1;
        s->OptLen = s->StaticLen = 0;
        s->SymNext = s->Matches = 0;
    }

    private static void Pqdownheap(DeflateState* s, CtData* tree, int k)
    {
        int v = s->Heap[k];
        int j = k << 1;
        while (j <= s->HeapLen)
        {
            if (j < s->HeapLen && Smaller(tree, s->Heap[j + 1], s->Heap[j], s->Depth))
                j++;
            if (Smaller(tree, v, s->Heap[j], s->Depth))
                break;
            s->Heap[k] = s->Heap[j];
            k = j;
            j <<= 1;
        }
        s->Heap[k] = v;
    }

    private static void GenBitlen(DeflateState* s, ref TreeDesc desc)
    {
        CtData* tree = desc.DynTree;
        int maxCode = desc.MaxCode;
        CtData* stree = desc.Stat.StaticTree;
        int* extra = desc.Stat.ExtraBits;
        int baseIdx = desc.Stat.ExtraBase;
        int maxLength = desc.Stat.MaxLength;
        int h, n, m, bits, xbits;
        ushort f;
        int overflow = 0;

        for (bits = 0; bits <= MaxBits; bits++)
            s->BlCount[bits] = 0;

        tree[s->Heap[s->HeapMax]].Len = 0; // root of the heap

        for (h = s->HeapMax + 1; h < HeapSize; h++)
        {
            n = s->Heap[h];
            bits = tree[tree[n].Dad].Len + 1;
            if (bits > maxLength) { bits = maxLength; overflow++; }
            tree[n].Len = (ushort)bits;
            if (n > maxCode)
                continue;
            s->BlCount[bits]++;
            xbits = 0;
            if (n >= baseIdx)
                xbits = extra[n - baseIdx];
            f = tree[n].Freq;
            s->OptLen += (ulong)f * (uint)(bits + xbits);
            if (stree != null)
                s->StaticLen += (ulong)f * (uint)(stree[n].Len + xbits);
        }
        if (overflow == 0)
            return;

        do
        {
            bits = maxLength - 1;
            while (s->BlCount[bits] == 0) bits--;
            s->BlCount[bits]--;
            s->BlCount[bits + 1] += 2;
            s->BlCount[maxLength]--;
            overflow -= 2;
        }
        while (overflow > 0);

        for (bits = maxLength; bits != 0; bits--)
        {
            n = s->BlCount[bits];
            while (n != 0)
            {
                m = s->Heap[--h];
                if (m > maxCode)
                    continue;
                if (tree[m].Len != bits)
                {
                    s->OptLen += ((ulong)bits - tree[m].Len) * tree[m].Freq;
                    tree[m].Len = (ushort)bits;
                }
                n--;
            }
        }
    }

    private static void BuildTree(DeflateState* s, ref TreeDesc desc)
    {
        CtData* tree = desc.DynTree;
        CtData* stree = desc.Stat.StaticTree;
        int elems = desc.Stat.Elems;
        int n, m;
        int maxCode = -1;
        int node;

        s->HeapLen = 0;
        s->HeapMax = HeapSize;

        for (n = 0; n < elems; n++)
        {
            if (tree[n].Freq != 0)
            {
                maxCode = n;
                s->Heap[++s->HeapLen] = n;
                s->Depth[n] = 0;
            }
            else
            {
                tree[n].Len = 0;
            }
        }

        while (s->HeapLen < 2)
        {
            node = s->Heap[++s->HeapLen] = maxCode < 2 ? ++maxCode : 0;
            tree[node].Freq = 1;
            s->Depth[node] = 0;
            s->OptLen--;
            if (stree != null)
                s->StaticLen -= stree[node].Len;
        }
        desc.MaxCode = maxCode;

        for (n = s->HeapLen / 2; n >= 1; n--)
            Pqdownheap(s, tree, n);

        node = elems;
        do
        {
            n = s->Heap[Smallest];
            s->Heap[Smallest] = s->Heap[s->HeapLen--];
            Pqdownheap(s, tree, Smallest);
            m = s->Heap[Smallest];

            s->Heap[--s->HeapMax] = n;
            s->Heap[--s->HeapMax] = m;

            tree[node].Freq = (ushort)(tree[n].Freq + tree[m].Freq);
            s->Depth[node] = (byte)((s->Depth[n] >= s->Depth[m] ? s->Depth[n] : s->Depth[m]) + 1);
            tree[n].Dad = tree[m].Dad = (ushort)node;

            s->Heap[Smallest] = node++;
            Pqdownheap(s, tree, Smallest);
        }
        while (s->HeapLen >= 2);

        s->Heap[--s->HeapMax] = s->Heap[Smallest];

        GenBitlen(s, ref desc);
        TreesTables.GenCodes(tree, maxCode, s->BlCount);
    }

    private static void ScanTree(DeflateState* s, CtData* tree, int maxCode)
    {
        int n;
        int prevlen = -1;
        int curlen;
        int nextlen = tree[0].Len;
        int count = 0;
        int maxCount = 7;
        int minCount = 4;

        if (nextlen == 0) { maxCount = 138; minCount = 3; }
        tree[maxCode + 1].Len = 0xffff; // guard

        for (n = 0; n <= maxCode; n++)
        {
            curlen = nextlen;
            nextlen = tree[n + 1].Len;
            if (++count < maxCount && curlen == nextlen)
                continue;
            else if (count < minCount)
                s->BlTree[curlen].Freq += (ushort)count;
            else if (curlen != 0)
            {
                if (curlen != prevlen) s->BlTree[curlen].Freq++;
                s->BlTree[Rep3To6].Freq++;
            }
            else if (count <= 10)
                s->BlTree[RepZ3To10].Freq++;
            else
                s->BlTree[RepZ11To138].Freq++;
            count = 0;
            prevlen = curlen;
            if (nextlen == 0) { maxCount = 138; minCount = 3; }
            else if (curlen == nextlen) { maxCount = 6; minCount = 3; }
            else { maxCount = 7; minCount = 4; }
        }
    }

    private static void SendTree(DeflateState* s, CtData* tree, int maxCode)
    {
        int n;
        int prevlen = -1;
        int curlen;
        int nextlen = tree[0].Len;
        int count = 0;
        int maxCount = 7;
        int minCount = 4;

        if (nextlen == 0) { maxCount = 138; minCount = 3; }

        for (n = 0; n <= maxCode; n++)
        {
            curlen = nextlen;
            nextlen = tree[n + 1].Len;
            if (++count < maxCount && curlen == nextlen)
                continue;
            else if (count < minCount)
            {
                do { s->SendCode(s->BlTree, curlen); } while (--count != 0);
            }
            else if (curlen != 0)
            {
                if (curlen != prevlen) { s->SendCode(s->BlTree, curlen); count--; }
                s->SendCode(s->BlTree, Rep3To6);
                s->SendBits(count - 3, 2);
            }
            else if (count <= 10)
            {
                s->SendCode(s->BlTree, RepZ3To10);
                s->SendBits(count - 3, 3);
            }
            else
            {
                s->SendCode(s->BlTree, RepZ11To138);
                s->SendBits(count - 11, 7);
            }
            count = 0;
            prevlen = curlen;
            if (nextlen == 0) { maxCount = 138; minCount = 3; }
            else if (curlen == nextlen) { maxCount = 6; minCount = 3; }
            else { maxCount = 7; minCount = 4; }
        }
    }

    private static int BuildBlTree(DeflateState* s)
    {
        int maxBlindex;

        ScanTree(s, s->DynLtree, s->LDesc.MaxCode);
        ScanTree(s, s->DynDtree, s->DDesc.MaxCode);

        BuildTree(s, ref s->BlDesc);

        for (maxBlindex = BlCodes - 1; maxBlindex >= 3; maxBlindex--)
        {
            if (s->BlTree[TreesTables.BlOrder[maxBlindex]].Len != 0)
                break;
        }
        s->OptLen += 3 * ((ulong)maxBlindex + 1) + 5 + 5 + 4;
        return maxBlindex;
    }

    private static void SendAllTrees(DeflateState* s, int lcodes, int dcodes, int blcodes)
    {
        s->SendBits(lcodes - 257, 5);
        s->SendBits(dcodes - 1, 5);
        s->SendBits(blcodes - 4, 4);
        for (int rank = 0; rank < blcodes; rank++)
            s->SendBits(s->BlTree[TreesTables.BlOrder[rank]].Len, 3);
        SendTree(s, s->DynLtree, lcodes - 1);
        SendTree(s, s->DynDtree, dcodes - 1);
    }

    public static void TrStoredBlock(DeflateState* s, byte* buf, uint storedLen, int last)
    {
        s->SendBits((StoredBlock << 1) + last, 3);
        s->BiWindup();
        s->PutShort((ushort)storedLen);
        s->PutShort((ushort)~storedLen);
        if (storedLen != 0)
        {
            Buffer.MemoryCopy(buf, s->PendingBuf + s->Pending, storedLen, storedLen);
            s->Pending += storedLen;
        }
    }

    public static void TrFlushBits(DeflateState* s) => s->BiFlush();

    public static void TrAlign(DeflateState* s)
    {
        s->SendBits(StaticTrees << 1, 3);
        s->SendCode(TreesTables.StaticLtree, EndBlock);
        s->BiFlush();
    }

    /// <summary>Emit a length/distance match against the given tree pair (shared by block-tally
    /// compression and <c>deflate_quick</c>'s direct-to-static-tree emission).</summary>
    internal static void EmitMatch(DeflateState* s, CtData* ltree, CtData* dtree, int lc, uint dist)
    {
        int code = TreesTables.LengthCode[lc];
        s->SendCode(ltree, code + Literals + 1);
        int extra = TreesTables.ExtraLbits[code];
        if (extra != 0)
        {
            lc -= TreesTables.BaseLength[code];
            s->SendBits(lc, extra);
        }
        dist--;
        code = DCode(dist);
        s->SendCode(dtree, code);
        extra = TreesTables.ExtraDbits[code];
        if (extra != 0)
        {
            dist -= (uint)TreesTables.BaseDist[code];
            s->SendBits((int)dist, extra);
        }
    }

    private static void CompressBlock(DeflateState* s, CtData* ltree, CtData* dtree)
    {
        uint sx = 0;
        if (s->SymNext != 0)
        {
            do
            {
                uint dist = s->DBuf[sx];
                int lc = s->LBuf[sx++];
                if (dist == 0)
                    s->SendCode(ltree, lc);
                else
                    EmitMatch(s, ltree, dtree, lc, dist);
            }
            while (sx < s->SymNext);
        }
        s->SendCode(ltree, EndBlock);
    }

    private static ZlibDataType DetectDataType(DeflateState* s)
    {
        uint blockMask = 0xf3ffc07fu;
        int n;
        for (n = 0; n <= 31; n++, blockMask >>= 1)
            if ((blockMask & 1) != 0 && s->DynLtree[n].Freq != 0)
                return ZlibDataType.Binary;

        if (s->DynLtree[9].Freq != 0 || s->DynLtree[10].Freq != 0 || s->DynLtree[13].Freq != 0)
            return ZlibDataType.Text;
        for (n = 32; n < Literals; n++)
            if (s->DynLtree[n].Freq != 0)
                return ZlibDataType.Text;

        return ZlibDataType.Binary;
    }

    public static void TrFlushBlock(DeflateState* s, byte* buf, uint storedLen, int last)
    {
        ulong optLenb, staticLenb;
        int maxBlindex = 0;

        if (s->Level > 0)
        {
            if (s->Strm->data_type == (int)ZlibDataType.Unknown)
                s->Strm->data_type = (int)DetectDataType(s);

            BuildTree(s, ref s->LDesc);
            BuildTree(s, ref s->DDesc);
            maxBlindex = BuildBlTree(s);

            optLenb = (s->OptLen + 3 + 7) >> 3;
            staticLenb = (s->StaticLen + 3 + 7) >> 3;

            if (staticLenb <= optLenb || s->Strategy == (int)CompressionStrategy.Fixed)
                optLenb = staticLenb;
        }
        else
        {
            optLenb = staticLenb = storedLen + 5;
        }

        if (storedLen + 4 <= optLenb && buf != null)
        {
            TrStoredBlock(s, buf, storedLen, last);
        }
        else if (staticLenb == optLenb)
        {
            s->SendBits((StaticTrees << 1) + last, 3);
            CompressBlock(s, TreesTables.StaticLtree, TreesTables.StaticDtree);
        }
        else
        {
            s->SendBits((DynTrees << 1) + last, 3);
            SendAllTrees(s, s->LDesc.MaxCode + 1, s->DDesc.MaxCode + 1, maxBlindex + 1);
            CompressBlock(s, s->DynLtree, s->DynDtree);
        }

        InitBlock(s);

        if (last != 0)
            s->BiWindup();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrTally(DeflateState* s, uint dist, uint lc)
    {
        s->DBuf[s->SymNext] = (ushort)dist;
        s->LBuf[s->SymNext++] = (byte)lc;
        if (dist == 0)
        {
            s->DynLtree[lc].Freq++;
        }
        else
        {
            s->Matches++;
            dist--;
            s->DynLtree[TreesTables.LengthCode[lc] + Literals + 1].Freq++;
            s->DynDtree[DCode(dist)].Freq++;
        }
        return s->SymNext == s->SymEnd;
    }
}
