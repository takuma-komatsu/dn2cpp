using DnBrotli;
using DnBrotli.Enc;
using DnBrotli.Raw;
using DnBrotli.Tests.Oracles;
using DnBrotli.Tests.Support;

namespace DnBrotli.Tests;

/// <summary>
/// Shared drivers over the Raw encoder surface (the exact encode.h shapes) used by the
/// encoder test suites. Compressed bytes are never compared against native brotli
/// (implementation-defined); the cross-interop guarantee is that DnBrotli's output decodes
/// identically via DnBrotli's own decoder and via the BCL decoder (= real native brotli).
/// </summary>
internal static unsafe class EncoderDrivers
{
    /// <summary>One-shot <c>BrotliEncoderCompress</c> with an output buffer sized exactly
    /// <c>BrotliEncoderMaxCompressedSize</c> — success also proves the bound.</summary>
    internal static byte[] CompressOneShot(byte[] input, int quality, int window)
    {
        nuint maxSize = RawBrotli.BrotliEncoderMaxCompressedSize((nuint)input.Length);
        Assert.True(maxSize > 0);
        byte[] encoded = new byte[(int)maxSize];
        nuint encodedSize = (nuint)encoded.Length;
        fixed (byte* inputPtr = input)
        fixed (byte* encodedPtr = encoded)
        {
            int result = RawBrotli.BrotliEncoderCompress(
                quality, window, BrotliEncoderMode.Generic,
                (nuint)input.Length, inputPtr, &encodedSize, encodedPtr);
            Assert.Equal(1, result);
        }
        Assert.True(encodedSize <= maxSize,
            $"compressed size {encodedSize} exceeds BrotliEncoderMaxCompressedSize {maxSize}");
        return encoded.AsSpan(0, (int)encodedSize).ToArray();
    }

    /// <summary>Runs one CompressStream operation to completion (input consumed, all
    /// output drained; for Finish, until the encoder reports finished).</summary>
    internal static void DriveOp(
        BrotliEncoderState* s, BrotliEncoderOperation op, ReadOnlySpan<byte> chunk,
        MemoryStream output)
    {
        byte[] outBuf = new byte[64 * 1024];
        int guard = 0;
        fixed (byte* inPtr = chunk)
        fixed (byte* outPtr = outBuf)
        {
            byte* next_in = inPtr;
            nuint available_in = (nuint)chunk.Length;
            while (true)
            {
                byte* next_out = outPtr;
                nuint available_out = (nuint)outBuf.Length;
                nuint total_out = 0;
                int result = RawBrotli.BrotliEncoderCompressStream(
                    s, op, &available_in, &next_in, &available_out, &next_out, &total_out);
                Assert.Equal(1, result);
                int produced = outBuf.Length - (int)available_out;
                output.Write(outBuf, 0, produced);
                bool done = available_in == 0 &&
                    RawBrotli.BrotliEncoderHasMoreOutput(s) == 0 &&
                    (op != BrotliEncoderOperation.Finish ||
                     RawBrotli.BrotliEncoderIsFinished(s) == 1);
                if (done) break;
                Assert.True(++guard < 10000, "encoder made no forward progress (hang)");
            }
        }
    }

    /// <summary>Streaming compression with a fixed input chunk size; optionally flushes
    /// after every chunk, reporting each flushed prefix via <paramref name="afterFlush"/>
    /// as (outputSoFar, inputBytesFedSoFar).</summary>
    internal static byte[] CompressStreaming(
        byte[] input, int quality, int window, int chunkSize,
        bool flushBetweenChunks = false, Action<byte[], int>? afterFlush = null)
    {
        var output = new MemoryStream();
        BrotliEncoderState* s = RawBrotli.BrotliEncoderCreateInstance();
        Assert.True(s != null);
        try
        {
            Assert.Equal(1, RawBrotli.BrotliEncoderSetParameter(
                s, BrotliEncoderParameter.Quality, (uint)quality));
            Assert.Equal(1, RawBrotli.BrotliEncoderSetParameter(
                s, BrotliEncoderParameter.LgWin, (uint)window));
            int offset = 0;
            while (offset < input.Length)
            {
                int len = Math.Min(chunkSize, input.Length - offset);
                DriveOp(s, BrotliEncoderOperation.Process, input.AsSpan(offset, len), output);
                offset += len;
                if (flushBetweenChunks)
                {
                    DriveOp(s, BrotliEncoderOperation.Flush, ReadOnlySpan<byte>.Empty, output);
                    afterFlush?.Invoke(output.ToArray(), offset);
                }
            }
            DriveOp(s, BrotliEncoderOperation.Finish, ReadOnlySpan<byte>.Empty, output);
            Assert.Equal(1, RawBrotli.BrotliEncoderIsFinished(s));
        }
        finally
        {
            RawBrotli.BrotliEncoderDestroyInstance(s);
        }
        return output.ToArray();
    }

    /// <summary>Decompresses with DnBrotli's own (pure C#) decoder via the idiomatic
    /// surface — the in-house half of the cross-interop check.</summary>
    internal static byte[] DecompressDn(byte[] compressed)
    {
        using var decoder = new BrotliDecoder();
        var output = new MemoryStream();
        byte[] buffer = new byte[64 * 1024];
        ReadOnlySpan<byte> remaining = compressed;
        int guard = 0;
        while (true)
        {
            System.Buffers.OperationStatus status =
                decoder.Decompress(remaining, buffer, out int consumed, out int written);
            output.Write(buffer, 0, written);
            remaining = remaining.Slice(consumed);
            if (status == System.Buffers.OperationStatus.Done)
            {
                return output.ToArray();
            }
            Assert.NotEqual(System.Buffers.OperationStatus.InvalidData, status);
            Assert.False(status == System.Buffers.OperationStatus.NeedMoreData && remaining.IsEmpty,
                "DnBrotli decoder: truncated stream");
            Assert.True(++guard < 100000, "DnBrotli decoder made no forward progress (hang)");
        }
    }
}

/// <summary>
/// Fast-path gate: qualities 0/1 (one-pass and two-pass fragment compressors) plus the full
/// streaming state machine. Everything DnBrotli emits must decode identically through
/// (a) DnBrotli's own decoder and (b) the BCL decoder backed by real native brotli.
/// </summary>
public sealed class EncoderFastPathTests
{
    public static IEnumerable<object[]> CorpusQualityWindow()
    {
        foreach ((string name, _) in Corpus.Entries)
        {
            foreach (int quality in new[] { 0, 1 })
            {
                foreach (int window in new[] { 10, 18, 22, 24 })
                {
                    yield return new object[] { name, quality, window };
                }
            }
        }
    }

    public static IEnumerable<object[]> CorpusQuality()
    {
        foreach ((string name, _) in Corpus.Entries)
        {
            foreach (int quality in new[] { 0, 1 })
            {
                yield return new object[] { name, quality };
            }
        }
    }

    [Theory]
    [MemberData(nameof(CorpusQualityWindow))]
    public void OneShot_RoundTrips_ThroughBothDecoders(string name, int quality, int window)
    {
        byte[] input = Corpus.Get(name);
        byte[] compressed = EncoderDrivers.CompressOneShot(input, quality, window);

        Assert.Equal(input, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(input, SystemBrotli.Decompress(compressed));
    }

    [Theory]
    [MemberData(nameof(CorpusQualityWindow))]
    public void Streaming_RoundTrips_ThroughBothDecoders(string name, int quality, int window)
    {
        byte[] input = Corpus.Get(name);
        byte[] compressed = EncoderDrivers.CompressStreaming(
            input, quality, window, chunkSize: 4099);

        Assert.Equal(input, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(input, SystemBrotli.Decompress(compressed));
    }

    [Theory]
    [InlineData("hello", 0, 1)]
    [InlineData("hello", 1, 1)]
    [InlineData("zeros-1k", 0, 1)]
    [InlineData("zeros-1k", 1, 1)]
    [InlineData("english-200k", 0, 4099)]
    [InlineData("english-200k", 1, 4099)]
    [InlineData("binary-structured-128k", 0, 65536)]
    [InlineData("binary-structured-128k", 1, 65536)]
    [InlineData("random-64k", 0, 7777)]
    [InlineData("random-64k", 1, 7777)]
    public void Streaming_FlushedPrefixDecodesToBytesWrittenSoFar(
        string name, int quality, int chunkSize)
    {
        byte[] input = Corpus.Get(name);
        int flushes = 0;
        byte[] compressed = EncoderDrivers.CompressStreaming(
            input, quality, window: 22, chunkSize, flushBetweenChunks: true,
            afterFlush: (outputSoFar, inputFedSoFar) =>
            {
                flushes++;
                /* Every flushed prefix must be a decodable stream prefix reproducing
                   exactly the bytes fed so far — verified with the BCL (native) decoder.
                   Full-corpus 1-byte feeds are exercised on the smaller entries. */
                if (flushes <= 16 || inputFedSoFar == input.Length)
                {
                    byte[] prefix = DecompressPrefixWithBcl(outputSoFar);
                    Assert.Equal(input.AsSpan(0, inputFedSoFar).ToArray(), prefix);
                }
            });
        Assert.True(flushes > 0);

        /* Finish-after-Flush: the finished stream still round-trips completely. */
        Assert.Equal(input, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(input, SystemBrotli.Decompress(compressed));
    }

    /// <summary>Decodes a flushed (unterminated) stream prefix with the BCL decoder:
    /// consumes everything without error and returns the produced bytes. Note the BCL
    /// reports <c>NeedMoreData</c> on an unfinished stream even while it still holds
    /// pending output, so we must keep calling until a call produces nothing.</summary>
    private static byte[] DecompressPrefixWithBcl(byte[] prefix)
    {
        using var decoder = new System.IO.Compression.BrotliDecoder();
        var output = new MemoryStream();
        byte[] buffer = new byte[64 * 1024];
        ReadOnlySpan<byte> remaining = prefix;
        int guard = 0;
        while (true)
        {
            System.Buffers.OperationStatus status =
                decoder.Decompress(remaining, buffer, out int consumed, out int written);
            output.Write(buffer, 0, written);
            remaining = remaining.Slice(consumed);
            if (status == System.Buffers.OperationStatus.Done)
            {
                break;
            }
            if (remaining.IsEmpty && written == 0 &&
                status == System.Buffers.OperationStatus.NeedMoreData)
            {
                break;  /* flushed prefix: fully decoded so far, stream not terminated */
            }
            Assert.True(
                status is System.Buffers.OperationStatus.DestinationTooSmall
                    or System.Buffers.OperationStatus.NeedMoreData,
                $"unexpected OperationStatus {status}");
            Assert.True(++guard < 100000, "BCL decoder made no forward progress (hang)");
        }
        return output.ToArray();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public unsafe void EmptyInput_Streaming_ProducesValidMinimalStream(int quality)
    {
        byte[] compressed = EncoderDrivers.CompressStreaming(
            Array.Empty<byte>(), quality, window: 22, chunkSize: 1);
        Assert.True(compressed.Length >= 1 && compressed.Length <= 4,
            $"expected a minimal stream, got {compressed.Length} bytes");
        Assert.Equal(Array.Empty<byte>(), EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(Array.Empty<byte>(), SystemBrotli.Decompress(compressed));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public unsafe void InputAfterFinish_IsRejected(int quality)
    {
        BrotliEncoderState* s = RawBrotli.BrotliEncoderCreateInstance();
        Assert.True(s != null);
        try
        {
            RawBrotli.BrotliEncoderSetParameter(s, BrotliEncoderParameter.Quality, (uint)quality);
            var output = new MemoryStream();
            EncoderDrivers.DriveOp(s, BrotliEncoderOperation.Process, "abc"u8, output);
            EncoderDrivers.DriveOp(s, BrotliEncoderOperation.Finish, ReadOnlySpan<byte>.Empty, output);
            Assert.Equal(1, RawBrotli.BrotliEncoderIsFinished(s));

            byte extra = 0x42;
            byte* next_in = &extra;
            nuint available_in = 1;
            byte[] outBuf = new byte[64];
            fixed (byte* outPtr = outBuf)
            {
                byte* next_out = outPtr;
                nuint available_out = (nuint)outBuf.Length;
                int result = RawBrotli.BrotliEncoderCompressStream(
                    s, BrotliEncoderOperation.Process,
                    &available_in, &next_in, &available_out, &next_out, null);
                Assert.Equal(0, result);  /* BROTLI_FALSE: no input after Finish */
            }
        }
        finally
        {
            RawBrotli.BrotliEncoderDestroyInstance(s);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public unsafe void EmitMetadata_RoundTrips_DecodersSkipIt(int quality)
    {
        byte[] head = Corpus.Get("hello");
        byte[] metadata = new byte[100];
        for (int i = 0; i < metadata.Length; i++) metadata[i] = (byte)(i * 7);
        byte[] tail = Corpus.Get("repeat-7-40k");

        var output = new MemoryStream();
        BrotliEncoderState* s = RawBrotli.BrotliEncoderCreateInstance();
        Assert.True(s != null);
        try
        {
            RawBrotli.BrotliEncoderSetParameter(s, BrotliEncoderParameter.Quality, (uint)quality);
            EncoderDrivers.DriveOp(s, BrotliEncoderOperation.Process, head, output);
            EncoderDrivers.DriveOp(s, BrotliEncoderOperation.EmitMetadata, metadata, output);
            EncoderDrivers.DriveOp(s, BrotliEncoderOperation.Process, tail, output);
            EncoderDrivers.DriveOp(s, BrotliEncoderOperation.Finish, ReadOnlySpan<byte>.Empty, output);
            Assert.Equal(1, RawBrotli.BrotliEncoderIsFinished(s));
        }
        finally
        {
            RawBrotli.BrotliEncoderDestroyInstance(s);
        }

        byte[] compressed = output.ToArray();
        byte[] expected = new byte[head.Length + tail.Length];
        head.CopyTo(expected, 0);
        tail.CopyTo(expected, head.Length);
        /* Metadata blocks are skipped by decoders — payload only. */
        Assert.Equal(expected, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(expected, SystemBrotli.Decompress(compressed));
    }

    [Theory]
    [MemberData(nameof(CorpusQuality))]
    public void MaxCompressedSize_BoundsActualOutput(string name, int quality)
    {
        byte[] input = Corpus.Get(name);
        /* CompressOneShot compresses into a MaxCompressedSize-sized buffer and asserts
           the bound internally. */
        byte[] compressed = EncoderDrivers.CompressOneShot(input, quality, 22);
        Assert.True((nuint)compressed.Length
            <= RawBrotli.BrotliEncoderMaxCompressedSize((nuint)input.Length));
    }

    [Fact]
    public void MaxCompressedSize_OfEmptyInput_IsPositive()
    {
        Assert.True(RawBrotli.BrotliEncoderMaxCompressedSize(0) > 0);
        Assert.Equal((nuint)2, RawBrotli.BrotliEncoderMaxCompressedSize(0));
    }

    /* All qualities 0..11 are functional; the q10/q11 Zopfli path is gated by
       EncoderZopfliTests. */

    [Fact]
    public void EncoderVersion_Matches_1_1_0()
    {
        Assert.Equal((1u << 24) | (1u << 12) | 0u, RawBrotli.BrotliEncoderVersion());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EstimatePeakMemoryUsage_FastPath_IsPositive(int quality)
    {
        Assert.True(RawBrotli.BrotliEncoderEstimatePeakMemoryUsage(quality, 22, 1 << 20) > 0);
    }
}
