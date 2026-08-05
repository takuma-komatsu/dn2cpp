#nullable disable
using System;
using System.Globalization;

namespace NumberStylesSubset
{
    // NumberStyles-honoring Parse/TryParse across all eight integer widths plus
    // decimal: hex/binary specifiers, thousands separators (invariant, de-DE,
    // fr-FR), parentheses, leading/trailing signs, decimal points and exponents
    // on integers, span inputs, invalid style combinations, and the TryParse-
    // false vs Parse-exception-type discrimination. The sub-word types
    // (byte/sbyte/short/ushort) also prove the plain Parse/TryParse intrinsic
    // cut-over. Only the modeled cultures are used.
    internal static class Program
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static void Show<T>(string tag, Func<T> f)
        {
            try { Console.WriteLine(tag + " = " + f()); }
            catch (Exception ex) { Console.WriteLine(tag + " ! " + ex.GetType().Name); }
        }

        private static T ParseOf<T>(string s, IFormatProvider p) where T : IParsable<T>
        {
            return T.Parse(s, p);
        }

        private static T ParseSpanOf<T>(ReadOnlySpan<char> s, IFormatProvider p) where T : ISpanParsable<T>
        {
            return T.Parse(s, p);
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("-- hex, all widths --");
            Console.WriteLine(byte.Parse("FF", NumberStyles.HexNumber, Inv));       // 255
            Console.WriteLine(sbyte.Parse("FF", NumberStyles.HexNumber, Inv));      // -1
            Console.WriteLine(sbyte.Parse("7f", NumberStyles.HexNumber, Inv));      // 127
            Console.WriteLine(short.Parse("FFFF", NumberStyles.HexNumber, Inv));    // -1
            Console.WriteLine(ushort.Parse("ffff", NumberStyles.HexNumber, Inv));   // 65535
            Console.WriteLine(int.Parse("FFFFFFFF", NumberStyles.HexNumber, Inv));  // -1
            Console.WriteLine(uint.Parse("FFFFFFFF", NumberStyles.HexNumber, Inv)); // 4294967295
            Console.WriteLine(long.Parse("FFFFFFFFFFFFFFFF", NumberStyles.HexNumber, Inv));  // -1
            Console.WriteLine(ulong.Parse("FFFFFFFFFFFFFFFF", NumberStyles.HexNumber, Inv)); // 18446744073709551615
            Console.WriteLine(byte.Parse("00FF", NumberStyles.HexNumber, Inv));     // 255 (leading zeros)
            Console.WriteLine(byte.Parse("000000000000ff", NumberStyles.HexNumber, Inv)); // 255
            Console.WriteLine(int.Parse("1f", NumberStyles.AllowHexSpecifier, Inv)); // 31 (bare specifier)
            Console.WriteLine(byte.Parse(" FF ", NumberStyles.HexNumber, Inv));     // 255 (white ok in HexNumber)
            Show("byte-hex[1FF]", () => byte.Parse("1FF", NumberStyles.HexNumber, Inv));       // OverflowException
            Show("ushort-hex[10000]", () => ushort.Parse("10000", NumberStyles.HexNumber, Inv)); // OverflowException
            Show("ulong-hex[10000000000000000]", () => ulong.Parse("10000000000000000", NumberStyles.HexNumber, Inv)); // OverflowException
            Show("byte-hex[0x1F]", () => byte.Parse("0x1F", NumberStyles.HexNumber, Inv));     // FormatException
            Show("byte-hex[]", () => byte.Parse("", NumberStyles.HexNumber, Inv));             // FormatException
            Show("byte-hex[+FF]", () => byte.Parse("+FF", NumberStyles.HexNumber, Inv));       // FormatException

            Console.WriteLine("-- binary, all widths --");
            Console.WriteLine(byte.Parse("1111", NumberStyles.BinaryNumber, Inv));      // 15
            Console.WriteLine(byte.Parse("11111111", NumberStyles.BinaryNumber, Inv));  // 255
            Console.WriteLine(sbyte.Parse("11111111", NumberStyles.BinaryNumber, Inv)); // -1
            Console.WriteLine(byte.Parse("011111111", NumberStyles.BinaryNumber, Inv)); // 255 (leading zero)
            Console.WriteLine(short.Parse("1000000000000000", NumberStyles.BinaryNumber, Inv));  // -32768
            Console.WriteLine(ushort.Parse("1000000000000000", NumberStyles.BinaryNumber, Inv)); // 32768
            Console.WriteLine(uint.Parse("11111111111111111111111111111111", NumberStyles.BinaryNumber, Inv)); // 4294967295
            Console.WriteLine(int.Parse("10000000000000000000000000000000", NumberStyles.BinaryNumber, Inv));  // -2147483648
            Console.WriteLine(long.Parse("1111111111111111111111111111111111111111111111111111111111111111", NumberStyles.BinaryNumber, Inv));  // -1
            Console.WriteLine(ulong.Parse("1111111111111111111111111111111111111111111111111111111111111111", NumberStyles.BinaryNumber, Inv)); // 18446744073709551615
            Show("byte-bin[100000000]", () => byte.Parse("100000000", NumberStyles.BinaryNumber, Inv)); // OverflowException (9 bits)
            Show("ulong-bin[65bits]", () => ulong.Parse("10000000000000000000000000000000000000000000000000000000000000000", NumberStyles.BinaryNumber, Inv)); // OverflowException
            Show("byte-bin[2]", () => byte.Parse("2", NumberStyles.BinaryNumber, Inv)); // FormatException

            Console.WriteLine("-- invalid style combinations --");
            Show("hex+sign", () => int.Parse("FF", NumberStyles.HexNumber | NumberStyles.AllowLeadingSign, Inv));           // ArgumentException
            Show("hex+thousands", () => int.Parse("FF", NumberStyles.AllowHexSpecifier | NumberStyles.AllowThousands, Inv)); // ArgumentException
            Show("hex+parens", () => int.Parse("FF", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, Inv));  // ArgumentException
            Show("hex+binary", () => int.Parse("11", NumberStyles.AllowHexSpecifier | NumberStyles.AllowBinarySpecifier, Inv)); // ArgumentException
            Show("bin+sign", () => int.Parse("11", NumberStyles.BinaryNumber | NumberStyles.AllowLeadingSign, Inv));        // ArgumentException
            Show("undefined-bit", () => int.Parse("1", (NumberStyles)0x800, Inv));                                          // ArgumentException
            Show("tryparse-hex+sign", () => { int q; return int.TryParse("FF", NumberStyles.HexNumber | NumberStyles.AllowLeadingSign, Inv, out q); }); // ArgumentException (TryParse still validates)
            Show("tryparse-undefined-bit", () => { byte q; return byte.TryParse("1", (NumberStyles)0x800, Inv, out q); });  // ArgumentException

            Console.WriteLine("-- thousands separators --");
            var thou = NumberStyles.Integer | NumberStyles.AllowThousands;
            Console.WriteLine(int.Parse("1,234", thou, Inv));       // 1234
            Console.WriteLine(int.Parse("1,234,567", thou, Inv));   // 1234567
            Console.WriteLine(int.Parse("1,2,3", thou, Inv));       // 123 (.NET does not validate group sizes)
            Console.WriteLine(int.Parse("1,,2", thou, Inv));        // 12 (adjacent separators allowed)
            Console.WriteLine(int.Parse("123,", thou, Inv));        // 123 (trailing separator allowed)
            Console.WriteLine(int.Parse("-1,234", thou, Inv));      // -1234
            Console.WriteLine(ulong.Parse("18,446,744,073,709,551,615", thou, Inv)); // max
            Show("int[,123]", () => int.Parse(",123", thou, Inv));  // FormatException (separator before any digit)
            Show("int[1, 234]", () => int.Parse("1, 234", thou, Inv)); // FormatException
            Show("int[1,234 no-thousands]", () => int.Parse("1,234", NumberStyles.Integer, Inv)); // FormatException
            Console.WriteLine(int.Parse("1.234", thou, CultureInfo.GetCultureInfo("de-DE")));     // 1234 (de-DE group '.')
            Console.WriteLine(int.Parse("1.234.567", thou, new CultureInfo("de-DE")));            // 1234567
            Console.WriteLine(int.Parse("1 234", thou, CultureInfo.GetCultureInfo("fr-FR"))); // 1234 (fr-FR narrow nbsp)
            Console.WriteLine(int.Parse("1 234", thou, CultureInfo.GetCultureInfo("fr-FR")));      // 1234 (plain space substitutes)
            Show("fr[1nbsp234]", () => int.Parse("1 234", thou, CultureInfo.GetCultureInfo("fr-FR"))); // FormatException (nbsp is not the fr separator)

            Console.WriteLine("-- parentheses --");
            var par = NumberStyles.Integer | NumberStyles.AllowParentheses;
            Console.WriteLine(int.Parse("(123)", par, Inv));    // -123
            Console.WriteLine(int.Parse(" (123) ", par, Inv));  // -123
            Console.WriteLine(int.Parse("(0)", par, Inv));      // 0
            Console.WriteLine(long.Parse("(9223372036854775808)", par, Inv)); // long.MinValue
            Show("int[(123]", () => int.Parse("(123", par, Inv));     // FormatException
            Show("int[123)]", () => int.Parse("123)", par, Inv));     // FormatException
            Show("int[(-123)]", () => int.Parse("(-123)", par, Inv)); // FormatException
            Show("int[( 123 )]", () => int.Parse("( 123 )", par, Inv)); // FormatException (no white inside)
            Show("int[-(123)]", () => int.Parse("-(123)", par, Inv)); // FormatException
            Show("uint[(5)]", () => uint.Parse("(5)", par, Inv));     // OverflowException (negative unsigned)
            Console.WriteLine(uint.Parse("(0)", par, Inv));           // 0

            Console.WriteLine("-- leading/trailing signs --");
            var tsign = NumberStyles.Integer | NumberStyles.AllowTrailingSign;
            Console.WriteLine(int.Parse("123-", tsign, Inv));   // -123
            Console.WriteLine(int.Parse("123+", tsign, Inv));   // 123
            Console.WriteLine(int.Parse("123 -", tsign, Inv));  // -123 (white before trailing sign)
            Show("int[+123-]", () => int.Parse("+123-", tsign, Inv)); // FormatException (both signs)
            Show("int[- 123]", () => int.Parse("- 123", NumberStyles.Integer, Inv)); // FormatException (white after sign)
            Show("int[ 5 None]", () => int.Parse(" 5", NumberStyles.None, Inv));     // FormatException
            Show("int[+5 None]", () => int.Parse("+5", NumberStyles.None, Inv));     // FormatException
            Console.WriteLine(int.Parse("5", NumberStyles.None, Inv));               // 5
            Console.WriteLine(int.Parse("\t 5 \t", NumberStyles.Integer, Inv));      // 5 (tab is parser whitespace)

            Console.WriteLine("-- decimal point on integers --");
            var idp = NumberStyles.Integer | NumberStyles.AllowDecimalPoint;
            Console.WriteLine(int.Parse("123.000", idp, Inv));  // 123
            Console.WriteLine(int.Parse("120.00", idp, Inv));   // 120
            Console.WriteLine(int.Parse("123.", idp, Inv));     // 123
            Console.WriteLine(int.Parse(".0", idp, Inv));       // 0
            Console.WriteLine(int.Parse("0.000", idp, Inv));    // 0
            Show("int[123.5]", () => int.Parse("123.5", idp, Inv));  // OverflowException (non-zero fraction)
            Show("int[.5]", () => int.Parse(".5", idp, Inv));        // OverflowException
            Show("int[.]", () => int.Parse(".", idp, Inv));          // FormatException (no digits)
            Show("ulong[-0.00]", () => ulong.Parse("-0.00", idp, Inv)); // OverflowException (negative-with-decimal unsigned)
            Console.WriteLine(ulong.Parse("-0", NumberStyles.Integer, Inv)); // 0
            Console.WriteLine(byte.Parse("255.000", idp, Inv));  // 255
            Console.WriteLine(int.Parse("1.234,00", thou | NumberStyles.AllowDecimalPoint, new CultureInfo("de-DE"))); // 1234

            Console.WriteLine("-- exponent on integers --");
            var iex = NumberStyles.Integer | NumberStyles.AllowExponent;
            Console.WriteLine(int.Parse("1e2", iex, Inv));    // 100
            Console.WriteLine(int.Parse("1E2", iex, Inv));    // 100
            Console.WriteLine(int.Parse("1e+3", iex, Inv));   // 1000
            Console.WriteLine(int.Parse("12e0", iex, Inv));   // 12
            Console.WriteLine(int.Parse("10e-1", iex, Inv));  // 1 (trailing zero cancels)
            Console.WriteLine(long.Parse("1e10", iex, Inv));  // 10000000000
            Show("int[1e10]", () => int.Parse("1e10", iex, Inv));   // OverflowException
            Show("int[1e-1]", () => int.Parse("1e-1", iex, Inv));   // OverflowException (fraction)
            Show("int[1e]", () => int.Parse("1e", iex, Inv));       // FormatException

            Console.WriteLine("-- sub-word plain Parse/TryParse (intrinsic cut-over) --");
            Console.WriteLine(byte.Parse("200"));      // 200
            Console.WriteLine(sbyte.Parse("-128"));    // -128
            Console.WriteLine(short.Parse("-32768"));  // -32768
            Console.WriteLine(ushort.Parse("65535"));  // 65535
            Console.WriteLine(byte.Parse("  7  "));    // 7
            Console.WriteLine(sbyte.Parse("+100"));    // 100
            Show("byte[256]", () => byte.Parse("256"));       // OverflowException
            Show("byte[-1]", () => byte.Parse("-1"));         // OverflowException
            Show("byte[zz]", () => byte.Parse("zz"));         // FormatException
            Show("byte[null]", () => byte.Parse((string)null)); // ArgumentNullException
            Show("sbyte[-129]", () => sbyte.Parse("-129"));   // OverflowException
            Show("short[32768]", () => short.Parse("32768")); // OverflowException
            Show("ushort[-1]", () => ushort.Parse("-1"));     // OverflowException
            Show("ushort[65536]", () => ushort.Parse("65536")); // OverflowException
            byte b8; sbyte s8; short s16; ushort u16;
            Console.WriteLine(byte.TryParse("255", out b8) + " " + b8);   // True 255
            Console.WriteLine(byte.TryParse("256", out b8) + " " + b8);   // False 0
            Console.WriteLine(sbyte.TryParse("-100", out s8) + " " + s8); // True -100
            Console.WriteLine(sbyte.TryParse((string)null, out s8) + " " + s8); // False 0
            Console.WriteLine(short.TryParse("-999", out s16) + " " + s16); // True -999
            Console.WriteLine(short.TryParse("xyz", out s16) + " " + s16);  // False 0
            Console.WriteLine(ushort.TryParse("60000", out u16) + " " + u16); // True 60000
            Console.WriteLine(ushort.TryParse("70000", out u16) + " " + u16); // False 0
            Console.WriteLine(byte.Parse("ff", NumberStyles.HexNumber));  // 255 (styles w/o provider)
            Console.WriteLine(short.Parse("100", NumberStyles.Integer));  // 100
            Console.WriteLine(byte.Parse("77", Inv));                     // 77 (provider only)
            Console.WriteLine(ushort.Parse("1,000", thou, Inv));          // 1000

            Console.WriteLine("-- span inputs --");
            Console.WriteLine(int.Parse("2,001".AsSpan(), thou, Inv));    // 2001
            Console.WriteLine(long.Parse("-42".AsSpan(), NumberStyles.Integer, Inv)); // -42
            Console.WriteLine(byte.Parse("FE".AsSpan(), NumberStyles.HexNumber, Inv)); // 254
            Console.WriteLine(ushort.Parse("321".AsSpan(), NumberStyles.Integer, Inv)); // 321
            int spanOut;
            Console.WriteLine(int.TryParse("777".AsSpan(), out spanOut) + " " + spanOut);  // True 777
            uint spanU;
            Console.WriteLine(uint.TryParse("88".AsSpan(), NumberStyles.None, Inv, out spanU) + " " + spanU); // True 88
            Console.WriteLine(uint.TryParse(" 88".AsSpan(), NumberStyles.None, Inv, out spanU) + " " + spanU); // False 0
            byte spanB;
            Console.WriteLine(byte.TryParse("31".AsSpan(), out spanB) + " " + spanB); // True 31

            Console.WriteLine("-- TryParse-false vs Parse-exception pairs --");
            int ti;
            Console.WriteLine(int.TryParse("2147483648", out ti) + " " + ti); // False 0
            Show("int[2147483648]", () => int.Parse("2147483648"));           // OverflowException
            Console.WriteLine(int.TryParse("abc", out ti) + " " + ti);        // False 0
            Show("int[abc]", () => int.Parse("abc"));                         // FormatException
            byte tb;
            Console.WriteLine(byte.TryParse("1FF", NumberStyles.HexNumber, Inv, out tb) + " " + tb); // False 0
            Show("byte-hex-parse[1FF]", () => byte.Parse("1FF", NumberStyles.HexNumber, Inv));       // OverflowException

            Console.WriteLine("-- generic T.Parse (IParsable/ISpanParsable) --");
            Console.WriteLine(ParseOf<int>("123", Inv));            // 123
            Console.WriteLine(ParseOf<int>("-77", Inv));            // -77
            Console.WriteLine(ParseSpanOf<int>("456".AsSpan(), Inv)); // 456

            Console.WriteLine("-- decimal NumberStyles --");
            Console.WriteLine(decimal.Parse("1.100", NumberStyles.Number, Inv));   // 1.100 (scale preserved)
            // Provider-less ON PURPOSE: the one-argument overload's implied
            // NumberStyles.Number is the subject, which the two-argument forms cannot
            // show. The driver's invariant pin makes the CurrentCulture read behind it
            // deterministic.
            Console.WriteLine(decimal.Parse("1,234.5"));                            // 1234.5 (Number default has thousands)
            Console.WriteLine(decimal.Parse("1e2", NumberStyles.Float, Inv));       // 100
            Console.WriteLine(decimal.Parse("1.5e1", NumberStyles.Float, Inv));     // 15
            Console.WriteLine(decimal.Parse("1.10e1", NumberStyles.Float, Inv));    // 11.0
            Console.WriteLine(decimal.Parse("1.5e-3", NumberStyles.Float, Inv));    // 0.0015
            Console.WriteLine(decimal.Parse("(1.5)", NumberStyles.Number | NumberStyles.AllowParentheses, Inv)); // -1.5
            Console.WriteLine(decimal.Parse("1.234,56", NumberStyles.Number, new CultureInfo("de-DE"))); // 1234.56
            Console.WriteLine(decimal.Parse("2.5".AsSpan(), NumberStyles.Number, Inv)); // 2.5
            Console.WriteLine(decimal.Parse("3.25", Inv));                          // 3.25 (provider only)
            Show("dec[1e2 Number]", () => decimal.Parse("1e2", NumberStyles.Number, Inv)); // FormatException
            Show("dec[hex]", () => decimal.Parse("FF", NumberStyles.HexNumber, Inv));      // ArgumentException
            Show("dec[abc]", () => decimal.Parse("abc"));                                  // FormatException
            decimal td;
            Console.WriteLine(decimal.TryParse("abc", out td) + " " + td);                 // False 0
            Console.WriteLine(decimal.TryParse("12.34", NumberStyles.Number, Inv, out td) + " " + td); // True 12.34
            Console.WriteLine(decimal.TryParse("79228162514264337593543950336", out td) + " " + td);   // False 0 (overflow)
            Show("dec[max+1]", () => decimal.Parse("79228162514264337593543950336"));      // OverflowException
            Console.WriteLine(decimal.Parse("79228162514264337593543950335"));             // decimal.MaxValue

            // NumberStyles.Any / .Currency carry AllowCurrencySymbol, which .NET
            // accepts (it permits, not requires, the symbol). Under the invariant
            // culture — the one the runtime setting / environment readers use —
            // the symbol is U+00A4 and never appears, so plain decimal text parses
            // exactly as .NET. A null input returns false, not an exception. (These
            // styles used to abort the process before ever looking at the input.)
            Console.WriteLine("-- NumberStyles.Any / Currency (currency-less input) --");
            var any = NumberStyles.Any;
            int va;
            Console.WriteLine(int.TryParse((string)null, any, Inv, out va) + " " + va);   // False 0 (no exception, no abort)
            Console.WriteLine(int.TryParse("42", any, Inv, out va) + " " + va);           // True 42
            Console.WriteLine(int.TryParse("-42", any, Inv, out va) + " " + va);          // True -42
            Console.WriteLine(int.TryParse(" 1,234 ", any, Inv, out va) + " " + va);      // True 1234
            Console.WriteLine(int.TryParse("(5)", any, Inv, out va) + " " + va);          // True -5
            Console.WriteLine(int.TryParse("", any, Inv, out va) + " " + va);             // False 0
            Console.WriteLine(int.TryParse("abc", any, Inv, out va) + " " + va);          // False 0
            Console.WriteLine(int.TryParse("$1,234", any, Inv, out va) + " " + va);       // False 0 ('$' is not the invariant symbol '¤')
            Console.WriteLine(int.Parse("1234", any, Inv));                               // 1234
            Console.WriteLine(int.Parse("1.5e3", any, Inv));                              // 1500 (Any carries decimal+exponent)
            Console.WriteLine(int.Parse("12.000", any, Inv));                             // 12
            long vl;
            Console.WriteLine(long.TryParse("9,876,543,210", any, Inv, out vl) + " " + vl); // True 9876543210
            uint vu;
            Console.WriteLine(uint.TryParse("(5)", any, Inv, out vu) + " " + vu);         // False 0 (negative unsigned)
            Console.WriteLine(int.Parse("42", NumberStyles.Currency, Inv));               // 42
            Console.WriteLine(decimal.Parse("42", NumberStyles.Currency, Inv));           // 42
            Console.WriteLine(double.Parse("1.5", any, Inv));                             // 1.5
            Console.WriteLine(decimal.Parse("1234.56", any, Inv));                        // 1234.56
            Show("int-null-parse-Any", () => int.Parse((string)null, any, Inv));          // ArgumentNullException
            Show("uint-neg-Any", () => uint.Parse("-1", any, Inv));                       // OverflowException
            Show("hex+currency", () => int.Parse("FF", NumberStyles.HexNumber | NumberStyles.AllowCurrencySymbol, Inv)); // ArgumentException

            // Cross-assembly enum ToString (UriKind is in System.Private.Uri): a
            // named value formats to its name, an undefined value to its number.
            Console.WriteLine("-- cross-assembly enum ToString --");
            Console.WriteLine(UriKind.Absolute.ToString());           // Absolute
            Console.WriteLine(UriKind.Relative.ToString());           // Relative
            Console.WriteLine(((UriKind)42).ToString());              // 42
            Console.WriteLine(UriKind.RelativeOrAbsolute.ToString()); // RelativeOrAbsolute
            Console.WriteLine("k=" + UriKind.Absolute);               // k=Absolute
        }
    }
}
