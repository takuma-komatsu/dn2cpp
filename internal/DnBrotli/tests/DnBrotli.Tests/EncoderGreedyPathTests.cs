using DnBrotli.Enc;
using DnBrotli.Raw;
using DnBrotli.Tests.Oracles;
using DnBrotli.Tests.Support;

namespace DnBrotli.Tests;

/// <summary>
/// q2/q3 gate: the greedy match-finding path — quickly hashers H2/H3
/// (hash_longest_match_quickly_inc.h), BrotliCreateBackwardReferences, the static
/// dictionary probe, and BrotliStoreMetaBlockFast/Trivial. Everything DnBrotli emits
/// must decode identically through (a) DnBrotli's own decoder and (b) the BCL decoder
/// backed by real native brotli. Compressed bytes are never asserted against native
/// output (implementation-defined); only round-trip facts and DnBrotli's own
/// determinism are pinned.
/// </summary>
public sealed class EncoderGreedyPathTests
{
    public static IEnumerable<object[]> CorpusQualityWindow()
    {
        foreach ((string name, _) in Corpus.Entries)
        {
            foreach (int quality in new[] { 2, 3 })
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

    /// <summary>Flush after every chunk (1-byte feeds on the small entry) — exercises
    /// StitchToPreviousBlock and the flush framing on the greedy path.</summary>
    [Theory]
    [InlineData("hello", 2, 1)]
    [InlineData("hello", 3, 1)]
    [InlineData("english-200k", 2, 4099)]
    [InlineData("english-200k", 3, 4099)]
    [InlineData("zeros-70k", 2, 4099)]
    [InlineData("random-64k", 3, 4099)]
    public void Streaming_WithFlushes_RoundTrips(string name, int quality, int chunkSize)
    {
        byte[] input = Corpus.Get(name);
        byte[] compressed = EncoderDrivers.CompressStreaming(
            input, quality, window: 22, chunkSize, flushBetweenChunks: true);

        Assert.Equal(input, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(input, SystemBrotli.Decompress(compressed));
    }

    /// <summary>Dictionary coverage: english-200k is rich in RFC 7932 words, and H2
    /// (quality 2) has USE_DICTIONARY == 1 — the static-dictionary probe must both
    /// round-trip and actually help: q2 (LZ77 + dictionary + static entropy codes)
    /// must beat q0's size on this entry. No brittle byte counts vs native.</summary>
    [Fact]
    public void Quality2_OnDictionaryHeavyText_BeatsQuality0Size()
    {
        byte[] input = Corpus.Get("english-200k");
        byte[] q0 = EncoderDrivers.CompressOneShot(input, 0, 22);
        byte[] q2 = EncoderDrivers.CompressOneShot(input, 2, 22);

        Assert.Equal(input, EncoderDrivers.DecompressDn(q2));
        Assert.Equal(input, SystemBrotli.Decompress(q2));
        Assert.True(q2.Length < q0.Length,
            $"expected q2 ({q2.Length} bytes) to beat q0 ({q0.Length} bytes) on english-200k");
    }

    /// <summary>The static-dictionary probe must fire on short dictionary-word inputs
    /// that H2 cannot cover with backward references (nothing earlier in the stream).</summary>
    [Theory]
    [InlineData("information development")]
    [InlineData(" the difference between")]
    public void Quality2_ShortDictionaryWords_RoundTrip(string text)
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes(text);
        byte[] compressed = EncoderDrivers.CompressOneShot(input, 2, 22);
        Assert.Equal(input, EncoderDrivers.DecompressDn(compressed));
        Assert.Equal(input, SystemBrotli.Decompress(compressed));
    }

    /// <summary>size_hint routing: ChooseHasher must not select an unimplemented
    /// hasher arm for q2/q3 whatever the size hint (q2/q3 map to H2/H3 regardless;
    /// this pins that fact). q4 remains fenced (see Quality4_CurrentlyHitsDb3bStub).</summary>
    [Theory]
    [InlineData(2, 0u)]
    [InlineData(2, 1u << 20)]
    [InlineData(3, 0u)]
    [InlineData(3, 1u << 20)]
    public unsafe void SizeHint_Routing_DoesNotHitUnimplementedHasherArms(int quality, uint sizeHint)
    {
        byte[] input = Corpus.Get("english-200k");
        var output = new MemoryStream();
        BrotliEncoderState* s = RawBrotli.BrotliEncoderCreateInstance();
        Assert.True(s != null);
        try
        {
            Assert.Equal(1, RawBrotli.BrotliEncoderSetParameter(
                s, BrotliEncoderParameter.Quality, (uint)quality));
            Assert.Equal(1, RawBrotli.BrotliEncoderSetParameter(
                s, BrotliEncoderParameter.LgWin, 22));
            Assert.Equal(1, RawBrotli.BrotliEncoderSetParameter(
                s, BrotliEncoderParameter.SizeHint, sizeHint));
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

    /// <summary>Estimate now covers the general (q&gt;=2) branch via the real
    /// HasherSize for the quickly hashers.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void EstimatePeakMemoryUsage_GreedyPath_IsPositive(int quality)
    {
        Assert.True(RawBrotli.BrotliEncoderEstimatePeakMemoryUsage(quality, 22, 1 << 20) > 0);
    }

    /// <summary>DnBrotli's q2/q3 one-shot output is currently byte-identical to native
    /// brotli v1.1.0 (the BCL encoder) for these entries — a strong fidelity signal for
    /// the whole greedy path (hasher, scoring, dictionary probe, entropy stores).
    /// NOTE: byte-identity with native output is NOT part of DnBrotli's contract
    /// (PORTING.md pins round-trip facts and DnBrotli's own determinism only); this
    /// assertion MAY be relaxed to a round-trip check when a future BCL/brotli bump
    /// legitimately changes native output.</summary>
    [Theory]
    [InlineData("hello", 2)]
    [InlineData("hello", 3)]
    [InlineData("english-200k", 2)]
    [InlineData("english-200k", 3)]
    [InlineData("repeat-7-40k", 2)]
    [InlineData("repeat-7-40k", 3)]
    [InlineData("utf8-64k", 2)]
    [InlineData("utf8-64k", 3)]
    [InlineData("binary-structured-128k", 2)]
    [InlineData("binary-structured-128k", 3)]
    [InlineData("random-64k", 2)]
    [InlineData("random-64k", 3)]
    public void OneShot_CurrentlyMatchesNativeBrotliBytes(string name, int quality)
    {
        byte[] input = Corpus.Get(name);
        byte[] mine = EncoderDrivers.CompressOneShot(input, quality, 22);
        byte[] native = SystemBrotli.Compress(input, quality, 22);
        Assert.Equal(native, mine);
    }

    /* ==================== FindMatchLength SIMD-vs-scalar agreement ==================== */

    public static IEnumerable<object[]> MatchLengthCases()
    {
        /* (bufferLength, mismatchIndex or -1 for full match, limit) — boundary cases
           around the 16-byte vector block, the 8-byte scalar block, and the byte tail. */
        int[] limits = { 0, 1, 2, 7, 8, 9, 15, 16, 17, 23, 24, 31, 32, 33, 40, 63, 64, 65, 100 };
        foreach (int limit in limits)
        {
            yield return new object[] { limit, -1 };
            foreach (int mismatch in new[] { 0, 1, 7, 8, 15, 16, 17, 31, 32, 63 })
            {
                if (mismatch < limit + 8)
                {
                    yield return new object[] { limit, mismatch };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(MatchLengthCases))]
    public unsafe void FindMatchLength_VectorAndScalar_Agree(int limit, int mismatchIndex)
    {
        const int size = 128;
        byte[] a = new byte[size];
        byte[] b = new byte[size];
        var rng = new Random(1234 + limit * 131 + mismatchIndex);
        rng.NextBytes(a);
        a.CopyTo(b, 0);
        if (mismatchIndex >= 0 && mismatchIndex < size)
        {
            b[mismatchIndex] ^= 0x5A;
        }

        fixed (byte* pa = a)
        fixed (byte* pb = b)
        {
            nuint scalar = FindMatchLength.FindMatchLengthWithLimitScalar(pa, pb, (nuint)limit);
            nuint vector = FindMatchLength.FindMatchLengthWithLimitVector128(pa, pb, (nuint)limit);
            nuint dispatched = FindMatchLength.FindMatchLengthWithLimit(pa, pb, (nuint)limit);
            Assert.Equal(scalar, vector);
            Assert.Equal(scalar, dispatched);

            nuint expected = (nuint)Math.Min(
                limit, mismatchIndex >= 0 ? mismatchIndex : int.MaxValue);
            Assert.Equal(expected, scalar);
        }
    }

    [Fact]
    public unsafe void FindMatchLength_VectorAndScalar_Agree_Randomized()
    {
        var rng = new Random(20260709);
        byte[] a = new byte[512];
        byte[] b = new byte[512];
        for (int iter = 0; iter < 2000; iter++)
        {
            rng.NextBytes(a);
            a.CopyTo(b, 0);
            int limit = rng.Next(0, 400);
            /* Randomly corrupt a few positions (possibly beyond the limit). */
            int corruptions = rng.Next(0, 4);
            for (int c = 0; c < corruptions; c++)
            {
                b[rng.Next(0, 450)] ^= (byte)(1 + rng.Next(255));
            }
            fixed (byte* pa = a)
            fixed (byte* pb = b)
            {
                Assert.Equal(
                    FindMatchLength.FindMatchLengthWithLimitScalar(pa, pb, (nuint)limit),
                    FindMatchLength.FindMatchLengthWithLimitVector128(pa, pb, (nuint)limit));
            }
        }
    }
}
