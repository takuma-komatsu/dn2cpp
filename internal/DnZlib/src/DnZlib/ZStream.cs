using System.Runtime.InteropServices;
using DnZlib.Raw;

namespace DnZlib;

/// <summary>
/// The low-level, zlib-compatible stream context — a thin, C#-idiomatic wrapper around a
/// <see cref="Raw.z_stream"/> allocated in unmanaged memory. Callers set
/// <see cref="Input"/>/<see cref="Output"/>, then drive <c>Deflate</c>/<c>Inflate</c> which advance
/// them in place. Every operation here delegates to <see cref="RawZlib"/>; this class exists only
/// to spare callers from juggling raw pointers and pinning.
/// </summary>
public sealed unsafe partial class ZStream : IDisposable
{
    /// <summary>Pending input the engine consumes (mirrors <c>next_in</c> + <c>avail_in</c>).</summary>
    public ReadOnlyMemory<byte> Input { get; set; }

    /// <summary>Output buffer the engine fills (mirrors <c>next_out</c> + <c>avail_out</c>).</summary>
    public Memory<byte> Output { get; set; }

    /// <summary>Total bytes consumed from input across all calls.</summary>
    public long TotalIn => (long)_strm->total_in;

    /// <summary>Total bytes produced to output across all calls.</summary>
    public long TotalOut => (long)_strm->total_out;

    /// <summary>Running checksum of the uncompressed data (adler32 for zlib, crc32 for gzip).</summary>
    public uint Adler => (uint)_strm->adler;

    /// <summary>Best-guess data type of the uncompressed data.</summary>
    public ZlibDataType DataType => (ZlibDataType)_strm->data_type;

    /// <summary>Last informational or error message, if any.</summary>
    public string? Message => _strm->msg == null ? null : Marshal.PtrToStringUTF8((IntPtr)_strm->msg);

    /// <summary>The underlying native stream struct. Exposed internally for the Deflate/Inflate partials.</summary>
    internal z_stream* Strm => _strm;

    private z_stream* _strm = (z_stream*)NativeMemory.AllocZeroed((nuint)sizeof(z_stream));

    // Tracks which *End function owns any live engine state, purely so Dispose/the finalizer know
    // which one to call if the caller forgets — z_stream.state itself carries no type information,
    // exactly like real zlib, where the caller is always assumed to know which init it called.
    private byte _engineKind; // 0 = none, 1 = deflate, 2 = inflate
    private const byte EngineNone = 0;
    private const byte EngineDeflate = 1;
    private const byte EngineInflate = 2;

    /// <summary>Releases native buffers held by the engine state (if any) and the stream struct itself.</summary>
    public void Dispose()
    {
        FreeEngineIfLive();
        if (_strm != null)
        {
            NativeMemory.Free(_strm);
            _strm = null;
        }
        GC.SuppressFinalize(this);
    }

    ~ZStream()
    {
        FreeEngineIfLive();
        if (_strm != null)
            NativeMemory.Free(_strm);
    }

    private void FreeEngineIfLive()
    {
        if (_strm == null || _strm->state == null)
            return;
        if (_engineKind == EngineDeflate)
            RawZlib.deflateEnd(_strm);
        else if (_engineKind == EngineInflate)
            RawZlib.inflateEnd(_strm);
    }
}
