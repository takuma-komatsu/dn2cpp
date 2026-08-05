using DnBrotli.Enc;

namespace DnBrotli.Raw;

/// <summary>
/// The encoder half of the brotli-compatible low-level API — the exact function shapes
/// encode.h declares (<c>BrotliEncoderCreateInstance</c>, <c>BrotliEncoderCompressStream</c>,
/// <c>BrotliEncoderTakeOutput</c>, ...), implemented in pure C#. Same conventions as the
/// decoder half in RawBrotli.cs: <c>size_t</c> = <c>nuint</c>, <c>BROTLI_BOOL</c> =
/// <c>int</c>, opaque <c>BrotliEncoderState*</c>.
///
/// Scope note: all qualities 0..11 are fully functional (fast fragment paths,
/// the greedy q2/q3 path, the generic q4..q9 path and the q10/q11 Zopfli
/// path). Only the deferred surfaces (large-window hashers for lgwin &gt; 24,
/// compound dictionaries) reach a clearly-marked
/// <see cref="NotImplementedException"/> at the exact C call boundary.
/// </summary>
public static unsafe partial class RawBrotli
{
    // ==================== encode.h ====================

    /// <summary><c>BrotliEncoderSetParameter</c>: sets an encoder parameter. Returns
    /// <c>BROTLI_TRUE</c> (1) on success, <c>BROTLI_FALSE</c> (0) if the encoder has
    /// already started compressing or the parameter/value is unsupported.</summary>
    public static int BrotliEncoderSetParameter(
        BrotliEncoderState* state, BrotliEncoderParameter param, uint value) =>
        EncoderEngine.BrotliEncoderSetParameter(state, param, value);

    /// <summary><c>BrotliEncoderCreateInstance</c>: creates an encoder instance. The
    /// allocator triple is accepted for signature fidelity but ignored — memory always
    /// comes from the native heap. Passing exactly one of the pair returns <c>null</c>,
    /// as in C. Free with <see cref="BrotliEncoderDestroyInstance"/>.</summary>
    public static BrotliEncoderState* BrotliEncoderCreateInstance(
        nint alloc_func, nint free_func, void* opaque) =>
        EncoderEngine.BrotliEncoderCreateInstance(alloc_func, free_func, opaque);

    /// <summary>Convenience overload mirroring the common C call
    /// <c>BrotliEncoderCreateInstance(NULL, NULL, NULL)</c>.</summary>
    public static BrotliEncoderState* BrotliEncoderCreateInstance() =>
        EncoderEngine.BrotliEncoderCreateInstance(0, 0, null);

    /// <summary><c>BrotliEncoderDestroyInstance</c>: deinitializes and frees an instance.
    /// A <c>null</c> pointer is ignored.</summary>
    public static void BrotliEncoderDestroyInstance(BrotliEncoderState* state) =>
        EncoderEngine.BrotliEncoderDestroyInstance(state);

    /// <summary><c>BrotliEncoderCompress</c>: one-shot compression at the given
    /// quality/lgwin/mode. On entry <c>*encoded_size</c> is the capacity of
    /// <c>encoded_buffer</c>; on exit it is the number of bytes written. Falls back to
    /// an uncompressed wrapping when the compressed form would not fit but
    /// <see cref="BrotliEncoderMaxCompressedSize"/> bytes are available.</summary>
    public static int BrotliEncoderCompress(
        int quality, int lgwin, BrotliEncoderMode mode,
        nuint input_size, byte* input_buffer,
        nuint* encoded_size, byte* encoded_buffer) =>
        EncoderEngine.BrotliEncoderCompress(
            quality, lgwin, mode, input_size, input_buffer, encoded_size, encoded_buffer);

    /// <summary><c>BrotliEncoderCompressStream</c>: streaming compression. Consumes from
    /// <c>*next_in</c>/<c>*available_in</c>, produces into
    /// <c>*next_out</c>/<c>*available_out</c>, advancing all four; <c>total_out</c>
    /// (optional) receives the cumulative number of bytes pushed out. <c>op</c> selects
    /// Process/Flush/Finish/EmitMetadata exactly as in C.</summary>
    public static int BrotliEncoderCompressStream(
        BrotliEncoderState* state, BrotliEncoderOperation op,
        nuint* available_in, byte** next_in,
        nuint* available_out, byte** next_out, nuint* total_out) =>
        EncoderEngine.BrotliEncoderCompressStream(
            state, op, available_in, next_in, available_out, next_out, total_out);

    /// <summary><c>BrotliEncoderIsFinished</c>: <c>BROTLI_TRUE</c> (1) once the stream is
    /// finalized and all output has been pushed/taken.</summary>
    public static int BrotliEncoderIsFinished(BrotliEncoderState* state) =>
        EncoderEngine.BrotliEncoderIsFinished(state);

    /// <summary><c>BrotliEncoderHasMoreOutput</c>: <c>BROTLI_TRUE</c> (1) when the encoder
    /// holds output that has not been pushed/taken yet.</summary>
    public static int BrotliEncoderHasMoreOutput(BrotliEncoderState* state) =>
        EncoderEngine.BrotliEncoderHasMoreOutput(state);

    /// <summary><c>BrotliEncoderTakeOutput</c>: acquires a pointer to internal output
    /// without copying. On entry <c>*size</c> is the maximum requested (0 = any amount);
    /// on exit it is the size of the returned region, which stays valid until the next
    /// encoder call. Returns <c>null</c> when no output is available.</summary>
    public static byte* BrotliEncoderTakeOutput(BrotliEncoderState* state, nuint* size) =>
        EncoderEngine.BrotliEncoderTakeOutput(state, size);

    /// <summary><c>BrotliEncoderMaxCompressedSize</c>: upper bound of the compressed size
    /// for any input of <paramref name="input_size"/> bytes (0 means "not computable",
    /// which only happens on overflow).</summary>
    public static nuint BrotliEncoderMaxCompressedSize(nuint input_size) =>
        EncoderEngine.BrotliEncoderMaxCompressedSize(input_size);

    /// <summary><c>BrotliEncoderVersion</c>: library version packed as
    /// <c>(major &lt;&lt; 24) | (minor &lt;&lt; 12) | patch</c> — 1.1.0, matching the
    /// vendored C source.</summary>
    public static uint BrotliEncoderVersion() =>
        EncoderEngine.BrotliEncoderVersion();

    /// <summary><c>BrotliEncoderEstimatePeakMemoryUsage</c>: rough peak-memory estimate
    /// for the given parameters. The general branch throws for qualities whose
    /// hasher type is not yet ported (see the HasherSize dispatch in Hash.cs).</summary>
    public static nuint BrotliEncoderEstimatePeakMemoryUsage(
        int quality, int lgwin, nuint input_size) =>
        EncoderEngine.BrotliEncoderEstimatePeakMemoryUsage(quality, lgwin, input_size);
}
