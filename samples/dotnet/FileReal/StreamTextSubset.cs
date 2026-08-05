using System;
using System.IO;
using System.Text;

namespace StreamTextSubset
{
    // StreamReader / StreamWriter constructed DIRECTLY on a path — the ctors, not the
    // File.CreateText / File.OpenText / File.AppendText factories the suite went through.
    //
    // Encoding.UTF8 ONLY. Encoding.ASCII / Latin1 / GetEncoding can newly reach the SIMD
    // transcoders (Ascii.WidenAsciiToUtf16), which are a separate carve-out — one that
    // fails loudly, naming the reach chain, rather than silently.
    internal static class Program
    {
        internal static void __GateEntry(string dir)
        {
            // new StreamWriter(path) — creates/truncates; new StreamReader(path) reads back.
            string p = Path.Combine(dir, "sw.txt");
            using (StreamWriter sw = new StreamWriter(p))
            {
                sw.WriteLine("alpha");
                sw.Write("beta");
                sw.Write('!');
                sw.Write(42);
            }
            using (StreamReader sr = new StreamReader(p))
                Console.WriteLine("swRT=[" + sr.ReadToEnd() + "]");

            // new StreamWriter(path, append: true) — appends rather than truncating.
            using (StreamWriter sw = new StreamWriter(p, append: true))
                sw.WriteLine();
            using (StreamWriter sw = new StreamWriter(p, append: true))
                sw.WriteLine("gamma");
            using (StreamReader sr = new StreamReader(p))
            {
                Console.WriteLine("appended1=[" + sr.ReadLine() + "]");
                Console.WriteLine("appended2=[" + sr.ReadLine() + "]");
                Console.WriteLine("appended3=[" + sr.ReadLine() + "]");
                Console.WriteLine("appendedEof=" + (sr.ReadLine() is null));
            }

            // new StreamWriter(path, append: false) — truncates.
            using (StreamWriter sw = new StreamWriter(p, append: false))
                sw.Write("fresh");
            Console.WriteLine("truncated=[" + File.ReadAllText(p) + "]");

            // The Encoding-taking ctors (UTF-8, no BOM on the writer's side by default in
            // .NET Core; a round trip through both is what is asserted, not the byte prefix).
            string e = Path.Combine(dir, "sw-enc.txt");
            using (StreamWriter sw = new StreamWriter(e, false, Encoding.UTF8))
                sw.Write("encé★");
            using (StreamReader sr = new StreamReader(e, Encoding.UTF8))
                Console.WriteLine("encRT=[" + sr.ReadToEnd() + "]");
            // The explicit-encoding reader and the default-encoding reader must agree.
            using (StreamReader a = new StreamReader(e))
            using (StreamReader b = new StreamReader(e, Encoding.UTF8))
                Console.WriteLine("encVsDefault=" + (a.ReadToEnd() == b.ReadToEnd()));

            // Peek: looks without consuming; EndOfStream / -1 at the end.
            using (StreamReader sr = new StreamReader(p))
            {
                Console.WriteLine("peek=" + (char)sr.Peek() + " read=" + (char)sr.Read()
                    + " peek2=" + (char)sr.Peek());
                Console.WriteLine("rest=[" + sr.ReadToEnd() + "] peekEof=" + sr.Peek()
                    + " eos=" + sr.EndOfStream);
            }

            // AutoFlush: with it on, the write reaches the file before Dispose does.
            string af = Path.Combine(dir, "sw-af.txt");
            using (StreamWriter sw = new StreamWriter(af))
            {
                sw.AutoFlush = true;
                sw.Write("now");
                Console.WriteLine("autoFlushOn=" + sw.AutoFlush + " len=" + new FileInfo(af).Length);
                sw.AutoFlush = false;
                sw.Write("later");
                Console.WriteLine("autoFlushOff=" + new FileInfo(af).Length);
                sw.Flush();
                Console.WriteLine("explicitFlush=" + new FileInfo(af).Length);
            }

            // A StreamWriter/StreamReader over a FileStream the caller owns (the ctor
            // overload taking a Stream), and the reader's BaseStream.
            string bs = Path.Combine(dir, "sw-bs.txt");
            using (FileStream fs = new FileStream(bs, FileMode.Create, FileAccess.ReadWrite))
            using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
            {
                sw.Write("over-a-stream");
                sw.Flush();
                Console.WriteLine("overStream=" + fs.Length);
            }
            using (StreamReader sr = new StreamReader(bs))
                Console.WriteLine("baseStream=" + sr.BaseStream.CanRead
                    + " text=[" + sr.ReadToEnd() + "]");

            // A reader on a path that is not there throws the real type.
            try
            {
                using (StreamReader sr = new StreamReader(Path.Combine(dir, "nope.txt"))) { }
                Console.WriteLine("readerMissing=noexc");
            }
            catch (FileNotFoundException) { Console.WriteLine("readerMissing=FileNotFoundException"); }
        }
    }
}
