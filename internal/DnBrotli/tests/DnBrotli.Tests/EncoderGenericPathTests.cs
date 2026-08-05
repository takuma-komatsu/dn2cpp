using DnBrotli;
using DnBrotli.Enc;
using DnBrotli.Raw;
using DnBrotli.Tests.Oracles;
using DnBrotli.Tests.Support;

namespace DnBrotli.Tests;

/// <summary>
/// q4..q9 gate: the generic encoder path — chained hashers H5/H6
/// (hash_longest_match[64]_inc.h) and forgetful-chain hashers H40/H41/H42
/// (hash_forgetful_chain_inc.h), block splitting (block_splitter.c + cluster.c),
/// metablock building (metablock.c, greedy + context modeling decisions from
/// encode.c) and the general BrotliStoreMetaBlock (block-switch commands +
/// context maps). Everything DnBrotli emits must decode identically through
/// (a) DnBrotli's own decoder and (b) the BCL decoder backed by real native
/// brotli. Compressed bytes are never asserted against native output as a
/// contract (implementation-defined); see the byte-identity note at the bottom.
/// </summary>
public sealed class EncoderGenericPathTests
{
    public static IEnumerable<object[]> CorpusQualityWindow()
    {
        foreach ((string name, _) in Corpus.Entries)
        {
            foreach (int quality in new[] { 4, 5, 6, 7, 8, 9 })
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

    /// <summary>Streaming with flushes after every chunk — exercises
    /// StitchToPreviousBlock on the chained/forgetful hashers and the flush framing
    /// on the generic path.</summary>
    [Theory]
    [InlineData("english-200k", 5, 4099)]
    [InlineData("english-200k", 9, 4099)]
    [InlineData("utf8-64k", 6, 4099)]
    [InlineData("zeros-70k", 7, 4099)]
    [InlineData("hello", 5, 1)]
    [InlineData("hello", 9, 1)]
    public void Streaming_WithFlushes_RoundTrips(string name, int quality, int chunkSize)
    {
        byte[] input = Corpus.Get(name);
        byte[] compressed = EncoderDrivers.CompressStreaming(
            input, quality, window: 22, chunkSize, flushBetweenChunks: true);

        Assert.Equal(input, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(input, SystemBrotli.Decompress(compressed));
    }

    /// <summary>Streaming compression with explicit encoder parameters (size hint /
    /// literal-context-modeling switch) — the knobs that steer ChooseHasher and the
    /// context-modeling decisions.</summary>
    private static unsafe byte[] CompressWithParams(
        byte[] input, int quality, int window,
        uint? sizeHint = null, bool disableLiteralContextModeling = false)
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
            if (sizeHint is uint hint)
            {
                Assert.Equal(1, RawBrotli.BrotliEncoderSetParameter(
                    s, BrotliEncoderParameter.SizeHint, hint));
            }
            if (disableLiteralContextModeling)
            {
                Assert.Equal(1, RawBrotli.BrotliEncoderSetParameter(
                    s, BrotliEncoderParameter.DisableLiteralContextModeling, 1));
            }
            EncoderDrivers.DriveOp(s, BrotliEncoderOperation.Process, input, output);
            EncoderDrivers.DriveOp(s, BrotliEncoderOperation.Finish, ReadOnlySpan<byte>.Empty, output);
            Assert.Equal(1, RawBrotli.BrotliEncoderIsFinished(s));
        }
        finally
        {
            RawBrotli.BrotliEncoderDestroyInstance(s);
        }
        return output.ToArray();
    }

    /// <summary>Hasher-routing matrix: each row pins one arm of ChooseHasher
    /// (c/enc/quality.h), verified against the C source:
    ///   - q4 + size_hint &gt;= 1 MiB                        -&gt; H54
    ///   - q5..q9, lgwin &gt; 16, size_hint &lt; 1 MiB        -&gt; H5
    ///   - q5..q9, lgwin &gt;= 19, size_hint &gt;= 1 MiB      -&gt; H6
    ///   - lgwin &lt;= 16: q5/q6 -&gt; H40, q7/q8 -&gt; H41, q9 -&gt; H42
    /// Every arm must produce output that round-trips through both decoders.</summary>
    [Theory]
    /* H54: q4 with a large size hint. */
    [InlineData("english-200k", 4, 22, 1u << 20, false)]
    /* H5: default arm for q5..q9 at lgwin > 16 with a small size hint. */
    [InlineData("english-200k", 5, 22, null, false)]
    [InlineData("english-200k", 6, 22, null, false)]
    [InlineData("english-200k", 7, 22, null, false)]
    [InlineData("english-200k", 9, 22, null, false)]
    /* H6: size_hint >= 1 MiB and lgwin >= 19 (block_bits = quality - 1). */
    [InlineData("english-200k", 5, 19, 1u << 20, false)]
    [InlineData("english-200k", 7, 22, 1u << 20, false)]
    [InlineData("english-200k", 9, 24, 1u << 20, false)]
    /* H40: lgwin <= 16, q5/q6. */
    [InlineData("english-200k", 5, 10, null, false)]
    [InlineData("english-200k", 6, 14, null, false)]
    [InlineData("utf8-64k", 5, 16, null, false)]
    /* H41: lgwin <= 16, q7/q8. */
    [InlineData("english-200k", 7, 14, null, false)]
    [InlineData("english-200k", 8, 16, null, false)]
    /* H42: lgwin <= 16, q9. */
    [InlineData("english-200k", 9, 10, null, false)]
    [InlineData("english-200k", 9, 16, null, false)]
    public void HasherRouting_EveryArm_RoundTrips(
        string name, int quality, int window, uint? sizeHint, bool disableCtx)
    {
        byte[] input = Corpus.Get(name);
        byte[] compressed = CompressWithParams(input, quality, window, sizeHint, disableCtx);

        Assert.Equal(input, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(input, SystemBrotli.Decompress(compressed));
    }

    /// <summary>Context-modeling coverage at q5+ (DecideOverLiteralContextModeling /
    /// ChooseContextMap): UTF-8 text and English text take the bigram-analysis path;
    /// a size hint &gt;= 1 MiB additionally arms ShouldUseComplexStaticContextMap
    /// (the 13-context static map probe).</summary>
    [Theory]
    [InlineData("utf8-64k", 5, null)]
    [InlineData("utf8-64k", 7, null)]
    [InlineData("utf8-64k", 9, null)]
    [InlineData("english-200k", 5, null)]
    [InlineData("english-200k", 7, null)]
    [InlineData("english-200k", 9, null)]
    [InlineData("english-200k", 5, 1u << 20)]
    [InlineData("utf8-64k", 9, 1u << 20)]
    public void ContextModeling_RoundTrips(string name, int quality, uint? sizeHint)
    {
        byte[] input = Corpus.Get(name);
        byte[] compressed = CompressWithParams(input, quality, 22, sizeHint);

        Assert.Equal(input, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(input, SystemBrotli.Decompress(compressed));
    }

    /// <summary><c>BROTLI_PARAM_DISABLE_LITERAL_CONTEXT_MODELING</c> forces the trivial
    /// literal context map (and the context-map redistribution in BrotliBuildMetaBlock);
    /// the output must still round-trip.</summary>
    [Theory]
    [InlineData("english-200k", 5)]
    [InlineData("english-200k", 9)]
    [InlineData("utf8-64k", 7)]
    public void DisableLiteralContextModeling_RoundTrips(string name, int quality)
    {
        byte[] input = Corpus.Get(name);
        byte[] compressed = CompressWithParams(
            input, quality, 22, disableLiteralContextModeling: true);

        Assert.Equal(input, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(input, SystemBrotli.Decompress(compressed));
    }

    /// <summary>Ratio sanity on dictionary-heavy text: higher qualities must not
    /// compress worse (non-brittle inequalities only, no absolute byte counts).</summary>
    [Fact]
    public void HigherQuality_CompressesNoWorse_OnEnglishText()
    {
        byte[] input = Corpus.Get("english-200k");
        byte[] q2 = EncoderDrivers.CompressOneShot(input, 2, 22);
        byte[] q4 = EncoderDrivers.CompressOneShot(input, 4, 22);
        byte[] q9 = EncoderDrivers.CompressOneShot(input, 9, 22);

        Assert.Equal(input, EncoderDrivers.DecompressDn(q4));
        Assert.Equal(input, EncoderDrivers.DecompressDn(q9));
        Assert.True(q4.Length <= q2.Length,
            $"expected q4 ({q4.Length} bytes) <= q2 ({q2.Length} bytes) on english-200k");
        Assert.True(q9.Length <= q4.Length,
            $"expected q9 ({q9.Length} bytes) <= q4 ({q4.Length} bytes) on english-200k");
    }

    /// <summary>Estimate covers the generic-path hashers via the real HasherSize.</summary>
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void EstimatePeakMemoryUsage_GenericPath_IsPositive(int quality)
    {
        Assert.True(RawBrotli.BrotliEncoderEstimatePeakMemoryUsage(quality, 22, 1 << 20) > 0);
    }

    /// <summary>DnBrotli's q4..q9 one-shot output is currently byte-identical to native
    /// brotli v1.1.0 (the BCL encoder) for these entries — a strong fidelity signal for
    /// the generic path (hashers, block splitter FP, clustering, context maps, stores).
    /// NOTE: byte-identity with native output is NOT part of DnBrotli's contract
    /// (PORTING.md pins round-trip facts and DnBrotli's own determinism only); this
    /// assertion MAY be relaxed to a round-trip check when a future BCL/brotli bump
    /// legitimately changes native output.</summary>
    [Theory]
    [InlineData("hello", 4)]
    [InlineData("hello", 5)]
    [InlineData("hello", 9)]
    [InlineData("english-200k", 4)]
    [InlineData("english-200k", 5)]
    [InlineData("english-200k", 6)]
    [InlineData("english-200k", 7)]
    [InlineData("english-200k", 8)]
    [InlineData("english-200k", 9)]
    [InlineData("repeat-7-40k", 4)]
    [InlineData("repeat-7-40k", 9)]
    [InlineData("utf8-64k", 5)]
    [InlineData("utf8-64k", 9)]
    [InlineData("binary-structured-128k", 6)]
    [InlineData("binary-structured-128k", 9)]
    [InlineData("random-64k", 4)]
    [InlineData("random-64k", 9)]
    [InlineData("zeros-70k", 5)]
    public void OneShot_CurrentlyMatchesNativeBrotliBytes(string name, int quality)
    {
        byte[] input = Corpus.Get(name);
        byte[] mine = EncoderDrivers.CompressOneShot(input, quality, 22);
        byte[] native = SystemBrotli.Compress(input, quality, 22);
        Assert.Equal(native, mine);
    }

    /// <summary>Same byte-identity signal on the small-window (forgetful-chain
    /// H40/H41/H42) arms — one-shot at lgwin &lt;= 16 routes native brotli and
    /// DnBrotli through the identical ChooseHasher arm. Same relaxation caveat as
    /// above.</summary>
    [Theory]
    [InlineData("english-200k", 5, 10)]
    [InlineData("english-200k", 6, 16)]
    [InlineData("english-200k", 7, 14)]
    [InlineData("english-200k", 8, 16)]
    [InlineData("english-200k", 9, 10)]
    [InlineData("english-200k", 9, 16)]
    [InlineData("utf8-64k", 5, 16)]
    [InlineData("utf8-64k", 9, 16)]
    public void OneShot_SmallWindow_CurrentlyMatchesNativeBrotliBytes(
        string name, int quality, int window)
    {
        byte[] input = Corpus.Get(name);
        byte[] mine = EncoderDrivers.CompressOneShot(input, quality, window);
        byte[] native = SystemBrotli.Compress(input, quality, window);
        Assert.Equal(native, mine);
    }
}
