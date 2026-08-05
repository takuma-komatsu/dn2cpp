using DnZlib.Raw;
using static DnZlib.Deflate.DeflateConstants;

namespace DnZlib;

public sealed unsafe partial class ZStream
{
    /// <summary>Initialize the stream for compression with the given level and default parameters.</summary>
    public ZlibResult DeflateInit(int level) =>
        DeflateInit2(level, CompressionMethod.Deflated, 15, DefMemLevel, CompressionStrategy.Default);

    /// <summary>Initialize the stream for compression. Negative windowBits = raw, +16 = gzip.</summary>
    public ZlibResult DeflateInit2(int level, CompressionMethod method, int windowBits, int memLevel, CompressionStrategy strategy)
    {
        var rc = (ZlibResult)RawZlib.deflateInit2(_strm, level, (int)method, windowBits, memLevel, (int)strategy);
        if (rc == ZlibResult.Ok)
            _engineKind = EngineDeflate;
        return rc;
    }

    /// <summary>Reset the stream to begin a fresh compression, keeping the allocated buffers.</summary>
    public ZlibResult DeflateReset() => (ZlibResult)RawZlib.deflateReset(_strm);

    /// <summary>Release the compression state and its native buffers.</summary>
    public ZlibResult DeflateEnd()
    {
        var rc = (ZlibResult)RawZlib.deflateEnd(_strm);
        _engineKind = EngineNone;
        return rc;
    }

    /// <summary>Upper bound on the compressed size of <paramref name="sourceLen"/> input bytes.</summary>
    public long DeflateBound(long sourceLen) => (long)RawZlib.deflateBound(_strm, (nuint)sourceLen);

    /// <summary>Compress from <see cref="Input"/> into <see cref="Output"/>, advancing both.</summary>
    public ZlibResult Deflate(FlushMode flush)
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

            var ret = (ZlibResult)RawZlib.deflate(_strm, (int)flush);

            int consumed = input.Length - (int)_strm->avail_in;
            int produced = output.Length - (int)_strm->avail_out;
            _strm->next_in = null;
            _strm->next_out = null;
            Input = Input[consumed..];
            Output = Output[produced..];
            return ret;
        }
    }

    /// <summary>Single-pass compress from spans without using the cursor properties.</summary>
    public ZlibResult Deflate(ReadOnlySpan<byte> input, Span<byte> output, FlushMode flush,
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

            var ret = (ZlibResult)RawZlib.deflate(_strm, (int)flush);

            consumed = input.Length - (int)_strm->avail_in;
            written = output.Length - (int)_strm->avail_out;
            _strm->next_in = null;
            _strm->next_out = null;
            return ret;
        }
    }

    /// <summary>Supply a preset dictionary before the first deflate call.</summary>
    public ZlibResult DeflateSetDictionary(ReadOnlySpan<byte> dictionary)
    {
        fixed (byte* d = dictionary)
            return (ZlibResult)RawZlib.deflateSetDictionary(_strm, d, (uint)dictionary.Length);
    }
}
