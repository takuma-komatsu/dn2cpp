// dn2cpp_zlib_native.cpp — native shim for System.IO.Compression's
// libSystem.IO.Compression.Native P/Invoke surface (DeflateStream/GZipStream).
//
// The real CoreLib's System.IO.Compression.ZLibNative (via the internal
// Interop.ZLib static class) bottoms out in eight
// `[DllImport("libSystem.IO.Compression.Native")]` P/Invokes, backed on real
// .NET by dotnet/runtime's pal_zlib.c (a thin, syscall-free wrapper around
// zlib). dn2cpp lowers those P/Invokes to direct native calls
// (Compilation.IsRuntimeProvidedPInvokeModule) and provides the
// CompressionNative_* symbols here against the vendored zlib
// (third_party/zlib/), so a transpiled binary stays self-contained instead of
// linking the dotnet install's libSystem.IO.Compression.Native.
//
// This file is REACHED, and it is also the fallback rather than the only
// path. Two facts about it are easy to get backwards, so both are stated
// here:
//
//   * IsRuntimeProvidedPInvokeModule DOES allow
//     libSystem.IO.Compression.Native (Compilation.cs) — an earlier note
//     here said it did not, which was true only until that allowlist was
//     flipped. The CompressionNative_* call sites below are the live
//     lowering for every transpiled DeflateStream/GZipStream/ZipArchive.
//   * dn2cpp nevertheless SWAPS this TU out by default. DnZlib
//     (internal/DnZlib) carries [NativeImplementation] adapters for the same
//     eight imports, and it is a conditional default reference: the CLI
//     injects it from beside itself whenever System.IO.Compression is in the
//     load set (Compilation.InjectDefaultRefs), which is whenever these
//     symbols could be called. A managed implementation wins over the native
//     one at every call site, so with the shim present nothing below is
//     reached and archive semantics drop it from the binary.
//
// So this file is what runs when the shim is NOT injected: a caller passing
// `--no-default-ref DnZlib` (which is how the compression-core / zip-core /
// zipfile-core gates keep asserting this path), an installation without
// DnZlib.dll beside the CLI, or an embedder that never sets DefaultRefDir.
// It is live code with live gates — not dead code awaiting a deletion.
//
// ABI source of truth: this mirrors dotnet/runtime's own
// src/native/libs/System.IO.Compression.Native/pal_zlib.c/.h (fetched at tag
// v10.0.1) function-for-function, and the Dn2CppPalZStream layout below
// mirrors the managed System.IO.Compression.ZLibNative.ZStream struct
// field-for-field — both verified directly (pal_zlib.c/.h via the
// dotnet/runtime GitHub source; ZLibNative.ZStream and the Interop.ZLib
// P/Invoke declarations by decompiling the real net10.0
// System.IO.Compression.dll with ilspycmd). Every PAL_* constant
// dotnet/runtime defines for this surface (error codes, flush codes,
// compression level/method/strategy) is numerically identical to zlib's own
// Z_* constant of the same meaning — pal_zlib.c itself static_asserts this —
// so every int32_t parameter/return below passes through to/from real zlib
// unmodified; no translation table is needed.
//
// Portable: like real pal_zlib.c, this file has zero OS-specific branching,
// so it lives under intrinsics/ (compiled for every target, native and
// wasm32) rather than platform/posix/ (reserved for genuinely OS-branching
// code, e.g. dn2cpp_system_native.cpp).

#ifdef DN2CPP_USE_ZLIB

#include <zlib.h>

#include <cassert>
#include <cstdint>
#include <cstdlib>

extern "C" {

// Mirrors managed System.IO.Compression.ZLibNative.ZStream
// ([StructLayout(LayoutKind.Sequential)]) field-for-field:
//   internal struct ZStream {
//       nint nextIn; nint nextOut; nint msg; nint internalState;
//       uint availIn; uint availOut;
//   }
// internalState is opaque to managed code; we stash a heap-allocated real
// z_stream* there (exactly like real pal_zlib.c's PAL_ZStream::internalState).
struct Dn2CppPalZStream
{
    uint8_t* nextIn;         // next input byte
    uint8_t* nextOut;        // next output byte goes here
    char*    msg;            // last error message, null if none
    void*    internalState;  // our real z_stream*, opaque to managed code
    uint32_t availIn;        // bytes available at nextIn
    uint32_t availOut;       // free space at nextOut
};

// ---- helpers, mirroring real pal_zlib.c's Init/End/TransferState*/GetCurrentZStream ----

// Allocates and zero-initializes the real z_stream, stashing it in
// internalState. Zero-initializing zalloc/zfree/opaque (rather than setting
// them) is intentional: zlib's deflateInit2_/inflateInit2_ fall back to their
// own default malloc/free-based allocator (zcalloc/zcfree in zutil.c) whenever
// zalloc is left null, so no custom allocator wiring is needed here.
static int32_t Dn2CppZlibInit(Dn2CppPalZStream* stream)
{
    z_stream* zStream = (z_stream*)calloc(1, sizeof(z_stream));
    stream->internalState = zStream;
    return zStream != nullptr ? Z_OK : Z_MEM_ERROR;
}

// Frees the z_stream allocated by Dn2CppZlibInit.
static void Dn2CppZlibEnd(Dn2CppPalZStream* stream)
{
    z_stream* zStream = (z_stream*)stream->internalState;
    assert(zStream != nullptr);
    if (zStream != nullptr)
    {
        free(zStream);
        stream->internalState = nullptr;
    }
}

// Copies a just-completed zlib call's outputs back to the managed-visible struct.
static void Dn2CppZlibTransferStateOut(z_stream* from, Dn2CppPalZStream* to)
{
    to->nextIn = from->next_in;
    to->availIn = from->avail_in;

    to->nextOut = from->next_out;
    to->availOut = from->avail_out;

    to->msg = from->msg;
}

// Copies the managed side's current buffers/counts into the real z_stream
// before a call (the managed caller may have updated nextIn/availIn/nextOut/
// availOut between calls, e.g. to hand over more input or a fresh output buffer).
static void Dn2CppZlibTransferStateIn(Dn2CppPalZStream* from, z_stream* to)
{
    to->next_in = from->nextIn;
    to->avail_in = from->availIn;

    to->next_out = from->nextOut;
    to->avail_out = from->availOut;
}

// Recovers the real z_stream* from internalState, first syncing in the
// managed side's current input/output state (mirrors real pal_zlib.c's
// GetCurrentZStream, which every entry point but *Init2_ funnels through).
static z_stream* Dn2CppZlibCurrent(Dn2CppPalZStream* stream)
{
    z_stream* zStream = (z_stream*)stream->internalState;
    assert(zStream != nullptr);

    Dn2CppZlibTransferStateIn(stream, zStream);
    return zStream;
}

// ---- the 8 CompressionNative_* entry points --------------------------------
// Names are the literal EntryPoint strings the real corelib's [DllImport]
// declarations carry (module libSystem.IO.Compression.Native).

// Initializes *stream for Deflate(). level/method/windowBits/memLevel/strategy
// are the managed ZLibNative.CompressionLevel/CompressionMethod/int/int/
// CompressionStrategy values, blittable as plain int32 (C# enums are
// int-backed by default). Returns a PAL_ErrorCode (== a real zlib error code).
int32_t CompressionNative_DeflateInit2_(
    Dn2CppPalZStream* stream, int32_t level, int32_t method, int32_t windowBits, int32_t memLevel, int32_t strategy)
{
    assert(stream != nullptr);

    int32_t result = Dn2CppZlibInit(stream);
    if (result == Z_OK)
    {
        z_stream* zStream = Dn2CppZlibCurrent(stream);
        result = deflateInit2(zStream, level, method, windowBits, memLevel, strategy);
        Dn2CppZlibTransferStateOut(zStream, stream);
    }

    return result;
}

// Deflates (compresses) stream->nextIn into stream->nextOut. flush is the
// managed ZLibNative.FlushCode value (plain int32).
int32_t CompressionNative_Deflate(Dn2CppPalZStream* stream, int32_t flush)
{
    assert(stream != nullptr);

    z_stream* zStream = Dn2CppZlibCurrent(stream);
    int32_t result = deflate(zStream, flush);
    Dn2CppZlibTransferStateOut(zStream, stream);

    return result;
}

// Frees the real z_stream's own internal state, then ours.
int32_t CompressionNative_DeflateEnd(Dn2CppPalZStream* stream)
{
    assert(stream != nullptr);

    z_stream* zStream = Dn2CppZlibCurrent(stream);
    int32_t result = deflateEnd(zStream);
    Dn2CppZlibEnd(stream);

    return result;
}

// Initializes *stream for Inflate().
int32_t CompressionNative_InflateInit2_(Dn2CppPalZStream* stream, int32_t windowBits)
{
    assert(stream != nullptr);

    int32_t result = Dn2CppZlibInit(stream);
    if (result == Z_OK)
    {
        z_stream* zStream = Dn2CppZlibCurrent(stream);
        result = inflateInit2(zStream, windowBits);
        Dn2CppZlibTransferStateOut(zStream, stream);
    }

    return result;
}

// Inflates (decompresses) stream->nextIn into stream->nextOut.
int32_t CompressionNative_Inflate(Dn2CppPalZStream* stream, int32_t flush)
{
    assert(stream != nullptr);

    z_stream* zStream = Dn2CppZlibCurrent(stream);
    int32_t result = inflate(zStream, flush);
    Dn2CppZlibTransferStateOut(zStream, stream);

    return result;
}

// Frees the real z_stream's own internal state, then ours.
int32_t CompressionNative_InflateEnd(Dn2CppPalZStream* stream)
{
    assert(stream != nullptr);

    z_stream* zStream = Dn2CppZlibCurrent(stream);
    int32_t result = inflateEnd(zStream);
    Dn2CppZlibEnd(stream);

    return result;
}

// Equivalent to InflateEnd + InflateInit2_ but reuses the allocated state
// (window size may change / reallocate internally as needed).
int32_t CompressionNative_InflateReset2_(Dn2CppPalZStream* stream, int32_t windowBits)
{
    assert(stream != nullptr);

    z_stream* zStream = Dn2CppZlibCurrent(stream);
    int32_t result = inflateReset2(zStream, windowBits);
    Dn2CppZlibTransferStateOut(zStream, stream);

    return result;
}

// Direct passthrough to zlib's own crc32(). Unreachable from the Deflate/GZip
// path today (DeflateStream/GZipStream never call it -- gzip/zlib compute
// their own CRC-32/Adler-32 trailer inside deflate()/inflate() itself); kept
// for ZipArchive work, which does need it.
uint32_t CompressionNative_Crc32(uint32_t crc, uint8_t* buffer, int32_t len)
{
    assert(buffer != nullptr);

    unsigned long result = crc32((uLong)crc, buffer, (uInt)len);
    assert(result <= UINT32_MAX);
    return (uint32_t)result;
}

} // extern "C"

#endif // DN2CPP_USE_ZLIB
