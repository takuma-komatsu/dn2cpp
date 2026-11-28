using System;
using System.IO;
using System.IO.Compression;

namespace CompressionCore;

internal static class ZstandardRoundTrip
{
    internal static void Run()
    {
        int[] sampleLengths = new int[128];
        byte[] samples = new byte[sampleLengths.Length * 128];
        for (int i = 0; i < sampleLengths.Length; i++)
        {
            sampleLengths[i] = 128;
            for (int j = 0; j < 128; j++)
                samples[i * 128 + j] = (byte)(i + (j % 16));
        }

        using MemoryStream compressed = new MemoryStream();
        using (ZstandardStream writer = new ZstandardStream(compressed, CompressionMode.Compress, leaveOpen: true))
            writer.Write(samples);
        compressed.Position = 0;
        using ZstandardStream reader = new ZstandardStream(compressed, CompressionMode.Decompress);
        using MemoryStream restored = new MemoryStream();
        reader.CopyTo(restored);
        Console.WriteLine("Zstandard stream roundtrip: " + samples.AsSpan().SequenceEqual(restored.ToArray()));

        byte[] trained = ZstandardDictionary.Train(samples, sampleLengths, maxDictionarySize: 1024);
        using ZstandardDictionary dictionary = ZstandardDictionary.Create(trained);
        ReadOnlySpan<byte> payload = samples.AsSpan(0, 128);
        byte[] frame = new byte[ZstandardEncoder.GetMaxCompressedLength(payload.Length)];
        bool encoded = ZstandardEncoder.TryCompress(payload, frame, out int size, dictionary, windowLog2: 0);
        byte[] output = new byte[payload.Length];
        bool requiresDictionary = !ZstandardDecoder.TryDecompress(frame.AsSpan(0, size), output, out _);
        bool decoded = ZstandardDecoder.TryDecompress(frame.AsSpan(0, size), output, out int written, dictionary);
        Console.WriteLine("Zstandard dictionary roundtrip: " + (encoded && decoded && requiresDictionary
            && payload.SequenceEqual(output.AsSpan(0, written))));
    }
}
