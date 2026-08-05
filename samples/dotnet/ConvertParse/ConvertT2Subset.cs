#nullable disable
using System;
using System.Buffers;
using System.Globalization;

namespace ConvertT2Subset
{
    // T2 gap fills: Convert.ToString(value, IFormatProvider) invariant overloads,
    // Convert.DefaultToType via IConvertible.ToType, the TryDecode-style
    // Convert.FromHexString(ReadOnlySpan<char>, Span<byte>, out, out), Boolean.Parse,
    // and the NumberFormatInfo PerMilleSymbol getter + the validate-and-trap
    // CurrencyDecimalSeparator setter. All diffed exactly vs real .NET (invariant).
    internal static class Program
    {
        internal static void __GateEntry()
        {
            var ci = CultureInfo.InvariantCulture;

            // Convert.ToString(value, IFormatProvider) — invariant provider overloads.
            Console.WriteLine(Convert.ToString(42, ci));          // 42
            Console.WriteLine(Convert.ToString(-7, ci));          // -7
            Console.WriteLine(Convert.ToString(3.5f, ci));        // 3.5
            Console.WriteLine(Convert.ToString(0.1f, ci));        // 0.1 (float round-trip)
            Console.WriteLine(Convert.ToString(-12.25f, ci));     // -12.25

            // (Convert.DefaultToType is mapped but not exercised here: reaching it through
            // IConvertible.ToType needs a boxed primitive's IConvertible dispatch map, which
            // dn2cpp does not carry — a separate limitation. The intrinsic covers the direct
            // Convert.DefaultToType(...) call the BCL makes.)

            // Convert.FromHexString(ReadOnlySpan<char>, Span<byte>, out, out).
            byte[] dst = new byte[8];
            OperationStatus s1 = Convert.FromHexString("48656C6C6F".AsSpan(), dst.AsSpan(), out int cc1, out int bw1);
            Console.WriteLine($"{(int)s1} {cc1} {bw1} {Convert.ToHexString(dst, 0, bw1)}"); // 0 10 5 48656C6C6F
            OperationStatus s2 = Convert.FromHexString("48656C6C6F".AsSpan(), dst.AsSpan(0, 3), out int cc2, out int bw2);
            Console.WriteLine($"{(int)s2} {cc2} {bw2}");           // 1 6 3 (DestinationTooSmall)
            OperationStatus s3 = Convert.FromHexString("486".AsSpan(), dst.AsSpan(), out int cc3, out int bw3);
            Console.WriteLine($"{(int)s3} {cc3} {bw3}");           // 2 2 1 (NeedMoreData)
            OperationStatus s4 = Convert.FromHexString("48ZZ".AsSpan(), dst.AsSpan(), out int cc4, out int bw4);
            Console.WriteLine($"{(int)s4} {cc4} {bw4}");           // 3 3 1 (InvalidData)

            // Boolean.Parse — round-trips (with trimming) + the throwing paths.
            Console.WriteLine(bool.Parse("True"));                 // True
            Console.WriteLine(bool.Parse("false"));                // False
            Console.WriteLine(bool.Parse("  TRUE  "));             // True (trimmed, case-insensitive)
            try { bool.Parse("nope"); } catch (Exception e) { Console.WriteLine(e.GetType().Name); } // FormatException
            try { bool.Parse(null); } catch (Exception e) { Console.WriteLine(e.GetType().Name); }   // ArgumentNullException

            // NumberFormatInfo.PerMilleSymbol (invariant).
            Console.WriteLine(NumberFormatInfo.InvariantInfo.PerMilleSymbol == "‰"); // True

            // set_CurrencyDecimalSeparator — the YamlFormatter pattern via the modeled ctor
            // path: a fresh NumberFormatInfo starts from the invariant, so writing the value
            // it already holds (".") is a true no-op. The NFI then formats exactly as the
            // invariant does. (A write that changed the separator would trap loudly.)
            var nfi = new NumberFormatInfo();
            nfi.CurrencyDecimalSeparator = ".";
            nfi.NumberDecimalSeparator = ".";
            Console.WriteLine(nfi.CurrencyDecimalSeparator);       // .
            Console.WriteLine((1234.5m).ToString("F2", nfi));      // 1234.50

            // The NFI separator setters are PLAIN FIELD WRITES into the per-instance
            // mutable copy `new NumberFormatInfo()` allocates. Here the writes reassign
            // the invariant values, so formatting is unchanged; the read-back and formats
            // diff exactly vs real .NET.
            var yaml = new NumberFormatInfo
            {
                CurrencyDecimalSeparator = ".",
                CurrencyGroupSeparator = ",",
                CurrencyGroupSizes = new[] { 3 },
                NumberDecimalSeparator = ".",
                NumberGroupSeparator = ",",
            };
            Console.WriteLine(yaml.CurrencyGroupSeparator);        // ,
            Console.WriteLine((1234567.89m).ToString("N2", yaml)); // 1,234,567.89

            // set_CurrencyGroupSizes — VALIDATE-AND-TRAP against the modeled group-size array
            // (new[] { 3 } == the invariant culture's [3], a TRUE no-op). The getter reads
            // it back and ToString("C") groups by it — all invariant, so this diffs exactly
            // vs .NET. The trap survives because ONE array serves NumberGroupSizes,
            // CurrencyGroupSizes and PercentGroupSizes, so honouring a write to one would
            // silently move the other two.
            var gs = yaml.CurrencyGroupSizes;
            Console.WriteLine($"{gs.Length} {gs[0]}");             // 1 3
            Console.WriteLine((1234567.89m).ToString("C", yaml));  // ¤1,234,567.89

            // The EXACT YamlDotNet YamlFormatter.NumberFormat object initializer, with its
            // NON-invariant values (group separators "_", 99 decimal digits, ".nan"/".inf"
            // symbols). The setters now honor these into the mutable copy, so the NFI
            // formats as real .NET does — where a validate-and-trap would instead throw at
            // runtime and break every YAML serialization. YamlFormatter formats through
            // ToString("G", nf) / Convert.ToString, which is what these lines assert;
            // "G" ignores the group sizes/decimal digits but uses the decimal/sign/NaN/inf
            // symbols. All diffed exactly vs real .NET.
            var yf = new NumberFormatInfo
            {
                CurrencyDecimalSeparator = ".",
                CurrencyGroupSeparator = "_",
                CurrencyGroupSizes = new[] { 3 },
                CurrencySymbol = string.Empty,
                CurrencyDecimalDigits = 99,
                NumberDecimalSeparator = ".",
                NumberGroupSeparator = "_",
                NumberGroupSizes = new[] { 3 },
                NumberDecimalDigits = 99,
                NaNSymbol = ".nan",
                PositiveInfinitySymbol = ".inf",
                NegativeInfinitySymbol = "-.inf",
            };
            Console.WriteLine(yf.CurrencyGroupSeparator);          // _   (honored)
            Console.WriteLine(yf.CurrencyDecimalDigits);           // 99  (honored)
            Console.WriteLine(yf.NumberGroupSeparator);            // _   (honored)
            Console.WriteLine((1234.5).ToString("G", yf));         // 1234.5
            Console.WriteLine((-0.25).ToString("G", yf));          // -0.25
            Console.WriteLine((6789.0).ToString("G", yf));         // 6789
            Console.WriteLine(double.NaN.ToString("G", yf));       // .nan
            Console.WriteLine(double.PositiveInfinity.ToString("G", yf)); // .inf
            Console.WriteLine(double.NegativeInfinity.ToString("G", yf)); // -.inf
        }
    }
}
