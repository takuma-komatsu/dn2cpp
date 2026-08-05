#nullable disable
using System;

namespace ConvertFromBaseMoreSubset
{
    // the rest of the Convert.To*(string, fromBase) family — the sub-word
    // (Byte/SByte/Int16/UInt16) and unsigned (UInt32/UInt64) targets, the
    // '+'/'-'/"0x" prefix rules, the raw-bit-pattern semantics of the
    // non-decimal bases ("FF" -> byte 255 / sbyte -1), the Argument/Format/
    // Overflow exception classification — plus the catchable OverflowException
    // from the double -> decimal conversions (Convert.ToDecimal and the cast).
    internal static class Program
    {
        private static void E(string label, Action a)
        {
            try
            {
                a();
                Console.WriteLine(label + ": ok");
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name);
            }
        }

        internal static void __GateEntry()
        {
            // Happy paths across bases and widths.
            Console.WriteLine(Convert.ToByte("FF", 16));
            Console.WriteLine(Convert.ToByte("11111111", 2));
            Console.WriteLine(Convert.ToByte("377", 8));
            Console.WriteLine(Convert.ToByte("0xFF", 16));
            Console.WriteLine(Convert.ToByte("+77", 10));
            Console.WriteLine(Convert.ToByte(null, 16));
            Console.WriteLine(Convert.ToSByte("FF", 16));
            Console.WriteLine(Convert.ToSByte("80", 16));
            Console.WriteLine(Convert.ToSByte("-128", 10));
            Console.WriteLine(Convert.ToInt16("FFFF", 16));
            Console.WriteLine(Convert.ToInt16("8000", 16));
            Console.WriteLine(Convert.ToInt16("-100", 10));
            Console.WriteLine(Convert.ToUInt16("FFFF", 16));
            Console.WriteLine(Convert.ToUInt16("1010", 2));
            Console.WriteLine(Convert.ToUInt32("FFFFFFFF", 16));
            Console.WriteLine(Convert.ToUInt32("+5", 10));
            Console.WriteLine(Convert.ToUInt64("FFFFFFFFFFFFFFFF", 16));
            Console.WriteLine(Convert.ToUInt64("18446744073709551615", 10));
            Console.WriteLine(Convert.ToInt64("0x7fffffffffffffff", 16));
            Console.WriteLine(Convert.ToInt32("0xff", 16));

            // Exception classification.
            E("byte-ovf-16", () => Convert.ToByte("100", 16));
            E("sbyte-ovf-16", () => Convert.ToSByte("100", 16));
            E("u16-ovf-16", () => Convert.ToUInt16("10000", 16));
            E("byte-ovf-10", () => Convert.ToByte("256", 10));
            E("sbyte-ovf-10", () => Convert.ToSByte("128", 10));
            E("byte-bad-base", () => Convert.ToByte("2", 3));
            E("byte-minus-16", () => Convert.ToByte("-1", 16));
            E("byte-minus-10", () => Convert.ToByte("-1", 10));
            E("byte-minus-zero", () => Convert.ToByte("-0", 10));
            E("u16-minus-10", () => Convert.ToUInt16("-1", 10));
            E("u32-minus-10", () => Convert.ToUInt32("-1", 10));
            E("u64-minus-10", () => Convert.ToUInt64("-1", 10));
            E("byte-empty", () => Convert.ToByte("", 16));
            E("byte-ws", () => Convert.ToByte(" FF", 16));
            E("byte-bad-digit", () => Convert.ToByte("12", 2));
            E("byte-just-prefix", () => Convert.ToByte("0x", 16));
            E("i32-minus-16", () => Convert.ToInt32("-1", 16));
            E("i64-ovf-16", () => Convert.ToInt64("10000000000000000", 16));

            // double -> decimal overflow is catchable (Convert and the cast).
            double big = 1e30;
            E("todecimal-1e30", () => Convert.ToDecimal(big));
            E("cast-decimal-1e30", () =>
            {
                decimal d = (decimal)big;
                Console.WriteLine(d);
            });
            E("todecimal-nan", () => Convert.ToDecimal(double.NaN));
            float bigf = 1e30f;
            E("cast-decimal-float", () =>
            {
                decimal d = (decimal)bigf;
                Console.WriteLine(d);
            });
        }
    }
}
