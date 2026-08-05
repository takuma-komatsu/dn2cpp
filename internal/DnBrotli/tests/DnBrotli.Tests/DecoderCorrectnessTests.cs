using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using DnBrotli.Dec;
using DnBrotli.Raw;
using DnBrotli.Tests.Oracles;
using DnBrotli.Tests.Support;

namespace DnBrotli.Tests;

/// <summary>
/// End-to-end correctness of the decode.c port, driven exclusively through the public
/// <see cref="RawBrotli"/> C-ABI surface: BCL-compressed data (real native brotli — the
/// oracle) must decode byte-identically via one-shot and streaming paths, resumability
/// must hold at 1-byte input/output granularity, and error paths must terminate with the
/// C error codes.
/// </summary>
public unsafe class DecoderCorrectnessTests
{
    // ==================== helpers ====================

    private static (BrotliDecoderResult Result, byte[] Output) OneShot(
        byte[] compressed, int outputCapacity)
    {
        byte[] output = new byte[outputCapacity + 16];
        nuint decodedSize = (nuint)output.Length;
        fixed (byte* inPtr = compressed)
        fixed (byte* outPtr = output)
        {
            BrotliDecoderResult result = RawBrotli.BrotliDecoderDecompress(
                (nuint)compressed.Length, inPtr, &decodedSize, outPtr);
            return (result, output.AsSpan(0, (int)decodedSize).ToArray());
        }
    }

    /// <summary>Streaming decode feeding input in <paramref name="inChunk"/>-byte pieces and
    /// draining output through an <paramref name="outChunk"/>-byte window, with strict result
    /// discipline (never feeds more input unless NeedsMoreInput was returned) and a hang
    /// guard.</summary>
    private static (BrotliDecoderResult Result, BrotliDecoderErrorCode ErrorCode, byte[] Output)
        Streaming(byte[] compressed, int inChunk, int outChunk, long maxOutput = 64L << 20)
    {
        BrotliDecoderState* s = RawBrotli.BrotliDecoderCreateInstance();
        Assert.True(s != null);
        byte* inBuf = (byte*)NativeMemory.Alloc((nuint)Math.Max(compressed.Length, 1));
        byte* outBuf = (byte*)NativeMemory.Alloc((nuint)outChunk);
        compressed.AsSpan().CopyTo(new Span<byte>(inBuf, Math.Max(compressed.Length, 1)));
        try
        {
            var output = new MemoryStream();
            int inOffset = 0;
            byte* next_in = inBuf;
            nuint avail_in = 0;
            long iterations = 0;
            long iterationGuard = 3L * compressed.Length + (maxOutput / outChunk) + 1024;
            BrotliDecoderResult result;
            for (;;)
            {
                byte* next_out = outBuf;
                nuint avail_out = (nuint)outChunk;
                result = RawBrotli.BrotliDecoderDecompressStream(
                    s, &avail_in, &next_in, &avail_out, &next_out, null);
                output.Write(new ReadOnlySpan<byte>(outBuf, outChunk - (int)avail_out));
                Assert.True(output.Length <= maxOutput, "decoder produced excessive output");
                Assert.True(++iterations <= iterationGuard, "decoder made no progress (hang)");
                if (result == BrotliDecoderResult.NeedsMoreOutput)
                {
                    continue;
                }
                if (result == BrotliDecoderResult.NeedsMoreInput)
                {
                    Assert.Equal((nuint)0, avail_in);  // invariant: input never left behind
                    if (inOffset >= compressed.Length)
                    {
                        break;  // truncated input
                    }
                    int chunk = Math.Min(inChunk, compressed.Length - inOffset);
                    next_in = inBuf + inOffset;
                    avail_in = (nuint)chunk;
                    inOffset += chunk;
                    continue;
                }
                break;  // Success or Error
            }
            return (result, RawBrotli.BrotliDecoderGetErrorCode(s), output.ToArray());
        }
        finally
        {
            RawBrotli.BrotliDecoderDestroyInstance(s);
            NativeMemory.Free(inBuf);
            NativeMemory.Free(outBuf);
        }
    }

    /// <summary>A deterministic &gt;1 MiB mixed-shape input (text + random + zeros +
    /// short-period repeats), local to this suite so the shared corpus stays small.</summary>
    private static byte[] BuildMixed1M()
    {
        var data = new MemoryStream();
        data.Write(Corpus.Get("english-200k"));
        data.Write(Corpus.Get("random-64k"));
        data.Write(new byte[256 * 1024]);  // zeros
        data.Write(Corpus.Get("utf8-64k"));
        data.Write(Corpus.Get("repeat-7-40k"));
        data.Write(Corpus.Get("binary-structured-128k"));
        while (data.Length <= 1 << 20)
        {
            data.Write(Corpus.Get("english-200k"));
        }
        return data.ToArray();
    }

    // ==================== one-shot ====================

    public static IEnumerable<object[]> CorpusNames() => Corpus.Names();

    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void OneShotDecodesBclOutputAcrossQualitiesAndWindows(string name)
    {
        byte[] original = Corpus.Get(name);
        foreach (int quality in (int[])[0, 1, 2, 4, 5, 6, 9, 11])
        {
            foreach (int window in (int[])[10, 18, 22, 24])
            {
                byte[] compressed = SystemBrotli.Compress(original, quality, window);
                (BrotliDecoderResult result, byte[] output) = OneShot(compressed, original.Length);
                Assert.Equal(BrotliDecoderResult.Success, result);
                Assert.Equal(original, output);
            }
        }
    }

    // ==================== streaming ====================

    [Theory]
    // The killer resumability case: 1 byte in x 1 byte out.
    [InlineData("hello", 1, 1)]
    [InlineData("english-200k", 1, 1)]
    [InlineData("hello", 7, 512)]
    [InlineData("english-200k", 7, 65536)]
    [InlineData("english-200k", 4093, 512)]
    [InlineData("zeros-70k", 65536, 512)]
    [InlineData("repeat-7-40k", 4093, 1)]
    [InlineData("utf8-64k", 65536, 65536)]
    [InlineData("random-64k", 7, 65536)]
    [InlineData("binary-structured-128k", 65536, 1)]
    [InlineData("zeros-1k", 1, 512)]
    [InlineData("empty", 1, 1)]
    public void StreamingDecodesChunkedInputIntoBoundedOutput(string name, int inChunk, int outChunk)
    {
        byte[] original = Corpus.Get(name);
        byte[] compressed = SystemBrotli.Compress(original, quality: 4, window: 22);
        (BrotliDecoderResult result, _, byte[] output) = Streaming(compressed, inChunk, outChunk);
        Assert.Equal(BrotliDecoderResult.Success, result);
        Assert.Equal(original, output);
    }

    [Fact]
    public void StreamingDecodesSmallWindowStreamByteByByte()
    {
        // Window 10 forces a tiny ring buffer -> exercises ring-buffer wraps under
        // 1-byte output pressure.
        byte[] original = Corpus.Get("repeat-7-40k");
        byte[] compressed = SystemBrotli.Compress(original, quality: 5, window: 10);
        (BrotliDecoderResult result, _, byte[] output) = Streaming(compressed, 1, 1);
        Assert.Equal(BrotliDecoderResult.Success, result);
        Assert.Equal(original, output);
    }

    // ==================== >1 MiB via BrotliStream ====================

    [Fact]
    public void BrotliStreamCompressedMegabyteInputRoundTrips()
    {
        byte[] original = BuildMixed1M();
        Assert.True(original.Length > 1 << 20);

        byte[] compressed;
        using (var buffer = new MemoryStream())
        {
            using (var brotli = new BrotliStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
            {
                brotli.Write(original);
            }
            compressed = buffer.ToArray();
        }

        (BrotliDecoderResult oneShotResult, byte[] oneShotOutput) = OneShot(compressed, original.Length);
        Assert.Equal(BrotliDecoderResult.Success, oneShotResult);
        Assert.Equal(original, oneShotOutput);

        (BrotliDecoderResult streamResult, _, byte[] streamOutput) =
            Streaming(compressed, 65536, 65536);
        Assert.Equal(BrotliDecoderResult.Success, streamResult);
        Assert.Equal(original, streamOutput);
    }

    // ==================== error paths ====================

    [Fact]
    public void EveryPrefixOfValidStreamNeedsMoreInput()
    {
        byte[] compressed = SystemBrotli.Compress(Corpus.Get("hello"), quality: 4, window: 22);
        for (int len = 0; len < compressed.Length; len++)
        {
            byte[] prefix = compressed.AsSpan(0, len).ToArray();
            (BrotliDecoderResult result, BrotliDecoderErrorCode code, _) =
                Streaming(prefix, 65536, 65536);
            Assert.Equal(BrotliDecoderResult.NeedsMoreInput, result);
            Assert.True((int)code >= 0, $"prefix {len}: unexpected error {code}");
        }
    }

    [Fact]
    public void FlippedBitsNeverYieldTheOriginalAndErrorsCarryNegativeCodes()
    {
        byte[] original = Corpus.Get("english-200k");
        byte[] compressed = SystemBrotli.Compress(original, quality: 4, window: 22);
        int errors = 0;
        uint state = 0x5EED5EEDu;
        for (int trial = 0; trial < 48; trial++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            // Bias half the flips into the first 64 bytes (headers/Huffman tables),
            // spread the rest across the stream.
            int byteIndex = (trial % 2 == 0)
                ? (int)(state % (uint)Math.Min(64, compressed.Length))
                : (int)(state % (uint)compressed.Length);
            int bit = trial % 8;

            byte[] corrupted = (byte[])compressed.Clone();
            corrupted[byteIndex] ^= (byte)(1 << bit);

            (BrotliDecoderResult result, BrotliDecoderErrorCode code, byte[] output) =
                Streaming(corrupted, 65536, 65536);
            if (result == BrotliDecoderResult.Error)
            {
                errors++;
                Assert.True((int)code < 0,
                    $"trial {trial}: Error result but non-negative code {code}");
            }
            else if (result == BrotliDecoderResult.Success)
            {
                // A flipped literal can still decode "successfully" — but never to the
                // original bytes.
                Assert.False(output.AsSpan().SequenceEqual(original),
                    $"trial {trial}: corrupted stream decoded to the original");
            }
            // NeedsMoreInput is acceptable: corrupted length fields may starve the decoder.
        }
        Assert.True(errors > 0, "no corruption trial produced a decoder error");
    }

    [Fact]
    public void GarbageFillErrorsWithNegativeCode()
    {
        byte[] garbage = new byte[64];
        garbage.AsSpan().Fill(0x91);
        (BrotliDecoderResult result, BrotliDecoderErrorCode code, _) =
            Streaming(garbage, 65536, 65536);
        Assert.Equal(BrotliDecoderResult.Error, result);
        Assert.True((int)code < 0);
        Assert.Equal(BrotliDecoderErrorCode.ErrorFormatWindowBits, code);
    }

    [Fact]
    public void OneShotWithZeroLengthInputIsError()
    {
        byte[] output = new byte[16];
        nuint decodedSize = (nuint)output.Length;
        fixed (byte* outPtr = output)
        {
            BrotliDecoderResult result = RawBrotli.BrotliDecoderDecompress(
                0, null, &decodedSize, outPtr);
            Assert.Equal(BrotliDecoderResult.Error, result);
        }
    }

    [Fact]
    public void OutputRequestedWithoutBufferIsInvalidArguments()
    {
        byte[] compressed = SystemBrotli.Compress(Corpus.Get("hello"));
        BrotliDecoderState* s = RawBrotli.BrotliDecoderCreateInstance();
        try
        {
            fixed (byte* inPtr = compressed)
            {
                byte* next_in = inPtr;
                nuint avail_in = (nuint)compressed.Length;
                nuint avail_out = 128;  // output requested ...
                BrotliDecoderResult result = RawBrotli.BrotliDecoderDecompressStream(
                    s, &avail_in, &next_in, &avail_out, null, null);  // ... but next_out == NULL
                Assert.Equal(BrotliDecoderResult.Error, result);
                Assert.Equal(BrotliDecoderErrorCode.ErrorInvalidArguments,
                    RawBrotli.BrotliDecoderGetErrorCode(s));
            }
        }
        finally
        {
            RawBrotli.BrotliDecoderDestroyInstance(s);
        }
    }

    // ==================== TakeOutput / state discipline ====================

    [Fact]
    public void TakeOutputDrainsEverythingWithoutUserBuffers()
    {
        byte[] original = Corpus.Get("english-200k");
        byte[] compressed = SystemBrotli.Compress(original, quality: 4, window: 22);
        BrotliDecoderState* s = RawBrotli.BrotliDecoderCreateInstance();
        byte* inBuf = (byte*)NativeMemory.Alloc((nuint)compressed.Length);
        compressed.AsSpan().CopyTo(new Span<byte>(inBuf, compressed.Length));
        try
        {
            var output = new MemoryStream();
            int inOffset = 0;
            byte* next_in = inBuf;
            nuint avail_in = 0;
            long guard = 0;
            for (;;)
            {
                nuint avail_out = 0;
                BrotliDecoderResult result = RawBrotli.BrotliDecoderDecompressStream(
                    s, &avail_in, &next_in, &avail_out, null, null);
                while (RawBrotli.BrotliDecoderHasMoreOutput(s) != 0)
                {
                    nuint size = 4096;
                    byte* chunk = RawBrotli.BrotliDecoderTakeOutput(s, &size);
                    Assert.True(chunk != null);
                    Assert.True(size > 0);
                    output.Write(new ReadOnlySpan<byte>(chunk, (int)size));
                }
                if (result == BrotliDecoderResult.Success)
                {
                    break;
                }
                Assert.NotEqual(BrotliDecoderResult.Error, result);
                if (result == BrotliDecoderResult.NeedsMoreInput)
                {
                    Assert.True(inOffset < compressed.Length, "decoder starved with input left");
                    int chunkLen = Math.Min(4093, compressed.Length - inOffset);
                    next_in = inBuf + inOffset;
                    avail_in = (nuint)chunkLen;
                    inOffset += chunkLen;
                }
                Assert.True(++guard < 1_000_000, "hang in TakeOutput loop");
            }
            Assert.Equal(0, RawBrotli.BrotliDecoderHasMoreOutput(s));
            Assert.Equal(1, RawBrotli.BrotliDecoderIsFinished(s));
            Assert.Equal(original, output.ToArray());

            // TakeOutput on a finished, fully drained decoder returns nothing.
            nuint zero = 0;
            byte* tail = RawBrotli.BrotliDecoderTakeOutput(s, &zero);
            Assert.True(tail == null || zero == 0);
        }
        finally
        {
            RawBrotli.BrotliDecoderDestroyInstance(s);
            NativeMemory.Free(inBuf);
        }
    }

    [Fact]
    public void IsUsedIsFinishedDiscipline()
    {
        byte[] compressed = SystemBrotli.Compress(Corpus.Get("hello"));
        BrotliDecoderState* s = RawBrotli.BrotliDecoderCreateInstance();
        try
        {
            // Fresh instance: unused, unfinished; parameters still settable.
            Assert.Equal(0, RawBrotli.BrotliDecoderIsUsed(s));
            Assert.Equal(0, RawBrotli.BrotliDecoderIsFinished(s));
            Assert.Equal(0, RawBrotli.BrotliDecoderHasMoreOutput(s));
            Assert.Equal(1, RawBrotli.BrotliDecoderSetParameter(
                s, BrotliDecoderParameter.LargeWindow, 0));
            Assert.Equal(0, RawBrotli.BrotliDecoderIsUsed(s));

            fixed (byte* inPtr = compressed)
            {
                // Feed a single byte: instance becomes "used" but not finished.
                byte* next_in = inPtr;
                nuint avail_in = 1;
                byte* outBuf = stackalloc byte[64];
                byte* next_out = outBuf;
                nuint avail_out = 64;
                BrotliDecoderResult result = RawBrotli.BrotliDecoderDecompressStream(
                    s, &avail_in, &next_in, &avail_out, &next_out, null);
                Assert.Equal(BrotliDecoderResult.NeedsMoreInput, result);
                Assert.Equal(1, RawBrotli.BrotliDecoderIsUsed(s));
                Assert.Equal(0, RawBrotli.BrotliDecoderIsFinished(s));
                // Used decoder rejects parameter changes.
                Assert.Equal(0, RawBrotli.BrotliDecoderSetParameter(
                    s, BrotliDecoderParameter.LargeWindow, 1));

                // Feed the rest: finished.
                next_in = inPtr + 1;
                avail_in = (nuint)(compressed.Length - 1);
                result = RawBrotli.BrotliDecoderDecompressStream(
                    s, &avail_in, &next_in, &avail_out, &next_out, null);
                Assert.Equal(BrotliDecoderResult.Success, result);
                Assert.Equal(1, RawBrotli.BrotliDecoderIsFinished(s));
                Assert.Equal(0, RawBrotli.BrotliDecoderHasMoreOutput(s));
            }
        }
        finally
        {
            RawBrotli.BrotliDecoderDestroyInstance(s);
        }
    }

    [Fact]
    public void FinishedInstanceRefusesReuse()
    {
        byte[] original = Corpus.Get("hello");
        byte[] compressed = SystemBrotli.Compress(original);
        BrotliDecoderState* s = RawBrotli.BrotliDecoderCreateInstance();
        try
        {
            fixed (byte* inPtr = compressed)
            {
                byte* outBuf = stackalloc byte[256];

                byte* next_in = inPtr;
                nuint avail_in = (nuint)compressed.Length;
                byte* next_out = outBuf;
                nuint avail_out = 256;
                Assert.Equal(BrotliDecoderResult.Success, RawBrotli.BrotliDecoderDecompressStream(
                    s, &avail_in, &next_in, &avail_out, &next_out, null));
                Assert.Equal(1, RawBrotli.BrotliDecoderIsFinished(s));
                Assert.Equal(original,
                    new ReadOnlySpan<byte>(outBuf, 256 - (int)avail_out).ToArray());

                // A second stream fed to the same (finished) instance is not decoded:
                // no input is consumed and no output is produced.
                next_in = inPtr;
                avail_in = (nuint)compressed.Length;
                next_out = outBuf;
                avail_out = 256;
                BrotliDecoderResult again = RawBrotli.BrotliDecoderDecompressStream(
                    s, &avail_in, &next_in, &avail_out, &next_out, null);
                Assert.Equal(BrotliDecoderResult.Success, again);
                Assert.Equal((nuint)compressed.Length, avail_in);
                Assert.Equal((nuint)256, avail_out);
                Assert.Equal(1, RawBrotli.BrotliDecoderIsFinished(s));

                // ... and parameters are rejected too; a fresh instance is required.
                Assert.Equal(0, RawBrotli.BrotliDecoderSetParameter(
                    s, BrotliDecoderParameter.LargeWindow, 1));
            }
        }
        finally
        {
            RawBrotli.BrotliDecoderDestroyInstance(s);
        }
    }

    [Fact]
    public void LargeWindowParameterStillDecodesRegularStreams()
    {
        byte[] original = Corpus.Get("english-200k");
        byte[] compressed = SystemBrotli.Compress(original, quality: 4, window: 24);
        BrotliDecoderState* s = RawBrotli.BrotliDecoderCreateInstance();
        byte* outBuf = (byte*)NativeMemory.Alloc((nuint)(original.Length + 16));
        try
        {
            Assert.Equal(1, RawBrotli.BrotliDecoderSetParameter(
                s, BrotliDecoderParameter.LargeWindow, 1));
            fixed (byte* inPtr = compressed)
            {
                byte* next_in = inPtr;
                nuint avail_in = (nuint)compressed.Length;
                byte* next_out = outBuf;
                nuint avail_out = (nuint)(original.Length + 16);
                Assert.Equal(BrotliDecoderResult.Success, RawBrotli.BrotliDecoderDecompressStream(
                    s, &avail_in, &next_in, &avail_out, &next_out, null));
                Assert.Equal(original,
                    new ReadOnlySpan<byte>(outBuf, original.Length + 16 - (int)avail_out).ToArray());
            }
        }
        finally
        {
            RawBrotli.BrotliDecoderDestroyInstance(s);
            NativeMemory.Free(outBuf);
        }
    }

    // ==================== misc ABI surface ====================

    [Fact]
    public void VersionAndErrorStringsMatchC()
    {
        Assert.Equal((1u << 24) | (1u << 12) | 0u, RawBrotli.BrotliDecoderVersion());

        static string ErrorString(BrotliDecoderErrorCode c)
        {
            byte* p = RawBrotli.BrotliDecoderErrorString(c);
            int len = 0;
            while (p[len] != 0) len++;
            return Encoding.ASCII.GetString(p, len);
        }

        Assert.Equal("_NO_ERROR", ErrorString(BrotliDecoderErrorCode.NoError));
        Assert.Equal("_SUCCESS", ErrorString(BrotliDecoderErrorCode.Success));
        Assert.Equal("_ERROR_FORMAT_WINDOW_BITS",
            ErrorString(BrotliDecoderErrorCode.ErrorFormatWindowBits));
        Assert.Equal("_ERROR_UNREACHABLE",
            ErrorString(BrotliDecoderErrorCode.ErrorUnreachable));
        Assert.Equal("INVALID", ErrorString((BrotliDecoderErrorCode)(-17)));
        Assert.Equal("INVALID", ErrorString((BrotliDecoderErrorCode)(-99)));
    }

    [Fact]
    public void AttachDictionaryAcceptsRawRejectsSerializedAndPostUse()
    {
        byte[] original = Corpus.Get("hello");
        byte[] compressed = SystemBrotli.Compress(original);
        byte[] dictBytes = Encoding.ASCII.GetBytes("compound dictionary prefix data");
        byte* dict = (byte*)NativeMemory.Alloc((nuint)dictBytes.Length);
        dictBytes.AsSpan().CopyTo(new Span<byte>(dict, dictBytes.Length));
        BrotliDecoderState* s = RawBrotli.BrotliDecoderCreateInstance();
        try
        {
            // Serialized shared dictionaries are rejected (no BROTLI_EXPERIMENTAL).
            Assert.Equal(0, RawBrotli.BrotliDecoderAttachDictionary(
                s, Common.BrotliSharedDictionaryType.BROTLI_SHARED_DICTIONARY_SERIALIZED,
                (nuint)dictBytes.Length, dict));
            // Raw LZ77 prefix dictionaries attach to a fresh instance.
            Assert.Equal(1, RawBrotli.BrotliDecoderAttachDictionary(
                s, Common.BrotliSharedDictionaryType.BROTLI_SHARED_DICTIONARY_RAW,
                (nuint)dictBytes.Length, dict));

            // A normal (non-dictionary-referencing) stream still decodes correctly.
            fixed (byte* inPtr = compressed)
            {
                byte* outBuf = stackalloc byte[256];
                byte* next_in = inPtr;
                nuint avail_in = (nuint)compressed.Length;
                byte* next_out = outBuf;
                nuint avail_out = 256;
                Assert.Equal(BrotliDecoderResult.Success, RawBrotli.BrotliDecoderDecompressStream(
                    s, &avail_in, &next_in, &avail_out, &next_out, null));
                Assert.Equal(original,
                    new ReadOnlySpan<byte>(outBuf, 256 - (int)avail_out).ToArray());
            }

            // A used decoder rejects further attachments.
            Assert.Equal(0, RawBrotli.BrotliDecoderAttachDictionary(
                s, Common.BrotliSharedDictionaryType.BROTLI_SHARED_DICTIONARY_RAW,
                (nuint)dictBytes.Length, dict));
        }
        finally
        {
            RawBrotli.BrotliDecoderDestroyInstance(s);
            NativeMemory.Free(dict);
        }
    }

    [Fact]
    public void CreateInstanceRejectsHalfSpecifiedAllocator()
    {
        Assert.True(RawBrotli.BrotliDecoderCreateInstance(1, 0, null) == null);
        Assert.True(RawBrotli.BrotliDecoderCreateInstance(0, 1, null) == null);
        BrotliDecoderState* s = RawBrotli.BrotliDecoderCreateInstance(0, 0, null);
        Assert.True(s != null);
        RawBrotli.BrotliDecoderDestroyInstance(s);
        RawBrotli.BrotliDecoderDestroyInstance(null);  // null is ignored, like free(NULL)
    }
}
