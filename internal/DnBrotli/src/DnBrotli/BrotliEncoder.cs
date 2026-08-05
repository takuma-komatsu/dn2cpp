using System.Buffers;
using System.Runtime.InteropServices;
using DnBrotli.Enc;
using DnBrotli.Raw;

namespace DnBrotli;

/// <summary>
/// Streaming brotli encoder shaped exactly like <c>System.IO.Compression.BrotliEncoder</c>, so
/// call sites are drop-in: the same constructor validation (quality 0..11, window 10..24), the
/// same lazy initialization on first use (a default instance compresses at the engine defaults,
/// quality 11 / window 22), and the same <see cref="Compress"/> loop shape — including the BCL's
/// quirks of returning <see cref="OperationStatus.DestinationTooSmall"/> when the final block
/// exactly fills the destination (the next call reports <see cref="OperationStatus.Done"/> with
/// nothing written) and of reporting <see cref="OperationStatus.InvalidData"/> when input is
/// pushed after the stream was finalized. Built on the <see cref="RawBrotli"/> C-ABI surface,
/// which owns the unmanaged encoder state.
/// </summary>
public struct BrotliEncoder : IDisposable
{
    internal const int QualityMin = 0;
    internal const int QualityDefault = 4;
    internal const int QualityMax = 11;
    internal const int WindowBitsMin = 10;
    internal const int WindowBitsDefault = 22;
    internal const int WindowBitsMax = 24;

    private unsafe BrotliEncoderState* _state;
    private bool _disposed;

    /// <summary>Creates an encoder at the given brotli quality (0..11) and window bits
    /// (10..24). Throws <see cref="ArgumentOutOfRangeException"/> exactly like the BCL —
    /// quality is validated first — and <see cref="IOException"/> if the engine cannot
    /// allocate the instance.</summary>
    public BrotliEncoder(int quality, int window)
    {
        // Validate both arguments before allocating unmanaged state so a bad argument can
        // never leak a native instance (the BCL allocates first; its SafeHandle finalizer
        // covers that path, which a struct over raw NativeMemory does not have). The
        // observable behavior — quality checked before window — is identical.
        ValidateQuality(quality);
        ValidateWindow(window);
        SetQuality(quality);
        SetWindow(window);
    }

    private unsafe void InitializeEncoder()
    {
        EnsureNotDisposed();
        _state = RawBrotli.BrotliEncoderCreateInstance();
        if (_state == null)
        {
            // Unreachable with the built-in allocator; kept for BCL shape fidelity
            // (the BCL throws IOException when BrotliEncoderCreateInstance fails).
            throw new IOException("Failed to create BrotliEncoder instance");
        }
    }

    private unsafe void EnsureInitialized()
    {
        EnsureNotDisposed();
        if (_state == null)
        {
            InitializeEncoder();
        }
    }

    private readonly void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(BrotliEncoder));
    }

    /// <summary>Releases the unmanaged encoder state. Safe to call more than once and on a
    /// default (never used) instance; any later <see cref="Compress"/> / <see cref="Flush"/>
    /// call throws <see cref="ObjectDisposedException"/>, matching the BCL.</summary>
    public void Dispose()
    {
        _disposed = true;
        unsafe
        {
            if (_state != null)
            {
                RawBrotli.BrotliEncoderDestroyInstance(_state);
                _state = null;
            }
        }
    }

    internal static void ValidateQuality(int quality)
    {
        if (quality < QualityMin || quality > QualityMax)
        {
            throw new ArgumentOutOfRangeException(nameof(quality),
                $"Provided BrotliEncoder Quality of {quality} is not between the minimum value of {QualityMin} and the maximum value of {QualityMax}");
        }
    }

    internal static void ValidateWindow(int window)
    {
        if (window < WindowBitsMin || window > WindowBitsMax)
        {
            throw new ArgumentOutOfRangeException(nameof(window),
                $"Provided BrotliEncoder Window of {window} is not between the minimum value of {WindowBitsMin} and the maximum value of {WindowBitsMax}");
        }
    }

    internal unsafe void SetQuality(int quality)
    {
        EnsureNotDisposed();
        ValidateQuality(quality);
        EnsureInitialized();
        if (RawBrotli.BrotliEncoderSetParameter(_state, BrotliEncoderParameter.Quality, (uint)quality) == 0)
        {
            throw new InvalidOperationException("The BrotliEncoder Quality parameter could not be set");
        }
    }

    internal unsafe void SetWindow(int window)
    {
        EnsureNotDisposed();
        ValidateWindow(window);
        EnsureInitialized();
        if (RawBrotli.BrotliEncoderSetParameter(_state, BrotliEncoderParameter.LgWin, (uint)window) == 0)
        {
            throw new InvalidOperationException("The BrotliEncoder Window parameter could not be set");
        }
    }

    /// <summary>Sets the encoder tuning mode — the knob the BCL struct hides (surfaced through
    /// <c>DnBrotli.Streams.BrotliCompressionOptions</c>). Must be applied before compression
    /// starts, like every encoder parameter.</summary>
    internal unsafe void SetMode(BrotliEncoderMode mode)
    {
        EnsureNotDisposed();
        if (mode < BrotliEncoderMode.Generic || mode > BrotliEncoderMode.Font)
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }
        EnsureInitialized();
        if (RawBrotli.BrotliEncoderSetParameter(_state, BrotliEncoderParameter.Mode, (uint)mode) == 0)
        {
            throw new InvalidOperationException("The BrotliEncoder Mode parameter could not be set");
        }
    }

    /// <summary>
    /// Upper bound of the compressed size for any input of <paramref name="inputSize"/> bytes —
    /// the engine's <c>BrotliEncoderMaxCompressedSize</c>, exactly as the BCL exposes it: throws
    /// <see cref="ArgumentOutOfRangeException"/> when <paramref name="inputSize"/> is negative or
    /// so large that the bound would exceed <see cref="int.MaxValue"/>; returns 2 for an input
    /// size of 0.
    /// </summary>
    public static int GetMaxCompressedLength(int inputSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputSize);

        nuint result = RawBrotli.BrotliEncoderMaxCompressedSize((nuint)inputSize);
        if (result > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(inputSize));
        }

        return (int)result;
    }

    /// <summary>
    /// Compresses as much of <paramref name="source"/> into <paramref name="destination"/> as
    /// possible, resumable across calls. With <paramref name="isFinalBlock"/> the stream is
    /// finalized (no more input may be pushed afterwards); returns
    /// <see cref="OperationStatus.Done"/> only when the operation fully completed — all input
    /// consumed and, for final blocks, every finalization byte emitted —
    /// <see cref="OperationStatus.DestinationTooSmall"/> when output space ran out, and
    /// <see cref="OperationStatus.InvalidData"/> when the engine rejects the call (input after
    /// finalization). Loop shape and quirks match the BCL call for call.
    /// </summary>
    public OperationStatus Compress(
        ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten,
        bool isFinalBlock) =>
        Compress(source, destination, out bytesConsumed, out bytesWritten,
            isFinalBlock ? BrotliEncoderOperation.Finish : BrotliEncoderOperation.Process);

    internal OperationStatus Compress(
        ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten,
        BrotliEncoderOperation operation)
    {
        EnsureInitialized();
        bytesWritten = 0;
        bytesConsumed = 0;
        unsafe
        {
            nuint availableOutput = (nuint)destination.Length;
            nuint availableInput = (nuint)source.Length;

            // The BCL loop, verbatim: hand the engine the remaining windows until it neither
            // consumes nor holds anything (Done) or the destination is exhausted. The int/nuint
            // casts are safe because spans cap at int.MaxValue and the engine only decreases
            // the available counts.
            while ((int)availableOutput > 0)
            {
                fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                {
                    byte* nextInBytes = inBytes;
                    byte* nextOutBytes = outBytes;
                    if (RawBrotli.BrotliEncoderCompressStream(
                            _state, operation, &availableInput, &nextInBytes,
                            &availableOutput, &nextOutBytes, null) == 0)
                    {
                        return OperationStatus.InvalidData;
                    }

                    bytesConsumed += source.Length - (int)availableInput;
                    bytesWritten += destination.Length - (int)availableOutput;

                    // Nothing written this round, no remaining input, no output held back by
                    // the encoder: the operation is complete (for Finish this implies the
                    // stream is finished — the engine only drains its pending output then).
                    if ((int)availableOutput == destination.Length
                        && RawBrotli.BrotliEncoderHasMoreOutput(_state) == 0
                        && availableInput == 0)
                    {
                        return OperationStatus.Done;
                    }

                    source = source.Slice(source.Length - (int)availableInput);
                    destination = destination.Slice(destination.Length - (int)availableOutput);
                }
            }

            return OperationStatus.DestinationTooSmall;
        }
    }

    /// <summary>
    /// Emits output for all processed input so the bytes written so far form a decodable
    /// prefix (encoder operation Flush), exactly like the BCL — a
    /// <see cref="Compress"/> call with empty input and the Flush operation.
    /// </summary>
    public OperationStatus Flush(Span<byte> destination, out int bytesWritten) =>
        Compress(ReadOnlySpan<byte>.Empty, destination, out _, out bytesWritten,
            BrotliEncoderOperation.Flush);

    /// <summary>
    /// One-shot compression at the default quality (4) and window (22) into a caller-provided
    /// buffer. Returns <see langword="false"/> (without throwing) when the compressed form does
    /// not fit, exactly like the BCL / C <c>BrotliEncoderCompress</c>.
    /// </summary>
    public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
        TryCompress(source, destination, out bytesWritten, QualityDefault, WindowBitsDefault);

    /// <summary>
    /// One-shot compression at an explicit quality/window into a caller-provided buffer.
    /// Invalid quality/window throw <see cref="ArgumentOutOfRangeException"/> like the BCL;
    /// a too-small destination returns <see langword="false"/> with nothing reported written.
    /// A destination of at least <see cref="GetMaxCompressedLength"/> bytes always succeeds.
    /// </summary>
    public static bool TryCompress(
        ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int window)
    {
        ValidateQuality(quality);
        ValidateWindow(window);

        unsafe
        {
            fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
            fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
            {
                nuint availableOutput = (nuint)destination.Length;
                bool success = RawBrotli.BrotliEncoderCompress(
                    quality, window, BrotliEncoderMode.Generic,
                    (nuint)source.Length, inBytes, &availableOutput, outBytes) != 0;
                bytesWritten = (int)availableOutput;
                return success;
            }
        }
    }
}
