using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using DnZlib.Internal;
using DnZlib.Raw;

namespace DnZlib.Inflate;

/// <summary>
/// The inflate (decompression) engine — a faithful port of zlib's <c>inflate.c</c>/<c>inffast.c</c>,
/// operating directly on unmanaged <see cref="InflateState"/>/<see cref="z_stream"/> pointers. This
/// is what <c>RawZlib.inflate</c>/<c>inflateSetDictionary</c>/<c>inflateReset</c> call; it never
/// touches any managed wrapper.
/// </summary>
internal static unsafe class InflateEngine
{
    // Permutation of code-length code lengths (RFC 1951), values all < 19 so a byte span suffices.
    private static ReadOnlySpan<byte> CodeLengthOrder =>
        [16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15];

    public static ZlibResult Reset2(z_stream* strm, InflateState* st, int windowBits)
    {
        int wrap;
        if (windowBits < 0)
        {
            if (windowBits < -15)
                return ZlibResult.StreamError;
            wrap = 0;
            windowBits = -windowBits;
        }
        else
        {
            wrap = (windowBits >> 4) + 5;
            if (windowBits < 48)
                windowBits &= 15;
        }

        if (windowBits != 0 && (windowBits < 8 || windowBits > 15))
            return ZlibResult.StreamError;

        st->FreeWindowIfResized((uint)windowBits);
        st->Wrap = wrap;
        st->WBits = (uint)windowBits;
        return Reset(strm, st);
    }

    public static ZlibResult Reset(z_stream* strm, InflateState* st)
    {
        st->WSize = 0;
        st->WHave = 0;
        st->WNext = 0;
        return ResetKeep(strm, st);
    }

    public static ZlibResult ResetKeep(z_stream* strm, InflateState* st)
    {
        strm->total_in = strm->total_out = 0;
        st->Total = 0;
        strm->msg = null;
        if (st->Wrap != 0)
            strm->adler = (uint)(st->Wrap & 1);
        st->Mode = InflateMode.Head;
        st->Last = 0;
        st->HaveDict = 0;
        st->Flags = -1;
        st->Dmax = 32768;
        st->Head = null;
        st->Hold = 0;
        st->Bits = 0;
        st->LenCode = st->DistCode = st->Next = st->Codes;
        st->Sane = 1;
        st->Back = -1;
        return ZlibResult.Ok;
    }

    /// <summary>Supply a preset dictionary — a faithful port of zlib's <c>inflateSetDictionary</c>.</summary>
    public static ZlibResult SetDictionary(z_stream* strm, InflateState* st, byte* dictionary, uint dictLength)
    {
        if (st->Wrap != 0 && st->Mode != InflateMode.Dict)
            return ZlibResult.StreamError;

        if (st->Mode == InflateMode.Dict)
        {
            uint dictid = Adler32.Compute(new ReadOnlySpan<byte>(dictionary, (int)dictLength));
            if (dictid != st->Check)
                return ZlibResult.DataError;
        }

        UpdateWindow(st, dictionary + dictLength, dictLength);
        st->HaveDict = 1;
        return ZlibResult.Ok;
    }

    /// <summary>The inflate entry point — a faithful port of zlib's <c>inflate()</c>.</summary>
    public static ZlibResult Run(z_stream* strm, FlushMode flush)
    {
        var st = (InflateState*)strm->state;

        if (strm->next_out == null && strm->avail_out != 0)
            return ZlibResult.StreamError;

        if (st->Mode == InflateMode.Type)
            st->Mode = InflateMode.TypeDo; // skip check

        // LOAD()
        byte* next = strm->next_in;
        uint have = strm->avail_in;
        byte* put = strm->next_out;
        uint left = strm->avail_out;
        ulong hold = st->Hold;
        uint bits = st->Bits;

        uint inStart = have;
        uint outCheckpoint = left;
        ZlibResult ret = ZlibResult.Ok;
        uint copy;
        uint len;
        byte* from;
        Code here;
        Code last;

        for (;;)
        {
            switch (st->Mode)
            {
                case InflateMode.Head:
                    if (st->Wrap == 0)
                    {
                        st->Mode = InflateMode.TypeDo;
                        break;
                    }
                    while (bits < 16) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                    if ((st->Wrap & 2) != 0 && hold == 0x8b1f)
                    {
                        if (st->WBits == 0)
                            st->WBits = 15;
                        st->Check = Crc32.Compute(ReadOnlySpan<byte>.Empty);
                        CrcBytes(st, hold, 2);
                        hold = 0; bits = 0;
                        st->Mode = InflateMode.Flags;
                        break;
                    }
                    st->Flags = 0; // expect zlib unless proven raw invalid
                    if ((st->Wrap & 1) == 0 ||
                        ((((uint)hold & 0xff) << 8) + (uint)((hold >> 8) & 0xff)) % 31 != 0)
                    {
                        strm->msg = Messages.IncorrectHeaderCheck;
                        st->Mode = InflateMode.Bad;
                        break;
                    }
                    if (((uint)hold & 0x0f) != ZlibConstants.ZDeflated)
                    {
                        strm->msg = Messages.UnknownCompressionMethod;
                        st->Mode = InflateMode.Bad;
                        break;
                    }
                    hold >>= 4; bits -= 4;
                    len = ((uint)hold & 0x0f) + 8;
                    if (st->WBits == 0)
                        st->WBits = len;
                    if (len > 15 || len > st->WBits)
                    {
                        strm->msg = Messages.InvalidWindowSize;
                        st->Mode = InflateMode.Bad;
                        break;
                    }
                    st->Dmax = 1u << (int)len;
                    st->Flags = 0;
                    strm->adler = st->Check = Adler32.Compute(ReadOnlySpan<byte>.Empty);
                    st->Mode = ((uint)hold & 0x200) != 0 ? InflateMode.DictId : InflateMode.Type;
                    hold = 0; bits = 0;
                    break;

                case InflateMode.Flags:
                    while (bits < 16) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                    st->Flags = (int)(uint)hold;
                    if ((st->Flags & 0xff) != ZlibConstants.ZDeflated)
                    {
                        strm->msg = Messages.UnknownCompressionMethod;
                        st->Mode = InflateMode.Bad;
                        break;
                    }
                    if ((st->Flags & 0xe000) != 0)
                    {
                        strm->msg = Messages.UnknownHeaderFlagsSet;
                        st->Mode = InflateMode.Bad;
                        break;
                    }
                    if ((st->Flags & 0x0200) != 0 && (st->Wrap & 4) != 0)
                        CrcBytes(st, hold, 2);
                    hold = 0; bits = 0;
                    st->Mode = InflateMode.Time;
                    goto case InflateMode.Time;

                case InflateMode.Time:
                    while (bits < 32) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                    if ((st->Flags & 0x0200) != 0 && (st->Wrap & 4) != 0)
                        CrcBytes(st, hold, 4);
                    hold = 0; bits = 0;
                    st->Mode = InflateMode.Os;
                    goto case InflateMode.Os;

                case InflateMode.Os:
                    while (bits < 16) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                    if ((st->Flags & 0x0200) != 0 && (st->Wrap & 4) != 0)
                        CrcBytes(st, hold, 2);
                    hold = 0; bits = 0;
                    st->Mode = InflateMode.ExLen;
                    goto case InflateMode.ExLen;

                case InflateMode.ExLen:
                    if ((st->Flags & 0x0400) != 0)
                    {
                        while (bits < 16) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                        st->Length = (uint)hold;
                        if ((st->Flags & 0x0200) != 0 && (st->Wrap & 4) != 0)
                            CrcBytes(st, hold, 2);
                        hold = 0; bits = 0;
                    }
                    st->Mode = InflateMode.Extra;
                    goto case InflateMode.Extra;

                case InflateMode.Extra:
                    if ((st->Flags & 0x0400) != 0)
                    {
                        copy = st->Length;
                        if (copy > have) copy = have;
                        if (copy != 0)
                        {
                            if ((st->Flags & 0x0200) != 0 && (st->Wrap & 4) != 0)
                                st->Check = Crc32.Update(st->Check, next, copy);
                            have -= copy;
                            next += copy;
                            st->Length -= copy;
                        }
                        if (st->Length != 0)
                            goto inf_leave;
                    }
                    st->Length = 0;
                    st->Mode = InflateMode.Name;
                    goto case InflateMode.Name;

                case InflateMode.Name:
                    if ((st->Flags & 0x0800) != 0)
                    {
                        if (have == 0) goto inf_leave;
                        copy = 0;
                        do
                        {
                            len = next[copy++];
                        }
                        while (len != 0 && copy < have);
                        if ((st->Flags & 0x0200) != 0 && (st->Wrap & 4) != 0)
                            st->Check = Crc32.Update(st->Check, next, copy);
                        have -= copy;
                        next += copy;
                        if (len != 0) goto inf_leave;
                    }
                    st->Length = 0;
                    st->Mode = InflateMode.Comment;
                    goto case InflateMode.Comment;

                case InflateMode.Comment:
                    if ((st->Flags & 0x1000) != 0)
                    {
                        if (have == 0) goto inf_leave;
                        copy = 0;
                        do
                        {
                            len = next[copy++];
                        }
                        while (len != 0 && copy < have);
                        if ((st->Flags & 0x0200) != 0 && (st->Wrap & 4) != 0)
                            st->Check = Crc32.Update(st->Check, next, copy);
                        have -= copy;
                        next += copy;
                        if (len != 0) goto inf_leave;
                    }
                    st->Mode = InflateMode.Hcrc;
                    goto case InflateMode.Hcrc;

                case InflateMode.Hcrc:
                    if ((st->Flags & 0x0200) != 0)
                    {
                        while (bits < 16) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                        if ((st->Wrap & 4) != 0 && (uint)hold != (st->Check & 0xffff))
                        {
                            strm->msg = Messages.HeaderCrcMismatch;
                            st->Mode = InflateMode.Bad;
                            break;
                        }
                        hold = 0; bits = 0;
                    }
                    strm->adler = st->Check = Crc32.Compute(ReadOnlySpan<byte>.Empty);
                    st->Mode = InflateMode.Type;
                    break;

                case InflateMode.DictId:
                    while (bits < 32) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                    strm->adler = st->Check = BinaryPrimitives.ReverseEndianness((uint)hold);
                    hold = 0; bits = 0;
                    st->Mode = InflateMode.Dict;
                    goto case InflateMode.Dict;

                case InflateMode.Dict:
                    if (st->HaveDict == 0)
                    {
                        // RESTORE and return, awaiting InflateSetDictionary.
                        goto restore_and_needdict;
                    }
                    strm->adler = st->Check = Adler32.Compute(ReadOnlySpan<byte>.Empty);
                    st->Mode = InflateMode.Type;
                    goto case InflateMode.Type;

                case InflateMode.Type:
                    if (flush == FlushMode.Block || flush == FlushMode.Trees)
                        goto inf_leave;
                    goto case InflateMode.TypeDo;

                case InflateMode.TypeDo:
                    if (st->Last != 0)
                    {
                        hold >>= (int)(bits & 7); bits -= bits & 7;
                        st->Mode = InflateMode.Check;
                        break;
                    }
                    while (bits < 3) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                    st->Last = (int)((uint)hold & 1);
                    hold >>= 1; bits -= 1;
                    switch ((uint)hold & 3)
                    {
                        case 0:
                            st->Mode = InflateMode.Stored;
                            break;
                        case 1:
                            InfTrees.GetFixedTables(out Code* lc, out uint lb, out Code* dc, out uint db);
                            st->LenCode = lc; st->LenBits = lb; st->DistCode = dc; st->DistBits = db;
                            st->Mode = InflateMode.Len_;
                            if (flush == FlushMode.Trees)
                            {
                                hold >>= 2; bits -= 2;
                                goto inf_leave;
                            }
                            break;
                        case 2:
                            st->Mode = InflateMode.Table;
                            break;
                        default:
                            strm->msg = Messages.InvalidBlockType;
                            st->Mode = InflateMode.Bad;
                            break;
                    }
                    hold >>= 2; bits -= 2;
                    break;

                case InflateMode.Stored:
                    hold >>= (int)(bits & 7); bits -= bits & 7; // byte-align
                    while (bits < 32) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                    if (((uint)hold & 0xffff) != (((uint)hold >> 16) ^ 0xffff))
                    {
                        strm->msg = Messages.InvalidStoredBlockLengths;
                        st->Mode = InflateMode.Bad;
                        break;
                    }
                    st->Length = (uint)hold & 0xffff;
                    hold = 0; bits = 0;
                    st->Mode = InflateMode.Copy_;
                    if (flush == FlushMode.Trees)
                        goto inf_leave;
                    goto case InflateMode.Copy_;

                case InflateMode.Copy_:
                    st->Mode = InflateMode.Copy;
                    goto case InflateMode.Copy;

                case InflateMode.Copy:
                    copy = st->Length;
                    if (copy != 0)
                    {
                        if (copy > have) copy = have;
                        if (copy > left) copy = left;
                        if (copy == 0) goto inf_leave;
                        Buffer.MemoryCopy(next, put, copy, copy);
                        have -= copy;
                        next += copy;
                        left -= copy;
                        put += copy;
                        st->Length -= copy;
                        break;
                    }
                    st->Mode = InflateMode.Type;
                    break;

                case InflateMode.Table:
                    while (bits < 14) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                    st->NLen = ((uint)hold & 0x1f) + 257;
                    hold >>= 5; bits -= 5;
                    st->NDist = ((uint)hold & 0x1f) + 1;
                    hold >>= 5; bits -= 5;
                    st->NCode = ((uint)hold & 0x0f) + 4;
                    hold >>= 4; bits -= 4;
                    if (st->NLen > 286 || st->NDist > 30)
                    {
                        strm->msg = Messages.TooManyLengthOrDistanceSymbols;
                        st->Mode = InflateMode.Bad;
                        break;
                    }
                    st->Have = 0;
                    st->Mode = InflateMode.LenLens;
                    goto case InflateMode.LenLens;

                case InflateMode.LenLens:
                    while (st->Have < st->NCode)
                    {
                        while (bits < 3) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                        st->Lens[CodeLengthOrder[(int)st->Have++]] = (ushort)((uint)hold & 7);
                        hold >>= 3; bits -= 3;
                    }
                    while (st->Have < 19)
                        st->Lens[CodeLengthOrder[(int)st->Have++]] = 0;
                    st->Next = st->Codes;
                    st->LenCode = st->Next;
                    st->LenBits = 7;
                    {
                        Code* tbl = st->Next;
                        uint tblBits = st->LenBits;
                        int rc = InfTrees.BuildTable(CodeType.Codes, st->Lens, 19, ref tbl, ref tblBits, st->Work);
                        st->Next = tbl;
                        st->LenBits = tblBits;
                        if (rc != 0)
                        {
                            strm->msg = Messages.InvalidCodeLengthsSet;
                            st->Mode = InflateMode.Bad;
                            break;
                        }
                    }
                    st->Have = 0;
                    st->Mode = InflateMode.CodeLens;
                    goto case InflateMode.CodeLens;

                case InflateMode.CodeLens:
                    while (st->Have < st->NLen + st->NDist)
                    {
                        for (;;)
                        {
                            here = st->LenCode[(uint)hold & ((1u << (int)st->LenBits) - 1)];
                            if (here.Bits <= bits) break;
                            if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8;
                        }
                        if (here.Val < 16)
                        {
                            hold >>= here.Bits; bits -= here.Bits;
                            st->Lens[st->Have++] = here.Val;
                        }
                        else
                        {
                            if (here.Val == 16)
                            {
                                int need = here.Bits + 2;
                                while (bits < need) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                                hold >>= here.Bits; bits -= here.Bits;
                                if (st->Have == 0)
                                {
                                    strm->msg = Messages.InvalidBitLengthRepeat;
                                    st->Mode = InflateMode.Bad;
                                    break;
                                }
                                len = st->Lens[st->Have - 1];
                                copy = 3 + ((uint)hold & 3);
                                hold >>= 2; bits -= 2;
                            }
                            else if (here.Val == 17)
                            {
                                int need = here.Bits + 3;
                                while (bits < need) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                                hold >>= here.Bits; bits -= here.Bits;
                                len = 0;
                                copy = 3 + ((uint)hold & 7);
                                hold >>= 3; bits -= 3;
                            }
                            else
                            {
                                int need = here.Bits + 7;
                                while (bits < need) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                                hold >>= here.Bits; bits -= here.Bits;
                                len = 0;
                                copy = 11 + ((uint)hold & 0x7f);
                                hold >>= 7; bits -= 7;
                            }
                            if (st->Have + copy > st->NLen + st->NDist)
                            {
                                strm->msg = Messages.InvalidBitLengthRepeat;
                                st->Mode = InflateMode.Bad;
                                break;
                            }
                            while (copy-- != 0)
                                st->Lens[st->Have++] = (ushort)len;
                        }
                    }

                    if (st->Mode == InflateMode.Bad) break;

                    if (st->Lens[256] == 0)
                    {
                        strm->msg = Messages.InvalidCodeMissingEndOfBlock;
                        st->Mode = InflateMode.Bad;
                        break;
                    }

                    st->Next = st->Codes;
                    st->LenCode = st->Next;
                    st->LenBits = 9;
                    {
                        Code* tbl = st->Next;
                        uint tblBits = st->LenBits;
                        int rc = InfTrees.BuildTable(CodeType.Lens, st->Lens, st->NLen, ref tbl, ref tblBits, st->Work);
                        st->Next = tbl;
                        st->LenBits = tblBits;
                        if (rc != 0)
                        {
                            strm->msg = Messages.InvalidLiteralLengthsSet;
                            st->Mode = InflateMode.Bad;
                            break;
                        }
                    }
                    st->DistCode = st->Next;
                    st->DistBits = 6;
                    {
                        Code* tbl = st->Next;
                        uint tblBits = st->DistBits;
                        int rc = InfTrees.BuildTable(CodeType.Dists, st->Lens + st->NLen, st->NDist, ref tbl, ref tblBits, st->Work);
                        st->Next = tbl;
                        st->DistBits = tblBits;
                        if (rc != 0)
                        {
                            strm->msg = Messages.InvalidDistancesSet;
                            st->Mode = InflateMode.Bad;
                            break;
                        }
                    }
                    st->Mode = InflateMode.Len_;
                    if (flush == FlushMode.Trees)
                        goto inf_leave;
                    goto case InflateMode.Len_;

                case InflateMode.Len_:
                    st->Mode = InflateMode.Len;
                    goto case InflateMode.Len;

                case InflateMode.Len:
                    if (have >= 8 && left >= 258)
                    {
                        // RESTORE, run the fast loop on strm fields, then LOAD back.
                        strm->next_in = next; strm->avail_in = have; strm->next_out = put; strm->avail_out = left;
                        st->Hold = hold; st->Bits = bits;
                        Fast(strm, outCheckpoint);
                        next = strm->next_in; have = strm->avail_in; put = strm->next_out; left = strm->avail_out;
                        hold = st->Hold; bits = st->Bits;
                        if (st->Mode == InflateMode.Type)
                            st->Back = -1;
                        break;
                    }
                    st->Back = 0;
                    for (;;)
                    {
                        here = st->LenCode[(uint)hold & ((1u << (int)st->LenBits) - 1)];
                        if (here.Bits <= bits) break;
                        if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8;
                    }
                    if (here.Op != 0 && (here.Op & 0xf0) == 0)
                    {
                        last = here;
                        for (;;)
                        {
                            uint idx = last.Val + (((uint)hold & ((1u << (last.Bits + last.Op)) - 1)) >> last.Bits);
                            here = st->LenCode[idx];
                            if (last.Bits + here.Bits <= bits) break;
                            if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8;
                        }
                        hold >>= last.Bits; bits -= last.Bits;
                        st->Back += last.Bits;
                    }
                    hold >>= here.Bits; bits -= here.Bits;
                    st->Back += here.Bits;
                    st->Length = here.Val;
                    if (here.Op == 0)
                    {
                        st->Mode = InflateMode.Lit;
                        break;
                    }
                    if ((here.Op & 32) != 0)
                    {
                        st->Back = -1;
                        st->Mode = InflateMode.Type;
                        break;
                    }
                    if ((here.Op & 64) != 0)
                    {
                        strm->msg = Messages.InvalidLiteralLengthCode;
                        st->Mode = InflateMode.Bad;
                        break;
                    }
                    st->Extra = (uint)here.Op & 15;
                    st->Mode = InflateMode.LenExt;
                    goto case InflateMode.LenExt;

                case InflateMode.LenExt:
                    if (st->Extra != 0)
                    {
                        int need = (int)st->Extra;
                        while (bits < need) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                        st->Length += (uint)hold & ((1u << (int)st->Extra) - 1);
                        hold >>= (int)st->Extra; bits -= st->Extra;
                        st->Back += (int)st->Extra;
                    }
                    st->Was = st->Length;
                    st->Mode = InflateMode.Dist;
                    goto case InflateMode.Dist;

                case InflateMode.Dist:
                    for (;;)
                    {
                        here = st->DistCode[(uint)hold & ((1u << (int)st->DistBits) - 1)];
                        if (here.Bits <= bits) break;
                        if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8;
                    }
                    if ((here.Op & 0xf0) == 0)
                    {
                        last = here;
                        for (;;)
                        {
                            uint idx = last.Val + (((uint)hold & ((1u << (last.Bits + last.Op)) - 1)) >> last.Bits);
                            here = st->DistCode[idx];
                            if (last.Bits + here.Bits <= bits) break;
                            if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8;
                        }
                        hold >>= last.Bits; bits -= last.Bits;
                        st->Back += last.Bits;
                    }
                    hold >>= here.Bits; bits -= here.Bits;
                    st->Back += here.Bits;
                    if ((here.Op & 64) != 0)
                    {
                        strm->msg = Messages.InvalidDistanceCode;
                        st->Mode = InflateMode.Bad;
                        break;
                    }
                    st->Offset = here.Val;
                    st->Extra = (uint)here.Op & 15;
                    st->Mode = InflateMode.DistExt;
                    goto case InflateMode.DistExt;

                case InflateMode.DistExt:
                    if (st->Extra != 0)
                    {
                        int need = (int)st->Extra;
                        while (bits < need) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                        st->Offset += (uint)hold & ((1u << (int)st->Extra) - 1);
                        hold >>= (int)st->Extra; bits -= st->Extra;
                        st->Back += (int)st->Extra;
                    }
                    st->Mode = InflateMode.Match;
                    goto case InflateMode.Match;

                case InflateMode.Match:
                    if (left == 0) goto inf_leave;
                    copy = outCheckpoint - left;
                    bool windowSourced = st->Offset > copy;
                    if (windowSourced)
                    {
                        copy = st->Offset - copy;
                        if (copy > st->WHave)
                        {
                            if (st->Sane != 0)
                            {
                                strm->msg = Messages.InvalidDistanceTooFarBack;
                                st->Mode = InflateMode.Bad;
                                break;
                            }
                        }
                        if (copy > st->WNext)
                        {
                            copy -= st->WNext;
                            from = st->Window + (st->WSize - copy);
                        }
                        else
                        {
                            from = st->Window + (st->WNext - copy);
                        }
                        if (copy > st->Length) copy = st->Length;
                    }
                    else
                    {
                        from = null; // unused: CopyOverlapping recomputes put - st->Offset itself.
                        copy = st->Length;
                    }
                    if (copy > left) copy = left;
                    byte* safeEnd = put + left;
                    left -= copy;
                    st->Length -= copy;
                    put = windowSourced
                        ? ChunkCopyHelper.CopyDisjoint(put, from, copy, safeEnd)
                        : ChunkCopyHelper.CopyOverlapping(put, st->Offset, copy, safeEnd);
                    if (st->Length == 0) st->Mode = InflateMode.Len;
                    break;

                case InflateMode.Lit:
                    if (left == 0) goto inf_leave;
                    *put++ = (byte)st->Length;
                    left--;
                    st->Mode = InflateMode.Len;
                    break;

                case InflateMode.Check:
                    if (st->Wrap != 0)
                    {
                        while (bits < 32) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                        outCheckpoint -= left;
                        strm->total_out += outCheckpoint;
                        st->Total += outCheckpoint;
                        if ((st->Wrap & 4) != 0 && outCheckpoint != 0)
                            strm->adler = st->Check = UpdateCheck(st, st->Check, put - outCheckpoint, outCheckpoint);
                        outCheckpoint = left;
                        uint want = st->Flags != 0 ? (uint)hold : BinaryPrimitives.ReverseEndianness((uint)hold);
                        if ((st->Wrap & 4) != 0 && want != st->Check)
                        {
                            strm->msg = Messages.IncorrectDataCheck;
                            st->Mode = InflateMode.Bad;
                            break;
                        }
                        hold = 0; bits = 0;
                    }
                    st->Mode = InflateMode.Length;
                    goto case InflateMode.Length;

                case InflateMode.Length:
                    if (st->Wrap != 0 && st->Flags != 0)
                    {
                        while (bits < 32) { if (have == 0) goto inf_leave; have--; hold |= (ulong)(*next++) << (int)bits; bits += 8; }
                        if ((st->Wrap & 4) != 0 && (uint)hold != (st->Total & 0xffffffff))
                        {
                            strm->msg = Messages.IncorrectLengthCheck;
                            st->Mode = InflateMode.Bad;
                            break;
                        }
                        hold = 0; bits = 0;
                    }
                    st->Mode = InflateMode.Done;
                    goto case InflateMode.Done;

                case InflateMode.Done:
                    ret = ZlibResult.StreamEnd;
                    goto inf_leave;

                case InflateMode.Bad:
                    ret = ZlibResult.DataError;
                    goto inf_leave;

                case InflateMode.Mem:
                    // RESTORE then return.
                    st->Hold = hold; st->Bits = bits;
                    strm->next_in = next; strm->avail_in = have; strm->next_out = put; strm->avail_out = left;
                    return ZlibResult.MemError;

                default:
                    st->Hold = hold; st->Bits = bits;
                    strm->next_in = next; strm->avail_in = have; strm->next_out = put; strm->avail_out = left;
                    return ZlibResult.StreamError;
            }
        }

    restore_and_needdict:
        st->Hold = hold; st->Bits = bits;
        strm->next_in = next; strm->avail_in = have; strm->next_out = put; strm->avail_out = left;
        return ZlibResult.NeedDict;

    inf_leave:
        // RESTORE()
        strm->next_out = put; strm->avail_out = left; strm->next_in = next; strm->avail_in = have;
        st->Hold = hold; st->Bits = bits;

        if (st->WSize != 0 ||
            (outCheckpoint != strm->avail_out && st->Mode < InflateMode.Bad &&
             (st->Mode < InflateMode.Check || flush != FlushMode.Finish)))
        {
            UpdateWindow(st, strm->next_out, outCheckpoint - strm->avail_out);
        }

        inStart -= strm->avail_in;
        outCheckpoint -= strm->avail_out;
        strm->total_in += inStart;
        strm->total_out += outCheckpoint;
        st->Total += outCheckpoint;
        if ((st->Wrap & 4) != 0 && outCheckpoint != 0)
            strm->adler = st->Check = UpdateCheck(st, st->Check, strm->next_out - outCheckpoint, outCheckpoint);

        if (((inStart == 0 && outCheckpoint == 0) || flush == FlushMode.Finish) && ret == ZlibResult.Ok)
            ret = ZlibResult.BufError;
        return ret;
    }

    private static uint UpdateCheck(InflateState* st, uint check, byte* buf, nuint len)
        => st->Flags != 0 ? Crc32.Update(check, buf, len) : Adler32.Update(check, buf, len);

    private static void CrcBytes(InflateState* st, ulong word, int n)
    {
        byte* buf = stackalloc byte[4];
        for (int i = 0; i < n; i++)
            buf[i] = (byte)(word >> (8 * i));
        st->Check = Crc32.Update(st->Check, buf, (nuint)n);
    }

    private static void UpdateWindow(InflateState* st, byte* end, uint copy)
    {
        byte* window = st->EnsureWindow();

        if (st->WSize == 0)
        {
            st->WSize = 1u << (int)st->WBits;
            st->WNext = 0;
            st->WHave = 0;
        }

        if (copy >= st->WSize)
        {
            Buffer.MemoryCopy(end - st->WSize, window, st->WSize, st->WSize);
            st->WNext = 0;
            st->WHave = st->WSize;
        }
        else
        {
            uint dist = st->WSize - st->WNext;
            if (dist > copy) dist = copy;
            Buffer.MemoryCopy(end - copy, window + st->WNext, dist, dist);
            copy -= dist;
            if (copy != 0)
            {
                Buffer.MemoryCopy(end - copy, window, copy, copy);
                st->WNext = copy;
                st->WHave = st->WSize;
            }
            else
            {
                st->WNext += dist;
                if (st->WNext == st->WSize) st->WNext = 0;
                if (st->WHave < st->WSize) st->WHave += dist;
            }
        }
    }

    /// <summary>
    /// The inflate fast loop — a libdeflate-style single-refill variant of zlib's <c>inflate_fast</c>.
    /// Every iteration starts by pulling a full 8 bytes into <c>hold</c>, which covers the worst-case
    /// symbol (litlen ≤ 15 + len-extra ≤ 5 + dist ≤ 15 + dist-extra ≤ 13 = 48 bits) — so no
    /// per-code-word bit-refills are needed. The caller must guarantee avail_in ≥ 8 and avail_out ≥
    /// 258; the tail (avail_in ∈ [0,8)) is decoded by the scalar loop in the caller.
    /// </summary>
    private static void Fast(z_stream* strm, uint start)
    {
        var st = (InflateState*)strm->state;

        byte* inp = strm->next_in;
        byte* lastp = inp + (strm->avail_in - 8);
        byte* outp = strm->next_out;
        byte* begp = outp - (start - strm->avail_out);
        byte* endp = outp + (strm->avail_out - 257);
        byte* safeEnd = outp + strm->avail_out;
        uint wsize = st->WSize;
        uint whave = st->WHave;
        uint wnext = st->WNext;
        byte* window = st->Window;
        ulong hold = st->Hold;
        uint bits = st->Bits;
        Code* lcode = st->LenCode;
        Code* dcode = st->DistCode;
        uint lmask = (1u << (int)st->LenBits) - 1;
        uint dmask = (1u << (int)st->DistBits) - 1;
        Code here;
        uint op, len, dist;
        byte* from;

        do
        {
            // libdeflate-style single refill: read a whole ulong, shift it into hold's free bits
            // (bits ∈ [0,63], so `<< bits` is well-defined), then advance inp by only the bytes
            // fully consumed. `bits |= 56` equals `bits + 8*((63-bits)>>3)` for bits ∈ [0,63] and
            // leaves 56–63 bits buffered — enough for one full symbol (litlen ≤ 15, len-extra ≤ 5,
            // dist ≤ 15, dist-extra ≤ 13, total ≤ 48). Subtable jumps (`goto dolen`/`goto dodist`)
            // re-index the same hold and don't consume input, so they remain covered too.
            hold |= Unsafe.ReadUnaligned<ulong>(inp) << (int)bits;
            inp += (63 - bits) >> 3;
            bits |= 56;
            here = lcode[hold & lmask];

        dolen:
            op = here.Bits;
            hold >>= (int)op; bits -= op;
            op = here.Op;
            if (op == 0)
            {
                *outp++ = (byte)here.Val;
            }
            else if ((op & 16) != 0)
            {
                len = here.Val;
                op &= 15;
                if (op != 0)
                {
                    len += (uint)hold & ((1u << (int)op) - 1);
                    hold >>= (int)op; bits -= op;
                }
                here = dcode[hold & dmask];

            dodist:
                op = here.Bits;
                hold >>= (int)op; bits -= op;
                op = here.Op;
                if ((op & 16) != 0)
                {
                    dist = here.Val;
                    op &= 15;
                    dist += (uint)hold & ((1u << (int)op) - 1);
                    hold >>= (int)op; bits -= op;

                    op = (uint)(outp - begp);
                    if (dist > op)
                    {
                        // Copy (some) bytes from the sliding window.
                        op = dist - op;
                        if (op > whave && st->Sane != 0)
                        {
                            strm->msg = Messages.InvalidDistanceTooFarBack;
                            st->Mode = InflateMode.Bad;
                            break;
                        }
                        from = window;
                        if (wnext == 0)
                        {
                            from += wsize - op;
                            if (op < len)
                            {
                                len -= op;
                                outp = ChunkCopyHelper.CopyDisjoint(outp, from, op, safeEnd);
                                outp = ChunkCopyHelper.CopyOverlapping(outp, dist, len, safeEnd);
                            }
                            else
                            {
                                outp = ChunkCopyHelper.CopyDisjoint(outp, from, len, safeEnd);
                            }
                        }
                        else if (wnext < op)
                        {
                            from += wsize + wnext - op;
                            op -= wnext;
                            if (op < len)
                            {
                                len -= op;
                                outp = ChunkCopyHelper.CopyDisjoint(outp, from, op, safeEnd);
                                from = window;
                                if (wnext < len)
                                {
                                    op = wnext;
                                    len -= op;
                                    outp = ChunkCopyHelper.CopyDisjoint(outp, from, op, safeEnd);
                                    outp = ChunkCopyHelper.CopyOverlapping(outp, dist, len, safeEnd);
                                }
                                else
                                {
                                    outp = ChunkCopyHelper.CopyDisjoint(outp, from, len, safeEnd);
                                }
                            }
                            else
                            {
                                outp = ChunkCopyHelper.CopyDisjoint(outp, from, len, safeEnd);
                            }
                        }
                        else
                        {
                            from += wnext - op;
                            if (op < len)
                            {
                                len -= op;
                                outp = ChunkCopyHelper.CopyDisjoint(outp, from, op, safeEnd);
                                outp = ChunkCopyHelper.CopyOverlapping(outp, dist, len, safeEnd);
                            }
                            else
                            {
                                outp = ChunkCopyHelper.CopyDisjoint(outp, from, len, safeEnd);
                            }
                        }
                    }
                    else
                    {
                        // Copy directly from already-emitted output (minimum length is three).
                        outp = ChunkCopyHelper.CopyOverlapping(outp, dist, len, safeEnd);
                    }
                }
                else if ((op & 64) == 0)
                {
                    here = dcode[here.Val + ((uint)hold & ((1u << (int)op) - 1))];
                    goto dodist;
                }
                else
                {
                    strm->msg = Messages.InvalidDistanceCode;
                    st->Mode = InflateMode.Bad;
                    break;
                }
            }
            else if ((op & 64) == 0)
            {
                here = lcode[here.Val + ((uint)hold & ((1u << (int)op) - 1))];
                goto dolen;
            }
            else if ((op & 32) != 0)
            {
                st->Mode = InflateMode.Type;
                break;
            }
            else
            {
                strm->msg = Messages.InvalidLiteralLengthCode;
                st->Mode = InflateMode.Bad;
                break;
            }
        }
        while (inp < lastp && outp < endp);

        // Return unused whole bytes to the input. The single 8-byte refill leaves 56–63 buffered
        // bits after each read, so we may need to rewind up to 7 bytes here (vs. <8 bits in the
        // classic two-byte-refill port). Both `lastp` deltas mirror the -8 margin above.
        len = bits >> 3;
        inp -= len;
        bits -= len << 3;
        hold &= (1uL << (int)bits) - 1;

        strm->next_in = inp;
        strm->next_out = outp;
        strm->avail_in = (uint)(inp < lastp ? 8 + (lastp - inp) : 8 - (inp - lastp));
        strm->avail_out = (uint)(outp < endp ? 257 + (endp - outp) : 257 - (outp - endp));
        st->Hold = hold;
        st->Bits = bits;
    }
}
