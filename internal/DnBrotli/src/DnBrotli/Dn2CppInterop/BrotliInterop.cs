using Dn2Cpp.Runtime;
using DnBrotli.Dec;
using DnBrotli.Enc;
using DnBrotli.Raw;

namespace DnBrotli.Dn2CppInterop;

/// <summary>
/// dn2cpp transpile-time backend swap for <c>System.IO.Compression.Brotli</c>: managed
/// implementations of the twelve raw brotli entry points the real BCL's
/// <c>[DllImport("libSystem.IO.Compression.Native")]</c> P/Invokes bottom out in. Unlike the
/// zlib face there is no <c>CompressionNative_*</c> wrapper layer — dotnet/runtime's
/// <c>entrypoints.c</c> re-exports the brotli library's own public symbols — so the entry-point
/// names here are the raw brotli API names. When a dn2cpp transpile references DnBrotli, calls
/// to those imports lower to these methods (see <see cref="NativeImplementationAttribute"/>),
/// so BrotliStream/BrotliEncoder/BrotliDecoder run fully managed and the transpiler's vendored
/// native brotli drops out of the binary. Under a normal .NET runtime this type is unused,
/// inert code (except as the entry point of its own tests).
///
/// The signatures mirror the raw stubs the BCL's <c>[LibraryImport]</c> source generator
/// produces (SafeHandle and BROTLI_BOOL marshalling is lowered into IL before the transpiler
/// sees it): handles are plain pointer-sized integers, <c>size_t</c> is <see cref="nuint"/>,
/// and the seven BROTLI_BOOL-returning functions return <see langword="int"/> (0/1) — never
/// <see langword="bool"/>, which has no blittable native ABI shape. These shapes were proven
/// end-to-end against the genuine native ABI at stage 0 and must not change.
///
/// Each body is a thin adapter over the pure-C# engine's <see cref="RawBrotli"/> face: the
/// opaque <see cref="nint"/> handle casts to the typed unmanaged state pointer
/// (<see cref="BrotliDecoderState"/>* / <see cref="BrotliEncoderState"/>*), raw
/// <see langword="int"/> parameters cast to the matching enum, and enum results cast back —
/// the numeric values are the C header values by construction (see BrotliEnums.cs). All state
/// lives in unmanaged memory (the DnZlib rule), so no managed reference is ever stored where a
/// GC cannot see it; the buffers the pointer arguments reference are pinned/rooted by the
/// managed caller across the call, exactly as for the native library this replaces.
/// </summary>
internal static unsafe class BrotliInterop
{
    private const string Module = "libSystem.IO.Compression.Native";

    // ---- decoder ------------------------------------------------------------

    [NativeImplementation(Module, "BrotliDecoderCreateInstance")]
    internal static nint BrotliDecoderCreateInstance(nint allocFunc, nint freeFunc, nint opaque)
    {
        return (nint)RawBrotli.BrotliDecoderCreateInstance(allocFunc, freeFunc, (void*)opaque);
    }

    [NativeImplementation(Module, "BrotliDecoderDecompressStream")]
    internal static int BrotliDecoderDecompressStream(
        nint state, nuint* availableIn, byte** nextIn, nuint* availableOut, byte** nextOut, nuint* totalOut)
    {
        return (int)RawBrotli.BrotliDecoderDecompressStream(
            (BrotliDecoderState*)state, availableIn, nextIn, availableOut, nextOut, totalOut);
    }

    [NativeImplementation(Module, "BrotliDecoderDecompress")]
    internal static int BrotliDecoderDecompress(
        nuint availableInput, byte* inBytes, nuint* availableOutput, byte* outBytes)
    {
        return (int)RawBrotli.BrotliDecoderDecompress(availableInput, inBytes, availableOutput, outBytes);
    }

    [NativeImplementation(Module, "BrotliDecoderDestroyInstance")]
    internal static void BrotliDecoderDestroyInstance(nint state)
    {
        RawBrotli.BrotliDecoderDestroyInstance((BrotliDecoderState*)state);
    }

    [NativeImplementation(Module, "BrotliDecoderIsFinished")]
    internal static int BrotliDecoderIsFinished(nint state)
    {
        return RawBrotli.BrotliDecoderIsFinished((BrotliDecoderState*)state);
    }

    // ---- encoder ------------------------------------------------------------

    [NativeImplementation(Module, "BrotliEncoderCreateInstance")]
    internal static nint BrotliEncoderCreateInstance(nint allocFunc, nint freeFunc, nint opaque)
    {
        return (nint)RawBrotli.BrotliEncoderCreateInstance(allocFunc, freeFunc, (void*)opaque);
    }

    [NativeImplementation(Module, "BrotliEncoderSetParameter")]
    internal static int BrotliEncoderSetParameter(nint state, int parameter, uint value)
    {
        return RawBrotli.BrotliEncoderSetParameter(
            (BrotliEncoderState*)state, (BrotliEncoderParameter)parameter, value);
    }

    [NativeImplementation(Module, "BrotliEncoderCompressStream")]
    internal static int BrotliEncoderCompressStream(
        nint state, int op, nuint* availableIn, byte** nextIn, nuint* availableOut, byte** nextOut, nuint* totalOut)
    {
        return RawBrotli.BrotliEncoderCompressStream(
            (BrotliEncoderState*)state, (BrotliEncoderOperation)op,
            availableIn, nextIn, availableOut, nextOut, totalOut);
    }

    [NativeImplementation(Module, "BrotliEncoderHasMoreOutput")]
    internal static int BrotliEncoderHasMoreOutput(nint state)
    {
        return RawBrotli.BrotliEncoderHasMoreOutput((BrotliEncoderState*)state);
    }

    [NativeImplementation(Module, "BrotliEncoderDestroyInstance")]
    internal static void BrotliEncoderDestroyInstance(nint state)
    {
        RawBrotli.BrotliEncoderDestroyInstance((BrotliEncoderState*)state);
    }

    [NativeImplementation(Module, "BrotliEncoderCompress")]
    internal static int BrotliEncoderCompress(
        int quality, int window, int mode, nuint availableInput, byte* inBytes, nuint* availableOutput, byte* outBytes)
    {
        return RawBrotli.BrotliEncoderCompress(
            quality, window, (BrotliEncoderMode)mode, availableInput, inBytes, availableOutput, outBytes);
    }

    [NativeImplementation(Module, "BrotliEncoderMaxCompressedSize")]
    internal static nuint BrotliEncoderMaxCompressedSize(nuint inputSize)
    {
        return RawBrotli.BrotliEncoderMaxCompressedSize(inputSize);
    }
}
