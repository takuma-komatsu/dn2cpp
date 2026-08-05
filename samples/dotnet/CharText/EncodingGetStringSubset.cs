#nullable disable
using System;
using System.Text;

// App-module code calls Encoding.GetString on the UTF8 / Unicode (UTF-16LE)
// encodings, covering valid, empty, and malformed byte runs. dn2cpp's own PEReader
// reads PE section names via
// Encoding.UTF8.GetString(byte[], int, int); the real GetString body reaches
// String.CreateStringFromEncoding -> the SIMD UTF-8 transcode subtree we never
// transpile, so the transpiler intercepts GetString and lowers it to a portable,
// .NET-EXACT decode helper (with the U+FFFD DecoderReplacementFallback on bad
// input), excluding the real bodies from reachability.
//
// To stay deterministic and ASCII-safe on stdout (the gate does an exact string
// diff), we never print the decoded string directly — we print its length and each
// UTF-16 code unit as 4 hex digits. This makes U+FFFD replacement chars visible and
// avoids any terminal/locale dependence, while still being a byte-for-byte exact
// diff against real .NET. CoreLib only (no Linq shim).
namespace EncodingGetStringSubset;

class Program
{
    // Render a decoded string as "n=<len> [XXXX XXXX ...]" (code units in hex).
    static string Dump(string s)
    {
        string r = "n=" + s.Length + " [";
        for (int i = 0; i < s.Length; i++)
        {
            if (i != 0) r = r + " ";
            r = r + ((int)s[i]).ToString("X4");
        }
        return r + "]";
    }

    static void U8(string label, byte[] bytes)
    {
        Console.WriteLine(label + ": " + Dump(Encoding.UTF8.GetString(bytes)));
    }
    static void U8R(string label, byte[] bytes, int index, int count)
    {
        Console.WriteLine(label + ": " + Dump(Encoding.UTF8.GetString(bytes, index, count)));
    }
    static void U16(string label, byte[] bytes)
    {
        Console.WriteLine(label + ": " + Dump(Encoding.Unicode.GetString(bytes)));
    }

    // GetString(byte*, int) — the unsafe-pointer overload used by SRM's metadata
    // string decode (MetadataStringDecoder.GetString / MemoryBlock.PeekUtf8).
    static unsafe void U8Ptr(string label, byte[] bytes, int count)
    {
        fixed (byte* p = bytes)
        {
            Console.WriteLine(label + ": " + Dump(Encoding.UTF8.GetString(p, count)));
        }
    }

    // GetString(ReadOnlySpan<byte>) — the span overload used by STJ's
    // JsonHelpers.Utf8GetString (property-key decode on the deserialize path).
    static void U8Span(string label, byte[] bytes, int start, int len)
    {
        Console.WriteLine(label + ": " + Dump(Encoding.UTF8.GetString(new ReadOnlySpan<byte>(bytes, start, len))));
    }

    internal static void __GateEntry()
    {
        // ---- UTF-8 valid ----
        U8("u8 ascii", new byte[] { 0x41, 0x42, 0x43 });
        U8("u8 empty", new byte[0]);
        U8("u8 2byte", new byte[] { 0xC3, 0xA9 });            // U+00E9
        U8("u8 3byte", new byte[] { 0xE2, 0x82, 0xAC });      // U+20AC
        U8("u8 4byte", new byte[] { 0xF0, 0x9F, 0x98, 0x80 });// U+1F600 (surrogate pair)
        U8("u8 mixed", new byte[] { 0x48, 0xC3, 0xA9, 0x21 });// Hé!
        U8("u8 d7ff", new byte[] { 0xED, 0x9F, 0xBF });       // valid U+D7FF
        U8("u8 e000", new byte[] { 0xEE, 0x80, 0x80 });       // valid U+E000

        // ---- UTF-8 malformed -> U+FFFD (maximal-subpart replacement) ----
        U8("u8 lone-cont", new byte[] { 0x80 });
        U8("u8 lone-cont2", new byte[] { 0x80, 0x80 });
        U8("u8 trunc2", new byte[] { 0xC3 });
        U8("u8 trunc3", new byte[] { 0xE2, 0x82 });
        U8("u8 bad+ascii", new byte[] { 0x80, 0x41 });
        U8("u8 overlong-c0", new byte[] { 0xC0, 0x80 });
        U8("u8 overlong-e0", new byte[] { 0xE0, 0x9F, 0x80 });
        U8("u8 overlong-f0", new byte[] { 0xF0, 0x8F, 0x80, 0x80 });
        U8("u8 c2+ascii", new byte[] { 0xC2, 0x41 });
        U8("u8 e2+ascii", new byte[] { 0xE2, 0x41, 0x42 });
        U8("u8 e2-82+ascii", new byte[] { 0xE2, 0x82, 0x41 });
        U8("u8 f0+ascii", new byte[] { 0xF0, 0x41 });
        U8("u8 f0-90+ascii", new byte[] { 0xF0, 0x90, 0x41 });
        U8("u8 ff", new byte[] { 0xFF });
        U8("u8 fe", new byte[] { 0xFE });
        U8("u8 surr-ed", new byte[] { 0xED, 0xA0, 0x80 });    // encodes U+D800 -> 3x FFFD
        U8("u8 surr-dfff", new byte[] { 0xED, 0xBF, 0xBF });  // encodes U+DFFF -> 3x FFFD
        U8("u8 over-range", new byte[] { 0xF4, 0x90, 0x80, 0x80 }); // > U+10FFFF
        U8("u8 f5", new byte[] { 0xF5, 0x80, 0x80, 0x80 });
        U8("u8 f4-90", new byte[] { 0xF4, 0x90 });            // bad 2nd byte then trunc
        U8("u8 c0-only", new byte[] { 0xC0 });

        // ---- UTF-8 GetString(byte[], index, count) ----
        byte[] big = new byte[] { 0x58, 0x48, 0xC3, 0xA9, 0x21, 0x59 };
        U8R("u8 range(1,4)", big, 1, 4);
        U8R("u8 range(0,len)", big, 0, big.Length);
        U8R("u8 range(2,0)", big, 2, 0);                      // empty range
        U8R("u8 range(len,0)", big, big.Length, 0);           // index == len, count 0 (valid)

        // ---- UTF-8 GetString(ReadOnlySpan<byte>) (the STJ key-decode overload) ----
        U8Span("u8 span ascii", big, 0, big.Length);
        U8Span("u8 span mid", big, 1, 4);                     // "Hé!"
        U8Span("u8 span empty", big, 2, 0);
        U8Span("u8 span bad", new byte[] { 0x80, 0x41 }, 0, 2);

        // ---- UTF-8 GetString(byte*, int) (the SRM metadata decode path) ----
        U8Ptr("u8ptr ascii", new byte[] { 0x41, 0x42, 0x43 }, 3);
        U8Ptr("u8ptr mixed", new byte[] { 0x48, 0xC3, 0xA9, 0x21 }, 4);
        U8Ptr("u8ptr partial", new byte[] { 0x41, 0x42, 0x43, 0x44 }, 2);
        U8Ptr("u8ptr bad", new byte[] { 0x80, 0x41 }, 2);

        // ---- UTF-16 (Unicode = little-endian) ----
        U16("u16 ascii", new byte[] { 0x41, 0x00, 0x42, 0x00 });
        U16("u16 empty", new byte[0]);
        U16("u16 e9", new byte[] { 0xE9, 0x00 });             // U+00E9
        U16("u16 euro", new byte[] { 0xAC, 0x20 });           // U+20AC
        U16("u16 surr-pair", new byte[] { 0x3D, 0xD8, 0x00, 0xDE }); // U+1F600
        U16("u16 odd-len", new byte[] { 0x41, 0x00, 0x42 });  // trailing odd byte -> FFFD
        U16("u16 lone-high", new byte[] { 0x00, 0xD8 });
        U16("u16 lone-low", new byte[] { 0x00, 0xDC });
        U16("u16 high-no-low", new byte[] { 0x00, 0xD8, 0x41, 0x00 }); // high then non-low

        // ---- argument validation (exact exception types) ----
        try { Encoding.UTF8.GetString((byte[])null); Console.WriteLine("null1: ok"); }
        catch (ArgumentNullException) { Console.WriteLine("null1: ArgumentNull"); }
        try { Encoding.UTF8.GetString((byte[])null, 0, 0); Console.WriteLine("null3: ok"); }
        catch (ArgumentNullException) { Console.WriteLine("null3: ArgumentNull"); }
        try { Encoding.UTF8.GetString(big, -1, 1); Console.WriteLine("neg-idx: ok"); }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("neg-idx: ArgumentOutOfRange"); }
        try { Encoding.UTF8.GetString(big, 0, -1); Console.WriteLine("neg-cnt: ok"); }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("neg-cnt: ArgumentOutOfRange"); }
        try { Encoding.UTF8.GetString(big, 2, 5); Console.WriteLine("over-cnt: ok"); }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("over-cnt: ArgumentOutOfRange"); }
        try { Encoding.Unicode.GetString((byte[])null); Console.WriteLine("u16-null: ok"); }
        catch (ArgumentNullException) { Console.WriteLine("u16-null: ArgumentNull"); }
    }
}
