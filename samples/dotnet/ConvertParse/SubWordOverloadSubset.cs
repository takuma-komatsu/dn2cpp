#nullable disable
using System;
using System.Globalization;

namespace SubWordOverloadSubset
{
    // The numeric-primitive OVERLOADS the inline lowering did not model. Byte/SByte/Int16/
    // UInt16 are not intrinsic types, so their ToString/Parse/TryParse/TryFormat are cut
    // from reachability member-by-member and lowered inline instead — the real bodies drag
    // in the whole System.Number.Format*/ParseBinaryInteger subtree, which is the biggest
    // remaining self-host cascade. That cut is routed by NAME, so an overload the emit table
    // did not model had nowhere to fall back to: it was a hard transpile failure.
    //
    // Unlike Path/File, the remedy is NOT to let these fall through — the fall-through IS
    // the cascade the cut exists to avoid. It is to model the whole surface. .NET 10 gives
    // all eight integer widths the identical 23-member shape (4 ToString, 8 Parse, 9
    // TryParse, 2 TryFormat); these were the two families missing.
    //
    // Everything here is invariant-cultured and diffs exact against real .NET.
    internal static class Program
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static void Show<T>(string tag, Func<T> f)
        {
            try { Console.WriteLine(tag + " = " + f()); }
            catch (Exception ex) { Console.WriteLine(tag + " ! " + ex.GetType().Name); }
        }

        internal static void __GateEntry()
        {
            // ---- ToString(format, provider): the 4th ToString overload ----------------
            // The (format) and (provider) arms both existed, so the PAIR of them did not —
            // and `b.ToString("X2", CultureInfo.InvariantCulture)` is an ordinary way to
            // write it. The width matters: a hex mask must cover exactly the type's own
            // two's-complement pattern, not int32's.
            Console.WriteLine("-- ToString(format, provider) --");
            Console.WriteLine(((byte)200).ToString("D", Inv));       // 200
            Console.WriteLine(((byte)200).ToString("X2", Inv));      // C8
            Console.WriteLine(((byte)5).ToString("D3", Inv));        // 005
            Console.WriteLine(((byte)255).ToString("x", Inv));       // ff
            Console.WriteLine(((sbyte)-1).ToString("X2", Inv));      // FF   (NOT FFFFFFFF)
            Console.WriteLine(((sbyte)-1).ToString("D", Inv));       // -1
            Console.WriteLine(((sbyte)-120).ToString("D5", Inv));    // -00120
            Console.WriteLine(((short)-1).ToString("x2", Inv));      // ffff (NOT ffffffff)
            Console.WriteLine(((short)-32000).ToString("D", Inv));   // -32000
            Console.WriteLine(((short)4660).ToString("X4", Inv));    // 1234
            Console.WriteLine(((ushort)65000).ToString("D", Inv));   // 65000
            Console.WriteLine(((ushort)65535).ToString("X", Inv));   // FFFF
            Console.WriteLine(((ushort)4096).ToString("x8", Inv));   // 00001000
            // Culture-sensitive standard formats through the provider.
            Console.WriteLine(((byte)200).ToString("N2", Inv));      // 200.00
            Console.WriteLine(((short)-32000).ToString("N0", Inv));  // -32,000
            Console.WriteLine(((ushort)65000).ToString("N1", Inv));  // 65,000.0
            Console.WriteLine(((sbyte)-12).ToString("N2", Inv));     // -12.00
            // A null provider means invariant, and must agree with the explicit one.
            Console.WriteLine("nullProvAgrees="
                + (((short)-1).ToString("x2", null) == ((short)-1).ToString("x2", Inv)));
            // And the (format, provider) lane must agree with the (format) lane it sits
            // beside — they are two overloads of one operation, and a divergence between
            // them would be silent.
            Console.WriteLine("fmtLanesAgree="
                + (((byte)200).ToString("X2", Inv) == ((byte)200).ToString("X2")
                   && ((sbyte)-1).ToString("D", Inv) == ((sbyte)-1).ToString("D")
                   && ((short)-1).ToString("x2", Inv) == ((short)-1).ToString("x2")
                   && ((ushort)65535).ToString("X", Inv) == ((ushort)65535).ToString("X")));

            // ---- Parse/TryParse(ReadOnlySpan<byte>): the IUtf8SpanParsable forms -------
            // A `"..."u8` literal is a ReadOnlySpan<byte> of UTF-8, and .NET 10 binds it to
            // a real overload on every one of these types. It is the same operation over a
            // different encoding of the same text, so the UTF-8 lane and the string lane
            // must not be able to disagree — the runtime widens the bytes and runs the ONE
            // NumberStyles engine, rather than carrying a second scanner.
            Console.WriteLine("-- Parse(ReadOnlySpan<byte>), all eight widths --");
            Console.WriteLine(byte.Parse("200"u8));                       // 200
            Console.WriteLine(sbyte.Parse("-120"u8));                     // -120
            Console.WriteLine(short.Parse("-32000"u8));                   // -32000
            Console.WriteLine(ushort.Parse("65000"u8));                   // 65000
            Console.WriteLine(int.Parse("-2000000000"u8));                // -2000000000
            Console.WriteLine(uint.Parse("4000000000"u8));                // 4000000000
            Console.WriteLine(long.Parse("-9000000000000000000"u8));      // -9000000000000000000
            Console.WriteLine(ulong.Parse("18000000000000000000"u8));     // 18000000000000000000
            // Surrounding whitespace and a leading sign, like the string lane.
            Console.WriteLine(int.Parse("  -7  "u8));                     // -7
            Console.WriteLine(byte.Parse("+42"u8));                       // 42

            Console.WriteLine("-- Parse(ReadOnlySpan<byte>, provider / styles) --");
            Console.WriteLine(byte.Parse("200"u8, Inv));                          // 200
            Console.WriteLine(short.Parse("-32000"u8, Inv));                      // -32000
            Console.WriteLine(byte.Parse("FF"u8, NumberStyles.HexNumber, Inv));   // 255
            Console.WriteLine(sbyte.Parse("FF"u8, NumberStyles.HexNumber, Inv));  // -1
            Console.WriteLine(short.Parse("FFFF"u8, NumberStyles.HexNumber, Inv));// -1
            Console.WriteLine(ushort.Parse("ffff"u8, NumberStyles.HexNumber, Inv));// 65535
            Console.WriteLine(int.Parse("1,234"u8, NumberStyles.Integer | NumberStyles.AllowThousands, Inv)); // 1234
            Console.WriteLine(long.Parse("(45)"u8, NumberStyles.Integer | NumberStyles.AllowParentheses, Inv)); // -45

            // The two lanes are the same operation: prove it, don't assume it.
            Console.WriteLine("u8VsStringAgree="
                + (byte.Parse("200"u8) == byte.Parse("200")
                   && sbyte.Parse("-120"u8) == sbyte.Parse("-120")
                   && short.Parse("-32000"u8) == short.Parse("-32000")
                   && ushort.Parse("65000"u8) == ushort.Parse("65000")
                   && int.Parse("  -7  "u8) == int.Parse("  -7  ")
                   && ulong.Parse("18000000000000000000"u8) == ulong.Parse("18000000000000000000")
                   && byte.Parse("FF"u8, NumberStyles.HexNumber, Inv)
                      == byte.Parse("FF", NumberStyles.HexNumber, Inv)));

            Console.WriteLine("-- TryParse(ReadOnlySpan<byte>) --");
            byte b;
            Console.WriteLine(byte.TryParse("77"u8, out b) + " " + b);            // True 77
            Console.WriteLine(byte.TryParse("300"u8, out b) + " " + b);           // False 0 (overflow)
            Console.WriteLine(byte.TryParse("zz"u8, out b) + " " + b);            // False 0
            Console.WriteLine(byte.TryParse(""u8, out b) + " " + b);              // False 0
            sbyte sb;
            Console.WriteLine(sbyte.TryParse("-128"u8, out sb) + " " + sb);       // True -128
            Console.WriteLine(sbyte.TryParse("128"u8, out sb) + " " + sb);        // False 0 (overflow)
            short sh;
            Console.WriteLine(short.TryParse("-32768"u8, out sh) + " " + sh);     // True -32768
            Console.WriteLine(short.TryParse("32768"u8, out sh) + " " + sh);      // False 0 (overflow)
            ushort us;
            Console.WriteLine(ushort.TryParse("65535"u8, out us) + " " + us);     // True 65535
            Console.WriteLine(ushort.TryParse("-1"u8, out us) + " " + us);        // False 0
            int i32;
            Console.WriteLine(int.TryParse("99"u8, Inv, out i32) + " " + i32);    // True 99
            Console.WriteLine(int.TryParse("7F"u8, NumberStyles.HexNumber, Inv, out i32) + " " + i32); // True 127
            long i64;
            Console.WriteLine(long.TryParse("-5000000000"u8, out i64) + " " + i64); // True -5000000000
            ulong u64;
            Console.WriteLine(ulong.TryParse("18446744073709551615"u8, out u64) + " " + u64); // True (UInt64.MaxValue)

            // Parse throws where TryParse answers false, and with the same discrimination
            // the string lane makes: format vs overflow.
            Show("u8ByteOverflow", () => byte.Parse("300"u8));                    // ! OverflowException
            Show("u8ByteFormat", () => byte.Parse("zz"u8));                       // ! FormatException
            Show("u8ShortOverflow", () => short.Parse("40000"u8));                // ! OverflowException

            // ---- UTF-8 that is not ASCII ----------------------------------------------
            // The bytes are UTF-8 text, not a digit array. Malformed UTF-8 decodes to
            // replacement chars (which are not digits and not any NumberFormatInfo symbol),
            // and non-ASCII digits are not digits either — both fail, exactly as .NET's own
            // UTF-8 parser does with the same bytes.
            Console.WriteLine("-- non-ASCII UTF-8 input --");
            byte bad;
            // C0 80 — an overlong NUL, the classic ill-formed sequence.
            Console.WriteLine(byte.TryParse(new byte[] { 0xC0, 0x80 }, out bad) + " " + bad); // False 0
            // A lone continuation byte.
            Console.WriteLine(byte.TryParse(new byte[] { 0x80 }, out bad) + " " + bad);       // False 0
            // A truncated 3-byte sequence followed by an ASCII digit.
            Console.WriteLine(byte.TryParse(new byte[] { 0xE2, 0x34 }, out bad) + " " + bad); // False 0
            // Fullwidth digits (U+FF11 U+FF12 U+FF13) — well-formed UTF-8, not ASCII digits.
            Console.WriteLine(byte.TryParse("１２３"u8, out bad) + " " + bad);                // False 0
            Show("u8Malformed", () => byte.Parse(new byte[] { 0xFF, 0xFE }));                // ! FormatException

            // ---- the float widths ride the same builder --------------------------------
            // Double/Single share the parse builder with the integers, so the UTF-8 input
            // form arrived for them at the same moment. A `double.Parse(u8)` that threw
            // while `byte.Parse(u8)` worked would be exactly the kind of split the one-
            // engine rule exists to prevent.
            Console.WriteLine("-- Parse(ReadOnlySpan<byte>): Double/Single --");
            Console.WriteLine(double.Parse("3.5"u8, Inv));                        // 3.5
            Console.WriteLine(double.Parse("-1.25e3"u8, Inv));                    // -1250
            Console.WriteLine(float.Parse("0.5"u8, Inv));                         // 0.5
            double d;
            Console.WriteLine(double.TryParse("2.75"u8, Inv, out d) + " " + d);   // True 2.75
            Console.WriteLine(double.TryParse("nope"u8, Inv, out d) + " " + d);   // False 0
            float f;
            Console.WriteLine(float.TryParse("-0.125"u8, out f) + " " + f);       // True -0.125
            Console.WriteLine("u8FloatVsStringAgree="
                + (double.Parse("3.5"u8, Inv) == double.Parse("3.5", Inv)
                   && float.Parse("0.5"u8, Inv) == float.Parse("0.5", Inv)));

            // ---- the out slot is read as a VALUE, not as an address --------------------
            // Every TryParse above hands its result straight to string concatenation,
            // which takes the local's ADDRESS (a constrained ToString) and so reads it
            // back at the storage width whatever the slot's upper bytes hold. An
            // arithmetic use is a plain `ldloc` of the int32-promoted slot instead, so
            // it is the only shape that can observe a narrow store that left those bytes
            // stale — and a negative sbyte/short is the only value where stale and
            // sign-extended differ. Nothing else in the corpus reads a sub-word TryParse
            // out this way.
            Console.WriteLine("-- TryParse out read arithmetically (sign extension) --");
            sbyte nsb;
            sbyte.TryParse("-100", NumberStyles.Integer, Inv, out nsb);
            Console.WriteLine("sbyte -100 as int = " + (nsb + 0));                // -100
            sbyte.TryParse("-128"u8, out nsb);
            Console.WriteLine("sbyte -128 as int = " + (nsb + 0));                // -128
            short nsh;
            short.TryParse("-30000", NumberStyles.Integer, Inv, out nsh);
            Console.WriteLine("short -30000 as int = " + (nsh + 0));              // -30000
            short.TryParse("-1"u8, out nsh);
            Console.WriteLine("short -1 as int = " + (nsh + 0));                  // -1
            // The unsigned widths are the control: a stale upper byte reads the same.
            byte nb;
            byte.TryParse("200", NumberStyles.Integer, Inv, out nb);
            Console.WriteLine("byte 200 as int = " + (nb + 0));                   // 200
            ushort nus;
            ushort.TryParse("60000", NumberStyles.Integer, Inv, out nus);
            Console.WriteLine("ushort 60000 as int = " + (nus + 0));              // 60000
        }
    }
}
