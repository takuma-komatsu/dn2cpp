using DnBrotli.Streams;
using DnBrotli.Tests.Oracles;
using DnBrotli.Tests.Support;
using BclBrotliStream = System.IO.Compression.BrotliStream;
using BclCompressionMode = System.IO.Compression.CompressionMode;

namespace DnBrotli.Tests;

/// <summary>
/// The <see cref="BrotliStream"/> class against the BCL's
/// <c>System.IO.Compression.BrotliStream</c>: round-trips over the corpus through both ctor
/// flavors and every <see cref="CompressionLevel"/>, sync and async, interop in both
/// directions (DnBrotli-compressed bytes decode through the BCL stream and vice versa),
/// flush-then-continue framing, leaveOpen/dispose discipline, and the BCL's mode/argument
/// exception shapes. The <see cref="BrotliCompressionOptions"/> ctor additionally pins the
/// window/mode knobs the BCL hides.
/// </summary>
public class StreamClassTests
{
    public static IEnumerable<object[]> CorpusNames() => Corpus.Names();

    private static byte[] CompressWithDnStream(
        byte[] input, Func<MemoryStream, BrotliStream> create, int writeChunk = 8192)
    {
        var output = new MemoryStream();
        using (BrotliStream stream = create(output))
        {
            for (int offset = 0; offset < input.Length; offset += writeChunk)
            {
                stream.Write(input, offset, Math.Min(writeChunk, input.Length - offset));
            }
        }
        return output.ToArray();
    }

    private static byte[] DecompressWithDnStream(byte[] compressed, int readChunk = 8192)
    {
        using var stream = new BrotliStream(new MemoryStream(compressed), CompressionMode.Decompress);
        var output = new MemoryStream();
        byte[] buffer = new byte[readChunk];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    private static byte[] DecompressWithBclStream(byte[] compressed)
    {
        using var stream = new BclBrotliStream(new MemoryStream(compressed), BclCompressionMode.Decompress);
        var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }

    // ==================== round-trips (both ctor flavors, all levels) ====================

    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void ModeCtorRoundTripsAndInteropsBothDirections(string name)
    {
        byte[] original = Corpus.Get(name);

        // DnBrotli stream compress -> DnBrotli stream decompress AND BCL stream decompress.
        byte[] compressed = CompressWithDnStream(
            original, ms => new BrotliStream(ms, CompressionMode.Compress, leaveOpen: true));
        Assert.Equal(original, DecompressWithDnStream(compressed));
        Assert.Equal(original, DecompressWithBclStream(compressed));

        // BCL stream compress -> DnBrotli stream decompress.
        var bclOutput = new MemoryStream();
        using (var bcl = new BclBrotliStream(bclOutput, BclCompressionMode.Compress, leaveOpen: true))
        {
            bcl.Write(original, 0, original.Length);
        }
        Assert.Equal(original, DecompressWithDnStream(bclOutput.ToArray()));
    }

    [Theory]
    [InlineData("hello", CompressionLevel.NoCompression)]
    [InlineData("hello", CompressionLevel.Fastest)]
    [InlineData("hello", CompressionLevel.Optimal)]
    [InlineData("hello", CompressionLevel.SmallestSize)]
    [InlineData("repeat-7-40k", CompressionLevel.NoCompression)]
    [InlineData("repeat-7-40k", CompressionLevel.Fastest)]
    [InlineData("repeat-7-40k", CompressionLevel.Optimal)]
    [InlineData("repeat-7-40k", CompressionLevel.SmallestSize)]
    [InlineData("random-64k", CompressionLevel.Fastest)]
    [InlineData("random-64k", CompressionLevel.Optimal)]
    [InlineData("english-200k", CompressionLevel.NoCompression)]
    [InlineData("english-200k", CompressionLevel.Fastest)]
    [InlineData("english-200k", CompressionLevel.Optimal)]
    [InlineData("english-200k", CompressionLevel.SmallestSize)]
    public void LevelCtorRoundTripsAndMatchesTheBclQualityMapping(string name, CompressionLevel level)
    {
        byte[] original = Corpus.Get(name);
        byte[] compressed = CompressWithDnStream(
            original, ms => new BrotliStream(ms, level, leaveOpen: true));
        Assert.Equal(original, DecompressWithDnStream(compressed));
        Assert.Equal(original, DecompressWithBclStream(compressed));

        // The BCL maps NoCompression/Fastest/Optimal/SmallestSize to q0/q1/q4/q11 (window 22).
        // Drive the BCL BrotliStream with the identical ctor flavor and write schedule (the
        // q0/q1 fragment paths are chunk-schedule-dependent): identical bytes prove the
        // identical mapping. (Byte equality with native output is a current-state signal,
        // same caveat as the encoder suites.)
        var bclOutput = new MemoryStream();
        using (var bcl = new BclBrotliStream(
            bclOutput, (System.IO.Compression.CompressionLevel)(int)level, leaveOpen: true))
        {
            for (int offset = 0; offset < original.Length; offset += 8192)
            {
                bcl.Write(original, offset, Math.Min(8192, original.Length - offset));
            }
        }
        Assert.Equal(bclOutput.ToArray(), compressed);
    }

    [Fact]
    public void LevelCtorRejectsUndefinedLevelsLikeBcl()
    {
        var ms = new MemoryStream();
        Assert.Throws<ArgumentException>(
            () => new BclBrotliStream(ms, (System.IO.Compression.CompressionLevel)42));
        Assert.Throws<ArgumentException>(() => new BrotliStream(ms, (CompressionLevel)42));
    }

    // ==================== options ctor (the DnZlib-style bonus knobs) ====================

    [Fact]
    public void OptionsCtorHonorsQualityAndWindow()
    {
        byte[] original = Corpus.Get("english-200k");
        var options = new BrotliCompressionOptions { Quality = 9, Window = 20 };
        byte[] compressed = CompressWithDnStream(
            original, ms => new BrotliStream(ms, options, leaveOpen: true));

        Assert.Equal(original, DecompressWithDnStream(compressed));
        Assert.Equal(original, DecompressWithBclStream(compressed));
        // Quality and window (encoded in the stream header) must both have been applied:
        // the bytes match a native streaming encode at q9/w20 and differ from the defaults.
        Assert.Equal(SystemBrotli.Compress(original, quality: 9, window: 20), compressed);
        Assert.NotEqual(SystemBrotli.Compress(original, quality: 4, window: 22), compressed);
    }

    [Theory]
    [InlineData(BrotliEncoderMode.Generic)]
    [InlineData(BrotliEncoderMode.Text)]
    [InlineData(BrotliEncoderMode.Font)]
    public void OptionsCtorModeKnobNeverBreaksTheRoundTrip(BrotliEncoderMode mode)
    {
        byte[] original = Corpus.Get("utf8-64k");
        var options = new BrotliCompressionOptions { Quality = 5, Mode = mode };
        byte[] compressed = CompressWithDnStream(
            original, ms => new BrotliStream(ms, options, leaveOpen: true));
        Assert.Equal(original, DecompressWithDnStream(compressed));
        Assert.Equal(original, DecompressWithBclStream(compressed));
    }

    [Fact]
    public void OptionsValidateTheirRanges()
    {
        var options = new BrotliCompressionOptions();
        Assert.Equal(4, options.Quality);
        Assert.Equal(22, options.Window);
        Assert.Equal(BrotliEncoderMode.Generic, options.Mode);

        Assert.Throws<ArgumentOutOfRangeException>(() => options.Quality = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Quality = 12);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Window = 9);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Window = 25);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Mode = (BrotliEncoderMode)3);
        Assert.Equal(4, options.Quality);  // failed sets leave the old values intact
        Assert.Equal(22, options.Window);

        Assert.Throws<ArgumentNullException>(
            () => new BrotliStream(new MemoryStream(), (BrotliCompressionOptions)null!));
    }

    // ==================== chunked and async I/O ====================

    [Theory]
    [InlineData("hello", 1, 1)]
    [InlineData("zeros-70k", 1, 4096)]     // 1-byte writes
    [InlineData("zeros-70k", 4096, 1)]     // 1-byte reads
    [InlineData("english-200k", 4093, 511)]
    [InlineData("binary-structured-128k", 65536, 65536)]
    public void ChunkedWritesAndReadsRoundTrip(string name, int writeChunk, int readChunk)
    {
        byte[] original = Corpus.Get(name);
        byte[] compressed = CompressWithDnStream(
            original, ms => new BrotliStream(ms, CompressionMode.Compress, leaveOpen: true), writeChunk);
        Assert.Equal(original, DecompressWithDnStream(compressed, readChunk));
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("english-200k")]
    [InlineData("random-64k")]
    public async Task AsyncWriteFlushDisposeAndReadRoundTrip(string name)
    {
        byte[] original = Corpus.Get(name);

        var output = new MemoryStream();
        var compressor = new BrotliStream(output, CompressionMode.Compress, leaveOpen: true);
        await using (compressor.ConfigureAwait(false))
        {
            int half = original.Length / 2;
            await compressor.WriteAsync(original.AsMemory(0, half));
            await compressor.FlushAsync(CancellationToken.None);
            await compressor.WriteAsync(original.AsMemory(half));
        }
        byte[] compressed = output.ToArray();
        Assert.Equal(original, DecompressWithBclStream(compressed));

        var decompressor = new BrotliStream(new MemoryStream(compressed), CompressionMode.Decompress);
        await using (decompressor.ConfigureAwait(false))
        {
            var result = new MemoryStream();
            byte[] buffer = new byte[4093];
            int read;
            while ((read = await decompressor.ReadAsync(buffer)) > 0)
            {
                result.Write(buffer, 0, read);
            }
            Assert.Equal(original, result.ToArray());
        }
    }

    [Fact]
    public async Task CopyToAndCopyToAsyncDecompressTheWholeStream()
    {
        byte[] original = Corpus.Get("english-200k");
        byte[] compressed = Brotli.Compress(original);

        using (var stream = new BrotliStream(new MemoryStream(compressed), CompressionMode.Decompress))
        {
            var output = new MemoryStream();
            stream.CopyTo(output);
            Assert.Equal(original, output.ToArray());
        }

        var decompressor = new BrotliStream(new MemoryStream(compressed), CompressionMode.Decompress);
        await using (decompressor.ConfigureAwait(false))
        {
            var output = new MemoryStream();
            await decompressor.CopyToAsync(output);
            Assert.Equal(original, output.ToArray());
        }
    }

    // ==================== Flush framing ====================

    [Fact]
    public void FlushMidWriteMakesAPrefixDecodableByTheBclThenTheStreamContinues()
    {
        byte[] original = Corpus.Get("english-200k");
        int half = original.Length / 2;

        var output = new MemoryStream();
        using (var stream = new BrotliStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            stream.Write(original, 0, half);
            stream.Flush();

            // Everything flushed so far is a decodable prefix (BCL non-strict mode tolerates
            // the missing final block and returns the bytes decoded so far).
            byte[] prefix = output.ToArray();
            Assert.Equal(original.AsSpan(0, half).ToArray(), DecompressWithBclStream(prefix));

            stream.Write(original, half, original.Length - half);
        }

        Assert.Equal(original, DecompressWithBclStream(output.ToArray()));
        Assert.Equal(original, DecompressWithDnStream(output.ToArray()));
    }

    [Fact]
    public void FlushOnAFreshCompressorEmitsTheSameBytesAsBcl()
    {
        var dnOut = new MemoryStream();
        var bclOut = new MemoryStream();
        using (var dn = new BrotliStream(dnOut, CompressionMode.Compress, leaveOpen: true))
        using (var bcl = new BclBrotliStream(bclOut, BclCompressionMode.Compress, leaveOpen: true))
        {
            dn.Flush();
            bcl.Flush();
            Assert.Equal(bclOut.ToArray(), dnOut.ToArray());
        }
        Assert.Equal(bclOut.ToArray(), dnOut.ToArray());  // dispose trailer also matches
        Assert.Empty(DecompressWithBclStream(dnOut.ToArray()));
    }

    [Fact]
    public void FlushOnADecompressorIsANoOpLikeBcl()
    {
        byte[] compressed = Brotli.Compress(Corpus.Get("hello"));
        using var stream = new BrotliStream(new MemoryStream(compressed), CompressionMode.Decompress);
        stream.Flush();  // must not throw, must not consume anything
        Assert.Equal(Corpus.Get("hello"), DecompressWithDnStream(compressed));
    }

    // ==================== dispose discipline ====================

    [Fact]
    public void DisposeWritesTheFinalBlockEvenWithNoWrites()
    {
        var output = new MemoryStream();
        using (new BrotliStream(output, CompressionMode.Compress, leaveOpen: true))
        {
        }
        byte[] compressed = output.ToArray();
        Assert.NotEmpty(compressed);
        Assert.Empty(DecompressWithBclStream(compressed));
        Assert.Empty(Brotli.Decompress(compressed));
    }

    [Fact]
    public async Task DoubleDisposeIsSafeAndSecondDisposeWritesNothing()
    {
        byte[] original = Corpus.Get("hello");
        var output = new MemoryStream();
        var stream = new BrotliStream(output, CompressionMode.Compress, leaveOpen: true);
        stream.Write(original, 0, original.Length);
        stream.Dispose();
        long lengthAfterFirstDispose = output.Length;
        stream.Dispose();
        await stream.DisposeAsync();
        Assert.Equal(lengthAfterFirstDispose, output.Length);
        Assert.Equal(original, DecompressWithBclStream(output.ToArray()));

        var reader = new BrotliStream(new MemoryStream(output.ToArray()), CompressionMode.Decompress);
        reader.Dispose();
        reader.Dispose();
        await reader.DisposeAsync();
    }

    [Fact]
    public void LeaveOpenTrueKeepsTheBaseStreamUsable()
    {
        byte[] original = Corpus.Get("hello");
        var output = new MemoryStream();
        using (var stream = new BrotliStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            stream.Write(original, 0, original.Length);
        }
        Assert.True(output.CanWrite);  // still open
        output.WriteByte(0xFF);        // and usable

        byte[] compressed = output.ToArray().AsSpan(0, (int)output.Length - 1).ToArray();
        Assert.Equal(original, DecompressWithBclStream(compressed));
    }

    [Fact]
    public void LeaveOpenFalseDisposesTheBaseStream()
    {
        var output = new MemoryStream();
        using (var stream = new BrotliStream(output, CompressionMode.Compress))
        {
            stream.Write(Corpus.Get("hello"), 0, 5);
        }
        Assert.False(output.CanWrite);  // MemoryStream reports unusable once disposed

        byte[] compressed = Brotli.Compress(Corpus.Get("hello"));
        var input = new MemoryStream(compressed);
        using (new BrotliStream(input, CompressionMode.Decompress))
        {
        }
        Assert.False(input.CanRead);
    }

    [Fact]
    public void UseAfterDisposeThrowsObjectDisposed()
    {
        var stream = new BrotliStream(new MemoryStream(), CompressionMode.Compress, leaveOpen: true);
        stream.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[1], 0, 1));
        Assert.Throws<ObjectDisposedException>(() => stream.Flush());
        Assert.False(stream.CanWrite);

        var reader = new BrotliStream(new MemoryStream(new byte[4]), CompressionMode.Decompress);
        reader.Dispose();
        Assert.Throws<ObjectDisposedException>(() => reader.Read(new byte[4], 0, 4));
        Assert.False(reader.CanRead);
    }

    // ==================== mode/argument exception parity ====================

    [Fact]
    public void ReadOnACompressStreamThrowsLikeBcl()
    {
        using var dn = new BrotliStream(new MemoryStream(), CompressionMode.Compress, leaveOpen: true);
        using var bcl = new BclBrotliStream(new MemoryStream(), BclCompressionMode.Compress, leaveOpen: true);
        Assert.Throws<InvalidOperationException>(() => bcl.Read(new byte[4], 0, 4));
        Assert.Throws<InvalidOperationException>(() => dn.Read(new byte[4], 0, 4));
        Assert.False(dn.CanRead);
        Assert.True(dn.CanWrite);
    }

    [Fact]
    public async Task WriteOnADecompressStreamThrowsLikeBcl()
    {
        using var dn = new BrotliStream(new MemoryStream(new byte[4]), CompressionMode.Decompress);
        using var bcl = new BclBrotliStream(new MemoryStream(new byte[4]), BclCompressionMode.Decompress);
        Assert.Throws<InvalidOperationException>(() => bcl.Write(new byte[4], 0, 4));
        Assert.Throws<InvalidOperationException>(() => dn.Write(new byte[4], 0, 4));
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dn.WriteAsync(new byte[4].AsMemory()));
        Assert.True(dn.CanRead);
        Assert.False(dn.CanWrite);
    }

    [Fact]
    public void ReadingGarbageThrowsTheSameExceptionTypeAsBcl()
    {
        byte[] garbage = new byte[64];
        garbage.AsSpan().Fill(0x91);

        Exception bclEx = Record.Exception(() =>
        {
            using var bcl = new BclBrotliStream(new MemoryStream(garbage), BclCompressionMode.Decompress);
            bcl.CopyTo(new MemoryStream());
        })!;
        Exception dnEx = Record.Exception(() =>
        {
            using var dn = new BrotliStream(new MemoryStream(garbage), CompressionMode.Decompress);
            dn.CopyTo(new MemoryStream());
        })!;
        Assert.NotNull(bclEx);
        Assert.NotNull(dnEx);
        Assert.Equal(bclEx.GetType(), dnEx.GetType());  // InvalidOperationException, historically
    }

    [Fact]
    public void CtorRejectsBadStreamsAndModesLikeBcl()
    {
        Assert.Throws<ArgumentNullException>(() => new BrotliStream(null!, CompressionMode.Compress));
        Assert.Throws<ArgumentException>(() => new BrotliStream(new MemoryStream(), (CompressionMode)42));

        // Not writable -> compression refused; not readable -> decompression refused.
        var readOnly = new MemoryStream(new byte[4], writable: false);
        Assert.Throws<ArgumentException>(() => new BrotliStream(readOnly, CompressionMode.Compress));
        var writeOnly = new WriteOnlyStream();
        Assert.Throws<ArgumentException>(() => new BrotliStream(writeOnly, CompressionMode.Decompress));
    }

    [Fact]
    public void NotSupportedSurfaceMatchesTheBclShape()
    {
        byte[] compressed = Brotli.Compress(Corpus.Get("hello"));
        using var stream = new BrotliStream(new MemoryStream(compressed), CompressionMode.Decompress);
        Assert.False(stream.CanSeek);
        Assert.Throws<NotSupportedException>(() => stream.Length);
        Assert.Throws<NotSupportedException>(() => stream.Position);
        Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        Assert.Throws<NotSupportedException>(() => stream.SetLength(1));
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
    }

    [Fact]
    public void BaseStreamExposesTheUnderlyingStream()
    {
        var ms = new MemoryStream();
        using var stream = new BrotliStream(ms, CompressionMode.Compress, leaveOpen: true);
        Assert.Same(ms, stream.BaseStream);
    }

    private sealed class WriteOnlyStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush()
        {
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }
}
