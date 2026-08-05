using System.Buffers;
using DnBrotli.Tests.Oracles;
using DnBrotli.Tests.Support;

namespace DnBrotli.Tests;

/// <summary>
/// Decoder hardening beyond <see cref="DecoderCorrectnessTests"/>: seeded (fully deterministic)
/// mutation fuzzing over the idiomatic layer. Every mutated blob is driven through BOTH the
/// chunked <see cref="BrotliDecoder"/> and the one-shot <see cref="Brotli.Decompress"/>, with the
/// invariants: never hangs (forward-progress guard in the driver), never writes outside the
/// destination span (canary bytes in the driver), <see cref="Brotli.Decompress"/> throws nothing
/// but <see cref="BrotliException"/>, and the outcome always matches the BCL oracle call-for-call
/// on the identical chunk schedule — so a mutated stream can never produce a false Done whose
/// content silently diverges from what real brotli accepts.
/// </summary>
public class RobustnessFuzzTests
{
    private const int InChunk = 4093;
    private const int OutChunk = 16384;

    public static IEnumerable<object[]> CorpusNames() => Corpus.Names();

    // ==================== helpers ====================

    private static uint XorShift(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    private static uint SeedFor(string name, uint salt)
    {
        uint seed = salt;
        foreach (char c in name)
        {
            seed = seed * 31 + c;
        }
        return seed == 0 ? 0xDEADBEEFu : seed;
    }

    /// <summary>The shared invariant bundle for one (possibly mutated) blob: chunked DnBrotli
    /// matches chunked BCL call-for-call (statuses, consumed/written pairs, and output bytes),
    /// the final status is a terminal one, and the one-shot API agrees with the chunked outcome
    /// while throwing nothing but <see cref="BrotliException"/>.</summary>
    private static (OperationStatus Final, byte[] Output) AssertBlobConsistency(byte[] blob)
    {
        (List<DecodeStep> dnSteps, byte[] dnOutput) = DecoderDrivers.DriveDn(blob, InChunk, OutChunk);
        (List<DecodeStep> bclSteps, byte[] bclOutput) = DecoderDrivers.DriveBcl(blob, InChunk, OutChunk);
        Assert.Equal(bclSteps, dnSteps);
        Assert.Equal(bclOutput, dnOutput);

        OperationStatus final = dnSteps[^1].Status;
        Assert.True(final is OperationStatus.Done or OperationStatus.NeedMoreData or OperationStatus.InvalidData,
            $"non-terminal final status {final}");

        try
        {
            byte[] oneShot = Brotli.Decompress(blob);
            Assert.Equal(OperationStatus.Done, final);
            Assert.Equal(dnOutput, oneShot);
        }
        catch (BrotliException)
        {
            // The only exception the one-shot API may surface — and only when the chunked
            // path also refused the stream.
            Assert.NotEqual(OperationStatus.Done, final);
        }
        return (final, dnOutput);
    }

    // ==================== (a) truncation ====================

    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void TruncatedPrefixesNeverHangCorruptMemoryOrClaimDone(string name)
    {
        byte[] blob = SystemBrotli.Compress(Corpus.Get(name), quality: 4, window: 22);
        uint seed = SeedFor(name, 0x7C0FFEE1u);
        for (int i = 0; i < 32; i++)
        {
            // Always include the two boundary prefixes; sample the rest.
            int length = i switch
            {
                0 => 0,
                1 => blob.Length - 1,
                _ => (int)(XorShift(ref seed) % (uint)blob.Length),
            };
            (OperationStatus final, _) = AssertBlobConsistency(blob.AsSpan(0, length).ToArray());
            // A strict prefix can never claim a completed stream.
            Assert.NotEqual(OperationStatus.Done, final);
        }
    }

    // ==================== (b) bit flips ====================

    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void BitFlippedStreamsNeverHangCorruptMemoryOrFakeTheOriginal(string name)
    {
        byte[] original = Corpus.Get(name);
        byte[] blob = SystemBrotli.Compress(original, quality: 4, window: 22);
        uint seed = SeedFor(name, 0xB17F11B5u);
        for (int trial = 0; trial < 64; trial++)
        {
            int flipBits = 1 << (trial % 3);  // 1-, 2-, and 4-bit flips
            int byteIndex = (int)(XorShift(ref seed) % (uint)blob.Length);
            int shift = (int)(XorShift(ref seed) % (uint)(9 - flipBits));
            byte[] mutated = (byte[])blob.Clone();
            mutated[byteIndex] ^= (byte)(((1 << flipBits) - 1) << shift);

            (OperationStatus final, byte[] dnOutput) = AssertBlobConsistency(mutated);

            // The critical anti-silent-corruption clause: a Done may only report content the
            // oracle reproduces byte-for-byte on the same mutated stream (AssertBlobConsistency
            // compared against the BCL's output). So a mutated stream reporting Done with the
            // ORIGINAL bytes is legitimate only when real brotli also accepts it as such
            // (e.g. a flip confined to padding bits); re-affirm that pairing explicitly.
            if (final == OperationStatus.Done && dnOutput.AsSpan().SequenceEqual(original))
            {
                Assert.Equal(SystemBrotli.Decompress(mutated), dnOutput);
            }
        }
    }

    // ==================== (c) random garbage ====================

    [Fact]
    public void RandomGarbageBuffersNeverHangOrCorruptMemory()
    {
        uint seed = 0xF00DFACEu;
        for (int i = 0; i < 256; i++)
        {
            int length = 1 + (int)(XorShift(ref seed) % 4096u);
            byte[] garbage = new byte[length];
            for (int j = 0; j < length; j++)
            {
                garbage[j] = (byte)XorShift(ref seed);
            }
            AssertBlobConsistency(garbage);
        }
    }

    // ==================== (d) valid stream + trailing garbage ====================

    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void TrailingGarbageAfterValidStreamStillDecodesExactly(string name)
    {
        byte[] original = Corpus.Get(name);
        byte[] blob = SystemBrotli.Compress(original, quality: 4, window: 22);
        uint seed = SeedFor(name, 0x7A11FADEu);
        byte[] extended = new byte[blob.Length + 64];
        blob.CopyTo(extended, 0);
        for (int i = blob.Length; i < extended.Length; i++)
        {
            extended[i] = (byte)XorShift(ref seed);
        }

        (List<DecodeStep> dnSteps, byte[] dnOutput) = DecoderDrivers.DriveDn(extended, InChunk, OutChunk);
        (List<DecodeStep> bclSteps, byte[] bclOutput) = DecoderDrivers.DriveBcl(extended, InChunk, OutChunk);
        Assert.Equal(bclSteps, dnSteps);
        Assert.Equal(OperationStatus.Done, dnSteps[^1].Status);
        Assert.Equal(bclOutput, dnOutput);
        Assert.Equal(original, dnOutput);

        // One-shot: unused input past the complete stream is ignored, like the C one-shot.
        Assert.Equal(original, Brotli.Decompress(extended));
    }
}
