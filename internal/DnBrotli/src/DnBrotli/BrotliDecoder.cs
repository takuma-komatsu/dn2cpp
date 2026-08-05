using System.Buffers;
using System.Runtime.InteropServices;
using DnBrotli.Dec;
using DnBrotli.Raw;

namespace DnBrotli;

/// <summary>
/// Streaming brotli decoder shaped exactly like <c>System.IO.Compression.BrotliDecoder</c>, so
/// call sites are drop-in: the same lazy initialization on first use, the same
/// <see cref="OperationStatus"/> mapping (Success → <see cref="OperationStatus.Done"/>,
/// NeedsMoreInput → <see cref="OperationStatus.NeedMoreData"/>, NeedsMoreOutput →
/// <see cref="OperationStatus.DestinationTooSmall"/>, Error →
/// <see cref="OperationStatus.InvalidData"/>), and the same after-end behavior — once the stream
/// has finished, further <see cref="Decompress"/> calls keep returning
/// <see cref="OperationStatus.Done"/> with nothing consumed or written. Built on the
/// <see cref="RawBrotli"/> C-ABI surface, which owns the unmanaged decoder state.
/// </summary>
public struct BrotliDecoder : IDisposable
{
    private unsafe BrotliDecoderState* _state;
    private bool _disposed;

    private unsafe void EnsureInitialized()
    {
        EnsureNotDisposed();
        if (_state == null)
        {
            _state = RawBrotli.BrotliDecoderCreateInstance();
            if (_state == null)
            {
                // Unreachable with the built-in allocator; kept for BCL shape fidelity
                // (the BCL throws IOException when BrotliDecoderCreateInstance fails).
                throw new IOException("Failed to create BrotliDecoder instance");
            }
        }
    }

    private readonly void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(BrotliDecoder));
    }

    /// <summary>Releases the unmanaged decoder state. Safe to call more than once and on a
    /// default (never used) instance; any later <see cref="Decompress"/> call throws
    /// <see cref="ObjectDisposedException"/>, matching the BCL.</summary>
    public void Dispose()
    {
        _disposed = true;
        unsafe
        {
            if (_state != null)
            {
                RawBrotli.BrotliDecoderDestroyInstance(_state);
                _state = null;
            }
        }
    }

    /// <summary>
    /// Decompresses as much of <paramref name="source"/> into <paramref name="destination"/> as
    /// possible, resumable across calls. Returns <see cref="OperationStatus.Done"/> when the
    /// brotli stream is complete (and on every call thereafter — trailing input past the end of
    /// the stream is never consumed), <see cref="OperationStatus.NeedMoreData"/> when all input
    /// was consumed mid-stream, <see cref="OperationStatus.DestinationTooSmall"/> when output
    /// space ran out, and <see cref="OperationStatus.InvalidData"/> (with nothing reported
    /// consumed or written) on malformed input.
    /// </summary>
    public OperationStatus Decompress(
        ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
    {
        EnsureInitialized();
        bytesConsumed = 0;
        bytesWritten = 0;
        unsafe
        {
            if (RawBrotli.BrotliDecoderIsFinished(_state) != 0)
            {
                return OperationStatus.Done;
            }
            nuint availableInput = (nuint)source.Length;
            nuint availableOutput = (nuint)destination.Length;
            fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
            fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
            {
                byte* nextInBytes = inBytes;
                byte* nextOutBytes = outBytes;
                while (true)
                {
                    BrotliDecoderResult result = RawBrotli.BrotliDecoderDecompressStream(
                        _state, &availableInput, &nextInBytes, &availableOutput, &nextOutBytes, null);
                    if (result == BrotliDecoderResult.Error)
                    {
                        return OperationStatus.InvalidData;
                    }

                    bytesConsumed = source.Length - (int)availableInput;
                    bytesWritten = destination.Length - (int)availableOutput;

                    switch (result)
                    {
                        case BrotliDecoderResult.Success:
                            return OperationStatus.Done;
                        case BrotliDecoderResult.NeedsMoreOutput:
                            return OperationStatus.DestinationTooSmall;
                    }

                    // NeedsMoreInput. The engine's invariant is that all input has been
                    // consumed here; the loop (mirroring the BCL) only re-enters if it wasn't.
                    if (availableInput == 0)
                    {
                        return OperationStatus.NeedMoreData;
                    }
                }
            }
        }
    }

    /// <summary>
    /// One-shot decode of a complete brotli stream into a caller-provided buffer. Returns
    /// <see langword="false"/> (without throwing) when the input is malformed, truncated, or
    /// <paramref name="destination"/> is too small; <paramref name="bytesWritten"/> reports the
    /// bytes produced either way, exactly like the BCL / C <c>BrotliDecoderDecompress</c>.
    /// </summary>
    public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
    {
        unsafe
        {
            fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
            fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
            {
                nuint written = (nuint)destination.Length;
                BrotliDecoderResult result = RawBrotli.BrotliDecoderDecompress(
                    (nuint)source.Length, inBytes, &written, outBytes);
                bytesWritten = (int)written;
                return result == BrotliDecoderResult.Success;
            }
        }
    }
}
