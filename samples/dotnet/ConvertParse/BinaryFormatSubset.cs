using System;

// .NET 8+ "B"/"b" binary format on the integer primitives: the two's-complement
// bit pattern at the type's declared width (short -1 is sixteen 1s — like hex "X"
// but base 2), zero-padded to the precision with a one-digit minimum; '0'/'1'
// carry no case, so "b" produces the same digits as "B". Exercised through both
// ToString(format) and the span-write TryFormat so the shared binary core is
// covered on both entry points, including the buffer-too-short false case
// (buffer left untouched). Exact-diffed vs real .NET.
namespace BinaryFormatSubset
{
    internal static class Program
    {
        private static void P(string label, string s)
            => Console.WriteLine(label + " = " + s);

        // One TryFormat probe at dest capacity `cap`: the bool, out count, written
        // prefix, and the count of '.' fill chars left untouched after the prefix
        // (proves a too-short dest is never partially written).
        private static void Probe(string label, bool ok, int written, char[] buf)
        {
            int w = written < 0 ? 0 : written;
            string text = "";
            for (int i = 0; i < w && i < buf.Length; i++) text += buf[i];
            int untouched = 0;
            for (int i = w; i < buf.Length; i++) if (buf[i] == '.') untouched++;
            Console.WriteLine($"{label}: ok={ok} written={written} cap={buf.Length} w=[{text}] dots={untouched}");
        }

        private static char[] Dots(int n)
        {
            char[] b = new char[n < 0 ? 0 : n];
            for (int i = 0; i < b.Length; i++) b[i] = '.';
            return b;
        }

        private static void TryByte(byte v, string f)
        {
            char[] big = new char[96];
            v.TryFormat(big, out int len, f.AsSpan());
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0 })
            {
                char[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, f.AsSpan());
                Probe($"try byte {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void TrySByte(sbyte v, string f)
        {
            char[] big = new char[96];
            v.TryFormat(big, out int len, f.AsSpan());
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0 })
            {
                char[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, f.AsSpan());
                Probe($"try sbyte {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void TryShort(short v, string f)
        {
            char[] big = new char[96];
            v.TryFormat(big, out int len, f.AsSpan());
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0 })
            {
                char[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, f.AsSpan());
                Probe($"try short {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void TryUShort(ushort v, string f)
        {
            char[] big = new char[96];
            v.TryFormat(big, out int len, f.AsSpan());
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0 })
            {
                char[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, f.AsSpan());
                Probe($"try ushort {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void TryInt(int v, string f)
        {
            char[] big = new char[96];
            v.TryFormat(big, out int len, f.AsSpan());
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0 })
            {
                char[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, f.AsSpan());
                Probe($"try int {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void TryUInt(uint v, string f)
        {
            char[] big = new char[96];
            v.TryFormat(big, out int len, f.AsSpan());
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0 })
            {
                char[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, f.AsSpan());
                Probe($"try uint {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void TryLong(long v, string f)
        {
            char[] big = new char[96];
            v.TryFormat(big, out int len, f.AsSpan());
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0 })
            {
                char[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, f.AsSpan());
                Probe($"try long {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void TryULong(ulong v, string f)
        {
            char[] big = new char[96];
            v.TryFormat(big, out int len, f.AsSpan());
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0 })
            {
                char[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, f.AsSpan());
                Probe($"try ulong {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("-- binary format B/b --");
            // ToString(format): no precision, small precision (pad-or-ignore), a
            // precision above the natural digit count, and lowercase parity.
            string[] fmts = { "B", "b", "B1", "B4", "B8", "b8", "B12", "B16", "B20", "B32", "B64" };
            foreach (string f in fmts)
            {
                P($"byte 0 [{f}]", ((byte)0).ToString(f));
                P($"byte 5 [{f}]", ((byte)5).ToString(f));
                P($"byte 200 [{f}]", ((byte)200).ToString(f));
                P($"byte 255 [{f}]", byte.MaxValue.ToString(f));
                P($"sbyte -1 [{f}]", ((sbyte)-1).ToString(f));
                P($"sbyte -128 [{f}]", sbyte.MinValue.ToString(f));
                P($"sbyte 127 [{f}]", sbyte.MaxValue.ToString(f));
                P($"short -1 [{f}]", ((short)-1).ToString(f));
                P($"short -32768 [{f}]", short.MinValue.ToString(f));
                P($"short 4660 [{f}]", ((short)4660).ToString(f));
                P($"ushort 65535 [{f}]", ushort.MaxValue.ToString(f));
                P($"ushort 32768 [{f}]", ((ushort)32768).ToString(f));
                P($"int 0 [{f}]", 0.ToString(f));
                P($"int -1 [{f}]", (-1).ToString(f));
                P($"int min [{f}]", int.MinValue.ToString(f));
                P($"int max [{f}]", int.MaxValue.ToString(f));
                P($"int 305419896 [{f}]", 305419896.ToString(f));
                P($"uint max [{f}]", uint.MaxValue.ToString(f));
                P($"long -1 [{f}]", (-1L).ToString(f));
                P($"long min [{f}]", long.MinValue.ToString(f));
                P($"long 1234567890123 [{f}]", 1234567890123L.ToString(f));
                P($"ulong max [{f}]", ulong.MaxValue.ToString(f));
                P($"ulong high [{f}]", 0x8000000000000000UL.ToString(f));
            }

            Console.WriteLine("-- binary TryFormat --");
            foreach (string f in new[] { "B", "b", "B8", "B12" })
            {
                TryByte(0, f); TryByte(200, f);
                TrySByte(-1, f); TrySByte(-128, f);
                TryShort(-1, f); TryShort(4660, f);
                TryUShort(65535, f);
                TryInt(-1, f); TryInt(int.MinValue, f); TryInt(305419896, f);
                TryUInt(uint.MaxValue, f);
                TryLong(-1, f); TryLong(long.MinValue, f);
                TryULong(ulong.MaxValue, f);
            }
        }
    }
}
