using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace DnZlib.Raw;

/// <summary>
/// The C ABI shape of zlib's <c>z_stream</c> (zlib.h) — every field lives directly in unmanaged
/// memory, exactly as it would if this were a real <c>DllImport</c> binding over native zlib. No
/// actual interop happens anywhere in this project; this struct only mirrors the native layout so
/// that <see cref="RawZlib"/>'s functions have the same shape as the C originals.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Mirrors zlib.h's z_stream field-for-field; this is the point of the type.")]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Matches zlib.h's z_stream field names exactly.")]
public unsafe struct z_stream
{
    /// <summary>Next input byte.</summary>
    public byte* next_in;

    /// <summary>Number of bytes available at <see cref="next_in"/>.</summary>
    public uint avail_in;

    /// <summary>Total number of input bytes read so far.</summary>
    public nuint total_in;

    /// <summary>Next output byte will go here.</summary>
    public byte* next_out;

    /// <summary>Remaining free space at <see cref="next_out"/>.</summary>
    public uint avail_out;

    /// <summary>Total number of bytes output so far.</summary>
    public nuint total_out;

    /// <summary>Last error message, or <see langword="null"/> if no error (NUL-terminated UTF-8).</summary>
    public byte* msg;

    /// <summary>Opaque engine state — a <c>DeflateState*</c> or <c>InflateState*</c> depending on
    /// which init function was called. Never inspected by <see cref="RawZlib"/> itself; the
    /// caller is expected to know which one it is, exactly as with real zlib.</summary>
    public void* state;

    /// <summary>Present for shape fidelity only; DnZlib always uses its own default allocator.</summary>
    public nint zalloc;

    /// <summary>Present for shape fidelity only; DnZlib always uses its own default allocator.</summary>
    public nint zfree;

    /// <summary>Present for shape fidelity only; passed to <see cref="zalloc"/>/<see cref="zfree"/> in real zlib.</summary>
    public void* opaque;

    /// <summary>Best guess about the data type: binary or text (see <see cref="ZlibDataType"/>).</summary>
    public int data_type;

    /// <summary>Adler-32 or CRC-32 value of the uncompressed data.</summary>
    public nuint adler;

    /// <summary>Reserved for future use; present for shape fidelity only.</summary>
    public nuint reserved;
}
