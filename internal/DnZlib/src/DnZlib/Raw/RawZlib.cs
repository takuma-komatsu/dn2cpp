using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using DnZlib.Deflate;
using DnZlib.Inflate;
using DnZlib.Internal;

namespace DnZlib.Raw;

/// <summary>
/// The zlib-compatible low-level API — the exact function shapes zlib.h declares
/// (<c>deflateInit2_</c>, <c>deflate</c>, <c>inflate</c>, <c>compress2</c>, <c>adler32</c>, ...),
/// implemented in Pure C# rather than bound via <c>DllImport</c>. Nothing here actually calls into
/// a native library; the point is that the shape matches what a real P/Invoke binding would look
/// like, so swapping in a real <c>libz</c> later would not change any call site.
/// <see cref="ZStream"/>/<see cref="Zlib"/> are built on top of this layer, not the other way
/// around.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Matches zlib.h's exported function names exactly.")]
[SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case", Justification = "Matches zlib.h's exported function names exactly.")]
public static unsafe class RawZlib
{
    /// <summary>The compatibility version string this implementation reports itself as (mirrors <c>ZLIB_VERSION</c>).</summary>
    public const string Version = ZlibConstants.Version;

    private static readonly byte* VersionUtf8 = InternVersion();

    private static byte* InternVersion()
    {
        byte* p = (byte*)NativeMemory.Alloc((nuint)(Version.Length + 1));
        for (int i = 0; i < Version.Length; i++)
            p[i] = (byte)Version[i];
        p[Version.Length] = 0;
        return p;
    }

    private static bool VersionOk(byte* version) => version != null && *version == (byte)Version[0];

    // ==================== deflate ====================

    public static int deflateInit_(z_stream* strm, int level, byte* version, int stream_size) =>
        deflateInit2_(strm, level, ZlibConstants.ZDeflated, ZlibConstants.MaxWBits, DeflateConstants.DefMemLevel,
            (int)CompressionStrategy.Default, version, stream_size);

    public static int deflateInit2_(z_stream* strm, int level, int method, int windowBits, int memLevel,
        int strategy, byte* version, int stream_size)
    {
        if (!VersionOk(version) || stream_size != sizeof(z_stream))
            return (int)ZlibResult.VersionError;
        if (strm == null)
            return (int)ZlibResult.StreamError;
        return (int)DeflateInit2Core(strm, level, method, windowBits, memLevel, strategy);
    }

    /// <summary>Convenience overload mirroring the real zlib.h <c>deflateInit</c> macro (auto-fills version/size).</summary>
    public static int deflateInit(z_stream* strm, int level) =>
        deflateInit_(strm, level, VersionUtf8, sizeof(z_stream));

    /// <summary>Convenience overload mirroring the real zlib.h <c>deflateInit2</c> macro (auto-fills version/size).</summary>
    public static int deflateInit2(z_stream* strm, int level, int method, int windowBits, int memLevel, int strategy) =>
        deflateInit2_(strm, level, method, windowBits, memLevel, strategy, VersionUtf8, sizeof(z_stream));

    private static ZlibResult DeflateInit2Core(z_stream* strm, int level, int method, int windowBits, int memLevel, int strategy)
    {
        int wrap = 1;
        if (level == -1)
            level = 6;

        if (windowBits < 0)
        {
            wrap = 0;
            if (windowBits < -15)
                return ZlibResult.StreamError;
            windowBits = -windowBits;
        }
        else if (windowBits > 15)
        {
            wrap = 2;
            windowBits -= 16;
        }

        if (memLevel < 1 || memLevel > DeflateConstants.MaxMemLevel || method != ZlibConstants.ZDeflated ||
            windowBits < 8 || windowBits > 15 || level < 0 || level > 9 ||
            strategy < 0 || strategy > (int)CompressionStrategy.Fixed ||
            (windowBits == 8 && wrap != 1))
            return ZlibResult.StreamError;

        if (windowBits == 8)
            windowBits = 9;

        var s = (DeflateState*)NativeMemory.AllocZeroed((nuint)sizeof(DeflateState));
        strm->state = s;
        strm->msg = null;
        s->Strm = strm;
        s->Wrap = wrap;
        s->GzHead = null;
        s->Allocate(windowBits, memLevel);
        s->Level = level;
        s->Strategy = strategy;
        s->Method = (byte)method;
        s->Status = DeflateConstants.InitState;
        DeflateEngine.Reset(s);
        return ZlibResult.Ok;
    }

    public static int deflate(z_stream* strm, int flush) => (int)DeflateEngine.Deflate(strm, (FlushMode)flush);

    public static int deflateEnd(z_stream* strm)
    {
        if (strm == null || strm->state == null)
            return (int)ZlibResult.StreamError;
        var s = (DeflateState*)strm->state;
        int status = s->Status;
        s->FreeNative();
        NativeMemory.Free(s);
        strm->state = null;
        return status is DeflateConstants.InitState or DeflateConstants.GzipState or DeflateConstants.ExtraState
            or DeflateConstants.NameState or DeflateConstants.CommentState or DeflateConstants.HcrcState
            or DeflateConstants.BusyState or DeflateConstants.FinishState
            ? (int)ZlibResult.Ok
            : (int)ZlibResult.StreamError;
    }

    public static int deflateReset(z_stream* strm)
    {
        if (strm == null || strm->state == null)
            return (int)ZlibResult.StreamError;
        DeflateEngine.Reset((DeflateState*)strm->state);
        return (int)ZlibResult.Ok;
    }

    public static int deflateSetDictionary(z_stream* strm, byte* dictionary, uint dictLength) =>
        (int)DeflateEngine.SetDictionary(strm, dictionary, dictLength);

    public static nuint deflateBound(z_stream* strm, nuint sourceLen) => DeflateEngine.Bound(strm, sourceLen);

    // ==================== inflate ====================

    public static int inflateInit_(z_stream* strm, byte* version, int stream_size) =>
        inflateInit2_(strm, ZlibConstants.DefWBits, version, stream_size);

    public static int inflateInit2_(z_stream* strm, int windowBits, byte* version, int stream_size)
    {
        if (!VersionOk(version) || stream_size != sizeof(z_stream))
            return (int)ZlibResult.VersionError;
        if (strm == null)
            return (int)ZlibResult.StreamError;
        return (int)InflateInit2Core(strm, windowBits);
    }

    /// <summary>Convenience overload mirroring the real zlib.h <c>inflateInit</c> macro (auto-fills version/size).</summary>
    public static int inflateInit(z_stream* strm) => inflateInit_(strm, VersionUtf8, sizeof(z_stream));

    /// <summary>Convenience overload mirroring the real zlib.h <c>inflateInit2</c> macro (auto-fills version/size).</summary>
    public static int inflateInit2(z_stream* strm, int windowBits) => inflateInit2_(strm, windowBits, VersionUtf8, sizeof(z_stream));

    private static ZlibResult InflateInit2Core(z_stream* strm, int windowBits)
    {
        var st = (InflateState*)NativeMemory.AllocZeroed((nuint)sizeof(InflateState));
        st->Init();
        strm->state = st;
        strm->msg = null;
        st->Strm = strm;
        st->Mode = InflateMode.Head;
        ZlibResult ret = InflateEngine.Reset2(strm, st, windowBits);
        if (ret != ZlibResult.Ok)
        {
            st->FreeNative();
            NativeMemory.Free(st);
            strm->state = null;
        }
        return ret;
    }

    public static int inflate(z_stream* strm, int flush)
    {
        if (strm == null || strm->state == null)
            return (int)ZlibResult.StreamError;
        return (int)InflateEngine.Run(strm, (FlushMode)flush);
    }

    public static int inflateEnd(z_stream* strm)
    {
        if (strm == null || strm->state == null)
            return (int)ZlibResult.StreamError;
        var st = (InflateState*)strm->state;
        st->FreeNative();
        NativeMemory.Free(st);
        strm->state = null;
        return (int)ZlibResult.Ok;
    }

    public static int inflateReset(z_stream* strm)
    {
        if (strm == null || strm->state == null)
            return (int)ZlibResult.StreamError;
        return (int)InflateEngine.Reset(strm, (InflateState*)strm->state);
    }

    public static int inflateReset2(z_stream* strm, int windowBits)
    {
        if (strm == null || strm->state == null)
            return (int)ZlibResult.StreamError;
        return (int)InflateEngine.Reset2(strm, (InflateState*)strm->state, windowBits);
    }

    public static int inflateSetDictionary(z_stream* strm, byte* dictionary, uint dictLength)
    {
        if (strm == null || strm->state == null)
            return (int)ZlibResult.StreamError;
        return (int)InflateEngine.SetDictionary(strm, (InflateState*)strm->state, dictionary, dictLength);
    }

    public static int inflateGetDictionary(z_stream* strm, byte* dictionary, uint* dictLength)
    {
        if (strm == null || strm->state == null)
            return (int)ZlibResult.StreamError;
        var st = (InflateState*)strm->state;
        if (st->WHave != 0 && dictionary != null && st->Window != null)
        {
            uint tail = st->WHave - st->WNext;
            Buffer.MemoryCopy(st->Window + st->WNext, dictionary, tail, tail);
            if (st->WNext != 0)
                Buffer.MemoryCopy(st->Window, dictionary + tail, st->WNext, st->WNext);
        }
        if (dictLength != null)
            *dictLength = st->WHave;
        return (int)ZlibResult.Ok;
    }

    // ==================== one-shot compress/uncompress ====================

    /// <summary>Worst-case compressed size for a zlib-wrapped stream of <paramref name="sourceLen"/> bytes.</summary>
    public static nuint compressBound(nuint sourceLen) =>
        sourceLen + (sourceLen >> 12) + (sourceLen >> 14) + (sourceLen >> 25) + 13;

    /// <summary>One-shot zlib-wrapped compression — a faithful port of zlib's <c>compress2</c> (compress.c).</summary>
    public static int compress2(byte* dest, nuint* destLen, byte* source, nuint sourceLen, int level)
    {
        z_stream strm = default;
        nuint left = *destLen;
        nuint remaining = sourceLen;
        *destLen = 0;

        int err = deflateInit_(&strm, level, VersionUtf8, sizeof(z_stream));
        if (err != (int)ZlibResult.Ok)
            return err;

        strm.next_out = dest;
        strm.avail_out = 0;
        strm.next_in = source;
        strm.avail_in = 0;

        do
        {
            if (strm.avail_out == 0)
            {
                strm.avail_out = left > uint.MaxValue ? uint.MaxValue : (uint)left;
                left -= strm.avail_out;
            }
            if (strm.avail_in == 0)
            {
                strm.avail_in = remaining > uint.MaxValue ? uint.MaxValue : (uint)remaining;
                remaining -= strm.avail_in;
            }
            err = deflate(&strm, remaining != 0 ? (int)FlushMode.NoFlush : (int)FlushMode.Finish);
        }
        while (err == (int)ZlibResult.Ok);

        *destLen = strm.total_out;
        deflateEnd(&strm);
        return err == (int)ZlibResult.StreamEnd ? (int)ZlibResult.Ok : err;
    }

    /// <summary>One-shot zlib-wrapped compression at the default level — mirrors zlib's <c>compress</c>.</summary>
    public static int compress(byte* dest, nuint* destLen, byte* source, nuint sourceLen) =>
        compress2(dest, destLen, source, sourceLen, ZlibConstants.DefaultCompression);

    /// <summary>One-shot zlib-wrapped decompression — a faithful port of zlib's <c>uncompress2</c> (uncompr.c).</summary>
    public static int uncompress2(byte* dest, nuint* destLen, byte* source, nuint* sourceLen)
    {
        z_stream strm = default;
        nuint len = *sourceLen;
        nuint left;
        byte oneByte;
        byte* destPtr;

        if (*destLen != 0)
        {
            left = *destLen;
            *destLen = 0;
            destPtr = dest;
        }
        else
        {
            left = 1;
            destPtr = &oneByte;
        }

        int err = inflateInit_(&strm, VersionUtf8, sizeof(z_stream));
        if (err != (int)ZlibResult.Ok)
            return err;

        strm.next_in = source;
        strm.avail_in = 0;
        strm.next_out = destPtr;
        strm.avail_out = 0;

        do
        {
            if (strm.avail_out == 0)
            {
                strm.avail_out = left > uint.MaxValue ? uint.MaxValue : (uint)left;
                left -= strm.avail_out;
            }
            if (strm.avail_in == 0)
            {
                strm.avail_in = len > uint.MaxValue ? uint.MaxValue : (uint)len;
                len -= strm.avail_in;
            }
            err = inflate(&strm, (int)FlushMode.NoFlush);
        }
        while (err == (int)ZlibResult.Ok);

        *sourceLen -= len + strm.avail_in;
        if (destPtr == dest)
            *destLen = strm.total_out;

        inflateEnd(&strm);
        return err == (int)ZlibResult.StreamEnd ? (int)ZlibResult.Ok
             : err == (int)ZlibResult.NeedDict ? (int)ZlibResult.DataError
             : err == (int)ZlibResult.MemError ? (int)ZlibResult.MemError
             : err == (int)ZlibResult.DataError ? (int)ZlibResult.DataError
             : (int)ZlibResult.BufError;
    }

    /// <summary>One-shot zlib-wrapped decompression — mirrors zlib's <c>uncompress</c>.</summary>
    public static int uncompress(byte* dest, nuint* destLen, byte* source, nuint sourceLen)
    {
        nuint srcLen = sourceLen;
        return uncompress2(dest, destLen, source, &srcLen);
    }

    // ==================== checksums ====================

    public static nuint adler32(nuint adler, byte* buf, uint len) =>
        buf == null ? 1u : DnZlib.Adler32.Update((uint)adler, buf, len);

    public static nuint crc32(nuint crc, byte* buf, uint len) =>
        buf == null ? 0u : DnZlib.Crc32.Update((uint)crc, buf, len);

    public static nuint adler32_combine(nuint adler1, nuint adler2, nint len2) =>
        DnZlib.Adler32.Combine((uint)adler1, (uint)adler2, len2);

    public static nuint crc32_combine(nuint crc1, nuint crc2, nint len2) =>
        DnZlib.Crc32.Combine((uint)crc1, (uint)crc2, len2);

    // ==================== misc ====================

    /// <summary>The library version string (mirrors zlib's <c>zlibVersion()</c>).</summary>
    public static byte* zlibVersion() => VersionUtf8;

    /// <summary>Human-readable message for a <see cref="ZlibResult"/> code (mirrors zlib's <c>zError()</c>).</summary>
    public static byte* zError(int err) => err switch
    {
        2 => Messages.NeedDictionary,
        1 => Messages.StreamEndMsg,
        0 => Messages.Empty,
        -1 => Messages.FileError,
        -2 => Messages.StreamErrorMsg,
        -3 => Messages.DataErrorMsg,
        -4 => Messages.InsufficientMemory,
        -5 => Messages.BufferError,
        -6 => Messages.IncompatibleVersion,
        _ => Messages.Empty,
    };
}
