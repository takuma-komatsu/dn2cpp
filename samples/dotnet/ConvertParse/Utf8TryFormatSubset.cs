using System;
using System.Text;

// IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten,
// ReadOnlySpan<char> format, IFormatProvider) on the integer primitives — the
// shape-identical UTF-8 twin of ISpanFormattable's char16 writer. The destination
// span's ELEMENT TYPE is the only discriminator between the two interface methods,
// so this pins both that the byte-span arm formats correct UTF-8 (never the char16
// writer corrupting a byte buffer) and that the char-span arm still fires for
// Span<char>. Covers direct calls on all eight integer widths, the buffer-too-small
// false case (buffer bytes printed to prove it stays untouched), and a generic
// `T : IUtf8SpanFormattable` helper exercising the constrained-callvirt route.
// Results decode via Encoding.UTF8.GetString; exact-diffed vs real .NET.
namespace Utf8TryFormatSubset
{
    internal static class Program
    {
        // One probe: format into a 0x2E ('.')-filled byte buffer of capacity `cap`,
        // print the bool, out count, the decoded written prefix, and every raw
        // buffer byte after the prefix (proves a too-short dest is never partially
        // written — the '.' fill must survive intact).
        private static void Probe(string label, bool ok, int written, byte[] buf)
        {
            int w = written < 0 ? 0 : written;
            string text = Encoding.UTF8.GetString(buf, 0, w <= buf.Length ? w : buf.Length);
            string tail = "";
            for (int i = w; i < buf.Length; i++) tail += " " + buf[i];
            Console.WriteLine($"{label}: ok={ok} written={written} cap={buf.Length} w=[{text}] tail=[{tail} ]");
        }

        private static byte[] Dots(int n)
        {
            byte[] b = new byte[n < 0 ? 0 : n];
            for (int i = 0; i < b.Length; i++) b[i] = (byte)'.';
            return b;
        }

        private static ReadOnlySpan<char> Fmt(string f) => f.Length == 0 ? default : f.AsSpan();

        private static void U8Byte(byte v, string f)
        {
            byte[] big = new byte[128];
            v.TryFormat(big, out int len, Fmt(f));
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0, 0 })
            {
                byte[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, Fmt(f));
                Probe($"u8 byte {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void U8SByte(sbyte v, string f)
        {
            byte[] big = new byte[128];
            v.TryFormat(big, out int len, Fmt(f));
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0, 0 })
            {
                byte[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, Fmt(f));
                Probe($"u8 sbyte {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void U8Short(short v, string f)
        {
            byte[] big = new byte[128];
            v.TryFormat(big, out int len, Fmt(f));
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0, 0 })
            {
                byte[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, Fmt(f));
                Probe($"u8 short {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void U8UShort(ushort v, string f)
        {
            byte[] big = new byte[128];
            v.TryFormat(big, out int len, Fmt(f));
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0, 0 })
            {
                byte[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, Fmt(f));
                Probe($"u8 ushort {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void U8Int(int v, string f)
        {
            byte[] big = new byte[128];
            v.TryFormat(big, out int len, Fmt(f));
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0, 0 })
            {
                byte[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, Fmt(f));
                Probe($"u8 int {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void U8UInt(uint v, string f)
        {
            byte[] big = new byte[128];
            v.TryFormat(big, out int len, Fmt(f));
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0, 0 })
            {
                byte[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, Fmt(f));
                Probe($"u8 uint {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void U8Long(long v, string f)
        {
            byte[] big = new byte[128];
            v.TryFormat(big, out int len, Fmt(f));
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0, 0 })
            {
                byte[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, Fmt(f));
                Probe($"u8 long {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        private static void U8ULong(ulong v, string f)
        {
            byte[] big = new byte[128];
            v.TryFormat(big, out int len, Fmt(f));
            foreach (int cap in new[] { len + 2, len, len - 1 >= 0 ? len - 1 : 0, 0 })
            {
                byte[] buf = Dots(cap);
                bool ok = v.TryFormat(buf, out int n, Fmt(f));
                Probe($"u8 ulong {v} [{f}] cap={cap}", ok, n, buf);
            }
        }

        // The generic constrained-callvirt route: `constrained. T callvirt
        // IUtf8SpanFormattable::TryFormat` with T an intrinsic integer primitive.
        private static string F<T>(T v, string fmt) where T : IUtf8SpanFormattable
        {
            byte[] buf = new byte[128];
            bool ok = v.TryFormat(buf, out int n, fmt.AsSpan(), null);
            return ok ? Encoding.UTF8.GetString(buf, 0, n) : "<false>";
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("-- utf8 TryFormat direct --");
            foreach (string f in new[] { "", "B", "X4", "x", "D3", "N0", "G" })
            {
                U8Byte(0, f); U8Byte(200, f); U8Byte(255, f);
                U8SByte(-1, f); U8SByte(-128, f); U8SByte(127, f);
                U8Short(-1, f); U8Short(-32768, f); U8Short(4660, f);
                U8UShort(65535, f); U8UShort(32768, f);
                U8Int(0, f); U8Int(-1, f); U8Int(int.MinValue, f); U8Int(305419896, f);
                U8UInt(uint.MaxValue, f);
                U8Long(-1L, f); U8Long(long.MinValue, f); U8Long(1234567890123L, f);
                U8ULong(ulong.MaxValue, f);
            }

            Console.WriteLine("-- utf8 TryFormat constrained generic --");
            foreach (string f in new[] { "", "B", "X4", "D3", "N0" })
            {
                Console.WriteLine($"F byte 200 [{f}] = {F((byte)200, f)}");
                Console.WriteLine($"F sbyte -1 [{f}] = {F((sbyte)-1, f)}");
                Console.WriteLine($"F short -32768 [{f}] = {F((short)-32768, f)}");
                Console.WriteLine($"F ushort 65535 [{f}] = {F((ushort)65535, f)}");
                Console.WriteLine($"F int min [{f}] = {F(int.MinValue, f)}");
                Console.WriteLine($"F uint max [{f}] = {F(uint.MaxValue, f)}");
                Console.WriteLine($"F long min [{f}] = {F(long.MinValue, f)}");
                Console.WriteLine($"F ulong max [{f}] = {F(ulong.MaxValue, f)}");
            }
        }
    }
}
