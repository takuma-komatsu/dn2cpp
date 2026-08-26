using System;
using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace FloatParseFormatInfo
{
    // Gate driver: the UTF-8 float parser (System.Buffers.Text.Utf8Parser) and the
    // span-based double/float Parse family. Both reach Number.NumberToFloatingPointBits
    // over TFloat : IBinaryFloatParseAndFormatInfo<TFloat>, whose static abstract
    // constants are explicit impls on Double/Single. Every value is printed with the
    // shortest round-trip format so the native side must agree bit-for-bit with real
    // .NET, including subnormals, overflow to infinity and the slow (big-integer) path.
    internal static class Program
    {
        private static readonly string[] Inputs =
        {
            "0", "-0", "1", "1.5", "3.141592653589793", "2.5e-3", "1e308", "1e309", "-1e309",
            "4.9406564584124654E-324", "2.2250738585072014E-308", "1.7976931348623157E+308",
            "123456789012345678901234567890", "0.1", "0.30000000000000004",
            "9007199254740993", "1.00000000000000011102230246251565404236316680908203125",
            "1.4e-45", "3.4028235E+38", "3.4028236E+38", "16777217", "0.1000000000000000055511151231257827",
            "NaN", "Infinity", "-Infinity", "abc", "",
        };

        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            foreach (string text in Inputs)
            {
                byte[] utf8 = Encoding.UTF8.GetBytes(text);
                bool okD = Utf8Parser.TryParse(utf8, out double d, out int consumedD);
                bool okF = Utf8Parser.TryParse(utf8, out float f, out int consumedF);
                bool okSpanD = double.TryParse(text.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out double sd);
                bool okSpanF = float.TryParse(text.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out float sf);
                Console.WriteLine(
                    $"{text}|utf8:{okD}:{consumedD}:{d.ToString("R", CultureInfo.InvariantCulture)}"
                    + $"|utf8f:{okF}:{consumedF}:{f.ToString("R", CultureInfo.InvariantCulture)}"
                    + $"|span:{okSpanD}:{sd.ToString("R", CultureInfo.InvariantCulture)}"
                    + $"|spanf:{okSpanF}:{sf.ToString("R", CultureInfo.InvariantCulture)}");
            }

            // The bit-reinterpreting members (BitsToFloat / FloatToBits) are reached by
            // the same parser on the slow path; pin them directly as well.
            Console.WriteLine(BitConverter.DoubleToUInt64Bits(double.Parse("1e-320", CultureInfo.InvariantCulture)).ToString("X16"));
            Console.WriteLine(BitConverter.SingleToUInt32Bits(float.Parse("1e-42", CultureInfo.InvariantCulture)).ToString("X8"));
        }
    }
}
