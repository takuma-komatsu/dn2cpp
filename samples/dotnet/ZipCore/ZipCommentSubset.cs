#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ZipCommentSubset
{
    // Comment round-trips past the zip format's 65535-byte limit, where the setter
    // truncates on a Rune boundary — so the comments carry 2/3/4-byte UTF-8
    // sequences straddling the cut. The stamp uses an even second because DOS time
    // has 2-second resolution and no zone.
    //
    // The truncation depends only on the string and UTF-8, never on the deflate
    // implementation, so only char counts and bools are printed — no compressed
    // bytes (see ZipCreateRoundTripSubset).
    internal static class Program
    {
        private static readonly DateTimeOffset FixedStamp =
            new DateTimeOffset(2022, 6, 7, 8, 9, 10, TimeSpan.Zero);

        internal static void __GateEntry()
        {
            // 17 UTF-8 bytes per unit, so 5000 units clears the 65535-byte limit.
            var sb = new StringBuilder();
            for (int i = 0; i < 5000; i++)
            {
                sb.Append("é€\U0001F600abcdefgh");
            }
            string longEntryComment = sb.ToString();
            string longArchiveComment = new string('z', 70000);
            const string shortComment = "short comment: café € \U0001F600";

            var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                archive.Comment = longArchiveComment;

                ZipArchiveEntry plain = archive.CreateEntry("short.txt");
                plain.Comment = shortComment;
                plain.LastWriteTime = FixedStamp;
                WritePayload(plain);

                ZipArchiveEntry truncated = archive.CreateEntry("truncated.txt");
                truncated.Comment = longEntryComment;
                truncated.LastWriteTime = FixedStamp;
                WritePayload(truncated);
            }

            ms.Position = 0;
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                Console.WriteLine("comment: archiveLen=" + archive.Comment.Length
                    + " archivePrefix=" + longArchiveComment.StartsWith(archive.Comment)
                    + " archiveTruncated=" + (archive.Comment.Length < longArchiveComment.Length));

                ZipArchiveEntry plain = archive.GetEntry("short.txt");
                Console.WriteLine("  short.txt commentOk=" + (plain.Comment == shortComment)
                    + " lwtOk=" + (plain.LastWriteTime.DateTime == FixedStamp.DateTime));

                ZipArchiveEntry truncated = archive.GetEntry("truncated.txt");
                Console.WriteLine("  truncated.txt commentLen=" + truncated.Comment.Length
                    + " prefix=" + longEntryComment.StartsWith(truncated.Comment)
                    + " truncated=" + (truncated.Comment.Length < longEntryComment.Length)
                    + " lwtOk=" + (truncated.LastWriteTime.DateTime == FixedStamp.DateTime));
            }
        }

        private static void WritePayload(ZipArchiveEntry entry)
        {
            byte[] payload = Encoding.UTF8.GetBytes("comment subset payload");
            using (Stream s = entry.Open())
            {
                s.Write(payload, 0, payload.Length);
            }
        }
    }
}
