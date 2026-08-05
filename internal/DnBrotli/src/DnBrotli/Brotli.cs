using System.Buffers;
using System.Runtime.InteropServices;
using DnBrotli.Dec;
using DnBrotli.Raw;

namespace DnBrotli;

/// <summary>High-level one-shot helpers over the <see cref="RawBrotli"/> engine (the DnZlib
/// <c>Zlib</c> pattern).</summary>
public static class Brotli
{
    /// <summary>Compress <paramref name="source"/>, returning a right-sized buffer.
    /// <c>BrotliEncoderMaxCompressedSize</c> guarantees enough output space for a single
    /// one-shot <c>BrotliEncoderCompress</c> call, so the work happens in a single ArrayPool
    /// rental that never needs to grow/retry (mirrors <see cref="TryCompress"/>). Invalid
    /// quality/window throw <see cref="ArgumentOutOfRangeException"/>; engine failure — not
    /// reachable with a max-bound buffer — throws <see cref="BrotliException"/>.</summary>
    public static unsafe byte[] Compress(ReadOnlySpan<byte> source, int quality = 4, int window = 22)
    {
        // The BCL-shaped validation (quality first, then window), shared with BrotliEncoder.
        BrotliEncoder.ValidateQuality(quality);
        BrotliEncoder.ValidateWindow(window);

        nuint bound = RawBrotli.BrotliEncoderMaxCompressedSize((nuint)source.Length);
        if (bound == 0 || bound > int.MaxValue)
        {
            // Only reachable for inputs within ~512 KiB of int.MaxValue, where the bound
            // itself no longer fits an array; kept as a hard stop rather than a wrap.
            throw new BrotliException("input too large to bound the brotli compressed size");
        }

        byte[] pooled = ArrayPool<byte>.Shared.Rent((int)bound);
        try
        {
            fixed (byte* inPtr = &MemoryMarshal.GetReference(source))
            fixed (byte* outPtr = pooled)
            {
                nuint encodedSize = (nuint)pooled.Length;
                if (RawBrotli.BrotliEncoderCompress(
                        quality, window, BrotliEncoderMode.Generic,
                        (nuint)source.Length, inPtr, &encodedSize, outPtr) == 0)
                {
                    throw new BrotliException("brotli encode failed");
                }
                return pooled.AsSpan(0, (int)encodedSize).ToArray();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pooled);
        }
    }

    /// <summary>
    /// Try to compress into a caller-provided buffer in a single pass at the default
    /// quality (4) and window (22); <see langword="false"/> if it doesn't fit. Forwards to
    /// <see cref="BrotliEncoder.TryCompress(ReadOnlySpan{byte}, Span{byte}, out int)"/>.
    /// </summary>
    public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
        BrotliEncoder.TryCompress(source, destination, out bytesWritten);

    /// <summary>
    /// Try to compress into a caller-provided buffer in a single pass at an explicit
    /// quality/window; <see langword="false"/> if it doesn't fit. Forwards to
    /// <see cref="BrotliEncoder.TryCompress(ReadOnlySpan{byte}, Span{byte}, out int, int, int)"/>.
    /// </summary>
    public static bool TryCompress(
        ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int window) =>
        BrotliEncoder.TryCompress(source, destination, out bytesWritten, quality, window);

    /// <summary>
    /// Decompress a complete brotli stream, returning a right-sized buffer. Work happens in a
    /// single ArrayPool rental grown by doubling; <paramref name="sizeHint"/> (when positive)
    /// seeds the initial capacity but never limits or pads the result. Unused input after a
    /// complete stream is ignored, like the C one-shot <c>BrotliDecoderDecompress</c>. Throws
    /// <see cref="BrotliException"/> carrying the engine's <see cref="BrotliDecoderErrorCode"/>
    /// on malformed input, and with <see cref="BrotliDecoderErrorCode.NeedsMoreInput"/> when the
    /// stream is truncated (or empty).
    /// </summary>
    public static unsafe byte[] Decompress(ReadOnlySpan<byte> source, int sizeHint = -1)
    {
        BrotliDecoderState* s = RawBrotli.BrotliDecoderCreateInstance();
        if (s == null)
        {
            throw new BrotliException("BrotliDecoderCreateInstance failed");
        }

        int cap = sizeHint > 0
            ? sizeHint
            : (int)Math.Min(int.MaxValue, Math.Max(64L, (long)source.Length * 4));
        byte[] pooled = ArrayPool<byte>.Shared.Rent(cap);
        int total = 0;

        try
        {
            fixed (byte* inPtr = &MemoryMarshal.GetReference(source))
            {
                byte* next_in = inPtr;
                nuint avail_in = (nuint)source.Length;
                while (true)
                {
                    if (total == pooled.Length)
                    {
                        byte[] grown = ArrayPool<byte>.Shared.Rent(
                            (int)Math.Min(int.MaxValue, (long)pooled.Length * 2));
                        pooled.AsSpan(0, total).CopyTo(grown);
                        ArrayPool<byte>.Shared.Return(pooled);
                        pooled = grown;
                    }

                    BrotliDecoderResult result;
                    fixed (byte* outPtr = pooled)
                    {
                        byte* next_out = outPtr + total;
                        nuint avail_out = (nuint)(pooled.Length - total);
                        result = RawBrotli.BrotliDecoderDecompressStream(
                            s, &avail_in, &next_in, &avail_out, &next_out, null);
                        total = pooled.Length - (int)avail_out;
                    }

                    if (result == BrotliDecoderResult.Success)
                    {
                        return pooled.AsSpan(0, total).ToArray();
                    }
                    if (result == BrotliDecoderResult.NeedsMoreOutput && total == pooled.Length)
                    {
                        continue; // output was full; grow and retry
                    }
                    if (result == BrotliDecoderResult.NeedsMoreInput)
                    {
                        // All input was handed over up front, so more input cannot come.
                        throw new BrotliException(
                            BrotliDecoderErrorCode.NeedsMoreInput, "brotli stream is truncated");
                    }
                    // Error — or NeedsMoreOutput without a full buffer, which the engine's
                    // invariants rule out (kept as a hard stop rather than a spin).
                    BrotliDecoderErrorCode code = RawBrotli.BrotliDecoderGetErrorCode(s);
                    throw new BrotliException(code, $"brotli decode failed: {code}");
                }
            }
        }
        finally
        {
            RawBrotli.BrotliDecoderDestroyInstance(s);
            ArrayPool<byte>.Shared.Return(pooled);
        }
    }

    /// <summary>
    /// Try to decompress a complete brotli stream into a caller-provided buffer in a single
    /// pass. Returns <see langword="false"/> (without throwing) when the input is malformed,
    /// truncated, or <paramref name="destination"/> is too small. Forwards to
    /// <see cref="BrotliDecoder.TryDecompress"/>.
    /// </summary>
    public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
        BrotliDecoder.TryDecompress(source, destination, out bytesWritten);
}
