using System;
using System.IO;

namespace FileStreamSubset
{
    // FileStream constructed DIRECTLY — the surface no gate had ever touched. Every
    // FileStream in the suite used to be handed over by a factory (File.Create /
    // File.OpenRead / File.Open), so the public ctors, the FileStreamOptions object,
    // FileShare, SetLength, Flush(flushToDisk), the sync CopyTo and the span overloads
    // were all untested.
    //
    // Prints no absolute paths: FileStream.Name IS one, so only its file-name component
    // is ever printed. Exception TYPE names only, never Message (it embeds the path).
    internal static class Program
    {
        internal static void __GateEntry(string dir)
        {
            byte[] five = new byte[] { 1, 2, 3, 4, 5 };

            // new FileStream(path, FileMode) — write access implied by Create.
            string p1 = Path.Combine(dir, "fs1.bin");
            using (FileStream fs = new FileStream(p1, FileMode.Create))
            {
                fs.Write(five, 0, 5);
                Console.WriteLine("ctor2=" + fs.Length + " canRead=" + fs.CanRead
                    + " canWrite=" + fs.CanWrite + " canSeek=" + fs.CanSeek
                    + " name=" + Path.GetFileName(fs.Name));
            }

            // new FileStream(path, FileMode, FileAccess) — read-only reopen.
            using (FileStream fs = new FileStream(p1, FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine("ctor3=" + fs.ReadByte() + " canWrite=" + fs.CanWrite);
                try { fs.WriteByte(9); Console.WriteLine("roWrite=noexc"); }
                catch (NotSupportedException) { Console.WriteLine("roWrite=NotSupportedException"); }
            }

            // new FileStream(path, FileMode, FileAccess, FileShare)
            using (FileStream fs = new FileStream(p1, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                fs.Position = 4;
                fs.WriteByte(50);
                Console.WriteLine("ctor4=" + fs.Length);
            }

            // new FileStream(path, FileMode, FileAccess, FileShare, bufferSize) — a small
            // buffer, so the write is larger than it and spills straight through.
            string p2 = Path.Combine(dir, "fs2.bin");
            using (FileStream fs = new FileStream(p2, FileMode.CreateNew, FileAccess.Write, FileShare.None, 16))
            {
                byte[] big = new byte[100];
                for (int i = 0; i < big.Length; i++)
                    big[i] = (byte)(i & 0x7f);
                fs.Write(big, 0, big.Length);
                fs.Flush(flushToDisk: true);   // Interop.Sys.FSync
                Console.WriteLine("ctor5=" + fs.Length);
            }
            Console.WriteLine("ctor5read=" + File.ReadAllBytes(p2)[99]);

            // new FileStream(path, FileStreamOptions) — the options-object path. The
            // preallocation hint goes to Interop.Sys.FAllocate; it does not change the
            // logical Length, which is what is asserted (the on-disk allocation is not
            // observable through the managed API, and must not be printed either way).
            string p3 = Path.Combine(dir, "fs3.bin");
            FileStreamOptions opts = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.ReadWrite,
                Share = FileShare.None,
                BufferSize = 4096,
                PreallocationSize = 1 << 16,
            };
            using (FileStream fs = new FileStream(p3, opts))
            {
                Console.WriteLine("optsLen=" + fs.Length + " canRW=" + (fs.CanRead && fs.CanWrite));
                fs.Write(five, 0, 5);
                fs.Position = 0;
                Console.WriteLine("optsRead=" + fs.ReadByte() + " len=" + fs.Length);
            }

            // The FileModes.
            string p4 = Path.Combine(dir, "fs4.bin");
            using (FileStream fs = new FileStream(p4, FileMode.OpenOrCreate, FileAccess.Write))
                fs.Write(five, 0, 5);
            Console.WriteLine("openOrCreate=" + new FileInfo(p4).Length);

            // Append: seeks to the end, and refuses to seek back.
            using (FileStream fs = new FileStream(p4, FileMode.Append, FileAccess.Write))
            {
                Console.WriteLine("appendPos=" + fs.Position + " canSeekBack=" + fs.CanSeek);
                fs.Write(new byte[] { 6, 7 }, 0, 2);
            }
            Console.WriteLine("appendLen=" + new FileInfo(p4).Length
                + " last=" + File.ReadAllBytes(p4)[6]);

            // Truncate: the file survives, its contents do not.
            using (FileStream fs = new FileStream(p4, FileMode.Truncate, FileAccess.Write))
                Console.WriteLine("truncate=" + fs.Length);

            // CreateNew over an existing file throws.
            try
            {
                using (FileStream fs = new FileStream(p4, FileMode.CreateNew, FileAccess.Write)) { }
                Console.WriteLine("createNewClash=noexc");
            }
            catch (IOException) { Console.WriteLine("createNewClash=IOException"); }

            // Create over an existing file truncates it instead.
            using (FileStream fs = new FileStream(p1, FileMode.Create, FileAccess.Write))
                Console.WriteLine("createTruncates=" + fs.Length);

            // SetLength: grow (the gap reads back as zeros) and shrink.
            string p5 = Path.Combine(dir, "fs5.bin");
            using (FileStream fs = new FileStream(p5, FileMode.Create, FileAccess.ReadWrite))
            {
                fs.Write(five, 0, 5);
                fs.SetLength(8);            // Interop.Sys.FTruncate, growing
                Console.WriteLine("grow=" + fs.Length + " pos=" + fs.Position);
                fs.Position = 0;
                byte[] eight = new byte[8];
                int n = fs.Read(eight, 0, 8);
                Console.WriteLine("growRead=" + n + ":" + eight[4] + "," + eight[5] + "," + eight[7]);
                fs.SetLength(2);            // shrinking; Position is clamped to the new end
                Console.WriteLine("shrink=" + fs.Length + " pos=" + fs.Position);
            }
            Console.WriteLine("shrunkOnDisk=" + new FileInfo(p5).Length);

            // The SPAN overloads: Read(Span<byte>) / Write(ReadOnlySpan<byte>).
            string p6 = Path.Combine(dir, "fs6.bin");
            using (FileStream fs = new FileStream(p6, FileMode.Create, FileAccess.ReadWrite))
            {
                fs.Write(new ReadOnlySpan<byte>(new byte[] { 11, 22, 33, 44 }));
                fs.Position = 1;
                Span<byte> dst = new byte[3];
                int n = fs.Read(dst);
                Console.WriteLine("span=" + n + ":" + dst[0] + "," + dst[1] + "," + dst[2]);
            }

            // Sync CopyTo(Stream) and CopyTo(Stream, bufferSize).
            using (FileStream src = new FileStream(p6, FileMode.Open, FileAccess.Read))
            using (MemoryStream dst = new MemoryStream())
            {
                src.CopyTo(dst);
                Console.WriteLine("copyTo=" + dst.Length + ":" + dst.ToArray()[0]);
            }
            string p7 = Path.Combine(dir, "fs7.bin");
            using (FileStream src = new FileStream(p6, FileMode.Open, FileAccess.Read))
            using (FileStream dst = new FileStream(p7, FileMode.Create, FileAccess.Write))
                src.CopyTo(dst, 2);   // a buffer smaller than the payload: several refills
            Console.WriteLine("copyToBuf=" + new FileInfo(p7).Length
                + ":" + File.ReadAllBytes(p7)[3]);

            // FileShare: on Unix the BCL emulates it with Interop.Sys.FLock, so a second
            // open that the first one's share mode forbids is refused. Asserted by TYPE.
            string p8 = Path.Combine(dir, "fs8.bin");
            using (FileStream first = new FileStream(p8, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                try
                {
                    using (FileStream second = new FileStream(p8, FileMode.Open, FileAccess.Read, FileShare.Read)) { }
                    Console.WriteLine("shareNone=noexc");
                }
                catch (IOException) { Console.WriteLine("shareNone=IOException"); }
            }
            // …and once the first handle is closed, the same open succeeds.
            using (FileStream after = new FileStream(p8, FileMode.Open, FileAccess.Read, FileShare.Read))
                Console.WriteLine("shareReleased=" + after.CanRead);

            // A share mode that DOES permit the second open.
            using (FileStream first = new FileStream(p8, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream second = new FileStream(p8, FileMode.Open, FileAccess.Read, FileShare.Read))
                Console.WriteLine("shareRead=" + (first.CanRead && second.CanRead));
        }
    }
}
