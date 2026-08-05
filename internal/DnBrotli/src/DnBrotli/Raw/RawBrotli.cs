using System.Diagnostics.CodeAnalysis;
using DnBrotli.Common;
using DnBrotli.Dec;

namespace DnBrotli.Raw;

/// <summary>
/// The brotli-compatible low-level API — the exact function shapes decode.h declares
/// (<c>BrotliDecoderCreateInstance</c>, <c>BrotliDecoderDecompressStream</c>,
/// <c>BrotliDecoderTakeOutput</c>, ...), implemented in Pure C# rather than bound via
/// <c>DllImport</c>. Nothing here actually calls into a native library; the point is that the
/// shape matches what a real P/Invoke binding would look like (<c>size_t</c> = <c>nuint</c>,
/// <c>BROTLI_BOOL</c> = <c>int</c>, opaque <c>BrotliDecoderState*</c>), so swapping in the real
/// <c>libbrotlidec</c> later would not change any call site. Higher-level surfaces are built on
/// top of this layer, not the other way around.
///
/// This is the decoder half; the encoder half (encode.h) lands with the encoder port.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Matches brotli's decode.h parameter spelling exactly.")]
[SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case", Justification = "Matches brotli's decode.h exported names exactly.")]
public static unsafe partial class RawBrotli
{
    // ==================== decode.h ====================

    /// <summary><c>BrotliDecoderSetParameter</c>: sets a decoder parameter. Returns
    /// <c>BROTLI_TRUE</c> (1) on success, <c>BROTLI_FALSE</c> (0) if the decoder is already
    /// used or the parameter is unrecognized.</summary>
    public static int BrotliDecoderSetParameter(
        BrotliDecoderState* state, BrotliDecoderParameter param, uint value) =>
        DecoderEngine.BrotliDecoderSetParameter(state, param, value);

    /// <summary><c>BrotliDecoderCreateInstance</c>: creates a decoder instance. The
    /// <c>brotli_alloc_func</c>/<c>brotli_free_func</c>/<c>opaque</c> triple is accepted for
    /// signature fidelity but ignored — memory always comes from the native heap. Passing
    /// exactly one of the pair returns <c>null</c>, as in C. Free with
    /// <see cref="BrotliDecoderDestroyInstance"/>.</summary>
    public static BrotliDecoderState* BrotliDecoderCreateInstance(
        nint alloc_func, nint free_func, void* opaque) =>
        DecoderEngine.BrotliDecoderCreateInstance(alloc_func, free_func, opaque);

    /// <summary>Convenience overload mirroring the common C call
    /// <c>BrotliDecoderCreateInstance(NULL, NULL, NULL)</c>.</summary>
    public static BrotliDecoderState* BrotliDecoderCreateInstance() =>
        DecoderEngine.BrotliDecoderCreateInstance(0, 0, null);

    /// <summary><c>BrotliDecoderDestroyInstance</c>: deinitializes and frees an instance
    /// created by <see cref="BrotliDecoderCreateInstance(nint, nint, void*)"/>. A
    /// <c>null</c> pointer is ignored.</summary>
    public static void BrotliDecoderDestroyInstance(BrotliDecoderState* state) =>
        DecoderEngine.BrotliDecoderDestroyInstance(state);

    /// <summary><c>BrotliDecoderDecompress</c>: one-shot decode. On entry
    /// <c>*decoded_size</c> is the capacity of <c>decoded_buffer</c>; on exit it is the
    /// number of bytes written. Anything but full success (including "needs more
    /// input/output") is reported as <see cref="BrotliDecoderResult.Error"/>.</summary>
    public static BrotliDecoderResult BrotliDecoderDecompress(
        nuint encoded_size, byte* encoded_buffer,
        nuint* decoded_size, byte* decoded_buffer) =>
        DecoderEngine.BrotliDecoderDecompress(
            encoded_size, encoded_buffer, decoded_size, decoded_buffer);

    /// <summary><c>BrotliDecoderDecompressStream</c>: streaming decode. Consumes from
    /// <c>*next_in</c>/<c>*available_in</c>, produces into <c>*next_out</c>/<c>*available_out</c>,
    /// advancing all four; <c>total_out</c> (optional) receives the cumulative number of bytes
    /// pushed out. Input is never overconsumed: on
    /// <see cref="BrotliDecoderResult.NeedsMoreInput"/> all input has been consumed, and on
    /// success unused input is returned to the caller.</summary>
    public static BrotliDecoderResult BrotliDecoderDecompressStream(
        BrotliDecoderState* state, nuint* available_in, byte** next_in,
        nuint* available_out, byte** next_out, nuint* total_out) =>
        DecoderEngine.BrotliDecoderDecompressStream(
            state, available_in, next_in, available_out, next_out, total_out);

    /// <summary><c>BrotliDecoderHasMoreOutput</c>: <c>BROTLI_TRUE</c> (1) when the decoder
    /// holds output that has not been pushed/taken yet (always 0 after an unrecoverable
    /// error).</summary>
    public static int BrotliDecoderHasMoreOutput(BrotliDecoderState* state) =>
        DecoderEngine.BrotliDecoderHasMoreOutput(state);

    /// <summary><c>BrotliDecoderTakeOutput</c>: acquires a pointer to internal output
    /// without copying. On entry <c>*size</c> is the maximum requested (0 = any amount);
    /// on exit it is the size of the returned region, which stays valid until the next
    /// decoder call. Returns <c>null</c> when no output is available.</summary>
    public static byte* BrotliDecoderTakeOutput(BrotliDecoderState* state, nuint* size) =>
        DecoderEngine.BrotliDecoderTakeOutput(state, size);

    /// <summary><c>BrotliDecoderIsUsed</c>: <c>BROTLI_TRUE</c> (1) once the instance has
    /// consumed any input or produced any output.</summary>
    public static int BrotliDecoderIsUsed(BrotliDecoderState* state) =>
        DecoderEngine.BrotliDecoderIsUsed(state);

    /// <summary><c>BrotliDecoderIsFinished</c>: <c>BROTLI_TRUE</c> (1) when the decoder
    /// reached its final state and all output has been drained.</summary>
    public static int BrotliDecoderIsFinished(BrotliDecoderState* state) =>
        DecoderEngine.BrotliDecoderIsFinished(state);

    /// <summary><c>BrotliDecoderGetErrorCode</c>: last saved detailed status. Negative
    /// values are errors; only valid after <see cref="BrotliDecoderDecompressStream"/>
    /// returned <see cref="BrotliDecoderResult.Error"/> or reached the end of the
    /// stream.</summary>
    public static BrotliDecoderErrorCode BrotliDecoderGetErrorCode(BrotliDecoderState* state) =>
        DecoderEngine.BrotliDecoderGetErrorCode(state);

    /// <summary><c>BrotliDecoderErrorString</c>: NUL-terminated, static ASCII name of an
    /// error code (e.g. <c>"_ERROR_FORMAT_WINDOW_BITS"</c>; <c>"INVALID"</c> for unknown
    /// codes), byte-identical to the C strings.</summary>
    public static byte* BrotliDecoderErrorString(BrotliDecoderErrorCode c) =>
        DecoderEngine.BrotliDecoderErrorString(c);

    /// <summary><c>BrotliDecoderVersion</c>: library version packed as
    /// <c>(major &lt;&lt; 24) | (minor &lt;&lt; 12) | patch</c> — 1.1.0, matching the
    /// vendored C source.</summary>
    public static uint BrotliDecoderVersion() =>
        DecoderEngine.BrotliDecoderVersion();

    /// <summary><c>BrotliDecoderAttachDictionary</c>: attaches an LZ77 prefix ("raw")
    /// dictionary to a not-yet-used decoder. Serialized shared dictionaries are rejected,
    /// matching a C build without <c>BROTLI_EXPERIMENTAL</c>. The data is NOT copied — the
    /// pointer must stay valid and unchanged for the lifetime of the instance.</summary>
    public static int BrotliDecoderAttachDictionary(
        BrotliDecoderState* state, BrotliSharedDictionaryType type,
        nuint data_size, byte* data) =>
        DecoderEngine.BrotliDecoderAttachDictionary(state, type, data_size, data);
}
