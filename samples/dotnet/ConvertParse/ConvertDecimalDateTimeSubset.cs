#nullable disable
using System;
using System.Globalization;

namespace ConvertDecimalDateTimeSubset
{
    // Convert.ToDecimal over string (NumberStyles.Number; a null string yields
    // 0m) / floating / integral / bool / decimal-identity / boxed-object
    // sources, and Convert.ToDateTime over string (null -> DateTime.MinValue) /
    // DateTime-identity / boxed-object sources, plus the thrown exception types.
    internal static class Program
    {
        private static void Show<T>(string tag, Func<T> f)
        {
            try { Console.WriteLine(tag + " = " + f()); }
            catch (Exception ex) { Console.WriteLine(tag + " ! " + ex.GetType().Name); }
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("-- ToDecimal --");
            Show("dec \"1.5\"", () => Convert.ToDecimal("1.5"));
            Show("dec \"1,234.56\"", () => Convert.ToDecimal("1,234.56", CultureInfo.InvariantCulture));
            Show("dec \" -7.25 \"", () => Convert.ToDecimal(" -7.25 ", CultureInfo.InvariantCulture));
            Show("dec null", () => Convert.ToDecimal((string)null));
            Show("dec \"\"", () => Convert.ToDecimal(""));
            Show("dec \"abc\"", () => Convert.ToDecimal("abc"));
            Show("dec 1.5d", () => Convert.ToDecimal(1.5d));
            Show("dec 0.1d", () => Convert.ToDecimal(0.1d));
            Show("dec 0.1f", () => Convert.ToDecimal(0.1f));
            Show("dec true", () => Convert.ToDecimal(true));
            Show("dec false", () => Convert.ToDecimal(false));
            Show("dec int.Min", () => Convert.ToDecimal(int.MinValue));
            Show("dec uint.Max", () => Convert.ToDecimal(uint.MaxValue));
            Show("dec long.Min", () => Convert.ToDecimal(long.MinValue));
            Show("dec ulong.Max", () => Convert.ToDecimal(ulong.MaxValue));
            Show("dec (byte)9", () => Convert.ToDecimal((byte)9));
            Show("dec (short)-3", () => Convert.ToDecimal((short)-3));
            Show("dec 2.75m", () => Convert.ToDecimal(2.75m));
            Show("dec (object)3.25", () => Convert.ToDecimal((object)3.25));
            Show("dec (object)\"6.5\"", () => Convert.ToDecimal((object)"6.5"));
            Show("dec (object)42", () => Convert.ToDecimal((object)42));
            Show("dec (object)null", () => Convert.ToDecimal((object)null));

            Console.WriteLine("-- ToDateTime --");
            Show("dt str", () => Convert.ToDateTime("2020-02-29 13:45:30").ToString("yyyy-MM-dd HH:mm:ss"));
            Show("dt null", () => Convert.ToDateTime((string)null).ToString("yyyy-MM-dd HH:mm:ss"));
            Show("dt \"\"", () => Convert.ToDateTime("").ToString("yyyy-MM-dd"));
            Show("dt \"nope\"", () => Convert.ToDateTime("nope").ToString("yyyy-MM-dd"));
            Show("dt prov", () => Convert.ToDateTime("2020-01-02", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"));
            Show("dt identity", () => Convert.ToDateTime(new DateTime(2021, 3, 4, 5, 6, 7)).ToString("yyyy-MM-dd HH:mm:ss"));
            Show("dt (object)dt", () => Convert.ToDateTime((object)new DateTime(2021, 3, 4)).ToString("yyyy-MM-dd"));
            Show("dt (object)str", () => Convert.ToDateTime((object)"2022-12-31").ToString("yyyy-MM-dd"));
            Show("dt (object)null", () => Convert.ToDateTime((object)null).ToString("yyyy-MM-dd"));
        }
    }
}
