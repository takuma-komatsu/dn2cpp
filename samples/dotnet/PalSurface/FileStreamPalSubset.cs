using System;
using System.IO;
using System.Text;

namespace FileStreamPalSubset
{
    // The PAL's file-I/O closure -- SafeFileHandle/FileStream/File.* bottoming out in
    // Interop.Sys.{Open,Close,LSeek,Read,PRead,PWrite,FSync,FStat,Stat,Unlink,FLock,
    // FTruncate,FAllocate,PosixFAdvise,GetCwd,GetFileSystemType}, and behind all of them
    // the error trio (StrErrorR and the two PAL/platform converters), which no single
    // line below reaches on its own. A game reaches the whole thing without asking:
    // Trace with a DefaultTraceListener log file goes File.AppendAllText ->
    // SafeFileHandle -> all of it.
    //
    // Nothing here may print a PATH or a CWD: the diff oracle is real .NET on the host,
    // whose temp directory and working directory are not Emscripten's MEMFS ones. Only
    // derived booleans and file CONTENT cross the diff.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("-- FileStreamPalSubset --");

            // getcwd(3). The value is host-specific; that it is rooted and non-empty is not.
            string cwd = Directory.GetCurrentDirectory();
            Console.WriteLine($"cwdRooted={cwd.Length > 0 && Path.IsPathRooted(cwd)}");

            string path = Path.Combine(Path.GetTempPath(), "dn2cpp-palsurface.txt");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.WriteAllText(path, "alpha\n");
            File.AppendAllText(path, "beta\n");
            Console.WriteLine($"roundTrip={File.ReadAllText(path).Replace("\n", "|")}");
            Console.WriteLine($"exists={File.Exists(path)}");

            // FileShare arms flock(2) and FileOptions.SequentialScan arms posix_fadvise(2),
            // on this open and on the plain one below alike. PreallocationSize arms the
            // fallocate hint, which is accepted only on a CREATING mode -- and it reserves
            // BLOCKS: the length stays what was written, or every length-driven reader sees
            // zeros nobody wrote.
            var options = new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.ReadWrite,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan,
                PreallocationSize = 4096,
            };
            string prealloc = Path.Combine(Path.GetTempPath(), "dn2cpp-palsurface-prealloc.bin");
            using (var fs = new FileStream(prealloc, options))
            {
                Console.WriteLine($"preallocLength={fs.Length}"); // fstat(2): 0, not 4096
                fs.Write(Encoding.UTF8.GetBytes("gamma\n"));
                fs.Flush();
                Console.WriteLine($"afterWrite={fs.Length}");
            }
            File.Delete(prealloc);

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                Console.WriteLine($"fsLength={fs.Length}"); // fstat(2)
                fs.Seek(0, SeekOrigin.End);                 // lseek(2)
                fs.Write(Encoding.UTF8.GetBytes("gamma\n"));
                fs.Flush();
                Console.WriteLine($"afterAppend={fs.Length}");

                fs.Seek(6, SeekOrigin.Begin);
                byte[] buf = new byte[4];
                int n = fs.Read(buf, 0, buf.Length);
                Console.WriteLine($"read={n}:{Encoding.UTF8.GetString(buf, 0, n)}");

                fs.SetLength(6); // ftruncate(2)
                Console.WriteLine($"afterTruncate={fs.Length}");
            }
            Console.WriteLine($"truncatedContent={File.ReadAllText(path).Replace("\n", "|")}");

            File.Delete(path); // unlink(2)
            Console.WriteLine($"existsAfterDelete={File.Exists(path)}");

            // Two failed opens, two errnos, one dedicated exception each. Only the
            // exception TYPE crosses the diff: the message carries the path, and where the
            // BCL falls back to strerror the wording is the host libc's.
            try
            {
                File.ReadAllText(Path.Combine(cwd, "dn2cpp-palsurface-absent.txt"));
                Console.WriteLine("openMissing=UNREACHABLE");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("openMissing=FileNotFoundException");
            }

            try
            {
                using var dirStream = new FileStream(cwd, FileMode.Open, FileAccess.Read);
                Console.WriteLine("openDirectory=UNREACHABLE");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("openDirectory=UnauthorizedAccessException");
            }
        }
    }
}
