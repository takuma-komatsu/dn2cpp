using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DnZlib.Tests.Oracles;

/// <summary>
/// P/Invoke oracle over the system's native zlib (macOS <c>libz.1.dylib</c> etc.). Used to
/// cross-check our checksums and, later, compress/decompress interop. Gated by <see cref="Available"/>
/// so tests skip cleanly on machines without a native zlib.
/// </summary>
internal static unsafe partial class LibZOracle
{
    private const string Lib = "libz";

    /// <summary>True if a native zlib could be loaded and called.</summary>
    public static bool Available { get; private set; }

    [ModuleInitializer]
    internal static void RegisterResolver()
    {
        NativeLibrary.SetDllImportResolver(typeof(LibZOracle).Assembly, static (name, _, _) =>
        {
            if (name != Lib)
                return IntPtr.Zero;

            string[] candidates =
            [
                "libz.1.dylib", "libz.dylib", "/usr/lib/libz.1.dylib",
                "libz.so.1", "libz.so", "z",
            ];
            foreach (string candidate in candidates)
            {
                if (NativeLibrary.TryLoad(candidate, out IntPtr handle))
                    return handle;
            }
            return IntPtr.Zero;
        });
    }

    static LibZOracle()
    {
        try
        {
            byte b = 0;
            _ = crc32(0, &b, 0);
            Available = true;
        }
        catch
        {
            Available = false;
        }
    }

    [LibraryImport(Lib)] private static partial nuint crc32(nuint crc, byte* buf, uint len);
    [LibraryImport(Lib)] private static partial nuint adler32(nuint adler, byte* buf, uint len);
    [LibraryImport(Lib)] private static partial nuint compressBound(nuint sourceLen);
    [LibraryImport(Lib)] private static partial int compress2(byte* dest, nuint* destLen, byte* source, nuint sourceLen, int level);
    [LibraryImport(Lib)] private static partial int uncompress(byte* dest, nuint* destLen, byte* source, nuint sourceLen);

    public static uint Crc32(ReadOnlySpan<byte> data)
    {
        fixed (byte* p = data)
            return (uint)crc32(0, p, (uint)data.Length);
    }

    public static uint Adler32(ReadOnlySpan<byte> data)
    {
        fixed (byte* p = data)
            return (uint)adler32(1, p, (uint)data.Length);
    }

    /// <summary>Compress with native zlib (zlib-wrapped DEFLATE) at the given level.</summary>
    public static byte[] Compress(ReadOnlySpan<byte> source, int level)
    {
        nuint destLen = compressBound((nuint)source.Length);
        var dest = new byte[destLen];
        int rc;
        fixed (byte* d = dest)
        fixed (byte* s = source)
            rc = compress2(d, &destLen, s, (nuint)source.Length, level);
        if (rc != 0)
            throw new InvalidOperationException($"native compress2 failed with code {rc}");
        Array.Resize(ref dest, (int)destLen);
        return dest;
    }

    /// <summary>Decompress a native zlib stream into a buffer of known size.</summary>
    public static byte[] Uncompress(ReadOnlySpan<byte> source, int expectedSize)
    {
        var dest = new byte[expectedSize];
        nuint destLen = (nuint)expectedSize;
        int rc;
        fixed (byte* d = dest)
        fixed (byte* s = source)
            rc = uncompress(d, &destLen, s, (nuint)source.Length);
        if (rc != 0)
            throw new InvalidOperationException($"native uncompress failed with code {rc}");
        Array.Resize(ref dest, (int)destLen);
        return dest;
    }
}
