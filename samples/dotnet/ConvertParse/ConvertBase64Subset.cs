#nullable disable
using System;

namespace ConvertBase64Subset
{
    // Convert.ToBase64String(byte[]) / FromBase64String(string) over the
    // packed byte[]. Standard MIME base64 with '=' padding; decode ignores
    // whitespace. Plus the offset/length and Base64FormattingOptions overloads
    // (CRLF after every full 76 chars, none trailing), ToBase64CharArray /
    // FromBase64CharArray, and the Try* span forms with their short-buffer /
    // invalid-input false semantics and argument-validation exception types.
    internal static class Program
    {
        private static void Show<T>(string tag, Func<T> f)
        {
            try { Console.WriteLine(tag + " = " + f()); }
            catch (Exception ex) { Console.WriteLine(tag + " ! " + ex.GetType().Name); }
        }

        private static byte[] Seq(int n)
        {
            byte[] a = new byte[n];
            for (int i = 0; i < n; i++)
            {
                a[i] = (byte)(i * 7 + 1);
            }
            return a;
        }

        private static string Vis(string s)
        {
            return s.Replace("\r", "<CR>").Replace("\n", "<LF>");
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Convert.ToBase64String(new byte[] { 77, 97, 110 })); // TWFu
            Console.WriteLine(Convert.ToBase64String(new byte[] { 77, 97 }));      // TWE=
            Console.WriteLine(Convert.ToBase64String(new byte[] { 77 }));          // TQ==
            Console.WriteLine("[" + Convert.ToBase64String(new byte[0]) + "]");    // []
            Console.WriteLine(Convert.ToBase64String(new byte[] { 0, 255, 128, 1 })); // AP+AAQ==

            byte[] dec = Convert.FromBase64String("TWFu");
            Console.WriteLine(dec.Length);     // 3
            Console.WriteLine((int)dec[0]);    // 77
            Console.WriteLine((int)dec[1]);    // 97
            Console.WriteLine((int)dec[2]);    // 110

            byte[] payload = new byte[] { 1, 2, 3, 4, 5, 250, 251, 252, 253, 254, 255 };
            string b64 = Convert.ToBase64String(payload);
            Console.WriteLine(b64);            // AQIDBAX6+/z9/v8=
            byte[] back = Convert.FromBase64String(b64);
            Console.WriteLine(back.Length);    // 11
            bool same = back.Length == payload.Length;
            for (int i = 0; i < payload.Length && same; i++)
            {
                if (back[i] != payload[i])
                {
                    same = false;
                }
            }
            Console.WriteLine(same);           // True

            byte[] ws = Convert.FromBase64String("TW Fu");
            Console.WriteLine(Convert.ToBase64String(ws)); // TWFu

            Console.WriteLine("-- offset/length --");
            byte[] arr9 = Seq(9);
            Show("(1,3)", () => Convert.ToBase64String(arr9, 1, 3));
            Show("(0,0)", () => "[" + Convert.ToBase64String(arr9, 0, 0) + "]");
            Show("(9,0)", () => "[" + Convert.ToBase64String(arr9, 9, 0) + "]");
            Show("(-1,3)", () => Convert.ToBase64String(arr9, -1, 3));
            Show("(0,-1)", () => Convert.ToBase64String(arr9, 0, -1));
            Show("(7,3)", () => Convert.ToBase64String(arr9, 7, 3));
            Show("(10,0)", () => Convert.ToBase64String(arr9, 10, 0));
            Show("(null)", () => Convert.ToBase64String((byte[])null));
            Show("(null,0,0)", () => Convert.ToBase64String(null, 0, 0));

            Console.WriteLine("-- InsertLineBreaks --");
            Show("len56", () => Vis(Convert.ToBase64String(Seq(56), Base64FormattingOptions.InsertLineBreaks)));
            Show("len57", () => Vis(Convert.ToBase64String(Seq(57), Base64FormattingOptions.InsertLineBreaks)));
            Show("len58", () => Vis(Convert.ToBase64String(Seq(58), Base64FormattingOptions.InsertLineBreaks)));
            Show("len76", () => Vis(Convert.ToBase64String(Seq(76), Base64FormattingOptions.InsertLineBreaks)));
            Show("len77", () => Vis(Convert.ToBase64String(Seq(77), Base64FormattingOptions.InsertLineBreaks)));
            Show("len115", () => Vis(Convert.ToBase64String(Seq(115), Base64FormattingOptions.InsertLineBreaks)));
            Show("len0", () => "[" + Vis(Convert.ToBase64String(Seq(0), Base64FormattingOptions.InsertLineBreaks)) + "]");
            Show("len1", () => Vis(Convert.ToBase64String(Seq(1), Base64FormattingOptions.InsertLineBreaks)));
            Show("none", () => Convert.ToBase64String(Seq(5), Base64FormattingOptions.None));
            Show("bad option", () => Convert.ToBase64String(Seq(5), (Base64FormattingOptions)7));
            Show("slice+breaks", () => Convert.ToBase64String(arr9, 1, 3, Base64FormattingOptions.InsertLineBreaks));
            Show("span", () => Convert.ToBase64String(new ReadOnlySpan<byte>(arr9, 1, 3)));
            Show("span+breaks", () => Vis(Convert.ToBase64String(new ReadOnlySpan<byte>(Seq(58)), Base64FormattingOptions.InsertLineBreaks)));

            Console.WriteLine("-- ToBase64CharArray --");
            Show("chararray", () =>
            {
                char[] outc = new char[16];
                int n = Convert.ToBase64CharArray(arr9, 1, 3, outc, 2);
                return n + ":" + new string(outc, 2, n);
            });
            Show("chararray breaks", () =>
            {
                char[] outc = new char[100];
                int n = Convert.ToBase64CharArray(Seq(58), 0, 58, outc, 0, Base64FormattingOptions.InsertLineBreaks);
                return n + ":" + Vis(new string(outc, 0, n));
            });
            Show("ca null in", () => Convert.ToBase64CharArray(null, 0, 0, new char[4], 0));
            Show("ca null out", () => Convert.ToBase64CharArray(arr9, 0, 3, null, 0));
            Show("ca small out", () => Convert.ToBase64CharArray(arr9, 0, 9, new char[4], 0));
            Show("ca offOut neg", () => Convert.ToBase64CharArray(arr9, 0, 3, new char[8], -1));
            Show("ca offOut over", () => Convert.ToBase64CharArray(arr9, 0, 3, new char[8], 5));
            Show("ca offOut edge", () =>
            {
                char[] c = new char[8];
                return Convert.ToBase64CharArray(arr9, 0, 3, c, 4) + ":" + new string(c, 4, 4);
            });

            Console.WriteLine("-- TryToBase64Chars --");
            Show("try ok", () =>
            {
                Span<char> dest = new char[8];
                bool ok = Convert.TryToBase64Chars(new ReadOnlySpan<byte>(arr9, 0, 3), dest, out int w);
                return ok + ":" + w + ":" + new string(dest.Slice(0, w));
            });
            Show("try short untouched", () =>
            {
                char[] destArr = new char[3] { 'x', 'y', 'z' };
                bool ok = Convert.TryToBase64Chars(new ReadOnlySpan<byte>(arr9, 0, 3), destArr, out int w);
                return ok + ":" + w + ":" + new string(destArr);
            });
            Show("try exact", () =>
            {
                Span<char> dest = new char[4];
                bool ok = Convert.TryToBase64Chars(new ReadOnlySpan<byte>(arr9, 0, 3), dest, out int w);
                return ok + ":" + w + ":" + new string(dest);
            });
            Show("try breaks short", () =>
            {
                char[] destArr = new char[78];
                for (int i = 0; i < 78; i++)
                {
                    destArr[i] = '.';
                }
                bool ok = Convert.TryToBase64Chars(new ReadOnlySpan<byte>(Seq(58)), destArr, out int w, Base64FormattingOptions.InsertLineBreaks);
                return ok + ":" + w + ":" + destArr[0];
            });
            Show("try breaks ok", () =>
            {
                Span<char> dest = new char[82];
                bool ok = Convert.TryToBase64Chars(new ReadOnlySpan<byte>(Seq(58)), dest, out int w, Base64FormattingOptions.InsertLineBreaks);
                return ok + ":" + w + ":" + Vis(new string(dest.Slice(0, w)));
            });
            Show("try empty", () =>
            {
                bool ok = Convert.TryToBase64Chars(ReadOnlySpan<byte>.Empty, Span<char>.Empty, out int w);
                return ok + ":" + w;
            });

            Console.WriteLine("-- TryFromBase64 --");
            Show("tf ok", () =>
            {
                Span<byte> dest = new byte[8];
                bool ok = Convert.TryFromBase64Chars("TWFu".AsSpan(), dest, out int w);
                return ok + ":" + w + ":" + dest[0] + "," + dest[1] + "," + dest[2];
            });
            Show("tf ws", () =>
            {
                Span<byte> dest = new byte[8];
                bool ok = Convert.TryFromBase64Chars(" T W\tFu \r\n".AsSpan(), dest, out int w);
                return ok + ":" + w + ":" + dest[0] + "," + dest[1] + "," + dest[2];
            });
            Show("tf short", () =>
            {
                byte[] destArr = new byte[2] { 9, 9 };
                bool ok = Convert.TryFromBase64Chars("TWFu".AsSpan(), destArr, out int w);
                return ok + ":" + w + ":" + destArr[0] + "," + destArr[1];
            });
            Show("tf short multiblock", () =>
            {
                byte[] destArr = new byte[4] { 9, 9, 9, 9 };
                bool ok = Convert.TryFromBase64Chars("TWFuTWFu".AsSpan(), destArr, out int w);
                return ok + ":" + w + ":" + destArr[0] + "," + destArr[1] + "," + destArr[2] + "," + destArr[3];
            });
            Show("tf invalid", () =>
            {
                byte[] destArr = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    destArr[i] = 9;
                }
                bool ok = Convert.TryFromBase64Chars("TWFuT!Fu".AsSpan(), destArr, out int w);
                return ok + ":" + w + ":" + destArr[0] + "," + destArr[1] + "," + destArr[2] + "," + destArr[3];
            });
            Show("tf badlen", () =>
            {
                Span<byte> dest = new byte[8];
                bool ok = Convert.TryFromBase64Chars("TWFuA".AsSpan(), dest, out int w);
                return ok + ":" + w;
            });
            Show("tf pad mid", () =>
            {
                Span<byte> dest = new byte[8];
                bool ok = Convert.TryFromBase64Chars("TW=u".AsSpan(), dest, out int w);
                return ok + ":" + w;
            });
            Show("tf pad ok", () =>
            {
                Span<byte> dest = new byte[8];
                bool ok = Convert.TryFromBase64Chars("TQ==".AsSpan(), dest, out int w);
                return ok + ":" + w + ":" + dest[0];
            });
            Show("tf empty", () =>
            {
                bool ok = Convert.TryFromBase64Chars(ReadOnlySpan<char>.Empty, Span<byte>.Empty, out int w);
                return ok + ":" + w;
            });
            Show("tf ws only", () =>
            {
                bool ok = Convert.TryFromBase64Chars("  \n".AsSpan(), Span<byte>.Empty, out int w);
                return ok + ":" + w;
            });
            Show("tfs ok", () =>
            {
                Span<byte> dest = new byte[8];
                bool ok = Convert.TryFromBase64String("TWE=", dest, out int w);
                return ok + ":" + w + ":" + dest[0] + "," + dest[1];
            });
            Show("tfs null", () =>
            {
                Span<byte> dest = new byte[8];
                bool ok = Convert.TryFromBase64String(null, dest, out int w);
                return ok + ":" + w;
            });

            Console.WriteLine("-- FromBase64CharArray / padding --");
            Show("fbca", () =>
            {
                char[] c = "xxTWFuyy".ToCharArray();
                byte[] b = Convert.FromBase64CharArray(c, 2, 4);
                return b.Length + ":" + b[0] + "," + b[1] + "," + b[2];
            });
            Show("fbca null", () => Convert.FromBase64CharArray(null, 0, 0));
            Show("fbca neg off", () => Convert.FromBase64CharArray("TWFu".ToCharArray(), -1, 4));
            Show("fbca neg len", () => Convert.FromBase64CharArray("TWFu".ToCharArray(), 0, -1));
            Show("fbca over", () => Convert.FromBase64CharArray("TWFu".ToCharArray(), 2, 4));
            Show("fbs bad pad", () => Convert.FromBase64String("TWFu="));
            Show("fbs pad mid", () => Convert.FromBase64String("TW=u"));
            Show("fbs ===", () => Convert.FromBase64String("T==="));
            Show("fbs badchar", () => Convert.FromBase64String("TW!u"));
            Show("fbs null", () => Convert.FromBase64String(null));
            Show("fbs ws pad", () =>
            {
                byte[] b = Convert.FromBase64String("TQ = =");
                return b.Length + ":" + b[0];
            });
        }
    }
}
