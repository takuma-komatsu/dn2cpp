using System.Runtime.InteropServices;
using Dn2Cpp.Runtime;
using DnZlib.Raw;

namespace DnZlib.Dn2CppInterop;

/// <summary>
/// dn2cpp transpile-time backend swap for <c>System.IO.Compression</c>: managed
/// implementations of the eight <c>CompressionNative_*</c> entry points the real
/// CoreLib's <c>[DllImport("libSystem.IO.Compression.Native")]</c> P/Invokes bottom
/// out in. When a dn2cpp transpile references DnZlib, calls to those imports lower to
/// these methods (see <see cref="NativeImplementationAttribute"/>), so
/// DeflateStream/GZipStream/ZLibStream — and ZipArchive's CRC-32 — run fully managed
/// and the transpiler's vendored native zlib drops out of the binary. Under a normal
/// .NET runtime this type is unused, inert code.
///
/// This is a line-for-line port of dn2cpp's
/// <c>runtime/core/intrinsics/dn2cpp_zlib_native.cpp</c> (itself mirroring
/// dotnet/runtime's <c>pal_zlib.c</c>) over <see cref="RawZlib"/>. Every PAL constant
/// the managed side passes (error/flush codes, level/method/strategy) is numerically
/// identical to the zlib constant of the same meaning — <c>pal_zlib.c</c>
/// static_asserts this — so every <see langword="int"/> passes through untranslated.
/// All state is per-stream and lives in unmanaged memory (<see cref="z_stream"/> plus
/// the engine state <see cref="RawZlib"/> allocates behind it), so no managed
/// reference is ever stored where a GC cannot see it, and no locking is needed — the
/// BCL serializes access per stream handle, like real zlib. The buffers
/// <see cref="PalZStream.NextIn"/>/<see cref="PalZStream.NextOut"/> point into are
/// pinned/rooted by the managed caller across the call, exactly as they are for the
/// native shim this replaces.
/// </summary>
internal static unsafe class CompressionNative
{
    private const string Module = "libSystem.IO.Compression.Native";

    /// <summary>
    /// Mirrors the managed <c>System.IO.Compression.ZLibNative.ZStream</c> struct
    /// field-for-field (<c>[StructLayout(LayoutKind.Sequential)]</c>):
    /// <c>nint nextIn; nint nextOut; nint msg; nint internalState; uint availIn;
    /// uint availOut;</c>. <see cref="InternalState"/> is opaque to the managed BCL
    /// code; a heap-allocated <see cref="z_stream"/> is stashed there, exactly like
    /// <c>pal_zlib.c</c>'s <c>PAL_ZStream::internalState</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct PalZStream
    {
        public byte* NextIn;
        public byte* NextOut;
        public byte* Msg;
        public z_stream* InternalState;
        public uint AvailIn;
        public uint AvailOut;
    }

    /// <summary>Allocates and zero-initializes the <see cref="z_stream"/>, stashing it
    /// in <see cref="PalZStream.InternalState"/>.</summary>
    private static int Init(PalZStream* stream)
    {
        var z = (z_stream*)NativeMemory.AllocZeroed((nuint)sizeof(z_stream));
        stream->InternalState = z;
        return (int)ZlibResult.Ok;
    }

    /// <summary>Frees the <see cref="z_stream"/> allocated by <see cref="Init"/>.</summary>
    private static void End(PalZStream* stream)
    {
        z_stream* z = stream->InternalState;
        if (z != null)
        {
            NativeMemory.Free(z);
            stream->InternalState = null;
        }
    }

    /// <summary>Copies a just-completed call's outputs back to the managed-visible struct.</summary>
    private static void TransferStateOut(z_stream* from, PalZStream* to)
    {
        to->NextIn = from->next_in;
        to->AvailIn = from->avail_in;

        to->NextOut = from->next_out;
        to->AvailOut = from->avail_out;

        to->Msg = from->msg;
    }

    /// <summary>Recovers the <see cref="z_stream"/> from
    /// <see cref="PalZStream.InternalState"/>, first syncing in the managed side's
    /// current buffers/counts (the managed caller may have handed over more input or a
    /// fresh output buffer between calls) — mirrors <c>pal_zlib.c</c>'s
    /// <c>GetCurrentZStream</c>.</summary>
    private static z_stream* Current(PalZStream* stream)
    {
        z_stream* z = stream->InternalState;

        z->next_in = stream->NextIn;
        z->avail_in = stream->AvailIn;

        z->next_out = stream->NextOut;
        z->avail_out = stream->AvailOut;

        return z;
    }

    // ---- the 8 CompressionNative_* entry points ----------------------------
    // The entry-point strings are the literal EntryPoint names the real CoreLib's
    // [DllImport] declarations carry. level/method/windowBits/memLevel/strategy/
    // flush arrive as the managed ZLibNative enum values, blittable as plain int.

    [NativeImplementation(Module, "CompressionNative_DeflateInit2_")]
    private static int DeflateInit2(PalZStream* stream, int level, int method, int windowBits, int memLevel, int strategy)
    {
        int result = Init(stream);
        if (result == (int)ZlibResult.Ok)
        {
            z_stream* z = Current(stream);
            result = RawZlib.deflateInit2(z, level, method, windowBits, memLevel, strategy);
            TransferStateOut(z, stream);
        }
        return result;
    }

    [NativeImplementation(Module, "CompressionNative_Deflate")]
    private static int Deflate(PalZStream* stream, int flush)
    {
        z_stream* z = Current(stream);
        int result = RawZlib.deflate(z, flush);
        TransferStateOut(z, stream);
        return result;
    }

    [NativeImplementation(Module, "CompressionNative_DeflateEnd")]
    private static int DeflateEnd(PalZStream* stream)
    {
        z_stream* z = Current(stream);
        int result = RawZlib.deflateEnd(z);
        End(stream);
        return result;
    }

    [NativeImplementation(Module, "CompressionNative_InflateInit2_")]
    private static int InflateInit2(PalZStream* stream, int windowBits)
    {
        int result = Init(stream);
        if (result == (int)ZlibResult.Ok)
        {
            z_stream* z = Current(stream);
            result = RawZlib.inflateInit2(z, windowBits);
            TransferStateOut(z, stream);
        }
        return result;
    }

    [NativeImplementation(Module, "CompressionNative_Inflate")]
    private static int Inflate(PalZStream* stream, int flush)
    {
        z_stream* z = Current(stream);
        int result = RawZlib.inflate(z, flush);
        TransferStateOut(z, stream);
        return result;
    }

    [NativeImplementation(Module, "CompressionNative_InflateEnd")]
    private static int InflateEnd(PalZStream* stream)
    {
        z_stream* z = Current(stream);
        int result = RawZlib.inflateEnd(z);
        End(stream);
        return result;
    }

    /// <summary>Equivalent to InflateEnd + InflateInit2 but reuses the allocated state.</summary>
    [NativeImplementation(Module, "CompressionNative_InflateReset2_")]
    private static int InflateReset2(PalZStream* stream, int windowBits)
    {
        z_stream* z = Current(stream);
        int result = RawZlib.inflateReset2(z, windowBits);
        TransferStateOut(z, stream);
        return result;
    }

    [NativeImplementation(Module, "CompressionNative_Crc32")]
    private static uint Crc32(uint crc, byte* buffer, int len)
    {
        return (uint)RawZlib.crc32(crc, buffer, (uint)len);
    }
}
