using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DnZlib.Raw;
using static DnZlib.Deflate.BlockState;
using static DnZlib.Deflate.DeflateConstants;

namespace DnZlib.Deflate;

/// <summary>
/// The deflate (compression) engine — a faithful port of zlib's <c>deflate.c</c>, operating
/// directly on unmanaged <see cref="DeflateState"/>/<see cref="z_stream"/> pointers. This is what
/// <c>RawZlib.deflate</c>/<c>deflateSetDictionary</c>/<c>deflateReset</c> call; it never touches
/// any managed wrapper.
/// </summary>
internal static unsafe class DeflateEngine
{
    /// <summary>Reset match-finding state to the start of a fresh compression (zlib's <c>lm_init</c>).</summary>
    public static void LmInit(DeflateState* s)
    {
        s->WindowSize = 2 * s->WSize;
        ClearHash(s);
        DeflateConfig cfg = Configuration[s->Level];
        s->MaxLazyMatch = cfg.MaxLazy;
        s->GoodMatch = cfg.GoodLength;
        s->NiceMatch = cfg.NiceLength;
        s->MaxChainLength = cfg.MaxChain;
        // Rolling hash + longest_match_slow on the top levels (max_chain >= 1024 => level 8/9), where
        // the chain walk on redundant input dominates enough to outweigh the offset-search overhead.
        s->RollHash = (byte)(cfg.MaxChain >= 1024 ? 1 : 0);
        s->Strstart = 0;
        s->BlockStart = 0;
        s->BlockOpen = 0;
        s->Lookahead = 0;
        s->Insert = 0;
        s->MatchLength = s->PrevLength = MinMatch - 1;
        s->MatchAvailable = 0;
        s->InsH = 0;
    }

    /// <summary>Reset the compression state, keeping allocated buffers (zlib's <c>deflateResetKeep</c> + <c>lm_init</c>).</summary>
    public static void Reset(DeflateState* s)
    {
        z_stream* strm = s->Strm;
        strm->total_in = strm->total_out = 0;
        strm->msg = null;
        strm->data_type = (int)ZlibDataType.Unknown;

        s->Pending = 0;
        s->PendingOut = 0;

        if (s->Wrap < 0)
            s->Wrap = -s->Wrap;
        s->Status = s->Wrap == 2 ? GzipState : InitState;
        strm->adler = s->Wrap == 2 ? Crc32.Compute(default) : Adler32.Compute(default);
        s->LastFlush = -2;

        Trees.TrInit(s);
        LmInit(s);
    }

    public static void ClearHash(DeflateState* s)
    {
        NativeMemory.Clear(s->Head, (nuint)s->HashSize * sizeof(ushort));
        s->Slid = 0;
    }

    private static void SlideHash(DeflateState* s)
    {
        ushort wsize = (ushort)s->WSize;
        SlideHashHelper.Slide(s->Head, s->HashSize, wsize);
        SlideHashHelper.Slide(s->Prev, s->WSize, wsize);
        s->Slid = 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MaxDist(DeflateState* s) => s->WSize - MinLookahead;

    /// <summary>
    /// zlib-ng-style 4-byte multiplicative hash. Spreads far better than zlib's 3-byte rolling hash,
    /// so hash chains stay short and match finding is faster. (Changes output vs zlib, but stays a
    /// valid DEFLATE stream.)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashCalc(DeflateState* s, uint pos) =>
        (Unsafe.ReadUnaligned<uint>(s->Window + pos) * 2654435761u) >> (32 - (int)s->HashBits);

    /// <summary>
    /// zlib-ng's rolling 3-byte hash (<c>deflate.c</c> <c>update_hash_roll</c>, <c>HASH_SLIDE</c>=5),
    /// used only at the levels that run <see cref="LongestMatchSlow"/>. Computed statelessly: because
    /// the value is masked after each fold, it depends only on the 3 bytes at <paramref name="pos"/>,
    /// so it matches the offset search's incremental <see cref="UpdateHashRoll"/> below without
    /// threading a running <c>ins_h</c> through every insertion site.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint UpdateHashRoll(uint h, uint val, uint mask) => ((h << 5) ^ val) & mask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashCalcRoll(DeflateState* s, uint pos)
    {
        // Single 4-byte read (matching HashCalc's read width), then fold the low 3 bytes the same way
        // UpdateHashRoll would — masked after each fold so the value matches the offset search.
        uint x = Unsafe.ReadUnaligned<uint>(s->Window + pos);
        uint mask = s->HashMask;
        uint h = (x & 0xFF) & mask;
        h = ((h << 5) ^ ((x >> 8) & 0xFF)) & mask;
        return ((h << 5) ^ ((x >> 16) & 0xFF)) & mask;
    }

    /// <summary>Hash dispatcher: the rolling hash on <see cref="LongestMatchSlow"/> levels, else the
    /// 4-byte multiplicative hash. The branch is on a per-compression-constant flag.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Hash(DeflateState* s, uint pos) =>
        s->RollHash != 0 ? HashCalcRoll(s, pos) : HashCalc(s, pos);

    private static uint ReadBuf(DeflateState* s, byte* buf, uint size)
    {
        z_stream* strm = s->Strm;
        uint len = strm->avail_in;
        if (len > size)
            len = size;
        if (len == 0)
            return 0;
        strm->avail_in -= len;
        Buffer.MemoryCopy(strm->next_in, buf, len, len);
        if (s->Wrap == 1)
            strm->adler = Adler32.Update((uint)strm->adler, buf, len);
        else if (s->Wrap == 2)
            strm->adler = Crc32.Update((uint)strm->adler, buf, len);
        strm->next_in += len;
        strm->total_in += len;
        return len;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void FillWindow(DeflateState* s)
    {
        uint wsize = s->WSize;
        uint more;

        do
        {
            more = s->WindowSize - s->Lookahead - s->Strstart;

            if (s->Strstart >= wsize + MaxDist(s))
            {
                Buffer.MemoryCopy(s->Window + wsize, s->Window, wsize, wsize - more);
                s->MatchStart -= wsize;
                s->Strstart -= wsize;
                s->BlockStart -= wsize;
                if (s->Insert > s->Strstart)
                    s->Insert = s->Strstart;
                SlideHash(s);
                more += wsize;
            }
            if (s->Strm->avail_in == 0)
                break;

            uint n = ReadBuf(s, s->Window + s->Strstart + s->Lookahead, more);
            s->Lookahead += n;

            if (s->Lookahead + s->Insert >= MinMatch)
            {
                uint str = s->Strstart - s->Insert;
                while (s->Insert != 0)
                {
                    uint h = Hash(s, str);
                    s->Prev[str & s->WMask] = s->Head[h];
                    s->Head[h] = (ushort)str;
                    str++;
                    s->Insert--;
                    if (s->Lookahead + s->Insert < MinMatch)
                        break;
                }
            }
        }
        while (s->Lookahead < MinLookahead && s->Strm->avail_in != 0);

        if (s->HighWater < s->WindowSize)
        {
            uint curr = s->Strstart + s->Lookahead;
            uint init;
            if (s->HighWater < curr)
            {
                init = s->WindowSize - curr;
                if (init > WinInit)
                    init = WinInit;
                NativeMemory.Clear(s->Window + curr, init);
                s->HighWater = curr + init;
            }
            else if (s->HighWater < curr + WinInit)
            {
                init = curr + WinInit - s->HighWater;
                if (init > s->WindowSize - s->HighWater)
                    init = s->WindowSize - s->HighWater;
                NativeMemory.Clear(s->Window + s->HighWater, init);
                s->HighWater += init;
            }
        }
    }

    // Below this compression level, longest_match bails out of the hash chain on the first probe
    // that fails to improve the match — cheaper, at a tiny cost in ratio (mirrors zlib-ng).
    private const int EarlyExitTriggerLevel = 5;

    // Read offset (relative to scan) whose 8-byte window straddles the current best_len boundary,
    // backed off just enough to stay inside already-scanned bytes. Mirrors zlib-ng/fast-zlib.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint OffsetFor(uint bestLen)
    {
        uint offset = bestLen - 1;
        if (bestLen >= 4)          // an extra byte still fits a 4-byte read
        {
            offset -= 2;
            if (bestLen >= 8)      // ...or an 8-byte read
                offset -= 4;
        }
        return offset;
    }

    /// <summary>
    /// fast-zlib-style longest_match: a width-adaptive early-out (compare the first bytes AND the
    /// bytes straddling the current best length, 2/4/8 at a time) rejects non-improving chain
    /// entries far faster than a byte-at-a-time probe, and low levels bail on the first miss. Finds
    /// the same match as zlib, only quicker. Port of zlib-ng match_tpl.h (non-SLOW variant).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static uint LongestMatch(DeflateState* s, uint curMatch)
    {
        uint strstart = s->Strstart;
        uint wmask = s->WMask;
        byte* window = s->Window;
        byte* scan = window + strstart;
        ushort* prev = s->Prev;
        uint lookahead = s->Lookahead;
        uint niceMatch = (uint)s->NiceMatch;

        uint bestLen = s->PrevLength != 0 ? s->PrevLength : (uint)(MinMatch - 1);
        uint offset = OffsetFor(bestLen);
        ulong scanStart = Unsafe.ReadUnaligned<ulong>(scan);
        ulong scanEnd = Unsafe.ReadUnaligned<ulong>(scan + offset);

        uint chainLength = s->MaxChainLength;
        if (bestLen >= s->GoodMatch)
            chainLength >>= 2;
        uint limit = strstart > MaxDist(s) ? strstart - MaxDist(s) : Nil;
        bool earlyExit = s->Level < EarlyExitTriggerLevel;

        for (;;)
        {
            if (curMatch >= strstart)
                break;

            byte* mbase = window + curMatch;

            // Reject on both the first bytes and the bytes around best_len before touching compare256.
            bool candidate;
            if (bestLen < 4)
                candidate = Unsafe.ReadUnaligned<ushort>(mbase + offset) == (ushort)scanEnd
                         && Unsafe.ReadUnaligned<ushort>(mbase) == (ushort)scanStart;
            else if (bestLen >= 8)
                candidate = Unsafe.ReadUnaligned<ulong>(mbase + offset) == scanEnd
                         && Unsafe.ReadUnaligned<ulong>(mbase) == scanStart;
            else
                candidate = Unsafe.ReadUnaligned<uint>(mbase + offset) == (uint)scanEnd
                         && Unsafe.ReadUnaligned<uint>(mbase) == (uint)scanStart;

            if (candidate)
            {
                uint len = Compare256Helper.Match(scan + 2, mbase + 2) + 2;
                if (len > bestLen)
                {
                    s->MatchStart = curMatch;
                    if (len > lookahead)
                        return lookahead;
                    bestLen = len;
                    if (bestLen >= niceMatch)
                        return bestLen;
                    offset = OffsetFor(bestLen);
                    scanEnd = Unsafe.ReadUnaligned<ulong>(scan + offset);
                }
                else if (earlyExit)
                {
                    break;
                }
            }

            if (--chainLength == 0)
                break;
            curMatch = prev[curMatch & wmask];
            if (curMatch <= limit)
                break;
        }

        return bestLen <= lookahead ? bestLen : lookahead;
    }

    /// <summary>
    /// zlib-ng's LONGEST_MATCH_SLOW: once a match is in hand, spend extra effort to find a longer one
    /// by jumping to a more distant hash chain via the rolling hash instead of only walking prev[].
    /// Two offset searches — one before the walk (over scan[1..best_len]) and one after each improved
    /// match (over the match interior plus a hash near its tail) — let it skip the short-match noise
    /// that otherwise clogs the chain on redundant input. Requires head/prev populated by the rolling
    /// hash (<see cref="HashCalcRoll"/>). Port of match_tpl.h's LONGEST_MATCH_SLOW variant.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static uint LongestMatchSlow(DeflateState* s, uint curMatch)
    {
        uint strstart = s->Strstart;
        uint wmask = s->WMask;
        byte* window = s->Window;
        byte* scan = window + strstart;
        ushort* prev = s->Prev;
        ushort* head = s->Head;
        uint mask = s->HashMask;
        uint lookahead = s->Lookahead;
        uint niceMatch = (uint)s->NiceMatch;

        uint bestLen = s->PrevLength != 0 ? s->PrevLength : (uint)(MinMatch - 1);
        uint offset = OffsetFor(bestLen);
        ulong scanStart = Unsafe.ReadUnaligned<ulong>(scan);
        ulong scanEnd = Unsafe.ReadUnaligned<ulong>(scan + offset);
        byte* mbaseStart = window;
        byte* mbaseEnd = window + offset;

        uint chainLength = s->MaxChainLength;
        if (bestLen >= s->GoodMatch)
            chainLength >>= 2;

        uint matchOffset = 0;
        uint limitBase = strstart > MaxDist(s) ? strstart - MaxDist(s) : Nil;
        uint limit = limitBase;

        // Pre-loop offset search (lazy continuation): over scan[1..best_len], jump to the most
        // distant chain that shares a 3-byte substring, so the walk starts nearer a beating match.
        // Reached only when best_len is seeded >= MIN_MATCH, i.e. via the lazy `deflate_slow` path;
        // `deflate_medium` (the only roll-hash caller today) seeds best_len at MIN_MATCH-1, so for it
        // the post-match search below is what does the work. Kept faithful to zlib-ng's variant.
        if (bestLen >= MinMatch)
        {
            uint hash = UpdateHashRoll(0, scan[1], mask);
            hash = UpdateHashRoll(hash, scan[2], mask);
            for (uint i = 3; i <= bestLen; i++)
            {
                hash = UpdateHashRoll(hash, scan[i], mask);
                uint pos = head[hash];
                if (pos < curMatch)
                {
                    matchOffset = i - 2;
                    curMatch = pos;
                }
            }
            limit = limitBase + matchOffset;
            if (curMatch <= limit)
                return bestLen <= lookahead ? bestLen : lookahead;
            mbaseStart = window - matchOffset;
            mbaseEnd = mbaseStart + offset;
        }

        for (;;)
        {
            if (curMatch >= strstart)
                break;

            byte* pStart = mbaseStart + curMatch;

            bool candidate;
            if (bestLen < 4)
                candidate = Unsafe.ReadUnaligned<ushort>(mbaseEnd + curMatch) == (ushort)scanEnd
                         && Unsafe.ReadUnaligned<ushort>(pStart) == (ushort)scanStart;
            else if (bestLen >= 8)
                candidate = Unsafe.ReadUnaligned<ulong>(mbaseEnd + curMatch) == scanEnd
                         && Unsafe.ReadUnaligned<ulong>(pStart) == scanStart;
            else
                candidate = Unsafe.ReadUnaligned<uint>(mbaseEnd + curMatch) == (uint)scanEnd
                         && Unsafe.ReadUnaligned<uint>(pStart) == (uint)scanStart;

            if (candidate)
            {
                uint len = Compare256Helper.Match(scan + 2, pStart + 2) + 2;
                if (len > bestLen)
                {
                    uint matchStart = curMatch - matchOffset;
                    s->MatchStart = matchStart;
                    if (len > lookahead)
                        return lookahead;
                    bestLen = len;
                    if (bestLen >= niceMatch)
                        return bestLen;
                    offset = OffsetFor(bestLen);
                    scanEnd = Unsafe.ReadUnaligned<ulong>(scan + offset);

                    // Post-match offset search: walk the match interior for a nearer-yet-more-distant
                    // chain, then try a rolling-hash lookup at the tail to extend by one more byte.
                    if (len > MinMatch && matchStart + len < strstart)
                    {
                        curMatch -= matchOffset;
                        matchOffset = 0;
                        uint nextPos = curMatch;
                        for (uint i = 0; i <= len - MinMatch; i++)
                        {
                            uint pos = prev[(curMatch + i) & wmask];
                            if (pos < nextPos)
                            {
                                if (pos <= limitBase + i)
                                    return bestLen <= lookahead ? bestLen : lookahead;
                                nextPos = pos;
                                matchOffset = i;
                            }
                        }
                        curMatch = nextPos;

                        byte* scanEndStr = scan + len - (MinMatch + 1);
                        uint hash = UpdateHashRoll(0, scanEndStr[0], mask);
                        hash = UpdateHashRoll(hash, scanEndStr[1], mask);
                        hash = UpdateHashRoll(hash, scanEndStr[2], mask);
                        uint hpos = head[hash];
                        if (hpos < curMatch)
                        {
                            matchOffset = len - (MinMatch + 1);
                            if (hpos <= limitBase + matchOffset)
                                return bestLen <= lookahead ? bestLen : lookahead;
                            curMatch = hpos;
                        }

                        limit = limitBase + matchOffset;
                        mbaseStart = window - matchOffset;
                        mbaseEnd = mbaseStart + offset;
                        continue;
                    }
                    mbaseEnd = mbaseStart + offset;
                }
            }

            if (--chainLength == 0)
                break;
            curMatch = prev[curMatch & wmask];
            if (curMatch <= limit)
                break;
        }

        return bestLen <= lookahead ? bestLen : lookahead;
    }

    private static void FlushBlockOnly(DeflateState* s, int last)
    {
        Trees.TrFlushBlock(s, s->BlockStart >= 0 ? s->Window + s->BlockStart : null,
                           (uint)((long)s->Strstart - s->BlockStart), last);
        s->BlockStart = s->Strstart;
        FlushPending(s->Strm);
    }

    public static void FlushPending(z_stream* strm)
    {
        var s = (DeflateState*)strm->state;
        Trees.TrFlushBits(s);
        uint len = s->Pending;
        if (len > strm->avail_out)
            len = strm->avail_out;
        if (len == 0)
            return;
        Buffer.MemoryCopy(s->PendingBuf + s->PendingOut, strm->next_out, len, len);
        strm->next_out += len;
        s->PendingOut += len;
        strm->total_out += len;
        strm->avail_out -= len;
        s->Pending -= len;
        if (s->Pending == 0)
            s->PendingOut = 0;
    }

    private static uint InsertString(DeflateState* s, uint str)
    {
        uint h = Hash(s, str);
        uint head = s->Prev[str & s->WMask] = s->Head[h];
        s->Head[h] = (ushort)str;
        return head;
    }

    /// <summary>The deflate entry point — a faithful port of zlib's <c>deflate()</c>.</summary>
    public static ZlibResult Deflate(z_stream* strm, FlushMode flush)
    {
        if (strm == null || strm->state == null || (int)flush > (int)FlushMode.Block || (int)flush < 0)
            return ZlibResult.StreamError;
        var s = (DeflateState*)strm->state;

        if (strm->next_out == null || (strm->avail_in != 0 && strm->next_in == null) ||
            (s->Status == FinishState && flush != FlushMode.Finish))
            return ZlibResult.StreamError;
        if (strm->avail_out == 0)
            return ZlibResult.BufError;

        int oldFlush = s->LastFlush;
        s->LastFlush = (int)flush;

        if (s->Pending != 0)
        {
            FlushPending(strm);
            if (strm->avail_out == 0)
            {
                s->LastFlush = -1;
                return ZlibResult.Ok;
            }
        }
        else if (strm->avail_in == 0 && Rank((int)flush) <= Rank(oldFlush) && flush != FlushMode.Finish)
        {
            return ZlibResult.BufError;
        }

        if (s->Status == FinishState && strm->avail_in != 0)
            return ZlibResult.BufError;

        // zlib header
        if (s->Status == InitState && s->Wrap == 0)
            s->Status = BusyState;
        if (s->Status == InitState)
        {
            uint header = (uint)((ZDeflated + ((s->WBits - 8) << 4)) << 8);
            uint levelFlags;
            if (s->Strategy >= (int)CompressionStrategy.HuffmanOnly || s->Level < 2)
                levelFlags = 0;
            else if (s->Level < 6)
                levelFlags = 1;
            else if (s->Level == 6)
                levelFlags = 2;
            else
                levelFlags = 3;
            header |= levelFlags << 6;
            if (s->Strstart != 0)
                header |= PresetDict;
            header += 31 - (header % 31);

            s->PutShortMsb(header);
            if (s->Strstart != 0)
            {
                s->PutShortMsb((uint)strm->adler >> 16);
                s->PutShortMsb((uint)strm->adler & 0xffff);
            }
            strm->adler = Adler32.Compute(default);
            s->Status = BusyState;

            FlushPending(strm);
            if (s->Pending != 0)
            {
                s->LastFlush = -1;
                return ZlibResult.Ok;
            }
        }

        // gzip header (no custom gz_header supported yet)
        if (s->Status == GzipState)
        {
            strm->adler = Crc32.Compute(default);
            s->PutByte(31);
            s->PutByte(139);
            s->PutByte(8);
            s->PutByte(0);
            s->PutByte(0);
            s->PutByte(0);
            s->PutByte(0);
            s->PutByte(0);
            s->PutByte(s->Level == 9 ? (byte)2
                : (byte)(s->Strategy >= (int)CompressionStrategy.HuffmanOnly || s->Level < 2 ? 4 : 0));
            s->PutByte(OsCode);
            s->Status = BusyState;

            FlushPending(strm);
            if (s->Pending != 0)
            {
                s->LastFlush = -1;
                return ZlibResult.Ok;
            }
        }

        if (strm->avail_in != 0 || s->Lookahead != 0 ||
            (flush != FlushMode.NoFlush && s->Status != FinishState))
        {
            BlockState bstate =
                s->Level == 0 ? DeflateStored(s, flush) :
                s->Strategy == (int)CompressionStrategy.HuffmanOnly ? DeflateHuff(s, flush) :
                s->Strategy == (int)CompressionStrategy.Rle ? DeflateRle(s, flush) :
                Configuration[s->Level].Func == DeflateFunc.Quick ? DeflateQuick(s, flush) :
                Configuration[s->Level].Func == DeflateFunc.Fast ? DeflateFast(s, flush) :
                Configuration[s->Level].Func == DeflateFunc.Medium ? DeflateMedium(s, flush) :
                DeflateSlow(s, flush);

            if (bstate == FinishStarted || bstate == FinishDone)
                s->Status = FinishState;
            if (bstate == NeedMore || bstate == FinishStarted)
            {
                if (strm->avail_out == 0)
                    s->LastFlush = -1;
                return ZlibResult.Ok;
            }
            if (bstate == BlockDone)
            {
                if (flush == FlushMode.PartialFlush)
                {
                    Trees.TrAlign(s);
                }
                else if (flush != FlushMode.Block)
                {
                    Trees.TrStoredBlock(s, null, 0, 0);
                    if (flush == FlushMode.FullFlush)
                    {
                        ClearHash(s);
                        if (s->Lookahead == 0)
                        {
                            s->Strstart = 0;
                            s->BlockStart = 0;
                            s->Insert = 0;
                        }
                    }
                }
                FlushPending(strm);
                if (strm->avail_out == 0)
                {
                    s->LastFlush = -1;
                    return ZlibResult.Ok;
                }
            }
        }

        if (flush != FlushMode.Finish)
            return ZlibResult.Ok;
        if (s->Wrap <= 0)
            return ZlibResult.StreamEnd;

        // trailer
        if (s->Wrap == 2)
        {
            s->PutByte((byte)((uint)strm->adler & 0xff));
            s->PutByte((byte)(((uint)strm->adler >> 8) & 0xff));
            s->PutByte((byte)(((uint)strm->adler >> 16) & 0xff));
            s->PutByte((byte)(((uint)strm->adler >> 24) & 0xff));
            s->PutByte((byte)((uint)strm->total_in & 0xff));
            s->PutByte((byte)(((uint)strm->total_in >> 8) & 0xff));
            s->PutByte((byte)(((uint)strm->total_in >> 16) & 0xff));
            s->PutByte((byte)(((uint)strm->total_in >> 24) & 0xff));
        }
        else
        {
            s->PutShortMsb((uint)strm->adler >> 16);
            s->PutShortMsb((uint)strm->adler & 0xffff);
        }
        FlushPending(strm);
        if (s->Wrap > 0)
            s->Wrap = -s->Wrap;
        return s->Pending != 0 ? ZlibResult.Ok : ZlibResult.StreamEnd;
    }

    private static BlockState DeflateStored(DeflateState* s, FlushMode flush)
    {
        z_stream* strm = s->Strm;
        uint minBlock = Math.Min(s->PendingBufSize - 5, s->WSize);
        int last = 0;
        uint len, left, have;
        uint used = strm->avail_in;
        do
        {
            len = MaxStored;
            have = ((uint)s->BiValid + 42) >> 3;
            if (strm->avail_out < have)
                break;
            have = strm->avail_out - have;
            left = (uint)((long)s->Strstart - s->BlockStart);
            if (len > left + strm->avail_in)
                len = left + strm->avail_in;
            if (len > have)
                len = have;

            if (len < minBlock && ((len == 0 && flush != FlushMode.Finish) ||
                                   flush == FlushMode.NoFlush ||
                                   len != left + strm->avail_in))
                break;

            last = flush == FlushMode.Finish && len == left + strm->avail_in ? 1 : 0;
            Trees.TrStoredBlock(s, null, 0, last);

            s->PendingBuf[s->Pending - 4] = (byte)len;
            s->PendingBuf[s->Pending - 3] = (byte)(len >> 8);
            s->PendingBuf[s->Pending - 2] = (byte)~len;
            s->PendingBuf[s->Pending - 1] = (byte)(~len >> 8);

            FlushPending(strm);

            if (left != 0)
            {
                if (left > len)
                    left = len;
                Buffer.MemoryCopy(s->Window + s->BlockStart, strm->next_out, left, left);
                strm->next_out += left;
                strm->avail_out -= left;
                strm->total_out += left;
                s->BlockStart += left;
                len -= left;
            }
            if (len != 0)
            {
                ReadBuf(s, strm->next_out, len);
                strm->next_out += len;
                strm->avail_out -= len;
                strm->total_out += len;
            }
        }
        while (last == 0);

        used -= strm->avail_in;
        if (used != 0)
        {
            if (used >= s->WSize)
            {
                s->Matches = 2;
                Buffer.MemoryCopy(strm->next_in - s->WSize, s->Window, s->WSize, s->WSize);
                s->Strstart = s->WSize;
                s->Insert = s->Strstart;
            }
            else
            {
                if (s->WindowSize - s->Strstart <= used)
                {
                    s->Strstart -= s->WSize;
                    Buffer.MemoryCopy(s->Window + s->WSize, s->Window, s->Strstart, s->Strstart);
                    if (s->Matches < 2)
                        s->Matches++;
                    if (s->Insert > s->Strstart)
                        s->Insert = s->Strstart;
                }
                Buffer.MemoryCopy(strm->next_in - used, s->Window + s->Strstart, used, used);
                s->Strstart += used;
                s->Insert += Math.Min(used, s->WSize - s->Insert);
            }
            s->BlockStart = s->Strstart;
        }
        if (s->HighWater < s->Strstart)
            s->HighWater = s->Strstart;

        if (last != 0)
        {
            s->BiUsed = 8;
            return FinishDone;
        }
        if (flush != FlushMode.NoFlush && flush != FlushMode.Finish &&
            strm->avail_in == 0 && (long)s->Strstart == s->BlockStart)
            return BlockDone;

        have = s->WindowSize - s->Strstart;
        if (strm->avail_in > have && s->BlockStart >= s->WSize)
        {
            s->BlockStart -= s->WSize;
            s->Strstart -= s->WSize;
            Buffer.MemoryCopy(s->Window + s->WSize, s->Window, s->Strstart, s->Strstart);
            if (s->Matches < 2)
                s->Matches++;
            have += s->WSize;
            if (s->Insert > s->Strstart)
                s->Insert = s->Strstart;
        }
        if (have > strm->avail_in)
            have = strm->avail_in;
        if (have != 0)
        {
            ReadBuf(s, s->Window + s->Strstart, have);
            s->Strstart += have;
            s->Insert += Math.Min(have, s->WSize - s->Insert);
        }
        if (s->HighWater < s->Strstart)
            s->HighWater = s->Strstart;

        have = ((uint)s->BiValid + 42) >> 3;
        have = Math.Min(s->PendingBufSize - have, (uint)MaxStored);
        minBlock = Math.Min(have, s->WSize);
        left = (uint)((long)s->Strstart - s->BlockStart);
        if (left >= minBlock ||
            ((left != 0 || flush == FlushMode.Finish) && flush != FlushMode.NoFlush &&
             strm->avail_in == 0 && left <= have))
        {
            len = Math.Min(left, have);
            last = flush == FlushMode.Finish && strm->avail_in == 0 && len == left ? 1 : 0;
            Trees.TrStoredBlock(s, s->Window + s->BlockStart, len, last);
            s->BlockStart += len;
            FlushPending(strm);
        }

        if (last != 0)
            s->BiUsed = 8;
        return last != 0 ? FinishStarted : NeedMore;
    }

    // Opens a static-Huffman block (zlib-ng's QUICK_START_BLOCK macro).
    private static void QuickStartBlock(DeflateState* s, int last)
    {
        s->SendBits((StaticTrees << 1) + last, 3);
        s->BlockOpen = 1 + last;
        s->BlockStart = s->Strstart;
    }

    // Closes the currently open static-Huffman block and flushes pending output (zlib-ng's
    // QUICK_END_BLOCK macro). Returns true if the caller must return `earlyReturn` immediately
    // because avail_out ran out mid-flush; false if the block was closed (or was already closed)
    // and the caller may continue.
    private static bool QuickEndBlock(DeflateState* s, int last, out BlockState earlyReturn)
    {
        if (s->BlockOpen != 0)
        {
            s->SendCode(TreesTables.StaticLtree, EndBlock);
            if (last != 0)
                s->BiWindup();
            s->BlockOpen = 0;
            s->BlockStart = s->Strstart;
            FlushPending(s->Strm);
            if (s->Strm->avail_out == 0)
            {
                earlyReturn = last != 0 ? FinishStarted : NeedMore;
                return true;
            }
        }
        earlyReturn = default;
        return false;
    }

    /// <summary>
    /// deflate_quick: level 1's dedicated fast path (zlib-ng's <c>deflate_quick.c</c>). Emits
    /// directly against the fixed static Huffman tree — no dyn_ltree/dyn_dtree frequency tallying,
    /// no per-block tree construction — and finds matches with a single hash-chain-head probe (no
    /// chain walk), trading a little ratio for a lot of speed. Reuses the existing 4-byte hash
    /// (<see cref="InsertString"/>) and vectorized match length (<see cref="Compare256Helper"/>).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static BlockState DeflateQuick(DeflateState* s, FlushMode flush)
    {
        int last = flush == FlushMode.Finish ? 1 : 0;

        if (last != 0 && s->BlockOpen != 2)
        {
            if (QuickEndBlock(s, 0, out BlockState early))
                return early;
            QuickStartBlock(s, last);
        }
        else if (s->BlockOpen == 0 && s->Lookahead > 0)
        {
            QuickStartBlock(s, last);
        }

        for (;;)
        {
            if (s->Pending + 8 >= s->PendingBufSize)
            {
                FlushPending(s->Strm);
                if (s->Strm->avail_out == 0)
                    return last != 0 && s->Strm->avail_in == 0 && s->BiValid == 0 && s->BlockOpen == 0
                        ? FinishStarted : NeedMore;
            }

            if (s->Lookahead < MinLookahead)
            {
                FillWindow(s);
                if (s->Lookahead < MinLookahead && flush == FlushMode.NoFlush)
                    return NeedMore;
                if (s->Lookahead == 0)
                    break;
                if (s->BlockOpen == 0)
                    QuickStartBlock(s, last);
            }

            if (s->Lookahead >= MinMatch)
            {
                uint hashHead = InsertString(s, s->Strstart);
                long dist = (long)s->Strstart - hashHead;

                if (dist <= MaxDist(s) && dist > 0)
                {
                    byte* strStart = s->Window + s->Strstart;
                    byte* matchStart = s->Window + hashHead;

                    if (Unsafe.ReadUnaligned<ushort>(strStart) == Unsafe.ReadUnaligned<ushort>(matchStart))
                    {
                        uint matchLen = Compare256Helper.Match(strStart + 2, matchStart + 2) + 2;

                        if (matchLen >= MinMatch)
                        {
                            if (matchLen > s->Lookahead)
                                matchLen = s->Lookahead;
                            if (matchLen > MaxMatch)
                                matchLen = MaxMatch;

                            Trees.EmitMatch(s, TreesTables.StaticLtree, TreesTables.StaticDtree,
                                             (int)(matchLen - MinMatch), (uint)dist);
                            s->Lookahead -= matchLen;
                            s->Strstart += matchLen;
                            continue;
                        }
                    }
                }
            }

            s->SendCode(TreesTables.StaticLtree, s->Window[s->Strstart]);
            s->Strstart++;
            s->Lookahead--;
        }

        s->Insert = s->Strstart < MinMatch - 1 ? s->Strstart : MinMatch - 1;
        if (last != 0)
        {
            if (QuickEndBlock(s, 1, out BlockState earlyLast))
                return earlyLast;
            return FinishDone;
        }

        if (QuickEndBlock(s, 0, out BlockState earlyDone))
            return earlyDone;
        return BlockDone;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static BlockState DeflateFast(DeflateState* s, FlushMode flush)
    {
        uint hashHead;
        bool bflush;

        for (;;)
        {
            if (s->Lookahead < MinLookahead)
            {
                FillWindow(s);
                if (s->Lookahead < MinLookahead && flush == FlushMode.NoFlush)
                    return NeedMore;
                if (s->Lookahead == 0)
                    break;
            }

            hashHead = Nil;
            if (s->Lookahead >= MinMatch)
                hashHead = InsertString(s, s->Strstart);

            if (hashHead != Nil && s->Strstart - hashHead <= MaxDist(s))
                s->MatchLength = LongestMatch(s, hashHead);

            if (s->MatchLength >= MinMatch)
            {
                bflush = Trees.TrTally(s, s->Strstart - s->MatchStart, s->MatchLength - MinMatch);
                s->Lookahead -= s->MatchLength;

                if (s->MatchLength <= s->MaxLazyMatch && s->Lookahead >= MinMatch)
                {
                    s->MatchLength--;

                    // s->Window/Head/Prev/WMask/HashBits are all invariant for the rest of this
                    // match (no FillWindow/SlideHash runs inside this loop) — cache them once
                    // instead of reloading through *s on every iteration the way the opaque
                    // InsertString/HashCalc calls do.
                    byte* window = s->Window;
                    ushort* headTable = s->Head;
                    ushort* prevTable = s->Prev;
                    uint wmask = s->WMask;
                    int hashShift = 32 - (int)s->HashBits;

                    do
                    {
                        s->Strstart++;
                        uint str = s->Strstart;
                        uint h = (Unsafe.ReadUnaligned<uint>(window + str) * 2654435761u) >> hashShift;
                        hashHead = prevTable[str & wmask] = headTable[h];
                        headTable[h] = (ushort)str;
                    }
                    while (--s->MatchLength != 0);
                    s->Strstart++;
                }
                else
                {
                    s->Strstart += s->MatchLength;
                    s->MatchLength = 0;
                    // 4-byte hash is stateless — no ins_h reseed needed after a long match.
                }
            }
            else
            {
                bflush = Trees.TrTally(s, 0, s->Window[s->Strstart]);
                s->Lookahead--;
                s->Strstart++;
            }
            if (bflush)
            {
                FlushBlockOnly(s, 0);
                if (s->Strm->avail_out == 0)
                    return NeedMore;
            }
        }

        s->Insert = s->Strstart < MinMatch - 1 ? s->Strstart : MinMatch - 1;
        if (flush == FlushMode.Finish)
        {
            FlushBlockOnly(s, 1);
            return s->Strm->avail_out == 0 ? FinishStarted : FinishDone;
        }
        if (s->SymNext != 0)
        {
            FlushBlockOnly(s, 0);
            if (s->Strm->avail_out == 0)
                return NeedMore;
        }
        return BlockDone;
    }

    private static BlockState DeflateSlow(DeflateState* s, FlushMode flush)
    {
        uint hashHead;
        bool bflush;

        for (;;)
        {
            if (s->Lookahead < MinLookahead)
            {
                FillWindow(s);
                if (s->Lookahead < MinLookahead && flush == FlushMode.NoFlush)
                    return NeedMore;
                if (s->Lookahead == 0)
                    break;
            }

            hashHead = Nil;
            if (s->Lookahead >= MinMatch)
                hashHead = InsertString(s, s->Strstart);

            s->PrevLength = s->MatchLength;
            s->PrevMatch = s->MatchStart;
            s->MatchLength = MinMatch - 1;

            if (hashHead != Nil && s->PrevLength < s->MaxLazyMatch &&
                s->Strstart - hashHead <= MaxDist(s))
            {
                s->MatchLength = s->RollHash != 0 ? LongestMatchSlow(s, hashHead) : LongestMatch(s, hashHead);
                if (s->MatchLength <= 5 && (s->Strategy == (int)CompressionStrategy.Filtered ||
                    (s->MatchLength == MinMatch && s->Strstart - s->MatchStart > TooFar)))
                    s->MatchLength = MinMatch - 1;
            }

            if (s->PrevLength >= MinMatch && s->MatchLength <= s->PrevLength)
            {
                uint maxInsert = s->Strstart + s->Lookahead - MinMatch;
                bflush = Trees.TrTally(s, s->Strstart - 1 - s->PrevMatch, s->PrevLength - MinMatch);
                s->Lookahead -= s->PrevLength - 1;
                s->PrevLength -= 2;
                do
                {
                    if (++s->Strstart <= maxInsert)
                        hashHead = InsertString(s, s->Strstart);
                }
                while (--s->PrevLength != 0);
                s->MatchAvailable = 0;
                s->MatchLength = MinMatch - 1;
                s->Strstart++;
                if (bflush)
                {
                    FlushBlockOnly(s, 0);
                    if (s->Strm->avail_out == 0)
                        return NeedMore;
                }
            }
            else if (s->MatchAvailable != 0)
            {
                bflush = Trees.TrTally(s, 0, s->Window[s->Strstart - 1]);
                if (bflush)
                    FlushBlockOnly(s, 0);
                s->Strstart++;
                s->Lookahead--;
                if (s->Strm->avail_out == 0)
                    return NeedMore;
            }
            else
            {
                s->MatchAvailable = 1;
                s->Strstart++;
                s->Lookahead--;
            }
        }

        if (s->MatchAvailable != 0)
        {
            _ = Trees.TrTally(s, 0, s->Window[s->Strstart - 1]);
            s->MatchAvailable = 0;
        }
        s->Insert = s->Strstart < MinMatch - 1 ? s->Strstart : MinMatch - 1;
        if (flush == FlushMode.Finish)
        {
            FlushBlockOnly(s, 1);
            return s->Strm->avail_out == 0 ? FinishStarted : FinishDone;
        }
        if (s->SymNext != 0)
        {
            FlushBlockOnly(s, 0);
            if (s->Strm->avail_out == 0)
                return NeedMore;
        }
        return BlockDone;
    }

    /// <summary>A pending match/literal decision for <see cref="DeflateMedium"/> — mirrors
    /// zlib-ng's <c>struct match</c>, widened to <see langword="uint"/> to match
    /// <see cref="DeflateState"/>'s own field widths rather than the original's uint16_t.</summary>
    private struct MediumMatch
    {
        public uint MatchStart, MatchLength, Strstart, OrgStart;
    }

    // Tallies a match/literal run for deflate_medium (zlib-ng's emit_match). `match` is taken by
    // value on purpose — the caller's copy (current_match) must not observe these mutations,
    // exactly as the original C passes `struct match match` by value.
    private static bool EmitMediumMatch(DeflateState* s, MediumMatch match)
    {
        if (match.MatchLength < MinMatch)
        {
            bool litFlush = false;
            while (match.MatchLength != 0)
            {
                litFlush |= Trees.TrTally(s, 0, s->Window[match.Strstart]);
                s->Lookahead--;
                match.Strstart++;
                match.MatchLength--;
            }
            return litFlush;
        }
        bool bflush = Trees.TrTally(s, match.Strstart - match.MatchStart, match.MatchLength - MinMatch);
        s->Lookahead -= match.MatchLength;
        return bflush;
    }

    // Inserts hash entries for `count` consecutive positions starting at `str` (zlib-ng's
    // multi-position INSERT_STRING, as opposed to the single-position quick_insert_string).
    private static void InsertStrings(DeflateState* s, uint str, uint count)
    {
        uint end = str + count;
        for (uint idx = str; idx < end; idx++)
            InsertString(s, idx);
    }

    // Registers the hash-chain entries a just-decided match/literal run passes over, without
    // necessarily inserting every position (zlib-ng's insert_match): long matches only get a single
    // insert near their end, trading a little future match quality for a lot less insertion work.
    // `match` is taken by value — same by-value contract as emit_match above.
    private static void InsertMediumMatch(DeflateState* s, MediumMatch match)
    {
        if (s->Lookahead <= match.MatchLength + MinMatch)
            return;

        if (match.MatchLength < MinMatch)
        {
            match.Strstart++;
            match.MatchLength--;
            if (match.MatchLength > 0 && match.Strstart >= match.OrgStart)
            {
                if (match.Strstart + match.MatchLength - 1 >= match.OrgStart)
                    InsertStrings(s, match.Strstart, match.MatchLength);
                else
                    InsertStrings(s, match.Strstart, match.OrgStart - match.Strstart + 1);
            }
            return;
        }

        if (match.MatchLength <= 16 * s->MaxLazyMatch && s->Lookahead >= MinMatch)
        {
            match.MatchLength--;
            match.Strstart++;

            if (match.Strstart >= match.OrgStart)
            {
                if (match.Strstart + match.MatchLength - 1 >= match.OrgStart)
                    InsertStrings(s, match.Strstart, match.MatchLength);
                else
                    InsertStrings(s, match.Strstart, match.OrgStart - match.Strstart + 1);
            }
            else if (match.OrgStart < match.Strstart + match.MatchLength)
            {
                InsertStrings(s, match.OrgStart, match.Strstart + match.MatchLength - match.OrgStart);
            }
        }
        else
        {
            uint newStrstart = match.Strstart + match.MatchLength;
            if (newStrstart >= MinMatch - 2)
                InsertString(s, newStrstart + 2 - MinMatch);
        }
    }

    // Tries to shift the boundary between two adjacent matches left-to-right (zlib-ng's
    // fizzle_matches): if the byte just before `next`'s match source equals the byte just before
    // `next`'s strstart, `current`'s tail can fold into `next`, occasionally erasing `current`
    // entirely (down to a 0-length no-op) in exchange for a longer, cheaper-to-encode `next`.
    private static void FizzleMatches(DeflateState* s, ref MediumMatch current, ref MediumMatch next)
    {
        if (current.MatchLength <= 1)
            return;
        if (current.MatchLength > 1 + next.MatchStart)
            return;
        if (current.MatchLength > 1 + next.Strstart)
            return;

        byte* window = s->Window;
        byte* match = window + (long)next.MatchStart - current.MatchLength + 1;
        byte* orig = window + (long)next.Strstart - current.MatchLength + 1;

        if (*match != *orig)
            return;

        // The walk below starts at (MatchStart-1)/(Strstart-1); bail rather than read before the
        // window buffer, which (unlike its end) carries no padding.
        if (next.MatchStart == 0 || next.Strstart == 0)
            return;

        MediumMatch c = current;
        MediumMatch n = next;

        uint limit = n.Strstart > MaxDist(s) ? n.Strstart - MaxDist(s) : 0;

        match = window + (long)n.MatchStart - 1;
        orig = window + (long)n.Strstart - 1;

        int changed = 0;
        while (*match == *orig)
        {
            if (c.MatchLength < 1)
                break;
            if (n.Strstart <= limit)
                break;
            if (n.MatchLength >= 256)
                break;
            if (n.MatchStart <= 1)
                break;

            n.Strstart--;
            n.MatchStart--;
            n.MatchLength++;
            c.MatchLength--;
            match--;
            orig--;
            changed++;
        }

        if (changed == 0)
            return;

        if (c.MatchLength <= 1 && n.MatchLength != 2)
        {
            n.OrgStart++;
            current = c;
            next = n;
        }
    }

    /// <summary>
    /// deflate_medium: levels 4-9's match-in-match strategy (zlib-ng's <c>deflate_medium.c</c>).
    /// Looks one match ahead of the current position and, via <see cref="FizzleMatches"/>, lets a
    /// short current match fold into a longer following one; long matches get only sparse hash-chain
    /// insertion (<see cref="InsertMediumMatch"/>) instead of one insert per byte. Reuses the
    /// existing chain-walking <see cref="LongestMatch"/> (already SIMD-accelerated) for both the
    /// current and look-ahead match — this is not a new search algorithm, just a different policy
    /// for when to search and how much hash-table bookkeeping to do around it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static BlockState DeflateMedium(DeflateState* s, FlushMode flush)
    {
        bool earlyExit = s->Level < 5;
        var currentMatch = default(MediumMatch);
        var nextMatch = default(MediumMatch);

        for (;;)
        {
            if (s->Lookahead < MinLookahead)
            {
                FillWindow(s);
                if (s->Lookahead < MinLookahead && flush == FlushMode.NoFlush)
                    return NeedMore;
                if (s->Lookahead == 0)
                    break;
                nextMatch.MatchLength = 0;
            }

            if (!earlyExit && nextMatch.MatchLength > 0)
            {
                currentMatch = nextMatch;
                nextMatch.MatchLength = 0;
            }
            else
            {
                uint hashHead = Nil;
                if (s->Lookahead >= MinMatch)
                    hashHead = InsertString(s, s->Strstart);

                currentMatch.Strstart = s->Strstart;
                currentMatch.OrgStart = currentMatch.Strstart;

                long dist = (long)s->Strstart - hashHead;
                if (dist <= MaxDist(s) && dist > 0 && hashHead != Nil)
                {
                    currentMatch.MatchLength = s->RollHash != 0 ? LongestMatchSlow(s, hashHead) : LongestMatch(s, hashHead);
                    currentMatch.MatchStart = s->MatchStart;
                    if (currentMatch.MatchLength < MinMatch)
                        currentMatch.MatchLength = 1;
                    if (currentMatch.MatchStart >= currentMatch.Strstart)
                        currentMatch.MatchLength = 1;
                }
                else
                {
                    currentMatch.MatchStart = 0;
                    currentMatch.MatchLength = 1;
                }
            }

            InsertMediumMatch(s, currentMatch);

            if (!earlyExit && s->Lookahead > MinLookahead &&
                currentMatch.Strstart + currentMatch.MatchLength < s->WindowSize - MinLookahead)
            {
                s->Strstart = currentMatch.Strstart + currentMatch.MatchLength;
                uint hashHead = InsertString(s, s->Strstart);

                nextMatch.Strstart = s->Strstart;
                nextMatch.OrgStart = nextMatch.Strstart;

                long dist = (long)s->Strstart - hashHead;
                if (dist <= MaxDist(s) && dist > 0 && hashHead != Nil)
                {
                    nextMatch.MatchLength = s->RollHash != 0 ? LongestMatchSlow(s, hashHead) : LongestMatch(s, hashHead);
                    nextMatch.MatchStart = s->MatchStart;
                    if (nextMatch.MatchStart >= nextMatch.Strstart)
                        nextMatch.MatchLength = 1;
                    if (nextMatch.MatchLength < MinMatch)
                        nextMatch.MatchLength = 1;
                    else
                        FizzleMatches(s, ref currentMatch, ref nextMatch);
                }
                else
                {
                    nextMatch.MatchStart = 0;
                    nextMatch.MatchLength = 1;
                }

                s->Strstart = currentMatch.Strstart;
            }
            else
            {
                nextMatch.MatchLength = 0;
            }

            bool bflush = EmitMediumMatch(s, currentMatch);
            s->Strstart += currentMatch.MatchLength;

            if (bflush)
            {
                FlushBlockOnly(s, 0);
                if (s->Strm->avail_out == 0)
                    return NeedMore;
            }
        }

        s->Insert = s->Strstart < MinMatch - 1 ? s->Strstart : MinMatch - 1;
        if (flush == FlushMode.Finish)
        {
            FlushBlockOnly(s, 1);
            return s->Strm->avail_out == 0 ? FinishStarted : FinishDone;
        }
        if (s->SymNext != 0)
        {
            FlushBlockOnly(s, 0);
            if (s->Strm->avail_out == 0)
                return NeedMore;
        }
        return BlockDone;
    }

    private static BlockState DeflateRle(DeflateState* s, FlushMode flush)
    {
        bool bflush;
        for (;;)
        {
            if (s->Lookahead <= MaxMatch)
            {
                FillWindow(s);
                if (s->Lookahead <= MaxMatch && flush == FlushMode.NoFlush)
                    return NeedMore;
                if (s->Lookahead == 0)
                    break;
            }

            s->MatchLength = 0;
            if (s->Lookahead >= MinMatch && s->Strstart > 0)
            {
                byte* scan = s->Window + s->Strstart;
                byte prev = scan[-1];
                if (prev == scan[0] && prev == scan[1])
                {
                    s->MatchLength = Compare256Helper.MatchByte(scan + 2, prev) + 2;
                    if (s->MatchLength > s->Lookahead)
                        s->MatchLength = s->Lookahead;
                }
            }

            if (s->MatchLength >= MinMatch)
            {
                bflush = Trees.TrTally(s, 1, s->MatchLength - MinMatch);
                s->Lookahead -= s->MatchLength;
                s->Strstart += s->MatchLength;
                s->MatchLength = 0;
            }
            else
            {
                bflush = Trees.TrTally(s, 0, s->Window[s->Strstart]);
                s->Lookahead--;
                s->Strstart++;
            }
            if (bflush)
            {
                FlushBlockOnly(s, 0);
                if (s->Strm->avail_out == 0)
                    return NeedMore;
            }
        }

        s->Insert = 0;
        if (flush == FlushMode.Finish)
        {
            FlushBlockOnly(s, 1);
            return s->Strm->avail_out == 0 ? FinishStarted : FinishDone;
        }
        if (s->SymNext != 0)
        {
            FlushBlockOnly(s, 0);
            if (s->Strm->avail_out == 0)
                return NeedMore;
        }
        return BlockDone;
    }

    private static BlockState DeflateHuff(DeflateState* s, FlushMode flush)
    {
        bool bflush;
        for (;;)
        {
            if (s->Lookahead == 0)
            {
                FillWindow(s);
                if (s->Lookahead == 0)
                {
                    if (flush == FlushMode.NoFlush)
                        return NeedMore;
                    break;
                }
            }

            s->MatchLength = 0;
            bflush = Trees.TrTally(s, 0, s->Window[s->Strstart]);
            s->Lookahead--;
            s->Strstart++;
            if (bflush)
            {
                FlushBlockOnly(s, 0);
                if (s->Strm->avail_out == 0)
                    return NeedMore;
            }
        }

        s->Insert = 0;
        if (flush == FlushMode.Finish)
        {
            FlushBlockOnly(s, 1);
            return s->Strm->avail_out == 0 ? FinishStarted : FinishDone;
        }
        if (s->SymNext != 0)
        {
            FlushBlockOnly(s, 0);
            if (s->Strm->avail_out == 0)
                return NeedMore;
        }
        return BlockDone;
    }

    /// <summary>Supply a preset dictionary — a faithful port of zlib's <c>deflateSetDictionary</c>.</summary>
    public static ZlibResult SetDictionary(z_stream* strm, byte* dictionary, uint dictLength)
    {
        if (strm == null || strm->state == null)
            return ZlibResult.StreamError;
        var s = (DeflateState*)strm->state;
        int wrap = s->Wrap;
        if (wrap == 2 || (wrap == 1 && s->Status != InitState) || s->Lookahead != 0)
            return ZlibResult.StreamError;

        if (wrap == 1)
            strm->adler = Adler32.Update((uint)strm->adler, dictionary, dictLength);
        s->Wrap = 0;

        byte* dict = dictionary;

        if (dictLength >= s->WSize)
        {
            if (wrap == 0)
            {
                ClearHash(s);
                s->Strstart = 0;
                s->BlockStart = 0;
                s->Insert = 0;
            }
            dict += dictLength - s->WSize;
            dictLength = s->WSize;
        }

        uint availSave = strm->avail_in;
        byte* nextSave = strm->next_in;
        strm->avail_in = dictLength;
        strm->next_in = dict;
        FillWindow(s);
        while (s->Lookahead >= MinMatch)
        {
            uint str = s->Strstart;
            uint n = s->Lookahead - (MinMatch - 1);
            do
            {
                uint h = Hash(s, str);
                s->Prev[str & s->WMask] = s->Head[h];
                s->Head[h] = (ushort)str;
                str++;
            }
            while (--n != 0);
            s->Strstart = str;
            s->Lookahead = MinMatch - 1;
            FillWindow(s);
        }
        s->Strstart += s->Lookahead;
        s->BlockStart = s->Strstart;
        s->Insert = s->Lookahead;
        s->Lookahead = 0;
        s->MatchLength = s->PrevLength = MinMatch - 1;
        s->MatchAvailable = 0;
        strm->next_in = nextSave;
        strm->avail_in = availSave;
        s->Wrap = wrap;
        return ZlibResult.Ok;
    }

    /// <summary>
    /// Upper bound on the compressed size of <paramref name="sourceLen"/> input bytes (zlib's
    /// <c>deflateBound</c>). Conservative (zlib-ng-style, ~14% max expansion) rather than the tight
    /// classic-zlib bound, because <c>deflate_quick</c>/<c>deflate_medium</c> emit static-Huffman-only
    /// blocks with no stored-block fallback for incompressible input.
    /// </summary>
    public static nuint Bound(z_stream* strm, nuint sourceLen)
    {
        int wrapOverhead = strm != null && strm->state != null
            ? ((DeflateState*)strm->state)->Wrap switch { 1 => 6, 2 => 18, _ => 0 }
            : 6;
        return sourceLen + ((sourceLen + 7) >> 3) + ((sourceLen + 63) >> 6) + 5 + (nuint)wrapOverhead;
    }
}
