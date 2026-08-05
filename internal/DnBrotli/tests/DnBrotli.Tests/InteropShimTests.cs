using DnBrotli.Dn2CppInterop;
using DnBrotli.Tests.Support;

namespace DnBrotli.Tests;

/// <summary>
/// Drives the <see cref="BrotliInterop"/> entry points directly (the twelve methods the dn2cpp
/// transpiler substitutes for the BCL's brotli P/Invokes). The bodies now run the pure-C#
/// engine, so these tests exercise it through the exact entry-point spellings, signatures and
/// streaming call discipline that were originally proven against the genuine native ABI (the
/// stage-0 forwarding stubs) — the swap must satisfy that contract unchanged.
/// </summary>
public unsafe class InteropShimTests
{
    [Fact]
    public void MaxCompressedSizeIsMonotonicAndPositive()
    {
        nuint previous = BrotliInterop.BrotliEncoderMaxCompressedSize(0);
        Assert.True(previous > 0);
        foreach (int size in (int[])[1, 100, 65536, 1 << 20])
        {
            nuint bound = BrotliInterop.BrotliEncoderMaxCompressedSize((nuint)size);
            Assert.True(bound >= (nuint)size);
            Assert.True(bound >= previous);
            previous = bound;
        }
    }

    [Theory]
    [InlineData("hello", 1)]
    [InlineData("hello", 11)]
    [InlineData("english-200k", 4)]
    [InlineData("random-64k", 4)]
    [InlineData("zeros-70k", 9)]
    public void OneShotRoundTrip(string corpusName, int quality)
    {
        byte[] original = Corpus.Get(corpusName);
        var compressed = new byte[BrotliInterop.BrotliEncoderMaxCompressedSize((nuint)original.Length)];
        nuint compressedLength = (nuint)compressed.Length;

        fixed (byte* inPtr = original)
        fixed (byte* outPtr = compressed)
        {
            int ok = BrotliInterop.BrotliEncoderCompress(
                quality, window: 22, mode: (int)BrotliEncoderMode.Generic,
                (nuint)original.Length, inPtr, &compressedLength, outPtr);
            Assert.Equal(1, ok);
        }

        var decompressed = new byte[original.Length];
        nuint decompressedLength = (nuint)decompressed.Length;
        fixed (byte* inPtr = compressed)
        fixed (byte* outPtr = decompressed)
        {
            int ok = BrotliInterop.BrotliDecoderDecompress(
                compressedLength, inPtr, &decompressedLength, outPtr);
            Assert.Equal(1, ok);
        }

        Assert.Equal((nuint)original.Length, decompressedLength);
        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void StreamingRoundTripWithSmallBuffers()
    {
        byte[] original = Corpus.Get("english-200k");
        byte[] compressed = StreamingCompress(original, quality: 5, chunk: 4093);
        byte[] decompressed = StreamingDecompress(compressed, original.Length, chunk: 512);
        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void DecoderRejectsGarbage()
    {
        var garbage = new byte[64];
        garbage.AsSpan().Fill(0x91);
        nint state = BrotliInterop.BrotliDecoderCreateInstance(0, 0, 0);
        Assert.NotEqual(0, state);
        try
        {
            var output = new byte[256];
            fixed (byte* inPtr = garbage)
            fixed (byte* outPtr = output)
            {
                byte* nextIn = inPtr;
                byte* nextOut = outPtr;
                nuint availIn = (nuint)garbage.Length;
                nuint availOut = (nuint)output.Length;
                nuint totalOut = 0;
                int result = BrotliInterop.BrotliDecoderDecompressStream(
                    state, &availIn, &nextIn, &availOut, &nextOut, &totalOut);
                Assert.Equal((int)BrotliDecoderResult.Error, result);
                Assert.Equal(0, BrotliInterop.BrotliDecoderIsFinished(state));
            }
        }
        finally
        {
            BrotliInterop.BrotliDecoderDestroyInstance(state);
        }
    }

    private static byte[] StreamingCompress(byte[] original, int quality, int chunk)
    {
        nint state = BrotliInterop.BrotliEncoderCreateInstance(0, 0, 0);
        Assert.NotEqual(0, state);
        try
        {
            Assert.Equal(1, BrotliInterop.BrotliEncoderSetParameter(
                state, (int)BrotliEncoderParameter.Quality, (uint)quality));
            Assert.Equal(1, BrotliInterop.BrotliEncoderSetParameter(
                state, (int)BrotliEncoderParameter.LgWin, 22));

            using var output = new MemoryStream();
            var outputChunk = new byte[chunk];
            int position = 0;
            while (true)
            {
                bool finalBlock = position >= original.Length;
                int take = Math.Min(chunk, original.Length - position);
                fixed (byte* inPtr = original)
                fixed (byte* outPtr = outputChunk)
                {
                    byte* nextIn = inPtr + position;
                    nuint availIn = (nuint)take;
                    // Iterate the operation until the encoder reports no pending output.
                    while (true)
                    {
                        byte* nextOut = outPtr;
                        nuint availOut = (nuint)outputChunk.Length;
                        nuint totalOut = 0;
                        int ok = BrotliInterop.BrotliEncoderCompressStream(
                            state,
                            (int)(finalBlock ? BrotliEncoderOperation.Finish : BrotliEncoderOperation.Process),
                            &availIn, &nextIn, &availOut, &nextOut, &totalOut);
                        Assert.Equal(1, ok);
                        output.Write(outputChunk, 0, outputChunk.Length - (int)availOut);
                        if (availIn == 0 && BrotliInterop.BrotliEncoderHasMoreOutput(state) == 0)
                        {
                            break;
                        }
                    }
                    position += take - (int)availIn;
                }
                if (finalBlock)
                {
                    return output.ToArray();
                }
            }
        }
        finally
        {
            BrotliInterop.BrotliEncoderDestroyInstance(state);
        }
    }

    private static byte[] StreamingDecompress(byte[] compressed, int expectedLength, int chunk)
    {
        nint state = BrotliInterop.BrotliDecoderCreateInstance(0, 0, 0);
        Assert.NotEqual(0, state);
        try
        {
            using var output = new MemoryStream();
            var outputChunk = new byte[chunk];
            fixed (byte* inPtr = compressed)
            {
                byte* nextIn = inPtr;
                nuint availIn = (nuint)compressed.Length;
                while (true)
                {
                    int result;
                    fixed (byte* outPtr = outputChunk)
                    {
                        byte* nextOut = outPtr;
                        nuint availOut = (nuint)outputChunk.Length;
                        nuint totalOut = 0;
                        result = BrotliInterop.BrotliDecoderDecompressStream(
                            state, &availIn, &nextIn, &availOut, &nextOut, &totalOut);
                        output.Write(outputChunk, 0, outputChunk.Length - (int)availOut);
                    }
                    if (result == (int)BrotliDecoderResult.Success)
                    {
                        Assert.Equal(1, BrotliInterop.BrotliDecoderIsFinished(state));
                        Assert.Equal(expectedLength, output.Length);
                        return output.ToArray();
                    }
                    Assert.Equal((int)BrotliDecoderResult.NeedsMoreOutput, result);
                }
            }
        }
        finally
        {
            BrotliInterop.BrotliDecoderDestroyInstance(state);
        }
    }
}
