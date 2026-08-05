using System.Runtime.InteropServices;
using DnZlib.Internal;
using DnZlib.Raw;

namespace DnZlib.Inflate;

/// <summary>
/// Inflate state machine mode. Values are ordered exactly as zlib's <c>inflate_mode</c> so the
/// ordinal comparisons in <c>inf_leave</c> (<c>mode &lt; Bad</c>, <c>mode &lt; Check</c>) hold.
/// </summary>
internal enum InflateMode
{
    Head = 16180,   // waiting for magic header
    Flags,          // waiting for method and flags (gzip)
    Time,           // waiting for modification time (gzip)
    Os,             // waiting for extra flags and operating system (gzip)
    ExLen,          // waiting for extra length (gzip)
    Extra,          // waiting for extra bytes (gzip)
    Name,           // waiting for end of file name (gzip)
    Comment,        // waiting for end of comment (gzip)
    Hcrc,           // waiting for header crc (gzip)
    DictId,         // waiting for dictionary check value
    Dict,           // waiting for inflateSetDictionary() call
    Type,           // waiting for type bits, including last-flag bit
    TypeDo,         // same, but skip check to exit inflate on new block
    Stored,         // waiting for stored size (length and complement)
    Copy_,          // same as Copy below, but only first time in
    Copy,           // waiting for input or output to copy stored block
    Table,          // waiting for dynamic block table lengths
    LenLens,        // waiting for code length code lengths
    CodeLens,       // waiting for length/lit and distance code lengths
    Len_,           // same as Len below, but only first time in
    Len,            // waiting for length/lit/eob code
    LenExt,         // waiting for length extra bits
    Dist,           // waiting for distance code
    DistExt,        // waiting for distance extra bits
    Match,          // waiting for output space to copy string
    Lit,            // waiting for output space to write literal
    Check,          // waiting for 32-bit check value
    Length,         // waiting for 32-bit length (gzip)
    Done,           // finished check, done -- remain here until reset
    Bad,            // got a data error -- remain here until reset
    Mem,            // got an inflate() memory error -- remain here until reset
    Sync,           // looking for synchronization bytes to restart inflate()
}

/// <summary>
/// Persistent inflate engine state — the C ABI shape of zlib's <c>inflate_state</c>. The struct
/// itself lives in unmanaged memory (allocated by <c>RawZlib.inflateInit2_</c>), exactly like
/// the real thing: <c>z_stream.state</c> points straight at it, not at a GC-tracked object.
/// </summary>
internal unsafe struct InflateState
{
    // Extra bytes past the logical window end so the chunked fast-copy can overshoot safely.
    public const int WindowPad = 64;

    public z_stream* Strm;
    public InflateMode Mode;
    public int Last;
    public int Wrap;        // bit0 = zlib, bit1 = gzip, bit2 = validate check
    public int HaveDict;
    public int Flags;       // gzip method+flags; 0 = zlib; -1 = raw / not yet known
    public uint Dmax;
    public uint Check;
    public uint Total;
    public void* Head;      // reserved for a future gz_header*; always null today (unconnected)

    // Sliding window.
    public uint WBits;
    public uint WSize;
    public uint WHave;
    public uint WNext;
    public byte* Window;

    // Bit accumulator (64-bit to allow 8-byte refills in the fast loop).
    public ulong Hold;
    public uint Bits;

    // String/stored-block copying.
    public uint Length;
    public uint Offset;
    public uint Extra;

    // Active decode tables.
    public Code* LenCode;
    public Code* DistCode;
    public uint LenBits;
    public uint DistBits;

    // Dynamic-table building.
    public uint NCode;
    public uint NLen;
    public uint NDist;
    public uint Have;
    public Code* Next;
    public ushort* Lens;    // [320]
    public ushort* Work;    // [288]
    public Code* Codes;     // [ENOUGH]
    public int Sane;
    public int Back;
    public uint Was;

    // Live per-call I/O cursors are mirrored on z_stream; see z_stream.next_in etc.

    /// <summary>Allocate the fixed-size working buffers. Called once, right after the raw struct itself is allocated.</summary>
    public void Init()
    {
        // Lens/Work/Codes are all compile-time-constant-sized — exactly what real zlib embeds as
        // fixed arrays directly inside inflate_state. Consolidated into one allocation, sliced by
        // pointer arithmetic, instead of three separate small native allocations per stream.
        nuint lensBytes = 320 * (nuint)sizeof(ushort);
        nuint workBytes = 288 * (nuint)sizeof(ushort);
        nuint codesBytes = (nuint)ZlibConstants.Enough * (nuint)sizeof(Code);

        byte* blob = (byte*)NativeMemory.Alloc(lensBytes + workBytes + codesBytes);
        Lens = (ushort*)blob; blob += lensBytes;
        Work = (ushort*)blob; blob += workBytes;
        Codes = (Code*)blob;
    }

    /// <summary>Allocate (once) and return the sliding window buffer of <c>1&lt;&lt;wbits</c> bytes (+ pad).</summary>
    public byte* EnsureWindow()
    {
        if (Window == null)
            Window = (byte*)NativeMemory.AllocZeroed((1u << (int)WBits) + WindowPad, sizeof(byte));
        return Window;
    }

    /// <summary>Free the window if its size no longer matches the requested wbits.</summary>
    public void FreeWindowIfResized(uint newWBits)
    {
        if (Window != null && WBits != newWBits)
        {
            NativeMemory.Free(Window);
            Window = null;
        }
    }

    /// <summary>
    /// Free the native buffers owned by this state (not the struct itself — the caller,
    /// <c>RawZlib.inflateEnd</c>, owns that allocation and frees it after this returns).
    /// </summary>
    public void FreeNative()
    {
        if (Lens != null) { NativeMemory.Free(Lens); Lens = null; Work = null; Codes = null; }
        if (Window != null) { NativeMemory.Free(Window); Window = null; }
    }
}
