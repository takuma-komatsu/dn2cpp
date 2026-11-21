using System;
using System.IO;
using System.IO.Compression;

namespace ZipCore
{
    // Gate driver: runs each section's __GateEntry() in order.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "aes-refusal")
            {
                AesRefusal();
                return;
            }
            ZipCreateRoundTripSubset.Program.__GateEntry();
            ZipFixedBlobReadSubset.Program.__GateEntry();
            ZipUpdateSubset.Program.__GateEntry();
            ZipCommentSubset.Program.__GateEntry();
            ZipErrorPathsSubset.Program.__GateEntry();
            ZipAsyncSubset.Program.__GateEntry();
        }

        // Native-only: the runtime has no AES or PBKDF2, so an encrypted entry is refused.
        private static void AesRefusal()
        {
            using var ms = new MemoryStream();
            using var zip = new ZipArchive(ms, ZipArchiveMode.Create, true);
            try
            {
                zip.CreateEntry("a.txt", "pw", ZipEncryptionMethod.Aes256);
                Console.WriteLine("created");
            }
            catch (PlatformNotSupportedException e)
            {
                Console.WriteLine("PlatformNotSupportedException:" + e.Message);
            }
        }
    }
}
