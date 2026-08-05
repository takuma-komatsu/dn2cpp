using System.Buffers;
using DnBrotli.Tests.Oracles;
using DnBrotli.Tests.Support;
using BclBrotliDecoder = System.IO.Compression.BrotliDecoder;

namespace DnBrotli.Tests;

/// <summary>
/// The idiomatic layer (<see cref="BrotliDecoder"/>, <see cref="Brotli"/>) against the BCL's
/// <c>System.IO.Compression.BrotliDecoder</c> (real native brotli). The core guarantee is
/// drop-in behavior: identical chunk schedules fed to both decoders must produce identical
/// <see cref="OperationStatus"/> sequences and (consumed, written) pairs call-for-call —
/// including the BCL's quirk of returning <see cref="OperationStatus.Done"/> forever (and
/// ignoring input) once the stream has ended.
/// </summary>
public class IdiomaticDecoderTests
{
    public static IEnumerable<object[]> CorpusNames() => Corpus.Names();

    // ==================== chunked resumption parity (call-for-call) ====================

    [Theory]
    // The killer resumability case: 1 byte in x 1 byte out.
    [InlineData("hello", 4, 1, 1)]
    [InlineData("empty", 4, 1, 1)]
    [InlineData("english-200k", 4, 1, 4096)]
    [InlineData("english-200k", 4, 4093, 512)]
    [InlineData("english-200k", 9, 65536, 65536)]
    [InlineData("zeros-70k", 4, 7, 512)]
    [InlineData("zeros-1k", 1, 3, 64)]
    [InlineData("repeat-7-40k", 5, 4093, 1)]
    [InlineData("utf8-64k", 11, 65536, 4096)]
    [InlineData("random-64k", 4, 7, 65536)]
    [InlineData("binary-structured-128k", 6, 65536, 4096)]
    public void ChunkedResumptionMatchesBclCallForCall(string name, int quality, int inChunk, int outChunk)
    {
        byte[] original = Corpus.Get(name);
        byte[] compressed = SystemBrotli.Compress(original, quality, window: 22);

        (List<DecodeStep> dnSteps, byte[] dnOutput) = DecoderDrivers.DriveDn(compressed, inChunk, outChunk);
        (List<DecodeStep> bclSteps, byte[] bclOutput) = DecoderDrivers.DriveBcl(compressed, inChunk, outChunk);

        Assert.Equal(bclSteps, dnSteps);  // statuses AND (consumed, written) pairs, call-for-call
        Assert.Equal(OperationStatus.Done, dnSteps[^1].Status);
        Assert.Equal(bclOutput, dnOutput);
        Assert.Equal(original, dnOutput);
    }

    // ==================== after-stream-end behavior (BCL quirk parity) ====================

    [Fact]
    public void AfterStreamEndKeepsReturningDoneAndIgnoresInputLikeBcl()
    {
        byte[] compressed = SystemBrotli.Compress(Corpus.Get("hello"), quality: 4, window: 22);
        var dn = new BrotliDecoder();
        var bcl = new BclBrotliDecoder();
        try
        {
            byte[] dnBuf = new byte[256];
            byte[] bclBuf = new byte[256];

            // First: pin down the actual BCL behavior this suite claims parity with.
            OperationStatus bclStatus = bcl.Decompress(compressed, bclBuf, out int bclConsumed, out int bclWritten);
            Assert.Equal(OperationStatus.Done, bclStatus);
            bclStatus = bcl.Decompress(compressed, bclBuf, out bclConsumed, out bclWritten);
            Assert.Equal(OperationStatus.Done, bclStatus);  // still Done after the end...
            Assert.Equal(0, bclConsumed);                    // ...with fresh input ignored
            Assert.Equal(0, bclWritten);

            // Now: DnBrotli matches, call for call.
            OperationStatus dnStatus = dn.Decompress(compressed, dnBuf, out int dnConsumed, out int dnWritten);
            Assert.Equal(OperationStatus.Done, dnStatus);
            for (int i = 0; i < 3; i++)
            {
                dnStatus = dn.Decompress(compressed, dnBuf, out dnConsumed, out dnWritten);
                Assert.Equal(OperationStatus.Done, dnStatus);
                Assert.Equal(0, dnConsumed);
                Assert.Equal(0, dnWritten);
            }
        }
        finally
        {
            dn.Dispose();
            bcl.Dispose();
        }
    }

    [Fact]
    public void TrailingBytesInTheFinalCallAreLeftUnconsumedLikeBcl()
    {
        byte[] original = Corpus.Get("hello");
        byte[] compressed = SystemBrotli.Compress(original, quality: 4, window: 22);
        byte[] extended = new byte[compressed.Length + 64];
        compressed.CopyTo(extended, 0);
        extended.AsSpan(compressed.Length).Fill(0x91);

        var dn = new BrotliDecoder();
        var bcl = new BclBrotliDecoder();
        try
        {
            byte[] dnBuf = new byte[256];
            byte[] bclBuf = new byte[256];
            OperationStatus dnStatus = dn.Decompress(extended, dnBuf, out int dnConsumed, out int dnWritten);
            OperationStatus bclStatus = bcl.Decompress(extended, bclBuf, out int bclConsumed, out int bclWritten);

            Assert.Equal(OperationStatus.Done, bclStatus);
            Assert.Equal(bclStatus, dnStatus);
            Assert.Equal(bclConsumed, dnConsumed);
            Assert.Equal(bclWritten, dnWritten);
            Assert.True(dnConsumed < extended.Length);  // trailing garbage was never consumed
            Assert.Equal(original, dnBuf.AsSpan(0, dnWritten).ToArray());
        }
        finally
        {
            dn.Dispose();
            bcl.Dispose();
        }
    }

    // ==================== Dispose discipline ====================

    [Fact]
    public void UseAfterDisposeThrowsObjectDisposedLikeBcl()
    {
        byte[] compressed = SystemBrotli.Compress(Corpus.Get("hello"), quality: 4, window: 22);
        byte[] buffer = new byte[64];

        var bcl = new BclBrotliDecoder();
        bcl.Decompress(compressed.AsSpan(0, 1), buffer, out _, out _);
        bcl.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
        {
            var copy = bcl;
            return copy.Decompress(compressed, new byte[64], out _, out _);
        });

        var dn = new BrotliDecoder();
        dn.Decompress(compressed.AsSpan(0, 1), buffer, out _, out _);
        dn.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
        {
            var copy = dn;
            return copy.Decompress(compressed, new byte[64], out _, out _);
        });
    }

    [Fact]
    public void DisposeIsIdempotentEvenBeforeFirstUse()
    {
        // Used instance: double dispose is safe.
        byte[] compressed = SystemBrotli.Compress(Corpus.Get("hello"), quality: 4, window: 22);
        var used = new BrotliDecoder();
        Assert.Equal(OperationStatus.Done,
            used.Decompress(compressed, new byte[64], out _, out _));
        used.Dispose();
        used.Dispose();

        // Never-used and default instances: dispose is a no-op, twice.
        var fresh = new BrotliDecoder();
        fresh.Dispose();
        fresh.Dispose();
        default(BrotliDecoder).Dispose();
    }

    // ==================== TryDecompress ====================

    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void TryDecompressMatchesBclOnExactAndOversizedDestinations(string name)
    {
        byte[] original = Corpus.Get(name);
        byte[] compressed = SystemBrotli.Compress(original, quality: 4, window: 22);

        foreach (int slack in (int[])[0, 17])
        {
            byte[] dnDest = new byte[original.Length + slack];
            byte[] bclDest = new byte[original.Length + slack];
            bool dnOk = BrotliDecoder.TryDecompress(compressed, dnDest, out int dnWritten);
            bool bclOk = BclBrotliDecoder.TryDecompress(compressed, bclDest, out int bclWritten);

            Assert.True(bclOk);
            Assert.Equal(bclOk, dnOk);
            Assert.Equal(bclWritten, dnWritten);
            Assert.Equal(original.Length, dnWritten);
            Assert.Equal(original, dnDest.AsSpan(0, dnWritten).ToArray());
        }
    }

    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void TryDecompressIntoTooSmallDestinationFailsLikeBcl(string name)
    {
        byte[] original = Corpus.Get(name);
        if (original.Length == 0)
        {
            return;  // no smaller destination exists for the empty entry
        }
        byte[] compressed = SystemBrotli.Compress(original, quality: 4, window: 22);

        byte[] dnDest = new byte[original.Length - 1];
        byte[] bclDest = new byte[original.Length - 1];
        bool dnOk = BrotliDecoder.TryDecompress(compressed, dnDest, out int dnWritten);
        bool bclOk = BclBrotliDecoder.TryDecompress(compressed, bclDest, out int bclWritten);

        Assert.False(bclOk);
        Assert.Equal(bclOk, dnOk);
        Assert.Equal(bclWritten, dnWritten);  // bytes produced before running out, C-style
        Assert.Equal(bclDest, dnDest);
    }

    [Fact]
    public void TryDecompressGarbageAndEmptyInputFailLikeBcl()
    {
        byte[] garbage = new byte[64];
        garbage.AsSpan().Fill(0x91);
        byte[] dest = new byte[128];

        Assert.False(BclBrotliDecoder.TryDecompress(garbage, dest, out int bclWritten));
        Assert.False(BrotliDecoder.TryDecompress(garbage, dest, out int dnWritten));
        Assert.Equal(bclWritten, dnWritten);

        Assert.False(BclBrotliDecoder.TryDecompress(ReadOnlySpan<byte>.Empty, dest, out bclWritten));
        Assert.False(BrotliDecoder.TryDecompress(ReadOnlySpan<byte>.Empty, dest, out dnWritten));
        Assert.Equal(bclWritten, dnWritten);
    }

    // ==================== Brotli.Decompress (one-shot) ====================

    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void OneShotDecompressReturnsExactBytesWithAndWithoutSizeHint(string name)
    {
        byte[] original = Corpus.Get(name);
        byte[] compressed = SystemBrotli.Compress(original, quality: 4, window: 22);

        // No hint (grows by doubling), exact hint, and a deliberately tiny hint (forces growth).
        Assert.Equal(original, Brotli.Decompress(compressed));
        Assert.Equal(original, Brotli.Decompress(compressed, sizeHint: original.Length));
        Assert.Equal(original, Brotli.Decompress(compressed, sizeHint: 3));
    }

    [Fact]
    public void OneShotDecompressOfEmptyStreamReturnsEmptyArray()
    {
        byte[] compressed = SystemBrotli.Compress(ReadOnlySpan<byte>.Empty, quality: 4, window: 22);
        Assert.Empty(Brotli.Decompress(compressed));
        Assert.Empty(Brotli.Decompress(compressed, sizeHint: 16));
    }

    [Fact]
    public void OneShotDecompressOfCorruptInputThrowsWithEngineErrorCode()
    {
        byte[] garbage = new byte[64];
        garbage.AsSpan().Fill(0x91);
        BrotliException ex = Assert.Throws<BrotliException>(() => Brotli.Decompress(garbage));
        Assert.Equal(BrotliDecoderErrorCode.ErrorFormatWindowBits, ex.ErrorCode);
    }

    [Fact]
    public void OneShotDecompressOfTruncatedOrEmptyInputThrowsNeedsMoreInput()
    {
        byte[] compressed = SystemBrotli.Compress(Corpus.Get("hello"), quality: 4, window: 22);
        byte[] truncated = compressed.AsSpan(0, compressed.Length - 1).ToArray();
        BrotliException ex = Assert.Throws<BrotliException>(() => Brotli.Decompress(truncated));
        Assert.Equal(BrotliDecoderErrorCode.NeedsMoreInput, ex.ErrorCode);

        ex = Assert.Throws<BrotliException>(() => Brotli.Decompress(ReadOnlySpan<byte>.Empty));
        Assert.Equal(BrotliDecoderErrorCode.NeedsMoreInput, ex.ErrorCode);
    }

    [Fact]
    public void StaticTryDecompressForwardsToBrotliDecoder()
    {
        byte[] original = Corpus.Get("hello");
        byte[] compressed = SystemBrotli.Compress(original, quality: 4, window: 22);
        byte[] dest = new byte[original.Length];

        Assert.True(Brotli.TryDecompress(compressed, dest, out int written));
        Assert.Equal(original.Length, written);
        Assert.Equal(original, dest);

        Assert.False(Brotli.TryDecompress(compressed, new byte[original.Length - 1], out _));
    }
}
