using System.Buffers;

namespace DnBrotli.Tests.Oracles;

/// <summary>
/// Cross-verification oracle over the BCL's <c>System.IO.Compression</c> brotli surface, which
/// P/Invokes the real native brotli (the exact library DnBrotli reimplements and, under dn2cpp,
/// replaces). Compressing here and decompressing with DnBrotli — and vice versa — is the core
/// interoperability guarantee.
/// </summary>
internal static class SystemBrotli
{
    /// <summary>Streaming-encoder compress at an explicit quality/window (handles any input
    /// size, unlike the one-shot <c>TryCompress</c> which needs a pre-sized buffer).</summary>
    internal static byte[] Compress(ReadOnlySpan<byte> source, int quality = 4, int window = 22)
    {
        using var encoder = new System.IO.Compression.BrotliEncoder(quality, window);
        using var output = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            ReadOnlySpan<byte> remaining = source;
            while (true)
            {
                OperationStatus status = encoder.Compress(
                    remaining, buffer, out int consumed, out int written, isFinalBlock: true);
                output.Write(buffer, 0, written);
                remaining = remaining.Slice(consumed);
                if (status == OperationStatus.Done)
                {
                    return output.ToArray();
                }
                if (status != OperationStatus.DestinationTooSmall)
                {
                    throw new InvalidOperationException($"oracle compress failed: {status}");
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>Streaming-decoder decompress (handles any output size).</summary>
    internal static byte[] Decompress(ReadOnlySpan<byte> compressed)
    {
        using var decoder = new System.IO.Compression.BrotliDecoder();
        using var output = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            ReadOnlySpan<byte> remaining = compressed;
            while (true)
            {
                OperationStatus status = decoder.Decompress(remaining, buffer, out int consumed, out int written);
                output.Write(buffer, 0, written);
                remaining = remaining.Slice(consumed);
                if (status == OperationStatus.Done)
                {
                    return output.ToArray();
                }
                if (status == OperationStatus.NeedMoreData && remaining.IsEmpty)
                {
                    throw new InvalidOperationException("oracle decompress: truncated input");
                }
                if (status is not OperationStatus.DestinationTooSmall and not OperationStatus.NeedMoreData)
                {
                    throw new InvalidOperationException($"oracle decompress failed: {status}");
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
