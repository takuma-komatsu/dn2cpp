using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

// Discovery-spike throwaway probe (real System.IO.Compression, no shim),
// expanded for the integration checkpoint. Two sections:
//
//   1. The original small-buffer round trip (kept byte-for-byte as the
//      spike measured it) -- DeflateStream/GZipStream via the CompressionMode
//      constructor overload.
//   2. A larger, low-compressibility buffer (deterministic PRNG fill, not
//      real randomness, so the run is reproducible) round-tripped through
//      both stream types across every CompressionLevel variant, via the
//      CompressionLevel constructor overload. 65536 bytes of near-incompressible
//      data forces the compressed output past DeflateStream's internal 8192-byte
//      _buffer (confirmed empirically via reflection: a fresh compress-mode
//      DeflateStream allocates an 8192-byte _buffer), so both the compress and
//      decompress paths loop through several internal buffer refills instead of
//      completing in one Deflate()/Inflate() call -- exercising the
//      ZLibStreamHandle/native-shim call sites more thoroughly than a single
//      small buffer would.
//   3. (async surface measurement) The async Stream face of both stream
//      types over the same MemoryStream backing: WriteAsync(byte[],int,int) /
//      WriteAsync(ReadOnlyMemory<byte>) on the compress side,
//      ReadAsync(byte[],int,int) / ReadAsync(Memory<byte>) loop-reads and
//      CopyToAsync on the decompress side, FlushAsync, and `await using`
//      (the DisposeAsync path). Driven synchronously from Main via
//      GetAwaiter().GetResult(); round-trip equality printed as bools. Added
//      purely so --measure enumerates the async surface's transpile gaps --
//      native execution is not expected yet.
//   4. (ZLibStream measurement) The zlib-format sibling (zlib header +
//      Adler-32 trailer; .NET 6+ public API) of sections 1-2: sync
//      round-trips through both constructor overloads (CompressionMode and
//      every CompressionLevel), small buffer + the same large
//      low-compressibility buffer, decompress via CopyTo. Same ZLibNative
//      path as Deflate/GZip, just a different windowBits.
//
// Still a throwaway sample -- NOT the eventual E2E gate (a polished, multi-
// section samples/dotnet/CompressionCore/ + gates/build-and-run-compression-core.sh
// is a later, separate ticket; this file stays disposable).

namespace CompressionProbe;

public static class Program
{
    public static int Main()
    {
        byte[] original = Encoding.UTF8.GetBytes(
            "The quick brown fox jumps over the lazy dog. " +
            "The quick brown fox jumps over the lazy dog.");

        byte[] deflated = CompressDeflate(original);
        byte[] inflated = DecompressDeflate(deflated);
        Console.WriteLine("Deflate roundtrip ok: " + BytesEqual(original, inflated));

        byte[] gzipped = CompressGZip(original);
        byte[] gunzipped = DecompressGZip(gzipped);
        Console.WriteLine("GZip roundtrip ok: " + BytesEqual(original, gunzipped));

        byte[] large = BuildLargeBuffer(65536);
        CompressionLevel[] levels =
        {
            CompressionLevel.Optimal,
            CompressionLevel.Fastest,
            CompressionLevel.NoCompression,
            CompressionLevel.SmallestSize,
        };
        foreach (CompressionLevel level in levels)
        {
            byte[] largeDeflated = CompressDeflateLevel(large, level);
            byte[] largeInflated = DecompressDeflate(largeDeflated);
            Console.WriteLine("Deflate large/" + level + " roundtrip ok: " + BytesEqual(large, largeInflated));

            byte[] largeGzipped = CompressGZipLevel(large, level);
            byte[] largeGunzipped = DecompressGZip(largeGzipped);
            Console.WriteLine("GZip large/" + level + " roundtrip ok: " + BytesEqual(large, largeGunzipped));
        }

        RunAsyncSection().GetAwaiter().GetResult();

        // Section 4: ZLibStream (zlib format) sync round-trips.
        byte[] zlibbed = CompressZLib(original);
        byte[] unzlibbed = DecompressZLib(zlibbed);
        Console.WriteLine("ZLib roundtrip ok: " + BytesEqual(original, unzlibbed));

        foreach (CompressionLevel level in levels)
        {
            byte[] largeZlibbed = CompressZLibLevel(large, level);
            byte[] largeUnzlibbed = DecompressZLib(largeZlibbed);
            Console.WriteLine("ZLib large/" + level + " roundtrip ok: " + BytesEqual(large, largeUnzlibbed));
        }

        return 0;
    }

    // Section 3: async Stream surface. Each helper mirrors a sync sibling
    // above but goes through the async entry points; the decompress helpers
    // loop-read so the full stream (not just the first chunk) is consumed.
    private static async Task RunAsyncSection()
    {
        byte[] original = Encoding.UTF8.GetBytes(
            "The quick brown fox jumps over the lazy dog. " +
            "The quick brown fox jumps over the lazy dog.");

        byte[] deflated = await CompressDeflateAsyncArray(original);
        byte[] inflated = await DecompressDeflateAsyncArray(deflated);
        Console.WriteLine("Async Deflate array roundtrip ok: " + BytesEqual(original, inflated));

        byte[] deflatedMem = await CompressDeflateAsyncMemory(original);
        byte[] inflatedMem = await DecompressDeflateAsyncMemory(deflatedMem);
        Console.WriteLine("Async Deflate memory roundtrip ok: " + BytesEqual(original, inflatedMem));

        byte[] copied = await DecompressDeflateCopyToAsync(deflated);
        Console.WriteLine("Async Deflate CopyToAsync roundtrip ok: " + BytesEqual(original, copied));

        byte[] gzipped = await CompressGZipAsyncArray(original);
        byte[] gunzipped = await DecompressGZipAsyncArray(gzipped);
        Console.WriteLine("Async GZip array roundtrip ok: " + BytesEqual(original, gunzipped));

        byte[] gzippedMem = await CompressGZipAsyncMemory(original);
        byte[] gunzippedMem = await DecompressGZipAsyncMemory(gzippedMem);
        Console.WriteLine("Async GZip memory roundtrip ok: " + BytesEqual(original, gunzippedMem));
    }

    // Compress side: WriteAsync(byte[], int, int) + FlushAsync + await using.
    private static async Task<byte[]> CompressDeflateAsyncArray(byte[] data)
    {
        using MemoryStream output = new MemoryStream();
        await using (DeflateStream deflate = new DeflateStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            await deflate.WriteAsync(data, 0, data.Length);
            await deflate.FlushAsync();
        }
        return output.ToArray();
    }

    // Compress side: WriteAsync(ReadOnlyMemory<byte>) (ValueTask-returning).
    private static async Task<byte[]> CompressDeflateAsyncMemory(byte[] data)
    {
        using MemoryStream output = new MemoryStream();
        await using (DeflateStream deflate = new DeflateStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            await deflate.WriteAsync(new ReadOnlyMemory<byte>(data));
        }
        return output.ToArray();
    }

    // Decompress side: ReadAsync(byte[], int, int) loop until EOF.
    private static async Task<byte[]> DecompressDeflateAsyncArray(byte[] compressed)
    {
        using MemoryStream input = new MemoryStream(compressed);
        await using DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        byte[] chunk = new byte[512];
        int read;
        while ((read = await deflate.ReadAsync(chunk, 0, chunk.Length)) > 0)
        {
            output.Write(chunk, 0, read);
        }
        return output.ToArray();
    }

    // Decompress side: ReadAsync(Memory<byte>) (ValueTask<int>-returning) loop.
    private static async Task<byte[]> DecompressDeflateAsyncMemory(byte[] compressed)
    {
        using MemoryStream input = new MemoryStream(compressed);
        await using DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        byte[] chunk = new byte[512];
        int read;
        while ((read = await deflate.ReadAsync(new Memory<byte>(chunk))) > 0)
        {
            output.Write(chunk, 0, read);
        }
        return output.ToArray();
    }

    // Decompress side: CopyToAsync into a MemoryStream.
    private static async Task<byte[]> DecompressDeflateCopyToAsync(byte[] compressed)
    {
        using MemoryStream input = new MemoryStream(compressed);
        await using DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        await deflate.CopyToAsync(output);
        return output.ToArray();
    }

    private static async Task<byte[]> CompressGZipAsyncArray(byte[] data)
    {
        using MemoryStream output = new MemoryStream();
        await using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            await gzip.WriteAsync(data, 0, data.Length);
            await gzip.FlushAsync();
        }
        return output.ToArray();
    }

    private static async Task<byte[]> CompressGZipAsyncMemory(byte[] data)
    {
        using MemoryStream output = new MemoryStream();
        await using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            await gzip.WriteAsync(new ReadOnlyMemory<byte>(data));
        }
        return output.ToArray();
    }

    private static async Task<byte[]> DecompressGZipAsyncArray(byte[] compressed)
    {
        using MemoryStream input = new MemoryStream(compressed);
        await using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        byte[] chunk = new byte[512];
        int read;
        while ((read = await gzip.ReadAsync(chunk, 0, chunk.Length)) > 0)
        {
            output.Write(chunk, 0, read);
        }
        return output.ToArray();
    }

    private static async Task<byte[]> DecompressGZipAsyncMemory(byte[] compressed)
    {
        using MemoryStream input = new MemoryStream(compressed);
        await using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        byte[] chunk = new byte[512];
        int read;
        while ((read = await gzip.ReadAsync(new Memory<byte>(chunk))) > 0)
        {
            output.Write(chunk, 0, read);
        }
        return output.ToArray();
    }

    // Deterministic xorshift32 fill behind a short readable prefix: mostly
    // incompressible, so the compressed size stays close to 65536 bytes
    // (comfortably more than one 8192-byte internal buffer's worth) on every
    // CompressionLevel, including NoCompression. Fixed seed -> byte-identical
    // input on every run, so the transpiled binary and real dotnet compress the
    // exact same bytes.
    private static byte[] BuildLargeBuffer(int length)
    {
        byte[] buffer = new byte[length];
        byte[] prefix = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. ");
        uint state = 0x9E3779B9u;
        for (int i = 0; i < length; i++)
        {
            if (i < prefix.Length)
            {
                buffer[i] = prefix[i];
                continue;
            }
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            buffer[i] = (byte)state;
        }
        return buffer;
    }

    private static byte[] CompressDeflate(byte[] data)
    {
        using MemoryStream output = new MemoryStream();
        using (DeflateStream deflate = new DeflateStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            deflate.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] CompressDeflateLevel(byte[] data, CompressionLevel level)
    {
        using MemoryStream output = new MemoryStream();
        using (DeflateStream deflate = new DeflateStream(output, level, leaveOpen: true))
        {
            deflate.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] DecompressDeflate(byte[] compressed)
    {
        using MemoryStream input = new MemoryStream(compressed);
        using DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] CompressGZip(byte[] data)
    {
        using MemoryStream output = new MemoryStream();
        using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] CompressGZipLevel(byte[] data, CompressionLevel level)
    {
        using MemoryStream output = new MemoryStream();
        using (GZipStream gzip = new GZipStream(output, level, leaveOpen: true))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] CompressZLib(byte[] data)
    {
        using MemoryStream output = new MemoryStream();
        using (ZLibStream zlib = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] CompressZLibLevel(byte[] data, CompressionLevel level)
    {
        using MemoryStream output = new MemoryStream();
        using (ZLibStream zlib = new ZLibStream(output, level, leaveOpen: true))
        {
            zlib.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] DecompressZLib(byte[] compressed)
    {
        using MemoryStream input = new MemoryStream(compressed);
        using ZLibStream zlib = new ZLibStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] DecompressGZip(byte[] compressed)
    {
        using MemoryStream input = new MemoryStream(compressed);
        using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private static bool BytesEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }
}
