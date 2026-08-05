using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DnZlib.Raw;
using static DnZlib.Deflate.DeflateConstants;

namespace DnZlib.Deflate;

/// <summary>
/// Static description of one of the three canonical Huffman trees (literal, distance, bit-length).
/// A blittable value type (not a class) so it can be embedded directly in <see cref="TreeDesc"/>,
/// which in turn lives inside the fully-unmanaged <see cref="DeflateState"/>.
/// </summary>
internal readonly unsafe struct StaticTreeDesc(CtData* staticTree, int* extraBits, int extraBase, int elems, int maxLength)
{
    public readonly CtData* StaticTree = staticTree;
    public readonly int* ExtraBits = extraBits;
    public readonly int ExtraBase = extraBase;
    public readonly int Elems = elems;
    public readonly int MaxLength = maxLength;
}

/// <summary>A dynamic Huffman tree plus its static description (mirrors zlib's <c>tree_desc</c>).</summary>
internal unsafe struct TreeDesc
{
    public CtData* DynTree;
    public int MaxCode;
    public StaticTreeDesc Stat;
}

/// <summary>
/// Persistent deflate engine state — the C ABI shape of zlib's <c>deflate_state</c>. The struct
/// itself lives in unmanaged memory (allocated by <c>RawZlib.deflateInit2_</c>), exactly like
/// the real thing: <c>z_stream.state</c> points straight at it, not at a GC-tracked object.
/// </summary>
internal unsafe struct DeflateState
{
    public z_stream* Strm;
    public int Status;
    public byte* PendingBuf;
    public uint PendingBufSize;
    public uint PendingOut;      // read index into PendingBuf
    public uint Pending;
    public int Wrap;             // 1 = zlib, 2 = gzip, 0 = raw (may be negated after finish)
    public void* GzHead;         // reserved for a future gz_header*; always null today (unconnected)
    public uint GzIndex;
    public byte Method;
    public int LastFlush;

    public uint WSize, WBits, WMask;
    public byte* Window;
    public uint WindowSize;
    public ushort* Prev;
    public ushort* Head;
    public uint InsH, HashSize, HashBits, HashMask, HashShift;
    public byte RollHash;        // 1 => populate head/prev with the 3-byte rolling hash (levels 8/9 longest_match_slow)
    public long BlockStart;
    public int BlockOpen; // deflate_quick's own block-open tracking (0 = closed, else 1 + last)

    public uint MatchLength, PrevMatch;
    public int MatchAvailable;
    public uint Strstart, MatchStart, Lookahead, PrevLength;
    public uint MaxChainLength, MaxLazyMatch;
    public int Level, Strategy;
    public uint GoodMatch;
    public int NiceMatch;

    public CtData* DynLtree;   // [HeapSize]
    public CtData* DynDtree;   // [2*DCodes+1]
    public CtData* BlTree;     // [2*BlCodes+1]
    public TreeDesc LDesc, DDesc, BlDesc;
    public ushort* BlCount;    // [MaxBits+1]
    public int* Heap;          // [2*LCodes+1]
    public int HeapLen, HeapMax;
    public byte* Depth;        // [2*LCodes+1]

    public ushort* DBuf;       // distances
    public byte* LBuf;         // literals/lengths
    public uint LitBufsize, SymNext, SymEnd;
    public ulong OptLen, StaticLen;
    public uint Matches, Insert;

    public ulong BiBuf;
    public int BiValid;
    public int BiUsed;
    public uint HighWater;
    public int Slid;

    /// <summary>Allocate the native buffers for the given window and memory levels.</summary>
    public void Allocate(int windowBits, int memLevel)
    {
        WBits = (uint)windowBits;
        WSize = 1u << windowBits;
        WMask = WSize - 1;
        WindowSize = 2 * WSize;

        HashBits = (uint)memLevel + 7;
        HashSize = 1u << (int)HashBits;
        HashMask = HashSize - 1;
        HashShift = (HashBits + MinMatch - 1) / MinMatch;

        LitBufsize = 1u << (memLevel + 6);

        Window = (byte*)NativeMemory.AllocZeroed(2 * WSize + 64, sizeof(byte));
        Prev = (ushort*)NativeMemory.AllocZeroed(WSize, sizeof(ushort));
        Head = (ushort*)NativeMemory.AllocZeroed(HashSize, sizeof(ushort));

        // pending_buf overlays the symbol buffers exactly as zlib (LIT_BUFS = 5).
        PendingBuf = (byte*)NativeMemory.AllocZeroed(LitBufsize, 5);
        PendingBufSize = LitBufsize * 4;
        DBuf = (ushort*)(PendingBuf + (LitBufsize << 1));
        LBuf = PendingBuf + (LitBufsize << 2);
        SymEnd = LitBufsize - 1;

        // The six buffers below are all compile-time-constant-sized (unlike Window/Prev/Head/
        // PendingBuf, which depend on windowBits/memLevel) — exactly what real zlib embeds as fixed
        // arrays directly inside deflate_state. Consolidated into one allocation, sliced by pointer
        // arithmetic, mirroring the PendingBuf/DBuf/LBuf split above.
        nuint dynLtreeBytes = (nuint)HeapSize * (nuint)sizeof(CtData);
        nuint dynDtreeBytes = (nuint)(2 * DCodes + 1) * (nuint)sizeof(CtData);
        nuint blTreeBytes = (nuint)(2 * BlCodes + 1) * (nuint)sizeof(CtData);
        nuint blCountBytes = (nuint)(MaxBits + 1) * (nuint)sizeof(ushort);
        nuint heapBytes = (nuint)(2 * LCodes + 1) * (nuint)sizeof(int);
        nuint depthBytes = (nuint)(2 * LCodes + 1) * (nuint)sizeof(byte);

        byte* treeBlob = (byte*)NativeMemory.AllocZeroed(
            dynLtreeBytes + dynDtreeBytes + blTreeBytes + blCountBytes + heapBytes + depthBytes);
        DynLtree = (CtData*)treeBlob; treeBlob += dynLtreeBytes;
        DynDtree = (CtData*)treeBlob; treeBlob += dynDtreeBytes;
        BlTree = (CtData*)treeBlob; treeBlob += blTreeBytes;
        BlCount = (ushort*)treeBlob; treeBlob += blCountBytes;
        Heap = (int*)treeBlob; treeBlob += heapBytes;
        Depth = treeBlob;
    }

    // --- pending output helpers ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PutByte(byte b) => PendingBuf[Pending++] = b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PutShort(uint w)
    {
        // Low byte first, matching the two PutByte calls this replaces (DEFLATE's stored-block
        // LEN/NLEN wire order). .NET's in-memory ushort layout is little-endian on every RID this
        // project targets, so a raw unaligned write reproduces that byte order exactly.
        Unsafe.WriteUnaligned(PendingBuf + Pending, (ushort)w);
        Pending += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PutShortMsb(uint b)
    {
        PutByte((byte)(b >> 8));
        PutByte((byte)(b & 0xff));
    }

    // --- bit buffer (64-bit accumulator, flushed 32 bits at a time; produces zlib's exact
    // bitstream — same LSB-first packing as the classic 16-bit path, just fewer stores) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendBits(int value, int length)
    {
        // Callers guarantee value < 2^length (same invariant the old (ushort) cast relied on).
        // Between calls BiValid stays in [0,31]; with length <= 15 the shift tops out at bit 45,
        // and one 32-bit flush per call always brings it back below 32.
        BiBuf |= (ulong)(uint)value << BiValid;
        BiValid += length;
        if (BiValid >= 32)
        {
            Unsafe.WriteUnaligned(PendingBuf + Pending, (uint)BiBuf);
            Pending += 4;
            BiBuf >>= 32;
            BiValid -= 32;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendCode(CtData* tree, int c) => SendBits(tree[c].Code, tree[c].Len);

    public void BiFlush()
    {
        // Flush every whole byte currently buffered, leaving a sub-byte remainder.
        while (BiValid >= 8)
        {
            PutByte((byte)BiBuf);
            BiBuf >>= 8;
            BiValid -= 8;
        }
    }

    public void BiWindup()
    {
        // Emit all remaining bits (the final partial byte is zero-padded, as zlib does).
        BiUsed = BiValid > 0 ? ((BiValid - 1) & 7) + 1 : 0;
        while (BiValid > 0)
        {
            PutByte((byte)BiBuf);
            BiBuf >>= 8;
            BiValid -= 8;
        }
        BiBuf = 0;
        BiValid = 0;
    }

    /// <summary>
    /// Free the native buffers owned by this state (not the struct itself — the caller,
    /// <c>RawZlib.deflateEnd</c>, owns that allocation and frees it after this returns).
    /// </summary>
    public void FreeNative()
    {
        if (Window != null) { NativeMemory.Free(Window); Window = null; }
        if (Prev != null) { NativeMemory.Free(Prev); Prev = null; }
        if (Head != null) { NativeMemory.Free(Head); Head = null; }
        if (PendingBuf != null) { NativeMemory.Free(PendingBuf); PendingBuf = null; DBuf = null; LBuf = null; }
        if (DynLtree != null)
        {
            NativeMemory.Free(DynLtree);
            DynLtree = null; DynDtree = null; BlTree = null; BlCount = null; Heap = null; Depth = null;
        }
    }
}
