using DnBrotli.Enc;
using DnBrotli.Raw;
using DnBrotli.Tests.Oracles;
using DnBrotli.Tests.Support;

namespace DnBrotli.Tests;

/// <summary>
/// q10/q11 gate: the Zopfli path — the binary-tree hasher H10
/// (hash_to_binary_tree_inc.h), the shortest-path command search over the
/// float cost model (backward_references_hq.c: BrotliCreateZopfli- and
/// BrotliCreateHqZopfliBackwardReferences), and their encode.c routing.
/// Everything DnBrotli emits must decode identically through (a) DnBrotli's
/// own decoder and (b) the BCL decoder backed by real native brotli.
/// Compressed bytes are never asserted against native output as a contract
/// (implementation-defined); see the byte-identity note at the bottom.
/// </summary>
public sealed class EncoderZopfliTests
{
    public static IEnumerable<object[]> CorpusQualityWindow()
    {
        foreach ((string name, _) in Corpus.Entries)
        {
            foreach (int quality in new[] { 10, 11 })
            {
                foreach (int window in new[] { 10, 18, 22, 24 })
                {
                    yield return new object[] { name, quality, window };
                }
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

    /// <summary>Streaming with flushes after every chunk at Zopfli qualities —
    /// exercises H10's StitchToPreviousBlock and proves the buffer-all q10/q11
    /// path still honors Flush semantics: every flushed prefix must be a
    /// decodable stream prefix reproducing exactly the bytes fed so far.</summary>
    [Theory]
    [InlineData("english-200k", 11, 40960)]
    [InlineData("english-200k", 10, 40960)]
    [InlineData("binary-structured-128k", 11, 65536)]
    [InlineData("utf8-64k", 11, 8191)]
    [InlineData("hello", 11, 1)]
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
    /// consumes everything without error and returns the produced bytes (same helper
    /// shape as EncoderFastPathTests).</summary>
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

    /// <summary>Deterministic multi-metablock input: 4.5 MiB of record-structured
    /// pseudo-binary (the Corpus BinaryStructured recipe scaled past the 4 MiB mark)
    /// — several q11 metablocks through the EncodeData merge/flush machinery.</summary>
    private static byte[] BigStructured()
    {
        const int length = (4 * 1024 + 512) * 1024;
        var data = new byte[length];
        uint state = 0x0BADF00Du;
        for (int i = 0; i < length; i++)
        {
            int column = i % 32;
            if (column < 20)
            {
                data[i] = (byte)(column * 3);
            }
            else
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                data[i] = (byte)state;
            }
        }
        return data;
    }

    [Fact]
    public void MultiMetablock_4MiBPlus_Q11_RoundTrips_ThroughBothDecoders()
    {
        byte[] input = BigStructured();
        byte[] compressed = EncoderDrivers.CompressOneShot(input, 11, 22);

        Assert.Equal(input, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(input, SystemBrotli.Decompress(compressed));
    }

    /// <summary>Same multi-metablock input with an explicit small LgBlock — forces
    /// more, smaller input blocks through the q11 merge path (encode.c
    /// ComputeLgBlock's explicit-lgblock arm).</summary>
    [Theory]
    [InlineData(16u)]
    [InlineData(18u)]
    public unsafe void MultiMetablock_Q11_WithExplicitLgBlock_RoundTrips(uint lgblock)
    {
        byte[] input = BigStructured();
        var output = new MemoryStream();
        BrotliEncoderState* s = RawBrotli.BrotliEncoderCreateInstance();
        Assert.True(s != null);
        try
        {
            Assert.Equal(1, RawBrotli.BrotliEncoderSetParameter(
                s, BrotliEncoderParameter.Quality, 11));
            Assert.Equal(1, RawBrotli.BrotliEncoderSetParameter(
                s, BrotliEncoderParameter.LgWin, 22));
            Assert.Equal(1, RawBrotli.BrotliEncoderSetParameter(
                s, BrotliEncoderParameter.LgBlock, lgblock));
            EncoderDrivers.DriveOp(s, BrotliEncoderOperation.Process, input, output);
            EncoderDrivers.DriveOp(s, BrotliEncoderOperation.Finish, ReadOnlySpan<byte>.Empty, output);
            Assert.Equal(1, RawBrotli.BrotliEncoderIsFinished(s));
        }
        finally
        {
            RawBrotli.BrotliEncoderDestroyInstance(s);
        }
        byte[] compressed = output.ToArray();

        Assert.Equal(input, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(input, SystemBrotli.Decompress(compressed));
    }

    /// <summary>Ratio sanity on the dictionary-heavy entry: higher quality must not
    /// compress worse (q11 &lt;= q9 &lt;= q4 on english-200k).</summary>
    [Fact]
    public void HigherQuality_CompressesEnglishAtLeastAsWell()
    {
        byte[] input = Corpus.Get("english-200k");
        byte[] q4 = EncoderDrivers.CompressOneShot(input, 4, 22);
        byte[] q9 = EncoderDrivers.CompressOneShot(input, 9, 22);
        byte[] q11 = EncoderDrivers.CompressOneShot(input, 11, 22);

        Assert.Equal(input, EncoderDrivers.DecompressDn(q11));
        Assert.True(q9.Length <= q4.Length,
            $"expected q9 ({q9.Length} bytes) <= q4 ({q4.Length} bytes) on english-200k");
        Assert.True(q11.Length <= q9.Length,
            $"expected q11 ({q11.Length} bytes) <= q9 ({q9.Length} bytes) on english-200k");
    }

    /// <summary>Estimate covers the H10 arm via the real HasherSize.</summary>
    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    public void EstimatePeakMemoryUsage_ZopfliPath_IsPositive(int quality)
    {
        Assert.True(RawBrotli.BrotliEncoderEstimatePeakMemoryUsage(quality, 22, 1 << 20) > 0);
    }

    /// <summary>DnBrotli's q10/q11 one-shot output is currently byte-identical to
    /// native brotli v1.1.0 (the BCL encoder) for the whole corpus — a strong
    /// fidelity signal for the Zopfli path (H10 tree search, the float cost model's
    /// exact FP shapes, dist_cache propagation, command emission).
    /// NOTE: byte-identity with native output is NOT part of DnBrotli's contract
    /// (PORTING.md pins round-trip facts and DnBrotli's own determinism only); this
    /// assertion MAY be relaxed to a round-trip check when a future BCL/brotli bump
    /// legitimately changes native output. The "empty" entry is exempt only because
    /// the one-shot C path special-cases it while the streaming oracle does not.</summary>
    [Theory]
    [InlineData("one-byte", 10)]
    [InlineData("one-byte", 11)]
    [InlineData("two-bytes", 10)]
    [InlineData("two-bytes", 11)]
    [InlineData("hello", 10)]
    [InlineData("hello", 11)]
    [InlineData("zeros-1k", 10)]
    [InlineData("zeros-1k", 11)]
    [InlineData("zeros-70k", 10)]
    [InlineData("zeros-70k", 11)]
    [InlineData("repeat-7-40k", 10)]
    [InlineData("repeat-7-40k", 11)]
    [InlineData("english-200k", 10)]
    [InlineData("english-200k", 11)]
    [InlineData("utf8-64k", 10)]
    [InlineData("utf8-64k", 11)]
    [InlineData("random-64k", 10)]
    [InlineData("random-64k", 11)]
    [InlineData("random-3", 10)]
    [InlineData("random-3", 11)]
    [InlineData("binary-structured-128k", 10)]
    [InlineData("binary-structured-128k", 11)]
    public void OneShot_CurrentlyMatchesNativeBrotliBytes(string name, int quality)
    {
        byte[] input = Corpus.Get(name);
        byte[] mine = EncoderDrivers.CompressOneShot(input, quality, 22);
        byte[] native = SystemBrotli.Compress(input, quality, 22);
        Assert.Equal(native, mine);
    }

    /// <summary>Same byte-identity signal across the window sweep (the H10
    /// window_mask_/forest sizing arms). Same relaxation caveat as above.</summary>
    [Theory]
    [InlineData("english-200k", 10, 10)]
    [InlineData("english-200k", 10, 18)]
    [InlineData("english-200k", 10, 24)]
    [InlineData("english-200k", 11, 10)]
    [InlineData("english-200k", 11, 18)]
    [InlineData("english-200k", 11, 24)]
    [InlineData("repeat-7-40k", 10, 10)]
    [InlineData("repeat-7-40k", 11, 24)]
    public void OneShot_WindowSweep_CurrentlyMatchesNativeBrotliBytes(
        string name, int quality, int window)
    {
        byte[] input = Corpus.Get(name);
        byte[] mine = EncoderDrivers.CompressOneShot(input, quality, window);
        byte[] native = SystemBrotli.Compress(input, quality, window);
        Assert.Equal(native, mine);
    }

    /// <summary>The multi-metablock 4.5 MiB q11 stream is also currently
    /// byte-identical to native. Same relaxation caveat as above.</summary>
    [Fact]
    public void MultiMetablock_Q11_CurrentlyMatchesNativeBrotliBytes()
    {
        byte[] input = BigStructured();
        byte[] mine = EncoderDrivers.CompressOneShot(input, 11, 22);
        byte[] native = SystemBrotli.Compress(input, 11, 22);
        Assert.Equal(native, mine);
    }
}
