#nullable disable
using System;
using System.IO;
using System.Reflection;
using System.Text;

// Assembly.GetManifestResourceStream over the app assembly's own embedded
// resources: a UTF-8 text file and a 256-byte binary blob holding every byte
// value (0x00 included, so a NUL-terminated carry would be visible). The stream
// dn2cpp hands back is a read-only MemoryStream over a copy of the .rodata blob
// where real .NET hands back an UnmanagedMemoryStream over the mapped image — the
// two are indistinguishable through the Stream surface, which is what this
// section pins: Length/Position/CanRead/CanWrite/CanSeek, sequential Read, Seek,
// ReadByte at EOF.
namespace ResourceStreamSubset
{
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("== resource streams ==");
            Assembly asm = Assembly.GetExecutingAssembly();

            using (Stream s = asm.GetManifestResourceStream("ManifestResources.hello.txt"))
            {
                Console.WriteLine("hello-null " + (s == null));
                Console.WriteLine("hello-len " + s.Length);
                Console.WriteLine("hello-pos " + s.Position);
                Console.WriteLine("hello-read " + s.CanRead);
                Console.WriteLine("hello-write " + s.CanWrite);
                Console.WriteLine("hello-seek " + s.CanSeek);
                byte[] buf = new byte[(int)s.Length];
                int n = s.Read(buf, 0, buf.Length);
                Console.WriteLine("hello-n " + n);
                Console.WriteLine("hello-eof " + s.ReadByte());
                // Trailing newline stripped so the line stays one line whatever the
                // checkout's line endings are (both sides read the same bytes, so the
                // LENGTH above already pins the payload exactly).
                Console.WriteLine("hello-text [" + Encoding.UTF8.GetString(buf, 0, n).Replace("\r", "\\r").Replace("\n", "\\n") + "]");
            }

            using (Stream s = asm.GetManifestResourceStream("ManifestResources.bytes.bin"))
            {
                Console.WriteLine("bin-len " + s.Length);
                int sum = 0, count = 0, b;
                while ((b = s.ReadByte()) >= 0)
                {
                    sum += b;
                    count++;
                }
                Console.WriteLine("bin-count " + count);
                Console.WriteLine("bin-sum " + sum);
                s.Position = 0;
                Console.WriteLine("bin-first " + s.ReadByte());
                s.Position = 255;
                Console.WriteLine("bin-last " + s.ReadByte());
                s.Seek(-3, SeekOrigin.End);
                Console.WriteLine("bin-seek " + s.ReadByte());
            }

            // A resource SET (.resources, what a .resx compiles to) travels as opaque
            // bytes like any other: the RuntimeResourceSet magic 0xBEEFCACE is the
            // first little-endian uint32 of the blob.
            using (Stream s = asm.GetManifestResourceStream("ManifestResources.Strings.resources"))
            {
                byte[] magic = new byte[4];
                int n = s.Read(magic, 0, 4);
                Console.WriteLine("set-n " + n);
                Console.WriteLine("set-magic " + magic[3].ToString("x2") + magic[2].ToString("x2")
                    + magic[1].ToString("x2") + magic[0].ToString("x2"));
                Console.WriteLine("set-big " + (s.Length > 100));
            }

            // The (Type, string) overload scopes the name by the type's NAMESPACE.
            using (Stream s = asm.GetManifestResourceStream(typeof(ManifestResourceScoped.Marker), "scoped.txt"))
            {
                Console.WriteLine("scoped-null " + (s == null));
                byte[] buf = new byte[(int)s.Length];
                int n = s.Read(buf, 0, buf.Length);
                Console.WriteLine("scoped-text " + Encoding.UTF8.GetString(buf, 0, n));
            }
        }
    }
}

namespace ManifestResourceScoped
{
    // Namespace anchor for the (Type, string) overload above — the resource's
    // manifest name is "ManifestResourceScoped.scoped.txt".
    internal sealed class Marker
    {
    }
}
