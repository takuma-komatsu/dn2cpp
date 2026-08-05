using DnZlib.Internal;
using DnZlib.Raw;

namespace DnZlib;

public sealed unsafe partial class ZStream
{
    /// <summary>Initialize the stream for decompression with the default window size.</summary>
    public ZlibResult InflateInit() => InflateInit2(ZlibConstants.DefWBits);

    /// <summary>
    /// Initialize the stream for decompression. <paramref name="windowBits"/> selects the format:
    /// 8..15 = zlib, negative = raw DEFLATE, +16 = gzip, +32 = auto-detect zlib/gzip.
    /// </summary>
    public ZlibResult InflateInit2(int windowBits)
    {
        var rc = (ZlibResult)RawZlib.inflateInit2(_strm, windowBits);
        if (rc == ZlibResult.Ok)
            _engineKind = EngineInflate;
        return rc;
    }

    /// <summary>Reset the stream keeping the allocated window, applying a new <paramref name="windowBits"/>.</summary>
    public ZlibResult InflateReset2(int windowBits) => (ZlibResult)RawZlib.inflateReset2(_strm, windowBits);

    /// <summary>Reset the stream to start decoding a fresh stream, discarding window contents.</summary>
    public ZlibResult InflateReset() => (ZlibResult)RawZlib.inflateReset(_strm);

    /// <summary>Release the decompression state and its native buffers.</summary>
    public ZlibResult InflateEnd()
    {
        var rc = (ZlibResult)RawZlib.inflateEnd(_strm);
        _engineKind = EngineNone;
        return rc;
    }

    /// <summary>
    /// Decompress from <see cref="Input"/> into <see cref="Output"/>, advancing both. Returns
    /// <see cref="ZlibResult.StreamEnd"/> at the end of the stream, <see cref="ZlibResult.Ok"/> while
    /// more work remains, or an error code.
    /// </summary>
    public ZlibResult Inflate(FlushMode flush)
    {
        ReadOnlySpan<byte> input = Input.Span;
        Span<byte> output = Output.Span;

        fixed (byte* pin = input)
        fixed (byte* pout = output)
        {
            _strm->next_in = pin;
            _strm->avail_in = (uint)input.Length;
            _strm->next_out = pout;
            _strm->avail_out = (uint)output.Length;

            var ret = (ZlibResult)RawZlib.inflate(_strm, (int)flush);

            int consumed = input.Length - (int)_strm->avail_in;
            int produced = output.Length - (int)_strm->avail_out;
            _strm->next_in = null;
            _strm->next_out = null;
            Input = Input[consumed..];
            Output = Output[produced..];
            return ret;
        }
    }

    /// <summary>
    /// Single-pass decompress from <paramref name="input"/> into <paramref name="output"/> without
    /// using the <see cref="Input"/>/<see cref="Output"/> cursors. State still persists across calls,
    /// so this can be looped with fresh spans.
    /// </summary>
    public ZlibResult Inflate(ReadOnlySpan<byte> input, Span<byte> output, FlushMode flush,
                              out int consumed, out int written)
    {
        consumed = 0;
        written = 0;

        fixed (byte* pin = input)
        fixed (byte* pout = output)
        {
            _strm->next_in = pin;
            _strm->avail_in = (uint)input.Length;
            _strm->next_out = pout;
            _strm->avail_out = (uint)output.Length;

            var ret = (ZlibResult)RawZlib.inflate(_strm, (int)flush);

            consumed = input.Length - (int)_strm->avail_in;
            written = output.Length - (int)_strm->avail_out;
            _strm->next_in = null;
            _strm->next_out = null;
            return ret;
        }
    }

    /// <summary>
    /// Supply a preset dictionary after inflate returned <see cref="ZlibResult.NeedDict"/> (zlib
    /// format), or before the first inflate for raw streams.
    /// </summary>
    public ZlibResult InflateSetDictionary(ReadOnlySpan<byte> dictionary)
    {
        fixed (byte* d = dictionary)
            return (ZlibResult)RawZlib.inflateSetDictionary(_strm, d, (uint)dictionary.Length);
    }

    /// <summary>Copy the sliding-window contents (the effective dictionary) into <paramref name="dictionary"/>.</summary>
    public ZlibResult InflateGetDictionary(Span<byte> dictionary, out int length)
    {
        length = 0;
        uint have = 0;
        RawZlib.inflateGetDictionary(_strm, null, &have);
        if (have > (uint)dictionary.Length)
            throw new ArgumentException("Destination buffer is too small to hold the dictionary.", nameof(dictionary));

        fixed (byte* d = dictionary)
        {
            var rc = (ZlibResult)RawZlib.inflateGetDictionary(_strm, d, &have);
            length = (int)have;
            return rc;
        }
    }
}
