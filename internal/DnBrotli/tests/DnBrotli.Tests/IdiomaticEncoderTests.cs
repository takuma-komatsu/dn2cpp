using System.Buffers;
using DnBrotli.Tests.Oracles;
using DnBrotli.Tests.Support;
using BclBrotliEncoder = System.IO.Compression.BrotliEncoder;

namespace DnBrotli.Tests;

/// <summary>One <c>Compress</c>/<c>Flush</c> call's observable result — status plus the
/// (consumed, written) pair. Sequences are compared call-for-call between DnBrotli and the
/// BCL.</summary>
internal readonly record struct EncodeStep(OperationStatus Status, int Consumed, int Written);

/// <summary>
/// The idiomatic encoder layer (<see cref="BrotliEncoder"/>, the compress half of
/// <see cref="Brotli"/>) against the BCL's <c>System.IO.Compression.BrotliEncoder</c> (real
/// native brotli). The core guarantee is drop-in behavior: identical chunk schedules fed to
/// both encoders must produce identical <see cref="OperationStatus"/> sequences and
/// (consumed, written) pairs call-for-call — including the BCL's quirks (DestinationTooSmall
/// when the final block exactly fills the destination, InvalidData for input pushed after
/// finalization, argument-validation exception shapes). NOTE: call-for-call (written) parity
/// and the byte-equality assertions ride on DnBrotli's current byte-identity with native
/// brotli v1.1.0; like the encoder-path suites, they MAY be relaxed to round-trip checks if a
/// future BCL/brotli bump legitimately changes native output (PORTING.md pins round-trip facts
/// and DnBrotli's own determinism only).
/// </summary>
public class IdiomaticEncoderTests
{
    public static IEnumerable<object[]> CorpusNames() => Corpus.Names();

    // ==================== chunk-schedule drivers ====================

    private const int CanaryPad = 32;
    private const byte CanaryByte = 0xC5;

    private delegate OperationStatus CompressFn(
        ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten,
        bool isFinalBlock);
    private delegate OperationStatus FlushFn(Span<byte> destination, out int bytesWritten);

    /// <summary>Wraps the struct encoder in a class so a single instance (not per-call copies)
    /// is mutated across delegate invocations, and is reliably disposed.</summary>
    private sealed class DnEncoderStepper : IDisposable
    {
        private BrotliEncoder _encoder;

        public DnEncoderStepper(int quality, int window) => _encoder = new BrotliEncoder(quality, window);

        public OperationStatus Compress(
            ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten,
            bool isFinalBlock) =>
            _encoder.Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);

        public OperationStatus Flush(Span<byte> destination, out int bytesWritten) =>
            _encoder.Flush(destination, out bytesWritten);

        public void Dispose() => _encoder.Dispose();
    }

    private sealed class BclEncoderStepper : IDisposable
    {
        private BclBrotliEncoder _encoder;

        public BclEncoderStepper(int quality, int window) => _encoder = new BclBrotliEncoder(quality, window);

        public OperationStatus Compress(
            ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten,
            bool isFinalBlock) =>
            _encoder.Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);

        public OperationStatus Flush(Span<byte> destination, out int bytesWritten) =>
            _encoder.Flush(destination, out bytesWritten);

        public void Dispose() => _encoder.Dispose();
    }

    private static (List<EncodeStep> Steps, byte[] Output) DriveDn(
        byte[] input, int quality, int window, int inChunk, int outChunk, bool flushBetweenChunks)
    {
        using var stepper = new DnEncoderStepper(quality, window);
        return Drive(stepper.Compress, stepper.Flush, input, inChunk, outChunk, flushBetweenChunks);
    }

    private static (List<EncodeStep> Steps, byte[] Output) DriveBcl(
        byte[] input, int quality, int window, int inChunk, int outChunk, bool flushBetweenChunks)
    {
        using var stepper = new BclEncoderStepper(quality, window);
        return Drive(stepper.Compress, stepper.Flush, input, inChunk, outChunk, flushBetweenChunks);
    }

    /// <summary>The shared feeding protocol: process the input in <paramref name="inChunk"/>
    /// windows against <paramref name="outChunk"/> destinations (optionally flushing after each
    /// window), then finalize with empty final-block calls until Done. Enforces the hardening
    /// invariants: bounded zero-progress (never hangs) and canary bytes around the destination
    /// (never writes past the span).</summary>
    private static (List<EncodeStep> Steps, byte[] Output) Drive(
        CompressFn compress, FlushFn flush, byte[] input, int inChunk, int outChunk, bool flushBetweenChunks)
    {
        var steps = new List<EncodeStep>();
        var output = new MemoryStream();
        byte[] outer = new byte[outChunk + 2 * CanaryPad];
        outer.AsSpan().Fill(CanaryByte);
        int zeroProgress = 0;

        void Record(OperationStatus status, int consumed, int written, int maxConsumable)
        {
            steps.Add(new EncodeStep(status, consumed, written));
            Assert.InRange(consumed, 0, maxConsumable);
            Assert.InRange(written, 0, outChunk);
            AssertCanariesIntact(outer, outChunk);
            output.Write(outer, CanaryPad, written);
            zeroProgress = (consumed == 0 && written == 0) ? zeroProgress + 1 : 0;
            Assert.True(zeroProgress <= 4, "encoder made no forward progress (hang)");
        }

        int offset = 0;
        while (offset < input.Length)
        {
            int remaining = Math.Min(inChunk, input.Length - offset);
            while (remaining > 0)
            {
                OperationStatus status = compress(
                    input.AsSpan(offset, remaining), outer.AsSpan(CanaryPad, outChunk),
                    out int consumed, out int written, isFinalBlock: false);
                Record(status, consumed, written, remaining);
                offset += consumed;
                remaining -= consumed;
                Assert.True(status is OperationStatus.Done or OperationStatus.DestinationTooSmall,
                    $"unexpected OperationStatus {status}");
            }
            if (flushBetweenChunks)
            {
                OperationStatus status;
                do
                {
                    status = flush(outer.AsSpan(CanaryPad, outChunk), out int written);
                    Record(status, 0, written, 0);
                } while (status == OperationStatus.DestinationTooSmall);
                Assert.Equal(OperationStatus.Done, status);
            }
        }

        OperationStatus finishStatus;
        do
        {
            finishStatus = compress(
                ReadOnlySpan<byte>.Empty, outer.AsSpan(CanaryPad, outChunk),
                out int consumed, out int written, isFinalBlock: true);
            Record(finishStatus, consumed, written, 0);
        } while (finishStatus == OperationStatus.DestinationTooSmall);
        Assert.Equal(OperationStatus.Done, finishStatus);

        return (steps, output.ToArray());
    }

    private static void AssertCanariesIntact(byte[] outer, int outChunk)
    {
        for (int i = 0; i < CanaryPad; i++)
        {
            Assert.True(outer[i] == CanaryByte, $"canary before destination overwritten at {i}");
            Assert.True(outer[CanaryPad + outChunk + i] == CanaryByte,
                $"canary after destination overwritten at {i}");
        }
    }

    // ==================== chunked compression parity (call-for-call) ====================

    [Theory]
    // The killer resumability case: 1 byte in x 1 byte out.
    [InlineData("hello", 4, 22, 1, 1, false)]
    [InlineData("hello", 11, 22, 3, 2, true)]
    [InlineData("empty", 4, 22, 1, 1, false)]
    [InlineData("empty", 7, 22, 1, 1, true)]
    [InlineData("one-byte", 3, 22, 1, 1, true)]
    [InlineData("english-200k", 1, 22, 65536, 4096, false)]
    [InlineData("english-200k", 4, 22, 4093, 512, false)]
    [InlineData("english-200k", 9, 22, 8191, 1024, true)]
    [InlineData("zeros-70k", 4, 22, 7, 512, false)]
    [InlineData("zeros-70k", 2, 22, 1024, 64, true)]
    [InlineData("repeat-7-40k", 5, 10, 4093, 1, false)]
    [InlineData("utf8-64k", 11, 22, 65536, 4096, false)]
    [InlineData("random-64k", 4, 24, 65536, 65536, false)]
    [InlineData("random-64k", 0, 22, 7001, 900, true)]
    [InlineData("binary-structured-128k", 6, 22, 65536, 4096, true)]
    public void ChunkedCompressionMatchesBclCallForCall(
        string name, int quality, int window, int inChunk, int outChunk, bool flushBetweenChunks)
    {
        byte[] original = Corpus.Get(name);

        (List<EncodeStep> dnSteps, byte[] dnOutput) =
            DriveDn(original, quality, window, inChunk, outChunk, flushBetweenChunks);
        (List<EncodeStep> bclSteps, byte[] bclOutput) =
            DriveBcl(original, quality, window, inChunk, outChunk, flushBetweenChunks);

        Assert.Equal(bclSteps, dnSteps);  // statuses AND (consumed, written) pairs, call-for-call
        Assert.Equal(bclOutput, dnOutput);  // currently byte-identical (see the class doc caveat)
        Assert.Equal(original, SystemBrotli.Decompress(dnOutput));
        Assert.Equal(original, Brotli.Decompress(dnOutput));
    }

    // ==================== isFinalBlock semantics ====================

    [Fact]
    public void NonFinalCompressThatConsumesEverythingReturnsDoneLikeBcl()
    {
        byte[] original = Corpus.Get("hello");
        var dn = new DnEncoderStepper(4, 22);
        var bcl = new BclEncoderStepper(4, 22);
        try
        {
            byte[] dnBuf = new byte[1024];
            byte[] bclBuf = new byte[1024];
            OperationStatus dnStatus = dn.Compress(original, dnBuf, out int dnC, out int dnW, isFinalBlock: false);
            OperationStatus bclStatus = bcl.Compress(original, bclBuf, out int bclC, out int bclW, isFinalBlock: false);

            Assert.Equal(OperationStatus.Done, bclStatus);  // Done = "operation complete", not "stream finished"
            Assert.Equal(bclStatus, dnStatus);
            Assert.Equal((bclC, bclW), (dnC, dnW));
            Assert.Equal(original.Length, dnC);
        }
        finally
        {
            dn.Dispose();
            bcl.Dispose();
        }
    }

    [Fact]
    public void AfterFinalBlockDoneFurtherCallsBehaveLikeBcl()
    {
        byte[] original = Corpus.Get("hello");
        var dn = new DnEncoderStepper(4, 22);
        var bcl = new BclEncoderStepper(4, 22);
        try
        {
            byte[] dnBuf = new byte[1024];
            byte[] bclBuf = new byte[1024];
            Assert.Equal(OperationStatus.Done,
                bcl.Compress(original, bclBuf, out _, out int bclW, isFinalBlock: true));
            Assert.Equal(OperationStatus.Done,
                dn.Compress(original, dnBuf, out _, out int dnW, isFinalBlock: true));
            Assert.Equal(bclW, dnW);

            // Pushing input after finalization: InvalidData with nothing consumed or written.
            OperationStatus bclStatus = bcl.Compress(original, bclBuf, out int bclC, out bclW, isFinalBlock: true);
            OperationStatus dnStatus = dn.Compress(original, dnBuf, out int dnC, out dnW, isFinalBlock: true);
            Assert.Equal(OperationStatus.InvalidData, bclStatus);
            Assert.Equal(bclStatus, dnStatus);
            Assert.Equal((bclC, bclW), (dnC, dnW));

            bclStatus = bcl.Compress(original, bclBuf, out bclC, out bclW, isFinalBlock: false);
            dnStatus = dn.Compress(original, dnBuf, out dnC, out dnW, isFinalBlock: false);
            Assert.Equal(OperationStatus.InvalidData, bclStatus);
            Assert.Equal(bclStatus, dnStatus);
            Assert.Equal((bclC, bclW), (dnC, dnW));

            // Empty input after finalization stays Done, and Flush is a Done no-op.
            bclStatus = bcl.Compress(ReadOnlySpan<byte>.Empty, bclBuf, out bclC, out bclW, isFinalBlock: true);
            dnStatus = dn.Compress(ReadOnlySpan<byte>.Empty, dnBuf, out dnC, out dnW, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, bclStatus);
            Assert.Equal(bclStatus, dnStatus);
            Assert.Equal((bclC, bclW), (dnC, dnW));

            bclStatus = bcl.Flush(bclBuf, out bclW);
            dnStatus = dn.Flush(dnBuf, out dnW);
            Assert.Equal(OperationStatus.Done, bclStatus);
            Assert.Equal(bclStatus, dnStatus);
            Assert.Equal(bclW, dnW);
        }
        finally
        {
            dn.Dispose();
            bcl.Dispose();
        }
    }

    /// <summary>The BCL quirk: when the final block exactly fills the destination, Compress
    /// reports DestinationTooSmall; the next call reports Done with nothing written.</summary>
    [Fact]
    public void FinalBlockExactlyFillingDestinationReportsDestinationTooSmallLikeBcl()
    {
        byte[] original = Corpus.Get("hello");
        int exact;
        using (var probe = new DnEncoderStepper(4, 22))
        {
            Assert.Equal(OperationStatus.Done,
                probe.Compress(original, new byte[1024], out _, out exact, isFinalBlock: true));
        }

        var dn = new DnEncoderStepper(4, 22);
        var bcl = new BclEncoderStepper(4, 22);
        try
        {
            byte[] dnBuf = new byte[exact];
            byte[] bclBuf = new byte[exact];
            OperationStatus bclStatus = bcl.Compress(original, bclBuf, out int bclC, out int bclW, isFinalBlock: true);
            OperationStatus dnStatus = dn.Compress(original, dnBuf, out int dnC, out int dnW, isFinalBlock: true);
            Assert.Equal(OperationStatus.DestinationTooSmall, bclStatus);
            Assert.Equal(bclStatus, dnStatus);
            Assert.Equal((bclC, bclW), (dnC, dnW));
            Assert.Equal(exact, dnW);
            Assert.Equal(bclBuf, dnBuf);

            // The follow-up call completes with nothing further to write.
            bclStatus = bcl.Compress(ReadOnlySpan<byte>.Empty, bclBuf, out bclC, out bclW, isFinalBlock: true);
            dnStatus = dn.Compress(ReadOnlySpan<byte>.Empty, dnBuf, out dnC, out dnW, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, bclStatus);
            Assert.Equal(bclStatus, dnStatus);
            Assert.Equal((bclC, bclW), (dnC, dnW));
        }
        finally
        {
            dn.Dispose();
            bcl.Dispose();
        }
    }

    [Fact]
    public void EmptyDestinationReportsDestinationTooSmallLikeBcl()
    {
        var dn = new DnEncoderStepper(4, 22);
        var bcl = new BclEncoderStepper(4, 22);
        try
        {
            OperationStatus bclStatus = bcl.Compress(
                ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out int bclC, out int bclW, isFinalBlock: true);
            OperationStatus dnStatus = dn.Compress(
                ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out int dnC, out int dnW, isFinalBlock: true);
            Assert.Equal(OperationStatus.DestinationTooSmall, bclStatus);
            Assert.Equal(bclStatus, dnStatus);
            Assert.Equal((bclC, bclW), (dnC, dnW));
        }
        finally
        {
            dn.Dispose();
            bcl.Dispose();
        }
    }

    // ==================== Flush semantics ====================

    [Fact]
    public void FlushBeforeAnyInputEmitsTheSameHeaderBytesAsBcl()
    {
        var dn = new DnEncoderStepper(4, 22);
        var bcl = new BclEncoderStepper(4, 22);
        try
        {
            byte[] dnBuf = new byte[64];
            byte[] bclBuf = new byte[64];
            OperationStatus bclStatus = bcl.Flush(bclBuf, out int bclW);
            OperationStatus dnStatus = dn.Flush(dnBuf, out int dnW);
            Assert.Equal(OperationStatus.Done, bclStatus);
            Assert.Equal(bclStatus, dnStatus);
            Assert.Equal(bclW, dnW);
            Assert.Equal(bclBuf, dnBuf);
        }
        finally
        {
            dn.Dispose();
            bcl.Dispose();
        }
    }

    [Fact]
    public void DefaultInstanceCompressesAtEngineDefaultsLikeBcl()
    {
        byte[] original = Corpus.Get("hello");
        var dn = new BrotliDefaultStepper();
        var bcl = new BclDefaultStepper();
        try
        {
            byte[] dnBuf = new byte[1024];
            byte[] bclBuf = new byte[1024];
            OperationStatus dnStatus = dn.Compress(original, dnBuf, out int dnC, out int dnW);
            OperationStatus bclStatus = bcl.Compress(original, bclBuf, out int bclC, out int bclW);
            Assert.Equal(OperationStatus.Done, bclStatus);
            Assert.Equal(bclStatus, dnStatus);
            Assert.Equal((bclC, bclW), (dnC, dnW));
            Assert.Equal(bclBuf, dnBuf);  // engine defaults (q11/w22) match native's defaults
            Assert.Equal(original, SystemBrotli.Decompress(dnBuf.AsSpan(0, dnW).ToArray()));
        }
        finally
        {
            dn.Dispose();
            bcl.Dispose();
        }
    }

    private sealed class BrotliDefaultStepper : IDisposable
    {
        private BrotliEncoder _encoder;
        public OperationStatus Compress(ReadOnlySpan<byte> s, Span<byte> d, out int c, out int w) =>
            _encoder.Compress(s, d, out c, out w, isFinalBlock: true);
        public void Dispose() => _encoder.Dispose();
    }

    private sealed class BclDefaultStepper : IDisposable
    {
        private BclBrotliEncoder _encoder;
        public OperationStatus Compress(ReadOnlySpan<byte> s, Span<byte> d, out int c, out int w) =>
            _encoder.Compress(s, d, out c, out w, isFinalBlock: true);
        public void Dispose() => _encoder.Dispose();
    }

    // ==================== ctor validation and Dispose discipline ====================

    [Theory]
    [InlineData(-1, 22)]
    [InlineData(12, 22)]
    [InlineData(4, 9)]
    [InlineData(4, 25)]
    [InlineData(12, 9)]  // quality is validated first, like the BCL
    public void CtorRejectsOutOfRangeArgumentsLikeBcl(int quality, int window)
    {
        ArgumentOutOfRangeException bclEx = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BclBrotliEncoder(quality, window));
        ArgumentOutOfRangeException dnEx = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BrotliEncoder(quality, window));
        Assert.Equal(bclEx.ParamName, dnEx.ParamName);
    }

    [Fact]
    public void UseAfterDisposeThrowsObjectDisposedLikeBcl()
    {
        byte[] buffer = new byte[64];

        var bcl = new BclBrotliEncoder(4, 22);
        bcl.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
        {
            var copy = bcl;
            return copy.Compress(buffer, buffer, out _, out _, isFinalBlock: false);
        });

        var dn = new BrotliEncoder(4, 22);
        dn.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
        {
            var copy = dn;
            return copy.Compress(buffer, buffer, out _, out _, isFinalBlock: false);
        });
        Assert.Throws<ObjectDisposedException>(() =>
        {
            var copy = dn;
            return copy.Flush(buffer, out _);
        });
    }

    [Fact]
    public void DisposeIsIdempotentEvenBeforeFirstUse()
    {
        var used = new BrotliEncoder(4, 22);
        Assert.Equal(OperationStatus.Done,
            used.Compress(Corpus.Get("hello"), new byte[1024], out _, out _, isFinalBlock: true));
        used.Dispose();
        used.Dispose();

        var fresh = new BrotliEncoder(4, 22);
        fresh.Dispose();
        fresh.Dispose();
        default(BrotliEncoder).Dispose();
    }

    // ==================== GetMaxCompressedLength ====================

    [Fact]
    public void GetMaxCompressedLengthMatchesBclIncludingEdges()
    {
        foreach (int inputSize in (int[])[
            int.MinValue, -1, 0, 1, 2, 3, 100,
            (1 << 14) - 1, 1 << 14, (1 << 14) + 1, 1 << 20, (1 << 24) + 17,
            1_000_000_000, 2_146_000_000, 2_146_959_000, 2_146_970_000, 2_147_000_000,
            int.MaxValue - 1, int.MaxValue])
        {
            (long Value, string? ParamName) bcl = Probe(BclBrotliEncoder.GetMaxCompressedLength, inputSize);
            (long Value, string? ParamName) dn = Probe(BrotliEncoder.GetMaxCompressedLength, inputSize);
            Assert.Equal(bcl, dn);
        }

        Assert.Equal(2, BrotliEncoder.GetMaxCompressedLength(0));

        static (long Value, string? ParamName) Probe(Func<int, int> f, int inputSize)
        {
            try
            {
                return (f(inputSize), null);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return (-1, ex.ParamName);
            }
        }
    }

    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void GetMaxCompressedLengthIsASufficientOneShotBound(string name)
    {
        byte[] original = Corpus.Get(name);
        byte[] dest = new byte[BrotliEncoder.GetMaxCompressedLength(original.Length)];
        Assert.True(BrotliEncoder.TryCompress(original, dest, out int written));
        Assert.InRange(written, 1, dest.Length);
    }

    // ==================== TryCompress ====================

    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void TryCompressDefaultOverloadMatchesBcl(string name)
    {
        byte[] original = Corpus.Get(name);
        byte[] dnDest = new byte[BrotliEncoder.GetMaxCompressedLength(original.Length)];
        byte[] bclDest = new byte[dnDest.Length];

        bool dnOk = BrotliEncoder.TryCompress(original, dnDest, out int dnWritten);
        bool bclOk = BclBrotliEncoder.TryCompress(original, bclDest, out int bclWritten);

        Assert.True(bclOk);
        Assert.Equal(bclOk, dnOk);
        Assert.Equal(bclWritten, dnWritten);
        Assert.Equal(bclDest, dnDest);  // currently byte-identical (see the class doc caveat)
        Assert.Equal(original, SystemBrotli.Decompress(dnDest.AsSpan(0, dnWritten).ToArray()));
    }

    [Theory]
    [InlineData("hello", 1, 22)]
    [InlineData("hello", 11, 22)]
    [InlineData("english-200k", 5, 18)]
    [InlineData("english-200k", 9, 10)]
    [InlineData("zeros-70k", 0, 22)]
    [InlineData("random-64k", 4, 24)]
    public void TryCompressExplicitOverloadMatchesBcl(string name, int quality, int window)
    {
        byte[] original = Corpus.Get(name);
        byte[] dnDest = new byte[BrotliEncoder.GetMaxCompressedLength(original.Length)];
        byte[] bclDest = new byte[dnDest.Length];

        bool dnOk = BrotliEncoder.TryCompress(original, dnDest, out int dnWritten, quality, window);
        bool bclOk = BclBrotliEncoder.TryCompress(original, bclDest, out int bclWritten, quality, window);

        Assert.True(bclOk);
        Assert.Equal(bclOk, dnOk);
        Assert.Equal(bclWritten, dnWritten);
        Assert.Equal(bclDest, dnDest);
        Assert.Equal(original, SystemBrotli.Decompress(dnDest.AsSpan(0, dnWritten).ToArray()));
    }

    [Fact]
    public void TryCompressIntoTooSmallDestinationFailsLikeBcl()
    {
        byte[] original = Corpus.Get("hello");
        Assert.True(BclBrotliEncoder.TryCompress(original, new byte[1024], out int exact));

        foreach (int size in (int[])[0, 1, exact - 1])
        {
            bool dnOk = BrotliEncoder.TryCompress(original, new byte[size], out int dnWritten);
            bool bclOk = BclBrotliEncoder.TryCompress(original, new byte[size], out int bclWritten);
            Assert.False(bclOk);
            Assert.Equal(bclOk, dnOk);
            Assert.Equal(bclWritten, dnWritten);  // 0: the one-shot reports nothing on failure
        }
    }

    [Theory]
    [InlineData(-1, 22)]
    [InlineData(12, 22)]
    [InlineData(4, 9)]
    [InlineData(4, 25)]
    public void TryCompressRejectsOutOfRangeArgumentsLikeBcl(int quality, int window)
    {
        byte[] source = Corpus.Get("hello");
        byte[] dest = new byte[1024];
        ArgumentOutOfRangeException bclEx = Assert.Throws<ArgumentOutOfRangeException>(
            () => BclBrotliEncoder.TryCompress(source, dest, out _, quality, window));
        ArgumentOutOfRangeException dnEx = Assert.Throws<ArgumentOutOfRangeException>(
            () => BrotliEncoder.TryCompress(source, dest, out _, quality, window));
        Assert.Equal(bclEx.ParamName, dnEx.ParamName);
    }

    // ==================== Brotli.Compress (one-shot) ====================

    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void OneShotCompressRoundTripsThroughBothDecodersAtQ1Q4Q11(string name)
    {
        byte[] original = Corpus.Get(name);
        foreach (int quality in (int[])[1, 4, 11])
        {
            byte[] compressed = Brotli.Compress(original, quality);
            Assert.Equal(original, Brotli.Decompress(compressed));
            Assert.Equal(original, SystemBrotli.Decompress(compressed));
        }
    }

    [Fact]
    public void OneShotCompressHonorsExplicitWindowAndDefaults()
    {
        byte[] original = Corpus.Get("english-200k");

        // The window bits are encoded in the stream header, so a different window must be
        // visible in the output; the defaults must equal the explicit (4, 22) call.
        Assert.Equal(Brotli.Compress(original, 4, 22), Brotli.Compress(original));
        Assert.NotEqual(Brotli.Compress(original, 4, 18), Brotli.Compress(original));
        Assert.Equal(original, Brotli.Decompress(Brotli.Compress(original, 4, 18)));
    }

    [Fact]
    public void OneShotCompressOfEmptyInputYieldsAValidEmptyStream()
    {
        byte[] compressed = Brotli.Compress(ReadOnlySpan<byte>.Empty);
        Assert.NotEmpty(compressed);
        Assert.Empty(Brotli.Decompress(compressed));
        Assert.Empty(SystemBrotli.Decompress(compressed));
    }

    [Theory]
    [InlineData(-1, 22)]
    [InlineData(12, 22)]
    [InlineData(4, 9)]
    [InlineData(4, 25)]
    public void OneShotCompressRejectsOutOfRangeArguments(int quality, int window)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Brotli.Compress(Corpus.Get("hello"), quality, window));
    }

    [Fact]
    public void StaticTryCompressForwardsToBrotliEncoder()
    {
        byte[] original = Corpus.Get("hello");
        byte[] viaBrotli = new byte[BrotliEncoder.GetMaxCompressedLength(original.Length)];
        byte[] viaEncoder = new byte[viaBrotli.Length];

        Assert.True(Brotli.TryCompress(original, viaBrotli, out int brotliWritten));
        Assert.True(BrotliEncoder.TryCompress(original, viaEncoder, out int encoderWritten));
        Assert.Equal(encoderWritten, brotliWritten);
        Assert.Equal(viaEncoder, viaBrotli);

        Assert.True(Brotli.TryCompress(original, viaBrotli, out brotliWritten, 9, 20));
        Assert.True(BrotliEncoder.TryCompress(original, viaEncoder, out encoderWritten, 9, 20));
        Assert.Equal(encoderWritten, brotliWritten);
        Assert.Equal(viaEncoder, viaBrotli);

        Assert.False(Brotli.TryCompress(original, new byte[1], out _));
        Assert.False(Brotli.TryCompress(original, new byte[1], out _, 9, 20));
    }
}
